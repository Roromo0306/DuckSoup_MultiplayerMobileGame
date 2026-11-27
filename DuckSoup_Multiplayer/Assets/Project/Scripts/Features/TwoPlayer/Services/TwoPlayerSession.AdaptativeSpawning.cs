using DuckSoup.Domain;
using DuckSoup.Features.PowerUps;

namespace DuckSoup.Features.TwoPlayer.Services
{
    public sealed partial class TwoPlayerSession
    {
        private AdaptivePowerTokenSpawner adaptiveSpawner;

        private readonly PlayerId[] specialTokenSides =
            new PlayerId[TokenSlots];

        public void SetSpecialTokenSide(
            string tokenId,
            PlayerId side)
        {
            int slot = FindToken(tokenId);

            if (slot >= 0)
                specialTokenSides[slot] = side;
        }

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
                        RequiredAtTable,
                        tokens,
                        pendingIngredients,
                        refillTimers,
                        specialKinds,
                        tokenOwners),
                    slot =>
                        powerUpRules.AdaptiveSpawning.SideResolver != null &&
                        tokens[slot] != null
                            ? powerUpRules.AdaptiveSpawning.SideResolver(
                                tokens[slot].Id)
                            : specialTokenSides[slot]);
            }

            return adaptiveSpawner.Tick(
                dt,
                RemainingSeconds,
                Player1Score,
                Player2Score);
        }

        public int GetSpecialAppearanceCount(PowerTokenKind kind)
        {
            return adaptiveSpawner != null
                ? adaptiveSpawner.GetAppearanceCount(kind)
                : 0;
        }
    }
}