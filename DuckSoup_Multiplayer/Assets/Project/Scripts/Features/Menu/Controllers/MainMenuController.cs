
using System;
using DuckSoup.Features.Menu.Contracts;
using DuckSoup.Features.Menu.Models;

namespace DuckSoup.Features.Menu.Controllers
{
    /// <summary>
    /// Controla la selección del modo
    /// y proporciona los datos del perfil.
    /// </summary>
    public sealed class MainMenuController
    {
        private readonly IPlayerProgressStore progressStore;
        private readonly IMenuModeStore modeStore;

        public string PlayerName { get; }

        public GameMode SelectedMode { get; private set; }

        public int TotalRecipesCompleted =>
            progressStore.GetTotalRecipesCompleted();

        public MainMenuController(
            IPlayerProgressStore progressStore,
            IMenuModeStore modeStore,
            string playerName)
        {
            this.progressStore = progressStore ??
                throw new ArgumentNullException(
                    nameof(progressStore));

            this.modeStore = modeStore ??
                throw new ArgumentNullException(
                    nameof(modeStore));

            if (string.IsNullOrWhiteSpace(playerName))
            {
                throw new ArgumentException(
                    "El nombre no puede estar vacío.",
                    nameof(playerName));
            }

            PlayerName = playerName.Trim();

            // Recuperamos la última selección.
            SelectedMode = modeStore.LoadMode();
        }

        public void SelectMode(GameMode mode)
        {
            if (mode != GameMode.SinglePlayer &&
                mode != GameMode.TwoPlayers)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(mode));
            }

            if (SelectedMode == mode)
                return;

            SelectedMode = mode;

            // Guardamos la preferencia.
            modeStore.SaveMode(mode);
        }
    }
}
