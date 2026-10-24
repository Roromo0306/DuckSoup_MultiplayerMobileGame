using System;

namespace DuckSoup.Features.Round.Models
{
    /// <summary>
    /// Representa un token individual de ingrediente.
    /// Controla si está revelado o consumido.
    /// </summary>
    public sealed class IngredientToken
    {
        public string Id { get; }

        public IngredientType Ingredient { get; }

        public bool IsRevealed { get; private set; }

        public bool IsConsumed { get; private set; }

        public IngredientToken(
            string id,
            IngredientType ingredient)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    "El token necesita un ID válido.",
                    nameof(id));
            }

            if (ingredient == IngredientType.None ||
                !Enum.IsDefined(
                    typeof(IngredientType), ingredient))
            {
                throw new ArgumentException(
                    "El ingrediente no es válido.",
                    nameof(ingredient));
            }

            Id = id.Trim();
            Ingredient = ingredient;

            IsRevealed = false;
            IsConsumed = false;
        }

        /// <summary>
        /// Revela un token oculto.
        /// </summary>
        public bool TryReveal()
        {
            if (IsRevealed || IsConsumed)
                return false;

            IsRevealed = true;
            return true;
        }

        /// <summary>
        /// Vuelve a poner boca abajo un token.
        /// Los tokens consumidos no pueden ocultarse.
        /// </summary>
        public bool TryHide()
        {
            if (!IsRevealed || IsConsumed)
                return false;

            IsRevealed = false;
            return true;
        }

        /// <summary>
        /// Consume definitivamente el token.
        /// Solo es válido si está revelado.
        /// </summary>
        public bool TryConsume()
        {
            if (!IsRevealed || IsConsumed)
                return false;

            IsConsumed = true;
            return true;
        }
    }
}