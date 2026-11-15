using System;
using System.Collections;
using DuckSoup.Application.StateMachine;
using DuckSoup.Core.Randomization;
using DuckSoup.Features.Conveyor;
using DuckSoup.Features.PowerUps;
using DuckSoup.Features.GameFeel.Menu;
using DuckSoup.Features.Menu.Models;
using DuckSoup.Features.Menu.Services;
using DuckSoup.Features.Round.Configuration;
using DuckSoup.Features.Settings;
using DuckSoup.Features.SinglePlayer.Services;
using DuckSoup.Features.SinglePlayer.States;
using DuckSoup.Features.SinglePlayer.Views;
using DuckSoup.Features.Transitions;
using UnityEngine;

namespace DuckSoup.Bootstrap
{
    /// <summary>
    /// Modo individual: usa la misma sesion y los mismos prefabs de siempre.
    /// Solo selecciona mesa normal o mesa de cintas en funcion del TableId.
    /// </summary>
    public sealed class SinglePlayerGameplayBootstrap : MonoBehaviour
    {
        [Header("Power-ups (optional)")]
        [SerializeField] private PowerUpSettings powerUpSettings;
        [SerializeField] private PowerUpHud powerUpHud;

        [Header("Views")]
        [SerializeField] private SinglePlayerBoardView boardView;
        [SerializeField] private SinglePlayerGameView gameView;

        [Header("Pause (optional)")]
        [SerializeField] private SettingsPanelView pauseSettings;

        [Header("Session")]
        [SerializeField, Min(1f)] private float gameDuration = 60f;
        [SerializeField, Min(0.1f)] private float ingredientRevealSeconds = 3f;
        [SerializeField, Min(0.1f)] private float recipeReplacementSeconds = 1.5f;
        [SerializeField, Min(0.1f)] private float tokenRefillSeconds = 1.2f;
        [SerializeField, Min(0.1f)] private float timeUpDisplaySeconds = 2f;
        [SerializeField] private int randomSeed = 2026;

        [Header("Conveyor (only for conveyor_table)")]
        [SerializeField] private ConveyorBeltTable conveyorBelt;
        [SerializeField] private string conveyorTableId = "conveyor_table";

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
        private bool paused;
        private bool leaving;
        private float timeScaleBeforePause;
        private Coroutine resumeCountdown;

        private void Awake()
        {
            Time.timeScale = 1f;
            if (boardView == null || gameView == null)
            {
                Debug.LogError("[1P] Asigna Board View y Game View.", this);
                enabled = false;
                return;
            }

            try
            {
                if (!GameLaunchHandoff.TryConsume(out launchRequest))
                    launchRequest = GameLaunchRequest.ForSinglePlayer(defaultTableId);
                if (launchRequest.Mode != GameMode.SinglePlayer)
                    throw new InvalidOperationException("La peticion recibida no es SinglePlayer.");

                bool conveyorSelected =
                    string.Equals(launchRequest.TableId, conveyorTableId,
                        StringComparison.Ordinal);
                if (conveyorSelected && conveyorBelt == null)
                    throw new InvalidOperationException(
                        "Seleccionaste Conveyor Table, pero falta Conveyor Belt en el Inspector.");

                // Configurar ANTES de crear el tablero; la cinta monta sus
                // slots en Start, despues de boardView.Initialize().
                if (conveyorBelt != null)
                    conveyorBelt.Configure(conveyorSelected);

                IRandomSource random = new SystemRandomSource(randomSeed);
                session = new SinglePlayerSession(
                    PrototypeRecipeCatalog.Create(), random, gameDuration,
                    ingredientRevealSeconds, recipeReplacementSeconds,
                    tokenRefillSeconds);

                progressStore = new PlayerPrefsProgressStore();
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
                Debug.Log($"[1P] Mesa: {launchRequest.TableId}; cinta: {conveyorSelected}.", this);
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
                stateMachine.RequestStateChange(countdownState);
        }

        private void Start()
        {
            if (initialized && pauseSettings != null)
                pauseSettings.BindPause(TryPause, BeginResumeCountdown, TryAbandonRun);
        }

        private void Update()
        {
            if (initialized && !paused) stateMachine.Tick(Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (!initialized || paused) return;

            stateMachine.ApplyPendingTransition();
            if (pauseSettings != null)
                pauseSettings.SetOpenButtonAvailable(CanPause());
        }

        private bool CanPause()
        {
            return initialized && isActiveAndEnabled && !paused && !leaving &&
                !session.IsTimeUp &&
                stateMachine.CurrentStateName == nameof(SinglePlayerPlayingState);
        }

        private bool TryPause()
        {
            if (!CanPause()) return false;

            paused = true;
            timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;

            // Bloquear solo el tablero evita reiniciar la musica al reanudar.
            boardView.SetInputEnabled(false);
            pauseSettings.SetOpenButtonAvailable(false);
            return true;
        }

        private void BeginResumeCountdown()
        {
            if (paused && !leaving && resumeCountdown == null && isActiveAndEnabled)
                resumeCountdown = StartCoroutine(ResumeAfterCountdown());
        }

        private IEnumerator ResumeAfterCountdown()
        {
            for (int number = 3; number > 0; number--)
            {
                gameView.ShowOverlay(number.ToString());
                yield return new WaitForSecondsRealtime(1f);
            }

            gameView.HideOverlay();
            boardView.SetInputEnabled(true);
            Time.timeScale = timeScaleBeforePause;
            paused = false;
            resumeCountdown = null;
        }

        private bool TryAbandonRun()
        {
            if (!paused || leaving || resumeCountdown != null) return false;

            // El fundido avanza con la partida pausada.
            leaving = true;
            if (SceneFadeTransition.LoadScene(mainMenuSceneName)) return true;

            leaving = false;
            return false;
        }

        private void SaveFinalResult()
        {
            if (resultSaved || leaving) return;
            try
            {
                int previousTotal = progressStore.GetTotalRecipesCompleted();
                // Se usa TableId: cada mesa guarda SU PROPIO highscore.
                progressStore.RegisterSinglePlayerRun(
                    launchRequest.TableId,
                    session.ActualRecipesCompleted,
                    session.CompletedRecipes);
                resultSaved = true;
                SinglePlayerRewardHandoff.Queue(previousTotal, session.ActualRecipesCompleted);
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
                Debug.LogError("[1P] MainMenuScene falta en Build Settings.", this);
                return;
            }
            SceneFadeTransition.LoadScene(mainMenuSceneName);
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
            if (resumeCountdown != null)
            {
                StopCoroutine(resumeCountdown);
                resumeCountdown = null;
            }
            if (paused)
            {
                Time.timeScale = timeScaleBeforePause;
                paused = false;
                if (gameView != null) gameView.HideOverlay();
            }

            if (pauseSettings != null) pauseSettings.ResetPausePanel();
            if (boardView != null) boardView.SetInputEnabled(false);
            stateMachine?.Stop();
            leaving = false;
        }
    }
}