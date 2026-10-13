using UnityEngine;

namespace RetroDiner.Data
{
    /// <summary>All interstitial / banner rules in one data asset (build prompt section 16).</summary>
    [CreateAssetMenu(menuName = "Retro Diner/Ad Policy", fileName = "AdPolicy")]
    public class AdPolicy : ScriptableObject
    {
        [Header("Interstitials")]
        public int interstitialStartLevel = 6;
        [Tooltip("At most one interstitial per this many level ends.")]
        public int levelEndsBetweenInterstitials = 3;
        public float minSecondsBetweenInterstitials = 90f;
        public bool skipIfRewardedWatchedThisLevelEnd = true;
        public float suppressMinutesAfterPurchase = 10f;
        [Tooltip("Seconds the 'Remove ads' link stays visible after an interstitial closes.")]
        public float removeAdsLinkSeconds = 3f;

        [Header("Banners")]
        public bool bannerOnMainMenu = true;
        public bool bannerOnLevelSelect = true;
        public bool bannerOnGameplay = false;   // never, by design
    }
}
