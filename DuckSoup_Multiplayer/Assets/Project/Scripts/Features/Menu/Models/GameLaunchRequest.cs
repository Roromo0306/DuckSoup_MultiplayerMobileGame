
using System;

namespace DuckSoup.Features.Menu.Models
{
    /// <summary>
    /// Configuración necesaria para iniciar una partida.
    ///
    /// No contiene componentes de Unity.
    /// </summary>
    public sealed class GameLaunchRequest
    {
        public const int AvailableDucks = 4;

        public GameMode Mode { get; }

        public string TableId { get; }

        public int Player1DuckIndex { get; }

        public int Player2DuckIndex { get; }

        private GameLaunchRequest(
            GameMode mode,
            string tableId,
            int player1DuckIndex,
            int player2DuckIndex)
        {
            Mode = mode;
            TableId = tableId;

            Player1DuckIndex = player1DuckIndex;
            Player2DuckIndex = player2DuckIndex;
        }

        /// <summary>
        /// Configuración del modo individual.
        /// No necesita selección de personajes.
        /// </summary>
        public static GameLaunchRequest ForSinglePlayer(
            string tableId)
        {
            return new GameLaunchRequest(
                GameMode.SinglePlayer,
                ValidateTableId(tableId),
                -1,
                -1);
        }

        /// <summary>
        /// Configuración del modo de dos jugadores.
        /// Los índices indican qué patito seleccionó
        /// cada participante.
        /// </summary>
        public static GameLaunchRequest ForTwoPlayers(
            string tableId,
            int player1DuckIndex,
            int player2DuckIndex)
        {
            ValidateDuckIndex(player1DuckIndex);
            ValidateDuckIndex(player2DuckIndex);

            return new GameLaunchRequest(
                GameMode.TwoPlayers,
                ValidateTableId(tableId),
                player1DuckIndex,
                player2DuckIndex);
        }

        private static string ValidateTableId(string tableId)
        {
            if (string.IsNullOrWhiteSpace(tableId))
            {
                throw new ArgumentException(
                    "La mesa necesita un ID válido.",
                    nameof(tableId));
            }

            return tableId.Trim();
        }

        private static void ValidateDuckIndex(int duckIndex)
        {
            if (duckIndex < 0 ||
                duckIndex >= AvailableDucks)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(duckIndex),
                    "El personaje seleccionado no existe.");
            }
        }
    }
}
