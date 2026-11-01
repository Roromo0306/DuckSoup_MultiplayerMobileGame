using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace DuckSoup.Features.Settings
{
    // Botones y paneles del menu; no requiere ningun evento OnClick en Inspector.
    public sealed class SettingsPanelView : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [Header("Settings")]
        [SerializeField] private Button settingsOpenButton;
        [SerializeField] private Button settingsCloseButton;
        [SerializeField] private GameObject settingsPanel;
        [Header("Credits")]
        [SerializeField] private Button creditsOpenButton;
        [SerializeField] private Button creditsCloseButton;
        [SerializeField] private GameObject creditsPanel;

        private AudioSettingsService service;
        private bool initialized;

        private void Awake()
        {
            if (audioMixer == null || masterSlider == null || musicSlider == null ||
                sfxSlider == null || settingsOpenButton == null || settingsCloseButton == null ||
                settingsPanel == null || creditsOpenButton == null ||
                creditsCloseButton == null || creditsPanel == null)
            {
                Debug.LogError("[Settings] Faltan referencias del Inspector.", this);
                enabled = false;
                return;
            }
            service = new AudioSettingsService(audioMixer);
            ConfigureSlider(masterSlider, service.Master);
            ConfigureSlider(musicSlider, service.Music);
            ConfigureSlider(sfxSlider, service.Sfx);

            masterSlider.onValueChanged.AddListener(service.SetMaster);
            musicSlider.onValueChanged.AddListener(service.SetMusic);
            sfxSlider.onValueChanged.AddListener(service.SetSfx);
            settingsOpenButton.onClick.AddListener(OpenSettings);
            settingsCloseButton.onClick.AddListener(CloseSettings);
            creditsOpenButton.onClick.AddListener(OpenCredits);
            creditsCloseButton.onClick.AddListener(CloseCredits);
            creditsPanel.SetActive(false);
            settingsPanel.SetActive(false);
            initialized = true;
        }

        private static void ConfigureSlider(Slider slider, float value)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.SetValueWithoutNotify(value);
        }

        public void OpenSettings()
        {
            creditsPanel.SetActive(false);
            settingsPanel.SetActive(true);
        }

        public void CloseSettings()
        {
            creditsPanel.SetActive(false);
            settingsPanel.SetActive(false);
        }

        public void OpenCredits()
        {
            settingsPanel.SetActive(false);
            creditsPanel.SetActive(true);
        }

        public void CloseCredits()
        {
            creditsPanel.SetActive(false);
            settingsPanel.SetActive(true);
        }

        private void OnDestroy()
        {
            if (!initialized) return;
            masterSlider.onValueChanged.RemoveListener(service.SetMaster);
            musicSlider.onValueChanged.RemoveListener(service.SetMusic);
            sfxSlider.onValueChanged.RemoveListener(service.SetSfx);
            settingsOpenButton.onClick.RemoveListener(OpenSettings);
            settingsCloseButton.onClick.RemoveListener(CloseSettings);
            creditsOpenButton.onClick.RemoveListener(OpenCredits);
            creditsCloseButton.onClick.RemoveListener(CloseCredits);
        }
    }
}
