
using DuckSoup.Features.Round.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.Art
{
    [DisallowMultipleComponent]
    public sealed class TokenArtView : MonoBehaviour
    {
        [Header("Ingredient Art")]
        [SerializeField]
        private IngredientArtLibrary art;

        [Header("Token Image")]
        [SerializeField]
        private Image tokenBaseImage;

        // Solo para mantener compatibilidad
        // con prefabs anteriores.
        // Debe quedarse sin asignar.
        [SerializeField]
        private Image ingredientImage;

        public bool IsConfigured => art != null;

        private void Awake()
        {
            if (tokenBaseImage == null)
            {
                tokenBaseImage = GetComponent<Image>();
            }

            if (ingredientImage != null)
            {
                ingredientImage.enabled = false;
                ingredientImage.raycastTarget = false;
            }
        }

        public void Render(IngredientTokenSnapshot token)
        {
            if (art == null || tokenBaseImage == null)
                return;

            // Desactivamos cualquier imagen antigua.
            if (ingredientImage != null)
            {
                ingredientImage.enabled = false;
            }

            // Si el espacio está vacío,
            // no mostramos ninguna ficha.
            if (token == null || token.IsConsumed)
            {
                tokenBaseImage.enabled = false;
                return;
            }

            tokenBaseImage.enabled = true;
            tokenBaseImage.color = Color.white;
            tokenBaseImage.preserveAspect = true;

            if (token.IsRevealed)
            {
                // La cara delantera ES el ingrediente.
                tokenBaseImage.sprite =
                    art.Get(token.VisibleIngredient);
            }
            else
            {
                // Reverso común para todos.
                tokenBaseImage.sprite = art.TokenBack;
            }
        }
    }
}
