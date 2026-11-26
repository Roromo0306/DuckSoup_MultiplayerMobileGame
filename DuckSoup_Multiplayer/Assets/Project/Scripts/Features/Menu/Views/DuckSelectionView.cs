using System;
using System.Collections;
using DuckSoup.Features.Audio;
using DuckSoup.Features.GameFeel.UI;
using DuckSoup.Features.Menu.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.Menu.Views
{
    public sealed class DuckSelectionView : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;

        [Header("Duck Sprites")]
        [SerializeField] private Sprite[] duckSprites = new Sprite[4];

        [Header("Optional Selected Duck Sprites")]
        [SerializeField]
        private Sprite[] duckSelectedSprites =
            new Sprite[4];

        [SerializeField]
        private bool useSelectedSpritesAsPreview = false;

        [Header("Player 1")]
        [SerializeField] private Image player1DuckImage;
        [SerializeField] private TextMeshProUGUI player1DuckNameText;
        [SerializeField] private Button player1PreviousButton;
        [SerializeField] private Button player1NextButton;

        [Header("Player 2")]
        [SerializeField] private Image player2DuckImage;
        [SerializeField] private TextMeshProUGUI player2DuckNameText;
        [SerializeField] private Button player2PreviousButton;
        [SerializeField] private Button player2NextButton;

        [Header("Main Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button closeButton;

        [Header("Old SELECT UI")]
        [SerializeField] private Button player1SelectButton;
        [SerializeField] private Button player2SelectButton;
        [SerializeField] private TextMeshProUGUI player1SelectionText;
        [SerializeField] private TextMeshProUGUI player2SelectionText;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Carousel Game Feel")]
        [SerializeField] private UICarouselFeel player1CarouselFeel;
        [SerializeField] private UICarouselFeel player2CarouselFeel;

        private Action<int, int> selectionConfirmed;
        private int player1ViewedIndex = 0;
        private int player2ViewedIndex = 0;
        private bool initialized;
        private bool launching;
        private Coroutine launchRoutine;

        public void Initialize(Action<int, int> onSelectionConfirmed)
        {
            if (initialized)
                return;

            ValidateReferences();

            selectionConfirmed = onSelectionConfirmed ??
                throw new ArgumentNullException(
                    nameof(onSelectionConfirmed));

            player1PreviousButton.onClick.AddListener(
                PreviousPlayer1Duck);

            player1NextButton.onClick.AddListener(
                NextPlayer1Duck);

            player2PreviousButton.onClick.AddListener(
                PreviousPlayer2Duck);

            player2NextButton.onClick.AddListener(
                NextPlayer2Duck);

            playButton.onClick.AddListener(ConfirmSelection);
            closeButton.onClick.AddListener(Close);

            initialized = true;

            HideOldSelectionUI();
            panelRoot.SetActive(false);
        }

        private void HideOldSelectionUI()
        {
            if (player1SelectButton != null)
                player1SelectButton.gameObject.SetActive(false);

            if (player2SelectButton != null)
                player2SelectButton.gameObject.SetActive(false);

            if (player1SelectionText != null)
                player1SelectionText.gameObject.SetActive(false);

            if (player2SelectionText != null)
                player2SelectionText.gameObject.SetActive(false);

            if (statusText != null)
                statusText.gameObject.SetActive(false);
        }

        public void Open()
        {
            if (!initialized)
            {
                Debug.LogError(
                    "[DuckSelection] Not initialized.",
                    this);

                return;
            }

            if (launchRoutine != null)
            {
                StopCoroutine(launchRoutine);
                launchRoutine = null;
            }

            launching = false;
            panelRoot.SetActive(true);

            player1CarouselFeel?.ResetVisual(false);
            player2CarouselFeel?.ResetVisual(false);

            HideOldSelectionUI();
            Refresh();
        }

        public void Close()
        {
            if (launchRoutine != null)
            {
                StopCoroutine(launchRoutine);
                launchRoutine = null;
            }

            if (panelRoot != null)
                panelRoot.SetActive(false);

            launching = false;
        }

        private void PreviousPlayer1Duck()
        {
            MovePlayer1(-1);
        }

        private void NextPlayer1Duck()
        {
            MovePlayer1(1);
        }

        private void MovePlayer1(int direction)
        {
            if (launching)
                return;

            if (player1CarouselFeel != null)
            {
                if (player1CarouselFeel.IsSwitching)
                    return;

                player1CarouselFeel.Switch(
                    direction,
                    () => ApplyPlayer1Move(direction));
            }
            else
            {
                ApplyPlayer1Move(direction);
            }
        }

        private void ApplyPlayer1Move(int direction)
        {
            player1ViewedIndex =
                WrapIndex(player1ViewedIndex + direction);

            Refresh();
            DuckSoupSoundPlayer.PlayDuck(player1ViewedIndex);
        }

        private void PreviousPlayer2Duck()
        {
            MovePlayer2(-1);
        }

        private void NextPlayer2Duck()
        {
            MovePlayer2(1);
        }

        private void MovePlayer2(int direction)
        {
            if (launching)
                return;

            if (player2CarouselFeel != null)
            {
                if (player2CarouselFeel.IsSwitching)
                    return;

                player2CarouselFeel.Switch(
                    direction,
                    () => ApplyPlayer2Move(direction));
            }
            else
            {
                ApplyPlayer2Move(direction);
            }
        }

        private void ApplyPlayer2Move(int direction)
        {
            player2ViewedIndex =
                WrapIndex(player2ViewedIndex + direction);

            Refresh();
            DuckSoupSoundPlayer.PlayDuck(player2ViewedIndex);
        }

        private int WrapIndex(int index)
        {
            int count = duckSprites.Length;
            return (index % count + count) % count;
        }

        private void Refresh()
        {
            if (!initialized)
                return;

            SetDuck(player1DuckImage, player1ViewedIndex);
            SetDuck(player2DuckImage, player2ViewedIndex);

            player1DuckNameText.text =
                "DUCK " + (player1ViewedIndex + 1);

            player2DuckNameText.text =
                "DUCK " + (player2ViewedIndex + 1);

            playButton.interactable = !launching;
        }

        private void SetDuck(Image image, int index)
        {
            if (image == null)
                return;

            Sprite sprite = null;

            if (useSelectedSpritesAsPreview &&
                duckSelectedSprites != null &&
                index >= 0 &&
                index < duckSelectedSprites.Length)
            {
                sprite = duckSelectedSprites[index];
            }

            if (sprite == null &&
                index >= 0 &&
                index < duckSprites.Length)
            {
                sprite = duckSprites[index];
            }

            image.sprite = sprite;
            image.enabled = sprite != null;
            image.preserveAspect = true;
            image.color = Color.white;
        }

        private void ConfirmSelection()
        {
            if (!initialized || launching)
                return;

            launching = true;
            playButton.interactable = false;

            if (CarouselsAreSwitching())
            {
                launchRoutine =
                    StartCoroutine(WaitForCarouselsAndLaunch());
            }
            else
            {
                LaunchMatch();
            }
        }

        private bool CarouselsAreSwitching()
        {
            bool player1Moving =
                player1CarouselFeel != null &&
                player1CarouselFeel.IsSwitching;

            bool player2Moving =
                player2CarouselFeel != null &&
                player2CarouselFeel.IsSwitching;

            return player1Moving || player2Moving;
        }

        private IEnumerator WaitForCarouselsAndLaunch()
        {
            while (CarouselsAreSwitching())
                yield return null;

            launchRoutine = null;
            LaunchMatch();
        }

        private void LaunchMatch()
        {
            int duck1 = player1ViewedIndex;
            int duck2 = player2ViewedIndex;

            Debug.Log(
                "[DuckSelection] Starting Multiplayer. " +
                "P1 Duck: " + duck1 +
                " | P2 Duck: " + duck2,
                this);

            selectionConfirmed?.Invoke(duck1, duck2);
        }

        private void ValidateReferences()
        {
            if (panelRoot == null ||
                player1DuckImage == null ||
                player2DuckImage == null ||
                player1DuckNameText == null ||
                player2DuckNameText == null ||
                player1PreviousButton == null ||
                player1NextButton == null ||
                player2PreviousButton == null ||
                player2NextButton == null ||
                playButton == null ||
                closeButton == null ||
                duckSprites == null ||
                duckSprites.Length != GameLaunchRequest.AvailableDucks)
            {
                throw new InvalidOperationException(
                    "[DuckSelection] Missing required references " +
                    "or duck sprites.");
            }
        }

        private void OnDestroy()
        {
            if (!initialized)
                return;

            player1PreviousButton.onClick.RemoveListener(
                PreviousPlayer1Duck);

            player1NextButton.onClick.RemoveListener(
                NextPlayer1Duck);

            player2PreviousButton.onClick.RemoveListener(
                PreviousPlayer2Duck);

            player2NextButton.onClick.RemoveListener(
                NextPlayer2Duck);

            playButton.onClick.RemoveListener(ConfirmSelection);
            closeButton.onClick.RemoveListener(Close);
        }
    }
}