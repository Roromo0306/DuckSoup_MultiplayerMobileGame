
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.PowerUps
{
    [DisallowMultipleComponent]
    public sealed class RecipeComboOverlay : MonoBehaviour
    {
        [Header("Optional customization")]
        [SerializeField]
        private Color backgroundColor =
            new Color(0.20f, 0.15f, 0.10f, 0.55f);

        [SerializeField]
        private Color timerColor =
            new Color(1f, 0.59f, 0.20f, 1f);

        [SerializeField, Range(32f, 160f)]
        private float circleSize = 74f;

        [SerializeField, Range(16f, 72f)]
        private float popupFontSize = 32f;

        [Header("Combo Text Animation")]
        [SerializeField]
        private Color comboTextColor =
    new Color32(255, 212, 90, 255);

        [SerializeField, Range(0.5f, 4f)]
        private float comboTextDuration = 1.8f;

        private static Sprite sharedRingSprite;

        private RectTransform timerRoot;
        private Image circleFill;

        private TextMeshProUGUI popupText;
        private RectTransform popupRect;
        private CanvasGroup popupCanvas;
        private Coroutine popupRoutine;

        private bool wasActive;

        private void Awake()
        {
            CreateVisuals();
        }

        private void CreateVisuals()
        {
            if (timerRoot != null)
                return;

            Sprite ringSprite = GetRingSprite();

            GameObject timerGO = new GameObject(
                "ComboCircle",
                typeof(RectTransform));

            timerRoot = timerGO.GetComponent<RectTransform>();
            timerRoot.SetParent(transform, false);

            timerRoot.anchorMin = new Vector2(0.5f, 0.5f);
            timerRoot.anchorMax = new Vector2(0.5f, 0.5f);
            timerRoot.pivot = new Vector2(0.5f, 0.5f);
            timerRoot.sizeDelta = Vector2.one * circleSize;
            timerRoot.anchoredPosition = Vector2.zero;
            timerRoot.localScale = Vector3.one;

            Image background = NewImage(
                "CircleTrack",
                timerRoot);

            background.sprite = ringSprite;
            background.color = backgroundColor;
            background.preserveAspect = true;

            circleFill = NewImage(
                "CircleProgress",
                timerRoot);

            circleFill.sprite = ringSprite;
            circleFill.color = timerColor;
            circleFill.type = Image.Type.Filled;
            circleFill.fillMethod = Image.FillMethod.Radial360;
            circleFill.fillOrigin = 2;
            circleFill.fillClockwise = true;
            circleFill.fillAmount = 1f;

            timerGO.SetActive(false);

            GameObject popupGO = new GameObject(
                "ComboPopup",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI),
                typeof(CanvasGroup));

            popupRect = popupGO.GetComponent<RectTransform>();
            popupRect.SetParent(transform, false);

            popupRect.anchorMin = new Vector2(0.5f, 1f);
            popupRect.anchorMax = new Vector2(0.5f, 1f);
            popupRect.pivot = new Vector2(0.5f, 0.5f);
            popupRect.sizeDelta = new Vector2(190f, 66f);
            popupRect.anchoredPosition = new Vector2(0f, 20f);

            popupText = popupGO.GetComponent<TextMeshProUGUI>();
            popupText.fontSize = popupFontSize;
            popupText.enableAutoSizing = true;
            popupText.fontSizeMin = 14f;
            popupText.fontSizeMax = popupFontSize;
            popupText.fontStyle = FontStyles.Bold;
            popupText.alignment = TextAlignmentOptions.Center;
            popupText.color = comboTextColor;
            popupText.raycastTarget = false;
            popupText.text = "";

            popupCanvas = popupGO.GetComponent<CanvasGroup>();
            popupCanvas.blocksRaycasts = false;
            popupCanvas.interactable = false;
            popupCanvas.alpha = 0f;

            popupGO.SetActive(false);
        }

        private static Image NewImage(
            string name,
            RectTransform parent)
        {
            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            RectTransform rect =
                go.GetComponent<RectTransform>();

            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = go.GetComponent<Image>();
            image.raycastTarget = false;

            return image;
        }

        public void Render(
            bool cardIsComplete,
            float timerFill01)
        {
            if (timerRoot == null)
                CreateVisuals();

            bool show =
                cardIsComplete && timerFill01 > 0f;

            if (show != wasActive)
            {
                timerRoot.gameObject.SetActive(show);
                wasActive = show;
            }

            if (show)
            {
                circleFill.fillAmount =
                    Mathf.Clamp01(timerFill01);
            }
        }

        public void PlayCombo(int multiplier)
        {
            if (multiplier < 2)
                return;

            if (timerRoot == null)
                CreateVisuals();

            if (popupRoutine != null)
                StopCoroutine(popupRoutine);

            popupRoutine = StartCoroutine(
                AnimatePopup(multiplier));
        }

        private IEnumerator AnimatePopup(int multiplier)
        {
            popupText.text = "COMBO x" + multiplier;
            popupRect.gameObject.SetActive(true);
            popupRect.SetAsLastSibling();

            float duration = comboTextDuration;
            float elapsed = 0f;
            Vector2 origin = new Vector2(0f, 20f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(
                    elapsed / duration);

                float punch =
                    1f + 0.40f * Mathf.Sin(
                        Mathf.PI *
                        Mathf.Clamp01(t / 0.48f));

                popupRect.localScale = Vector3.one *
                    Mathf.Lerp(
                        0.65f,
                        punch,
                        Mathf.Clamp01(t * 8f));

                popupRect.anchoredPosition =
                    origin + Vector2.up * (40f * t);

                float fadeIn =
                    Mathf.Clamp01(t / 0.10f);

                float fadeOut =
                    Mathf.Clamp01((1f - t) / 0.18f);

                popupCanvas.alpha =
                    Mathf.Min(fadeIn, fadeOut);

                yield return null;
            }

            popupCanvas.alpha = 0f;
            popupRect.localScale = Vector3.one;
            popupRect.anchoredPosition = origin;
            popupRect.gameObject.SetActive(false);
            popupRoutine = null;
        }

        private void OnDisable()
        {
            if (popupRoutine != null)
                StopCoroutine(popupRoutine);

            popupRoutine = null;
            wasActive = false;

            if (timerRoot != null)
                timerRoot.gameObject.SetActive(false);

            if (popupCanvas != null)
                popupCanvas.alpha = 0f;

            if (popupRect != null)
                popupRect.gameObject.SetActive(false);
        }

        private static Sprite GetRingSprite()
        {
            if (sharedRingSprite != null)
                return sharedRingSprite;

            const int size = 128;
            const float outerRadius = 61f;
            const float innerRadius = 44f;

            Texture2D texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false);

            texture.name = "DuckSoupComboRingRuntime";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - size / 2f;
                    float dy = y + 0.5f - size / 2f;

                    float distance = Mathf.Sqrt(
                        dx * dx + dy * dy);

                    float outer = Mathf.Clamp01(
                        outerRadius + 0.5f - distance);

                    float inner = Mathf.Clamp01(
                        distance - innerRadius + 0.5f);

                    pixels[y * size + x] =
                        new Color(
                            1f,
                            1f,
                            1f,
                            outer * inner);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);

            sharedRingSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);

            sharedRingSprite.name = "DuckSoupComboRing";

            return sharedRingSprite;
        }
    }
}
