using DuckSoup.Features.PowerUps;
using DuckSoup.Features.Round.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.Art
{
    // Both ingredients and power-ups are drawn by the SAME existing token prefab.
    [DisallowMultipleComponent]
    public sealed class TokenArtView : MonoBehaviour
    {
        [SerializeField] private IngredientArtLibrary art;
        [SerializeField] private Image tokenBaseImage;
        [SerializeField] private Image ingredientImage;

        [Header("Power-up FRONT sprites (only required on gameplay token prefabs)")]
        [SerializeField] private Sprite wildcardDuck;
        [SerializeField] private Sprite poison;
        [SerializeField] private Sprite completeRecipe;
        [SerializeField] private Sprite extraTime;

        private void Awake()
        {
            if (tokenBaseImage == null) tokenBaseImage = GetComponent<Image>();
            if (ingredientImage != null) ingredientImage.raycastTarget = false;
        }

        public bool IsConfigured { get { return art != null; } }

        public void Render(IngredientTokenSnapshot token)
        {
            Render(token, PowerTokenKind.None);
        }

        public void Render(IngredientTokenSnapshot token, PowerTokenKind kind)
        {
            if (tokenBaseImage == null || art == null) return;
            bool visible = token != null && !token.IsConsumed;
            tokenBaseImage.enabled = visible;
            if (ingredientImage != null) ingredientImage.enabled = false;
            if (!visible) return;

            tokenBaseImage.color = Color.white;
            bool revealed = token.IsRevealed;
            if (revealed && kind != PowerTokenKind.None)
            {
                tokenBaseImage.sprite = SpecialSprite(kind);
                tokenBaseImage.preserveAspect = true;
                if (tokenBaseImage.sprite == null)
                    Debug.LogWarning("Missing power-up Sprite: " + kind, this);
                return;
            }

            tokenBaseImage.sprite = revealed && art.TokenFrontBackground != null
                ? art.TokenFrontBackground : art.TokenBack;
            if (ingredientImage != null && revealed)
            {
                ingredientImage.sprite = art.Get(token.VisibleIngredient);
                ingredientImage.color = Color.white;
                ingredientImage.preserveAspect = true;
                ingredientImage.enabled = ingredientImage.sprite != null;
            }
        }

        private Sprite SpecialSprite(PowerTokenKind kind)
        {
            switch (kind)
            {
                case PowerTokenKind.WildcardDuck: return wildcardDuck;
                case PowerTokenKind.Poison: return poison;
                case PowerTokenKind.CompleteRecipe: return completeRecipe;
                case PowerTokenKind.ExtraComboTime: return extraTime;
                default: return null;
            }
        }
    }
}
