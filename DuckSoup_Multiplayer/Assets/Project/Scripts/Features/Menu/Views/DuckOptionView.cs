
using System;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.Menu.Views
{
    /// <summary>
    /// Representa una opción seleccionable de patito.
    /// </summary>
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public sealed class DuckOptionView : MonoBehaviour
    {
        [Header("Portrait")]
        [SerializeField]
        private UnityEngine.UI.Image duckPortrait;

        [Header("Selection Colors")]
        [SerializeField]
        private Color normalColor = Color.white;

        [SerializeField]
        private Color selectedColor =
            new Color(1f, 0.80f, 0.30f, 1f);

        private Button button;
        private Action clickAction;

        private void EnsureReferences()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        /// <summary>
        /// Configura el patito y su acción.
        /// </summary>
        public void Configure(
            Sprite duckSprite,
            Action onClicked)
        {
            EnsureReferences();

            if (duckPortrait == null)
            {
                throw new InvalidOperationException(
                    "Falta Duck Portrait en el Inspector.");
            }

            button.onClick.RemoveListener(
                HandleClicked);

            clickAction = onClicked ??
                throw new ArgumentNullException(
                    nameof(onClicked));

            duckPortrait.sprite = duckSprite;

            duckPortrait.enabled = duckSprite != null;

            // El estado visual se controla manualmente.
            button.transition =
                Selectable.Transition.None;

            button.onClick.AddListener(
                HandleClicked);

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            EnsureReferences();

            if (button.image != null)
            {
                button.image.color = selected
                    ? selectedColor
                    : normalColor;
            }
        }

        private void HandleClicked()
        {
            clickAction?.Invoke();
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(
                    HandleClicked);
            }
        }
    }
}
