using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using U1W.SceneManagement;

namespace U1W.Game
{
    public sealed class SequenceManager : MonoBehaviour
    {
        [Serializable]
        private sealed class ChapterFailureBranch
        {
            [SerializeField] private string judgementId = string.Empty;
            [SerializeField] private StoryAsset story;

            public string JudgementId => judgementId;
            public StoryAsset Story => story;
        }

        [Serializable]
        private sealed class ChapterSequenceDefinition
        {
            [SerializeField] private string chapterId = "chapter1";
            [SerializeField] private StoryAsset openingStory;
            [SerializeField] private StoryAsset successStory;
            [SerializeField] private StoryAsset defaultFailureStory;
            [SerializeField] private ChapterFailureBranch[] failureBranches = Array.Empty<ChapterFailureBranch>();

            public string ChapterId => chapterId;
            public StoryAsset OpeningStory => openingStory;
            public StoryAsset SuccessStory => successStory;

            public StoryAsset ResolveFailureStory(string judgementId)
            {
                if (!string.IsNullOrWhiteSpace(judgementId) && failureBranches != null)
                {
                    for (int i = 0; i < failureBranches.Length; i++)
                    {
                        ChapterFailureBranch branch = failureBranches[i];
                        if (branch == null)
                        {
                            continue;
                        }

                        if (string.Equals(
                                branch.JudgementId,
                                judgementId,
                                StringComparison.Ordinal))
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

        [Header("Chapter Sequence")]
        [SerializeField] private int startChapterIndex;
        [SerializeField] private ChapterSequenceDefinition[] chapters = Array.Empty<ChapterSequenceDefinition>();

        [Header("Ending")]
        [SerializeField] private StoryAsset endingStory;
        [SerializeField] private string titleSceneName = "Title";
        [SerializeField] [Min(0f)] private float endingTransitionBlackoutDuration;

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
                StoryAsset failureStory = chapter.ResolveFailureStory(result.JudgementId);
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
                blackoutDuration: endingTransitionBlackoutDuration);
        }

        private void ApplyIdleView()
        {
            conversationPartManager?.Hide();
            operationPartManager?.Hide();
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
    }
}
