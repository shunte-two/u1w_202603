using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace U1W.Game
{
    public sealed class OperationPartManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject operationRoot;
        [SerializeField] private TextMeshProUGUI phaseTitleText;
        [SerializeField] private TextMeshProUGUI operationText;
        [SerializeField] private Button completeOperationButton;
        [SerializeField] private Button restartSequenceButton;

        [Header("Display")]
        [SerializeField] private string operationPhaseTitle = "操作パート";
        [SerializeField] [TextArea(2, 4)] private string operationInstruction =
            "仮の操作パートです。調査やカード操作の代わりに、ボタンか Enter キーで進めます。";
        [SerializeField] private string completedPhaseTitle = "進行完了";
        [SerializeField] [TextArea(2, 4)] private string completedMessage =
            "仮シーケンス完了。Restart で会話パートから再確認できます。";

        private bool completeRequested;
        private bool listenersBound;

        public event Action RestartRequested;

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
            UnbindListeners();
        }

        public async UniTask PlayAsync(CancellationToken cancellationToken)
        {
            ShowOperationView();
            completeRequested = false;
            await UniTask.WaitUntil(() => completeRequested, cancellationToken: cancellationToken);
        }

        public void ShowCompleted()
        {
            SetRootState(true);
            SetButtonState(completeOperationButton, false);
            SetButtonState(restartSequenceButton, true);
            SetOperationText(completedMessage);
            SetPhaseTitle(completedPhaseTitle);
        }

        public void Hide()
        {
            SetRootState(false);
            SetButtonState(completeOperationButton, false);
            SetButtonState(restartSequenceButton, false);
            SetOperationText(string.Empty);
        }

        private void ShowOperationView()
        {
            SetRootState(true);
            SetButtonState(completeOperationButton, true);
            SetButtonState(restartSequenceButton, false);
            SetOperationText(operationInstruction);
            SetPhaseTitle(operationPhaseTitle);
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

        private void ValidateReferences()
        {
            WarnIfMissing(operationRoot, nameof(operationRoot));
            WarnIfMissing(phaseTitleText, nameof(phaseTitleText));
            WarnIfMissing(operationText, nameof(operationText));
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
    }
}
