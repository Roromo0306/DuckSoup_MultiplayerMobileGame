using System;
using DuckSoup.Application.StateMachine;
using DuckSoup.Core.Randomization;
using DuckSoup.Features.GameFeel.Menu;
using DuckSoup.Features.Menu.Models;
using DuckSoup.Features.Menu.Services;
using DuckSoup.Features.Round.Configuration;
using DuckSoup.Features.SinglePlayer.Services;
using DuckSoup.Features.SinglePlayer.States;
using DuckSoup.Features.SinglePlayer.Views;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DuckSoup.Bootstrap
{
    public sealed class SinglePlayerGameplayBootstrap : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] private SinglePlayerBoardView boardView;
        [SerializeField] private SinglePlayerGameView gameView;
        [Header("Session")]
        [SerializeField, Min(1f)] private float gameDuration = 60f;
        [SerializeField, Min(0.1f)] private float ingredientRevealSeconds = 3f;
        [SerializeField, Min(0.1f)] private float recipeReplacementSeconds = 0.8f;
        [SerializeField, Min(0.1f)] private float tokenRefillSeconds = 1.2f;
        [SerializeField, Min(0.1f)] private float timeUpDisplaySeconds = 2f;
        [SerializeField] private int randomSeed = 2026;
        [Header("Navigation")]
        [SerializeField] private string mainMenuSceneName = "MainMenuScene";
        [SerializeField] private string defaultTableId = "classic_table";

        private GameStateMachine stateMachine;
        private SinglePlayerSession session;
        private IGameState countdownState;
        private IGameState playingState;
        private IGameState timeUpState;
        private GameLaunchRequest launchRequest;
        private PlayerPrefsProgressStore progressStore;
        private bool initialized;
        private bool resultSaved;

        private void Awake()
        {
            Time.timeScale = 1f;
            if (boardView == null || gameView == null)
            {
                Debug.LogError("[1P] Faltan vistas.", this);
                enabled = false;
                return;
            }
            try
            {
                if (!GameLaunchHandoff.TryConsume(out launchRequest))
                    launchRequest = GameLaunchRequest.ForSinglePlayer(defaultTableId);
                if (launchRequest.Mode != GameMode.SinglePlayer)
                    throw new InvalidOperationException("La peticion no es SinglePlayer.");

                IRandomSource random = new SystemRandomSource(randomSeed);
                session = new SinglePlayerSession(
                    PrototypeRecipeCatalog.Create(), random, gameDuration,
                    ingredientRevealSeconds, recipeReplacementSeconds,
                    tokenRefillSeconds);
                progressStore = new PlayerPrefsProgressStore();
                boardView.Initialize(session);
                gameView.Initialize(session);
                stateMachine = new GameStateMachine();
                countdownState = new SinglePlayerCountdownState(
                    gameView, () => stateMachine.RequestStateChange(playingState));
                playingState = new SinglePlayerPlayingState(
                    session, gameView, () => stateMachine.RequestStateChange(timeUpState));
                timeUpState = new SinglePlayerTimeUpState(
                    session, gameView, SaveFinalResult, ReturnToMainMenu,
                    timeUpDisplaySeconds);
                initialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (initialized) stateMachine.RequestStateChange(countdownState);
        }

        private void Update()
        {
            if (initialized) stateMachine.Tick(Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (!initialized) return;
            stateMachine.ApplyPendingTransition();
        }

        private void SaveFinalResult()
        {
            if (resultSaved) return;
            try
            {
                // Es necesario leer el total ANTES de guardar.
                int previousTotal = progressStore.GetTotalRecipesCompleted();
                progressStore.RegisterSinglePlayerRun(
                    launchRequest.TableId, session.CompletedRecipes);
                resultSaved = true;
                SinglePlayerRewardHandoff.Queue(previousTotal, session.CompletedRecipes);
                Debug.Log($"[1P] Saved +{session.CompletedRecipes} recipes.", this);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
            }
        }

        private void ReturnToMainMenu()
        {
            if (!UnityEngine.Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError("[1P] MainMenuScene no esta en Build Settings.", this);
                return;
            }
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) boardView?.CancelAllDrags();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) boardView?.CancelAllDrags();
        }

        private void OnDisable()
        {
            boardView?.SetInputEnabled(false);
            stateMachine?.Stop();
        }
    }
}
