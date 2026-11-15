using System;
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

        [Header("Single-player pause only")]
        [SerializeField] private Button returnToMenuButton;
        [SerializeField] private GameObject returnConfirmationPanel;
        [SerializeField] private Button confirmReturnYesButton;
        [SerializeField] private Button confirmReturnNoButton;
        [SerializeField] private UIPanelTransition returnConfirmationTransition;

        private enum PanelState { Closed, Settings, Credits, ConfirmReturn, Transitioning, Leaving }

        private AudioSettingsService service;
        private PanelState state;
        private Func<bool> tryPause;
        private Action resumeAfterClose;
        private Func<bool> tryReturnToMenu;
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

            service = new AudioSettingsService(audioMixer);

            if (settingsTransition == null)
                settingsTransition = settingsPanel.GetComponent<UIPanelTransition>();
            if (creditsTransition == null)
                creditsTransition = creditsPanel.GetComponent<UIPanelTransition>();
            if (returnConfirmationTransition == null && returnConfirmationPanel != null)
                returnConfirmationTransition = returnConfirmationPanel.GetComponent<UIPanelTransition>();

            settingsOpenButton.onClick.AddListener(OpenSettings);
            settingsCloseButton.onClick.AddListener(CloseSettings);

            creditsOpenButton.onClick.AddListener(OpenCredits);
            creditsCloseButton.onClick.AddListener(CloseCredits);

            settingsPanel.SetActive(false);
            creditsPanel.SetActive(false);
            if (returnConfirmationPanel != null)
                returnConfirmationPanel.SetActive(false);

            if (returnToMenuButton != null)
            {
                returnToMenuButton.onClick.AddListener(OpenReturnConfirmation);
                returnToMenuButton.gameObject.SetActive(false);
            }
            if (confirmReturnYesButton != null)
                confirmReturnYesButton.onClick.AddListener(ConfirmReturnToMenu);
            if (confirmReturnNoButton != null)
                confirmReturnNoButton.onClick.AddListener(CancelReturnToMenu);

            initialized = true;
        }

        private void Start()
        {
            if (!initialized)
                return;

            ConfigureSlider(masterSlider, service.Master);
            ConfigureSlider(musicSlider, service.Music);
            ConfigureSlider(sfxSlider, service.Sfx);

            masterSlider.onValueChanged.AddListener(OnMasterChanged);
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);

            service.Apply();
        }

        public void BindPause(Func<bool> pause, Action resume, Func<bool> returnToMenu)
        {
            SetOpenButtonAvailable(false);
            if (!initialized) return;

            tryPause = pause;
            resumeAfterClose = resume;
            tryReturnToMenu = returnToMenu;

            if (returnToMenuButton == null)
            {
                Debug.LogError("[Settings] Asigna Return To Menu Button en la escena 1P.", this);
                return;
            }

            bool confirmationReady = returnConfirmationPanel != null &&
                confirmReturnYesButton != null && confirmReturnNoButton != null;
            returnToMenuButton.gameObject.SetActive(true);
            returnToMenuButton.interactable = confirmationReady;
            if (!confirmationReady)
                Debug.LogError("[Settings] Asigna el panel de confirmacion y sus botones Yes y No.", this);
        }

        public void SetOpenButtonAvailable(bool available)
        {
            if (settingsOpenButton != null)
                settingsOpenButton.interactable = available && isActiveAndEnabled;
        }

        public void ResetPausePanel()
        {
            SetOpenButtonAvailable(false);
            state = PanelState.Closed;
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (returnConfirmationPanel != null) returnConfirmationPanel.SetActive(false);
        }

        private static void ConfigureSlider(Slider slider, float value)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
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
            if (!initialized || !isActiveAndEnabled || state != PanelState.Closed) return;
            if (tryPause != null && !tryPause()) return;

            state = PanelState.Settings;
            ShowPanel(settingsPanel, settingsTransition);
        }

        public void CloseSettings()
        {
            if (state != PanelState.Settings) return;

            state = PanelState.Transitioning;
            HidePanel(settingsPanel, settingsTransition, ResumeAfterPanelCloses);
        }

        public void OpenCredits()
        {
            if (state != PanelState.Settings) return;

            state = PanelState.Transitioning;
            HidePanel(settingsPanel, settingsTransition, () =>
            {
                state = PanelState.Credits;
                ShowPanel(creditsPanel, creditsTransition);
            });
        }

        public void CloseCredits()
        {
            if (state != PanelState.Credits) return;

            state = PanelState.Transitioning;
            HidePanel(creditsPanel, creditsTransition, () =>
            {
                state = PanelState.Settings;
                ShowPanel(settingsPanel, settingsTransition);
            });
        }

        private void OpenReturnConfirmation()
        {
            if (state != PanelState.Settings || tryReturnToMenu == null) return;

            state = PanelState.Transitioning;
            HidePanel(settingsPanel, settingsTransition, () =>
            {
                state = PanelState.ConfirmReturn;
                ShowPanel(returnConfirmationPanel, returnConfirmationTransition);
            });
        }

        private void ConfirmReturnToMenu()
        {
            if (state != PanelState.ConfirmReturn) return;

            state = PanelState.Leaving;
            if (!tryReturnToMenu()) state = PanelState.ConfirmReturn;
        }

        private void CancelReturnToMenu()
        {
            if (state != PanelState.ConfirmReturn) return;

            state = PanelState.Transitioning;
            HidePanel(returnConfirmationPanel, returnConfirmationTransition, ResumeAfterPanelCloses);
        }

        private void ResumeAfterPanelCloses()
        {
            state = PanelState.Closed;
            resumeAfterClose?.Invoke();
        }

        private static void ShowPanel(GameObject panel, UIPanelTransition transition)
        {
            if (transition != null && transition.enabled)
                transition.Show();
            else
                panel.SetActive(true);
        }

        private static void HidePanel(GameObject panel, UIPanelTransition transition, Action afterClose)
        {
            if (transition != null && transition.enabled)
            {
                transition.Hide(afterClose);
            }
            else
            {
                panel.SetActive(false);
                afterClose?.Invoke();
            }
        }

        private void OnDestroy()
        {
            if (!initialized)
                return;

            if (masterSlider != null)
                masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
            if (musicSlider != null)
                musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            if (sfxSlider != null)
                sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);

            if (settingsOpenButton != null)
                settingsOpenButton.onClick.RemoveListener(OpenSettings);
            if (settingsCloseButton != null)
                settingsCloseButton.onClick.RemoveListener(CloseSettings);
            if (creditsOpenButton != null)
                creditsOpenButton.onClick.RemoveListener(OpenCredits);
            if (creditsCloseButton != null)
                creditsCloseButton.onClick.RemoveListener(CloseCredits);
            if (returnToMenuButton != null)
                returnToMenuButton.onClick.RemoveListener(OpenReturnConfirmation);
            if (confirmReturnYesButton != null)
                confirmReturnYesButton.onClick.RemoveListener(ConfirmReturnToMenu);
            if (confirmReturnNoButton != null)
                confirmReturnNoButton.onClick.RemoveListener(CancelReturnToMenu);
        }
    }
}