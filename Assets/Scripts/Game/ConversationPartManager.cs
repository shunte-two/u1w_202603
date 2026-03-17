using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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

        [Header("Display")]
        [SerializeField] private string phaseTitle = "会話パート";
        [SerializeField] [TextArea(2, 4)] private string[] openingConversationLines =
        {
            "現場の資料はそろった。まずは警察の見立てを整理しよう。",
            "容疑者の行動は不審に見えるが、まだ断定は早い。",
            "次は現場の状況を操作して、矛盾がどこで生まれるかを確かめる。"
        };
        [SerializeField] [TextArea(2, 4)] private string[] resultConversationLines =
        {
            "表面的な推理だけでは、まだ筋が通らない。",
            "次はカードの解釈反転や時系列整理に繋がる操作を差し込める状態になった。"
        };

        private bool advanceRequested;
        private bool listenersBound;

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
            UnbindListeners();
        }

        public UniTask PlayOpeningAsync(CancellationToken cancellationToken)
        {
            return PlayLinesAsync(openingConversationLines, cancellationToken);
        }

        public UniTask PlayResultAsync(CancellationToken cancellationToken)
        {
            return PlayLinesAsync(resultConversationLines, cancellationToken);
        }

        public void Hide()
        {
            SetRootState(false);
            SetButtonState(nextConversationButton, false);
            SetConversationText(string.Empty);
        }

        private async UniTask PlayLinesAsync(string[] lines, CancellationToken cancellationToken)
        {
            ShowConversationView();

            if (lines == null || lines.Length == 0)
            {
                SetConversationText("会話テキストが未設定です。");
                await WaitForAdvanceAsync(cancellationToken);
                return;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                SetConversationText(lines[i]);
                await WaitForAdvanceAsync(cancellationToken);
            }
        }

        private async UniTask WaitForAdvanceAsync(CancellationToken cancellationToken)
        {
            advanceRequested = false;
            await UniTask.WaitUntil(() => advanceRequested, cancellationToken: cancellationToken);
        }

        private void ShowConversationView()
        {
            SetRootState(true);
            SetButtonState(nextConversationButton, true);
            SetPhaseTitle(phaseTitle);
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

        private void ValidateReferences()
        {
            WarnIfMissing(conversationRoot, nameof(conversationRoot));
            WarnIfMissing(phaseTitleText, nameof(phaseTitleText));
            WarnIfMissing(conversationText, nameof(conversationText));
            WarnIfMissing(nextConversationButton, nameof(nextConversationButton));
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
