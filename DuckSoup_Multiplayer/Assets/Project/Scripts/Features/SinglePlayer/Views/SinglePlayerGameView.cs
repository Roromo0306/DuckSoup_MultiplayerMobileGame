using System;
using DuckSoup.Features.GameFeel.Gameplay;
using DuckSoup.Features.GameFeel.UI;
using DuckSoup.Features.SinglePlayer.Contracts;
using DuckSoup.Features.SinglePlayer.Services;
using TMPro;
using UnityEngine;

namespace DuckSoup.Features.SinglePlayer.Views
{
    public sealed class SinglePlayerGameView : MonoBehaviour, ISinglePlayerFlowView
    {
        [Header("Board")]
        [SerializeField] private SinglePlayerBoardView board;
        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [Header("Game Feel - optional number-only UI")]
        [SerializeField] private ScoreNumberFeel scoreNumber;
        [SerializeField] private TimerMusicFeel timerMusic;
        [SerializeField] private UIShakeFeel overlayShake;
        [Header("Overlays")]
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private TextMeshProUGUI recipeCompletedText;
        [SerializeField, Min(0.1f)] private float recipeMessageSeconds = 0.8f;

        private SinglePlayerSession session;
        private float messageRemaining;
        private bool initialized;

        public void Initialize(SinglePlayerSession currentSession)
        {
            if (initialized) return;
            session = currentSession ?? throw new ArgumentNullException(nameof(currentSession));
            if (board == null || timerText == null || scoreText == null ||
                countdownText == null || recipeCompletedText == null)
                throw new InvalidOperationException("Faltan referencias en SinglePlayerGameView.");
            board.RecipeCompleted += ShowRecipeCompleted;
            countdownText.gameObject.SetActive(false);
            recipeCompletedText.gameObject.SetActive(false);
            RefreshHud(session.RemainingSeconds, session.CompletedRecipes);
            initialized = true;
        }

        public void SetGameplayInputEnabled(bool enabled)
        {
            board.SetInputEnabled(enabled);
            if (timerMusic != null)
            {
                if (enabled) timerMusic.BeginGameplay();
                else timerMusic.EndGameplay();
            }
            if (!enabled) HideRecipeCompleted();
        }

        public void ShowOverlay(string message)
        {
            HideRecipeCompleted();
            countdownText.text = message;
            countdownText.gameObject.SetActive(true);
            if (overlayShake != null &&
                message.StartsWith("TIME UP", StringComparison.OrdinalIgnoreCase))
                overlayShake.Shake();
        }

        public void HideOverlay() => countdownText.gameObject.SetActive(false);
        public void RefreshBoard() => board.Refresh();

        public void RefreshHud(float seconds, int recipes)
        {
            int remaining = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            timerText.text = $"{remaining / 60:00}:{remaining % 60:00}";
            if (scoreNumber == null)
                scoreText.text = "RECIPES: " + recipes;
            else
            {
                scoreText.text = "RECIPES";
                scoreNumber.Show(recipes);
            }
            if (timerMusic != null) timerMusic.RenderRemaining(seconds);
        }

        private void ShowRecipeCompleted()
        {
            recipeCompletedText.text = "RECIPE COMPLETED!";
            recipeCompletedText.gameObject.SetActive(true);
            messageRemaining = recipeMessageSeconds;
            RefreshHud(session.RemainingSeconds, session.CompletedRecipes);
        }

        private void Update()
        {
            if (messageRemaining <= 0f) return;
            messageRemaining -= Time.unscaledDeltaTime;
            if (messageRemaining <= 0f) HideRecipeCompleted();
        }

        private void HideRecipeCompleted()
        {
            messageRemaining = 0f;
            if (recipeCompletedText != null)
                recipeCompletedText.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (board != null) board.RecipeCompleted -= ShowRecipeCompleted;
        }
    }
}
