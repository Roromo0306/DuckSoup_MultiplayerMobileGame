
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.GameFeel.Menu
{
    [DisallowMultipleComponent]
    public sealed class MenuInfiniteBackground : MonoBehaviour
    {
        [Header("REFERENCES")]

        [Tooltip("Zona que ocupa el fondo animado.")]
        [SerializeField]
        private RectTransform scrollArea;

        [Tooltip("Imagen original que queremos mover.")]
        [SerializeField]
        private Image originalImage;

        [Header("MOVEMENT")]

        [Tooltip("Velocidad en unidades de UI por segundo.")]
        [SerializeField, Min(0f)]
        private float scrollSpeed = 25f;

        [Tooltip("Bajar o subir.")]
        [SerializeField]
        private bool moveDown = true;

        [Tooltip("Continuar aunque Time.timeScale sea 0.")]
        [SerializeField]
        private bool useUnscaledTime = true;

        private Image duplicateImage;

        private RectTransform firstRect;
        private RectTransform secondRect;

        private float scrollOffset;
        private float currentHeight;

        private bool initialized;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (initialized)
                return;

            if (scrollArea == null)
            {
                scrollArea = transform as RectTransform;
            }

            if (scrollArea == null ||
                originalImage == null)
            {
                Debug.LogError(
                    "[MenuInfiniteBackground] " +
                    "Asigna Scroll Area y Original Image.",
                    this);

                return;
            }

            // La imagen original debe estar
            // dentro del area de movimiento.
            if (originalImage.transform.parent != scrollArea)
            {
                Debug.LogError(
                    "[MenuInfiniteBackground] " +
                    "Original Image debe ser hija " +
                    "directa de Scroll Area.",
                    this);

                return;
            }

            // Recortar todo lo que salga
            // de la zona del fondo.
            RectMask2D mask =
                scrollArea.GetComponent<RectMask2D>();

            if (mask == null)
            {
                mask = scrollArea.gameObject
                    .AddComponent<RectMask2D>();
            }

            mask.enabled = true;

            // Primera imagen.
            firstRect =
                originalImage.rectTransform;

            ConfigureImage(originalImage);

            // Segunda imagen: se crea
            // automaticamente con el mismo sprite.
            GameObject copyObject = new GameObject(
                "BackgroundLoopCopy",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            copyObject.transform.SetParent(
                scrollArea, false);

            duplicateImage =
                copyObject.GetComponent<Image>();

            duplicateImage.sprite =
                originalImage.sprite;

            duplicateImage.color =
                originalImage.color;

            duplicateImage.material =
                originalImage.material;

            ConfigureImage(duplicateImage);

            secondRect =
                duplicateImage.rectTransform;

            // Mantener ambas dentro del
            // contenedor de fondo.
            firstRect.SetSiblingIndex(0);
            secondRect.SetSiblingIndex(1);

            Canvas.ForceUpdateCanvases();

            currentHeight = scrollArea.rect.height;
            scrollOffset = 0f;

            UpdateImageSizes();
            UpdatePositions();

            initialized = true;
        }

        private void ConfigureImage(Image image)
        {
            RectTransform rect =
                image.rectTransform;

            // Ocupar todo el ancho.
            // La altura se calcula despues.
            rect.anchorMin =
                new Vector2(0f, 1f);

            rect.anchorMax =
                new Vector2(1f, 1f);

            // Origen arriba.
            rect.pivot =
                new Vector2(0.5f, 1f);

            rect.localScale =
                Vector3.one;

            rect.localRotation =
                Quaternion.identity;

            // No interceptar botones del menu.
            image.raycastTarget = false;

            // No utilizar Filled ni Tiled.
            image.type = Image.Type.Simple;

            image.preserveAspect = false;
        }

        private void UpdateImageSizes()
        {
            if (firstRect == null ||
                secondRect == null)
                return;

            // Cada imagen mide exactamente
            // lo mismo que la zona visible.
            firstRect.sizeDelta =
                new Vector2(0f, currentHeight);

            secondRect.sizeDelta =
                new Vector2(0f, currentHeight);
        }

        private void Update()
        {
            if (!initialized)
                return;

            float height =
                scrollArea.rect.height;

            if (height <= 0f)
                return;

            // Adaptarse si cambia el tamaño
            // del Canvas o la pantalla.
            if (!Mathf.Approximately(
                height, currentHeight))
            {
                float normalizedOffset =
                    currentHeight > 0f
                        ? scrollOffset / currentHeight
                        : 0f;

                currentHeight = height;

                scrollOffset =
                    normalizedOffset * currentHeight;

                UpdateImageSizes();
            }

            float deltaTime = useUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

            scrollOffset += scrollSpeed * deltaTime;

            // Reinicia el desplazamiento cada vez
            // que completa la altura de una imagen.
            //
            // Como ambas imagenes son identicas,
            // el cambio es continuo.
            scrollOffset =
                Mathf.Repeat(
                    scrollOffset,
                    currentHeight);

            UpdatePositions();
        }

        private void UpdatePositions()
        {
            if (firstRect == null ||
                secondRect == null)
                return;

            if (moveDown)
            {
                // Imagen principal:
                // comienza visible y baja.
                firstRect.anchoredPosition =
                    new Vector2(
                        0f,
                        -scrollOffset);

                // Segunda imagen:
                // comienza fuera por arriba
                // y entra cuando la primera baja.
                secondRect.anchoredPosition =
                    new Vector2(
                        0f,
                        currentHeight - scrollOffset);
            }
            else
            {
                // Movimiento en sentido contrario.
                firstRect.anchoredPosition =
                    new Vector2(
                        0f,
                        scrollOffset);

                secondRect.anchoredPosition =
                    new Vector2(
                        0f,
                        scrollOffset - currentHeight);
            }
        }

        private void OnEnable()
        {
            scrollOffset = 0f;

            if (initialized)
                UpdatePositions();
        }

        [ContextMenu("Restart Background Movement")]
        private void RestartMovement()
        {
            scrollOffset = 0f;
            UpdatePositions();
        }
    }
}
