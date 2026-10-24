
using System;
using DuckSoup.Features.Menu.Models;

namespace DuckSoup.Features.Menu.Services
{
    /// <summary>
    /// Transfiere la configuración de una partida
    /// entre escenas durante la ejecución.
    ///
    /// No es un sistema de guardado permanente.
    /// </summary>
    public static class GameLaunchHandoff
    {
        private static GameLaunchRequest pendingRequest;

        public static void Prepare(GameLaunchRequest request)
        {
            pendingRequest = request ??
                throw new ArgumentNullException(
                    nameof(request));
        }

        /// <summary>
        /// Recupera la configuración una sola vez.
        /// </summary>
        public static bool TryConsume(
            out GameLaunchRequest request)
        {
            request = pendingRequest;
            pendingRequest = null;

            return request != null;
        }

        public static void Clear()
        {
            pendingRequest = null;
        }
    }
}
