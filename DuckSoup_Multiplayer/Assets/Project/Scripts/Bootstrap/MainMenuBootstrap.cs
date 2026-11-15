
using System;
using DuckSoup.Features.GameFeel.Menu;
using DuckSoup.Features.Menu.Contracts;
using DuckSoup.Features.Menu.Controllers;
using DuckSoup.Features.Menu.Models;
using DuckSoup.Features.Menu.Services;
using DuckSoup.Features.Menu.Views;
using DuckSoup.Features.Transitions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DuckSoup.Bootstrap
{
    public sealed class MainMenuBootstrap : MonoBehaviour
    {
        private const string SelectedTableSaveKey =
            "DuckSoup_1P_SelectedTableId";

        [Header("Existing Views")]
        [SerializeField] private MainMenuView mainMenuView;
        [SerializeField] private TableSelectorView tableSelectorView;
        [SerializeField] private DuckSelectionView duckSelectionView;

        [Header("Profile")]
        [SerializeField]
        private string fakePlayerName = "ChefDuck01";

        [Header("Single Player Reward")]
        [SerializeField]
        private MainMenuRewardFeel rewardFeel;

        [Header("NEW - Main Menu Buttons")]
        [Tooltip("PLAY principal del menu.")]
        [SerializeField] private Button mainPlayButton;

        [Tooltip("Boton debajo de PLAY que abre las mesas.")]
        [SerializeField] private Button selectTableButton;

        [Tooltip("Texto SELECT TABLE / nombre de la mesa.")]
        [SerializeField]
        private TextMeshProUGUI selectedTableButtonText;

        [Tooltip("Objeto que contiene el boton de mesa.")]
        [SerializeField]
        private GameObject selectTableButtonRoot;

        [Header("Mode Tabs")]
        [SerializeField] private Button singlePlayerTabButton;
        [SerializeField] private Button twoPlayerTabButton;

        [Tooltip("Activalo si el PLAY principal se reutiliza en 1P y 2P.")]
        [SerializeField]
        private bool mainPlayButtonIsShared = true;

        [SerializeField]
        private bool startOnSinglePlayerTab = true;

        [Header("Remember Selected Table")]
        [SerializeField]
        private bool rememberLastSelectedTable = true;

        [Header("Scenes")]
        [SerializeField]
        private string singlePlayerSceneName =
            "SinglePlayerGameplayScene";

        [SerializeField]
        private string twoPlayerSceneName =
            "TwoPlayerGameplayScene";

        [Header("Multiplayer")]
        [SerializeField]
        private string twoPlayerTableId = "classic_table";

        private IPlayerProgressStore progressStore;
        private IMenuModeStore modeStore;
        private MainMenuController controller;

        private TableDisplayData selectedTable;

        private bool singlePlayerModeActive = true;
        private bool initialized;
        private bool launching;

        private void Awake()
        {
            try
            {
                ValidateReferences();

                // Limpiar solicitudes anteriores.
                GameLaunchHandoff.Clear();

                progressStore =
                    new PlayerPrefsProgressStore();

                modeStore =
                    new PlayerPrefsMenuModeStore();

                controller =
                    new MainMenuController(
                        progressStore,
                        modeStore,
                        fakePlayerName);

                // El callback de mesa AHORA SOLO GUARDA
                // la selección, no inicia gameplay.
                tableSelectorView.Initialize(
                    progressStore,
                    HandleTableConfirmed);

                duckSelectionView.Initialize(
                    HandleDucksConfirmed);

                // Mantener la lógica original del menú,
                // del perfil y de sus pestañas.
                mainMenuView.Initialize(
                    controller,
                    tableSelectorView,
                    duckSelectionView);

                // Recuperar la última mesa seleccionada,
                // si existe.
                LoadSavedTable();

                // Conectar la navegación nueva.
                ConnectButtons();

                initialized = true;

                if (startOnSinglePlayerTab &&
                    singlePlayerTabButton != null)
                {
                    // Sincroniza también la parte visual
                    // del menú que ya teníamos.
                    singlePlayerTabButton.onClick.Invoke();
                }
                else
                {
                    singlePlayerModeActive = true;
                    RefreshSelectedTableButton();
                    RefreshTableButtonVisibility();
                }

                // Conservar la animación del perfil
                // al volver de una partida individual.
                if (SinglePlayerRewardHandoff.TryConsume(
                    out SinglePlayerRewardHandoff.Reward reward))
                {
                    if (rewardFeel != null)
                    {
                        rewardFeel.Play(
                            reward.PreviousTotal,
                            reward.Earned);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                enabled = false;
            }
        }

        private void ValidateReferences()
        {
            if (mainMenuView == null ||
                tableSelectorView == null ||
                duckSelectionView == null)
            {
                throw new InvalidOperationException(
                    "[MainMenu] Assign the three menu views.");
            }

            if (mainPlayButton == null ||
                selectTableButton == null ||
                selectedTableButtonText == null)
            {
                throw new InvalidOperationException(
                    "[MainMenu] Assign Main Play Button, " +
                    "Select Table Button and its Text.");
            }

            if (mainPlayButton == selectTableButton)
            {
                throw new InvalidOperationException(
                    "[MainMenu] PLAY and SELECT TABLE " +
                    "must be different buttons.");
            }

            if (mainPlayButtonIsShared &&
                (singlePlayerTabButton == null ||
                 twoPlayerTabButton == null))
            {
                Debug.LogWarning(
                    "[MainMenu] PLAY is shared. Assign the " +
                    "1P and 2P tab buttons so it knows " +
                    "which mode should open.",
                    this);
            }
        }

        private void ConnectButtons()
        {
            // MainMenuView ya ha añadido su listener
            // original. Lo reemplazamos para que
            // PLAY no abra el popup de mesas.

            mainPlayButton.onClick.RemoveAllListeners();

            mainPlayButton.onClick.AddListener(
                HandleMainPlayClicked);

            selectTableButton.onClick.AddListener(
                HandleSelectTableClicked);

            if (singlePlayerTabButton != null)
            {
                singlePlayerTabButton.onClick.AddListener(
                    HandleSinglePlayerTab);
            }

            if (twoPlayerTabButton != null)
            {
                twoPlayerTabButton.onClick.AddListener(
                    HandleTwoPlayerTab);
            }
        }

        // --------------------------------------
        // 1P / 2P TABS
        // --------------------------------------

        private void HandleSinglePlayerTab()
        {
            singlePlayerModeActive = true;
            RefreshTableButtonVisibility();
            RefreshSelectedTableButton();
        }

        private void HandleTwoPlayerTab()
        {
            singlePlayerModeActive = false;
            RefreshTableButtonVisibility();
        }

        private void RefreshTableButtonVisibility()
        {
            GameObject root = selectTableButtonRoot != null
                ? selectTableButtonRoot
                : selectTableButton.gameObject;

            if (root != null)
            {
                root.SetActive(singlePlayerModeActive);
            }
        }

        // --------------------------------------
        // SELECT TABLE
        // --------------------------------------

        private void HandleSelectTableClicked()
        {
            if (!initialized || launching)
                return;

            // No iniciamos ninguna partida.
            // Solo mostramos el popup.
            tableSelectorView.Open();
        }

        private void HandleTableConfirmed(TableDisplayData table)
        {
            if (table == null)
                return;

            selectedTable = table;

            if (rememberLastSelectedTable)
            {
                PlayerPrefs.SetString(
                    SelectedTableSaveKey,
                    table.Id);

                PlayerPrefs.Save();
            }

            RefreshSelectedTableButton();
        }

        private void RefreshSelectedTableButton()
        {
            selectedTableButtonText.text =
                selectedTable != null
                    ? selectedTable.DisplayName
                    : "SELECT TABLE";
        }

        private void LoadSavedTable()
        {
            selectedTable = null;

            if (!rememberLastSelectedTable)
                return;

            string savedId = PlayerPrefs.GetString(
                SelectedTableSaveKey,
                "");

            if (string.IsNullOrEmpty(savedId))
                return;

            if (tableSelectorView.TrySelectTableById(
                savedId,
                out TableDisplayData table))
            {
                selectedTable = table;
            }
        }

        // --------------------------------------
        // MAIN PLAY BUTTON
        // --------------------------------------

        private void HandleMainPlayClicked()
        {
            if (!initialized || launching)
                return;

            // Si ambos modos comparten PLAY,
            // conservamos el flujo 2P:
            // PLAY -> Selector de patitos.
            if (mainPlayButtonIsShared &&
                !singlePlayerModeActive)
            {
                duckSelectionView.Open();
                return;
            }

            StartSelectedSinglePlayerTable();
        }

        private void StartSelectedSinglePlayerTable()
        {
            // Primera vez: hay que elegir una mesa.
            if (selectedTable == null)
            {
                mainMenuView.ShowNotice(
                    "SELECT A TABLE FIRST!");

                tableSelectorView.Open();
                return;
            }

            if (!SceneExists(singlePlayerSceneName))
                return;

            launching = true;

            // Aquí enviamos el ID correcto.
            // El GameplayBootstrap seleccionará
            // mesa Classic o Conveyor según TableId.
            GameLaunchHandoff.Prepare(
                GameLaunchRequest.ForSinglePlayer(
                    selectedTable.Id));

            bool started = SceneFadeTransition.LoadScene(
                singlePlayerSceneName);

            if (!started)
            {
                launching = false;

                mainMenuView.ShowNotice(
                    "COULD NOT START GAME.");
            }
        }

        // --------------------------------------
        // MULTIPLAYER - UNCHANGED
        // --------------------------------------


        private void HandleDucksConfirmed(int duck1, int duck2)
        {
            if (launching)
                return;

            if (!SceneExists(twoPlayerSceneName))
                return;

            launching = true;

            // Guardar los dos patitos visibles en el selector.
            GameLaunchHandoff.Prepare(
                GameLaunchRequest.ForTwoPlayers(
                    twoPlayerTableId,
                    duck1,
                    duck2));

            // IMPORTANTE:
            // Utilizar nuestro sistema de transiciones.
            // Hace fade out, cambia de escena y hace fade in.
            bool transitionStarted =
                SceneFadeTransition.LoadScene(twoPlayerSceneName);

            if (!transitionStarted)
            {
                launching = false;

                Debug.LogWarning(
                    "[MainMenu] No se pudo iniciar " +
                    "la transición a Multiplayer.",
                    this);

                duckSelectionView.Open();
            }
        }


        private bool SceneExists(string sceneName)
        {
            if (UnityEngine.Application
                .CanStreamedLevelBeLoaded(sceneName))
            {
                return true;
            }

            mainMenuView.ShowNotice(
                "Scene missing from Build Settings:\n" +
                sceneName);

            return false;
        }

        private void OnDestroy()
        {
            if (!initialized)
                return;

            if (mainPlayButton != null)
            {
                mainPlayButton.onClick.RemoveListener(
                    HandleMainPlayClicked);
            }

            if (selectTableButton != null)
            {
                selectTableButton.onClick.RemoveListener(
                    HandleSelectTableClicked);
            }

            if (singlePlayerTabButton != null)
            {
                singlePlayerTabButton.onClick.RemoveListener(
                    HandleSinglePlayerTab);
            }

            if (twoPlayerTabButton != null)
            {
                twoPlayerTabButton.onClick.RemoveListener(
                    HandleTwoPlayerTab);
            }
        }
    }
}
