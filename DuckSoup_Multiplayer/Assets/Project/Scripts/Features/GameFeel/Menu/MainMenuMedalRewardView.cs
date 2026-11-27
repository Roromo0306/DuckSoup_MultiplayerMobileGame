using System;
using System.Collections;
using System.Collections.Generic;
using DuckSoup.Features.Menu.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.GameFeel.Menu
{
    [DefaultExecutionOrder(-250)]
    [DisallowMultipleComponent]
    public sealed class MainMenuMedalRewardView : MonoBehaviour
    {
        [Header("Menu")]
        [SerializeField] private TableSelectorView tableSelector;

        [Header("Popup")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private CanvasGroup popupCanvasGroup;
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private Button nextButton;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI tableNameText;

        [Header("Medal Images")]
        [SerializeField] private Image bronzeImage;
        [SerializeField] private Image silverImage;
        [SerializeField] private Image goldImage;
        [SerializeField] private Image masterImage;

        [Header("Effects")]
        [SerializeField] private RectTransform medalRoot;
        [SerializeField] private RectTransform shineRoot;
        [SerializeField] private float shineDegreesPerSecond = 25f;

        [SerializeField, Min(0f)]
        private float floatDistance = 8f;

        [SerializeField, Min(0f)]
        private float floatSpeed = 2f;

        [Header("Animation")]
        [SerializeField, Min(0.05f)]
        private float openSeconds = 0.45f;

        [SerializeField, Min(0.05f)]
        private float closeSeconds = 0.22f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        private readonly Queue<MedalRewardKind> pending =
            new Queue<MedalRewardKind>();

        private Vector3 originalScale;
        private Vector2 medalPosition;

        private Coroutine transition;
        private Action sequenceFinished;

        private bool prepared;
        private bool transitioning;
        private bool showing;

        private float effectTime;

        private void Awake()
        {
            if (!Prepare())
                return;

            HideNow();
        }

        private bool Prepare()
        {
            if (prepared)
                return true;

            if (popupRoot == null ||
                visualRoot == null ||
                nextButton == null ||
                titleText == null ||
                descriptionText == null)
            {
                Debug.LogError(
                    "[MedalReward] Asigna Popup Root, Visual Root, " +
                    "Next Button y los textos.",
                    this);

                return false;
            }

            if (transform.IsChildOf(popupRoot.transform))
            {
                Debug.LogError(
                    "[MedalReward] Coloca este componente en " +
                    "un objeto activo fuera del popup.",
                    this);

                return false;
            }

            if (popupCanvasGroup == null)
            {
                popupCanvasGroup =
                    popupRoot.GetComponent<CanvasGroup>();
            }

            if (popupCanvasGroup == null)
            {
                popupCanvasGroup =
                    popupRoot.AddComponent<CanvasGroup>();
            }

            originalScale = visualRoot.localScale;

            if (medalRoot != null)
                medalPosition = medalRoot.anchoredPosition;

            nextButton.onClick.RemoveListener(Next);
            nextButton.onClick.AddListener(Next);

            prepared = true;

            return true;
        }

        public bool TryShowPendingRewards(Action whenFinished)
        {
            if (!Prepare() || tableSelector == null)
            {
                Debug.LogWarning(
                    "[MedalReward] Revisa Table Selector " +
                    "y las referencias del popup.",
                    this);

                return false;
            }

            if (!SinglePlayerRewardHandoff.TryConsumeMedalReward(
                out var reward))
            {
                Log(
                    "Sin recompensa pendiente. Mesa='" +
                    reward.TableId +
                    "', record anterior=" +
                    reward.PreviousHighScore +
                    ", record actual=" +
                    reward.HighScore +
                    ".");

                return false;
            }

            if (!tableSelector.TryGetTableById(
                reward.TableId,
                out TableDisplayData table))
            {
                Debug.LogWarning(
                    "[MedalReward] No se encontro la mesa " +
                    reward.TableId,
                    this);

                return false;
            }

            List<MedalRewardKind> medals =
                SinglePlayerRewardHandoff.GetNewMedals(
                    reward,
                    table.BronzeScore,
                    table.SilverScore,
                    table.GoldScore,
                    table.MasterScore);

            if (medals.Count == 0)
            {
                Log(
                    "Mesa " + table.DisplayName +
                    ": record " + reward.PreviousHighScore +
                    " -> " + reward.HighScore +
                    ". No se ha cruzado un requisito nuevo de medalla.");

                return false;
            }

            StopAllCoroutines();

            transition = null;
            pending.Clear();

            foreach (MedalRewardKind kind in medals)
                pending.Enqueue(kind);

            sequenceFinished = whenFinished;

            if (tableNameText != null)
                tableNameText.text = table.DisplayName;

            Log(
                "Mostrando " + medals.Count +
                " medalla(s) nueva(s) de " +
                table.DisplayName + ".");

            ShowNextMedal();

            return true;
        }

        private void Log(string message)
        {
            if (showDebugLogs)
                Debug.Log("[MedalReward] " + message, this);
        }

        private void ShowNextMedal()
        {
            if (pending.Count == 0)
            {
                Action completed = sequenceFinished;
                sequenceFinished = null;

                HideNow();

                // Al cerrar la ultima medalla, mostrar las recetas.
                completed?.Invoke();

                return;
            }

            MedalRewardKind kind = pending.Dequeue();

            SetMedal(
                bronzeImage,
                kind == MedalRewardKind.Bronze);

            SetMedal(
                silverImage,
                kind == MedalRewardKind.Silver);

            SetMedal(
                goldImage,
                kind == MedalRewardKind.Gold);

            SetMedal(
                masterImage,
                kind == MedalRewardKind.Master);

            titleText.text = "CONGRATULATIONS";

            descriptionText.text =
                "YOU WON A " +
                kind.ToString().ToUpperInvariant() +
                " MEDAL";

            popupRoot.SetActive(true);
            popupRoot.transform.SetAsLastSibling();

            popupCanvasGroup.blocksRaycasts = true;
            popupCanvasGroup.interactable = false;

            nextButton.interactable = false;

            showing = true;
            effectTime = 0f;

            if (transition != null)
                StopCoroutine(transition);

            transitioning = true;

            transition = StartCoroutine(AnimateOpen());
        }

        private static void SetMedal(
            Image image,
            bool visible)
        {
            if (image == null)
                return;

            image.raycastTarget = false;
            image.preserveAspect = true;
            image.enabled = visible;

            image.gameObject.SetActive(visible);
        }

        private IEnumerator AnimateOpen()
        {
            transitioning = true;

            popupCanvasGroup.alpha = 0f;
            visualRoot.localScale = originalScale * 0.6f;

            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, openSeconds);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(
                    elapsed / duration);

                float scale;

                if (t < 0.75f)
                {
                    float p = t / 0.75f;

                    float eased =
                        1f - (1f - p) * (1f - p) * (1f - p);

                    scale = Mathf.Lerp(
                        0.6f,
                        1.08f,
                        eased);
                }
                else
                {
                    scale = Mathf.Lerp(
                        1.08f,
                        1f,
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            (t - 0.75f) / 0.25f));
                }

                visualRoot.localScale =
                    originalScale * scale;

                popupCanvasGroup.alpha =
                    Mathf.Clamp01(t / 0.5f);

                yield return null;
            }

            visualRoot.localScale = originalScale;

            popupCanvasGroup.alpha = 1f;
            popupCanvasGroup.interactable = true;

            nextButton.interactable = true;

            transitioning = false;
            transition = null;
        }

        public void Next()
        {
            if (!showing || transitioning)
                return;

            nextButton.interactable = false;
            popupCanvasGroup.interactable = false;

            transitioning = true;

            transition = StartCoroutine(AnimateClose());
        }

        private IEnumerator AnimateClose()
        {
            transitioning = true;

            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, closeSeconds);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(
                    elapsed / duration);

                float eased = t * t;

                visualRoot.localScale =
                    originalScale *
                    Mathf.Lerp(1f, 0.78f, eased);

                popupCanvasGroup.alpha = 1f - eased;

                yield return null;
            }

            transition = null;
            transitioning = false;
            showing = false;

            ShowNextMedal();
        }

        private void Update()
        {
            if (!showing)
                return;

            float dt = Time.unscaledDeltaTime;

            effectTime += dt;

            if (shineRoot != null)
            {
                shineRoot.Rotate(
                    0f,
                    0f,
                    -shineDegreesPerSecond * dt);
            }

            if (medalRoot != null)
            {
                medalRoot.anchoredPosition =
                    medalPosition +
                    Vector2.up *
                    (Mathf.Sin(effectTime * floatSpeed) *
                     floatDistance);
            }
        }

        private void HideNow()
        {
            showing = false;
            transitioning = false;

            if (popupRoot != null)
                popupRoot.SetActive(false);

            if (visualRoot != null)
                visualRoot.localScale = originalScale;

            if (medalRoot != null)
                medalRoot.anchoredPosition = medalPosition;

            if (popupCanvasGroup != null)
            {
                popupCanvasGroup.alpha = 1f;
                popupCanvasGroup.interactable = false;
                popupCanvasGroup.blocksRaycasts = false;
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();

            transition = null;
            sequenceFinished = null;

            pending.Clear();

            HideNow();
        }

        [ContextMenu("Preview Bronze (Play Mode)")]
        private void PreviewBronze()
        {
            if (!UnityEngine.Application.isPlaying || !prepared)
                return;

            StopAllCoroutines();

            transition = null;
            pending.Clear();
            sequenceFinished = null;

            pending.Enqueue(MedalRewardKind.Bronze);

            if (tableNameText != null)
                tableNameText.text = "PREVIEW";

            ShowNextMedal();
        }

        private void OnDestroy()
        {
            if (nextButton != null)
                nextButton.onClick.RemoveListener(Next);
        }
    }
}