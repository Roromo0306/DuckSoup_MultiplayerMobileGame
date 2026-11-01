using DuckSoup.Features.Round.Models;
using UnityEngine;

namespace DuckSoup.Features.Art
{
    [CreateAssetMenu(fileName = "DuckSoupIngredientArt", menuName = "Duck Soup/Ingredient Art Library")]
    public sealed class IngredientArtLibrary : ScriptableObject
    {
        [Header("Token card sides")]
        [SerializeField] private Sprite tokenBack;
        [SerializeField] private Sprite tokenFrontBackground;
        [Header("The eight ingredient sprites")]
        [SerializeField] private Sprite carrot;
        [SerializeField] private Sprite potato;
        [SerializeField] private Sprite onion;
        [SerializeField] private Sprite mushroom;
        [SerializeField] private Sprite tomato;
        [SerializeField] private Sprite pumpkin;
        [SerializeField] private Sprite corn;
        [SerializeField] private Sprite leek;

        public Sprite TokenBack => tokenBack;
        public Sprite TokenFrontBackground => tokenFrontBackground;

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
    }
}
