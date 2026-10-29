using System;
using DuckSoup.Features.GameFeel.Gameplay;
using DuckSoup.Features.GameFeel.UI;
using DuckSoup.Features.TwoPlayer.Contracts;
using DuckSoup.Features.TwoPlayer.Services;
using TMPro;
using UnityEngine;

namespace DuckSoup.Features.TwoPlayer.Views
{
    // HUD 2P: los marcadores individuales permiten pulsar solo la cifra que suma.
    public sealed class TwoPlayerGameView : MonoBehaviour, ITwoPlayerFlowView
    {
        [Header("Original HUD")]
        [SerializeField] private TwoPlayerBoardView board;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI scoreText; // Opcional: texto antiguo 0-0.
        [SerializeField] private TextMeshProUGUI overlayText;

        [Header("Game Feel")]
        [SerializeField] private ScoreNumberFeel player1ScoreNumber;
        [SerializeField] private ScoreNumberFeel player2ScoreNumber;
        [SerializeField] private TimerMusicFeel timerMusic;
        [SerializeField] private UIShakeFeel overlayShake;

        private TwoPlayerSession session;

        public void Initialize(TwoPlayerSession newSession)
        {
            session = newSession ?? throw new ArgumentNullException(nameof(newSession));
            if (board == null || timerText == null || overlayText == null ||
                (scoreText == null && (player1ScoreNumber == null || player2ScoreNumber == null)))
                throw new InvalidOperationException("Asigna Timer, Overlay y marcadores en TwoPlayerGameView.");
            overlayText.gameObject.SetActive(false);
            RefreshHud(session.RemainingSeconds, session.Player1Score, session.Player2Score);
        }

        public void SetInputEnabled(bool enabled)
        {
            board.SetInputEnabled(enabled);
            if (timerMusic != null)
            {
                if (enabled) timerMusic.BeginGameplay();
                else timerMusic.EndGameplay();
            }
        }

        public void ShowOverlay(string message)
        {
            overlayText.text = message;
            overlayText.gameObject.SetActive(true);
            if (overlayShake != null && message.StartsWith("TIME UP", StringComparison.OrdinalIgnoreCase))
                overlayShake.Shake();
        }

        public void HideOverlay() => overlayText.gameObject.SetActive(false);
        public void RefreshBoard() => board.Refresh();

        public void RefreshHud(float seconds, int player1Score, int player2Score)
        {
            int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            timerText.text = $"{total / 60:00}:{total % 60:00}";
            if (scoreText != null) scoreText.text = $"{player1Score} - {player2Score}";
            if (player1ScoreNumber != null) player1ScoreNumber.Show(player1Score);
            if (player2ScoreNumber != null) player2ScoreNumber.Show(player2Score);
            if (timerMusic != null) timerMusic.RenderRemaining(seconds);
        }
    }
}
