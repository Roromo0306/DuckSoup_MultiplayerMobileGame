
using System.Collections;
using DuckSoup.Features.Transitions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DuckSoup.Features.Loading
{
    public sealed class LoadingScreenController : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField]
        private string mainMenuSceneName = "MainMenuScene";

        [Header("Loading UI")]
        [SerializeField]
        private Slider progressSlider;

        [SerializeField]
        private TextMeshProUGUI percentageText;

        [SerializeField]
        private TextMeshProUGUI loadingText;

        [Header("Fake Loading")]
        [SerializeField, Min(0.1f)]
        private float fakeLoadingDuration = 3f;

        [SerializeField, Min(0f)]
        private float completedDelay = 0.3f;

        private IEnumerator Start()
        {
            Time.timeScale = 1f;

            if (progressSlider == null ||
                percentageText == null ||
                loadingText == null)
            {
                Debug.LogError(
                    "[Loading] Faltan referencias.",
                    this);
                yield break;
            }

            if (!UnityEngine.Application
                .CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError(
                    "[Loading] MainMenuScene no esta " +
                    "en Build Settings.",
                    this);
                yield break;
            }

            // Configuramos el Slider normal.
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.wholeNumbers = false;

            progressSlider.transition =
                Selectable.Transition.None;

            progressSlider.interactable = false;

            UpdateProgress(0f);

            loadingText.text =
                "Preparing the kitchen...";

            // Preparamos la siguiente escena.
            AsyncOperation operation =
                SceneManager.LoadSceneAsync(
                    mainMenuSceneName);

            if (operation == null)
                yield break;

            operation.allowSceneActivation = false;

            float elapsed = 0f;

            // Carga falsa con tiempo obligatorio.
            while (elapsed < fakeLoadingDuration ||
                   operation.progress < 0.9f)
            {
                elapsed += Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(
                    elapsed / fakeLoadingDuration);

                // Si la escena real no esta lista,
                // mantenemos el progreso al 99%.
                if (operation.progress < 0.9f)
                {
                    progress = Mathf.Min(
                        progress, 0.99f);
                }

                UpdateProgress(progress);

                yield return null;
            }

            // Barra completa.
            UpdateProgress(1f);

            loadingText.text = "Kitchen ready!";

            if (completedDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    completedDelay);
            }

            // Fundido antes de activar el menu.
            bool fadeStarted =
                SceneFadeTransition.FadeOutThen(
                    () =>
                    {
                        operation.allowSceneActivation = true;
                    });

            if (!fadeStarted)
            {
                operation.allowSceneActivation = true;
            }
        }

        private void UpdateProgress(float value)
        {
            float progress = Mathf.Clamp01(value);

            // Solo cambiamos el valor del Slider.
            progressSlider.SetValueWithoutNotify(
                progress);

            // El porcentaje usa ese mismo valor.
            percentageText.text =
                Mathf.RoundToInt(
                    progressSlider.normalizedValue * 100f)
                + "%";
        }
    }
}
