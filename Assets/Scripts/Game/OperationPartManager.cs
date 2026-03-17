using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace U1W.Game
{
    public readonly struct OperationPartResult
    {
        public OperationPartResult(string chapterId, string judgementId, bool isSuccess)
        {
            ChapterId = chapterId;
            JudgementId = judgementId;
            IsSuccess = isSuccess;
        }

        public string ChapterId { get; }
        public string JudgementId { get; }
        public bool IsSuccess { get; }
    }

    public sealed class OperationPartManager : MonoBehaviour
    {
        [Serializable]
        private sealed class ChapterOperationDefinition
        {
            [SerializeField] private string chapterId = "chapter1";
            [SerializeField] private OperationChapterAsset operationAsset;
            [SerializeField] private LocalizedString phaseTitle;
            [SerializeField] private string phaseTitleFallback = "操作パート";
            [SerializeField] private LocalizedString instruction;
            [SerializeField] [TextArea(2, 4)] private string instructionFallback =
                "カードをクリックして解釈を切り替え、ドラッグで順番を入れ替えてください。";
            [SerializeField] private string successJudgementId = "success";
            [SerializeField] private string defaultJudgementIdOnComplete = "incorrect";

            public string ChapterId => chapterId;
            public OperationChapterAsset OperationAsset => operationAsset;
            public LocalizedString PhaseTitle => phaseTitle;
            public string PhaseTitleFallback => phaseTitleFallback;
            public LocalizedString Instruction => instruction;
            public string InstructionFallback => instructionFallback;
            public string SuccessJudgementId => successJudgementId;
            public string DefaultJudgementIdOnComplete => defaultJudgementIdOnComplete;
        }

        [Header("References")]
        [SerializeField] private GameObject operationRoot;
        [SerializeField] private TextMeshProUGUI phaseTitleText;
        [SerializeField] private TextMeshProUGUI operationText;
        [SerializeField] private RectTransform cardAreaRoot;
        [SerializeField] private OperationCardView cardViewPrefab;
        [SerializeField] private RectTransform cardDescriptionPopupRoot;
        [SerializeField] private TextMeshProUGUI cardDescriptionText;
        [SerializeField] private Button completeOperationButton;
        [SerializeField] private Button restartSequenceButton;

        [Header("Card Labels")]
        [SerializeField] private LocalizedString frontFaceLabel;
        [SerializeField] private string frontFaceLabelFallback = "表の解釈";
        [SerializeField] private LocalizedString backFaceLabel;
        [SerializeField] private string backFaceLabelFallback = "裏の解釈";

        [Header("Card Layout")]
        [SerializeField] private float cardSpacing = 24f;
        [SerializeField] private float cardSlideDuration = 0.2f;
        [SerializeField] private Ease cardSlideEase = Ease.OutCubic;
        [SerializeField] private float popupVerticalOffset = 180f;
        [SerializeField] private string emptyDescriptionFallback = "説明は未設定です。";

        [Header("Default Display")]
        [SerializeField] private LocalizedString operationPhaseTitle;
        [SerializeField] private string operationPhaseTitleFallback = "操作パート";
        [SerializeField] private LocalizedString operationInstruction;
        [SerializeField] [TextArea(2, 4)] private string operationInstructionFallback =
            "カードをクリックして解釈を切り替え、ドラッグで順番を入れ替えてください。";
        [SerializeField] private LocalizedString completedPhaseTitle;
        [SerializeField] private string completedPhaseTitleFallback = "進行完了";
        [SerializeField] private LocalizedString completedMessage;
        [SerializeField] [TextArea(2, 4)] private string completedMessageFallback =
            "全チャプター完了です。Restart で最初から確認できます。";

        [Header("Chapter Definitions")]
        [SerializeField] private ChapterOperationDefinition[] chapterDefinitions =
            Array.Empty<ChapterOperationDefinition>();

        private bool completeRequested;
        private bool listenersBound;
        private LocalizedString boundOperationText;
        private LocalizedString boundPhaseTitle;
        private ChapterOperationDefinition currentChapterDefinition;
        private string currentChapterId = string.Empty;
        private string pendingJudgementId = string.Empty;
        private OperationCardInteractionController cardInteractionController;

        public event Action RestartRequested;

        private void Awake()
        {
            ValidateReferences();
            CreateCardInteractionController();
            BindListeners();
            Hide();
        }

        private void OnValidate()
        {
            ValidateReferences();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !IsAwaitingComplete())
            {
                return;
            }

            if (keyboard.enterKey.wasPressedThisFrame)
            {
                RequestComplete();
            }
        }

        private void OnDestroy()
        {
            cardInteractionController?.Dispose();
            ReleaseBindings();
            UnbindListeners();
        }

        public async UniTask<OperationPartResult> PlayAsync(
            string chapterId,
            CancellationToken cancellationToken)
        {
            PrepareChapter(chapterId);
            await ShowOperationViewAsync(cancellationToken);
            await BuildCardsAsync(cancellationToken);
            completeRequested = false;
            await UniTask.WaitUntil(() => completeRequested, cancellationToken: cancellationToken);
            return BuildCurrentResult();
        }

        public void ShowCompleted()
        {
            ShowCompletedAsync(destroyCancellationToken).Forget();
        }

        public void SetPendingJudgementId(string judgementId)
        {
            pendingJudgementId = judgementId ?? string.Empty;
        }

        public void ResetPendingJudgementId()
        {
            pendingJudgementId = ResolveDefaultJudgementId(currentChapterDefinition);
        }

        public void Hide()
        {
            SetRootState(false);
            SetButtonState(completeOperationButton, false);
            SetButtonState(restartSequenceButton, false);
            ReleaseBindings();
            cardInteractionController?.HideCardDescription();
            cardInteractionController?.Clear();
            SetOperationText(string.Empty);
        }

        private void CreateCardInteractionController()
        {
            cardInteractionController?.Dispose();
            cardInteractionController = new OperationCardInteractionController(
                cardAreaRoot,
                cardViewPrefab,
                cardDescriptionPopupRoot,
                cardDescriptionText,
                cardSpacing,
                cardSlideDuration,
                cardSlideEase,
                popupVerticalOffset,
                emptyDescriptionFallback);
        }

        private async UniTask ShowCompletedAsync(CancellationToken cancellationToken)
        {
            SetRootState(true);
            SetButtonState(completeOperationButton, false);
            SetButtonState(restartSequenceButton, true);
            cardInteractionController?.Clear();
            cardInteractionController?.HideCardDescription();
            BindPhaseTitle(completedPhaseTitle, completedPhaseTitleFallback);
            await SetOperationTextAsync(completedMessage, completedMessageFallback, cancellationToken);
        }

        private async UniTask ShowOperationViewAsync(CancellationToken cancellationToken)
        {
            SetRootState(true);
            SetButtonState(completeOperationButton, true);
            SetButtonState(restartSequenceButton, false);
            BindPhaseTitle(
                currentChapterDefinition?.PhaseTitle ?? operationPhaseTitle,
                currentChapterDefinition?.PhaseTitleFallback ?? operationPhaseTitleFallback);
            await SetOperationTextAsync(
                currentChapterDefinition?.Instruction ?? operationInstruction,
                currentChapterDefinition?.InstructionFallback ?? operationInstructionFallback,
                cancellationToken);
        }

        private void PrepareChapter(string chapterId)
        {
            currentChapterId = chapterId ?? string.Empty;
            currentChapterDefinition = FindChapterDefinition(currentChapterId);
            ResetPendingJudgementId();
        }

        private OperationPartResult BuildCurrentResult()
        {
            string resolvedJudgementId = string.IsNullOrWhiteSpace(pendingJudgementId)
                ? ResolveDefaultJudgementId(currentChapterDefinition)
                : pendingJudgementId;
            string successJudgementId = ResolveSuccessJudgementId(currentChapterDefinition);
            bool isSuccess = string.Equals(
                resolvedJudgementId,
                successJudgementId,
                StringComparison.Ordinal);

            return new OperationPartResult(currentChapterId, resolvedJudgementId, isSuccess);
        }

        private bool IsAwaitingComplete()
        {
            return operationRoot != null &&
                   operationRoot.activeInHierarchy &&
                   completeOperationButton != null &&
                   completeOperationButton.gameObject.activeInHierarchy;
        }

        private void RequestComplete()
        {
            pendingJudgementId = ResolveJudgementIdOnComplete();
            completeRequested = true;
        }

        private void RequestRestart()
        {
            RestartRequested?.Invoke();
        }

        private void SetRootState(bool isActive)
        {
            if (operationRoot != null && operationRoot.activeSelf != isActive)
            {
                operationRoot.SetActive(isActive);
            }
        }

        private void SetButtonState(Button button, bool isEnabled)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = isEnabled;
            button.gameObject.SetActive(isEnabled);
        }

        private void SetOperationText(string value)
        {
            if (operationText != null)
            {
                operationText.text = value;
            }
        }

        private void SetPhaseTitle(string value)
        {
            if (phaseTitleText != null)
            {
                phaseTitleText.text = value;
            }
        }

        private async UniTask SetOperationTextAsync(
            LocalizedString localizedString,
            string fallbackValue,
            CancellationToken cancellationToken)
        {
            ReleaseOperationTextBinding();

            if (operationText == null)
            {
                return;
            }

            if (IsMissing(localizedString))
            {
                SetOperationText(fallbackValue);
                return;
            }

            boundOperationText = localizedString;
            boundOperationText.StringChanged += HandleOperationTextChanged;
            SetOperationText(await ResolveLocalizedStringAsync(localizedString, cancellationToken));
        }

        private void BindPhaseTitle(LocalizedString localizedString, string fallbackValue)
        {
            ReleasePhaseTitleBinding();

            if (phaseTitleText == null)
            {
                return;
            }

            if (IsMissing(localizedString))
            {
                SetPhaseTitle(fallbackValue);
                return;
            }

            boundPhaseTitle = localizedString;
            boundPhaseTitle.StringChanged += HandlePhaseTitleChanged;
            RefreshPhaseTitleAsync(localizedString, destroyCancellationToken).Forget();
        }

        private void HandleOperationTextChanged(string value)
        {
            SetOperationText(value);
        }

        private void HandlePhaseTitleChanged(string value)
        {
            SetPhaseTitle(value);
        }

        private void ReleaseBindings()
        {
            ReleaseOperationTextBinding();
            ReleasePhaseTitleBinding();
        }

        private void ReleaseOperationTextBinding()
        {
            if (boundOperationText == null)
            {
                return;
            }

            boundOperationText.StringChanged -= HandleOperationTextChanged;
            boundOperationText = null;
        }

        private void ReleasePhaseTitleBinding()
        {
            if (boundPhaseTitle == null)
            {
                return;
            }

            boundPhaseTitle.StringChanged -= HandlePhaseTitleChanged;
            boundPhaseTitle = null;
        }

        private async UniTask BuildCardsAsync(CancellationToken cancellationToken)
        {
            string resolvedFrontFaceLabel = await ResolveTextAsync(
                frontFaceLabel,
                frontFaceLabelFallback,
                cancellationToken);
            string resolvedBackFaceLabel = await ResolveTextAsync(
                backFaceLabel,
                backFaceLabelFallback,
                cancellationToken);

            await cardInteractionController.InitializeAsync(
                currentChapterDefinition?.OperationAsset,
                resolvedFrontFaceLabel,
                resolvedBackFaceLabel,
                ResolveTextAsync,
                cancellationToken);
        }

        private string ResolveJudgementIdOnComplete()
        {
            string successJudgementId = ResolveSuccessJudgementId(currentChapterDefinition);
            string defaultJudgementId = ResolveDefaultJudgementId(currentChapterDefinition);
            return cardInteractionController == null
                ? defaultJudgementId
                : cardInteractionController.ResolveJudgementId(successJudgementId, defaultJudgementId);
        }

        private ChapterOperationDefinition FindChapterDefinition(string chapterId)
        {
            if (string.IsNullOrWhiteSpace(chapterId) || chapterDefinitions == null)
            {
                return null;
            }

            for (int i = 0; i < chapterDefinitions.Length; i++)
            {
                ChapterOperationDefinition definition = chapterDefinitions[i];
                if (definition == null)
                {
                    continue;
                }

                if (string.Equals(definition.ChapterId, chapterId, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        private static string ResolveDefaultJudgementId(ChapterOperationDefinition definition)
        {
            if (definition != null &&
                !string.IsNullOrWhiteSpace(definition.DefaultJudgementIdOnComplete))
            {
                return definition.DefaultJudgementIdOnComplete;
            }

            return "incorrect";
        }

        private static string ResolveSuccessJudgementId(ChapterOperationDefinition definition)
        {
            if (definition != null &&
                !string.IsNullOrWhiteSpace(definition.SuccessJudgementId))
            {
                return definition.SuccessJudgementId;
            }

            return "success";
        }

        private void ValidateReferences()
        {
            WarnIfMissing(operationRoot, nameof(operationRoot));
            WarnIfMissing(phaseTitleText, nameof(phaseTitleText));
            WarnIfMissing(operationText, nameof(operationText));
            WarnIfMissing(cardAreaRoot, nameof(cardAreaRoot));
            WarnIfMissing(cardViewPrefab, nameof(cardViewPrefab));
            WarnIfMissing(cardDescriptionPopupRoot, nameof(cardDescriptionPopupRoot));
            WarnIfMissing(cardDescriptionText, nameof(cardDescriptionText));
            WarnIfMissing(completeOperationButton, nameof(completeOperationButton));
            WarnIfMissing(restartSequenceButton, nameof(restartSequenceButton));
        }

        private void BindListeners()
        {
            if (listenersBound)
            {
                return;
            }

            if (completeOperationButton != null)
            {
                completeOperationButton.onClick.RemoveListener(RequestComplete);
                completeOperationButton.onClick.AddListener(RequestComplete);
            }

            if (restartSequenceButton != null)
            {
                restartSequenceButton.onClick.RemoveListener(RequestRestart);
                restartSequenceButton.onClick.AddListener(RequestRestart);
            }

            listenersBound = true;
        }

        private void UnbindListeners()
        {
            if (!listenersBound)
            {
                return;
            }

            if (completeOperationButton != null)
            {
                completeOperationButton.onClick.RemoveListener(RequestComplete);
            }

            if (restartSequenceButton != null)
            {
                restartSequenceButton.onClick.RemoveListener(RequestRestart);
            }

            listenersBound = false;
        }

        private static void WarnIfMissing(UnityEngine.Object target, string fieldName)
        {
            if (target == null)
            {
                Debug.LogWarning(
                    $"OperationPartManager requires {fieldName} to be assigned via SerializeField.");
            }
        }

        private static bool IsMissing(LocalizedString localizedString)
        {
            return localizedString == null || localizedString.IsEmpty;
        }

        private static async UniTask<string> ResolveLocalizedStringAsync(
            LocalizedString localizedString,
            CancellationToken cancellationToken)
        {
            return await localizedString.GetLocalizedStringAsync().Task.AsUniTask()
                .AttachExternalCancellation(cancellationToken);
        }

        private async UniTask<string> ResolveTextAsync(
            LocalizedTextReference textReference,
            CancellationToken cancellationToken)
        {
            if (textReference == null || textReference.IsMissing)
            {
                return textReference?.Fallback ?? string.Empty;
            }

            return await ResolveLocalizedStringAsync(textReference.LocalizedString, cancellationToken);
        }

        private async UniTask<string> ResolveTextAsync(
            LocalizedString localizedString,
            string fallbackValue,
            CancellationToken cancellationToken)
        {
            if (IsMissing(localizedString))
            {
                return fallbackValue;
            }

            return await ResolveLocalizedStringAsync(localizedString, cancellationToken);
        }

        private async UniTask RefreshPhaseTitleAsync(
            LocalizedString localizedString,
            CancellationToken cancellationToken)
        {
            SetPhaseTitle(await ResolveLocalizedStringAsync(localizedString, cancellationToken));
        }
    }
}
