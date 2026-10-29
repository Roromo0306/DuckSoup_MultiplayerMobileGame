using UnityEngine;
using TMPro;

namespace DuckSoup.Features.GameFeel.Gameplay
{
    // Timer rojo <= 30s y pitch de musica acelerado de forma suave.
    [DisallowMultipleComponent]
    public sealed class TimerMusicFeel : MonoBehaviour
    {
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private AudioSource musicSource;
        [SerializeField, Min(0.1f)] private float warningAtSeconds = 30f;
        [SerializeField] private Color warningColor = new Color(0.95f, 0.18f, 0.20f, 1f);
        [SerializeField, Range(1f, 2f)] private float fastPitch = 1.18f;
        [SerializeField, Min(0.05f)] private float pitchTransitionSeconds = 1f;

        private Color normalColor;
        private float originalPitch = 1f;
        private bool playing;
        private bool warning;

        private void Awake()
        {
            if (timerText != null) normalColor = timerText.color;
            if (musicSource != null) originalPitch = musicSource.pitch;
        }

        public void BeginGameplay()
        {
            playing = true;
            warning = false;
            if (timerText != null) timerText.color = normalColor;
            if (musicSource != null)
            {
                musicSource.pitch = originalPitch;
                musicSource.loop = true;
                if (musicSource.clip != null && !musicSource.isPlaying)
                    musicSource.Play();
            }
        }

        public void RenderRemaining(float seconds)
        {
            if (!playing) return;
            warning = seconds <= warningAtSeconds && seconds > 0f;
            if (timerText != null)
                timerText.color = warning ? warningColor : normalColor;
        }

        private void Update()
        {
            if (!playing || musicSource == null) return;
            float target = originalPitch * (warning ? fastPitch : 1f);
            float speed = (Mathf.Max(0.01f, fastPitch - 1f) * originalPitch) /
                pitchTransitionSeconds;
            musicSource.pitch = Mathf.MoveTowards(
                musicSource.pitch, target, speed * Time.unscaledDeltaTime);
        }

        public void EndGameplay()
        {
            playing = false;
            warning = false;
            if (timerText != null) timerText.color = normalColor;
            if (musicSource != null)
            {
                musicSource.pitch = originalPitch;
                musicSource.Stop();
            }
        }

        private void OnDisable() => EndGameplay();
    }
}
