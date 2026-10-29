using System.Collections;
using TMPro;
using UnityEngine;
using DuckSoup.Features.GameFeel.UI;

namespace DuckSoup.Features.GameFeel.Menu
{
    // Al volver de 1P: icono +X y total incrementandose sin cambiar los datos persistentes.
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

        private Coroutine active;

        private void Awake()
        {
            if (rewardPanel != null) rewardPanel.SetActive(false);
        }

        public void Play(int oldTotal, int earned)
        {
            if (earned < 0 || rewardPanel == null || rewardCanvasGroup == null ||
                earnedText == null || totalRecipesText == null)
                return;

            if (active != null) StopCoroutine(active);
            active = StartCoroutine(Animate(oldTotal, earned));
        }

        private IEnumerator Animate(int from, int earned)
        {
            rewardPanel.SetActive(true);
            rewardCanvasGroup.alpha = 1f;
            if (rewardIconPunch != null) rewardIconPunch.Punch();
            int target = from + earned;
            float elapsed = 0f;
            while (elapsed < countSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / countSeconds);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                int currentEarned = Mathf.RoundToInt(earned * eased);
                totalRecipesText.text = "TOTAL RECIPES: " + (from + currentEarned);
                earnedText.text = "+" + currentEarned + " RECIPES";
                yield return null;
            }
            totalRecipesText.text = "TOTAL RECIPES: " + target;
            earnedText.text = "+" + earned + " RECIPES";
            if (visibleSeconds > 0f)
                yield return new WaitForSecondsRealtime(visibleSeconds);

            elapsed = 0f;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                rewardCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeSeconds);
                yield return null;
            }
            rewardPanel.SetActive(false);
            rewardCanvasGroup.alpha = 1f;
            active = null;
        }

        private void OnDisable()
        {
            if (active != null) StopCoroutine(active);
            active = null;
            if (rewardPanel != null) rewardPanel.SetActive(false);
        }
    }
}
