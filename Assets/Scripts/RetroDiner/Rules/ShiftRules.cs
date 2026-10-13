using RetroDiner.Core;
using RetroDiner.Data;

namespace RetroDiner.Rules
{
    /// <summary>Star and coin maths (build prompt section 10).</summary>
    public static class ShiftRules
    {
        public static int ComputeStars(LevelDefinition level, int freeSlotsAtFinish, bool charredMatched)
        {
            int stars = 1;
            int needed = level.stars != null ? level.stars.freeSlotsForTwoStars : 3;
            if (freeSlotsAtFinish >= needed) stars = 2;
            if (stars == 2 && !charredMatched) stars = 3;
            return stars;
        }

        public static int ComputeCoins(GameConfig cfg, int stars) => cfg.coinsBase + cfg.coinsPerStar * stars;

        public static bool IsBoosterUnlocked(GameConfig cfg, BoosterType b, int levelIndex)
        {
            switch (b)
            {
                case BoosterType.GoldenSpatula: return levelIndex >= cfg.spatulaUnlockLevel;
                case BoosterType.OrderUpBell: return levelIndex >= cfg.bellUnlockLevel;
                case BoosterType.RewindCoaster: return levelIndex >= cfg.rewindUnlockLevel;
                default: return true;
            }
        }
    }
}
