using System;
using DuckSoup.Features.TwoPlayer.Contracts;
using DuckSoup.Features.TwoPlayer.Services;
using TMPro;
using UnityEngine;

namespace DuckSoup.Features.TwoPlayer.Views
{
    /// <summary>HUD y cuenta atras del modo 2P. No contiene reglas de juego.</summary>
    public sealed class TwoPlayerGameView : MonoBehaviour, ITwoPlayerFlowView
    {
        [SerializeField] private TwoPlayerBoardView board;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI overlayText;

        private TwoPlayerSession session;

        public void Initialize(TwoPlayerSession newSession)
        {
            session = newSession ?? throw new ArgumentNullException(nameof(newSession));
            if (board == null || timerText == null || scoreText == null || overlayText == null)
                throw new InvalidOperationException("Faltan referencias de HUD en TwoPlayerGameView.");
            overlayText.gameObject.SetActive(false);
            RefreshHud(session.RemainingSeconds, session.Player1Score, session.Player2Score);
        }

        public void SetInputEnabled(bool enabled) => board.SetInputEnabled(enabled);

        public void ShowOverlay(string text)
        {
            overlayText.text = text;
            overlayText.gameObject.SetActive(true);
        }

        public void HideOverlay() => overlayText.gameObject.SetActive(false);
        public void RefreshBoard() => board.Refresh();

        public void RefreshHud(float time, int player1Score, int player2Score)
        {
            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, time));
            timerText.text = $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
            scoreText.text = $"{player1Score} - {player2Score}";
        }
    }
}
