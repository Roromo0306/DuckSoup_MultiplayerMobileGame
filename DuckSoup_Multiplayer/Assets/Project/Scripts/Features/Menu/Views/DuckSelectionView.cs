
using System;
using DuckSoup.Features.Menu.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Image = UnityEngine.UI.Image;

namespace DuckSoup.Features.Menu.Views
{
    /// <summary>
    /// Selector de patitos para dos jugadores.
    ///
    /// Cada jugador navega por un carrusel
    /// y confirma su elección con SELECT.
    ///
    /// PLAY solo está disponible cuando
    /// ambos jugadores han confirmado.
    /// </summary>
    public sealed class DuckSelectionView : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField]
        private GameObject panelRoot;

        [Header("Available Ducks")]
        [SerializeField]
        private Sprite[] duckSprites = new Sprite[4];

        [Header("Player 1 - Carousel")]
        [SerializeField]
        private Image player1DuckImage;

        [SerializeField]
        private TextMeshProUGUI player1DuckNameText;

        [SerializeField]
        private Button player1PreviousButton;

        [SerializeField]
        private Button player1NextButton;

        [SerializeField]
        private Button player1SelectButton;

        [SerializeField]
        private TextMeshProUGUI player1SelectionText;

        [Header("Player 2 - Carousel")]
        [SerializeField]
        private Image player2DuckImage;

        [SerializeField]
        private TextMeshProUGUI player2DuckNameText;

        [SerializeField]
        private Button player2PreviousButton;

        [SerializeField]
        private Button player2NextButton;

        [SerializeField]
        private Button player2SelectButton;

        [SerializeField]
        private TextMeshProUGUI player2SelectionText;

        [Header("Central Controls")]
        [SerializeField]
        private TextMeshProUGUI statusText;

        [SerializeField]
        private Button playButton;

        [SerializeField]
        private Button closeButton;

        private Action<int, int> selectionConfirmed;

        // Índice del pato que se está mostrando.
        private int player1ViewedIndex;
        private int player2ViewedIndex;

        // -1 significa que no está confirmado.
        private int player1ConfirmedIndex = -1;
        private int player2ConfirmedIndex = -1;

        private bool initialized;

        /// <summary>
        /// Conecta el selector con el Bootstrap.
        /// </summary>
        public void Initialize(
            Action<int, int> onSelectionConfirmed)
        {
            if (initialized)
                return;

            ValidateReferences();

            selectionConfirmed =
                onSelectionConfirmed ??
                throw new ArgumentNullException(
                    nameof(onSelectionConfirmed));

            // Flechas del jugador 1.
            player1PreviousButton.onClick.AddListener(
                PreviousPlayer1Duck);

            player1NextButton.onClick.AddListener(
                NextPlayer1Duck);

            // Flechas del jugador 2.
            player2PreviousButton.onClick.AddListener(
                PreviousPlayer2Duck);

            player2NextButton.onClick.AddListener(
                NextPlayer2Duck);

            // Botones de confirmación.
            player1SelectButton.onClick.AddListener(
                ConfirmPlayer1Duck);

            player2SelectButton.onClick.AddListener(
                ConfirmPlayer2Duck);

            // Controles centrales.
            playButton.onClick.AddListener(
                ConfirmSelection);

            closeButton.onClick.AddListener(
                Close);

            initialized = true;

            panelRoot.SetActive(false);
        }

        /// <summary>
        /// Abre el selector e inicia
        /// una selección nueva.
        /// </summary>
        public void Open()
        {
            if (!initialized)
            {
                Debug.LogError(
                    "[DuckSelection] No inicializado.",
                    this);

                return;
            }

            // Ambos comienzan viendo el primer pato.
            player1ViewedIndex = 0;
            player2ViewedIndex = 0;

            // Ninguno ha confirmado todavía.
            player1ConfirmedIndex = -1;
            player2ConfirmedIndex = -1;

            panelRoot.SetActive(true);

            Refresh();
        }

        /// <summary>
        /// Oculta la pantalla de selección.
        /// </summary>
        public void Close()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        // ==========================
        // PLAYER 1 - NAVIGATION
        // ==========================

        private void PreviousPlayer1Duck()
        {
            ChangePlayer1Duck(-1);
        }

        private void NextPlayer1Duck()
        {
            ChangePlayer1Duck(1);
        }

        private void ChangePlayer1Duck(int direction)
        {
            player1ViewedIndex = WrapIndex(
                player1ViewedIndex + direction);

            // Cambiar de personaje anula la
            // confirmación anterior.
            player1ConfirmedIndex = -1;

            Refresh();
        }

        private void ConfirmPlayer1Duck()
        {
            player1ConfirmedIndex =
                player1ViewedIndex;

            Refresh();

            Debug.Log(
                $"[DuckSelection] Player 1 selected " +
                $"Duck {player1ConfirmedIndex + 1}",
                this);
        }

        // ==========================
        // PLAYER 2 - NAVIGATION
        // ==========================

        private void PreviousPlayer2Duck()
        {
            ChangePlayer2Duck(-1);
        }

        private void NextPlayer2Duck()
        {
            ChangePlayer2Duck(1);
        }

        private void ChangePlayer2Duck(int direction)
        {
            player2ViewedIndex = WrapIndex(
                player2ViewedIndex + direction);

            player2ConfirmedIndex = -1;

            Refresh();
        }

        private void ConfirmPlayer2Duck()
        {
            player2ConfirmedIndex =
                player2ViewedIndex;

            Refresh();

            Debug.Log(
                $"[DuckSelection] Player 2 selected " +
                $"Duck {player2ConfirmedIndex + 1}",
                this);
        }

        /// <summary>
        /// Permite pasar del último al primero
        /// y del primero al último.
        /// </summary>
        private int WrapIndex(int index)
        {
            int count = duckSprites.Length;

            return (index % count + count) % count;
        }

        // ==========================
        // PRESENTATION
        // ==========================

        private void Refresh()
        {
            // Actualizamos los retratos.
            UpdateDuckImage(
                player1DuckImage,
                player1ViewedIndex);

            UpdateDuckImage(
                player2DuckImage,
                player2ViewedIndex);

            // Nombre del pato visible.
            player1DuckNameText.text =
                $"DUCK {player1ViewedIndex + 1}";

            player2DuckNameText.text =
                $"DUCK {player2ViewedIndex + 1}";

            bool player1Ready =
                player1ConfirmedIndex >= 0;

            bool player2Ready =
                player2ConfirmedIndex >= 0;

            // Estado de selección.
            player1SelectionText.text =
                player1Ready
                    ? "SELECTED"
                    : "PRESS SELECT";

            player2SelectionText.text =
                player2Ready
                    ? "SELECTED"
                    : "PRESS SELECT";

            // Una vez seleccionado, no hace
            // falta repetir SELECT hasta cambiar pato.
            player1SelectButton.interactable =
                !player1Ready;

            player2SelectButton.interactable =
                !player2Ready;

            bool bothReady =
                player1Ready && player2Ready;

            // El botón central solo se activa
            // cuando los dos jugadores confirman.
            playButton.interactable = bothReady;

            if (bothReady)
            {
                statusText.text =
                    "BOTH PLAYERS READY!";
            }
            else if (player1Ready)
            {
                statusText.text =
                    "WAITING FOR PLAYER 2...";
            }
            else if (player2Ready)
            {
                statusText.text =
                    "WAITING FOR PLAYER 1...";
            }
            else
            {
                statusText.text =
                    "WAITING FOR PLAYERS...";
            }
        }

        private void UpdateDuckImage(
            Image image,
            int duckIndex)
        {
            Sprite sprite = duckSprites[duckIndex];

            image.sprite = sprite;
            image.enabled = sprite != null;

            // Conservamos el aspecto
            // y los colores de cada patito.
            image.preserveAspect = true;
            image.color = Color.white;
        }

        // ==========================
        // CONFIRM GAME
        // ==========================

        private void ConfirmSelection()
        {
            // Seguridad: no permitir empezar
            // sin dos selecciones confirmadas.
            if (player1ConfirmedIndex < 0 ||
                player2ConfirmedIndex < 0)
            {
                return;
            }

            int player1Duck =
                player1ConfirmedIndex;

            int player2Duck =
                player2ConfirmedIndex;

            Close();

            // Notificamos al Bootstrap.
            selectionConfirmed?.Invoke(
                player1Duck,
                player2Duck);
        }

        // ==========================
        // VALIDATION
        // ==========================

        private void ValidateReferences()
        {
            if (panelRoot == null ||
                player1DuckImage == null ||
                player1DuckNameText == null ||
                player1PreviousButton == null ||
                player1NextButton == null ||
                player1SelectButton == null ||
                player1SelectionText == null ||
                player2DuckImage == null ||
                player2DuckNameText == null ||
                player2PreviousButton == null ||
                player2NextButton == null ||
                player2SelectButton == null ||
                player2SelectionText == null ||
                statusText == null ||
                playButton == null ||
                closeButton == null)
            {
                throw new InvalidOperationException(
                    "[DuckSelection] Faltan referencias " +
                    "en el Inspector.");
            }

            if (duckSprites == null ||
                duckSprites.Length !=
                GameLaunchRequest.AvailableDucks)
            {
                throw new InvalidOperationException(
                    "[DuckSelection] Deben existir " +
                    "exactamente cuatro sprites.");
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

            player1SelectButton.onClick.RemoveListener(
                ConfirmPlayer1Duck);

            player2PreviousButton.onClick.RemoveListener(
                PreviousPlayer2Duck);

            player2NextButton.onClick.RemoveListener(
                NextPlayer2Duck);

            player2SelectButton.onClick.RemoveListener(
                ConfirmPlayer2Duck);

            playButton.onClick.RemoveListener(
                ConfirmSelection);

            closeButton.onClick.RemoveListener(
                Close);
        }
    }
}
