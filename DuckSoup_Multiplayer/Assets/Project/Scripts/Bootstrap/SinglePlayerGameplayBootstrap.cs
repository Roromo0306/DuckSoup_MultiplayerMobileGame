
using System;
using DuckSoup.Application.StateMachine;
using DuckSoup.Core.Randomization;
using DuckSoup.Features.Conveyor;
using DuckSoup.Features.GameFeel.Menu;
using DuckSoup.Features.Menu.Models;
using DuckSoup.Features.Menu.Services;
using DuckSoup.Features.Round.Configuration;
using DuckSoup.Features.SinglePlayer.Services;
using DuckSoup.Features.SinglePlayer.States;
using DuckSoup.Features.SinglePlayer.Views;
using DuckSoup.Features.Transitions;
using UnityEngine;

namespace DuckSoup.Bootstrap
{
    public sealed class SinglePlayerGameplayBootstrap : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField]
        private SinglePlayerBoardView boardView;

        [SerializeField]
        private SinglePlayerGameView gameView;

        [Header("Session")]
        [SerializeField, Min(1f)]
        private float gameDuration = 60f;

        [SerializeField, Min(0.1f)]
        private float ingredientRevealSeconds = 3f;

        [SerializeField, Min(0.1f)]
        private float recipeReplacementSeconds = 1.5f;

        [SerializeField, Min(0.1f)]
        private float tokenRefillSeconds = 1.2f;

        [SerializeField, Min(0.1f)]
        private float timeUpDisplaySeconds = 2f;

        [SerializeField]
        private int randomSeed = 2026;

        [Header("Conveyor Table")]
        [SerializeField]
        private ConveyorBeltTable conveyorBelt;

        [SerializeField]
        private string conveyorTableId = "conveyor_table";

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
                    "[1P] Faltan referencias de las vistas.",
                    this);

                enabled = false;
                return;
            }

            try
            {
                // Recuperar la mesa seleccionada en el menu.
                if (!GameLaunchHandoff.TryConsume(
                    out launchRequest))
                {
                    launchRequest =
                        GameLaunchRequest.ForSinglePlayer(
                            defaultTableId);
                }

                if (launchRequest.Mode != GameMode.SinglePlayer)
                {
                    throw new InvalidOperationException(
                        "La peticion no corresponde a 1P.");
                }

                // NUEVO: detectar mesa conveyor.
                bool useConveyor = string.Equals(
                    launchRequest.TableId,
                    conveyorTableId,
                    StringComparison.Ordinal);

                if (useConveyor && conveyorBelt == null)
                {
                    throw new InvalidOperationException(
                        "Falta ConveyorBeltTable en el Inspector.");
                }

                if (conveyorBelt != null)
                {
                    conveyorBelt.Configure(useConveyor);
                }

                // Sesion de gameplay que ya utilizabamos.
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

                boardView.Initialize(session);
                gameView.Initialize(session);

                // Estados del gameplay.
                stateMachine = new GameStateMachine();

                countdownState =
                    new SinglePlayerCountdownState(
                        gameView,
                        () => stateMachine.RequestStateChange(
                            playingState));

                playingState =
                    new SinglePlayerPlayingState(
                        session,
                        gameView,
                        () => stateMachine.RequestStateChange(
                            timeUpState));

                timeUpState =
                    new SinglePlayerTimeUpState(
                        session,
                        gameView,
                        SaveFinalResult,
                        ReturnToMainMenu,
                        timeUpDisplaySeconds);

                initialized = true;

                Debug.Log(
                    $"[1P] Mesa: {launchRequest.TableId}. " +
                    $"Conveyor: {useConveyor}",
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
            if (initialized)
            {
                stateMachine.RequestStateChange(
                    countdownState);
            }
        }

        private void Update()
        {
            if (initialized)
            {
                stateMachine.Tick(Time.deltaTime);
            }
        }

        private void LateUpdate()
        {
            if (initialized)
            {
                stateMachine.ApplyPendingTransition();
            }
        }

        private void SaveFinalResult()
        {
            if (resultSaved)
                return;

            try
            {
                int previousTotal =
                    progressStore.GetTotalRecipesCompleted();

                // TableId permite guardar un record
                // diferente para cada tipo de mesa.
                progressStore.RegisterSinglePlayerRun(
                    launchRequest.TableId,
                    session.CompletedRecipes);

                resultSaved = true;

                SinglePlayerRewardHandoff.Queue(
                    previousTotal,
                    session.CompletedRecipes);

                Debug.Log(
                    $"[1P] Guardadas " +
                    $"{session.CompletedRecipes} recetas.",
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
                    "[1P] MainMenuScene no esta en Build Settings.",
                    this);

                return;
            }

            SceneFadeTransition.LoadScene(mainMenuSceneName);
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
