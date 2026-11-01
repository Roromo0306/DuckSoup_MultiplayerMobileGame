
using DuckSoup.Features.Round.Models;
using UnityEngine;

namespace DuckSoup.Features.Art
{
    [CreateAssetMenu(
        fileName = "DuckSoupIngredientArt",
        menuName = "Duck Soup/Ingredient Art Library")]
    public sealed class IngredientArtLibrary : ScriptableObject
    {
        [Header("Tokens")]
        [SerializeField] private Sprite tokenBack;
        [SerializeField] private Sprite tokenFrontBackground;

        [Header("Token ingredient sprites")]
        [SerializeField] private Sprite carrot;
        [SerializeField] private Sprite potato;
        [SerializeField] private Sprite onion;
        [SerializeField] private Sprite mushroom;
        [SerializeField] private Sprite tomato;
        [SerializeField] private Sprite pumpkin;
        [SerializeField] private Sprite corn;
        [SerializeField] private Sprite leek;

        [Header("Recipe card ingredient sprites - separate from tokens")]
        [SerializeField] private Sprite recipeCarrot;
        [SerializeField] private Sprite recipePotato;
        [SerializeField] private Sprite recipeOnion;
        [SerializeField] private Sprite recipeMushroom;
        [SerializeField] private Sprite recipeTomato;
        [SerializeField] private Sprite recipePumpkin;
        [SerializeField] private Sprite recipeCorn;
        [SerializeField] private Sprite recipeLeek;

        public Sprite TokenBack => tokenBack;
        public Sprite TokenFrontBackground => tokenFrontBackground;

        // Sprites que utilizan los tokens.
        public Sprite Get(IngredientType type)
        {
            switch (type)
            {
                case IngredientType.Carrot: return carrot;
                case IngredientType.Potato: return potato;
                case IngredientType.Onion: return onion;
                case IngredientType.Mushroom: return mushroom;
                case IngredientType.Tomato: return tomato;
                case IngredientType.Pumpkin: return pumpkin;
                case IngredientType.Corn: return corn;
                case IngredientType.Leek: return leek;
                default: return null;
            }
        }

        // Sprites exclusivos de las cartas.
        public Sprite GetRecipe(IngredientType type)
        {
            Sprite result;

            switch (type)
            {
                case IngredientType.Carrot:
                    result = recipeCarrot;
                    break;
                case IngredientType.Potato:
                    result = recipePotato;
                    break;
                case IngredientType.Onion:
                    result = recipeOnion;
                    break;
                case IngredientType.Mushroom:
                    result = recipeMushroom;
                    break;
                case IngredientType.Tomato:
                    result = recipeTomato;
                    break;
                case IngredientType.Pumpkin:
                    result = recipePumpkin;
                    break;
                case IngredientType.Corn:
                    result = recipeCorn;
                    break;
                case IngredientType.Leek:
                    result = recipeLeek;
                    break;
                default:
                    result = null;
                    break;
            }

            // Si falta una imagen de carta,
            // usamos provisionalmente la del token.
            return result != null ? result : Get(type);
        }
    }
}
