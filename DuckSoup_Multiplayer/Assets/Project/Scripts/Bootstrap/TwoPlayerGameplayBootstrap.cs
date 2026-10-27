using System;
using DuckSoup.Application.StateMachine;
using DuckSoup.Core.Randomization;
using DuckSoup.Features.Menu.Models;
using DuckSoup.Features.Menu.Services;
using DuckSoup.Features.Round.Configuration;
using DuckSoup.Features.TwoPlayer.Services;
using DuckSoup.Features.TwoPlayer.States;
using DuckSoup.Features.TwoPlayer.Views;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DuckSoup.Bootstrap
{
    /// <summary>CompositionRoot de la nueva escena 2P por tiempo.</summary>
    public sealed class TwoPlayerGameplayBootstrap : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] private TwoPlayerBoardView boardView;
        [SerializeField] private TwoPlayerGameView gameView;
        [SerializeField] private TwoPlayerResultsView resultsView;
        [Header("Timing")]
        [SerializeField, Min(1f)] private float gameDurationSeconds = 60f;
        [SerializeField, Min(0.1f)] private float ingredientRevealSeconds = 3f;
        [SerializeField, Min(0.1f)] private float tokenRefillSeconds = 1.2f;
        [SerializeField, Min(0.1f)] private float recipeReplacementSeconds = 0.8f;
        [SerializeField, Min(0.1f)] private float timeUpMessageSeconds = 1.5f;
        [Header("Other")]
        [SerializeField] private int randomSeed = 2026;
        [SerializeField] private string mainMenuSceneName = "MainMenuScene";

        private GameStateMachine machine;
        private bool initialized;
        private IGameState countdown;
        private IGameState playing;
        private IGameState timeUp;
        private IGameState results;

        private void Awake()
        {
            Time.timeScale = 1f;
            if (boardView == null || gameView == null || resultsView == null)
            {
                Debug.LogError("[2P] Asigna Board, Game y Results en el Bootstrap.", this);
                enabled = false;
                return;
            }
            try
            {
                // Si arrancamos directamente desde el Editor, dos patitos de prueba.
                if (!GameLaunchHandoff.TryConsume(out GameLaunchRequest request))
                    request = GameLaunchRequest.ForTwoPlayers("classic_table", 0, 1);
                if (request.Mode != GameMode.TwoPlayers)
                    throw new InvalidOperationException("La peticion no corresponde a 2 Players.");

                var session = new TwoPlayerSession(
                    PrototypeRecipeCatalog.Create(),
                    new SystemRandomSource(randomSeed),
                    gameDurationSeconds,
                    ingredientRevealSeconds,
                    tokenRefillSeconds,
                    recipeReplacementSeconds);

                boardView.Initialize(session);
                gameView.Initialize(session);
                resultsView.NextRequested += ReturnToMainMenu;
                machine = new GameStateMachine();

                countdown = new TwoPlayerCountdownState(
                    gameView, () => machine.RequestStateChange(playing));
                playing = new TwoPlayerPlayingState(
                    session, gameView, () => machine.RequestStateChange(timeUp));
                timeUp = new TwoPlayerTimeUpState(
                    gameView, () => machine.RequestStateChange(results), timeUpMessageSeconds);
                results = new TwoPlayerResultsState(
                    session, resultsView, request.Player1DuckIndex, request.Player2DuckIndex);

                initialized = true;
                Debug.Log($"[2P] Ready. Ducks: {request.Player1DuckIndex + 1} / {request.Player2DuckIndex + 1}", this);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (initialized) machine.RequestStateChange(countdown);
        }

        private void Update()
        {
            if (initialized) machine.Tick(Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (!initialized) return;
            string oldState = machine.CurrentStateName;
            machine.ApplyPendingTransition();
            string current = machine.CurrentStateName;
            if (oldState != current) Debug.Log($"[2P FSM] {oldState} -> {current}", this);
        }

        private void ReturnToMainMenu()
        {
            if (!UnityEngine.Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError("[2P] MainMenuScene no esta en Build Settings.", this);
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
            machine?.Stop();
        }
        private void OnDestroy()
        {
            if (resultsView != null) resultsView.NextRequested -= ReturnToMainMenu;
        }
    }
}
