using System.Collections;
using UnityEngine;

namespace DuckSoup.Features.GameFeel.UI
{
    [DisallowMultipleComponent]
    public sealed class UIShakeFeel : MonoBehaviour
    {
        [SerializeField] private RectTransform visualTarget;
        [SerializeField, Min(0f)] private float amplitude = 16f;
        [SerializeField, Min(0.05f)] private float duration = 0.5f;
        [SerializeField, Min(1f)] private float cycles = 8f;
        private Vector2 home;
        private Coroutine current;

        private void Awake()
        {
            if (visualTarget == null) visualTarget = GetComponent<RectTransform>();
            if (visualTarget != null) home = visualTarget.anchoredPosition;
        }

        public void Shake()
        {
            if (visualTarget == null) return;
            ResetNow();
            current = StartCoroutine(ShakeRoutine());
        }

        private IEnumerator ShakeRoutine()
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float x = Mathf.Sin(t * cycles * Mathf.PI * 2f) *
                          (1f - t) * amplitude;
                visualTarget.anchoredPosition = home + Vector2.right * x;
                yield return null;
            }
            visualTarget.anchoredPosition = home;
            current = null;
        }

        public void ResetNow()
        {
            if (current != null) StopCoroutine(current);
            current = null;
            if (visualTarget != null) visualTarget.anchoredPosition = home;
        }

        private void OnDisable() => ResetNow();
    }
}
