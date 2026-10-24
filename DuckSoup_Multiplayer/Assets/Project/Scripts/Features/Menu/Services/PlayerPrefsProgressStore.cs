
using System;
using DuckSoup.Features.Menu.Contracts;
using UnityEngine;

namespace DuckSoup.Features.Menu.Services
{
    /// <summary>
    /// Guarda los resultados individuales
    /// mediante PlayerPrefs.
    /// </summary>
    public sealed class PlayerPrefsProgressStore :
        IPlayerProgressStore
    {
        private const string TotalRecipesKey =
            "DuckSoup_1P_TotalRecipes";

        private const string HighScorePrefix =
            "DuckSoup_1P_HighScore_";

        public int GetTotalRecipesCompleted()
        {
            return PlayerPrefs.GetInt(
                TotalRecipesKey, 0);
        }

        public int GetTableHighScore(string tableId)
        {
            return PlayerPrefs.GetInt(
                GetHighScoreKey(tableId), 0);
        }

        /// <summary>
        /// Se llamará una única vez al finalizar
        /// una partida individual.
        ///
        /// Aumenta el total de recetas y guarda
        /// el mejor resultado de la mesa.
        /// </summary>
        public void RegisterSinglePlayerRun(
            string tableId,
            int completedRecipes)
        {
            string highScoreKey =
                GetHighScoreKey(tableId);

            if (completedRecipes < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedRecipes));
            }

            int previousTotal =
                GetTotalRecipesCompleted();

            int newTotal = checked(
                previousTotal + completedRecipes);

            int previousHighScore =
                PlayerPrefs.GetInt(highScoreKey, 0);

            PlayerPrefs.SetInt(
                TotalRecipesKey,
                newTotal);

            if (completedRecipes > previousHighScore)
            {
                PlayerPrefs.SetInt(
                    highScoreKey,
                    completedRecipes);
            }

            PlayerPrefs.Save();
        }

        private static string GetHighScoreKey(
            string tableId)
        {
            if (string.IsNullOrWhiteSpace(tableId))
            {
                throw new ArgumentException(
                    "La mesa necesita un ID válido.",
                    nameof(tableId));
            }

            return HighScorePrefix + tableId.Trim();
        }
    }
}
