using System.Collections;
using TMPro;
using UnityEngine;
using DuckSoup.Features.GameFeel.UI;

namespace DuckSoup.Features.GameFeel.Menu
{
    // El total cuenta hacia arriba; el aviso +X aparece con fade-in,
    // se desplaza hacia arriba y desaparece con fade-out.
    [DefaultExecutionOrder(-300)]
    public sealed class MainMenuRewardFeel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI totalRecipesText;
        [SerializeField] private GameObject rewardPanel;
        [SerializeField] private CanvasGroup rewardCanvasGroup;
        [SerializeField] private TextMeshProUGUI earnedText;
        [SerializeField] private UIPunchFeel rewardIconPunch;
        [SerializeField, Min(0.1f)] private float countSeconds = 1.2f;
        [SerializeField, Min(0f)] private float visibleSeconds = 0.7f;
        [SerializeField, Min(0.1f)] private float fadeSeconds = 0.35f;
        [Header("New: floating entrance")]
        [SerializeField, Min(0.01f)] private float fadeInSeconds = 0.22f;
        [SerializeField, Min(0f)] private float riseDistance = 65f;

        private RectTransform rewardRect;
        private Vector2 restingPosition;
        private Coroutine active;

        private void Awake()
        {
            if (rewardPanel != null)
            {
                rewardRect = rewardPanel.GetComponent<RectTransform>();
                if (rewardRect != null) restingPosition = rewardRect.anchoredPosition;
                rewardPanel.SetActive(false);
            }
        }

        public void Play(int oldTotal, int earned)
        {
            if (earned < 0 || rewardPanel == null || rewardCanvasGroup == null ||
                earnedText == null || totalRecipesText == null) return;
            if (active != null) StopCoroutine(active);
            active = StartCoroutine(Animate(oldTotal, earned));
        }

        private IEnumerator Animate(int from, int earned)
        {
            rewardPanel.SetActive(true);
            rewardCanvasGroup.alpha = 0f;
            if (rewardRect != null) rewardRect.anchoredPosition = restingPosition;
            if (rewardIconPunch != null) rewardIconPunch.Punch();
            float elapsed = 0f;
            float total = Mathf.Max(countSeconds, fadeInSeconds + visibleSeconds + fadeSeconds);
            int target = from + earned;

            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                float countT = Mathf.Clamp01(elapsed / countSeconds);
                float eased = 1f - Mathf.Pow(1f - countT, 3f);
                int earnedNow = Mathf.RoundToInt(earned * eased);
                totalRecipesText.text = "TOTAL RECIPES: " + (from + earnedNow);
                earnedText.text = "+" + earned + " RECIPES";

                float alpha = Mathf.Clamp01(elapsed / fadeInSeconds);
                float fadeStart = total - fadeSeconds;
                if (elapsed > fadeStart)
                    alpha = 1f - Mathf.Clamp01((elapsed - fadeStart) / fadeSeconds);
                rewardCanvasGroup.alpha = alpha;
                if (rewardRect != null)
                {
                    float moveT = Mathf.SmoothStep(0f, 1f, elapsed / total);
                    rewardRect.anchoredPosition = restingPosition + Vector2.up * (riseDistance * moveT);
                }
                yield return null;
            }

            totalRecipesText.text = "TOTAL RECIPES: " + target;
            rewardPanel.SetActive(false);
            rewardCanvasGroup.alpha = 1f;
            if (rewardRect != null) rewardRect.anchoredPosition = restingPosition;
            active = null;
        }

        private void OnDisable()
        {
            if (active != null) StopCoroutine(active);
            active = null;
            if (rewardPanel != null) rewardPanel.SetActive(false);
            if (rewardRect != null) rewardRect.anchoredPosition = restingPosition;
        }
    }
}
