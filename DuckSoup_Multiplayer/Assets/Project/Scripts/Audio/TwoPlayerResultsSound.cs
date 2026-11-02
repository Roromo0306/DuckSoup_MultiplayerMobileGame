
using System;
using System.Collections;
using DuckSoup.Features.Transitions;
using TMPro;
using UnityEngine;

namespace DuckSoup.Features.Audio
{
    /// <summary>
    /// Gestiona la música de victoria y
    /// el sonido del confeti.
    ///
    /// La música se reproduce desde un
    /// AudioSource independiente.
    ///
    /// SceneFadeTransition se encarga
    /// de apagarla suavemente al cambiar
    /// de escena.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TwoPlayerResultsSound : MonoBehaviour
    {
        [Header("Results Texts")]
        [SerializeField]
        private TMP_Text player1StatusText;

        [SerializeField]
        private TMP_Text player2StatusText;

        [Header("Victory Music")]
        [SerializeField]
        private SceneMusicFader victoryMusicFader;

        [Header("Confetti")]
        [SerializeField, Min(0f)]
        private float confettiSoundDelay = 0.15f;

        private Coroutine pendingConfetti;

        private void OnEnable()
        {
            if (player1StatusText == null ||
                player2StatusText == null)
            {
                Debug.LogWarning(
                    "[ResultsAudio] Faltan los " +
                    "textos de resultados.",
                    this);

                return;
            }

            // Solo hay música de victoria
            // cuando existe un ganador.
            bool hasWinner =
                IsWinner(player1StatusText.text) ||
                IsWinner(player2StatusText.text);

            if (!hasWinner)
                return;

            // Ya no reproducimos Winner
            // desde el reproductor global SFX.
            if (victoryMusicFader != null)
            {
                victoryMusicFader.FadeInAndPlay();
            }
            else
            {
                Debug.LogWarning(
                    "[ResultsAudio] Asigna " +
                    "Victory Music Fader.",
                    this);
            }

            // El confeti sigue utilizando
            // nuestro sistema normal de SFX.
            pendingConfetti = StartCoroutine(
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
            yield return new WaitForSecondsRealtime(
                confettiSoundDelay);

            DuckSoupSoundPlayer.Play(
                DuckSoupSoundId.Confetti);

            pendingConfetti = null;
        }

        private void OnDisable()
        {
            if (pendingConfetti != null)
            {
                StopCoroutine(pendingConfetti);
                pendingConfetti = null;
            }
        }
    }
}
