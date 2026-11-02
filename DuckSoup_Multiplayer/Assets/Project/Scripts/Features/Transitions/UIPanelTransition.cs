using System;
using System.Collections;
using UnityEngine;

namespace DuckSoup.Features.Transitions
{
    /// <summary>
    /// Transicion de panel: fade + pop suave.
    /// Anadir a la RAIZ del panel junto a CanvasGroup.
    /// Animar solo el RectTransform de la tarjeta interna (Visual Root).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UIPanelTransition : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform visualRoot;

        [Header("Open")]
        [SerializeField, Min(0.01f)] private float openSeconds = 0.22f;
        [SerializeField, Range(0.75f, 1f)] private float startingScale = 0.94f;
        [SerializeField, Range(0f, 0.06f)] private float popOvershoot = 0.015f;

        [Header("Close")]
        [SerializeField, Min(0.01f)] private float closeSeconds = 0.17f;
        [SerializeField, Range(0.75f, 1f)] private float closingScale = 0.97f;

        private Vector3 originalScale = Vector3.one;
        private Coroutine currentAnimation;
        private bool isClosing;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (visualRoot == null)
                visualRoot = GetComponent<RectTransform>();

            if (visualRoot != null)
                originalScale = visualRoot.localScale;
        }

        private void OnEnable()
        {
            // Tambien funciona cuando un script antiguo usa panel.SetActive(true).
            PlayOpen();
        }

        public void Show()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                return; // OnEnable llama a PlayOpen.
            }
            PlayOpen();
        }

        private void PlayOpen()
        {
            if (canvasGroup == null || visualRoot == null)
                return;

            StopCurrentAnimation();
            isClosing = false;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
            visualRoot.localScale = originalScale * startingScale;
            currentAnimation = StartCoroutine(AnimateOpen());
        }

        public void Hide(Action afterHidden = null)
        {
            if (!gameObject.activeInHierarchy)
            {
                afterHidden?.Invoke();
                return;
            }

            if (isClosing)
                return;

            StopCurrentAnimation();
            isClosing = true;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
            currentAnimation = StartCoroutine(AnimateClose(afterHidden));
        }

        private IEnumerator AnimateOpen()
        {
            float elapsed = 0f;
            while (elapsed < openSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / openSeconds);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
                float scale = Mathf.Lerp(startingScale, 1f, eased)
                            + Mathf.Sin(t * Mathf.PI) * popOvershoot;
                visualRoot.localScale = originalScale * scale;
                yield return null;
            }

            visualRoot.localScale = originalScale;
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            currentAnimation = null;
        }

        private IEnumerator AnimateClose(Action afterHidden)
        {
            Vector3 fromScale = visualRoot.localScale;
            float fromAlpha = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < closeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / closeSeconds);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                canvasGroup.alpha = Mathf.Lerp(fromAlpha, 0f, eased);
                visualRoot.localScale = Vector3.Lerp(
                    fromScale, originalScale * closingScale, eased);
                yield return null;
            }

            currentAnimation = null;
            isClosing = false;
            gameObject.SetActive(false);
            afterHidden?.Invoke();
        }

        private void StopCurrentAnimation()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
                currentAnimation = null;
            }
        }

        private void OnDisable()
        {
            StopCurrentAnimation();
            isClosing = false;
            if (visualRoot != null)
                visualRoot.localScale = originalScale;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }
    }
}
