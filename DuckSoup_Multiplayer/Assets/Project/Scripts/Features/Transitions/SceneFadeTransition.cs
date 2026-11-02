using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DuckSoup.Features.Transitions
{
    /// <summary>
    /// Fundido de pantalla para cambios de escena.
    /// Instalar una vez como prefab RAIZ con Canvas Screen Space Overlay
    /// + Image a pantalla completa + CanvasGroup.
    /// Vive entre escenas y descarta sus copias duplicadas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneFadeTransition : MonoBehaviour
    {
        [Header("Full-screen overlay")]
        [SerializeField] private CanvasGroup overlay;
        [SerializeField, Min(0.01f)] private float fadeOutSeconds = 0.36f;
        [SerializeField, Min(0.01f)] private float fadeInSeconds = 0.40f;
        [SerializeField] private bool fadeInOnFirstScene = true;

        private static SceneFadeTransition instance;
        private bool busy;
        private Coroutine overlayFade;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (overlay == null)
                overlay = GetComponentInChildren<CanvasGroup>(true);
            if (overlay == null)
            {
                Debug.LogError("[SceneFade] Falta Overlay / CanvasGroup.", this);
                enabled = false;
                return;
            }

            overlay.alpha = fadeInOnFirstScene ? 1f : 0f;
            overlay.interactable = false;
            overlay.blocksRaycasts = fadeInOnFirstScene;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            if (instance == this && fadeInOnFirstScene && enabled)
                BeginFadeIn();
        }

        public static bool LoadScene(string sceneName)
        {
            if (!UnityEngine.Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("[SceneFade] La escena no esta en Build Settings: " + sceneName);
                return false;
            }

            if (instance == null || !instance.enabled)
            {
                Debug.LogWarning("[SceneFade] No hay SceneFadeTransition. Carga sin fade.");
                SceneManager.LoadScene(sceneName);
                return true;
            }

            if (instance.busy)
                return false;

            instance.busy = true;
            instance.StopOverlayFade();
            instance.StartCoroutine(instance.LoadRoutine(sceneName));
            return true;
        }

        /// <summary>
        /// Para escenas que YA estan precargando con LoadSceneAsync
        /// y allowSceneActivation=false (LoadingScene).
        /// No inicia una segunda carga: solo libera la activacion al oscurecer.
        /// </summary>
        public static bool FadeOutThen(Action whenBlack)
        {
            if (whenBlack == null)
                return false;

            if (instance == null || !instance.enabled)
                return false;

            if (instance.busy)
                return false;

            instance.busy = true;
            instance.StopOverlayFade();
            instance.StartCoroutine(instance.FadeOutThenRoutine(whenBlack));
            return true;
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            FadeOutSceneMusic();
            yield return AnimateOverlay(1f, fadeOutSeconds);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                Debug.LogError("[SceneFade] No se pudo cargar " + sceneName);
                BeginFadeIn();
            }
            // La nueva escena inicia el fade-in en HandleSceneLoaded.
        }

        private IEnumerator FadeOutThenRoutine(Action whenBlack)
        {
            FadeOutSceneMusic();
            yield return AnimateOverlay(1f, fadeOutSeconds);
            whenBlack.Invoke();
            // HandleSceneLoaded inicia el fade-in cuando la escena se active.
        }

        private static void FadeOutSceneMusic()
        {
            // Solo fuentes de musica de la escena actual, nunca GameAudio/SFX.
            SceneMusicFader[] musicTracks =
                FindObjectsOfType<SceneMusicFader>();
            foreach (SceneMusicFader track in musicTracks)
                track.FadeOutAndStop();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (instance != this || !enabled)
                return;
            BeginFadeIn();
        }

        private void StopOverlayFade()
        {
            if (overlayFade == null) return;
            StopCoroutine(overlayFade);
            overlayFade = null;
        }

        private void BeginFadeIn()
        {
            StopOverlayFade();
            overlay.alpha = 1f;
            overlayFade = StartCoroutine(FadeInRoutine());
        }

        private IEnumerator FadeInRoutine()
        {
            yield return AnimateOverlay(0f, fadeInSeconds);
            overlay.blocksRaycasts = false;
            busy = false;
            overlayFade = null;
        }

        private IEnumerator AnimateOverlay(float target, float duration)
        {
            float initial = overlay.alpha;
            overlay.blocksRaycasts = true;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                overlay.alpha = Mathf.Lerp(
                    initial, target, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            overlay.alpha = target;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                instance = null;
            }
        }
    }
}
