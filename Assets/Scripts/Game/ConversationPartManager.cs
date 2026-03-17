using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace U1W.Game
{
    public sealed class ConversationPartManager : MonoBehaviour
    {
        [System.Serializable]
        private sealed class CharacterExpressionBinding
        {
            [SerializeField] private string characterId;
            [SerializeField] private Image targetImage;
            [SerializeField] private bool hideWhenSpriteMissing = true;

            public string CharacterId => characterId;
            public Image TargetImage => targetImage;
            public bool HideWhenSpriteMissing => hideWhenSpriteMissing;
        }

        [Header("References")]
        [SerializeField] private GameObject conversationRoot;
        [SerializeField] private TextMeshProUGUI phaseTitleText;
        [SerializeField] private TextMeshProUGUI conversationText;
        [SerializeField] private Button nextConversationButton;
        [SerializeField] private CharacterExpressionBinding[] characterExpressionBindings;

        [Header("Display")]
        [SerializeField] private LocalizedString phaseTitle;
        [SerializeField] private string phaseTitleFallback = "会話パート";
        [SerializeField] private LocalizedString emptyConversationMessage;
        [SerializeField] private string emptyConversationMessageFallback = "会話テキストが未設定です。";

        private bool advanceRequested;
        private bool listenersBound;
        private LocalizedString boundConversationText;
        private LocalizedString boundPhaseTitle;

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
            ReleaseBindings();
            UnbindListeners();
        }

        public UniTask PlayAsync(StoryAsset storyAsset, CancellationToken cancellationToken)
        {
            return PlayStoryAsync(storyAsset, cancellationToken);
        }

        public void Hide()
        {
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
            BindPhaseTitle(phaseTitle, phaseTitleFallback);
        }

        private void ApplyExpression(StoryStep step)
        {
            CharacterExpressionBinding binding = FindCharacterBinding(step.CharacterId);
            if (binding == null || binding.TargetImage == null)
            {
                if (!string.IsNullOrWhiteSpace(step.CharacterId))
                {
                    Debug.LogWarning(
                        $"ConversationPartManager could not resolve character expression target: {step.CharacterId}");
                }

                return;
            }

            Image targetImage = binding.TargetImage;
            targetImage.sprite = step.ExpressionSprite;

            bool hasSprite = step.ExpressionSprite != null;
            bool hideWhenMissing = step.HideWhenSpriteMissing || binding.HideWhenSpriteMissing;
            if (hideWhenMissing)
            {
                targetImage.enabled = hasSprite;
            }

            if (hasSprite && step.SetNativeSize)
            {
                targetImage.SetNativeSize();
            }
        }

        private CharacterExpressionBinding FindCharacterBinding(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || characterExpressionBindings == null)
            {
                return null;
            }

            for (int i = 0; i < characterExpressionBindings.Length; i++)
            {
                CharacterExpressionBinding binding = characterExpressionBindings[i];
                if (binding == null)
                {
                    continue;
                }

                if (string.Equals(binding.CharacterId, characterId, System.StringComparison.Ordinal))
                {
                    return binding;
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
                SetConversationText(fallbackValue);
                return;
            }

            boundConversationText = localizedString;
            boundConversationText.StringChanged += HandleConversationTextChanged;
            SetConversationText(await ResolveLocalizedStringAsync(localizedString, cancellationToken));
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

        private void ValidateReferences()
        {
            WarnIfMissing(conversationRoot, nameof(conversationRoot));
            WarnIfMissing(phaseTitleText, nameof(phaseTitleText));
            WarnIfMissing(conversationText, nameof(conversationText));
            WarnIfMissing(nextConversationButton, nameof(nextConversationButton));

            if (characterExpressionBindings == null)
            {
                return;
            }

            for (int i = 0; i < characterExpressionBindings.Length; i++)
            {
                CharacterExpressionBinding binding = characterExpressionBindings[i];
                if (binding == null || string.IsNullOrWhiteSpace(binding.CharacterId))
                {
                    continue;
                }

                WarnIfMissing(binding.TargetImage, $"{nameof(characterExpressionBindings)}[{i}]");
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
    }
}
