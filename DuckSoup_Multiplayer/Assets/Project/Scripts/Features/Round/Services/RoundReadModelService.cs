
using System;
using System.Collections.Generic;
using DuckSoup.Features.Round.Contracts;
using DuckSoup.Features.Round.Models;

namespace DuckSoup.Features.Round.Services
{
    /// <summary>
    /// Servicio encargado de transformar
    /// el estado de la ronda en datos de lectura.
    ///
    /// No modifica el progreso de las recetas
    /// ni consume ingredientes.
    /// </summary>
    public sealed class RoundReadModelService : IRoundReadModel
    {
        private readonly RoundModel round;

        public RoundReadModelService(RoundModel round)
        {
            this.round = round ??
                throw new ArgumentNullException(nameof(round));
        }

        public RoundSnapshot GetSnapshot()
        {
            RoundPlayerSnapshot player1 =
                CreatePlayerSnapshot(round.Player1);

            RoundPlayerSnapshot player2 =
                CreatePlayerSnapshot(round.Player2);

            var tokens =
                new List<IngredientTokenSnapshot>();

            foreach (IngredientToken token in round.Tokens)
            {
                tokens.Add(new IngredientTokenSnapshot(
                    token.Id,
                    token.Ingredient,
                    token.IsRevealed,
                    token.IsConsumed));
            }

            return new RoundSnapshot(
                round.Id,
                player1,
                player2,
                tokens);
        }

        private static RoundPlayerSnapshot CreatePlayerSnapshot(
            RoundPlayerModel player)
        {
            var cards = new List<RecipeCardSnapshot>();

            foreach (RecipeCardModel card in player.Cards)
            {
                cards.Add(CreateCardSnapshot(card));
            }

            return new RoundPlayerSnapshot(
                player.Id,
                cards);
        }

        private static RecipeCardSnapshot CreateCardSnapshot(
            RecipeCardModel card)
        {
            var requirements =
                new List<IngredientRequirementSnapshot>();

            // Evita crear filas duplicadas cuando
            // una receta necesita dos unidades
            // del mismo ingrediente.
            var visited = new HashSet<IngredientType>();

            foreach (IngredientType ingredient in
                     card.Definition.RequiredIngredients)
            {
                if (!visited.Add(ingredient))
                    continue;

                int required =
                    card.Progress.GetRequiredCount(ingredient);

                int completed =
                    card.Progress.GetCompletedCount(ingredient);

                requirements.Add(
                    new IngredientRequirementSnapshot(
                        ingredient,
                        required,
                        completed));
            }

            return new RecipeCardSnapshot(
                card.CardId,
                card.Definition.Id,
                card.Definition.DisplayName,
                requirements);
        }
    }
}
