using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.UI;
using U1W.Audio;

namespace U1W.Game
{
    public sealed class ConversationPartManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject conversationRoot;
        [SerializeField] private GameObject conversationWindowRoot;
        [SerializeField] private TextMeshProUGUI phaseTitleText;
        [SerializeField] private TextMeshProUGUI conversationText;
        [SerializeField] private Button nextConversationButton;
        [SerializeField] private TextMeshProUGUI advanceIndicatorText;
        [SerializeField] private GameObject titleSpriteRoot;
        [SerializeField] private Image titleSpriteImage;
        [SerializeField] private StoryCharacterPortrait[] characterPortraits;
        [SerializeField] private ConversationLogPanel conversationLogPanel;

        [Header("Display")]
        [SerializeField] private LocalizedString phaseTitle;
        [SerializeField] private string phaseTitleFallback = "会話パート";
        [SerializeField] private LocalizedString emptyConversationMessage;
        [SerializeField] private string emptyConversationMessageFallback = "会話テキストが未設定です。";
        [SerializeField, Min(0f)] private float conversationCharactersPerSecond = 30f;
        [SerializeField] private string conversationTypewriterSeKey = "Conversation";
        [SerializeField, Range(0f, 1f)] private float conversationTypewriterSeVolume = 0.25f;
        [SerializeField, Min(0f)] private float conversationTypewriterSeIntervalSeconds = 0.045f;
        [SerializeField, Min(0f)] private float advanceIndicatorFadeDuration = 0.5f;
        [SerializeField, Min(0f)] private float titleSpriteFadeOutDuration = 0.35f;

        private bool advanceRequested;
        private bool isAnimatingConversation;
        private bool listenersBound;
        private LocalizedString boundConversationText;
        private LocalizedString boundPhaseTitle;
        private Tween activeConversationTween;
        private Tween advanceIndicatorTween;
        private Tween titleSpriteFadeTween;
        private bool showAdvanceIndicatorWhenConversationCompletes;
        private int previousTypewriterTextLength;
        private float lastTypewriterSeTime = float.NegativeInfinity;

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
            KillTitleSpriteFadeTween(false);
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
            SetTitleSprite(null);
            SetConversationWindowVisible(true);
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
            if (step.StepType != StoryStepType.ShowMessage)
            {
                showAdvanceIndicatorWhenConversationCompletes = false;
                SetAdvanceIndicatorVisible(false);
            }

            switch (step.StepType)
            {
                case StoryStepType.ShowMessage:
                    showAdvanceIndicatorWhenConversationCompletes = step.WaitForAdvance;
                    await SetConversationTextAsync(step.Message, step.MessageFallback, cancellationToken);
                    if (step.WaitForAdvance)
                    {
                        await WaitForAdvanceAsync(cancellationToken);
                    }
                    else
                    {
                        SetAdvanceIndicatorVisible(false);
                        await WaitAsync(step.WaitSeconds, cancellationToken);
                    }

                    break;

                case StoryStepType.ChangeExpression:
                    ApplyExpression(step);
                    break;

                case StoryStepType.Wait:
                    SetConversationWindowVisible(step.ShowConversationWindowDuringWait);
                    await WaitAsync(step.WaitSeconds, cancellationToken);
                    SetConversationWindowVisible(true);
                    break;

                case StoryStepType.ShowTitleSprite:
                    SetConversationWindowVisible(false);
                    SetTitleSprite(step.TitleSprite);
                    await WaitAsync(step.WaitSeconds, cancellationToken);
                    await HideTitleSpriteAsync(cancellationToken);
                    SetConversationWindowVisible(true);
                    break;

                case StoryStepType.PlayBgm:
                    AudioManager.PlayBgm(step.AudioKey, step.AudioVolume, step.LoopBgm);
                    break;

                case StoryStepType.StopBgm:
                    AudioManager.StopBgm(step.AudioFadeSeconds);
                    break;

                case StoryStepType.PlaySe:
                    AudioManager.PlaySe(step.AudioKey, step.AudioVolume);
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
            SetConversationWindowVisible(true);
            SetTitleSprite(null);
            BindPhaseTitle(phaseTitle, phaseTitleFallback);
        }

        private void SetConversationWindowVisible(bool isVisible)
        {
            if (conversationWindowRoot != null)
            {
                conversationWindowRoot.SetActive(isVisible);
            }
        }

        private void SetTitleSprite(Sprite sprite)
        {
            KillTitleSpriteFadeTween(false);

            if (titleSpriteImage != null)
            {
                titleSpriteImage.sprite = sprite;
                titleSpriteImage.color = new Color(
                    titleSpriteImage.color.r,
                    titleSpriteImage.color.g,
                    titleSpriteImage.color.b,
                    sprite != null ? 1f : 0f);
                titleSpriteImage.enabled = sprite != null;
            }

            if (titleSpriteRoot != null)
            {
                titleSpriteRoot.SetActive(sprite != null);
            }
        }

        private async UniTask HideTitleSpriteAsync(CancellationToken cancellationToken)
        {
            if (titleSpriteImage == null ||
                !titleSpriteImage.enabled ||
                titleSpriteImage.sprite == null ||
                titleSpriteFadeOutDuration <= 0f)
            {
                SetTitleSprite(null);
                return;
            }

            bool completed = false;
            titleSpriteFadeTween = titleSpriteImage
                .DOFade(0f, titleSpriteFadeOutDuration)
                .SetEase(Ease.OutSine)
                .OnComplete(() => completed = true)
                .OnKill(() =>
                {
                    titleSpriteFadeTween = null;
                    completed = true;
                });

            try
            {
                await UniTask.WaitUntil(() => completed, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                KillTitleSpriteFadeTween(false);
                throw;
            }

            SetTitleSprite(null);
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
            ResetConversationTypewriterSeState();
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
            ResetConversationTypewriterSeState();

            if (conversationText == null)
            {
                return;
            }

            value ??= string.Empty;
            SetConversationText(string.Empty);

            if (string.IsNullOrEmpty(value) || conversationCharactersPerSecond <= 0f)
            {
                SetConversationText(value);
                SetAdvanceIndicatorVisible(showAdvanceIndicatorWhenConversationCompletes);
                return;
            }

            isAnimatingConversation = true;
            float duration = value.Length / conversationCharactersPerSecond;
            activeConversationTween = conversationText
                .DOText(value, duration, true, ScrambleMode.None, null)
                .SetEase(Ease.Linear)
                .OnUpdate(HandleConversationTypewriterUpdated)
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
                SetAdvanceIndicatorVisible(showAdvanceIndicatorWhenConversationCompletes);
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
            ResetConversationTypewriterSeState();
            SetAdvanceIndicatorVisible(showAdvanceIndicatorWhenConversationCompletes);
        }

        private void HandleConversationTypewriterUpdated()
        {
            if (conversationText == null)
            {
                return;
            }

            string currentText = conversationText.text ?? string.Empty;
            int currentLength = currentText.Length;
            if (currentLength <= previousTypewriterTextLength)
            {
                return;
            }

            bool hasNewAudibleCharacter = false;
            for (int i = previousTypewriterTextLength; i < currentLength; i++)
            {
                if (!char.IsWhiteSpace(currentText[i]))
                {
                    hasNewAudibleCharacter = true;
                    break;
                }
            }

            previousTypewriterTextLength = currentLength;
            if (!hasNewAudibleCharacter || string.IsNullOrWhiteSpace(conversationTypewriterSeKey))
            {
                return;
            }

            float currentTime = Time.unscaledTime;
            if (currentTime - lastTypewriterSeTime < conversationTypewriterSeIntervalSeconds)
            {
                return;
            }

            lastTypewriterSeTime = currentTime;
            AudioManager.PlaySe(conversationTypewriterSeKey, conversationTypewriterSeVolume);
        }

        private void ResetConversationTypewriterSeState()
        {
            previousTypewriterTextLength = 0;
            lastTypewriterSeTime = float.NegativeInfinity;
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

        private void KillTitleSpriteFadeTween(bool complete)
        {
            if (titleSpriteFadeTween == null)
            {
                return;
            }

            Tween tween = titleSpriteFadeTween;
            titleSpriteFadeTween = null;
            if (tween.IsActive())
            {
                tween.Kill(complete);
            }
        }

        private void ValidateReferences()
        {
            WarnIfMissing(conversationRoot, nameof(conversationRoot));
            WarnIfMissing(conversationWindowRoot, nameof(conversationWindowRoot));
            WarnIfMissing(phaseTitleText, nameof(phaseTitleText));
            WarnIfMissing(conversationText, nameof(conversationText));
            WarnIfMissing(nextConversationButton, nameof(nextConversationButton));
            WarnIfMissing(advanceIndicatorText, nameof(advanceIndicatorText));
            WarnIfMissing(titleSpriteRoot, nameof(titleSpriteRoot));
            WarnIfMissing(titleSpriteImage, nameof(titleSpriteImage));
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
