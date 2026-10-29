using System;
using DuckSoup.Features.GameFeel.UI;
using DuckSoup.Features.Menu.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

namespace DuckSoup.Features.Menu.Views
{
    // Selector por carrusel: flechas, SELECT individual, PLAY al confirmar ambos.
    public sealed class DuckSelectionView : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [Header("Ducks")]
        [SerializeField] private Sprite[] duckSprites = new Sprite[4];
        [Header("Player 1")]
        [SerializeField] private Image player1DuckImage;
        [SerializeField] private TextMeshProUGUI player1DuckNameText;
        [SerializeField] private Button player1PreviousButton;
        [SerializeField] private Button player1NextButton;
        [SerializeField] private Button player1SelectButton;
        [SerializeField] private TextMeshProUGUI player1SelectionText;
        [Header("Player 2")]
        [SerializeField] private Image player2DuckImage;
        [SerializeField] private TextMeshProUGUI player2DuckNameText;
        [SerializeField] private Button player2PreviousButton;
        [SerializeField] private Button player2NextButton;
        [SerializeField] private Button player2SelectButton;
        [SerializeField] private TextMeshProUGUI player2SelectionText;
        [Header("Center")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button playButton;
        [SerializeField] private Button closeButton;
        [Header("Game Feel - image + name groups (NOT arrow buttons)")]
        [SerializeField] private UICarouselFeel player1CarouselFeel;
        [SerializeField] private UICarouselFeel player2CarouselFeel;

        private Action<int, int> selectionConfirmed;
        private int player1ViewedIndex;
        private int player2ViewedIndex;
        private int player1ConfirmedIndex = -1;
        private int player2ConfirmedIndex = -1;
        private bool initialized;

        public void Initialize(Action<int, int> onSelectionConfirmed)
        {
            if (initialized) return;
            ValidateReferences();
            selectionConfirmed = onSelectionConfirmed ??
                throw new ArgumentNullException(nameof(onSelectionConfirmed));
            player1PreviousButton.onClick.AddListener(PreviousPlayer1Duck);
            player1NextButton.onClick.AddListener(NextPlayer1Duck);
            player2PreviousButton.onClick.AddListener(PreviousPlayer2Duck);
            player2NextButton.onClick.AddListener(NextPlayer2Duck);
            player1SelectButton.onClick.AddListener(ConfirmPlayer1Duck);
            player2SelectButton.onClick.AddListener(ConfirmPlayer2Duck);
            playButton.onClick.AddListener(ConfirmSelection);
            closeButton.onClick.AddListener(Close);
            initialized = true;
            panelRoot.SetActive(false);
        }

        public void Open()
        {
            if (!initialized)
            {
                Debug.LogError("[DuckSelection] No inicializado.", this);
                return;
            }
            player1ViewedIndex = 0;
            player2ViewedIndex = 0;
            player1ConfirmedIndex = -1;
            player2ConfirmedIndex = -1;
            panelRoot.SetActive(true);
            player1CarouselFeel?.ResetVisual();
            player2CarouselFeel?.ResetVisual();
            Refresh();
        }

        public void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void PreviousPlayer1Duck() => MovePlayer1(-1);
        private void NextPlayer1Duck() => MovePlayer1(1);
        private void PreviousPlayer2Duck() => MovePlayer2(-1);
        private void NextPlayer2Duck() => MovePlayer2(1);

        private void MovePlayer1(int direction)
        {
            if (player1CarouselFeel != null)
            {
                player1CarouselFeel.Switch(direction, () =>
                {
                    player1ViewedIndex = WrapIndex(player1ViewedIndex + direction);
                    player1ConfirmedIndex = -1;
                    Refresh();
                });
            }
            else
            {
                player1ViewedIndex = WrapIndex(player1ViewedIndex + direction);
                player1ConfirmedIndex = -1;
                Refresh();
            }
        }

        private void MovePlayer2(int direction)
        {
            if (player2CarouselFeel != null)
            {
                player2CarouselFeel.Switch(direction, () =>
                {
                    player2ViewedIndex = WrapIndex(player2ViewedIndex + direction);
                    player2ConfirmedIndex = -1;
                    Refresh();
                });
            }
            else
            {
                player2ViewedIndex = WrapIndex(player2ViewedIndex + direction);
                player2ConfirmedIndex = -1;
                Refresh();
            }
        }

        private void ConfirmPlayer1Duck()
        {
            if (player1CarouselFeel != null && player1CarouselFeel.IsSwitching) return;
            player1ConfirmedIndex = player1ViewedIndex;
            Refresh();
        }

        private void ConfirmPlayer2Duck()
        {
            if (player2CarouselFeel != null && player2CarouselFeel.IsSwitching) return;
            player2ConfirmedIndex = player2ViewedIndex;
            Refresh();
        }

        private int WrapIndex(int index) =>
            (index % duckSprites.Length + duckSprites.Length) % duckSprites.Length;

        private void Refresh()
        {
            SetDuck(player1DuckImage, player1ViewedIndex);
            SetDuck(player2DuckImage, player2ViewedIndex);
            player1DuckNameText.text = $"DUCK {player1ViewedIndex + 1}";
            player2DuckNameText.text = $"DUCK {player2ViewedIndex + 1}";
            bool ready1 = player1ConfirmedIndex >= 0;
            bool ready2 = player2ConfirmedIndex >= 0;
            player1SelectionText.text = ready1 ? "SELECTED" : "PRESS SELECT";
            player2SelectionText.text = ready2 ? "SELECTED" : "PRESS SELECT";
            player1SelectButton.interactable = !ready1;
            player2SelectButton.interactable = !ready2;
            player1CarouselFeel?.SetSelected(ready1);
            player2CarouselFeel?.SetSelected(ready2);
            playButton.interactable = ready1 && ready2;
            statusText.text = ready1 && ready2 ? "BOTH PLAYERS READY!" :
                ready1 ? "WAITING FOR PLAYER 2..." :
                ready2 ? "WAITING FOR PLAYER 1..." : "WAITING FOR PLAYERS...";
        }

        private void SetDuck(Image image, int index)
        {
            Sprite sprite = duckSprites[index];
            image.sprite = sprite;
            image.enabled = sprite != null;
            image.preserveAspect = true;
            image.color = Color.white;
        }

        private void ConfirmSelection()
        {
            if (player1ConfirmedIndex < 0 || player2ConfirmedIndex < 0) return;
            int duck1 = player1ConfirmedIndex;
            int duck2 = player2ConfirmedIndex;
            Close();
            selectionConfirmed?.Invoke(duck1, duck2);
        }

        private void ValidateReferences()
        {
            if (panelRoot == null || player1DuckImage == null ||
                player1DuckNameText == null || player1PreviousButton == null ||
                player1NextButton == null || player1SelectButton == null ||
                player1SelectionText == null || player2DuckImage == null ||
                player2DuckNameText == null || player2PreviousButton == null ||
                player2NextButton == null || player2SelectButton == null ||
                player2SelectionText == null || statusText == null ||
                playButton == null || closeButton == null ||
                duckSprites == null || duckSprites.Length != GameLaunchRequest.AvailableDucks)
                throw new InvalidOperationException(
                    "[DuckSelection] Revisa las referencias y los cuatro sprites.");
        }

        private void OnDestroy()
        {
            if (!initialized) return;
            player1PreviousButton.onClick.RemoveListener(PreviousPlayer1Duck);
            player1NextButton.onClick.RemoveListener(NextPlayer1Duck);
            player2PreviousButton.onClick.RemoveListener(PreviousPlayer2Duck);
            player2NextButton.onClick.RemoveListener(NextPlayer2Duck);
            player1SelectButton.onClick.RemoveListener(ConfirmPlayer1Duck);
            player2SelectButton.onClick.RemoveListener(ConfirmPlayer2Duck);
            playButton.onClick.RemoveListener(ConfirmSelection);
            closeButton.onClick.RemoveListener(Close);
        }
    }
}
