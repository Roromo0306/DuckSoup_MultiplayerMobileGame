using System;
using System.Collections.Generic;
using DuckSoup.Features.Round.Contracts;

namespace DuckSoup.Features.TwoPlayer.Models
{
    /// <summary>Datos de lectura del modo 2P. Se permite null en Tokens para huecos vacios.</summary>
    public sealed class TwoPlayerSnapshot
    {
        public IReadOnlyList<RecipeCardSnapshot> Player1Cards { get; }
        public IReadOnlyList<RecipeCardSnapshot> Player2Cards { get; }
        public IReadOnlyList<IngredientTokenSnapshot> Tokens { get; }
        public int Player1Score { get; }
        public int Player2Score { get; }
        public float RemainingSeconds { get; }
        public bool IsTimeUp { get; }

        public TwoPlayerSnapshot(
            IEnumerable<RecipeCardSnapshot> player1Cards,
            IEnumerable<RecipeCardSnapshot> player2Cards,
            IEnumerable<IngredientTokenSnapshot> tokens,
            int player1Score, int player2Score,
            float remainingSeconds, bool isTimeUp)
        {
            if (player1Cards == null) throw new ArgumentNullException(nameof(player1Cards));
            if (player2Cards == null) throw new ArgumentNullException(nameof(player2Cards));
            if (tokens == null) throw new ArgumentNullException(nameof(tokens));

            Player1Cards = new List<RecipeCardSnapshot>(player1Cards).AsReadOnly();
            Player2Cards = new List<RecipeCardSnapshot>(player2Cards).AsReadOnly();
            Tokens = new List<IngredientTokenSnapshot>(tokens).AsReadOnly();
            Player1Score = player1Score;
            Player2Score = player2Score;
            RemainingSeconds = remainingSeconds;
            IsTimeUp = isTimeUp;
        }
    }
}
