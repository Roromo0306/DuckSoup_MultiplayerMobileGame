using System;
using System.Collections;
using System.Collections.Generic;
using DuckSoup.Features.GameFeel.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.Menu.Views
{
    [DisallowMultipleComponent]
    public sealed class TableMedalsView : MonoBehaviour
    {
        [Serializable]
        private sealed class MedalImages
        {
            [SerializeField] private Image lockedImage;
            [SerializeField] private Image unlockedImage;

            private Vector3 originalScale;
            private bool scaleCached;

            public void Render(bool unlocked)
            {
                SetUnlockedScale(1f);

                SetVisible(lockedImage, !unlocked);
                SetVisible(unlockedImage, unlocked);
            }

            public void SetUnlockedScale(float multiplier)
            {
                if (unlockedImage == null)
                    return;

                if (!scaleCached)
                {
                    originalScale =
                        unlockedImage.transform.localScale;

                    scaleCached = true;
                }

                unlockedImage.transform.localScale =
                    originalScale * multiplier;
            }

            private static void SetVisible(
                Image image,
                bool visible)
            {
                if (image == null)
                    return;

                image.gameObject.SetActive(true);
                image.raycastTarget = false;
                image.enabled = visible;
            }
        }

        [Header("Bronze")]
        [SerializeField]
        private MedalImages bronze = new MedalImages();

        [Header("Silver")]
        [SerializeField]
        private MedalImages silver = new MedalImages();

        [Header("Gold")]
        [SerializeField]
        private MedalImages gold = new MedalImages();

        [Header("Master")]
        [SerializeField]
        private MedalImages master = new MedalImages();

        [Header("Unlock animation")]

        [SerializeField, Min(0f)]
        private float initialDelaySeconds = 0.25f;

        [SerializeField, Min(0.05f)]
        private float popSeconds = 0.3f;

        [SerializeField, Min(0f)]
        private float betweenMedalsSeconds = 0.12f;

        [SerializeField, Range(0.1f, 1f)]
        private float startScale = 0.7f;

        [SerializeField, Range(1f, 1.5f)]
        private float peakScale = 1.15f;

        private MedalImages[] medals;

        private readonly bool[] unlocked = new bool[4];

        private Coroutine animation;

        private TableDisplayData currentTable;
        private int currentHighScore;

        public void Render(
            TableDisplayData table,
            int highScore)
        {
            highScore = Math.Max(0, highScore);

            // Un refresco del mismo estado no reinicia
            // la animacion que ya se esta reproduciendo.
            if (animation != null &&
                table == currentTable &&
                highScore == currentHighScore)
            {
                return;
            }

            Prepare();
            StopAnimation();

            currentTable = table;
            currentHighScore = highScore;

            unlocked[0] =
                table != null &&
                highScore >= table.BronzeScore;

            unlocked[1] =
                table != null &&
                highScore >= table.SilverScore;

            unlocked[2] =
                table != null &&
                highScore >= table.GoldScore;

            unlocked[3] =
                table != null &&
                highScore >= table.MasterScore;

            ApplyFinalState();

            // El efecto espera hasta que el selector
            // este visible y muestre la mesa correcta.
            if (table == null ||
                !isActiveAndEnabled ||
                !SinglePlayerRewardHandoff.TryConsumeSelectorMedalReward(
                    table.Id,
                    out var reward))
            {
                return;
            }

            List<MedalRewardKind> newMedals =
                SinglePlayerRewardHandoff.GetNewMedals(
                    reward,
                    table.BronzeScore,
                    table.SilverScore,
                    table.GoldScore,
                    table.MasterScore);

            newMedals.RemoveAll(
                kind => !unlocked[(int)kind]);

            if (newMedals.Count == 0)
                return;

            // Las medallas nuevas siguen bloqueadas
            // hasta que les llega su turno.
            foreach (MedalRewardKind kind in newMedals)
            {
                medals[(int)kind]?.Render(false);
            }

            animation = StartCoroutine(
                AnimateUnlocks(newMedals));
        }

        private void Prepare()
        {
            if (medals == null)
            {
                medals = new[]
                {
                    bronze,
                    silver,
                    gold,
                    master
                };
            }
        }

        private IEnumerator AnimateUnlocks(
            List<MedalRewardKind> newMedals)
        {
            if (initialDelaySeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    initialDelaySeconds);
            }

            for (int i = 0; i < newMedals.Count; i++)
            {
                MedalImages medal =
                    medals[(int)newMedals[i]];

                if (medal != null)
                {
                    yield return Pop(medal);
                }

                if (i < newMedals.Count - 1 &&
                    betweenMedalsSeconds > 0f)
                {
                    yield return new WaitForSecondsRealtime(
                        betweenMedalsSeconds);
                }
            }

            ApplyFinalState();

            animation = null;
        }

        private IEnumerator Pop(MedalImages medal)
        {
            medal.Render(true);
            medal.SetUnlockedScale(startScale);

            float elapsed = 0f;

            float duration = Mathf.Max(
                0.05f,
                popSeconds);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(
                    elapsed / duration);

                float scale;

                if (t < 0.65f)
                {
                    float p = t / 0.65f;

                    float eased =
                        1f -
                        (1f - p) *
                        (1f - p) *
                        (1f - p);

                    scale = Mathf.Lerp(
                        startScale,
                        peakScale,
                        eased);
                }
                else
                {
                    float p =
                        (t - 0.65f) / 0.35f;

                    scale = Mathf.Lerp(
                        peakScale,
                        1f,
                        Mathf.SmoothStep(0f, 1f, p));
                }

                medal.SetUnlockedScale(scale);

                yield return null;
            }

            medal.SetUnlockedScale(1f);
        }

        private void ApplyFinalState()
        {
            if (medals == null)
                return;

            for (int i = 0; i < medals.Length; i++)
            {
                medals[i]?.Render(unlocked[i]);
            }
        }

        private void StopAnimation()
        {
            if (animation != null)
            {
                StopCoroutine(animation);
                animation = null;
            }

            ApplyFinalState();
        }

        private void OnDisable()
        {
            StopAnimation();
        }
    }
}