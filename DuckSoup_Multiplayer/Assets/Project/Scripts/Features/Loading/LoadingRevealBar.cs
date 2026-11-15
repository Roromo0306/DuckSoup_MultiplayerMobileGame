
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.Loading
{
    public sealed class LoadingRevealBar : MonoBehaviour
    {
        [Header("Loading Bar")]
        [SerializeField] private Image fillImage;

        private float progress;

        public float Progress => progress;

        private void Awake()
        {
            if (fillImage == null)
            {
                Debug.LogError(
                    "[LoadingRevealBar] Assign Fill Image.",
                    this);

                return;
            }

            // Configurar el relleno horizontal.
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;

            // Empieza por la izquierda.
            fillImage.fillOrigin =
                (int)Image.OriginHorizontal.Left;

            // No se modifica el tamaño del sprite.
            SetProgress(0f);
        }

        public void SetProgress(float value)
        {
            progress = Mathf.Clamp01(value);

            if (fillImage == null)
                return;

            fillImage.fillAmount = progress;
        }
    }
}
