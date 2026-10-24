
using DuckSoup.Application.StateMachine;
using DuckSoup.Core.Randomization;
using DuckSoup.Features.Match.Contracts;
using DuckSoup.Features.Match.Models;
using DuckSoup.Features.Match.Services;
using DuckSoup.Features.Match.States;
using DuckSoup.Features.Match.Views;
using DuckSoup.Features.Round.Configuration;
using DuckSoup.Features.Round.Contracts;
using DuckSoup.Features.Round.Controllers;
using DuckSoup.Features.Round.Models;
using DuckSoup.Features.Round.Services;
using DuckSoup.Features.Round.States;
using DuckSoup.Features.Round.Views;
using System;

using UnityEngine;

namespace DuckSoup.Bootstrap
{
    /// <summary>
    /// Construye los sistemas de ronda y partida.
    ///
    /// Mantiene la progresión económica
    /// al generar nuevas rondas.
    /// </summary>
    public sealed class RoundPrototypeCompositionRoot :
        MonoBehaviour
    {
        [Header("Views")]
        [SerializeField]
        private RoundBoardView roundBoard;

        [SerializeField]
        private ResultsScreenView resultsScreen;

        [Header("Round Settings")]
        [SerializeField]
        private int randomSeed = 2026;

        [SerializeField, Min(1)]
        private int cardsPerPlayer = 3;

        [Header("Ingredient Reveal")]
        [SerializeField, Min(0.1f)]
        private float revealDurationSeconds = 3f;

        [Header("Economy")]
        [SerializeField, Min(1)]
        private int coinsPerCompletedRecipe = 3;

        [SerializeField]
        private int[] upgradeThresholds = { 9, 18, 30, 45 };

        private GameStateMachine stateMachine;
        private IGameState targetState;

        private IRoundFactory roundFactory;
        private IRoundSettlementService settlementService;

        private MatchModel match;
        private RoundModel currentRound;

        private RoundGameplayController gameplayController;
        private RoundPreviewState roundState;

        private RoundSettlementResult latestResult;

        private int matchCounter;
        private bool initialized;

        private void Awake()
        {
            if (roundBoard == null ||
                resultsScreen == null)
            {
                Debug.LogError(
                    "[Bootstrap] Faltan vistas por asignar.",
                    this);

                enabled = false;
                return;
            }

            try
            {
                stateMachine = new GameStateMachine();

                resultsScreen.ContinueRequested +=
                    HandleContinueRequested;

                CreateNewMatch();

                targetState = roundState;
                initialized = true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (!initialized || targetState == null)
                return;

            stateMachine.RequestStateChange(targetState);
        }

        private void Update()
        {
            if (!initialized)
                return;

            stateMachine.Tick(Time.deltaTime);

            // Comprobar si la ronda acaba de terminar.
            // latestResult evita liquidarla más de una vez.
            if (latestResult != null ||
                gameplayController == null ||
                !gameplayController.IsFinished)
            {
                return;
            }

            try
            {
                latestResult = settlementService.Settle(
                    currentRound,
                    gameplayController.Winner);

                IGameState nextState;

                if (latestResult.MatchFinished)
                {
                    nextState = new MatchResultsState(
                        resultsScreen,
                        latestResult);
                }
                else
                {
                    nextState = new RoundResultsState(
                        resultsScreen,
                        latestResult);
                }

                RequestTransition(nextState);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                enabled = false;
            }
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

        /// <summary>
        /// Comienza una partida completamente nueva,
        /// con monedas desde cero.
        /// </summary>
        private void CreateNewMatch()
        {
            string matchId =
                "match_" + Guid.NewGuid().ToString("N");

            match = new MatchModel(matchId);

            var progression =
                new RestaurantProgressionPolicy(
                    upgradeThresholds);

            settlementService = new RoundSettlementService(
                match,
                progression,
                coinsPerCompletedRecipe);

            int seed = unchecked(
                randomSeed + matchCounter);

            matchCounter++;

            IRandomSource random =
                new SystemRandomSource(seed);

            roundFactory = new RoundFactory(
                PrototypeRecipeCatalog.Create(),
                random,
                cardsPerPlayer);

            PrepareNextRound();
        }

        /// <summary>
        /// Genera cartas y tokens nuevos,
        /// conservando el progreso de MatchModel.
        /// </summary>
        private void PrepareNextRound()
        {
            int roundNumber =
                match.CompletedRounds + 1;

            string roundId =
                $"{match.Id}_round_{roundNumber:000}";

            currentRound =
                roundFactory.CreateRound(roundId);

            IRoundReadModel readModel =
                new RoundReadModelService(currentRound);

            IRoundInteractionService interaction =
                 new RoundInteractionService(
                    currentRound,
                     revealDurationSeconds);

            gameplayController =
                new RoundGameplayController(
                    readModel,
                    interaction);

            roundBoard.Initialize(gameplayController);

            roundState = new RoundPreviewState(
                readModel,
                roundBoard,
                interaction);

            Debug.Log(
                $"[Match] Preparada ronda {roundNumber}",
                this);
        }

        /// <summary>
        /// Recibe el botón de siguiente ronda
        /// o revancha desde ResultsScreenView.
        /// </summary>
        private void HandleContinueRequested()
        {
            if (latestResult == null)
                return;

            try
            {
                bool restartMatch =
                    latestResult.MatchFinished;

                if (restartMatch)
                {
                    CreateNewMatch();
                }
                else
                {
                    PrepareNextRound();
                }

                latestResult = null;

                RequestTransition(roundState);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private void RequestTransition(IGameState nextState)
        {
            stateMachine.RequestStateChange(nextState);
            targetState = nextState;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                roundBoard?.CancelAllTokenDrags();
            }
        }

        private void OnDisable()
        {
            roundBoard?.CancelAllTokenDrags();
            stateMachine?.Stop();
            resultsScreen?.Hide();
        }

        private void OnDestroy()
        {
            if (resultsScreen != null)
            {
                resultsScreen.ContinueRequested -=
                    HandleContinueRequested;
            }
        }
    }
}
