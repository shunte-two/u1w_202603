using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace U1W.Title
{
    public sealed class OptionUI : MonoBehaviour
    {
        [SerializeField] private GameObject optionPanel;
        [SerializeField] private Button closeButton;

        private bool listenersBound;
        private bool openingRequested;

        private void Awake()
        {
            ValidateReferences();
            BindListeners();
            if (!openingRequested)
            {
                CloseOptionsImmediate();
            }
        }

        private void OnValidate()
        {
            ValidateReferences();
        }

        private void OnDestroy()
        {
            UnbindListeners();
        }

        public void OpenOptions()
        {
            if (optionPanel == null)
            {
                Debug.LogWarning("OptionUI.OpenOptions skipped: OptionPanel was not assigned.", this);
                return;
            }

            openingRequested = true;
            optionPanel.SetActive(true);
            openingRequested = false;
        }

        public void CloseOptions()
        {
            if (optionPanel == null)
            {
                return;
            }

            optionPanel.SetActive(false);
        }

        public void ToggleOptions()
        {
            if (IsOptionsOpen())
            {
                CloseOptions();
                return;
            }

            OpenOptions();
        }

        private void ValidateReferences()
        {
            if (optionPanel == null)
            {
                Debug.LogWarning("OptionUI requires Option Panel to be assigned via SerializeField.", this);
            }

            if (closeButton == null)
            {
                Debug.LogWarning("OptionUI requires Close Button to be assigned via SerializeField.", this);
            }
        }

        private void BindListeners()
        {
            if (listenersBound)
            {
                return;
            }

            BindButton(closeButton, CloseOptions);
            listenersBound = true;
        }

        private void UnbindListeners()
        {
            if (!listenersBound)
            {
                return;
            }

            UnbindButton(closeButton, CloseOptions);
            listenersBound = false;
        }

        private bool IsOptionsOpen()
        {
            return optionPanel != null && optionPanel.activeSelf;
        }

        private void CloseOptionsImmediate()
        {
            if (optionPanel != null)
            {
                optionPanel.SetActive(false);
            }
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static void UnbindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
        }
    }
}
