
using System;
using DuckSoup.Features.Menu.Contracts;
using DuckSoup.Features.Menu.Models;
using UnityEngine;

namespace DuckSoup.Features.Menu.Services
{
    /// <summary>
    /// Guarda el modo seleccionado
    /// utilizando PlayerPrefs.
    /// </summary>
    public sealed class PlayerPrefsMenuModeStore :
        IMenuModeStore
    {
        private const string SelectedModeKey =
            "DuckSoup_Menu_SelectedMode";

        public GameMode LoadMode()
        {
            int savedValue = PlayerPrefs.GetInt(
                SelectedModeKey,
                (int)GameMode.SinglePlayer);

            if (savedValue != (int)GameMode.SinglePlayer &&
                savedValue != (int)GameMode.TwoPlayers)
            {
                return GameMode.SinglePlayer;
            }

            return (GameMode)savedValue;
        }

        public void SaveMode(GameMode mode)
        {
            if (mode != GameMode.SinglePlayer &&
                mode != GameMode.TwoPlayers)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(mode));
            }

            PlayerPrefs.SetInt(
                SelectedModeKey,
                (int)mode);

            PlayerPrefs.Save();
        }
    }
}
