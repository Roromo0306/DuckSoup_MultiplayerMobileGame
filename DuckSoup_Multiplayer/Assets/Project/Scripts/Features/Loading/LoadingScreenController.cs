
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
        private Image progressFill;

        [SerializeField]
        private TextMeshProUGUI percentageText;

        [SerializeField]
        private TextMeshProUGUI loadingText;

        [Header("Fake Loading")]
        [SerializeField, Min(0.1f)]
        private float fakeLoadingDuration = 3f;

        [SerializeField, Min(0f)]
        private float completedDelay = 0.3f;

        private RectTransform fillRect;

        private IEnumerator Start()
        {
            Time.timeScale = 1f;

            if (progressFill == null ||
                percentageText == null ||
                loadingText == null)
            {
                Debug.LogError(
                    "[Loading] Faltan referencias de UI.",
                    this);

                yield break;
            }

            if (!UnityEngine.Application
                .CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError(
                    "[Loading] La escena no esta " +
                    "en Build Settings.",
                    this);

                yield break;
            }

            // ==================================
            // CONFIGURAR LA BARRA
            // ==================================

            fillRect = progressFill.rectTransform;

            progressFill.type = Image.Type.Simple;
            progressFill.preserveAspect = false;

            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.localScale = Vector3.one;

            UpdateProgress(0f);

            loadingText.text =
                "Preparing the kitchen...";

            // Precargamos el menu sin activarlo.
            AsyncOperation operation =
                SceneManager.LoadSceneAsync(
                    mainMenuSceneName);

            if (operation == null)
            {
                Debug.LogError(
                    "[Loading] No se pudo cargar el menu.",
                    this);

                yield break;
            }

            operation.allowSceneActivation = false;

            float elapsed = 0f;

            // ==================================
            // CARGA SIMULADA
            // ==================================

            while (elapsed < fakeLoadingDuration ||
                   operation.progress < 0.9f)
            {
                elapsed += Time.unscaledDeltaTime;

                // Valor de 0 a 1, calculado
                // exclusivamente por tiempo.
                float normalized = Mathf.Clamp01(
                    elapsed / fakeLoadingDuration);

                // Reservamos el ultimo 1% hasta
                // que la escena este preparada.
                float progress = Mathf.Min(
                    normalized,
                    0.99f);

                // Un unico valor controla
                // barra y porcentaje.
                UpdateProgress(progress);

                if (elapsed >= fakeLoadingDuration &&
                    operation.progress < 0.9f)
                {
                    loadingText.text =
                        "Almost ready...";
                }

                yield return null;
            }

            // ==================================
            // FINALIZAR AL 100%
            // ==================================

            UpdateProgress(1f);

            loadingText.text = "Kitchen ready!";

            if (completedDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    completedDelay);
            }

            // ==================================
            // FUNDIDO Y CAMBIO DE ESCENA
            // ==================================

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

        // ==================================
        // SINCRONIZAR BARRA Y PORCENTAJE
        // ==================================

        private void UpdateProgress(float value)
        {
            float progress = Mathf.Clamp01(value);

            // La anchura visible equivale
            // exactamente al porcentaje.
            fillRect.anchorMin = Vector2.zero;

            fillRect.anchorMax = new Vector2(
                progress, 1f);

            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            // Mostramos el mismo progreso.
            int percentage =
                Mathf.RoundToInt(progress * 100f);

            percentageText.text =
                percentage + "%";
        }
    }
}
