
using System;
using System.Collections;
using DuckSoup.Features.Menu.Controllers;
using DuckSoup.Features.Menu.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.Menu.Views
{
    /// <summary>
    /// Vista principal del menú.
    ///
    /// Gestiona la selección de modos,
    /// la pestaña desplegable y la navegación
    /// hacia los selectores correspondientes.
    /// </summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        [Header("Player Information")]
        [SerializeField]
        private GameObject singlePlayerStats;

        [SerializeField]
        private TextMeshProUGUI playerNameText;

        [SerializeField]
        private TextMeshProUGUI totalRecipesText;

        [SerializeField]
        private TextMeshProUGUI selectedModeText;

        [Header("Main Action")]
        [SerializeField]
        private Button mainPlayButton;

        [Header("Mode Drawer")]
        [SerializeField]
        private RectTransform modeDrawer;

        [SerializeField]
        private Button drawerTabButton;

        [SerializeField]
        private Button onePlayerButton;

        [SerializeField]
        private Button twoPlayersButton;

        [SerializeField]
        private TextMeshProUGUI drawerTabText;

        [Header("Drawer Animation")]
        [SerializeField]
        private float closedY = -260f;

        [SerializeField]
        private float openedY = 0f;

        [SerializeField, Min(0.01f)]
        private float animationDuration = 0.3f;

        [Header("Notice Panel")]
        [SerializeField]
        private GameObject noticePanel;

        [SerializeField]
        private TextMeshProUGUI noticeText;

        [SerializeField]
        private Button noticeCloseButton;

        private MainMenuController controller;

        private TableSelectorView tableSelector;

        private DuckSelectionView duckSelector;

        private bool initialized;
        private bool drawerOpened;

        private Coroutine drawerAnimation;

        /// <summary>
        /// Recibe las dependencias del menú.
        /// </summary>
        public void Initialize(
            MainMenuController menuController,
            TableSelectorView tableSelectorView,
            DuckSelectionView duckSelectionView)
        {
            if (initialized)
                return;

            controller = menuController ??
                throw new ArgumentNullException(
                    nameof(menuController));

            tableSelector = tableSelectorView ??
                throw new ArgumentNullException(
                    nameof(tableSelectorView));

            duckSelector = duckSelectionView ??
                throw new ArgumentNullException(
                    nameof(duckSelectionView));

            ValidateReferences();

            drawerTabButton.onClick.AddListener(
                ToggleDrawer);

            onePlayerButton.onClick.AddListener(
                SelectOnePlayer);

            twoPlayersButton.onClick.AddListener(
                SelectTwoPlayers);

            mainPlayButton.onClick.AddListener(
                HandleMainPlay);

            noticeCloseButton.onClick.AddListener(
                HideNotice);

            drawerOpened = false;

            SetDrawerPosition(closedY);

            drawerTabText.text = "▲ GAME MODE";

            noticePanel.SetActive(false);

            initialized = true;

            Refresh();
        }

        /// <summary>
        /// Actualiza la información del menú
        /// según el modo seleccionado.
        /// </summary>
        private void Refresh()
        {
            if (controller == null)
                return;

            bool isSinglePlayer =
                controller.SelectedMode ==
                GameMode.SinglePlayer;

            singlePlayerStats.SetActive(isSinglePlayer);

            playerNameText.text =
                controller.PlayerName;

            totalRecipesText.text =
                "TOTAL RECIPES: " +
                controller.TotalRecipesCompleted;

            selectedModeText.text = isSinglePlayer
                ? "1 PLAYER MODE"
                : "2 PLAYERS MODE";
        }

        private void SelectOnePlayer()
        {
            controller.SelectMode(
                GameMode.SinglePlayer);

            Refresh();
            CloseDrawer();
        }

        /// <summary>
        /// Cambia a dos jugadores y abre
        /// inmediatamente el selector de patitos.
        /// </summary>
        private void SelectTwoPlayers()
        {
            controller.SelectMode(
                GameMode.TwoPlayers);

            Refresh();
            CloseDrawer();

            duckSelector.Open();
        }

        /// <summary>
        /// Abre el selector apropiado
        /// según el modo actual.
        /// </summary>
        private void HandleMainPlay()
        {
            CloseDrawer();

            if (controller.SelectedMode ==
                GameMode.SinglePlayer)
            {
                tableSelector.Open();
            }
            else
            {
                duckSelector.Open();
            }
        }

        private void ToggleDrawer()
        {
            AnimateDrawer(!drawerOpened);
        }

        private void CloseDrawer()
        {
            AnimateDrawer(false);
        }

        private void AnimateDrawer(bool open)
        {
            drawerOpened = open;

            if (drawerAnimation != null)
            {
                StopCoroutine(drawerAnimation);
            }

            float targetY = open
                ? openedY
                : closedY;

            drawerAnimation = StartCoroutine(
                MoveDrawer(targetY));

            drawerTabText.text = open
                ? "▼ GAME MODE"
                : "▲ GAME MODE";
        }

        private IEnumerator MoveDrawer(float targetY)
        {
            float startY =
                modeDrawer.anchoredPosition.y;

            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float normalized = Mathf.Clamp01(
                    elapsed / animationDuration);

                float eased = Mathf.SmoothStep(
                    0f, 1f, normalized);

                float newY = Mathf.Lerp(
                    startY,
                    targetY,
                    eased);

                SetDrawerPosition(newY);

                yield return null;
            }

            SetDrawerPosition(targetY);
            drawerAnimation = null;
        }

        private void SetDrawerPosition(float y)
        {
            Vector2 position =
                modeDrawer.anchoredPosition;

            position.y = y;

            modeDrawer.anchoredPosition = position;
        }

        public void ShowNotice(string message)
        {
            noticeText.text = message;

            noticePanel.SetActive(true);
        }

        private void HideNotice()
        {
            noticePanel.SetActive(false);
        }

        private void ValidateReferences()
        {
            if (singlePlayerStats == null ||
                playerNameText == null ||
                totalRecipesText == null ||
                selectedModeText == null ||
                mainPlayButton == null ||
                modeDrawer == null ||
                drawerTabButton == null ||
                onePlayerButton == null ||
                twoPlayersButton == null ||
                drawerTabText == null ||
                noticePanel == null ||
                noticeText == null ||
                noticeCloseButton == null)
            {
                throw new InvalidOperationException(
                    "[MainMenu] Faltan referencias " +
                    "en el Inspector.");
            }
        }

        private void OnDestroy()
        {
            if (!initialized)
                return;

            drawerTabButton.onClick.RemoveListener(
                ToggleDrawer);

            onePlayerButton.onClick.RemoveListener(
                SelectOnePlayer);

            twoPlayersButton.onClick.RemoveListener(
                SelectTwoPlayers);

            mainPlayButton.onClick.RemoveListener(
                HandleMainPlay);

            noticeCloseButton.onClick.RemoveListener(
                HideNotice);
        }
    }
}
