
using System;
using TMPro;
using UnityEngine;

namespace DuckSoup.Features.Audio
{
    [DisallowMultipleComponent]
    public sealed class GameplayOverlaySound :
        MonoBehaviour
    {
        [SerializeField]
        private TMP_Text overlayText;

        [Header("Countdown Audio")]
        [Tooltip(
            "True: un audio completo de cuenta atras. " +
            "False: ticks individuales y sonido GO.")]
        [SerializeField]
        private bool useRecordedCountdown = true;

        private string lastMessage;

        private void Awake()
        {
            if (overlayText == null)
                overlayText = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            lastMessage = null;
            TryPlayChangedText();
        }

        private void LateUpdate()
        {
            TryPlayChangedText();
        }

        private void TryPlayChangedText()
        {
            if (overlayText == null)
                return;

            string current =
                (overlayText.text ?? "").Trim();

            if (string.Equals(
                current,
                lastMessage,
                StringComparison.Ordinal))
            {
                return;
            }

            lastMessage = current;

            // Sonido del final de partida.
            if (current.StartsWith(
                "TIME UP",
                StringComparison.OrdinalIgnoreCase))
            {
                DuckSoupSoundPlayer.Play(
                    DuckSoupSoundId.TimeUp);

                return;
            }

            if (useRecordedCountdown)
            {
                // Una grabacion completa:
                // 3... 2... 1... DUCK SOUP!
                if (current == "3")
                {
                    DuckSoupSoundPlayer.Play(
                        DuckSoupSoundId.CountdownSequence);
                }
            }
            else
            {
                // Sonidos independientes.
                if (current == "3" ||
                    current == "2" ||
                    current == "1")
                {
                    DuckSoupSoundPlayer.Play(
                        DuckSoupSoundId.CountdownTick);
                }
                else if (current.Equals(
                    "DUCK SOUP!",
                    StringComparison.OrdinalIgnoreCase))
                {
                    DuckSoupSoundPlayer.Play(
                        DuckSoupSoundId.CountdownGo);
                }
            }
        }
    }
}
