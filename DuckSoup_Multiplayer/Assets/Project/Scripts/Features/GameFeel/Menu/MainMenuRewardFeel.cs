using System.Collections;
using DuckSoup.Features.GameFeel.UI;
using TMPro;
using UnityEngine;

namespace DuckSoup.Features.GameFeel.Menu
{
    [DefaultExecutionOrder(-300)]
    public sealed class MainMenuRewardFeel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI totalRecipesText;
        [SerializeField] private GameObject rewardPanel;
        [SerializeField] private CanvasGroup rewardCanvasGroup;
        [SerializeField] private TextMeshProUGUI earnedText;
        [SerializeField] private UIPunchFeel rewardIconPunch;

        [Header("Medal reward before recipe banner")]
        [SerializeField] private MainMenuMedalRewardView medalRewardView;

        [Header("Timing")]
        [SerializeField, Min(0f)]
        private float startDelaySeconds = 0.25f;

        [SerializeField, Min(0.05f)]
        private float slideInSeconds = 0.45f;

        [SerializeField, Min(0.1f)]
        private float countSeconds = 1.2f;

        [SerializeField, Min(0f)]
        private float visibleSeconds = 2f;

        [SerializeField, Min(0.05f)]
        private float slideOutSeconds = 0.4f;

        [Header("Movement")]
        [SerializeField, Min(0f)]
        private float offscreenMargin = 40f;

        private RectTransform rewardRect;
        private Vector2 restingPosition;
        private Vector2 hiddenPosition;

        private Coroutine active;

        private bool prepared;
        private bool hasPendingReward;

        private int targetTotal;
        private int playVersion;

        public bool IsPlaying => hasPendingReward;

        private void Awake()
        {
            if (PreparePanel())
                HidePanel();
        }

        private bool PreparePanel()
        {
            if (prepared)
                return true;

            if (rewardPanel == null)
                return false;

            if (transform.IsChildOf(rewardPanel.transform))
            {
                Debug.LogError(
                    "[MainMenuRewardFeel] Coloca este componente " +
                    "en un objeto activo fuera de Reward Panel.",
                    this);

                return false;
            }

            rewardRect =
                rewardPanel.GetComponent<RectTransform>();

            if (rewardRect == null)
            {
                Debug.LogError(
                    "[MainMenuRewardFeel] Reward Panel necesita RectTransform.",
                    this);

                return false;
            }

            if (rewardCanvasGroup == null)
            {
                rewardCanvasGroup =
                    rewardPanel.GetComponent<CanvasGroup>();
            }

            if (rewardCanvasGroup == null)
            {
                rewardCanvasGroup =
                    rewardPanel.AddComponent<CanvasGroup>();
            }

            restingPosition = rewardRect.anchoredPosition;

            rewardCanvasGroup.interactable = false;
            rewardCanvasGroup.blocksRaycasts = false;

            prepared = true;

            return true;
        }

        public void Play(int oldTotal, int earned)
        {
            if (oldTotal < 0 || earned < 0)
                return;

            if (!PreparePanel() ||
                totalRecipesText == null ||
                earnedText == null)
            {
                Debug.LogError(
                    "[MainMenuRewardFeel] Revisa las referencias " +
                    "del panel y los textos.",
                    this);

                return;
            }

            StopAndReset();

            targetTotal = oldTotal + earned;
            hasPendingReward = true;

            // El contador espera hasta cerrar las medallas.
            totalRecipesText.text = oldTotal.ToString();

            earnedText.text =
                $"<size=80%>+{earned}</size>\nRECIPES";

            int version = playVersion;

            if (medalRewardView != null &&
                medalRewardView.TryShowPendingRewards(() =>
                {
                    if (this == null ||
                        version != playVersion ||
                        !hasPendingReward ||
                        !isActiveAndEnabled)
                    {
                        return;
                    }

                    BeginRecipeBanner(oldTotal, earned);
                }))
            {
                // Hay medallas nuevas: esperar a NEXT.
                return;
            }

            if (medalRewardView == null)
            {
                Debug.LogWarning(
                    "[MainMenuRewardFeel] Asigna Medal Reward View " +
                    "para mostrar las medallas antes de las recetas.",
                    this);
            }

            // Sin medallas nuevas: funcionamiento habitual.
            BeginRecipeBanner(oldTotal, earned);
        }

        private void BeginRecipeBanner(int oldTotal, int earned)
        {
            Canvas.ForceUpdateCanvases();

            RectTransform parentRect =
                rewardRect.parent as RectTransform;

            float parentWidth = parentRect != null
                ? parentRect.rect.width
                : rewardRect.rect.width;

            float distance =
                Mathf.Max(0f, parentWidth) +
                rewardRect.rect.width +
                Mathf.Max(0f, offscreenMargin);

            hiddenPosition =
                restingPosition + Vector2.left * distance;

            active = StartCoroutine(
                Animate(oldTotal, earned));
        }

        private IEnumerator Animate(int oldTotal, int earned)
        {
            if (startDelaySeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    startDelaySeconds);
            }

            rewardRect.anchoredPosition = hiddenPosition;
            rewardCanvasGroup.alpha = 1f;

            rewardPanel.SetActive(true);
            rewardRect.SetAsLastSibling();

            yield return Slide(
                hiddenPosition,
                restingPosition,
                slideInSeconds,
                true);

            if (rewardIconPunch != null)
                rewardIconPunch.Punch();

            float elapsed = 0f;

            float countingDuration =
                Mathf.Max(0.1f, countSeconds);

            float holdDuration =
                Mathf.Max(visibleSeconds, countingDuration);

            while (elapsed < holdDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(
                    elapsed / countingDuration);

                float eased =
                    1f - (1f - t) * (1f - t) * (1f - t);

                int earnedNow =
                    Mathf.RoundToInt(earned * eased);

                totalRecipesText.text =
                    (oldTotal + earnedNow).ToString();

                yield return null;
            }

            totalRecipesText.text = targetTotal.ToString();

            yield return Slide(
                restingPosition,
                hiddenPosition,
                slideOutSeconds,
                false);

            hasPendingReward = false;

            HidePanel();

            active = null;
        }

        private IEnumerator Slide(
            Vector2 from,
            Vector2 to,
            float seconds,
            bool entering)
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, seconds);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(
                    elapsed / duration);

                float eased = entering
                    ? 1f - (1f - t) * (1f - t) * (1f - t)
                    : t * t * t;

                rewardRect.anchoredPosition =
                    Vector2.LerpUnclamped(
                        from,
                        to,
                        eased);

                yield return null;
            }

            rewardRect.anchoredPosition = to;
        }

        private void HidePanel()
        {
            if (rewardPanel != null)
                rewardPanel.SetActive(false);

            if (rewardRect != null)
                rewardRect.anchoredPosition = restingPosition;

            if (rewardCanvasGroup != null)
                rewardCanvasGroup.alpha = 1f;
        }

        private void StopAndReset()
        {
            playVersion++;

            if (active != null)
            {
                StopCoroutine(active);
                active = null;
            }

            if (hasPendingReward && totalRecipesText != null)
                totalRecipesText.text = targetTotal.ToString();

            hasPendingReward = false;

            HidePanel();
        }

        private void OnDisable()
        {
            StopAndReset();
        }
    }
}