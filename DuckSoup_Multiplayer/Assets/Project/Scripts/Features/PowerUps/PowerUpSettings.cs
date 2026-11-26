using UnityEngine;

namespace DuckSoup.Features.PowerUps
{
    public sealed class PowerUpSettings : MonoBehaviour
    {
        [Header("Combo & Penalties")]
        [SerializeField, Min(0.1f)]
        private float comboSeconds = 6f;

        [SerializeField, Min(0.1f)]
        private float extraSeconds = 4f;

        [SerializeField, Min(0.1f)]
        private float singlePenaltySeconds = 1f;

        [SerializeField, Min(0.1f)]
        private float multiplayerLockSeconds = 2.5f;

        [Header("Special Tokens in Existing Slots")]
        [SerializeField, Min(0.1f)]
        private float spawnCheckSeconds = 2.5f;

        [SerializeField, Min(0.1f)]
        private float tokenLifetimeSeconds = 10f;

        [SerializeField, Min(1)]
        private int maxActiveSpecials = 2;

        [Header("Weights: 67 / 12 / 9 / 5 / 7")]
        [SerializeField, Min(0f)]
        private float noSpecial = 67f;

        [SerializeField, Min(0f)]
        private float wildcardDuck = 12f;

        [Tooltip("Poop token. Field name kept for existing scene data.")]
        [SerializeField, Min(0f)]
        private float poison = 9f;

        [SerializeField, Min(0f)]
        private float instantRecipe = 5f;

        [SerializeField, Min(0f)]
        private float clock = 7f;

        public PowerUpRules CreateRules()
        {
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
                clock);
        }
    }
}