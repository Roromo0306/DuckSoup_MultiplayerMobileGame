using System;
using System.Collections;
using UnityEngine;

namespace DuckSoup.Features.GameFeel.UI
{
    // Anima una agrupacion visual (imagen+nombre+record) sin mover flechas ni SELECT.
    [DisallowMultipleComponent]
    public sealed class UICarouselFeel : MonoBehaviour
    {
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0.05f)] private float switchSeconds = 0.38f;
        [SerializeField, Min(0f)] private float travelDistance = 170f;
        [SerializeField, Range(1f, 1.25f)] private float selectedScale = 1.08f;

        private Vector2 home;
        private Vector3 baseScale;
        private Coroutine routine;
        private bool selected;

        public bool IsSwitching { get; private set; }

        private void Awake()
        {
            if (contentRoot == null) contentRoot = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = contentRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = contentRoot.gameObject.AddComponent<CanvasGroup>();
            home = contentRoot.anchoredPosition;
            baseScale = contentRoot.localScale;
        }

        public void ResetVisual(bool initialSelected = false)
        {
            StopCurrent();
            selected = initialSelected;
            contentRoot.anchoredPosition = home;
            contentRoot.localScale = baseScale * (selected ? selectedScale : 1f);
            canvasGroup.alpha = 1f;
        }

        public bool Switch(int direction, Action changeAtMidpoint)
        {
            if (IsSwitching || direction == 0 || !isActiveAndEnabled)
                return false;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(SwitchRoutine(direction > 0 ? 1 : -1, changeAtMidpoint));
            return true;
        }

        public void SetSelected(bool value)
        {
            selected = value;
            if (IsSwitching) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(SelectionRoutine());
        }

        private IEnumerator SwitchRoutine(int direction, Action changeAtMidpoint)
        {
            IsSwitching = true;
            float half = switchSeconds * 0.5f;
            float elapsed = 0f;
            Vector2 start = contentRoot.anchoredPosition;
            Vector2 outgoing = home + Vector2.right * (travelDistance * direction);
            float startAlpha = canvasGroup.alpha;

            // Direccion +1: sale hacia la derecha y entra por la izquierda.
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / half));
                contentRoot.anchoredPosition = Vector2.Lerp(start, outgoing, t);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }

            // Cambiar datos solo cuando no se ve el contenido anterior.
            changeAtMidpoint?.Invoke();
            contentRoot.anchoredPosition = home - Vector2.right * (travelDistance * direction);
            contentRoot.localScale = baseScale * 0.94f;
            canvasGroup.alpha = 0f;

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / half));
                contentRoot.anchoredPosition = Vector2.Lerp(
                    home - Vector2.right * travelDistance * direction, home, t);
                contentRoot.localScale = Vector3.Lerp(
                    baseScale * 0.94f,
                    baseScale * (selected ? selectedScale : 1f), t);
                canvasGroup.alpha = t;
                yield return null;
            }
            contentRoot.anchoredPosition = home;
            contentRoot.localScale = baseScale * (selected ? selectedScale : 1f);
            canvasGroup.alpha = 1f;
            IsSwitching = false;
            routine = null;
        }

        private IEnumerator SelectionRoutine()
        {
            Vector3 from = contentRoot.localScale;
            Vector3 to = baseScale * (selected ? selectedScale : 1f);
            const float seconds = 0.18f;
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / seconds));
                contentRoot.localScale = Vector3.Lerp(from, to, t);
                yield return null;
            }
            contentRoot.localScale = to;
            routine = null;
        }

        private void StopCurrent()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            IsSwitching = false;
        }

        private void OnDisable()
        {
            if (contentRoot != null) ResetVisual(selected);
        }
    }
}
