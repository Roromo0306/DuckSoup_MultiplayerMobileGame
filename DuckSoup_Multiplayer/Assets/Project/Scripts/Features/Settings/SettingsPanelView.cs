
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using DuckSoup.Features.Transitions;

namespace DuckSoup.Features.Settings
{
    public sealed class SettingsPanelView : MonoBehaviour
    {
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer audioMixer;

        [Header("Volume Sliders")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;

        [Header("Settings Panel")]
        [SerializeField] private Button settingsOpenButton;
        [SerializeField] private Button settingsCloseButton;
        [SerializeField] private GameObject settingsPanel;

        [Header("Credits Panel")]
        [SerializeField] private Button creditsOpenButton;
        [SerializeField] private Button creditsCloseButton;
        [SerializeField] private GameObject creditsPanel;

        [Header("Panel Transitions")]
        [SerializeField] private UIPanelTransition settingsTransition;
        [SerializeField] private UIPanelTransition creditsTransition;

        private AudioSettingsService service;
        private bool initialized;

        private void Awake()
        {
            if (audioMixer == null ||
                masterSlider == null ||
                musicSlider == null ||
                sfxSlider == null ||
                settingsOpenButton == null ||
                settingsCloseButton == null ||
                settingsPanel == null ||
                creditsOpenButton == null ||
                creditsCloseButton == null ||
                creditsPanel == null)
            {
                Debug.LogError(
                    "[Settings] Faltan referencias del Inspector.",
                    this);

                enabled = false;
                return;
            }

            // Compatible con el servicio que ya tienes.
            service = new AudioSettingsService(audioMixer);

            settingsOpenButton.onClick.AddListener(OpenSettings);
            settingsCloseButton.onClick.AddListener(CloseSettings);

            creditsOpenButton.onClick.AddListener(OpenCredits);
            creditsCloseButton.onClick.AddListener(CloseCredits);

            settingsPanel.SetActive(false);
            creditsPanel.SetActive(false);

            initialized = true;
        }

        private void Start()
        {
            if (!initialized)
                return;

            ConfigureSlider(masterSlider, service.Master);
            ConfigureSlider(musicSlider, service.Music);
            ConfigureSlider(sfxSlider, service.Sfx);

            masterSlider.onValueChanged.AddListener(
                OnMasterChanged);

            musicSlider.onValueChanged.AddListener(
                OnMusicChanged);

            sfxSlider.onValueChanged.AddListener(
                OnSfxChanged);

            service.Apply();
        }

        private static void ConfigureSlider(
            Slider slider,
            float value)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;

            slider.SetValueWithoutNotify(
                Mathf.Clamp01(value));
        }

        private void OnMasterChanged(float value)
        {
            service.SetMaster(value);
        }

        private void OnMusicChanged(float value)
        {
            service.SetMusic(value);
        }

        private void OnSfxChanged(float value)
        {
            service.SetSfx(value);
        }

        public void OpenSettings()
        {
            CloseCreditsPanel(ShowSettingsPanel);
        }

        public void CloseSettings()
        {
            CloseSettingsPanel(null);
        }

        public void OpenCredits()
        {
            CloseSettingsPanel(ShowCreditsPanel);
        }

        public void CloseCredits()
        {
            CloseCreditsPanel(ShowSettingsPanel);
        }

        private void ShowSettingsPanel()
        {
            if (settingsTransition != null)
                settingsTransition.Show();
            else
                settingsPanel.SetActive(true);
        }

        private void ShowCreditsPanel()
        {
            if (creditsTransition != null)
                creditsTransition.Show();
            else
                creditsPanel.SetActive(true);
        }

        private void CloseSettingsPanel(
            System.Action afterClose)
        {
            if (settingsTransition != null)
            {
                settingsTransition.Hide(afterClose);
            }
            else
            {
                settingsPanel.SetActive(false);
                afterClose?.Invoke();
            }
        }

        private void CloseCreditsPanel(
            System.Action afterClose)
        {
            if (creditsTransition != null)
            {
                creditsTransition.Hide(afterClose);
            }
            else
            {
                creditsPanel.SetActive(false);
                afterClose?.Invoke();
            }
        }

        private void OnDestroy()
        {
            if (!initialized)
                return;

            masterSlider.onValueChanged.RemoveListener(
                OnMasterChanged);

            musicSlider.onValueChanged.RemoveListener(
                OnMusicChanged);

            sfxSlider.onValueChanged.RemoveListener(
                OnSfxChanged);

            settingsOpenButton.onClick.RemoveListener(
                OpenSettings);

            settingsCloseButton.onClick.RemoveListener(
                CloseSettings);

            creditsOpenButton.onClick.RemoveListener(
                OpenCredits);

            creditsCloseButton.onClick.RemoveListener(
                CloseCredits);
        }
    }
}

