
using System;
using DuckSoup.Features.SinglePlayer.Contracts;
using DuckSoup.Features.SinglePlayer.Services;
using TMPro;
using UnityEngine;

namespace DuckSoup.Features.SinglePlayer.Views
{
    public sealed class SinglePlayerGameView :
        MonoBehaviour, ISinglePlayerFlowView
    {
        [Header("Board")]
        [SerializeField]
        private SinglePlayerBoardView board;

        [Header("HUD")]
        [SerializeField]
        private TextMeshProUGUI timerText;

        [SerializeField]
        private TextMeshProUGUI scoreText;

        [Header("Overlays")]
        [SerializeField]
        private TextMeshProUGUI countdownText;

        [SerializeField]
        private TextMeshProUGUI recipeCompletedText;

        [SerializeField, Min(0.1f)]
        private float recipeMessageSeconds = 0.8f;

        private SinglePlayerSession session;

        private float recipeMessageRemaining;
        private bool initialized;

        public void Initialize(
            SinglePlayerSession currentSession)
        {
            if (initialized)
                return;

            session = currentSession ??
                throw new ArgumentNullException(
                    nameof(currentSession));

            if (board == null ||
                timerText == null ||
                scoreText == null ||
                countdownText == null ||
                recipeCompletedText == null)
            {
                throw new InvalidOperationException(
                    "[SinglePlayerGameView] " +
                    "Faltan referencias.");
            }

            board.RecipeCompleted += ShowRecipeCompleted;

            countdownText.gameObject.SetActive(false);
            recipeCompletedText.gameObject.SetActive(false);

            RefreshHud(
                session.RemainingSeconds,
                session.CompletedRecipes);

            initialized = true;
        }

        public void SetGameplayInputEnabled(bool enabled)
        {
            board.SetInputEnabled(enabled);

            if (!enabled)
                HideRecipeCompleted();
        }

        public void ShowOverlay(string message)
        {
            HideRecipeCompleted();

            countdownText.text = message;
            countdownText.gameObject.SetActive(true);
        }

        public void HideOverlay()
        {
            countdownText.gameObject.SetActive(false);
        }

        public void RefreshBoard()
        {
            board.Refresh();
        }

        public void RefreshHud(
            float remainingSeconds,
            int completedRecipes)
        {
            int seconds = Mathf.CeilToInt(
                Mathf.Max(0f, remainingSeconds));

            int minutesPart = seconds / 60;
            int secondsPart = seconds % 60;

            timerText.text =
                $"{minutesPart:00}:{secondsPart:00}";

            scoreText.text =
                $"RECIPES: {completedRecipes}";
        }

        private void ShowRecipeCompleted()
        {
            recipeCompletedText.text =
                "RECIPE COMPLETED!";

            recipeCompletedText.gameObject.SetActive(true);

            recipeMessageRemaining =
                recipeMessageSeconds;

            RefreshHud(
                session.RemainingSeconds,
                session.CompletedRecipes);
        }

        private void Update()
        {
            if (recipeMessageRemaining <= 0f)
                return;

            recipeMessageRemaining -=
                Time.unscaledDeltaTime;

            if (recipeMessageRemaining <= 0f)
                HideRecipeCompleted();
        }

        private void HideRecipeCompleted()
        {
            recipeMessageRemaining = 0f;

            if (recipeCompletedText != null)
            {
                recipeCompletedText.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (board != null)
            {
                board.RecipeCompleted -= ShowRecipeCompleted;
            }
        }
    }
}
