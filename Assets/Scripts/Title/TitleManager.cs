using UnityEngine;
using UnityEngine.UI;
using U1W.SceneManagement;

namespace U1W.Title
{
    public sealed class TitleManager : MonoBehaviour
    {
        private const string Chapter1Id = "chapter1";
        private const string Chapter2Id = "chapter2";
        private const string Chapter3Id = "chapter3";

        [Header("Scene Names")]
        [SerializeField] private string gameSceneName = "Game";
        [SerializeField] [Min(0f)] private float gameSceneTransitionBlackoutDuration;


        [Header("UI")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private OptionUI optionUI;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button chapter1Button;
        [SerializeField] private Button chapter2Button;
        [SerializeField] private Button chapter3Button;
        [SerializeField] private Button settingsButton;
        [SerializeField] private bool hideUnavailableChapterButtons;

        private bool listenersBound;

        private void Awake()
        {
            ValidateReferences();
            ApplyButtonState();
            BindListeners();
        }

        private void OnValidate()
        {
            ValidateReferences();
            ApplyButtonState();
        }

        private void OnDestroy()
        {
            UnbindListeners();
        }

        public void StartNewGame()
        {
            StartChapter(gameSceneName, Chapter1Id, skipOpeningStory: false);
        }

        public void StartChapter1()
        {
            StartChapter(gameSceneName, Chapter1Id, skipOpeningStory: true);
        }
        
        public void StartChapter2()
        {
            StartChapter(gameSceneName, Chapter2Id, skipOpeningStory: true);
        }

        public void StartChapter3()
        {
            StartChapter(gameSceneName, Chapter3Id, skipOpeningStory: true);
        }

        private void ValidateReferences()
        {
            if (rootCanvas == null)
            {
                Debug.LogWarning("TitleManager requires Root Canvas to be assigned via SerializeField.", this);
            }

            if (optionUI == null)
            {
                Debug.LogWarning("TitleManager requires Option UI to be assigned via SerializeField.", this);
            }

            if (newGameButton == null)
            {
                Debug.LogWarning("TitleManager requires New Game Button to be assigned via SerializeField.", this);
            }

            if (settingsButton == null)
            {
                Debug.LogWarning("TitleManager requires Settings Button to be assigned via SerializeField.", this);
            }
        }

        private void ApplyButtonState()
        {
            ConfigureChapterButton(chapter1Button, gameSceneName);
            ConfigureChapterButton(chapter2Button, gameSceneName);
            ConfigureChapterButton(chapter3Button, gameSceneName);
        }

        private void ConfigureChapterButton(Button button, string sceneName)
        {
            if (button == null)
            {
                return;
            }

            bool available = !string.IsNullOrWhiteSpace(sceneName);
            button.interactable = available;

            if (hideUnavailableChapterButtons)
            {
                button.gameObject.SetActive(available);
            }
        }

        private void BindListeners()
        {
            if (listenersBound)
            {
                return;
            }

            BindButton(newGameButton, StartNewGame);
            BindButton(chapter1Button, StartChapter1);
            BindButton(chapter2Button, StartChapter2);
            BindButton(chapter3Button, StartChapter3);
            BindButton(settingsButton, ToggleOptions);
            listenersBound = true;
        }

        private void UnbindListeners()
        {
            if (!listenersBound)
            {
                return;
            }

            UnbindButton(newGameButton, StartNewGame);
            UnbindButton(chapter1Button, StartChapter1);
            UnbindButton(chapter2Button, StartChapter2);
            UnbindButton(chapter3Button, StartChapter3);
            UnbindButton(settingsButton, ToggleOptions);
            listenersBound = false;
        }

        private static bool CanLoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("TitleManager.LoadScene skipped: target scene name is empty.");
                return false;
            }

            return true;
        }

        private void StartChapter(string sceneName, string chapterId, bool skipOpeningStory)
        {
            if (!CanLoadScene(sceneName))
            {
                return;
            }

            GameSceneStartContext.SetRequestedChapter(chapterId, skipOpeningStory);
            SceneTransitionManager.LoadScene(
                sceneName,
                blackoutDuration: gameSceneTransitionBlackoutDuration);
        }

        private void ToggleOptions()
        {
            if (optionUI == null)
            {
                Debug.LogWarning("TitleManager.ToggleOptions skipped: OptionUI was not assigned.");
                return;
            }

            optionUI.ToggleOptions();
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
