using UnityEngine;

namespace DuckSoup.Features.PowerUps
{
    // Add this component to the same object as a gameplay Bootstrap.
    // No additional token slots or prefabs are needed.
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

        [Header("Weights: 75 / 10 / 7 / 3 / 5")]
        [SerializeField, Min(0f)] private float noSpecial = 75f;
        [SerializeField, Min(0f)] private float wildcardDuck = 10f;
        [SerializeField, Min(0f)] private float poison = 7f;
        [SerializeField, Min(0f)] private float instantRecipe = 3f;
        [SerializeField, Min(0f)] private float clock = 5f;

        public PowerUpRules CreateRules()
        {
            return new PowerUpRules(
                comboSeconds, extraSeconds, singlePenaltySeconds,
                multiplayerLockSeconds, spawnCheckSeconds,
                tokenLifetimeSeconds, maxActiveSpecials,
                noSpecial, wildcardDuck, poison, instantRecipe, clock);
        }
    }
}
