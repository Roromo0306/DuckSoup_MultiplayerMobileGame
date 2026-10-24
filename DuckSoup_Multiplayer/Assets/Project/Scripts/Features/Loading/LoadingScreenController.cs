
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DuckSoup.Features.Loading
{
    /// <summary>
    /// Muestra una barra de carga simulada
    /// mientras prepara el menú principal.
    /// </summary>
    public sealed class LoadingScreenController : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField]
        private string mainMenuSceneName = "MainMenuScene";

        [Header("Loading UI")]
        [SerializeField]
        private Image progressFill;

        [SerializeField]
        private TextMeshProUGUI percentageText;

        [SerializeField]
        private TextMeshProUGUI loadingText;

        [Header("Timing")]
        [SerializeField, Min(0.1f)]
        private float fakeLoadingDuration = 3f;

        [SerializeField, Min(0f)]
        private float completedDelay = 0.3f;

        private void Awake()
        {
            // Evita heredar una pausa de una partida anterior.
            Time.timeScale = 1f;

            if (progressFill != null)
            {
                progressFill.type = Image.Type.Filled;
                progressFill.fillMethod =
                    Image.FillMethod.Horizontal;
                progressFill.fillOrigin = 0;

                progressFill.fillAmount = 0f;
            }
        }

        private IEnumerator Start()
        {
            if (progressFill == null ||
                percentageText == null ||
                loadingText == null)
            {
                Debug.LogError(
                    "[Loading] Faltan referencias de UI.",
                    this);

                yield break;
            }

            if (!UnityEngine.Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError(
                    "[Loading] MainMenuScene no está " +
                    "en Scenes In Build.",
                    this);

                yield break;
            }

            loadingText.text = "Preparing the kitchen...";
            UpdateProgress(0f);

            // Precargamos la escena sin activarla.
            AsyncOperation operation =
                SceneManager.LoadSceneAsync(mainMenuSceneName);

            operation.allowSceneActivation = false;

            float elapsed = 0f;

            // Esperamos la duración simulada y
            // a que la escena esté lista.
            while (elapsed < fakeLoadingDuration ||
                   operation.progress < 0.9f)
            {
                elapsed += Time.unscaledDeltaTime;

                float normalized = Mathf.Clamp01(
                    elapsed / fakeLoadingDuration);

                // Avance visual suave hasta el 95%.
                float simulatedProgress =
                    Mathf.SmoothStep(0f, 0.95f, normalized);

                UpdateProgress(simulatedProgress);

                if (operation.progress >= 0.9f)
                {
                    loadingText.text = "Almost ready...";
                }

                yield return null;
            }

            UpdateProgress(1f);
            loadingText.text = "Kitchen ready!";

            yield return new WaitForSecondsRealtime(
                completedDelay);

            // Permitimos activar el menú.
            operation.allowSceneActivation = true;
        }

        private void UpdateProgress(float value)
        {
            float clamped = Mathf.Clamp01(value);

            progressFill.fillAmount = clamped;

            percentageText.text =
                Mathf.RoundToInt(clamped * 100f) + "%";
        }
    }
}

