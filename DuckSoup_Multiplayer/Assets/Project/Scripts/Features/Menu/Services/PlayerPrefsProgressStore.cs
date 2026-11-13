using System;
using DuckSoup.Features.Menu.Contracts;
using UnityEngine;

namespace DuckSoup.Features.Menu.Services
{
    public sealed class PlayerPrefsProgressStore : IPlayerProgressStore
    {
        private const string TotalRecipesKey = "DuckSoup_1P_TotalRecipes";
        private const string HighScorePrefix = "DuckSoup_1P_HighScore_";

        public int GetTotalRecipesCompleted() =>
            PlayerPrefs.GetInt(TotalRecipesKey, 0);

        public int GetTableHighScore(string tableId) =>
            PlayerPrefs.GetInt(HighScorePrefix + ValidId(tableId), 0);

        // Keeps the old interface and all legacy callers working.
        public void RegisterSinglePlayerRun(string tableId, int completedRecipes) =>
            RegisterSinglePlayerRun(tableId, completedRecipes, completedRecipes);

        // Physical recipe count and combo-weighted score are deliberately different.
        public void RegisterSinglePlayerRun(
            string tableId, int completedRecipes, int score)
        {
            tableId = ValidId(tableId);
            if (completedRecipes < 0 || score < 0)
                throw new ArgumentOutOfRangeException("Score and recipes must be nonnegative.");
            int previous = GetTotalRecipesCompleted();
            int total = checked(previous + completedRecipes);
            PlayerPrefs.SetInt(TotalRecipesKey, total);
            int highscore = GetTableHighScore(tableId);
            if (score > highscore)
                PlayerPrefs.SetInt(HighScorePrefix + tableId, score);
            PlayerPrefs.Save();
        }

        private static string ValidId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Table id cannot be empty.", nameof(id));
            return id.Trim();
        }
    }
}
