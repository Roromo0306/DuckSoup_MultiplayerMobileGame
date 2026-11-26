using System;
using DuckSoup.Features.UI;
using DuckSoup.Features.GameFeel.Gameplay;
using DuckSoup.Features.GameFeel.UI;
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
        [SerializeField] private SinglePlayerBoardView board;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI scoreText;

        [Header("Game Feel")]
        [SerializeField] private ScoreNumberFeel scoreNumber;
        [SerializeField] private TimerMusicFeel timerMusic;
        [SerializeField] private UIShakeFeel overlayShake;

        [Header("Overlays")]
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private TextMeshProUGUI recipeCompletedText;

        [SerializeField, Min(0.1f)]
        private float recipeMessageSeconds = 0.8f;

        [Header("Countdown images")]
        [SerializeField] private CountdownSpriteView countdownSprites;

        [Header("Time penalty next to the timer")]
        [SerializeField] private TextMeshProUGUI timePenaltyText;

        [SerializeField, Min(0.1f)]
        private float penaltyVisibleSeconds = 1.1f;

        private float penaltyRemaining;
        private SinglePlayerSession session;
        private float messageRemaining;
        private bool initialized;

        public void Initialize(SinglePlayerSession currentSession)
        {
            if (initialized)
                return;

            session = currentSession ??
                throw new ArgumentNullException(nameof(currentSession));

            if (board == null ||
                timerText == null ||
                scoreText == null ||
                countdownText == null ||
                recipeCompletedText == null)
            {
                throw new InvalidOperationException(
                    "Faltan referencias en SinglePlayerGameView.");
            }

            board.RecipeCompleted += ShowRecipeCompleted;
            board.TimePenaltyApplied += ShowTimePenalty;

            if (timePenaltyText != null)
                timePenaltyText.gameObject.SetActive(false);

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

            if (timerMusic != null)
            {
                if (enabled)
                    timerMusic.BeginGameplay();
                else
                    timerMusic.EndGameplay();
            }

            if (!enabled)
                HideRecipeCompleted();
        }

        public void ShowOverlay(string message)
        {
            HideRecipeCompleted();

            countdownText.enabled =
                countdownSprites == null ||
                !countdownSprites.TryShow(message);

            countdownText.text = message;
            countdownText.gameObject.SetActive(true);

            if (overlayShake != null &&
                message.StartsWith(
                    "TIME UP",
                    StringComparison.OrdinalIgnoreCase))
            {
                overlayShake.Shake();
            }
        }

        public void HideOverlay()
        {
            countdownSprites?.Hide();
            countdownText.gameObject.SetActive(false);
        }

        private void ShowTimePenalty()
        {
            RefreshHud(
                session.RemainingSeconds,
                session.CompletedRecipes);

            if (timePenaltyText == null)
                return;

            timePenaltyText.text = "-1";
            timePenaltyText.alpha = 1f;
            timePenaltyText.gameObject.SetActive(true);

            penaltyRemaining = penaltyVisibleSeconds;
        }

        public void RefreshBoard()
        {
            board.Refresh();
        }

        public void RefreshHud(float seconds, int recipes)
        {
            int remaining =
                Mathf.CeilToInt(Mathf.Max(0f, seconds));

            timerText.text =
                $"{remaining / 60:00}:{remaining % 60:00}";

            if (scoreNumber == null)
            {
                scoreText.text = "SCORE: " + recipes;
            }
            else
            {
                scoreText.text = "SCORE";
                scoreNumber.Show(recipes);
            }

            if (timerMusic != null)
                timerMusic.RenderRemaining(seconds);
        }

        private void ShowRecipeCompleted()
        {
            if (session.PowerUpsEnabled)
            {
                HideRecipeCompleted();

                RefreshHud(
                    session.RemainingSeconds,
                    session.CompletedRecipes);

                return;
            }

            recipeCompletedText.text = "RECIPE COMPLETED!";
            recipeCompletedText.gameObject.SetActive(true);
            messageRemaining = recipeMessageSeconds;

            RefreshHud(
                session.RemainingSeconds,
                session.CompletedRecipes);
        }

        private void Update()
        {
            if (timePenaltyText != null && penaltyRemaining > 0f)
            {
                penaltyRemaining -= Time.deltaTime;

                timePenaltyText.alpha =
                    Mathf.Clamp01(penaltyRemaining / 0.25f);

                if (penaltyRemaining <= 0f)
                    timePenaltyText.gameObject.SetActive(false);
            }

            if (messageRemaining <= 0f)
                return;

            messageRemaining -= Time.unscaledDeltaTime;

            if (messageRemaining <= 0f)
                HideRecipeCompleted();
        }

        private void HideRecipeCompleted()
        {
            messageRemaining = 0f;

            if (recipeCompletedText != null)
                recipeCompletedText.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (board != null)
            {
                board.RecipeCompleted -= ShowRecipeCompleted;
                board.TimePenaltyApplied -= ShowTimePenalty;
            }
        }
    }
}