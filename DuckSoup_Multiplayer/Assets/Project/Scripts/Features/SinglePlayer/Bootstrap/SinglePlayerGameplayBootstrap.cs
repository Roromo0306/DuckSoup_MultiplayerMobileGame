
using System;
using DuckSoup.Application.StateMachine;
using DuckSoup.Core.Randomization;
using DuckSoup.Features.Menu.Models;
using DuckSoup.Features.Menu.Services;
using DuckSoup.Features.SinglePlayer.Services;
using DuckSoup.Features.SinglePlayer.States;
using DuckSoup.Features.SinglePlayer.Views;
using DuckSoup.Features.Round.Configuration;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DuckSoup.Bootstrap
{
    /// <summary>
    /// Construye y conecta los sistemas
    /// del modo individual.
    /// </summary>
    public sealed class SinglePlayerGameplayBootstrap :
        MonoBehaviour
    {
        [Header("Views")]
        [SerializeField]
        private SinglePlayerBoardView boardView;

        [SerializeField]
        private SinglePlayerGameView gameView;

        [Header("Session Settings")]
        [SerializeField, Min(1f)]
        private float gameDuration = 60f;

        [SerializeField, Min(0.1f)]
        private float ingredientRevealSeconds = 3f;

        [SerializeField, Min(0.1f)]
        private float recipeReplacementSeconds = 0.8f;

        [Header("Token Replacement")]
        [SerializeField, Min(0.1f)]
        private float tokenRefillSeconds = 1.2f;

        [SerializeField, Min(0.1f)]
        private float timeUpDisplaySeconds = 2f;

        [SerializeField]
        private int randomSeed = 2026;

        [Header("Navigation")]
        [SerializeField]
        private string mainMenuSceneName = "MainMenuScene";


        [SerializeField]
        private string defaultTableId = "classic_table";

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
                Debug.LogError(
                    "[SinglePlayer] Faltan vistas.",
                    this);

                enabled = false;
                return;
            }

            try
            {
                // Recuperamos la mesa que eligió
                // el jugador desde el menú.
                if (!GameLaunchHandoff.TryConsume(
                    out launchRequest))
                {
                    // Permite probar esta escena
                    // directamente desde el Editor.
                    launchRequest =
                        GameLaunchRequest.ForSinglePlayer(
                            defaultTableId);
                }

                if (launchRequest.Mode !=
                    GameMode.SinglePlayer)
                {
                    throw new InvalidOperationException(
                        "Se intentó iniciar el modo 1P " +
                        "con una petición de otro modo.");
                }

                // Configuración y dependencias.
                IRandomSource random =
                    new SystemRandomSource(randomSeed);


                session = new SinglePlayerSession(
                    PrototypeRecipeCatalog.Create(),
                    random,
                    gameDuration,
                    ingredientRevealSeconds,
                    recipeReplacementSeconds,
                    tokenRefillSeconds);


                progressStore =
                    new PlayerPrefsProgressStore();

                // Vistas.
                boardView.Initialize(session);
                gameView.Initialize(session);

                // FSM.
                stateMachine = new GameStateMachine();

                countdownState =
                    new SinglePlayerCountdownState(
                        gameView,
                        HandleCountdownFinished);

                playingState =
                    new SinglePlayerPlayingState(
                        session,
                        gameView,
                        HandleTimeUp);

                timeUpState =
                    new SinglePlayerTimeUpState(
                        session,
                        gameView,
                        SaveFinalResult,
                        ReturnToMainMenu,
                        timeUpDisplaySeconds);

                initialized = true;

                Debug.Log(
                    "[SinglePlayer] Session ready. " +
                    $"Table: {launchRequest.TableId}",
                    this);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (!initialized)
                return;

            stateMachine.RequestStateChange(
                countdownState);
        }

        private void Update()
        {
            if (!initialized)
                return;

            stateMachine.Tick(Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (!initialized)
                return;

            string previous =
                stateMachine.CurrentStateName;

            stateMachine.ApplyPendingTransition();

            string current =
                stateMachine.CurrentStateName;

            if (previous != current)
            {
                Debug.Log(
                    $"[FSM] {previous} -> {current}",
                    this);
            }
        }

        private void HandleCountdownFinished()
        {
            stateMachine.RequestStateChange(
                playingState);
        }

        private void HandleTimeUp()
        {
            stateMachine.RequestStateChange(
                timeUpState);
        }

        /// <summary>
        /// Guarda el resultado exactamente
        /// una vez al acabar el tiempo.
        /// </summary>
        private void SaveFinalResult()
        {
            if (resultSaved)
                return;

            try
            {
                progressStore.RegisterSinglePlayerRun(
                    launchRequest.TableId,
                    session.CompletedRecipes);

                resultSaved = true;

                Debug.Log(
                    "[SinglePlayer] Result saved. " +
                    $"Recipes: {session.CompletedRecipes}",
                    this);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private void ReturnToMainMenu()
        {
            if (!UnityEngine.Application
                .CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError(
                    "[SinglePlayer] MainMenuScene no " +
                    "está en Build Settings.",
                    this);

                return;
            }

            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                boardView?.CancelAllDrags();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                boardView?.CancelAllDrags();
        }

        private void OnDisable()
        {
            boardView?.SetInputEnabled(false);
            stateMachine?.Stop();
        }
    }
}

