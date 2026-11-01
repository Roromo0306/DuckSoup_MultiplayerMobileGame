using System;
using DuckSoup.Features.Menu.Models;
using DuckSoup.Features.GameFeel.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

namespace DuckSoup.Features.TwoPlayer.Views
{
    /// <summary>Resultados 2P: patitos, marcador y ganador/derrotado (o empate).</summary>
    public sealed class TwoPlayerResultsView : MonoBehaviour
    {
        [Header("Results Panel")]
        [SerializeField] private GameObject resultsPanel;
        [Header("Ducks (same order as character selection)")]
        [SerializeField] private Sprite[] duckSprites = new Sprite[4];
        [SerializeField] private Image player1DuckImage;
        [SerializeField] private Image player2DuckImage;
        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI player1StatusText;
        [SerializeField] private TextMeshProUGUI player2StatusText;
        [SerializeField] private TextMeshProUGUI player1ScoreText;
        [SerializeField] private TextMeshProUGUI player2ScoreText;
        [Header("Loser Tint - Images over each entire half")]
        [SerializeField] private Image player1LoserShade;
        [SerializeField] private Image player2LoserShade;
        [SerializeField, Range(0f, 1f)] private float loserShadeAlpha = 0.55f;
        [Header("Next")]
        [SerializeField] private Button nextButton;
        [SerializeField, Min(0.1f)] private float nextDelaySeconds = 2f;

        [Header("Winner / Loser Game Feel (optional)")]
        [SerializeField] private ResultStatusFeel player1StatusFeel;
        [SerializeField] private ResultStatusFeel player2StatusFeel;
        [SerializeField] private UIConfettiEmitter confetti;
        [Header("Optional native ParticleSystems: use Screen Space - Camera for visibility")]
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

        public void Show(
            int player1Duck, int player2Duck,
            int player1Score, int player2Score)
        {
            ValidateReferences();
            SetDuck(player1DuckImage, player1Duck);
            SetDuck(player2DuckImage, player2Duck);
            player1ScoreText.text = $"RECIPES: {player1Score}";
            player2ScoreText.text = $"RECIPES: {player2Score}";

            bool draw = player1Score == player2Score;
            bool player1Wins = player1Score > player2Score;
            player1StatusText.text = draw ? "DRAW" : player1Wins ? "WINNER" : "LOSER";
            player2StatusText.text = draw ? "DRAW" : player1Wins ? "LOSER" : "WINNER";

            // Los overlays se colocan al final de cada mitad para oscurecerla.
            SetShade(player1LoserShade, !draw && !player1Wins);
            SetShade(player2LoserShade, !draw && player1Wins);

            showing = true;
            nextUnlocked = false;
            nextElapsed = 0f;
            nextButton.interactable = false;
            nextButton.gameObject.SetActive(false);
            resultsPanel.SetActive(true);
            if (player1StatusFeel != null)
                player1StatusFeel.Play(draw ? ResultStatusFeel.Status.Draw :
                    player1Wins ? ResultStatusFeel.Status.Winner : ResultStatusFeel.Status.Loser);
            if (player2StatusFeel != null)
                player2StatusFeel.Play(draw ? ResultStatusFeel.Status.Draw :
                    player1Wins ? ResultStatusFeel.Status.Loser : ResultStatusFeel.Status.Winner);
            if (!draw)
            {
                confetti?.Burst();
                if (leftNativeConfetti != null) leftNativeConfetti.Play();
                if (rightNativeConfetti != null) rightNativeConfetti.Play();
            }
        }

        private void SetDuck(Image image, int index)
        {
            if (index < 0 || index >= duckSprites.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            image.sprite = duckSprites[index];
            image.enabled = duckSprites[index] != null;
            image.color = Color.white;
            image.preserveAspect = true;
        }

        private void SetShade(Image shade, bool darken)
        {
            shade.enabled = darken;
            shade.color = new Color(0f, 0f, 0f, loserShadeAlpha);
            shade.raycastTarget = false;
        }

        private void Update()
        {
            if (!showing || nextUnlocked) return;
            nextElapsed += Time.unscaledDeltaTime;
            if (nextElapsed < nextDelaySeconds) return;
            nextUnlocked = true;
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
        }

        private void HandleNext()
        {
            if (!showing || !nextUnlocked) return;
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
            confetti?.StopAndClear();
            if (leftNativeConfetti != null)
                leftNativeConfetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (rightNativeConfetti != null)
                rightNativeConfetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (resultsPanel != null) resultsPanel.SetActive(false);
        }

        private void ValidateReferences()
        {
            if (resultsPanel == null || player1DuckImage == null ||
                player2DuckImage == null || player1StatusText == null ||
                player2StatusText == null || player1ScoreText == null ||
                player2ScoreText == null || player1LoserShade == null ||
                player2LoserShade == null || nextButton == null ||
                duckSprites == null || duckSprites.Length != GameLaunchRequest.AvailableDucks)
                throw new InvalidOperationException("Faltan referencias o 4 sprites en TwoPlayerResultsView.");
        }

        private void OnDestroy()
        {
            if (nextButton != null) nextButton.onClick.RemoveListener(HandleNext);
        }
    }
}
