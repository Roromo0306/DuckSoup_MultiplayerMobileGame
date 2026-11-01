
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.Audio
{
    [DisallowMultipleComponent]
    public sealed class CanvasButtonSoundInstaller :
        MonoBehaviour
    {
        private void Awake()
        {
            // Busca botones activos e inactivos.
            Button[] buttons =
                GetComponentsInChildren<Button>(true);

            foreach (Button button in buttons)
            {
                UIButtonSound sound =
                    button.GetComponent<UIButtonSound>();

                if (sound == null)
                {
                    sound = button.gameObject
                        .AddComponent<UIButtonSound>();
                }

                string name =
                    button.gameObject.name.ToLowerInvariant();

                bool isClose =
                    name.Contains("close") ||
                    name.Contains("back") ||
                    name.Contains("cancel") ||
                    name.Contains("return");

                sound.ConfigureDefault(
                    isClose
                        ? DuckSoupSoundId.CloseWindow
                        : DuckSoupSoundId.ButtonClick);
            }
        }
    }
}

