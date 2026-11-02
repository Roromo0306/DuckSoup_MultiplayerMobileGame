
using System;
using DuckSoup.Features.GameFeel.Menu;
using DuckSoup.Features.Menu.Contracts;
using DuckSoup.Features.Menu.Controllers;
using DuckSoup.Features.Menu.Models;
using DuckSoup.Features.Menu.Services;
using DuckSoup.Features.Menu.Views;
using DuckSoup.Features.Transitions;
using UnityEngine;

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

        [Header("Profile")]
        [SerializeField]
        private string fakePlayerName = "ChefDuck01";

        [Header("Reward Animation")]
        [SerializeField]
        private MainMenuRewardFeel rewardFeel;

        [Header("Scenes")]
        [SerializeField]
        private string singlePlayerSceneName =
            "SinglePlayerGameplayScene";

        [SerializeField]
        private string twoPlayerSceneName =
            "TwoPlayerGameplayScene";

        [Header("Two Players Table")]
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
                    "[Menu] Faltan referencias de vistas.",
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

        private void HandleTableConfirmed(TableDisplayData table)
        {
            if (!SceneExists(singlePlayerSceneName))
                return;

            GameLaunchHandoff.Prepare(
                GameLaunchRequest.ForSinglePlayer(table.Id));

            SceneFadeTransition.LoadScene(
                singlePlayerSceneName);
        }

        private void HandleDucksConfirmed(
            int player1Duck,
            int player2Duck)
        {
            if (!SceneExists(twoPlayerSceneName))
                return;

            GameLaunchHandoff.Prepare(
                GameLaunchRequest.ForTwoPlayers(
                    twoPlayerTableId,
                    player1Duck,
                    player2Duck));

            SceneFadeTransition.LoadScene(
                twoPlayerSceneName);
        }

        private bool SceneExists(string sceneName)
        {
            if (UnityEngine.Application
                .CanStreamedLevelBeLoaded(sceneName))
                return true;

            mainMenuView.ShowNotice(
                $"Scene missing from Build Settings:\n{sceneName}");

            return false;
        }
    }
}
