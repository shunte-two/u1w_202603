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
    public sealed class ConversationPartManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject conversationRoot;
        [SerializeField] private TextMeshProUGUI phaseTitleText;
        [SerializeField] private TextMeshProUGUI conversationText;
        [SerializeField] private Button nextConversationButton;
        [SerializeField] private TextMeshProUGUI advanceIndicatorText;
        [SerializeField] private StoryCharacterPortrait[] characterPortraits;
        [SerializeField] private ConversationLogPanel conversationLogPanel;

        [Header("Display")]
        [SerializeField] private LocalizedString phaseTitle;
        [SerializeField] private string phaseTitleFallback = "会話パート";
        [SerializeField] private LocalizedString emptyConversationMessage;
        [SerializeField] private string emptyConversationMessageFallback = "会話テキストが未設定です。";
        [SerializeField, Min(0f)] private float conversationCharactersPerSecond = 30f;
        [SerializeField, Min(0f)] private float advanceIndicatorFadeDuration = 0.5f;

        private bool advanceRequested;
        private bool isAnimatingConversation;
        private bool listenersBound;
        private LocalizedString boundConversationText;
        private LocalizedString boundPhaseTitle;
        private Tween activeConversationTween;
        private Tween advanceIndicatorTween;

        private void Awake()
        {
            ValidateReferences();
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
            if (keyboard == null || !IsAwaitingAdvance())
            {
                return;
            }

            if (keyboard.enterKey.wasPressedThisFrame)
            {
                RequestAdvance();
            }
        }

        private void OnDestroy()
        {
            KillConversationTween(false);
            KillAdvanceIndicatorTween();
            ReleaseBindings();
            UnbindListeners();
        }

        public UniTask PlayAsync(StoryAsset storyAsset, CancellationToken cancellationToken)
        {
            return PlayStoryAsync(storyAsset, cancellationToken);
        }

        public void Hide()
        {
            KillConversationTween(false);
            isAnimatingConversation = false;
            SetAdvanceIndicatorVisible(false);
            SetRootState(false);
            SetButtonState(nextConversationButton, false);
            ReleaseBindings();
            SetConversationText(string.Empty);
        }

        private async UniTask PlayStoryAsync(
            StoryAsset storyAsset,
            CancellationToken cancellationToken)
        {
            ShowConversationView();

            StoryStep[] steps = storyAsset != null ? storyAsset.Steps : null;
            if (steps == null || steps.Length == 0)
            {
                await SetConversationTextAsync(
                    emptyConversationMessage,
                    emptyConversationMessageFallback,
                    cancellationToken);
                await WaitForAdvanceAsync(cancellationToken);
                return;
            }

            for (int i = 0; i < steps.Length; i++)
            {
                StoryStep step = steps[i];
                if (step == null)
                {
                    continue;
                }

                await PlayStepAsync(step, cancellationToken);
            }
        }

        private async UniTask PlayStepAsync(StoryStep step, CancellationToken cancellationToken)
        {
            switch (step.StepType)
            {
                case StoryStepType.ShowMessage:
                    await SetConversationTextAsync(step.Message, step.MessageFallback, cancellationToken);
                    if (step.WaitForAdvance)
                    {
                        await WaitForAdvanceAsync(cancellationToken);
                    }

                    break;

                case StoryStepType.ChangeExpression:
                    ApplyExpression(step);
                    break;

                case StoryStepType.Wait:
                    await WaitAsync(step.WaitSeconds, cancellationToken);
                    break;
            }
        }

        private async UniTask WaitForAdvanceAsync(CancellationToken cancellationToken)
        {
            advanceRequested = false;
            await UniTask.WaitUntil(() => advanceRequested, cancellationToken: cancellationToken);
        }

        private static async UniTask WaitAsync(float seconds, CancellationToken cancellationToken)
        {
            if (seconds <= 0f)
            {
                return;
            }

            int milliseconds = Mathf.CeilToInt(seconds * 1000f);
            await UniTask.Delay(milliseconds, cancellationToken: cancellationToken);
        }

        private void ShowConversationView()
        {
            SetRootState(true);
            SetButtonState(nextConversationButton, true);
            SetAdvanceIndicatorVisible(false);
            BindPhaseTitle(phaseTitle, phaseTitleFallback);
        }

        private void ApplyExpression(StoryStep step)
        {
            StoryCharacterPortrait portrait = FindCharacterPortrait(step.CharacterId);
            if (portrait == null)
            {
                if (step.CharacterId != StoryCharacterId.None)
                {
                    Debug.LogWarning(
                        $"ConversationPartManager could not resolve character portrait target: {step.CharacterId}");
                }

                return;
            }

            bool applied = portrait.ApplyExpression(
                step.ExpressionId,
                step.HideCharacterWhenExpressionMissing);
            if (!applied)
            {
                Debug.LogWarning(
                    $"ConversationPartManager could not resolve expression '{step.ExpressionId}' for character '{step.CharacterId}'.",
                    this);
            }
        }

        private StoryCharacterPortrait FindCharacterPortrait(StoryCharacterId characterId)
        {
            if (characterId == StoryCharacterId.None || characterPortraits == null)
            {
                return null;
            }

            for (int i = 0; i < characterPortraits.Length; i++)
            {
                StoryCharacterPortrait portrait = characterPortraits[i];
                if (portrait == null)
                {
                    continue;
                }

                if (portrait.CharacterId == characterId)
                {
                    return portrait;
                }
            }

            return null;
        }

        private bool IsAwaitingAdvance()
        {
            return conversationRoot != null &&
                   conversationRoot.activeInHierarchy &&
                   nextConversationButton != null &&
                   nextConversationButton.gameObject.activeInHierarchy;
        }

        private void RequestAdvance()
        {
            if (isAnimatingConversation)
            {
                CompleteConversationAnimation();
                return;
            }

            advanceRequested = true;
        }

        private void SetRootState(bool isActive)
        {
            if (conversationRoot != null && conversationRoot.activeSelf != isActive)
            {
                conversationRoot.SetActive(isActive);
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

        private void SetConversationText(string value)
        {
            if (conversationText != null)
            {
                conversationText.text = value;
            }
        }

        private void SetPhaseTitle(string value)
        {
            if (phaseTitleText != null)
            {
                phaseTitleText.text = value;
            }
        }

        private async UniTask SetConversationTextAsync(
            LocalizedString localizedString,
            string fallbackValue,
            CancellationToken cancellationToken)
        {
            ReleaseConversationBinding();

            if (conversationText == null)
            {
                return;
            }

            if (IsMissing(localizedString))
            {
                AppendConversationLogEntry(fallbackValue);
                await AnimateConversationTextAsync(fallbackValue, cancellationToken);
                return;
            }

            boundConversationText = localizedString;
            boundConversationText.StringChanged += HandleConversationTextChanged;
            string resolvedValue =
                await ResolveLocalizedStringAsync(localizedString, cancellationToken);
            AppendConversationLogEntry(resolvedValue);
            await AnimateConversationTextAsync(resolvedValue, cancellationToken);
        }

        public void ResetConversationLog()
        {
            conversationLogPanel?.ClearMessages();
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

        private void HandleConversationTextChanged(string value)
        {
            KillConversationTween(false);
            isAnimatingConversation = false;
            SetAdvanceIndicatorVisible(true);
            SetConversationText(value);
        }

        private void HandlePhaseTitleChanged(string value)
        {
            SetPhaseTitle(value);
        }

        private void ReleaseBindings()
        {
            ReleaseConversationBinding();
            ReleasePhaseTitleBinding();
        }

        private void ReleaseConversationBinding()
        {
            KillConversationTween(false);
            isAnimatingConversation = false;
            SetAdvanceIndicatorVisible(false);

            if (boundConversationText == null)
            {
                return;
            }

            boundConversationText.StringChanged -= HandleConversationTextChanged;
            boundConversationText = null;
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

        private async UniTask RefreshPhaseTitleAsync(
            LocalizedString localizedString,
            CancellationToken cancellationToken)
        {
            SetPhaseTitle(await ResolveLocalizedStringAsync(localizedString, cancellationToken));
        }

        private async UniTask AnimateConversationTextAsync(string value, CancellationToken cancellationToken)
        {
            KillConversationTween(false);
            isAnimatingConversation = false;
            SetAdvanceIndicatorVisible(false);

            if (conversationText == null)
            {
                return;
            }

            value ??= string.Empty;
            SetConversationText(string.Empty);

            if (string.IsNullOrEmpty(value) || conversationCharactersPerSecond <= 0f)
            {
                SetConversationText(value);
                SetAdvanceIndicatorVisible(true);
                return;
            }

            isAnimatingConversation = true;
            float duration = value.Length / conversationCharactersPerSecond;
            activeConversationTween = conversationText
                .DOText(value, duration, true, ScrambleMode.None, null)
                .SetEase(Ease.Linear)
                .OnKill(HandleConversationTweenFinished)
                .OnComplete(HandleConversationTweenFinished);

            try
            {
                await UniTask.WaitUntil(
                    () => !isAnimatingConversation,
                    cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                KillConversationTween(false);
                throw;
            }
        }

        private void CompleteConversationAnimation()
        {
            if (activeConversationTween == null || !activeConversationTween.IsActive())
            {
                isAnimatingConversation = false;
                SetAdvanceIndicatorVisible(true);
                return;
            }

            activeConversationTween.Complete();
        }

        private void KillConversationTween(bool complete)
        {
            if (activeConversationTween == null)
            {
                return;
            }

            Tween tween = activeConversationTween;
            activeConversationTween = null;
            if (tween.IsActive())
            {
                tween.Kill(complete);
            }
        }

        private void HandleConversationTweenFinished()
        {
            activeConversationTween = null;
            isAnimatingConversation = false;
            SetAdvanceIndicatorVisible(true);
        }

        private void SetAdvanceIndicatorVisible(bool isVisible)
        {
            if (advanceIndicatorText == null)
            {
                return;
            }

            KillAdvanceIndicatorTween();
            advanceIndicatorText.gameObject.SetActive(isVisible);
            if (!isVisible)
            {
                return;
            }

            advanceIndicatorText.alpha = 1f;
            if (advanceIndicatorFadeDuration <= 0f)
            {
                return;
            }

            advanceIndicatorTween = advanceIndicatorText
                .DOFade(0.2f, advanceIndicatorFadeDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void KillAdvanceIndicatorTween()
        {
            if (advanceIndicatorTween == null)
            {
                return;
            }

            Tween tween = advanceIndicatorTween;
            advanceIndicatorTween = null;
            if (tween.IsActive())
            {
                tween.Kill(false);
            }
        }

        private void ValidateReferences()
        {
            WarnIfMissing(conversationRoot, nameof(conversationRoot));
            WarnIfMissing(phaseTitleText, nameof(phaseTitleText));
            WarnIfMissing(conversationText, nameof(conversationText));
            WarnIfMissing(nextConversationButton, nameof(nextConversationButton));
            WarnIfMissing(advanceIndicatorText, nameof(advanceIndicatorText));
            WarnIfMissing(conversationLogPanel, nameof(conversationLogPanel));

            if (characterPortraits == null)
            {
                return;
            }

            for (int i = 0; i < characterPortraits.Length; i++)
            {
                StoryCharacterPortrait portrait = characterPortraits[i];
                if (portrait == null || portrait.CharacterId == StoryCharacterId.None)
                {
                    continue;
                }

                WarnIfMissing(portrait, $"{nameof(characterPortraits)}[{i}]");
            }
        }

        private void BindListeners()
        {
            if (listenersBound || nextConversationButton == null)
            {
                return;
            }

            nextConversationButton.onClick.RemoveListener(RequestAdvance);
            nextConversationButton.onClick.AddListener(RequestAdvance);
            listenersBound = true;
        }

        private void UnbindListeners()
        {
            if (!listenersBound || nextConversationButton == null)
            {
                return;
            }

            nextConversationButton.onClick.RemoveListener(RequestAdvance);
            listenersBound = false;
        }

        private static void WarnIfMissing(UnityEngine.Object target, string fieldName)
        {
            if (target == null)
            {
                Debug.LogWarning(
                    $"ConversationPartManager requires {fieldName} to be assigned via SerializeField.");
            }
        }

        private void AppendConversationLogEntry(string value)
        {
            conversationLogPanel?.AppendMessage(value);
        }
    }
}
