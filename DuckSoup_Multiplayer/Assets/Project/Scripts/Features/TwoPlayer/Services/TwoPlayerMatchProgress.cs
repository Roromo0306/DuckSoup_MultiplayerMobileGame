using System;
using DuckSoup.Features.Menu.Models;

namespace DuckSoup.Features.TwoPlayer.Services
{
    /// <summary>
    /// Datos temporales de una partida 2P formada por dos rondas.
    /// Ronda 1: mesa normal. Ronda 2: mesa de cintas.
    /// No guarda PlayerPrefs ni modifica los scores de TwoPlayerSession.
    /// </summary>
    public static class TwoPlayerMatchProgress
    {
        public static bool IsActive { get; private set; }
        public static GameLaunchRequest Request { get; private set; }
        public static int CurrentRound { get; private set; }
        public static int Player1Total { get; private set; }
        public static int Player2Total { get; private set; }
        public static bool IsComplete { get; private set; }

        public static void Begin(GameLaunchRequest request)
        {
            if (request == null || request.Mode != GameMode.TwoPlayers)
                throw new ArgumentException("Se requiere una peticion 2P valida.", nameof(request));

            Request = request;
            IsActive = true;
            CurrentRound = 1;
            Player1Total = 0;
            Player2Total = 0;
            IsComplete = false;
        }

        /// <summary>
        /// Aumenta el acumulado exactamente una vez por ronda.
        /// Devuelve true si ahora toca empezar la segunda ronda.
        /// </summary>
        public static bool CompleteCurrentRound(int player1, int player2)
        {
            if (!IsActive || IsComplete)
                throw new InvalidOperationException("No hay ronda activa para cerrar.");
            if (player1 < 0 || player2 < 0)
                throw new ArgumentOutOfRangeException("Las puntuaciones no pueden ser negativas.");

            Player1Total = checked(Player1Total + player1);
            Player2Total = checked(Player2Total + player2);

            if (CurrentRound == 1)
            {
                CurrentRound = 2;
                return true;
            }

            IsComplete = true;
            return false;
        }

        public static void Clear()
        {
            IsActive = false;
            Request = null;
            CurrentRound = 0;
            Player1Total = 0;
            Player2Total = 0;
            IsComplete = false;
        }
    }
}
