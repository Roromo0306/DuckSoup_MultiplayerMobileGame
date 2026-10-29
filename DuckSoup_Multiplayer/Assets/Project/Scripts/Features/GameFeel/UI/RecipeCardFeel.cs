using System.Collections;
using UnityEngine;

namespace DuckSoup.Features.GameFeel.UI
{
    // Mantener la raiz de la carta bajo el LayoutGroup; animar VisualRoot.
    [DisallowMultipleComponent]
    public sealed class RecipeCardFeel : MonoBehaviour
    {
        [Header("Use a child VisualRoot to avoid fighting LayoutGroup")]
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0.05f)] private float completeSeconds = 0.32f;
        [SerializeField, Min(0.05f)] private float dealSeconds = 0.38f;
        [SerializeField, Min(0f)] private float liftDistance = 45f;
        [SerializeField, Min(0f)] private float fallbackSideDistance = 160f;
        [SerializeField, Min(0f)] private float fallbackCenterHeight = 125f;

        private Vector3 restingLocalPosition;
        private Vector3 restingScale;
        private string observedId;
        private Coroutine mainAnimation;
        private Coroutine shakeAnimation;

        private void Awake()
        {
            if (visualRoot == null) visualRoot = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = visualRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = visualRoot.gameObject.AddComponent<CanvasGroup>();
            restingLocalPosition = visualRoot.localPosition;
            restingScale = visualRoot.localScale;
            ResetVisual();
        }

        // Llamar en cada Bind: solo dispara animacion cuando cambia el ID.
        public void ObserveCard(string cardId, RectTransform dealingOrigin, int slotIndex)
        {
            if (string.IsNullOrEmpty(cardId)) return;
            if (observedId == null)
            {
                observedId = cardId;
                ResetVisual();
                return;
            }
            if (observedId == cardId) return;
            observedId = cardId;
            StartDeal(dealingOrigin, slotIndex);
        }

        public void Complete()
        {
            StopAnimations();
            mainAnimation = StartCoroutine(CompleteRoutine());
        }

        public void ShakeWrong()
        {
            if (mainAnimation != null || canvasGroup.alpha < 0.95f) return;
            if (shakeAnimation != null) StopCoroutine(shakeAnimation);
            shakeAnimation = StartCoroutine(ShakeRoutine());
        }

        private void StartDeal(RectTransform origin, int slotIndex)
        {
            StopAnimations();
            mainAnimation = StartCoroutine(DealRoutine(origin, slotIndex));
        }

        private IEnumerator CompleteRoutine()
        {
            float elapsed = 0f;
            while (elapsed < completeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / completeSeconds);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                visualRoot.localPosition = restingLocalPosition +
                    Vector3.up * (liftDistance * eased);
                visualRoot.localScale = restingScale * (1f + 0.07f * Mathf.Sin(t * Mathf.PI));
                canvasGroup.alpha = 1f - eased;
                yield return null;
            }
            visualRoot.localScale = restingScale;
            canvasGroup.alpha = 0f; // Oculta la carta hasta su reemplazo.
            mainAnimation = null;
        }

        private IEnumerator DealRoutine(RectTransform origin, int slotIndex)
        {
            Vector3 from;
            if (origin != null && visualRoot.parent != null)
            {
                from = visualRoot.parent.InverseTransformPoint(origin.position);
            }
            else
            {
                int side = slotIndex == 0 ? -1 : slotIndex == 2 ? 1 : 0;
                from = restingLocalPosition + new Vector3(
                    side * fallbackSideDistance, fallbackCenterHeight, 0f);
            }

            visualRoot.localPosition = from;
            visualRoot.localScale = restingScale * 0.72f;
            canvasGroup.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < dealSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / dealSeconds);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                visualRoot.localPosition = Vector3.LerpUnclamped(
                    from, restingLocalPosition, eased);
                float pop = 1f + 0.055f * Mathf.Sin(t * Mathf.PI);
                visualRoot.localScale = restingScale * Mathf.Lerp(0.72f, pop, eased);
                canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
                yield return null;
            }
            visualRoot.localPosition = restingLocalPosition;
            visualRoot.localScale = restingScale;
            canvasGroup.alpha = 1f;
            mainAnimation = null;
        }

        private IEnumerator ShakeRoutine()
        {
            float elapsed = 0f;
            const float seconds = 0.31f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                float x = 13f * Mathf.Sin(t * Mathf.PI * 10f) * (1f - t);
                visualRoot.localPosition = restingLocalPosition + Vector3.right * x;
                yield return null;
            }
            visualRoot.localPosition = restingLocalPosition;
            shakeAnimation = null;
        }

        private void StopAnimations()
        {
            if (mainAnimation != null) StopCoroutine(mainAnimation);
            if (shakeAnimation != null) StopCoroutine(shakeAnimation);
            mainAnimation = null;
            shakeAnimation = null;
        }

        public void ResetVisual()
        {
            StopAnimations();
            if (visualRoot == null) return;
            visualRoot.localPosition = restingLocalPosition;
            visualRoot.localScale = restingScale;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        private void OnDisable() => ResetVisual();
    }
}
