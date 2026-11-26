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
        [SerializeField] private string fakePlayerName = "ChefDuck01";

        [Header("Single Player Reward")]
        [SerializeField] private MainMenuRewardFeel rewardFeel;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button mainPlayButton;
        [SerializeField] private Button selectTableButton;
        [SerializeField] private TextMeshProUGUI selectedTableButtonText;
        [SerializeField] private GameObject selectTableButtonRoot;

        [Header("Mode Tabs")]
        [SerializeField] private Button singlePlayerTabButton;
        [SerializeField] private Button twoPlayerTabButton;
        [SerializeField] private bool mainPlayButtonIsShared = true;
        [SerializeField] private bool startOnSinglePlayerTab = true;

        [Header("Remember Selected Table")]
        [SerializeField] private bool rememberLastSelectedTable = true;

        [Header("Scenes")]
        [SerializeField]
        private string singlePlayerSceneName = "SinglePlayerGameplayScene";

        [SerializeField]
        private string twoPlayerSceneName = "TwoPlayerGameplayScene";

        [Header("Multiplayer")]
        [SerializeField] private string twoPlayerTableId = "classic_table";

        [Header("Select-table hint")]
        [SerializeField] private TextMeshProUGUI selectTableHint;

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
                HideTableHint();

                GameLaunchHandoff.Clear();

                progressStore = new PlayerPrefsProgressStore();
                modeStore = new PlayerPrefsMenuModeStore();

                controller = new MainMenuController(
                    progressStore,
                    modeStore,
                    fakePlayerName);

                tableSelectorView.Initialize(
                    progressStore,
                    HandleTableConfirmed);

                duckSelectionView.Initialize(HandleDucksConfirmed);

                mainMenuView.Initialize(
                    controller,
                    tableSelectorView,
                    duckSelectionView);

                LoadSavedTable();
                ConnectButtons();

                initialized = true;

                if (startOnSinglePlayerTab &&
                    singlePlayerTabButton != null)
                {
                    singlePlayerTabButton.onClick.Invoke();
                }
                else
                {
                    singlePlayerModeActive = true;
                    RefreshSelectedTableButton();
                    RefreshTableButtonVisibility();
                }

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
                    "1P and 2P tab buttons.",
                    this);
            }
        }

        private void ConnectButtons()
        {
            mainPlayButton.onClick.RemoveAllListeners();
            mainPlayButton.onClick.AddListener(HandleMainPlayClicked);

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

        private void HandleSinglePlayerTab()
        {
            singlePlayerModeActive = true;
            RefreshTableButtonVisibility();
            RefreshSelectedTableButton();
        }

        private void HandleTwoPlayerTab()
        {
            singlePlayerModeActive = false;
            HideTableHint();
            RefreshTableButtonVisibility();
        }

        private void RefreshTableButtonVisibility()
        {
            GameObject root = selectTableButtonRoot != null
                ? selectTableButtonRoot
                : selectTableButton.gameObject;

            if (root != null)
                root.SetActive(singlePlayerModeActive);
        }

        private void HandleSelectTableClicked()
        {
            if (!initialized || launching)
                return;

            HideTableHint();
            tableSelectorView.Open();
        }

        private void HandleTableConfirmed(TableDisplayData table)
        {
            if (table == null)
                return;

            selectedTable = table;
            HideTableHint();

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

        private void HideTableHint()
        {
            if (selectTableHint != null)
                selectTableHint.gameObject.SetActive(false);
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
                HideTableHint();
            }
        }

        private void HandleMainPlayClicked()
        {
            if (!initialized || launching)
                return;

            if (mainPlayButtonIsShared && !singlePlayerModeActive)
            {
                duckSelectionView.Open();
                return;
            }

            StartSelectedSinglePlayerTable();
        }

        private void StartSelectedSinglePlayerTable()
        {
            if (selectedTable == null)
            {
                if (selectTableHint != null)
                {
                    selectTableHint.text = "select a table to start";
                    selectTableHint.gameObject.SetActive(true);
                }
                else
                {
                    Debug.LogWarning(
                        "[MainMenu] Assign Select Table Hint.",
                        this);
                }

                return;
            }

            if (!SceneExists(singlePlayerSceneName))
                return;

            launching = true;

            GameLaunchHandoff.Prepare(
                GameLaunchRequest.ForSinglePlayer(selectedTable.Id));

            bool started =
                SceneFadeTransition.LoadScene(singlePlayerSceneName);

            if (!started)
            {
                launching = false;
                mainMenuView.ShowNotice("COULD NOT START GAME.");
            }
        }

        private void HandleDucksConfirmed(int duck1, int duck2)
        {
            if (launching)
                return;

            if (!SceneExists(twoPlayerSceneName))
                return;

            launching = true;

            GameLaunchHandoff.Prepare(
                GameLaunchRequest.ForTwoPlayers(
                    twoPlayerTableId,
                    duck1,
                    duck2));

            bool transitionStarted =
                SceneFadeTransition.LoadScene(twoPlayerSceneName);

            if (!transitionStarted)
            {
                launching = false;

                Debug.LogWarning(
                    "[MainMenu] No se pudo iniciar " +
                    "la transicion a Multiplayer.",
                    this);

                duckSelectionView.Open();
            }
        }

        private bool SceneExists(string sceneName)
        {
            if (UnityEngine.Application.CanStreamedLevelBeLoaded(sceneName))
                return true;

            mainMenuView.ShowNotice(
                "Scene missing from Build Settings:\n" + sceneName);

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