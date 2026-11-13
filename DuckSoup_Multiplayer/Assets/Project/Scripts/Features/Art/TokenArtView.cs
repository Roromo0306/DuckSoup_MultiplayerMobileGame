
using DuckSoup.Features.PowerUps;
using DuckSoup.Features.Round.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.Art
{
    [DisallowMultipleComponent]
    public sealed class TokenArtView : MonoBehaviour
    {
        [Header("Ingredient Art Library")]
        [SerializeField] private IngredientArtLibrary art;

        [Header("Token Images")]
        [SerializeField] private Image tokenBaseImage;
        [SerializeField] private Image ingredientImage;

        [Header("Power-up FRONT sprites")]
        [SerializeField] private Sprite wildcardDuck;
        [SerializeField] private Sprite poison;
        [SerializeField] private Sprite completeRecipe;
        [SerializeField] private Sprite extraTime;

        // Solo existe una capa independiente cuando
        // las dos referencias apuntan a Images diferentes.
        private bool HasSeparateIngredientImage
        {
            get
            {
                return ingredientImage != null &&
                       ingredientImage != tokenBaseImage;
            }
        }

        public bool IsConfigured
        {
            get { return art != null; }
        }

        private void Awake()
        {
            if (tokenBaseImage == null)
            {
                tokenBaseImage = GetComponent<Image>();
            }

            // IMPORTANTE:
            // Nunca desactivar el Raycast Target de
            // tokenBaseImage. Los TokenView lo controlan
            // para permitir el arrastre multitactil.
            if (HasSeparateIngredientImage)
            {
                ingredientImage.raycastTarget = false;
            }
        }

        // Compatibilidad con el sistema original.
        public void Render(IngredientTokenSnapshot token)
        {
            Render(token, PowerTokenKind.None);
        }

        // Compatible con ingredientes y power-ups.
        public void Render(
            IngredientTokenSnapshot token,
            PowerTokenKind kind)
        {
            if (tokenBaseImage == null)
            {
                tokenBaseImage = GetComponent<Image>();
            }

            if (tokenBaseImage == null || art == null)
            {
                return;
            }

            bool exists =
                token != null && !token.IsConsumed;

            // Apagamos solamente la capa secundaria,
            // nunca la Image principal.
            HideIngredientOverlay();

            tokenBaseImage.enabled = exists;

            if (!exists)
            {
                return;
            }

            tokenBaseImage.color = Color.white;
            tokenBaseImage.preserveAspect = true;

            // 1. TOKEN BOCA ABAJO
            // Todos usan la misma imagen trasera.
            if (!token.IsRevealed)
            {
                tokenBaseImage.sprite = art.TokenBack;
                return;
            }

            // 2. TOKEN ESPECIAL REVELADO
            if (kind != PowerTokenKind.None)
            {
                Sprite specialSprite = GetPowerUpSprite(kind);

                if (specialSprite != null)
                {
                    tokenBaseImage.sprite = specialSprite;
                }
                else
                {
                    tokenBaseImage.sprite = art.TokenBack;

                    Debug.LogWarning(
                        "[TokenArtView] Falta el sprite de " +
                        kind,
                        this);
                }

                return;
            }

            // 3. INGREDIENTE NORMAL REVELADO
            Sprite ingredientSprite =
                art.Get(token.VisibleIngredient);

            // Si tenemos una Image secundaria y un
            // fondo frontal configurado, usamos ambas.
            if (HasSeparateIngredientImage &&
                art.TokenFrontBackground != null)
            {
                tokenBaseImage.sprite =
                    art.TokenFrontBackground;

                ingredientImage.sprite = ingredientSprite;
                ingredientImage.color = Color.white;
                ingredientImage.preserveAspect = true;

                ingredientImage.enabled =
                    ingredientSprite != null;
            }
            else
            {
                // CONFIGURACION DE DUCK SOUP:
                // El ingrediente ocupa la Image principal.
                // Sin fondo frontal adicional.
                //
                // Funciona aunque Ingredient Image
                // apunte a la misma Image que Token Base.
                tokenBaseImage.sprite =
                    ingredientSprite != null
                        ? ingredientSprite
                        : art.TokenBack;
            }

            if (ingredientSprite == null)
            {
                Debug.LogWarning(
                    "[TokenArtView] No hay sprite para " +
                    token.VisibleIngredient,
                    this);
            }
        }

        private void HideIngredientOverlay()
        {
            if (!HasSeparateIngredientImage)
            {
                return;
            }

            ingredientImage.enabled = false;
        }

        private Sprite GetPowerUpSprite(PowerTokenKind kind)
        {
            switch (kind)
            {
                case PowerTokenKind.WildcardDuck:
                    return wildcardDuck;

                case PowerTokenKind.Poison:
                    return poison;

                case PowerTokenKind.CompleteRecipe:
                    return completeRecipe;

                case PowerTokenKind.ExtraComboTime:
                    return extraTime;

                default:
                    return null;
            }
        }
    }
}
