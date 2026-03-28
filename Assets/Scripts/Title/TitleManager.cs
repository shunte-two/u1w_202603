using UnityEngine;
using UnityEngine.UI;
using U1W.Audio;
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

        [Header("Audio")]
        [SerializeField] private string titleBgmKey = "Key";
        [SerializeField] [Range(0f, 1f)] private float titleBgmVolume = 1f;
        [SerializeField] private bool titleBgmLoop = true;

        [Header("UI")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private OptionUI optionUI;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button chapter1Button;
        [SerializeField] private Button chapter2Button;
        [SerializeField] private Button chapter3Button;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button developerLinkButton;
        [SerializeField] private string developerLinkUrl = string.Empty;
        [SerializeField] private bool hideUnavailableChapterButtons;

        private bool listenersBound;

        private void Awake()
        {
            ValidateReferences();
            ApplyButtonState();
            BindListeners();
        }

        private void Start()
        {
            PlayTitleBgmIfNeeded();
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

            if (developerLinkButton == null)
            {
                Debug.LogWarning("TitleManager requires Developer Link Button to be assigned via SerializeField.", this);
            }
        }

        private void ApplyButtonState()
        {
            ConfigureChapterButton(chapter1Button, gameSceneName, Chapter1Id);
            ConfigureChapterButton(chapter2Button, gameSceneName, Chapter2Id);
            ConfigureChapterButton(chapter3Button, gameSceneName, Chapter3Id);
            ConfigureButton(developerLinkButton, !string.IsNullOrWhiteSpace(developerLinkUrl));
        }

        private void ConfigureChapterButton(Button button, string sceneName, string chapterId)
        {
            if (button == null)
            {
                return;
            }

            bool visible =
                !string.IsNullOrWhiteSpace(sceneName) &&
                ChapterProgressStore.CanSelectFromTitle(chapterId);
            button.gameObject.SetActive(visible);
            button.interactable = visible;
        }

        private static void ConfigureButton(Button button, bool interactable)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = interactable;
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
            BindButton(developerLinkButton, OpenDeveloperLink);
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
            UnbindButton(developerLinkButton, OpenDeveloperLink);
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

        private void OpenDeveloperLink()
        {
            if (string.IsNullOrWhiteSpace(developerLinkUrl))
            {
                Debug.LogWarning("TitleManager.OpenDeveloperLink skipped: Developer Link URL was not assigned.", this);
                return;
            }

            Application.OpenURL(developerLinkUrl);
        }

        private void PlayTitleBgmIfNeeded()
        {
            if (string.IsNullOrWhiteSpace(titleBgmKey))
            {
                Debug.LogWarning("TitleManager requires Title BGM Key to be assigned via SerializeField.", this);
                return;
            }

            if (AudioManager.IsBgmPlaying(titleBgmKey))
            {
                return;
            }

            AudioManager.PlayBgm(titleBgmKey, titleBgmVolume, titleBgmLoop);
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
