namespace DuckSoup.Features.GameFeel.Menu
{
    // Evento temporal en memoria. El guardado permanente sigue en PlayerPrefsProgressStore.
    public static class SinglePlayerRewardHandoff
    {
        public struct Reward
        {
            public int PreviousTotal;
            public int Earned;
        }

        private static bool pending;
        private static Reward reward;

        public static void Queue(int previousTotal, int earned)
        {
            if (earned < 0)
            {
                pending = false;
                return;
            }
            reward = new Reward { PreviousTotal = previousTotal, Earned = earned };
            pending = true;
        }

        public static bool TryConsume(out Reward result)
        {
            result = reward;
            bool hadPending = pending;
            pending = false;
            return hadPending;
        }
    }
}
