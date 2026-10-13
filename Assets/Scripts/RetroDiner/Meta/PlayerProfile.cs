using System;
using RetroDiner.Core;

namespace RetroDiner.Meta
{
    /// <summary>Local save data. Plain serializable class; persisted as JSON by SaveService.</summary>
    [Serializable]
    public class PlayerProfile
    {
        public int saveVersion = 1;
        public int coins = 150;
        public int tokens = 0;
        public int tipJarTokens = 0;
        public int spatula = 0;
        public int bell = 0;
        public int rewind = 0;
        public int extraPlate = 0;
        public int highestLevelUnlocked = 1;
        public int[] stars = new int[64];
        public bool noAds = false;
        public int energy = 5;
        public long lastEnergyTicks = 0;
        public bool soundOn = true;
        public bool musicOn = true;
        public bool hapticsOn = true;
        public bool introSpatula, introBell, introRewind;
        public string lastPunchCardDate = "";
        public int punchCardDay = 0;
        public int streakFreezesLeft = 1;
        public long lastInterstitialTicks = 0;
        public int levelEndsSinceInterstitial = 0;
        public long lastPurchaseTicks = 0;
        public long firstLaunchTicks = 0;
        public System.Collections.Generic.List<string> purchasedSkus = new System.Collections.Generic.List<string>();
        public bool neonSignOwned = false;
        public string lastDailySpinDate = "";
        public int lastStreakFreezeWeek = 0;

        public int GetStars(int level) => (level >= 0 && level < stars.Length) ? stars[level] : 0;

        public void SetStars(int level, int value)
        {
            if (level < 0) return;
            if (level >= stars.Length) Array.Resize(ref stars, level + 16);
            if (value > stars[level]) stars[level] = value;
        }

        public int GetBooster(BoosterType b)
        {
            switch (b)
            {
                case BoosterType.GoldenSpatula: return spatula;
                case BoosterType.OrderUpBell: return bell;
                case BoosterType.RewindCoaster: return rewind;
                default: return extraPlate;
            }
        }

        public void AddBooster(BoosterType b, int delta)
        {
            switch (b)
            {
                case BoosterType.GoldenSpatula: spatula = Math.Max(0, spatula + delta); break;
                case BoosterType.OrderUpBell: bell = Math.Max(0, bell + delta); break;
                case BoosterType.RewindCoaster: rewind = Math.Max(0, rewind + delta); break;
                default: extraPlate = Math.Max(0, extraPlate + delta); break;
            }
        }
    }
}
