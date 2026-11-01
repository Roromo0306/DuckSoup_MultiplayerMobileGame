
using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace DuckSoup.Features.Audio
{
    [DisallowMultipleComponent]
    public sealed class TwoPlayerResultsSound :
        MonoBehaviour
    {
        [Header("Results Texts")]
        [SerializeField]
        private TMP_Text player1StatusText;

        [SerializeField]
        private TMP_Text player2StatusText;

        [Header("Confetti")]
        [SerializeField, Min(0f)]
        private float confettiSoundDelay = 0.15f;

        private Coroutine pending;

        private void OnEnable()
        {
            if (player1StatusText == null ||
                player2StatusText == null)
            {
                return;
            }

            bool hasWinner =
                IsWinner(player1StatusText.text) ||
                IsWinner(player2StatusText.text);

            if (!hasWinner)
                return;

            // Sonido de victoria.
            DuckSoupSoundPlayer.Play(
                DuckSoupSoundId.Winner);

            pending = StartCoroutine(
                PlayConfettiLater());
        }

        private static bool IsWinner(string text)
        {
            return string.Equals(
                (text ?? "").Trim(),
                "WINNER",
                StringComparison.OrdinalIgnoreCase);
        }

        private IEnumerator PlayConfettiLater()
        {
            if (confettiSoundDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    confettiSoundDelay);
            }
            else
            {
                yield return null;
            }

            DuckSoupSoundPlayer.Play(
                DuckSoupSoundId.Confetti);

            pending = null;
        }

        private void OnDisable()
        {
            if (pending != null)
            {
                StopCoroutine(pending);
                pending = null;
            }
        }
    }
}

