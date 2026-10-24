
using System;
using System.Text;
using DuckSoup.Domain;
using DuckSoup.Features.Round.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.Round.Views
{
    [RequireComponent(typeof(Image))]
    public sealed class RecipeCardView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI recipeText;

        private RectTransform cardRect;
        private Image backgroundImage;
        private Color originalColor;

        public string CardId { get; private set; }

        public PlayerId Owner { get; private set; }

        private void Awake()
        {
            cardRect = GetComponent<RectTransform>();
            backgroundImage = GetComponent<Image>();

            originalColor = backgroundImage.color;
        }

        public void Bind(
            RecipeCardSnapshot snapshot,
            PlayerId owner)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            if (recipeText == null)
                throw new InvalidOperationException(
                    "Falta RecipeText.");

            CardId = snapshot.CardId;
            Owner = owner;

            if (snapshot.IsComplete)
            {
                recipeText.text =
                    "✓\nRECETA COMPLETADA";

                backgroundImage.color =
                    new Color(0.55f, 0.82f, 0.58f);

                return;
            }

            backgroundImage.color = originalColor;

            var builder = new StringBuilder();

            builder.AppendLine(snapshot.DisplayName);
            builder.AppendLine();

            foreach (IngredientRequirementSnapshot ingredient
                     in snapshot.Requirements)
            {
                builder.Append(
                    ingredient.IsComplete ? "✓ " : "○ ");

                builder.Append(ingredient.Ingredient);
                builder.Append(" ");

                builder.Append(ingredient.CompletedCount);
                builder.Append("/");
                builder.Append(ingredient.RequiredCount);

                builder.AppendLine();
            }

            recipeText.text = builder.ToString();
        }

        /// <summary>
        /// Comprueba si el dedo se ha soltado
        /// dentro de esta carta.
        ///
        /// Funciona también con zonas rotadas.
        /// </summary>
        public bool ContainsScreenPoint(
            Vector2 screenPoint,
            Camera eventCamera)
        {
            return RectTransformUtility
                .RectangleContainsScreenPoint(
                    cardRect,
                    screenPoint,
                    eventCamera);
        }
    }
}
