using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using U1W.Audio;

namespace U1W.Title
{
    public sealed class OptionUI : MonoBehaviour
    {
        private const string LocalePreferenceKey = "u1w.localization.selectedLocale";
        private const string JapaneseLocaleCode = "ja";
        private const string EnglishLocaleCode = "en";

        [Header("References")]
        [SerializeField] private GameObject optionPanel;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI panelTitleText;
        [SerializeField] private Slider audioVolumeSlider;
        [SerializeField] private TextMeshProUGUI audioVolumeLabelText;
        [SerializeField] private TextMeshProUGUI audioVolumeValueText;
        [SerializeField] private TextMeshProUGUI languageLabelText;
        [SerializeField] private Button languageToggleButton;
        [SerializeField] private TextMeshProUGUI languageToggleButtonLabelText;
        [SerializeField] private TextMeshProUGUI creditsLabelText;
        [SerializeField] private TextMeshProUGUI creditsBodyText;

        [Header("Display")]
        [SerializeField] private LocalizedString panelTitle;
        [SerializeField] private string panelTitleFallback = "Options";
        [SerializeField] private LocalizedString audioVolumeLabel;
        [SerializeField] private string audioVolumeLabelFallback = "Audio Volume";
        [SerializeField] private LocalizedString languageLabel;
        [SerializeField] private string languageLabelFallback = "Language";
        [SerializeField] private LocalizedString japaneseButtonLabel;
        [SerializeField] private string japaneseButtonLabelFallback = "Japanese";
        [SerializeField] private LocalizedString englishButtonLabel;
        [SerializeField] private string englishButtonLabelFallback = "English";
        [SerializeField] private LocalizedString creditsLabel;
        [SerializeField] private string creditsLabelFallback = "Credits";
        [SerializeField] private LocalizedString creditsBody;
        [SerializeField] [TextArea(3, 8)] private string creditsBodyFallback =
            "Unity\nTextMesh Pro\nDOTween Pro\nUniTask";

        private bool listenersBound;
        private bool openingRequested;

        private void Awake()
        {
            ValidateReferences();
            BindListeners();
            ApplySavedAudioVolume();
            InitializeAsync(destroyCancellationToken).Forget();
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

        private void OnEnable()
        {
            RefreshLocalizedTextsAsync(destroyCancellationToken).Forget();
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
            WarnIfMissing(optionPanel, nameof(optionPanel));
            WarnIfMissing(closeButton, nameof(closeButton));
            WarnIfMissing(panelTitleText, nameof(panelTitleText));
            WarnIfMissing(audioVolumeSlider, nameof(audioVolumeSlider));
            WarnIfMissing(audioVolumeLabelText, nameof(audioVolumeLabelText));
            WarnIfMissing(audioVolumeValueText, nameof(audioVolumeValueText));
            WarnIfMissing(languageLabelText, nameof(languageLabelText));
            WarnIfMissing(languageToggleButton, nameof(languageToggleButton));
            WarnIfMissing(languageToggleButtonLabelText, nameof(languageToggleButtonLabelText));
            WarnIfMissing(creditsLabelText, nameof(creditsLabelText));
            WarnIfMissing(creditsBodyText, nameof(creditsBodyText));
        }

        private void BindListeners()
        {
            if (listenersBound)
            {
                return;
            }

            BindButton(closeButton, CloseOptions);
            BindSlider(audioVolumeSlider, HandleAudioVolumeChanged);
            BindButton(languageToggleButton, HandleLanguageToggleButtonClicked);
            listenersBound = true;
        }

        private void UnbindListeners()
        {
            if (!listenersBound)
            {
                return;
            }

            UnbindButton(closeButton, CloseOptions);
            UnbindSlider(audioVolumeSlider, HandleAudioVolumeChanged);
            UnbindButton(languageToggleButton, HandleLanguageToggleButtonClicked);
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

        private async UniTaskVoid InitializeAsync(System.Threading.CancellationToken cancellationToken)
        {
            await ApplySavedLocaleAsync(cancellationToken);
            await RefreshLocalizedTextsAsync(cancellationToken);
        }

        private void ApplySavedAudioVolume()
        {
            float volume = AudioSettingsStore.LoadMasterVolume();
            if (audioVolumeSlider != null)
            {
                audioVolumeSlider.SetValueWithoutNotify(volume);
            }

            ApplyAudioVolume(volume);
            UpdateAudioVolumeValue(volume);
        }

        private void HandleAudioVolumeChanged(float volume)
        {
            ApplyAudioVolume(volume);
            AudioSettingsStore.SaveMasterVolume(volume);
            UpdateAudioVolumeValue(volume);
        }

        private void HandleLanguageToggleButtonClicked()
        {
            string nextLocaleCode = string.Equals(
                GetCurrentLocaleCode(),
                JapaneseLocaleCode,
                StringComparison.OrdinalIgnoreCase)
                ? EnglishLocaleCode
                : JapaneseLocaleCode;
            ChangeLocaleAsync(nextLocaleCode, destroyCancellationToken).Forget();
        }

        private async UniTask ChangeLocaleAsync(
            string localeCode,
            System.Threading.CancellationToken cancellationToken)
        {
            if (string.Equals(GetCurrentLocaleCode(), localeCode, StringComparison.OrdinalIgnoreCase))
            {
                RefreshLocalizedTextsAsync(cancellationToken).Forget();
                return;
            }

            await SetSelectedLocaleAsync(localeCode, cancellationToken);
            SaveLocaleCode(localeCode);
            await RefreshLocalizedTextsAsync(cancellationToken);
        }

        private async UniTask ApplySavedLocaleAsync(System.Threading.CancellationToken cancellationToken)
        {
            string savedLocaleCode = PlayerPrefs.GetString(LocalePreferenceKey, string.Empty);
            if (string.IsNullOrWhiteSpace(savedLocaleCode))
            {
                return;
            }

            await SetSelectedLocaleAsync(savedLocaleCode, cancellationToken);
        }

        private async UniTask SetSelectedLocaleAsync(
            string localeCode,
            System.Threading.CancellationToken cancellationToken)
        {
            await LocalizationSettings.InitializationOperation.Task.AsUniTask()
                .AttachExternalCancellation(cancellationToken);

            var locale = FindLocale(localeCode);
            if (locale == null)
            {
                Debug.LogWarning($"OptionUI could not find locale '{localeCode}'.", this);
                return;
            }

            LocalizationSettings.SelectedLocale = locale;
        }

        private async UniTask RefreshLocalizedTextsAsync(System.Threading.CancellationToken cancellationToken)
        {
            SetText(panelTitleText,
                await ResolveTextAsync(panelTitle, panelTitleFallback, cancellationToken));
            SetText(audioVolumeLabelText,
                await ResolveTextAsync(audioVolumeLabel, audioVolumeLabelFallback, cancellationToken));
            SetText(languageLabelText,
                await ResolveTextAsync(languageLabel, languageLabelFallback, cancellationToken));
            SetText(languageToggleButtonLabelText,
                await ResolveTextAsync(
                    GetCurrentLanguageButtonLabel(),
                    GetCurrentLanguageButtonLabelFallback(),
                    cancellationToken));
            SetText(creditsLabelText,
                await ResolveTextAsync(creditsLabel, creditsLabelFallback, cancellationToken));
            SetText(creditsBodyText,
                await ResolveTextAsync(creditsBody, creditsBodyFallback, cancellationToken));
        }

        private void ApplyAudioVolume(float volume)
        {
            AudioManager.SetMasterVolume(volume);
        }

        private void UpdateAudioVolumeValue(float volume)
        {
            SetText(audioVolumeValueText, $"{Mathf.RoundToInt(Mathf.Clamp01(volume) * 100f)}%");
        }

        private LocalizedString GetCurrentLanguageButtonLabel()
        {
            string currentLocaleCode = GetCurrentLocaleCode();
            return string.Equals(currentLocaleCode, EnglishLocaleCode, StringComparison.OrdinalIgnoreCase)
                ? englishButtonLabel
                : japaneseButtonLabel;
        }

        private string GetCurrentLanguageButtonLabelFallback()
        {
            string currentLocaleCode = GetCurrentLocaleCode();
            return string.Equals(currentLocaleCode, EnglishLocaleCode, StringComparison.OrdinalIgnoreCase)
                ? englishButtonLabelFallback
                : japaneseButtonLabelFallback;
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

        private static void WarnIfMissing(UnityEngine.Object target, string fieldName)
        {
            if (target == null)
            {
                Debug.LogWarning($"OptionUI requires {fieldName} to be assigned via SerializeField.", null);
            }
        }

        private static void BindSlider(Slider slider, UnityEngine.Events.UnityAction<float> action)
        {
            if (slider == null)
            {
                return;
            }

            slider.onValueChanged.RemoveListener(action);
            slider.onValueChanged.AddListener(action);
        }

        private static void UnbindSlider(Slider slider, UnityEngine.Events.UnityAction<float> action)
        {
            if (slider == null)
            {
                return;
            }

            slider.onValueChanged.RemoveListener(action);
        }

        private static UnityEngine.Localization.Locale FindLocale(string localeCode)
        {
            if (string.IsNullOrWhiteSpace(localeCode))
            {
                return null;
            }

            foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
            {
                if (string.Equals(locale.Identifier.Code, localeCode, StringComparison.OrdinalIgnoreCase))
                {
                    return locale;
                }
            }

            return null;
        }

        private static void SaveLocaleCode(string localeCode)
        {
            PlayerPrefs.SetString(LocalePreferenceKey, localeCode);
            PlayerPrefs.Save();
        }

        private static string GetCurrentLocaleCode()
        {
            return LocalizationSettings.SelectedLocale != null
                ? LocalizationSettings.SelectedLocale.Identifier.Code
                : string.Empty;
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
