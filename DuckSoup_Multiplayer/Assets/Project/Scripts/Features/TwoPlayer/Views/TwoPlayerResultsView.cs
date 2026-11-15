
using System;
using DuckSoup.Features.GameFeel.UI;
using DuckSoup.Features.Menu.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.TwoPlayer.Views
{
    // Resultados de 2P: patitos, puntos, WINNER/LOSER, NEXT y lluvia UI.
    public sealed class TwoPlayerResultsView : MonoBehaviour
    {
        [Header("Results Panel")]
        [SerializeField] private GameObject resultsPanel;

        [Header("Ducks - same order as character selection")]
        [SerializeField] private Sprite[] duckSprites = new Sprite[4];
        [SerializeField] private Image player1DuckImage;
        [SerializeField] private Image player2DuckImage;

        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI player1StatusText;
        [SerializeField] private TextMeshProUGUI player2StatusText;
        [SerializeField] private TextMeshProUGUI player1ScoreText;
        [SerializeField] private TextMeshProUGUI player2ScoreText;

        [Header("Loser Shade")]
        [SerializeField] private Image player1LoserShade;
        [SerializeField] private Image player2LoserShade;
        [SerializeField, Range(0f, 1f)]
        private float loserShadeAlpha = 0.55f;

        [Header("Next")]
        [SerializeField] private Button nextButton;
        [SerializeField, Min(0.1f)]
        private float nextDelaySeconds = 2f;

        [Header("WINNER / LOSER text animations")]
        [SerializeField] private ResultStatusFeel player1StatusFeel;
        [SerializeField] private ResultStatusFeel player2StatusFeel;

        [Header("New UI rain effects - one per PLAYER PANEL")]
        [SerializeField] private UIResultRainEmitter player1ResultFX;
        [SerializeField] private UIResultRainEmitter player2ResultFX;

        [Header("Old effects - kept only to stop them")]
        [SerializeField] private UIConfettiEmitter confetti;
        [SerializeField] private ParticleSystem leftNativeConfetti;
        [SerializeField] private ParticleSystem rightNativeConfetti;

        public event Action NextRequested;

        private bool showing;
        private bool nextUnlocked;
        private float nextElapsed;

        private void Awake()
        {
            if (nextButton != null)
                nextButton.onClick.AddListener(HandleNext);

            Hide();
        }

        // Mantiene la firma que utiliza TwoPlayerGameplayBootstrap.
        public void Show(
            int player1Duck,
            int player2Duck,
            int player1Score,
            int player2Score)
        {
            ValidateReferences();

            SetDuck(player1DuckImage, player1Duck);
            SetDuck(player2DuckImage, player2Duck);

            player1ScoreText.text = $"RECIPES: {player1Score}";
            player2ScoreText.text = $"RECIPES: {player2Score}";

            bool isDraw = player1Score == player2Score;
            bool player1Wins = player1Score > player2Score;

            player1StatusText.text = isDraw
                ? "DRAW"
                : player1Wins ? "WINNER" : "LOSER";

            player2StatusText.text = isDraw
                ? "DRAW"
                : player1Wins ? "LOSER" : "WINNER";

            SetShade(
                player1LoserShade,
                !isDraw && !player1Wins);

            SetShade(
                player2LoserShade,
                !isDraw && player1Wins);

            showing = true;
            nextUnlocked = false;
            nextElapsed = 0f;

            nextButton.interactable = false;
            nextButton.gameObject.SetActive(false);
            resultsPanel.SetActive(true);

            // Limpiar las explosiones antiguas y los efectos
            // de partidas anteriores.
            StopOldEffects();
            player1ResultFX?.StopEffect();
            player2ResultFX?.StopEffect();

            if (player1StatusFeel != null)
            {
                player1StatusFeel.Play(
                    isDraw
                        ? ResultStatusFeel.Status.Draw
                        : player1Wins
                            ? ResultStatusFeel.Status.Winner
                            : ResultStatusFeel.Status.Loser);
            }

            if (player2StatusFeel != null)
            {
                player2StatusFeel.Play(
                    isDraw
                        ? ResultStatusFeel.Status.Draw
                        : player1Wins
                            ? ResultStatusFeel.Status.Loser
                            : ResultStatusFeel.Status.Winner);
            }

            // Cada efecto pertenece al PANEL del jugador,
            // no a una posicion fija de la pantalla.
            if (!isDraw)
            {
                if (player1Wins)
                {
                    player1ResultFX?.PlayWinner();
                    player2ResultFX?.PlayLoser();
                }
                else
                {
                    player1ResultFX?.PlayLoser();
                    player2ResultFX?.PlayWinner();
                }
            }
        }

        private void SetDuck(Image image, int index)
        {
            if (index < 0 || index >= duckSprites.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index));
            }

            image.sprite = duckSprites[index];
            image.enabled = duckSprites[index] != null;
            image.color = Color.white;
            image.preserveAspect = true;
        }

        private void SetShade(Image shade, bool darken)
        {
            shade.enabled = darken;
            shade.color = new Color(
                0f, 0f, 0f, loserShadeAlpha);

            shade.raycastTarget = false;
        }

        private void Update()
        {
            if (!showing || nextUnlocked)
                return;

            nextElapsed += Time.unscaledDeltaTime;

            if (nextElapsed < nextDelaySeconds)
                return;

            nextUnlocked = true;
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
        }

        private void HandleNext()
        {
            if (!showing || !nextUnlocked)
                return;

            nextUnlocked = false;
            nextButton.interactable = false;

            NextRequested?.Invoke();
        }

        public void Hide()
        {
            showing = false;
            nextUnlocked = false;

            player1StatusFeel?.StopNow();
            player2StatusFeel?.StopNow();

            player1ResultFX?.StopEffect();
            player2ResultFX?.StopEffect();

            StopOldEffects();

            if (resultsPanel != null)
                resultsPanel.SetActive(false);
        }

        private void StopOldEffects()
        {
            confetti?.StopAndClear();

            if (leftNativeConfetti != null)
            {
                leftNativeConfetti.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (rightNativeConfetti != null)
            {
                rightNativeConfetti.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void ValidateReferences()
        {
            if (resultsPanel == null ||
                player1DuckImage == null ||
                player2DuckImage == null ||
                player1StatusText == null ||
                player2StatusText == null ||
                player1ScoreText == null ||
                player2ScoreText == null ||
                player1LoserShade == null ||
                player2LoserShade == null ||
                nextButton == null ||
                duckSprites == null ||
                duckSprites.Length != GameLaunchRequest.AvailableDucks)
            {
                throw new InvalidOperationException(
                    "Missing references or duck sprites " +
                    "in TwoPlayerResultsView.");
            }
        }

        private void OnDestroy()
        {
            if (nextButton != null)
                nextButton.onClick.RemoveListener(HandleNext);
        }
    }
}
