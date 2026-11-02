using System.Collections;
using UnityEngine;

namespace DuckSoup.Features.Transitions
{
    /// <summary>
    /// Fundidos de volumen en un AudioSource individual.
    /// No modifica el AudioMixer: Master/Music/SFX siguen funcionando.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class SceneMusicFader : MonoBehaviour
    {
        [SerializeField] private AudioSource musicSource;
        [Header("Menu: true / Gameplay: false")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField, Min(0.01f)] private float fadeInSeconds = 0.65f;
        [SerializeField, Min(0.01f)] private float fadeOutSeconds = 0.30f;

        private float normalVolume = 1f;
        private Coroutine currentFade;

        private void Awake()
        {
            if (musicSource == null)
                musicSource = GetComponent<AudioSource>();

            normalVolume = musicSource.volume;
            musicSource.playOnAwake = false;
            musicSource.volume = 0f;
        }

        private void Start()
        {
            if (playOnStart)
                FadeInAndPlay();
        }

        public void FadeInAndPlay()
        {
            if (musicSource == null || musicSource.clip == null)
                return;

            StopCurrentFade();
            if (!musicSource.isPlaying)
                musicSource.Play();

            currentFade = StartCoroutine(FadeTo(
                normalVolume, fadeInSeconds, false));
        }

        public void FadeOutAndStop()
        {
            if (musicSource == null)
                return;

            StopCurrentFade();
            if (!musicSource.isPlaying)
            {
                musicSource.volume = 0f;
                return;
            }
            currentFade = StartCoroutine(FadeTo(
                0f, fadeOutSeconds, true));
        }

        private IEnumerator FadeTo(float target, float seconds, bool stopAtEnd)
        {
            float initial = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                musicSource.volume = Mathf.Lerp(
                    initial, target, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            musicSource.volume = target;
            if (stopAtEnd)
                musicSource.Stop();
            currentFade = null;
        }

        private void StopCurrentFade()
        {
            if (currentFade == null)
                return;
            StopCoroutine(currentFade);
            currentFade = null;
        }

        private void OnDisable()
        {
            StopCurrentFade();
        }
    }
}
