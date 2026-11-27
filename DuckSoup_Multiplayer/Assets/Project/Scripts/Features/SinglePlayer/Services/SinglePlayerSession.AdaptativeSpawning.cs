using DuckSoup.Features.PowerUps;

namespace DuckSoup.Features.SinglePlayer.Services
{
    public sealed partial class SinglePlayerSession
    {
        private AdaptivePowerTokenSpawner adaptiveSpawner;

        private bool TickSpecialTokens(float dt)
        {
            if (powerUpRules.AdaptiveSpawning == null)
                return TickSpecialTokensLegacy(dt);

            if (adaptiveSpawner == null ||
                adaptiveSpawner.Rules != powerUpRules)
            {
                adaptiveSpawner = new AdaptivePowerTokenSpawner(
                    powerUpRules,
                    random,
                    tokens,
                    specialKinds,
                    specialLeft,
                    tokenOwners,
                    () => PowerTokenAvailability.GetEligibleSlots(
                        AllCards(),
                        tokens,
                        pendingIngredients,
                        refillTimers,
                        specialKinds,
                        tokenOwners));
            }

            return adaptiveSpawner.Tick(
                dt,
                RemainingSeconds);
        }

        public int GetSpecialAppearanceCount(PowerTokenKind kind)
        {
            return adaptiveSpawner != null
                ? adaptiveSpawner.GetAppearanceCount(kind)
                : 0;
        }
    }
}