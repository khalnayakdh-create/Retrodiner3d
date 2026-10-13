using System;
using RetroDiner.Core;
using RetroDiner.Data;

namespace RetroDiner.Meta
{
    /// <summary>Decides when interstitials and banners may show. Pure rules over PlayerProfile + AdPolicy.</summary>
    public class AdPolicyService
    {
        readonly AdPolicy policy;
        PlayerProfile P => GameServices.Profile;
        public AdPolicy Policy => policy;

        /// <summary>Set by the gameplay flow when a rewarded ad was watched during the current level end.</summary>
        public bool RewardedWatchedThisLevelEnd;

        public AdPolicyService(AdPolicy adPolicy) { policy = adPolicy; }

        public bool BannersAllowed => !P.noAds;

        public bool ShouldShowInterstitial(int levelIndex, bool duringShift, bool inNearMiss)
        {
            if (P.noAds) return false;
            if (duringShift || inNearMiss) return false;
            if (levelIndex < policy.interstitialStartLevel) return false;
            if (policy.skipIfRewardedWatchedThisLevelEnd && RewardedWatchedThisLevelEnd) return false;
            if (P.levelEndsSinceInterstitial + 1 < policy.levelEndsBetweenInterstitials) return false;
            if (P.lastInterstitialTicks != 0)
            {
                double s = (DateTime.UtcNow - new DateTime(P.lastInterstitialTicks, DateTimeKind.Utc)).TotalSeconds;
                if (s < policy.minSecondsBetweenInterstitials) return false;
            }
            if (P.lastPurchaseTicks != 0)
            {
                double m = (DateTime.UtcNow - new DateTime(P.lastPurchaseTicks, DateTimeKind.Utc)).TotalMinutes;
                if (m < policy.suppressMinutesAfterPurchase) return false;
            }
            return true;
        }

        /// <summary>Call once per level end (win or lose), after deciding about the interstitial.</summary>
        public void RecordLevelEnd(bool interstitialShown)
        {
            if (interstitialShown)
            {
                P.levelEndsSinceInterstitial = 0;
                P.lastInterstitialTicks = DateTime.UtcNow.Ticks;
            }
            else P.levelEndsSinceInterstitial++;
            RewardedWatchedThisLevelEnd = false;
            GameServices.SaveProfile();
        }
    }
}
