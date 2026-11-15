
using System.Collections;
using DuckSoup.Features.Transitions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DuckSoup.Features.Loading
{
    public sealed class LoadingScreenController : MonoBehaviour
    {
        [Header("Destination")]
        [SerializeField]
        private string mainMenuSceneName = "MainMenuScene";

        [Header("Animations")]
        [SerializeField]
        private LoadingTitleBounce titleAnimation;

        [SerializeField]
        private LoadingFoodEmitter foodEmitter;

        [Header("Loading Bar")]
        [SerializeField]
        private LoadingRevealBar loadingBar;

        [Header("Optional Text")]
        [SerializeField]
        private TextMeshProUGUI percentageText;

        [SerializeField]
        private TextMeshProUGUI loadingText;

        [Header("Timing")]
        [SerializeField, Min(0.5f)]
        private float minimumLoadingSeconds = 5f;

        [SerializeField, Min(0f)]
        private float completedDelay = 0.3f;

        private IEnumerator Start()
        {
            Time.timeScale = 1f;

            if (loadingBar == null)
            {
                Debug.LogError(
                    "[Loading] Missing LoadingBar.",
                    this);

                yield break;
            }

            if (!UnityEngine.Application
                .CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError(
                    "[Loading] Scene not found: " +
                    mainMenuSceneName,
                    this);

                yield break;
            }

            loadingBar.SetProgress(0f);

            if (percentageText != null)
                percentageText.text = "0%";

            if (loadingText != null)
                loadingText.text = "Preparing the kitchen...";

            // MISMA DURACION para titulo y barra.
            titleAnimation?.Play(minimumLoadingSeconds);

            foodEmitter?.Play();

            AsyncOperation operation =
                SceneManager.LoadSceneAsync(mainMenuSceneName);

            if (operation == null)
            {
                Debug.LogError(
                    "[Loading] Scene loading failed.",
                    this);

                yield break;
            }

            operation.allowSceneActivation = false;

            float elapsed = 0f;

            while (true)
            {
                elapsed += Time.unscaledDeltaTime;

                float visualProgress = Mathf.Clamp01(
                    elapsed / minimumLoadingSeconds);

                float realProgress = Mathf.Clamp01(
                    operation.progress / 0.9f);

                float displayedProgress = Mathf.Min(
                    visualProgress,
                    realProgress);

                loadingBar.SetProgress(displayedProgress);

                if (percentageText != null)
                {
                    percentageText.text =
                        Mathf.RoundToInt(
                            displayedProgress * 100f) + "%";
                }

                bool minimumFinished =
                    elapsed >= minimumLoadingSeconds;

                bool sceneReady =
                    operation.progress >= 0.9f;

                bool titleFinished =
                    titleAnimation == null ||
                    titleAnimation.IsFinished;

                if (minimumFinished &&
                    sceneReady &&
                    titleFinished)
                    break;

                yield return null;
            }

            loadingBar.SetProgress(1f);

            if (percentageText != null)
                percentageText.text = "100%";

            if (loadingText != null)
                loadingText.text = "Kitchen ready!";

            if (completedDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    completedDelay);
            }

            bool fadeStarted =
                SceneFadeTransition.FadeOutThen(
                    () =>
                    {
                        operation.allowSceneActivation = true;
                    });

            if (!fadeStarted)
                operation.allowSceneActivation = true;
        }
    }
}

