using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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

        private CancellationTokenSource sequenceCancellationTokenSource;
        private int currentChapterIndex;
        private string requestedStartChapterId;

        public int CurrentChapterIndex => currentChapterIndex;

        private void Awake()
        {
            ConsumeStartContext();
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
        }

        public void RestartSequence()
        {
            CancelRunningSequence();
            ApplyIdleView();

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
                OperationPartResult result =
                    await operationPartManager.PlayAsync(chapter.ChapterId, cancellationToken);
                operationPartManager.Hide();

                if (result.IsSuccess)
                {
                    await PlayStoryIfAssignedAsync(chapter.SuccessStory, cancellationToken);
                    return;
                }

                StoryAsset failureStory = chapter.ResolveFailureStory(result.JudgementId);
                await PlayStoryIfAssignedAsync(failureStory, cancellationToken);
            }
        }

        private async UniTask PlayStoryIfAssignedAsync(
            StoryAsset storyAsset,
            CancellationToken cancellationToken)
        {
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

            SceneTransitionManager.LoadScene(titleSceneName);
        }

        private void ApplyIdleView()
        {
            conversationPartManager?.Hide();
            operationPartManager?.Hide();
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
