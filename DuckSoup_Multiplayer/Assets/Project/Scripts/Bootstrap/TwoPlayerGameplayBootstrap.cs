using System;
using DuckSoup.Application.StateMachine;
using DuckSoup.Core.Randomization;
using DuckSoup.Features.Conveyor;
using DuckSoup.Features.PowerUps;
using DuckSoup.Features.Menu.Models;
using DuckSoup.Features.Menu.Services;
using DuckSoup.Features.Round.Configuration;
using DuckSoup.Features.Transitions;
using DuckSoup.Features.TwoPlayer.Contracts;
using DuckSoup.Features.TwoPlayer.Services;
using DuckSoup.Features.TwoPlayer.States;
using DuckSoup.Features.TwoPlayer.Views;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DuckSoup.Bootstrap
{
    public sealed class TwoPlayerGameplayBootstrap : MonoBehaviour
    {
        [Header("Power-ups (optional)")]
        [SerializeField] private PowerUpSettings powerUpSettings;
        [SerializeField] private PowerUpHud powerUpHud;

        [Header("Views")]
        [SerializeField] private TwoPlayerBoardView boardView;
        [SerializeField] private TwoPlayerGameView gameView;
        [SerializeField] private TwoPlayerResultsView resultsView;

        [Header("Timing (each round)")]
        [SerializeField, Min(1f)] private float gameDurationSeconds = 60f;
        [SerializeField, Min(0.1f)] private float ingredientRevealSeconds = 3f;
        [SerializeField, Min(0.1f)] private float tokenRefillSeconds = 1.2f;
        [SerializeField, Min(0.1f)] private float recipeReplacementSeconds = 1.5f;
        [SerializeField, Min(0.1f)] private float timeUpMessageSeconds = 1.5f;
        [SerializeField, Min(0.1f)] private float roundIntroSeconds = 1.25f;
        [SerializeField, Min(0.1f)] private float betweenRoundsSeconds = 2f;

        [Header("Conveyor - second round only")]
        [SerializeField] private ConveyorBeltTable conveyorBelt;

        [Header("Other")]
        [SerializeField] private int randomSeed = 2026;
        [SerializeField] private string mainMenuSceneName = "MainMenuScene";

        private GameStateMachine machine;
        private TwoPlayerSession session;

        private IGameState roundIntro;
        private IGameState countdown;
        private IGameState playing;
        private IGameState timeUp;
        private IGameState intermission;
        private IGameState results;

        private bool initialized;
        private int currentRound;

        private void Awake()
        {
            Time.timeScale = 1f;

            if (boardView == null ||
                gameView == null ||
                resultsView == null)
            {
                Debug.LogError(
                    "[2P] Asigna Board, Game y Results.",
                    this);

                enabled = false;
                return;
            }

            try
            {
                if (GameLaunchHandoff.TryConsume(
                    out GameLaunchRequest request))
                {
                    TwoPlayerMatchProgress.Begin(request);
                }
                else if (!TwoPlayerMatchProgress.IsActive ||
                         TwoPlayerMatchProgress.IsComplete)
                {
                    TwoPlayerMatchProgress.Begin(
                        GameLaunchRequest.ForTwoPlayers(
                            "classic_table",
                            0,
                            1));
                }

                currentRound = TwoPlayerMatchProgress.CurrentRound;

                bool useConveyor = currentRound == 2;

                if (useConveyor && conveyorBelt == null)
                {
                    throw new InvalidOperationException(
                        "Ronda 2 requiere Conveyor Belt " +
                        "asignado en el Inspector.");
                }

                if (conveyorBelt != null)
                    conveyorBelt.Configure(useConveyor);

                session = new TwoPlayerSession(
                    PrototypeRecipeCatalog.Create(),
                    new SystemRandomSource(
                        randomSeed + currentRound * 101),
                    gameDurationSeconds,
                    ingredientRevealSeconds,
                    tokenRefillSeconds,
                    recipeReplacementSeconds);

                if (powerUpSettings == null)
                    powerUpSettings = GetComponent<PowerUpSettings>();

                if (powerUpSettings != null)
                    session.EnablePowerUps(powerUpSettings.CreateRules());

                if (powerUpHud != null)
                {
                    powerUpHud.Initialize(session);
                    boardView.SetPowerUpHud(powerUpHud);
                }

                boardView.Initialize(session);

                string roundTitle = useConveyor
                    ? "ROUND 2\nCONVEYOR TABLE"
                    : "ROUND 1\nCLASSIC TABLE";

                gameView.Initialize(session, roundTitle);

                machine = new GameStateMachine();

                roundIntro = new TextIntroState(
                    gameView,
                    () => roundTitle,
                    roundIntroSeconds,
                    () => machine.RequestStateChange(countdown));

                countdown = new TwoPlayerCountdownState(
                    gameView,
                    () => machine.RequestStateChange(playing));

                playing = new TwoPlayerPlayingState(
                    session,
                    gameView,
                    () => machine.RequestStateChange(timeUp));

                timeUp = new TwoPlayerTimeUpState(
                    gameView,
                    RoundFinished,
                    timeUpMessageSeconds);

                intermission = new TextIntroState(
                    gameView,
                    () => "ROUND 1 COMPLETE!",
                    betweenRoundsSeconds,
                    LoadNextRound);

                results = new AggregateResultsState(resultsView);

                resultsView.NextRequested += ReturnToMainMenu;

                initialized = true;

                Debug.Log(
                    $"[2P] Ronda {currentRound}/2. " +
                    $"Conveyor: {useConveyor}.",
                    this);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (initialized)
                machine.RequestStateChange(roundIntro);
        }

        private void Update()
        {
            if (!initialized)
                return;

            machine.Tick(Time.deltaTime);

            if (conveyorBelt != null)
            {
                conveyorBelt.UpdateSpeedFromRemainingTime(
                    session.RemainingSeconds);
            }
        }

        private void LateUpdate()
        {
            if (initialized)
                machine.ApplyPendingTransition();
        }

        private void RoundFinished()
        {
            bool hasAnotherRound =
                TwoPlayerMatchProgress.CompleteCurrentRound(
                    session.Player1Score,
                    session.Player2Score);

            if (hasAnotherRound)
            {
                machine.RequestStateChange(intermission);
            }
            else
            {
                machine.RequestStateChange(results);
            }
        }

        private void LoadNextRound()
        {
            string sceneName =
                SceneManager.GetActiveScene().name;

            if (!UnityEngine.Application.CanStreamedLevelBeLoaded(
                sceneName))
            {
                Debug.LogError(
                    "[2P] Falta la escena 2P en Build Settings.",
                    this);

                return;
            }

            SceneFadeTransition.LoadScene(sceneName);
        }

        private void ReturnToMainMenu()
        {
            if (!UnityEngine.Application.CanStreamedLevelBeLoaded(
                mainMenuSceneName))
            {
                Debug.LogError(
                    "[2P] Falta MainMenuScene en Build Settings.",
                    this);

                return;
            }

            TwoPlayerMatchProgress.Clear();

            SceneFadeTransition.LoadScene(mainMenuSceneName);
        }

        private void OnApplicationPause(bool applicationPaused)
        {
            if (applicationPaused)
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
            machine?.Stop();
        }

        private void OnDestroy()
        {
            if (resultsView != null)
                resultsView.NextRequested -= ReturnToMainMenu;
        }

        private sealed class TextIntroState : IGameState
        {
            private readonly ITwoPlayerFlowView view;
            private readonly Func<string> messageFactory;
            private readonly float seconds;
            private readonly Action completed;

            private float elapsed;
            private bool requested;

            public TextIntroState(
                ITwoPlayerFlowView view,
                Func<string> message,
                float seconds,
                Action completed)
            {
                this.view = view;
                messageFactory = message;
                this.seconds = seconds;
                this.completed = completed;
            }

            public void Enter()
            {
                elapsed = 0f;
                requested = false;

                view.SetInputEnabled(false);
                view.ShowOverlay(messageFactory());
            }

            public void Tick(float deltaTime)
            {
                if (requested)
                    return;

                elapsed += deltaTime;

                if (elapsed >= seconds)
                {
                    requested = true;
                    completed.Invoke();
                }
            }

            public void Exit()
            {
                view.HideOverlay();
            }
        }

        private sealed class AggregateResultsState : IGameState
        {
            private readonly TwoPlayerResultsView resultsView;

            public AggregateResultsState(
                TwoPlayerResultsView results)
            {
                resultsView = results;
            }

            public void Enter()
            {
                GameLaunchRequest request =
                    TwoPlayerMatchProgress.Request;

                resultsView.Show(
                    request.Player1DuckIndex,
                    request.Player2DuckIndex,
                    TwoPlayerMatchProgress.Player1Total,
                    TwoPlayerMatchProgress.Player2Total);
            }

            public void Tick(float deltaTime)
            {
            }

            public void Exit()
            {
                resultsView.Hide();
            }
        }
    }
}