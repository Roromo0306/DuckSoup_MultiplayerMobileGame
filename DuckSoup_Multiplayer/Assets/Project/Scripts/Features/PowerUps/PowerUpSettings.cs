using DuckSoup.Domain;
using DuckSoup.Features.TwoPlayer.Views;
using UnityEngine;

namespace DuckSoup.Features.PowerUps
{
    public sealed class PowerUpSettings : MonoBehaviour
    {
        [Header("Combo & Penalties")]
        [SerializeField, Min(0.1f)] private float comboSeconds = 6f;
        [SerializeField, Min(0.1f)] private float extraSeconds = 4f;
        [SerializeField, Min(0.1f)] private float singlePenaltySeconds = 1f;
        [SerializeField, Min(0.1f)] private float multiplayerLockSeconds = 2.5f;

        [Header("Special Tokens in Existing Slots")]
        [SerializeField, Min(0.1f)] private float spawnCheckSeconds = 2.5f;
        [SerializeField, Min(0.1f)] private float tokenLifetimeSeconds = 10f;
        [SerializeField, Min(1)] private int maxActiveSpecials = 2;

        [Header("Weights: 67 / 12 / 9 / 5 / 7")]
        [SerializeField, Min(0f)] private float noSpecial = 67f;
        [SerializeField, Min(0f)] private float wildcardDuck = 12f;

        [Tooltip("Caca / Poop. Nombre interno conservado para las escenas existentes.")]
        [SerializeField, Min(0f)] private float poison = 9f;

        [SerializeField, Min(0f)] private float instantRecipe = 5f;
        [SerializeField, Min(0f)] private float clock = 7f;

        [Header("Adaptive spawning (both modes)")]
        [SerializeField] private bool adaptiveSpawning = true;

        [SerializeField, Min(1f)]
        private float probabilityRampStart = 90f;

        [SerializeField, Min(0f)]
        private float probabilityRampEnd = 30f;

        [Header("At least one appearance before this remaining time")]
        [SerializeField, Min(0f)] private float extraTimeDeadline = 70f;
        [SerializeField, Min(0f)] private float duckDeadline = 60f;
        [SerializeField, Min(0f)] private float poopDeadline = 45f;
        [SerializeField, Min(0f)] private float completeRecipeDeadline = 30f;

        [SerializeField, Min(0.1f)]
        private float guaranteeLeadSeconds = 3f;

        [Header("Multiplayer: extra appearances on trailing side")]
        [SerializeField] private bool catchUpSpawning = true;

        [SerializeField, Min(0f)]
        private float bonus6To10Percent = 20f;

        [SerializeField, Min(0f)]
        private float bonus11To15Percent = 50f;

        [SerializeField, Min(0f)]
        private float bonus16PlusPercent = 100f;

        [Header("Multiplayer token positions")]
        [SerializeField] private Transform tokenSidesRoot;

        private TwoPlayerTokenView[] sideViews;
        private Transform[] homeSlots;
        private Camera sideCamera;
        private bool warnedMissingRoot;

        public PowerUpRules CreateRules()
        {
            PowerTokenSpawnRules spawning = adaptiveSpawning
                ? new PowerTokenSpawnRules(
                    probabilityRampStart,
                    probabilityRampEnd,
                    extraTimeDeadline,
                    duckDeadline,
                    poopDeadline,
                    completeRecipeDeadline,
                    guaranteeLeadSeconds,
                    catchUpSpawning,
                    bonus6To10Percent,
                    bonus11To15Percent,
                    bonus16PlusPercent,
                    GetTokenSide)
                : null;

            return new PowerUpRules(
                comboSeconds,
                extraSeconds,
                singlePenaltySeconds,
                multiplayerLockSeconds,
                spawnCheckSeconds,
                tokenLifetimeSeconds,
                maxActiveSpecials,
                noSpecial,
                wildcardDuck,
                poison,
                instantRecipe,
                clock,
                spawning);
        }

        private PlayerId GetTokenSide(string tokenId)
        {
            if (tokenSidesRoot == null)
            {
                if (!warnedMissingRoot)
                {
                    Debug.LogWarning(
                        "[PowerUpSettings] Asigna Token Sides Root en multiplayer.",
                        this);

                    warnedMissingRoot = true;
                }

                return PlayerId.None;
            }

            if (sideViews == null || sideViews.Length == 0)
            {
                sideViews =
                    tokenSidesRoot.GetComponentsInChildren<TwoPlayerTokenView>(true);

                homeSlots = new Transform[sideViews.Length];

                Canvas canvas =
                    tokenSidesRoot.GetComponentInParent<Canvas>();

                if (canvas != null)
                    canvas = canvas.rootCanvas;

                sideCamera =
                    canvas != null &&
                    canvas.renderMode != RenderMode.ScreenSpaceOverlay
                        ? canvas.worldCamera
                        : null;
            }

            for (int i = 0; i < sideViews.Length; i++)
            {
                TwoPlayerTokenView view = sideViews[i];

                if (view == null || view.TokenId != tokenId)
                    continue;

                Transform parent = view.transform.parent;

                if (parent != null &&
                    parent.IsChildOf(tokenSidesRoot))
                {
                    homeSlots[i] = parent;
                }

                if (homeSlots[i] == null)
                    return PlayerId.None;

                float y =
                    RectTransformUtility.WorldToScreenPoint(
                        sideCamera,
                        homeSlots[i].position).y;

                float difference =
                    y - Screen.height * 0.5f;

                if (Mathf.Abs(difference) <= 1f)
                    return PlayerId.None;

                return difference > 0f
                    ? PlayerId.Player1
                    : PlayerId.Player2;
            }

            return PlayerId.None;
        }
    }
}