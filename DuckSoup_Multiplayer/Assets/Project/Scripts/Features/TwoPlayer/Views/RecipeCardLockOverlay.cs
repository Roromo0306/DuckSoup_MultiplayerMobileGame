
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.TwoPlayer.Views
{
    [DisallowMultipleComponent]
    public sealed class RecipeCardLockOverlay : MonoBehaviour
    {
        private GameObject overlayObject;
        private Image iconImage;
        private TextMeshProUGUI countdownText;
        private Sprite configuredIcon;

        public void Configure(Sprite optionalLockIcon)
        {
            configuredIcon = optionalLockIcon;

            if (overlayObject == null)
                CreateOverlay();

            iconImage.sprite = configuredIcon;
            iconImage.enabled = configuredIcon != null;
        }

        public void SetRemaining(float remainingSeconds)
        {
            if (overlayObject == null)
                CreateOverlay();

            bool locked = remainingSeconds > 0.01f;

            if (overlayObject.activeSelf != locked)
                overlayObject.SetActive(locked);

            if (locked)
            {
                overlayObject.transform.SetAsLastSibling();

                countdownText.text =
                    "LOCKED\n" +
                    remainingSeconds.ToString("0.0") +
                    "s";
            }
        }

        private void CreateOverlay()
        {
            overlayObject = new GameObject(
                "CardLockOverlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            RectTransform overlayRect =
                overlayObject.GetComponent<RectTransform>();

            overlayRect.SetParent(transform, false);
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlayRect.SetAsLastSibling();

            Image background =
                overlayObject.GetComponent<Image>();

            background.color =
                new Color(0.08f, 0.12f, 0.16f, 0.72f);

            background.raycastTarget = false;

            // Icono opcional del candado.
            GameObject iconGO = new GameObject(
                "LockIcon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            RectTransform iconRect =
                iconGO.GetComponent<RectTransform>();

            iconRect.SetParent(overlayRect, false);

            iconRect.anchorMin =
                new Vector2(0.25f, 0.49f);

            iconRect.anchorMax =
                new Vector2(0.75f, 0.94f);

            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            iconImage = iconGO.GetComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;
            iconImage.raycastTarget = false;
            iconImage.sprite = configuredIcon;
            iconImage.enabled = configuredIcon != null;

            // Temporizador.
            GameObject textGO = new GameObject(
                "LockCountdown",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

            RectTransform textRect =
                textGO.GetComponent<RectTransform>();

            textRect.SetParent(overlayRect, false);

            textRect.anchorMin =
                new Vector2(0.05f, 0.1f);

            textRect.anchorMax =
                new Vector2(0.95f, 0.67f);

            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            countdownText =
                textGO.GetComponent<TextMeshProUGUI>();

            countdownText.text = "LOCKED";
            countdownText.fontSize = 30f;
            countdownText.enableAutoSizing = true;
            countdownText.fontSizeMin = 12f;
            countdownText.fontSizeMax = 30f;
            countdownText.alignment =
                TextAlignmentOptions.Center;

            countdownText.color = Color.white;
            countdownText.raycastTarget = false;

            overlayObject.SetActive(false);
        }
    }
}
