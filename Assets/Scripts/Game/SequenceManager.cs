using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace U1W.Game
{
    public sealed class SequenceManager : MonoBehaviour
    {
        private enum SequencePhase
        {
            None,
            Conversation,
            Operation,
            Completed
        }

        [Header("Roots")]
        [SerializeField] private GameObject conversationRoot;
        [SerializeField] private GameObject operationRoot;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI phaseTitleText;
        [SerializeField] private TextMeshProUGUI conversationText;
        [SerializeField] private TextMeshProUGUI operationText;

        [Header("Buttons")]
        [SerializeField] private Button nextConversationButton;
        [SerializeField] private Button completeOperationButton;
        [SerializeField] private Button restartSequenceButton;

        [Header("Conversation")]
        [SerializeField] private string conversationPhaseTitle = "会話パート";
        [SerializeField] [TextArea(2, 4)] private string[] openingConversationLines =
        {
            "現場の資料はそろった。まずは警察の見立てを整理しよう。",
            "容疑者の行動は不審に見えるが、まだ断定は早い。",
            "次は現場の状況を操作して、矛盾がどこで生まれるかを確かめる。"
        };

        [Header("Operation")]
        [SerializeField] private string operationPhaseTitle = "操作パート";
        [SerializeField] [TextArea(2, 4)] private string operationInstruction =
            "仮の操作パートです。調査やカード操作の代わりに、ボタンか Enter キーで進めます。";

        [Header("Result")]
        [SerializeField] private string completedPhaseTitle = "進行完了";
        [SerializeField] [TextArea(2, 4)] private string[] resultConversationLines =
        {
            "表面的な推理だけでは、まだ筋が通らない。",
            "次はカードの解釈反転や時系列整理に繋がる操作を差し込める状態になった。"
        };

        private bool listenersBound;
        private bool conversationAdvanceRequested;
        private bool operationCompleteRequested;
        private int sequenceVersion;
        private SequencePhase currentPhase;

        private void Awake()
        {
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

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (currentPhase == SequencePhase.Conversation && keyboard.enterKey.wasPressedThisFrame)
            {
                RequestConversationAdvance();
            }

            if (currentPhase == SequencePhase.Operation && keyboard.enterKey.wasPressedThisFrame)
            {
                RequestOperationComplete();
            }
        }

        private void OnDestroy()
        {
            UnbindListeners();
        }

        public void RestartSequence()
        {
            sequenceVersion++;
            ResetSignals();
            RunSequenceAsync(sequenceVersion, destroyCancellationToken).Forget();
        }

        private async UniTaskVoid RunSequenceAsync(int version, CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                bool stillCurrent = await PlayConversationAsync(
                    version,
                    cancellationToken,
                    openingConversationLines);

                if (!stillCurrent)
                {
                    return;
                }

                stillCurrent = await PlayOperationAsync(version, cancellationToken);
                if (!stillCurrent)
                {
                    return;
                }

                stillCurrent = await PlayConversationAsync(
                    version,
                    cancellationToken,
                    resultConversationLines);

                if (!stillCurrent)
                {
                    return;
                }

                ShowCompletedView();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async UniTask<bool> PlayConversationAsync(
            int version,
            CancellationToken cancellationToken,
            string[] lines)
        {
            SetConversationView();

            if (lines == null || lines.Length == 0)
            {
                SetConversationText("会話テキストが未設定です。");
                return await WaitForConversationAdvanceAsync(version, cancellationToken);
            }

            for (int i = 0; i < lines.Length; i++)
            {
                SetConversationText(lines[i]);
                bool stillCurrent = await WaitForConversationAdvanceAsync(version, cancellationToken);
                if (!stillCurrent)
                {
                    return false;
                }
            }

            return true;
        }

        private async UniTask<bool> PlayOperationAsync(int version, CancellationToken cancellationToken)
        {
            SetOperationView();
            SetOperationText(operationInstruction);
            return await WaitForOperationCompleteAsync(version, cancellationToken);
        }

        private async UniTask<bool> WaitForConversationAdvanceAsync(int version, CancellationToken cancellationToken)
        {
            conversationAdvanceRequested = false;
            await UniTask.WaitUntil(
                () => conversationAdvanceRequested || version != sequenceVersion,
                cancellationToken: cancellationToken);

            return version == sequenceVersion;
        }

        private async UniTask<bool> WaitForOperationCompleteAsync(int version, CancellationToken cancellationToken)
        {
            operationCompleteRequested = false;
            await UniTask.WaitUntil(
                () => operationCompleteRequested || version != sequenceVersion,
                cancellationToken: cancellationToken);

            return version == sequenceVersion;
        }

        private void SetConversationView()
        {
            currentPhase = SequencePhase.Conversation;
            SetRootState(conversationRoot, true);
            SetRootState(operationRoot, false);
            SetButtonState(nextConversationButton, true);
            SetButtonState(completeOperationButton, false);
            SetButtonState(restartSequenceButton, false);
            SetPhaseTitle(conversationPhaseTitle);
        }

        private void SetOperationView()
        {
            currentPhase = SequencePhase.Operation;
            SetRootState(conversationRoot, false);
            SetRootState(operationRoot, true);
            SetButtonState(nextConversationButton, false);
            SetButtonState(completeOperationButton, true);
            SetButtonState(restartSequenceButton, false);
            SetPhaseTitle(operationPhaseTitle);
        }

        private void ShowCompletedView()
        {
            currentPhase = SequencePhase.Completed;
            SetRootState(conversationRoot, false);
            SetRootState(operationRoot, true);
            SetOperationText("仮シーケンス完了。Restart で会話パートから再確認できます。");
            SetButtonState(nextConversationButton, false);
            SetButtonState(completeOperationButton, false);
            SetButtonState(restartSequenceButton, true);
            SetPhaseTitle(completedPhaseTitle);
        }

        private void ApplyIdleView()
        {
            currentPhase = SequencePhase.None;
            SetRootState(conversationRoot, false);
            SetRootState(operationRoot, false);
            SetButtonState(nextConversationButton, false);
            SetButtonState(completeOperationButton, false);
            SetButtonState(restartSequenceButton, false);
            SetConversationText(string.Empty);
            SetOperationText(string.Empty);
            SetPhaseTitle("準備中");
        }

        private void RequestConversationAdvance()
        {
            conversationAdvanceRequested = true;
        }

        private void RequestOperationComplete()
        {
            operationCompleteRequested = true;
        }

        private void ResetSignals()
        {
            conversationAdvanceRequested = false;
            operationCompleteRequested = false;
        }

        private void SetRootState(GameObject target, bool isActive)
        {
            if (target != null && target.activeSelf != isActive)
            {
                target.SetActive(isActive);
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

        private void ValidateReferences()
        {
            WarnIfMissing(conversationRoot, nameof(conversationRoot));
            WarnIfMissing(operationRoot, nameof(operationRoot));
            WarnIfMissing(phaseTitleText, nameof(phaseTitleText));
            WarnIfMissing(conversationText, nameof(conversationText));
            WarnIfMissing(operationText, nameof(operationText));
            WarnIfMissing(nextConversationButton, nameof(nextConversationButton));
            WarnIfMissing(completeOperationButton, nameof(completeOperationButton));
            WarnIfMissing(restartSequenceButton, nameof(restartSequenceButton));
        }

        private void BindListeners()
        {
            if (listenersBound)
            {
                return;
            }

            BindButton(nextConversationButton, RequestConversationAdvance);
            BindButton(completeOperationButton, RequestOperationComplete);
            BindButton(restartSequenceButton, RestartSequence);
            listenersBound = true;
        }

        private void UnbindListeners()
        {
            if (!listenersBound)
            {
                return;
            }

            UnbindButton(nextConversationButton, RequestConversationAdvance);
            UnbindButton(completeOperationButton, RequestOperationComplete);
            UnbindButton(restartSequenceButton, RestartSequence);
            listenersBound = false;
        }

        private static void WarnIfMissing(UnityEngine.Object target, string fieldName)
        {
            if (target == null)
            {
                Debug.LogWarning($"SequenceManager requires {fieldName} to be assigned via SerializeField.");
            }
        }

        private static void BindButton(Button button, Action action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action.Invoke);
            button.onClick.AddListener(action.Invoke);
        }

        private static void UnbindButton(Button button, Action action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action.Invoke);
        }
    }
}
