using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.PowerUps
{
    [DisallowMultipleComponent]
    public sealed class RecipeComboOverlay : MonoBehaviour
    {
        [Header("Images de la rueda")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image radialFillImage;

        [Header("Images de los combos")]
        [SerializeField] private Image comboX2Image;
        [SerializeField] private Image comboX3Image;

        [Header("Animacion del combo")]
        [SerializeField, Min(0.5f)]
        private float popupSeconds = 2.8f;

        [SerializeField, Min(0f)]
        private float popupRise = 18f;

        private Coroutine popupRoutine;
        private Image activePopup;
        private CanvasGroup popupCanvas;

        private Vector2 originalPopupPosition;
        private Vector3 originalPopupScale;

        private void Awake()
        {
            ConfigureImage(backgroundImage);
            ConfigureImage(radialFillImage);
            ConfigureImage(comboX2Image);
            ConfigureImage(comboX3Image);

            if (radialFillImage != null)
            {
                radialFillImage.type = Image.Type.Filled;
                radialFillImage.fillMethod =
                    Image.FillMethod.Radial360;

                radialFillImage.fillOrigin =
                    (int)Image.Origin360.Top;

                radialFillImage.fillClockwise = false;
                radialFillImage.fillAmount = 1f;
            }

            SetTimerVisible(false);

            if (comboX2Image != null)
                comboX2Image.enabled = false;

            if (comboX3Image != null)
                comboX3Image.enabled = false;
        }

        private static void ConfigureImage(Image image)
        {
            if (image == null)
                return;

            image.raycastTarget = false;
            image.preserveAspect = true;
        }

        public void Render(
            bool cardIsComplete,
            float timerFill01)
        {
            bool visible =
                cardIsComplete && timerFill01 > 0f;

            SetTimerVisible(visible);

            if (radialFillImage != null)
            {
                radialFillImage.fillAmount =
                    Mathf.Clamp01(timerFill01);
            }
        }

        private void SetTimerVisible(bool visible)
        {
            SetImageVisible(backgroundImage, visible);
            SetImageVisible(radialFillImage, visible);
        }

        private static void SetImageVisible(
            Image image,
            bool visible)
        {
            if (image == null)
                return;

            if (visible)
                image.gameObject.SetActive(true);

            image.enabled = visible;
        }

        public void PlayCombo(int multiplier)
        {
            if (multiplier < 2 || !isActiveAndEnabled)
                return;

            StopPopup();

            Image selectedImage = multiplier >= 3
                ? comboX3Image
                : comboX2Image;

            if (selectedImage == null)
            {
                Debug.LogWarning(
                    "[RecipeComboOverlay] Asigna las Images " +
                    "de COMBO x2 y COMBO x3 en el prefab.",
                    this);

                return;
            }

            activePopup = selectedImage;

            originalPopupPosition =
                activePopup.rectTransform.anchoredPosition;

            originalPopupScale =
                activePopup.rectTransform.localScale;

            popupCanvas =
                activePopup.GetComponent<CanvasGroup>();

            if (popupCanvas == null)
            {
                popupCanvas =
                    activePopup.gameObject.AddComponent<CanvasGroup>();
            }

            popupCanvas.blocksRaycasts = false;
            popupCanvas.interactable = false;
            popupCanvas.alpha = 0f;

            activePopup.gameObject.SetActive(true);
            activePopup.enabled = true;
            activePopup.transform.SetAsLastSibling();

            popupRoutine =
                StartCoroutine(AnimatePopup());
        }

        private IEnumerator AnimatePopup()
        {
            float elapsed = 0f;

            while (elapsed < popupSeconds)
            {
                elapsed += Time.deltaTime;

                float progress =
                    Mathf.Clamp01(elapsed / popupSeconds);

                float appear =
                    Mathf.Clamp01(elapsed / 0.18f);

                float disappear =
                    Mathf.Clamp01(
                        (popupSeconds - elapsed) / 0.35f);

                popupCanvas.alpha =
                    Mathf.Min(appear, disappear);

                activePopup.rectTransform.localScale =
                    originalPopupScale *
                    Mathf.Lerp(0.75f, 1f, appear);

                activePopup.rectTransform.anchoredPosition =
                    originalPopupPosition +
                    Vector2.up * (popupRise * progress);

                yield return null;
            }

            popupRoutine = null;
            StopPopup();
        }

        private void StopPopup()
        {
            if (popupRoutine != null)
            {
                StopCoroutine(popupRoutine);
                popupRoutine = null;
            }

            if (activePopup != null)
            {
                activePopup.rectTransform.anchoredPosition =
                    originalPopupPosition;

                activePopup.rectTransform.localScale =
                    originalPopupScale;

                activePopup.enabled = false;
            }

            if (popupCanvas != null)
                popupCanvas.alpha = 0f;

            activePopup = null;
            popupCanvas = null;
        }

        private void OnDisable()
        {
            SetTimerVisible(false);
            StopPopup();
        }
    }
}