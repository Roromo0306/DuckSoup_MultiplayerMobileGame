using System;
using DuckSoup.Features.Menu.Contracts;
using DuckSoup.Features.Menu.Controllers;
using DuckSoup.Features.Menu.Models;
using DuckSoup.Features.Menu.Services;
using DuckSoup.Features.Menu.Views;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DuckSoup.Bootstrap
{
    /// <summary>Menu: 1P carga su escena y 2P carga su escena tras elegir patitos.</summary>
    public sealed class MainMenuBootstrap : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] private MainMenuView mainMenuView;
        [SerializeField] private TableSelectorView tableSelectorView;
        [SerializeField] private DuckSelectionView duckSelectionView;
        [Header("Profile")]
        [SerializeField] private string fakePlayerName = "ChefDuck01";
        [Header("Scenes")]
        [SerializeField] private string singlePlayerSceneName = "SinglePlayerGameplayScene";
        [SerializeField] private string twoPlayerSceneName = "TwoPlayerGameplayScene";
        [Header("2P Table")]
        [SerializeField] private string twoPlayerTableId = "classic_table";

        private IPlayerProgressStore progressStore;
        private IMenuModeStore modeStore;
        private MainMenuController controller;

        private void Awake()
        {
            if (mainMenuView == null || tableSelectorView == null || duckSelectionView == null)
            {
                Debug.LogError("[Menu] Faltan referencias de vistas.", this);
                enabled = false;
                return;
            }
            try
            {
                GameLaunchHandoff.Clear();
                progressStore = new PlayerPrefsProgressStore();
                modeStore = new PlayerPrefsMenuModeStore();
                controller = new MainMenuController(progressStore, modeStore, fakePlayerName);
                tableSelectorView.Initialize(progressStore, HandleTableConfirmed);
                duckSelectionView.Initialize(HandleDucksConfirmed);
                mainMenuView.Initialize(controller, tableSelectorView, duckSelectionView);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
                enabled = false;
            }
        }

        private void HandleTableConfirmed(TableDisplayData table)
        {
            if (!SceneExists(singlePlayerSceneName)) return;
            GameLaunchHandoff.Prepare(GameLaunchRequest.ForSinglePlayer(table.Id));
            SceneManager.LoadScene(singlePlayerSceneName);
        }

        private void HandleDucksConfirmed(int duck1, int duck2)
        {
            if (!SceneExists(twoPlayerSceneName)) return;
            GameLaunchHandoff.Prepare(
                GameLaunchRequest.ForTwoPlayers(twoPlayerTableId, duck1, duck2));
            SceneManager.LoadScene(twoPlayerSceneName);
        }

        private bool SceneExists(string sceneName)
        {
            if (UnityEngine.Application.CanStreamedLevelBeLoaded(sceneName)) return true;
            mainMenuView.ShowNotice($"Scene missing from Build Settings:\n{sceneName}");
            return false;
        }
    }
}
