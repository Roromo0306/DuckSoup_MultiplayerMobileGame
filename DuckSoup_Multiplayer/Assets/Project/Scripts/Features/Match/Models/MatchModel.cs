
using System;
using DuckSoup.Domain;

namespace DuckSoup.Features.Match.Models
{
    /// <summary>
    /// Progreso económico de un jugador.
    /// </summary>
    public sealed class PlayerMatchProgress
    {
        public PlayerId Id { get; }

        public int TotalCoins { get; private set; }

        public PlayerMatchProgress(PlayerId id)
        {
            if (id != PlayerId.Player1 &&
                id != PlayerId.Player2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(id));
            }

            Id = id;
        }

        internal void AddCoins(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(amount));

            TotalCoins = checked(TotalCoins + amount);
        }
    }

    /// <summary>
    /// Estado acumulado de una partida.
    /// Se mantiene entre rondas.
    /// </summary>
    public sealed class MatchModel
    {
        public string Id { get; }

        public PlayerMatchProgress Player1 { get; }

        public PlayerMatchProgress Player2 { get; }

        public int CompletedRounds { get; private set; }

        public bool IsFinished { get; private set; }

        public PlayerId Winner { get; private set; }

        public MatchModel(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException(
                    "La partida necesita un ID.",
                    nameof(id));

            Id = id.Trim();

            Player1 = new PlayerMatchProgress(
                PlayerId.Player1);

            Player2 = new PlayerMatchProgress(
                PlayerId.Player2);

            Winner = PlayerId.None;
        }

        internal void RegisterSettlement(
            bool isFinished,
            PlayerId winner)
        {
            if (IsFinished)
                throw new InvalidOperationException(
                    "La partida ya ha terminado.");

            CompletedRounds++;

            IsFinished = isFinished;

            Winner = isFinished
                ? winner
                : PlayerId.None;
        }
    }
}
