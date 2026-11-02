
using System;
using DuckSoup.Application.StateMachine;
using DuckSoup.Core.Randomization;
using DuckSoup.Features.Conveyor;
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
        [Header("Views")]
        [SerializeField]
        private TwoPlayerBoardView boardView;

        [SerializeField]
        private TwoPlayerGameView gameView;

        [SerializeField]
        private TwoPlayerResultsView resultsView;

        [Header("Timing - Each Round")]
        [SerializeField, Min(1f)]
        private float gameDurationSeconds = 60f;

        [SerializeField, Min(0.1f)]
        private float ingredientRevealSeconds = 3f;

        [SerializeField, Min(0.1f)]
        private float tokenRefillSeconds = 1.2f;

        [SerializeField, Min(0.1f)]
        private float recipeReplacementSeconds = 1.5f;

        [SerializeField, Min(0.1f)]
        private float timeUpMessageSeconds = 1.5f;

        [SerializeField, Min(0.1f)]
        private float roundIntroSeconds = 1.25f;

        [SerializeField, Min(0.1f)]
        private float betweenRoundsSeconds = 2f;

        [Header("Conveyor - Round 2")]
        [SerializeField]
        private ConveyorBeltTable conveyorBelt;

        [Header("Other")]
        [SerializeField]
        private int randomSeed = 2026;

        [SerializeField]
        private string mainMenuSceneName = "MainMenuScene";

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
                    "[2P] Faltan referencias de las vistas.",
                    this);

                enabled = false;
                return;
            }

            try
            {
                // Si venimos del menu, comenzamos
                // una partida nueva de dos rondas.
                if (GameLaunchHandoff.TryConsume(
                    out GameLaunchRequest request))
                {
                    TwoPlayerMatchProgress.Begin(request);
                }
                else if (!TwoPlayerMatchProgress.IsActive ||
                         TwoPlayerMatchProgress.IsComplete)
                {
                    // Permite probar directamente
                    // desde el Editor de Unity.
                    TwoPlayerMatchProgress.Begin(
                        GameLaunchRequest.ForTwoPlayers(
                            "classic_table", 0, 1));
                }

                currentRound =
                    TwoPlayerMatchProgress.CurrentRound;

                // La primera ronda es normal.
                // La segunda utiliza las cintas.
                bool useConveyor = currentRound == 2;

                if (useConveyor && conveyorBelt == null)
                {
                    throw new InvalidOperationException(
                        "Falta ConveyorBeltTable para Round 2.");
                }

                if (conveyorBelt != null)
                {
                    conveyorBelt.Configure(useConveyor);
                }

                // Creamos una sesion nueva por ronda.
                session = new TwoPlayerSession(
                    PrototypeRecipeCatalog.Create(),
                    new SystemRandomSource(
                        randomSeed + currentRound * 101),
                    gameDurationSeconds,
                    ingredientRevealSeconds,
                    tokenRefillSeconds,
                    recipeReplacementSeconds);

                boardView.Initialize(session);
                gameView.Initialize(session);

                resultsView.NextRequested +=
                    ReturnToMainMenu;

                machine = new GameStateMachine();

                // Presentacion de la mesa actual.
                roundIntro = new TextIntroState(
                    gameView,
                    () => currentRound == 1
                        ? "ROUND 1\nCLASSIC TABLE"
                        : "ROUND 2\nCONVEYOR TABLE",
                    roundIntroSeconds,
                    () => machine.RequestStateChange(
                        countdown));

                // Countdown original del juego.
                countdown = new TwoPlayerCountdownState(
                    gameView,
                    () => machine.RequestStateChange(
                        playing));

                // Gameplay original de dos jugadores.
                playing = new TwoPlayerPlayingState(
                    session,
                    gameView,
                    () => machine.RequestStateChange(
                        timeUp));

                timeUp = new TwoPlayerTimeUpState(
                    gameView,
                    RoundFinished,
                    timeUpMessageSeconds);

                // Aviso entre ambas rondas.
                intermission = new TextIntroState(
                    gameView,
                    () => "ROUND 1 COMPLETE!\n" +
                        TwoPlayerMatchProgress.Player1Total +
                        " - " +
                        TwoPlayerMatchProgress.Player2Total +
                        "\nNEXT: CONVEYOR TABLE",
                    betweenRoundsSeconds,
                    LoadNextRound);

                // Resultado final acumulado.
                results = new AggregateResultsState(
                    resultsView);

                initialized = true;

                Debug.Log(
                    $"[2P] Ronda {currentRound}/2. " +
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
                machine.RequestStateChange(roundIntro);
            }
        }

        private void Update()
        {
            if (initialized)
            {
                machine.Tick(Time.deltaTime);
            }
        }

        private void LateUpdate()
        {
            if (initialized)
            {
                machine.ApplyPendingTransition();
            }
        }

        private void RoundFinished()
        {
            // Guardamos los puntos de esta ronda.
            bool hasAnotherRound =
                TwoPlayerMatchProgress.CompleteCurrentRound(
                    session.Player1Score,
                    session.Player2Score);

            if (hasAnotherRound)
            {
                // Acaba Round 1 y pasamos a cintas.
                machine.RequestStateChange(intermission);
            }
            else
            {
                // Acaba Round 2: resultados finales.
                machine.RequestStateChange(results);
            }
        }

        private void LoadNextRound()
        {
            // Recargamos la MISMA escena para
            // generar un tablero completamente nuevo.
            string sceneName =
                SceneManager.GetActiveScene().name;

            if (!UnityEngine.Application
                .CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(
                    "[2P] La escena no esta en Build Settings.",
                    this);

                return;
            }

            SceneFadeTransition.LoadScene(sceneName);
        }

        private void ReturnToMainMenu()
        {
            if (!UnityEngine.Application
                .CanStreamedLevelBeLoaded(
                    mainMenuSceneName))
            {
                Debug.LogError(
                    "[2P] MainMenuScene no esta en Build Settings.",
                    this);

                return;
            }

            // Limpiamos el resultado acumulado.
            TwoPlayerMatchProgress.Clear();

            SceneFadeTransition.LoadScene(
                mainMenuSceneName);
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
            machine?.Stop();
        }

        private void OnDestroy()
        {
            if (resultsView != null)
            {
                resultsView.NextRequested -=
                    ReturnToMainMenu;
            }
        }

        // Estado sencillo para presentar
        // el nombre de la mesa y el cambio de ronda.
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

        // Reutiliza la pantalla WINNER/LOSER,
        // pero con las puntuaciones acumuladas.
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
