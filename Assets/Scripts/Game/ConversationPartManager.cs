using System;
using System.Text.RegularExpressions;
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
        [SerializeField] private ConversationFactCardOverlay factCardOverlay;
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
        [SerializeField, Min(0f)] private float conversationSkipAdvanceIntervalSeconds = 0.05f;
        [SerializeField, Min(0f)] private float advanceIndicatorFadeDuration = 0.5f;
        [SerializeField, Min(0f)] private float titleSpriteFadeOutDuration = 0.35f;

        private static readonly Regex NameTokenRegex =
            new(@"\{(?<key>[^{}\s]+)\}", RegexOptions.Compiled);

        private bool advanceRequested;
        private bool isAnimatingConversation;
        private bool listenersBound;
        private bool isMessageStepActive;
        private LocalizedString boundConversationText;
        private LocalizedString boundPhaseTitle;
        private Tween activeConversationTween;
        private Tween advanceIndicatorTween;
        private Tween titleSpriteFadeTween;
        private bool showAdvanceIndicatorWhenConversationCompletes;
        private int previousTypewriterTextLength;
        private float lastTypewriterSeTime = float.NegativeInfinity;
        private float nextConversationSkipRequestTime;
        private StoryNameTable activeNameTable;

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
                nextConversationSkipRequestTime = Time.unscaledTime;
                return;
            }

            if (keyboard.enterKey.wasPressedThisFrame)
            {
                RequestAdvance();
                return;
            }

            if (isMessageStepActive)
            {
                TryRequestConversationSkip(keyboard);
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
            isMessageStepActive = false;
            SetAdvanceIndicatorVisible(false);
            SetTitleSprite(null);
            SetConversationWindowVisible(true);
            SetRootState(false);
            SetButtonState(nextConversationButton, false);
            ReleaseBindings();
            SetConversationText(string.Empty);
            factCardOverlay?.ResetImmediate();
            activeNameTable = null;
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
                    storyAsset != null ? storyAsset.NameTable : null,
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

                await PlayStepAsync(step, storyAsset.NameTable, cancellationToken);
            }
        }

        private async UniTask PlayStepAsync(
            StoryStep step,
            StoryNameTable nameTable,
            CancellationToken cancellationToken)
        {
            isMessageStepActive = step.StepType == StoryStepType.ShowMessage;
            if (step.StepType != StoryStepType.ShowMessage)
            {
                showAdvanceIndicatorWhenConversationCompletes = false;
                SetAdvanceIndicatorVisible(false);
            }

            switch (step.StepType)
            {
                case StoryStepType.ShowMessage:
                    showAdvanceIndicatorWhenConversationCompletes = step.WaitForAdvance;
                    await SetConversationTextAsync(
                        step.Message,
                        step.MessageFallback,
                        nameTable,
                        cancellationToken);
                    if (step.WaitForAdvance)
                    {
                        await WaitForAdvanceAsync(cancellationToken);
                    }
                    else
                    {
                        SetAdvanceIndicatorVisible(false);
                        await WaitAsync(step.WaitSeconds, allowConversationSkip: true, cancellationToken);
                    }

                    break;

                case StoryStepType.ChangeExpression:
                    ApplyExpression(step);
                    break;

                case StoryStepType.Wait:
                    SetConversationWindowVisible(step.ShowConversationWindowDuringWait);
                    await WaitAsync(step.WaitSeconds, allowConversationSkip: true, cancellationToken);
                    SetConversationWindowVisible(true);
                    break;

                case StoryStepType.ShowTitleSprite:
                    SetConversationWindowVisible(false);
                    SetTitleSprite(step.TitleSprite);
                    await WaitAsync(step.WaitSeconds, allowConversationSkip: false, cancellationToken);
                    await HideTitleSpriteAsync(cancellationToken);
                    SetConversationWindowVisible(true);
                    break;

                case StoryStepType.ShowEndSprite:
                    SetConversationWindowVisible(false);
                    SetTitleSprite(step.EndSprite);
                    await WaitAsync(step.WaitSeconds, allowConversationSkip: false, cancellationToken);
                    await HideTitleSpriteAsync(cancellationToken);
                    SetConversationWindowVisible(true);
                    break;

                case StoryStepType.ShowFactCard:
                    if (factCardOverlay != null)
                    {
                        await factCardOverlay.ShowCardAsync(
                            step.FactCardChapterAsset,
                            step.FactCardId,
                            step.FactCardFaceMode,
                            cancellationToken);
                    }

                    break;

                case StoryStepType.StackFactCard:
                    if (factCardOverlay != null)
                    {
                        await factCardOverlay.StackCurrentCardAsync(cancellationToken);
                    }

                    break;

                case StoryStepType.ClearFactCards:
                    if (factCardOverlay != null)
                    {
                        await factCardOverlay.ClearCardsAsync(cancellationToken);
                    }

                    break;

                case StoryStepType.PlayBgm:
                    if (!AudioManager.IsBgmPlaying(step.AudioKey))
                    {
                        AudioManager.PlayBgm(step.AudioKey, step.AudioVolume, step.LoopBgm);
                    }

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

        private async UniTask WaitAsync(
            float seconds,
            bool allowConversationSkip,
            CancellationToken cancellationToken)
        {
            if (seconds <= 0f)
            {
                return;
            }

            float endTime = Time.unscaledTime + seconds;
            while (Time.unscaledTime < endTime)
            {
                if (allowConversationSkip && IsConversationSkipHeld())
                {
                    return;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
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

        private void TryRequestConversationSkip(Keyboard keyboard)
        {
            if (!IsConversationSkipHeld(keyboard))
            {
                nextConversationSkipRequestTime = Time.unscaledTime;
                return;
            }

            float currentTime = Time.unscaledTime;
            if (currentTime < nextConversationSkipRequestTime)
            {
                return;
            }

            RequestAdvance();
            nextConversationSkipRequestTime =
                currentTime + conversationSkipAdvanceIntervalSeconds;
        }

        private bool IsConversationSkipHeld()
        {
            return IsConversationSkipHeld(Keyboard.current);
        }

        private static bool IsConversationSkipHeld(Keyboard keyboard)
        {
            return keyboard != null && keyboard.spaceKey.isPressed;
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
            StoryNameTable nameTable,
            CancellationToken cancellationToken)
        {
            ReleaseConversationBinding();
            activeNameTable = nameTable;

            if (conversationText == null)
            {
                return;
            }

            if (IsMissing(localizedString))
            {
                string processedFallback = ResolveNameTokens(fallbackValue, nameTable);
                AppendConversationLogEntry(processedFallback);
                await AnimateConversationTextAsync(processedFallback, cancellationToken);
                return;
            }

            boundConversationText = localizedString;
            boundConversationText.StringChanged += HandleConversationTextChanged;
            string resolvedValue =
                await ResolveLocalizedStringAsync(localizedString, cancellationToken);
            string processedValue = ResolveNameTokens(resolvedValue, nameTable);
            AppendConversationLogEntry(processedValue);
            await AnimateConversationTextAsync(processedValue, cancellationToken);
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
            SetConversationText(ResolveNameTokens(value, activeNameTable));
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

            int visibleCharacterCount = CountVisibleCharacters(value);
            if (visibleCharacterCount <= 0 || conversationCharactersPerSecond <= 0f)
            {
                SetConversationText(value);
                SetAdvanceIndicatorVisible(showAdvanceIndicatorWhenConversationCompletes);
                return;
            }

            isAnimatingConversation = true;
            float duration = visibleCharacterCount / conversationCharactersPerSecond;
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
            int currentLength = CountVisibleCharacters(currentText);
            if (currentLength <= previousTypewriterTextLength)
            {
                return;
            }

            bool hasNewAudibleCharacter =
                HasNewAudibleVisibleCharacter(currentText, previousTypewriterTextLength);

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
            WarnIfMissing(factCardOverlay, nameof(factCardOverlay));
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

        private static string ResolveNameTokens(string value, StoryNameTable nameTable)
        {
            if (string.IsNullOrEmpty(value) || nameTable == null)
            {
                return value ?? string.Empty;
            }

            return NameTokenRegex.Replace(
                value,
                match =>
                {
                    string key = match.Groups["key"].Value;
                    if (!nameTable.TryGetEntry(key, out StoryNameEntry entry))
                    {
                        return match.Value;
                    }

                    string displayName = entry.DisplayName;
                    if (string.IsNullOrEmpty(displayName))
                    {
                        return match.Value;
                    }

                    string colorCode = ColorUtility.ToHtmlStringRGBA(entry.NameColor);
                    return $"<color=#{colorCode}>{displayName}</color>";
                });
        }

        private static int CountVisibleCharacters(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            int count = 0;
            bool insideTag = false;
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (current == '<')
                {
                    insideTag = true;
                    continue;
                }

                if (insideTag)
                {
                    if (current == '>')
                    {
                        insideTag = false;
                    }

                    continue;
                }

                count++;
            }

            return count;
        }

        private static bool HasNewAudibleVisibleCharacter(string value, int previousVisibleCharacterCount)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            int visibleCharacterIndex = 0;
            bool insideTag = false;
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (current == '<')
                {
                    insideTag = true;
                    continue;
                }

                if (insideTag)
                {
                    if (current == '>')
                    {
                        insideTag = false;
                    }

                    continue;
                }

                if (visibleCharacterIndex >= previousVisibleCharacterCount &&
                    !char.IsWhiteSpace(current))
                {
                    return true;
                }

                visibleCharacterIndex++;
            }

            return false;
        }
    }
}
