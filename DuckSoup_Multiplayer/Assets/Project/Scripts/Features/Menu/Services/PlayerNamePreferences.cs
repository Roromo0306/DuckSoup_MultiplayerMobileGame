using System.Text;
using UnityEngine;

namespace DuckSoup.Features.Menu.Services
{
    public static class PlayerNamePreferences
    {
        private const string SaveKey =
            "DuckSoup_Profile_PlayerName";

        public static string Load(string fallbackName)
        {
            string saved = Normalize(
                PlayerPrefs.GetString(SaveKey, ""));

            if (!string.IsNullOrEmpty(saved))
                return saved;

            string fallback = Normalize(fallbackName);

            return string.IsNullOrEmpty(fallback)
                ? "ChefDuck01"
                : fallback;
        }

        public static bool TrySave(
            string value,
            out string savedName)
        {
            savedName = Normalize(value);

            if (string.IsNullOrEmpty(savedName))
                return false;

            PlayerPrefs.SetString(SaveKey, savedName);
            PlayerPrefs.Save();

            return true;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            var result = new StringBuilder(value.Length);

            foreach (char character in value)
            {
                if (character == '\r' ||
                    character == '\n' ||
                    character == '\t')
                {
                    result.Append(' ');
                }
                else if (!char.IsControl(character))
                {
                    result.Append(character);
                }
            }

            return result.ToString().Trim();
        }
    }
}