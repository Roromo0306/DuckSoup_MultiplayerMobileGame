
using System;
using DuckSoup.Features.Menu.Contracts;
using DuckSoup.Features.Menu.Controllers;
using DuckSoup.Features.Menu.Models;
using DuckSoup.Features.Menu.Services;
using DuckSoup.Features.Menu.Views;
using UnityEngine;

namespace DuckSoup.Bootstrap
{
    /// <summary>
    /// Punto de composición del menú principal.
    ///
    /// Construye los servicios, el controlador
    /// y conecta las vistas de selección.
    /// </summary>
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
                // Limpiamos cualquier petición antigua.
                GameLaunchHandoff.Clear();

                // Servicios.
                progressStore =
                    new PlayerPrefsProgressStore();

                modeStore =
                    new PlayerPrefsMenuModeStore();

                // Controlador.
                controller = new MainMenuController(
                    progressStore,
                    modeStore,
                    fakePlayerName);

                // Selector de mesas.
                tableSelectorView.Initialize(
                    progressStore,
                    HandleTableConfirmed);

                // Selector de patitos.
                duckSelectionView.Initialize(
                    HandleDucksConfirmed);

                // Vista principal.
                mainMenuView.Initialize(
                    controller,
                    tableSelectorView,
                    duckSelectionView);

                Debug.Log(
                    "[MenuBootstrap] Main Menu ready. " +
                    $"Mode: {controller.SelectedMode}",
                    this);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                enabled = false;
            }
        }

        /// <summary>
        /// Preparación de una partida individual.
        /// </summary>
        private void HandleTableConfirmed(
            TableDisplayData table)
        {
            GameLaunchRequest request =
                GameLaunchRequest.ForSinglePlayer(
                    table.Id);

            GameLaunchHandoff.Prepare(request);

            Debug.Log(
                "[GameLaunch] Single Player. " +
                $"Table: {request.TableId}",
                this);

            // Todavía no navegamos al gameplay antiguo.
            mainMenuView.ShowNotice(
                $"TABLE: {table.DisplayName}\n" +
                "1 PLAYER READY!\n" +
                "Gameplay connection: next phase.");
        }

        /// <summary>
        /// Preparación de una partida de dos jugadores.
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
                "[GameLaunch] Two Players. " +
                $"P1 Duck: {player1Duck + 1}, " +
                $"P2 Duck: {player2Duck + 1}",
                this);

            // La escena 2P se conectará cuando
            // adaptemos sus reglas de gameplay.
            mainMenuView.ShowNotice(
                "BOTH PLAYERS READY!\n" +
                $"P1: DUCK {player1Duck + 1}\n" +
                $"P2: DUCK {player2Duck + 1}\n" +
                "Gameplay connection: next phase.");
        }
    }
}
