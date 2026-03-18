using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace U1W.Game
{
    public sealed class ConversationLogPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button toggleButton;
        [SerializeField] private TextMeshProUGUI toggleButtonLabelText;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI panelTitleText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private TextMeshProUGUI logText;
        [SerializeField] private TextMeshProUGUI emptyStateText;

        [Header("Display")]
        [SerializeField] private LocalizedString toggleButtonLabel;
        [SerializeField] private string toggleButtonLabelFallback = "ログ";
        [SerializeField] private LocalizedString panelTitle;
        [SerializeField] private string panelTitleFallback = "会話ログ";
        [SerializeField] private LocalizedString emptyStateMessage;
        [SerializeField] private string emptyStateMessageFallback = "まだ会話ログはありません。";
        [SerializeField] [Min(1)] private int maxMessages = 100;

        private readonly List<string> messages = new();
        private readonly StringBuilder logBuilder = new();
        private bool listenersBound;

        private void Awake()
        {
            ValidateReferences();
            BindListeners();
            ApplyStaticLabelsAsync(destroyCancellationToken).Forget();
            SetPanelVisible(false);
            RefreshView();
        }

        private void OnValidate()
        {
            ValidateReferences();
        }

        private void OnDestroy()
        {
            UnbindListeners();
        }

        public void AppendMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            messages.Add(message);
            while (messages.Count > maxMessages)
            {
                messages.RemoveAt(0);
            }

            RefreshView();
        }

        public void ClearMessages()
        {
            if (messages.Count == 0)
            {
                RefreshView();
                return;
            }

            messages.Clear();
            RefreshView();
        }

        private void TogglePanel()
        {
            SetPanelVisible(!IsPanelVisible());
        }

        private void ClosePanel()
        {
            SetPanelVisible(false);
        }

        private void SetPanelVisible(bool isVisible)
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = isVisible ? 1f : 0f;
                panelCanvasGroup.interactable = isVisible;
                panelCanvasGroup.blocksRaycasts = isVisible;
            }
            else if (panelRoot != null && panelRoot.activeSelf != isVisible)
            {
                panelRoot.SetActive(isVisible);
            }

            if (isVisible)
            {
                ScrollToLatest();
            }
        }

        private bool IsPanelVisible()
        {
            if (panelCanvasGroup != null)
            {
                return panelCanvasGroup.alpha > 0f &&
                       panelCanvasGroup.interactable &&
                       panelCanvasGroup.blocksRaycasts;
            }

            return panelRoot != null && panelRoot.activeSelf;
        }

        private void RefreshView()
        {
            bool hasMessages = messages.Count > 0;
            SetText(logText, BuildLogText());

            if (logText != null)
            {
                logText.gameObject.SetActive(hasMessages);
            }

            if (emptyStateText != null)
            {
                emptyStateText.gameObject.SetActive(!hasMessages);
            }

            if (panelRoot != null && panelRoot.activeInHierarchy)
            {
                ScrollToLatest();
            }
        }

        private void ScrollToLatest()
        {
            if (scrollRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        private void BindListeners()
        {
            if (listenersBound)
            {
                return;
            }

            if (toggleButton != null)
            {
                toggleButton.onClick.RemoveListener(TogglePanel);
                toggleButton.onClick.AddListener(TogglePanel);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ClosePanel);
                closeButton.onClick.AddListener(ClosePanel);
            }

            listenersBound = true;
        }

        private void UnbindListeners()
        {
            if (!listenersBound)
            {
                return;
            }

            if (toggleButton != null)
            {
                toggleButton.onClick.RemoveListener(TogglePanel);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ClosePanel);
            }

            listenersBound = false;
        }

        private async UniTaskVoid ApplyStaticLabelsAsync(System.Threading.CancellationToken cancellationToken)
        {
            SetText(toggleButtonLabelText,
                await ResolveTextAsync(toggleButtonLabel, toggleButtonLabelFallback, cancellationToken));
            SetText(panelTitleText,
                await ResolveTextAsync(panelTitle, panelTitleFallback, cancellationToken));
            SetText(emptyStateText,
                await ResolveTextAsync(emptyStateMessage, emptyStateMessageFallback, cancellationToken));
        }

        private static async UniTask<string> ResolveTextAsync(
            LocalizedString localizedString,
            string fallbackValue,
            System.Threading.CancellationToken cancellationToken)
        {
            if (localizedString == null || localizedString.IsEmpty)
            {
                return fallbackValue;
            }

            return await localizedString.GetLocalizedStringAsync().Task.AsUniTask()
                .AttachExternalCancellation(cancellationToken);
        }

        private static void SetText(TMP_Text textComponent, string value)
        {
            if (textComponent != null)
            {
                textComponent.text = value ?? string.Empty;
            }
        }

        private string BuildLogText()
        {
            logBuilder.Clear();
            for (int i = 0; i < messages.Count; i++)
            {
                if (i > 0)
                {
                    logBuilder.AppendLine();
                    logBuilder.AppendLine();
                }

                logBuilder.Append(messages[i]);
            }

            return logBuilder.ToString();
        }

        private void ValidateReferences()
        {
            WarnIfMissing(toggleButton, nameof(toggleButton));
            WarnIfMissing(toggleButtonLabelText, nameof(toggleButtonLabelText));
            WarnIfMissing(panelRoot, nameof(panelRoot));
            WarnIfMissing(panelCanvasGroup, nameof(panelCanvasGroup));
            WarnIfMissing(closeButton, nameof(closeButton));
            WarnIfMissing(panelTitleText, nameof(panelTitleText));
            WarnIfMissing(scrollRect, nameof(scrollRect));
            WarnIfMissing(logText, nameof(logText));
            WarnIfMissing(emptyStateText, nameof(emptyStateText));
        }

        private static void WarnIfMissing(Object target, string fieldName)
        {
            if (target == null)
            {
                Debug.LogWarning(
                    $"ConversationLogPanel requires {fieldName} to be assigned via SerializeField.");
            }
        }
    }
}
