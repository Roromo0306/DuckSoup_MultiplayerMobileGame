using System;
using DuckSoup.Features.Menu.Contracts;
using DuckSoup.Features.Menu.Models;
using DuckSoup.Features.Menu.Services;

namespace DuckSoup.Features.Menu.Controllers
{
    public sealed class MainMenuController
    {
        private readonly IPlayerProgressStore progressStore;
        private readonly IMenuModeStore modeStore;

        private readonly string defaultPlayerName;

        public string PlayerName =>
            PlayerNamePreferences.Load(defaultPlayerName);

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
                    "El nombre no puede estar vacio.",
                    nameof(playerName));
            }

            defaultPlayerName = playerName.Trim();

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
            modeStore.SaveMode(mode);
        }
    }
}