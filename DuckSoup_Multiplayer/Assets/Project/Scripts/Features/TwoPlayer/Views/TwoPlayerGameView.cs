
using System;
using DuckSoup.Features.GameFeel.Gameplay;
using DuckSoup.Features.GameFeel.UI;
using DuckSoup.Features.TwoPlayer.Contracts;
using DuckSoup.Features.TwoPlayer.Services;
using TMPro;
using UnityEngine;

namespace DuckSoup.Features.TwoPlayer.Views
{
    public sealed class TwoPlayerGameView : MonoBehaviour, ITwoPlayerFlowView
    {
        [Header("Gameplay")]
        [SerializeField] private TwoPlayerBoardView board;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI overlayText;

        [Header("Old score (optional; leave empty)")]
        [SerializeField] private TextMeshProUGUI scoreText;

        [Header("New 3-text score: P1 - P2")]
        [SerializeField] private TextMeshProUGUI player1ScoreText;
        [SerializeField] private TextMeshProUGUI scoreSeparator;
        [SerializeField] private TextMeshProUGUI player2ScoreText;

        [Header("Optional score animation")]
        [SerializeField] private ScoreNumberFeel player1ScoreNumber;
        [SerializeField] private ScoreNumberFeel player2ScoreNumber;

        [Header("Other game feel")]
        [SerializeField] private TimerMusicFeel timerMusic;
        [SerializeField] private UIShakeFeel overlayShake;

        private TwoPlayerSession session;
        private int lastLoggedPlayer1 = int.MinValue;
        private int lastLoggedPlayer2 = int.MinValue;

        public void Initialize(TwoPlayerSession newSession)
        {
            session = newSession ??
                throw new ArgumentNullException(nameof(newSession));

            if (board == null ||
                timerText == null ||
                overlayText == null ||
                player1ScoreText == null ||
                player2ScoreText == null)
            {
                throw new InvalidOperationException(
                    "TwoPlayerGameView: asigna Board, Timer Text, " +
                    "Overlay Text, Player1 Score Text y Player2 Score Text.");
            }

            overlayText.gameObject.SetActive(false);

            player1ScoreText.gameObject.SetActive(true);
            player2ScoreText.gameObject.SetActive(true);

            if (scoreSeparator != null)
            {
                scoreSeparator.gameObject.SetActive(true);
                scoreSeparator.text = "-";
            }

            if (scoreText != null &&
                scoreText != player1ScoreText &&
                scoreText != player2ScoreText &&
                scoreText != scoreSeparator)
            {
                scoreText.gameObject.SetActive(false);
            }

            RefreshHud(
                session.RemainingSeconds,
                session.Player1Score,
                session.Player2Score);
        }

        public void SetInputEnabled(bool enabled)
        {
            board.SetInputEnabled(enabled);

            if (timerMusic != null)
            {
                if (enabled)
                    timerMusic.BeginGameplay();
                else
                    timerMusic.EndGameplay();
            }
        }

        public void ShowOverlay(string message)
        {
            overlayText.text = message;
            overlayText.gameObject.SetActive(true);

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
            overlayText.gameObject.SetActive(false);
        }

        public void RefreshBoard()
        {
            board.Refresh();
        }

        public void RefreshHud(
            float remainingSeconds,
            int player1Score,
            int player2Score)
        {
            int total = Mathf.CeilToInt(
                Mathf.Max(0f, remainingSeconds));

            timerText.text =
                $"{total / 60:00}:{total % 60:00}";

            // Actualizamos los textos directamente.
            player1ScoreText.text = player1Score.ToString();
            player2ScoreText.text = player2Score.ToString();

            if (player1Score != lastLoggedPlayer1 ||
                player2Score != lastLoggedPlayer2)
            {
                Debug.Log(
                    $"[2P SCORE] P1={player1Score}, P2={player2Score}",
                    this);

                lastLoggedPlayer1 = player1Score;
                lastLoggedPlayer2 = player2Score;
            }

            // El efecto pop es opcional.
            if (player1ScoreNumber != null)
                player1ScoreNumber.Show(player1Score);

            if (player2ScoreNumber != null)
                player2ScoreNumber.Show(player2Score);

            if (timerMusic != null)
                timerMusic.RenderRemaining(remainingSeconds);
        }
    }
}

