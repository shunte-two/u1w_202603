using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Localization;
using U1W.SceneManagement;

namespace U1W.Game
{
    public sealed class SequenceManager : MonoBehaviour
    {
        private enum RelativeOrderRequirement
        {
            Any = 0,
            BeforeReferenceCard = 1,
            AfterReferenceCard = 2
        }

        [Serializable]
        private sealed class FailureCardCondition
        {
            [SerializeField] private string cardId = "card";
            [SerializeField] private bool requireFaceMatch = true;
            [SerializeField] private bool isFlipped;
            [SerializeField] private bool requireOrderMatch = true;
            [SerializeField] [Min(0)] private int orderIndex;
            [SerializeField] private RelativeOrderRequirement relativeOrderRequirement;
            [SerializeField] private string referenceCardId = "card";

            public bool Matches(OperationStateSnapshot operationStateSnapshot)
            {
                if (operationStateSnapshot == null ||
                    string.IsNullOrWhiteSpace(cardId) ||
                    !operationStateSnapshot.TryFindCard(cardId, out OperationCardStateSnapshot card))
                {
                    return false;
                }

                if (requireFaceMatch && card.IsFlipped != isFlipped)
                {
                    return false;
                }

                if (requireOrderMatch && card.OrderIndex != orderIndex)
                {
                    return false;
                }

                if (!MatchesRelativeOrder(operationStateSnapshot, card))
                {
                    return false;
                }

                return true;
            }

            private bool MatchesRelativeOrder(
                OperationStateSnapshot operationStateSnapshot,
                OperationCardStateSnapshot card)
            {
                if (relativeOrderRequirement == RelativeOrderRequirement.Any)
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(referenceCardId) ||
                    string.Equals(cardId, referenceCardId, StringComparison.Ordinal) ||
                    !operationStateSnapshot.TryFindCard(
                        referenceCardId,
                        out OperationCardStateSnapshot referenceCard))
                {
                    return false;
                }

                return relativeOrderRequirement switch
                {
                    RelativeOrderRequirement.BeforeReferenceCard =>
                        card.OrderIndex < referenceCard.OrderIndex,
                    RelativeOrderRequirement.AfterReferenceCard =>
                        card.OrderIndex > referenceCard.OrderIndex,
                    _ => true
                };
            }
        }

        [Serializable]
        private sealed class ChapterFailureBranch
        {
            [SerializeField] private string judgementId = string.Empty;
            [SerializeField] private FailureCardCondition[] cardConditions =
                Array.Empty<FailureCardCondition>();
            [SerializeField] private StoryAsset story;

            public StoryAsset Story => story;

            public bool Matches(OperationPartResult result)
            {
                if (!string.IsNullOrWhiteSpace(judgementId) &&
                    !string.Equals(judgementId, result.JudgementId, StringComparison.Ordinal))
                {
                    return false;
                }

                if (cardConditions == null || cardConditions.Length == 0)
                {
                    return true;
                }

                for (int i = 0; i < cardConditions.Length; i++)
                {
                    FailureCardCondition condition = cardConditions[i];
                    if (condition == null || !condition.Matches(result.StateSnapshot))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        [Serializable]
        private sealed class ChapterSequenceDefinition
        {
            [SerializeField] private string chapterId = "chapter1";
            [SerializeField] private LocalizedString chapterTitle;
            [SerializeField] private string chapterTitleFallback = string.Empty;
            [SerializeField] private StoryAsset openingStory;
            [SerializeField] private StoryAsset successStory;
            [SerializeField] private StoryAsset defaultFailureStory;
            [SerializeField] private ChapterFailureBranch[] failureBranches = Array.Empty<ChapterFailureBranch>();

            public string ChapterId => chapterId;
            public LocalizedString ChapterTitle => chapterTitle;
            public string ChapterTitleFallback => chapterTitleFallback;
            public StoryAsset OpeningStory => openingStory;
            public StoryAsset SuccessStory => successStory;

            public StoryAsset ResolveFailureStory(OperationPartResult result)
            {
                if (failureBranches != null)
                {
                    for (int i = 0; i < failureBranches.Length; i++)
                    {
                        ChapterFailureBranch branch = failureBranches[i];
                        if (branch == null)
                        {
                            continue;
                        }

                        if (branch.Matches(result))
                        {
                            return branch.Story;
                        }
                    }
                }

                return defaultFailureStory;
            }
        }

        [Header("Managers")]
        [SerializeField] private ConversationPartManager conversationPartManager;
        [SerializeField] private OperationPartManager operationPartManager;
        [SerializeField] private ChapterIntroOverlay chapterIntroOverlay;

        [Header("Chapter Sequence")]
        [SerializeField] private StoryAsset openingStory;
        [SerializeField] private int startChapterIndex;
        [SerializeField] private ChapterSequenceDefinition[] chapters = Array.Empty<ChapterSequenceDefinition>();

        [Header("Ending")]
        [SerializeField] private StoryAsset endingStory;
        [SerializeField] private string titleSceneName = "Title";
        [SerializeField] [Min(0f)] private float endingTransitionBlackoutDuration;
        [SerializeField] [Min(0f)] private float endingTransitionFadeInDuration;

        [Header("Character Focus")]
        [SerializeField] private Transform characterFocusRoot;
        [SerializeField] private Vector3 operationCharacterLocalOffset = new(0f, 2.25f, 0f);
        [SerializeField] [Min(0f)] private float characterFocusMoveDuration = 0.45f;
        [SerializeField] private Ease characterFocusMoveEase = Ease.InOutSine;
        [SerializeField] [Min(0f)] private float characterFocusSkipDistance = 0.001f;

        private CancellationTokenSource sequenceCancellationTokenSource;
        private Tween characterFocusTween;
        private int currentChapterIndex;
        private Vector3 defaultCharacterLocalPosition;
        private bool hasDefaultCharacterLocalPosition;
        private string requestedStartChapterId;
        private bool shouldSkipOpeningStory;

        public int CurrentChapterIndex => currentChapterIndex;

        private void Awake()
        {
            ConsumeStartContext();
            CacheCharacterDefaultPosition();
            ValidateReferences();
            BindListeners();
            ApplyIdleView();
        }

        private void OnValidate()
        {
            ValidateReferences();
        }

        private void Start()
        {
            RestartSequence();
        }

        private void OnDestroy()
        {
            UnbindListeners();
            CancelRunningSequence();
            KillCharacterFocusTween(false);
        }

        public void RestartSequence()
        {
            CancelRunningSequence();
            ApplyIdleView();
            conversationPartManager?.ResetConversationLog();

            if (!HasValidChapterConfiguration())
            {
                return;
            }

            currentChapterIndex = GetInitialChapterIndex();

            sequenceCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

            RunSequenceAsync(sequenceCancellationTokenSource.Token).Forget();
        }

        private async UniTaskVoid RunSequenceAsync(CancellationToken cancellationToken)
        {
            try
            {
                ChapterSequenceDefinition[] sequenceChapters = GetPlayableChapters();
                if (sequenceChapters.Length == 0)
                {
                    Debug.LogError("SequenceManager requires at least one configured chapter.", this);
                    return;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                if (!shouldSkipOpeningStory)
                {
                    await PlayStoryIfAssignedAsync(openingStory, cancellationToken);
                }

                for (; currentChapterIndex < sequenceChapters.Length; currentChapterIndex++)
                {
                    await RunChapterAsync(sequenceChapters[currentChapterIndex], cancellationToken);
                }

                await RunEndingAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async UniTask RunChapterAsync(
            ChapterSequenceDefinition chapter,
            CancellationToken cancellationToken)
        {
            if (chapter == null)
            {
                return;
            }

            ChapterProgressStore.MarkReached(chapter.ChapterId);
            await PlayChapterIntroAsync(chapter, currentChapterIndex, cancellationToken);
            await PlayStoryIfAssignedAsync(chapter.OpeningStory, cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                await TransitionToOperationAsync(cancellationToken);
                OperationPartResult result =
                    await operationPartManager.PlayAsync(chapter.ChapterId, cancellationToken);

                if (result.IsSuccess)
                {
                    operationPartManager.Hide();
                    await PlayStoryIfAssignedAsync(chapter.SuccessStory, cancellationToken);
                    return;
                }

                operationPartManager.Hide(clearCardState: false);
                StoryAsset failureStory = chapter.ResolveFailureStory(result);
                await PlayStoryIfAssignedAsync(failureStory, cancellationToken);
            }
        }

        private async UniTask PlayStoryIfAssignedAsync(
            StoryAsset storyAsset,
            CancellationToken cancellationToken)
        {
            await TransitionToConversationAsync(cancellationToken);

            if (storyAsset == null)
            {
                conversationPartManager.Hide();
                return;
            }

            await conversationPartManager.PlayAsync(storyAsset, cancellationToken);
            conversationPartManager.Hide();
        }

        private async UniTask PlayChapterIntroAsync(
            ChapterSequenceDefinition chapter,
            int chapterIndex,
            CancellationToken cancellationToken)
        {
            if (chapterIntroOverlay == null || chapter == null)
            {
                return;
            }

            await chapterIntroOverlay.PlayAsync(
                chapter.ChapterTitle,
                ResolveChapterTitleFallback(chapter, chapterIndex),
                cancellationToken);
        }

        private async UniTask RunEndingAsync(CancellationToken cancellationToken)
        {
            operationPartManager.Hide();
            await PlayStoryIfAssignedAsync(endingStory, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(titleSceneName))
            {
                Debug.LogWarning(
                    "SequenceManager ending completed, but titleSceneName is empty. Falling back to completed view.",
                    this);
                operationPartManager.ShowCompleted();
                return;
            }

            SceneTransitionManager.LoadScene(
                titleSceneName,
                blackoutDuration: endingTransitionBlackoutDuration,
                fadeInDurationOverride: endingTransitionFadeInDuration);
        }

        private void ApplyIdleView()
        {
            conversationPartManager?.Hide();
            operationPartManager?.Hide();
            chapterIntroOverlay?.HideImmediate();
            MoveCharacterToDefaultImmediate();
        }

        private void BindListeners()
        {
            if (operationPartManager == null)
            {
                return;
            }

            operationPartManager.RestartRequested -= RestartSequence;
            operationPartManager.RestartRequested += RestartSequence;
        }

        private void UnbindListeners()
        {
            if (operationPartManager == null)
            {
                return;
            }

            operationPartManager.RestartRequested -= RestartSequence;
        }

        private void CancelRunningSequence()
        {
            if (sequenceCancellationTokenSource == null)
            {
                return;
            }

            sequenceCancellationTokenSource.Cancel();
            sequenceCancellationTokenSource.Dispose();
            sequenceCancellationTokenSource = null;
        }

        private void CacheCharacterDefaultPosition()
        {
            if (characterFocusRoot == null)
            {
                hasDefaultCharacterLocalPosition = false;
                defaultCharacterLocalPosition = Vector3.zero;
                return;
            }

            defaultCharacterLocalPosition = characterFocusRoot.localPosition;
            hasDefaultCharacterLocalPosition = true;
        }

        private void MoveCharacterToDefaultImmediate()
        {
            if (!hasDefaultCharacterLocalPosition || characterFocusRoot == null)
            {
                return;
            }

            KillCharacterFocusTween(false);
            characterFocusRoot.localPosition = defaultCharacterLocalPosition;
        }

        private UniTask TransitionToConversationAsync(CancellationToken cancellationToken)
        {
            return AnimateCharacterFocusAsync(defaultCharacterLocalPosition, cancellationToken);
        }

        private UniTask TransitionToOperationAsync(CancellationToken cancellationToken)
        {
            return AnimateCharacterFocusAsync(
                defaultCharacterLocalPosition + operationCharacterLocalOffset,
                cancellationToken);
        }

        private async UniTask AnimateCharacterFocusAsync(
            Vector3 targetLocalPosition,
            CancellationToken cancellationToken)
        {
            if (!hasDefaultCharacterLocalPosition || characterFocusRoot == null)
            {
                return;
            }

            KillCharacterFocusTween(false);

            if ((characterFocusRoot.localPosition - targetLocalPosition).sqrMagnitude <=
                characterFocusSkipDistance * characterFocusSkipDistance)
            {
                characterFocusRoot.localPosition = targetLocalPosition;
                return;
            }

            if (characterFocusMoveDuration <= 0f)
            {
                characterFocusRoot.localPosition = targetLocalPosition;
                return;
            }

            bool completed = false;
            characterFocusTween = characterFocusRoot
                .DOLocalMove(targetLocalPosition, characterFocusMoveDuration)
                .SetEase(characterFocusMoveEase)
                .OnComplete(() => completed = true)
                .OnKill(() =>
                {
                    characterFocusTween = null;
                    completed = true;
                });

            try
            {
                await UniTask.WaitUntil(() => completed, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                KillCharacterFocusTween(false);
                throw;
            }
        }

        private void KillCharacterFocusTween(bool complete)
        {
            if (characterFocusTween == null)
            {
                return;
            }

            Tween tween = characterFocusTween;
            characterFocusTween = null;
            if (tween.IsActive())
            {
                tween.Kill(complete);
            }
        }

        private int GetInitialChapterIndex()
        {
            ChapterSequenceDefinition[] playableChapters = GetPlayableChapters();
            if (playableChapters.Length == 0)
            {
                return 0;
            }

            if (TryResolveRequestedStartChapterIndex(playableChapters, out int requestedIndex))
            {
                return requestedIndex;
            }

            return Mathf.Clamp(startChapterIndex, 0, playableChapters.Length - 1);
        }

        private void ConsumeStartContext()
        {
            if (GameSceneStartContext.TryConsumeRequestedChapter(out string chapterId))
            {
                requestedStartChapterId = chapterId;
            }

            shouldSkipOpeningStory = GameSceneStartContext.TryConsumeShouldSkipOpeningStory();
        }

        private bool TryResolveRequestedStartChapterIndex(
            ChapterSequenceDefinition[] playableChapters,
            out int chapterIndex)
        {
            chapterIndex = 0;
            if (string.IsNullOrWhiteSpace(requestedStartChapterId))
            {
                return false;
            }

            for (int i = 0; i < playableChapters.Length; i++)
            {
                ChapterSequenceDefinition chapter = playableChapters[i];
                if (chapter == null)
                {
                    continue;
                }

                if (string.Equals(chapter.ChapterId, requestedStartChapterId, StringComparison.Ordinal))
                {
                    chapterIndex = i;
                    requestedStartChapterId = null;
                    return true;
                }
            }

            Debug.LogWarning(
                $"SequenceManager could not resolve requested start chapter '{requestedStartChapterId}'. Falling back to startChapterIndex.",
                this);
            requestedStartChapterId = null;
            return false;
        }

        private ChapterSequenceDefinition[] GetPlayableChapters()
        {
            return chapters ?? Array.Empty<ChapterSequenceDefinition>();
        }

        private bool HasValidChapterConfiguration()
        {
            return chapters != null && chapters.Length > 0;
        }

        private void ValidateReferences()
        {
            WarnIfMissing(conversationPartManager, nameof(conversationPartManager));
            WarnIfMissing(operationPartManager, nameof(operationPartManager));
            WarnIfMissing(chapterIntroOverlay, nameof(chapterIntroOverlay));
            WarnIfMissing(characterFocusRoot, nameof(characterFocusRoot));

            if (!HasValidChapterConfiguration())
            {
                Debug.LogError("SequenceManager requires at least one configured chapter.", this);
            }
        }

        private static void WarnIfMissing(UnityEngine.Object target, string fieldName)
        {
            if (target == null)
            {
                Debug.LogWarning($"SequenceManager requires {fieldName} to be assigned via SerializeField.");
            }
        }

        private static string ResolveChapterTitleFallback(
            ChapterSequenceDefinition chapter,
            int chapterIndex)
        {
            if (chapter != null && !string.IsNullOrWhiteSpace(chapter.ChapterTitleFallback))
            {
                return chapter.ChapterTitleFallback;
            }

            return $"CHAPTER {chapterIndex + 1}";
        }
    }
}
