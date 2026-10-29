using System.Collections;
using UnityEngine;

namespace DuckSoup.Features.GameFeel.UI
{
    [DisallowMultipleComponent]
    public sealed class UIPunchFeel : MonoBehaviour
    {
        [SerializeField] private Transform visualTarget;
        [SerializeField, Range(1f, 1.5f)] private float peakMultiplier = 1.18f;
        [SerializeField, Min(0.02f)] private float durationSeconds = 0.23f;

        private Vector3 baseScale;
        private Coroutine active;

        private void Awake()
        {
            if (visualTarget == null) visualTarget = transform;
            baseScale = visualTarget.localScale;
        }

        public void Punch()
        {
            ResetNow();
            if (isActiveAndEnabled) active = StartCoroutine(PunchRoutine());
        }

        private IEnumerator PunchRoutine()
        {
            float elapsed = 0f;
            while (elapsed < durationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / durationSeconds);
                float amount = t < 0.35f
                    ? Mathf.SmoothStep(0f, 1f, t / 0.35f)
                    : 1f - Mathf.SmoothStep(0f, 1f, (t - 0.35f) / 0.65f);
                visualTarget.localScale = baseScale * Mathf.Lerp(1f, peakMultiplier, amount);
                yield return null;
            }
            visualTarget.localScale = baseScale;
            active = null;
        }

        public void ResetNow()
        {
            if (active != null) StopCoroutine(active);
            active = null;
            if (visualTarget != null) visualTarget.localScale = baseScale;
        }

        private void OnDisable() => ResetNow();
    }
}
