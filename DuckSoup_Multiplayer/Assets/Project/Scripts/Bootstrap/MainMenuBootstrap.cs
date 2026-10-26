
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
    public sealed class MainMenuBootstrap : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField]
        private MainMenuView mainMenuView;

        [SerializeField]
        private TableSelectorView tableSelectorView;

        [SerializeField]
        private DuckSelectionView duckSelectionView;

        [Header("Player")]
        [SerializeField]
        private string fakePlayerName = "ChefDuck01";

        [Header("Single Player Navigation")]
        [SerializeField]
        private string singlePlayerSceneName =
            "SinglePlayerGameplayScene";

        [Header("Two Players Settings")]
        [SerializeField]
        private string twoPlayerTableId =
            "classic_table";

        private IPlayerProgressStore progressStore;
        private IMenuModeStore modeStore;

        private MainMenuController controller;

        private void Awake()
        {
            if (mainMenuView == null ||
                tableSelectorView == null ||
                duckSelectionView == null)
            {
                Debug.LogError(
                    "[MenuBootstrap] Faltan vistas.",
                    this);

                enabled = false;
                return;
            }

            try
            {
                GameLaunchHandoff.Clear();

                progressStore =
                    new PlayerPrefsProgressStore();

                modeStore =
                    new PlayerPrefsMenuModeStore();

                controller = new MainMenuController(
                    progressStore,
                    modeStore,
                    fakePlayerName);

                tableSelectorView.Initialize(
                    progressStore,
                    HandleTableConfirmed);

                duckSelectionView.Initialize(
                    HandleDucksConfirmed);

                mainMenuView.Initialize(
                    controller,
                    tableSelectorView,
                    duckSelectionView);

                Debug.Log(
                    "[MenuBootstrap] Main Menu ready.",
                    this);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                enabled = false;
            }
        }

        /// <summary>
        /// Selección de mesa confirmada
        /// mediante Hold to Play.
        /// </summary>
        private void HandleTableConfirmed(
            TableDisplayData table)
        {
            if (!UnityEngine.Application
                .CanStreamedLevelBeLoaded(singlePlayerSceneName))
            {
                mainMenuView.ShowNotice(
                    "SinglePlayerGameplayScene is " +
                    "missing from Build Settings.");

                return;
            }

            GameLaunchRequest request =
                GameLaunchRequest.ForSinglePlayer(
                    table.Id);

            GameLaunchHandoff.Prepare(request);

            Debug.Log(
                "[MainMenu] Starting Single Player. " +
                $"Table: {request.TableId}",
                this);

            SceneManager.LoadScene(
                singlePlayerSceneName);
        }

        /// <summary>
        /// Conservamos el selector de 2P.
        /// Su gameplay se conectará en fase 4.
        /// </summary>
        private void HandleDucksConfirmed(
            int player1Duck,
            int player2Duck)
        {
            GameLaunchRequest request =
                GameLaunchRequest.ForTwoPlayers(
                    twoPlayerTableId,
                    player1Duck,
                    player2Duck);

            GameLaunchHandoff.Prepare(request);

            Debug.Log(
                "[MainMenu] 2P ready. " +
                $"P1: {player1Duck + 1}, " +
                $"P2: {player2Duck + 1}",
                this);

            mainMenuView.ShowNotice(
                "BOTH PLAYERS READY!\n" +
                $"P1: DUCK {player1Duck + 1}\n" +
                $"P2: DUCK {player2Duck + 1}\n" +
                "Two-player gameplay comes next.");
        }
    }
}
