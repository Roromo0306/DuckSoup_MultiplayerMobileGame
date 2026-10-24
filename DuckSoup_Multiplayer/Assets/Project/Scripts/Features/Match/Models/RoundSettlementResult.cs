
using DuckSoup.Domain;

namespace DuckSoup.Features.Match.Models
{
    /// <summary>
    /// Estado económico de un jugador
    /// después de liquidar una ronda.
    /// </summary>
    public sealed class PlayerEconomySnapshot
    {
        public PlayerId Id { get; }
        public int EarnedThisRound { get; }
        public int TotalCoins { get; }
        public int UpgradeLevel { get; }
        public int NextUpgradeAt { get; }

        public PlayerEconomySnapshot(
            PlayerId id,
            int earned,
            int total,
            int level,
            int nextUpgradeAt)
        {
            Id = id;
            EarnedThisRound = earned;
            TotalCoins = total;
            UpgradeLevel = level;
            NextUpgradeAt = nextUpgradeAt;
        }
    }

    /// <summary>
    /// Resultado definitivo de la liquidación.
    /// Es un objeto de datos de solo lectura.
    /// </summary>
    public sealed class RoundSettlementResult
    {
        public string RoundId { get; }
        public int RoundNumber { get; }

        public PlayerId RoundWinner { get; }
        public PlayerId MatchWinner { get; }

        public bool MatchFinished { get; }
        public int MaxUpgradeLevel { get; }

        public PlayerEconomySnapshot Player1 { get; }
        public PlayerEconomySnapshot Player2 { get; }

        public RoundSettlementResult(
            string roundId,
            int roundNumber,
            PlayerId roundWinner,
            PlayerId matchWinner,
            bool matchFinished,
            int maxUpgradeLevel,
            PlayerEconomySnapshot player1,
            PlayerEconomySnapshot player2)
        {
            RoundId = roundId;
            RoundNumber = roundNumber;
            RoundWinner = roundWinner;
            MatchWinner = matchWinner;
            MatchFinished = matchFinished;
            MaxUpgradeLevel = maxUpgradeLevel;
            Player1 = player1;
            Player2 = player2;
        }
    }
}
