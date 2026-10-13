using System;
using UnityEngine;
using RetroDiner.Data;

namespace RetroDiner.Core
{
    // ---------- Ads ----------
    public enum AdPlacement { RewardedContinue, RewardedBoosterRefill, RewardedDoubleCoins, Interstitial, Banner }

    public interface IAdService
    {
        bool IsRewardedReady(AdPlacement placement);
        /// <summary>Shows a rewarded ad. Callback receives true when the reward should be granted.</summary>
        void ShowRewarded(AdPlacement placement, Action<bool> onDone);
        void ShowInterstitial(Action onDone);
    }

    // ---------- IAP ----------
    public interface IIAPService
    {
        void Purchase(string sku, Action<bool> onDone);
        void RestorePurchases(Action<bool> onDone);
        bool Owns(string sku);
    }

    // ---------- Haptics ----------
    public interface IHapticService
    {
        void Play(HapticType type);
        bool Enabled { get; set; }
    }

    // ---------- Share ----------
    public interface IShareService
    {
        void ShareFile(string path, string caption);
    }

    // ---------- Stub implementations (swapped for real SDKs before soft launch) ----------

    public class HandheldHapticService : IHapticService
    {
        public bool Enabled { get; set; } = true;
        public void Play(HapticType type)
        {
            if (!Enabled || type == HapticType.None) return;
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }

    public class StubShareService : IShareService
    {
        public void ShareFile(string path, string caption)
        {
            Debug.Log($"[Share] Would open native share sheet for '{path}' with caption '{caption}'.");
            GameEvents.RaiseToast("Share sheet (stub)");
        }
    }

    public class FakeIAPService : IIAPService
    {
        readonly System.Collections.Generic.HashSet<string> owned = new System.Collections.Generic.HashSet<string>();

        public void Purchase(string sku, Action<bool> onDone)
        {
            Debug.Log($"[IAP] Fake purchase of '{sku}' succeeded.");
            owned.Add(sku);
            onDone?.Invoke(true);
        }

        public void RestorePurchases(Action<bool> onDone)
        {
            Debug.Log("[IAP] Fake restore.");
            onDone?.Invoke(true);
        }

        public bool Owns(string sku) => owned.Contains(sku) || (GameServices.Profile != null && GameServices.Profile.purchasedSkus.Contains(sku));
    }

    /// <summary>
    /// Fake ad service: shows a 3 second full-screen "ad" overlay (unscaled time) then grants the reward.
    /// The overlay is provided by the UI layer through a delegate so this class stays view-agnostic.
    /// </summary>
    public class FakeAdService : IAdService
    {
        public Action<string, float, Action> ShowOverlay; // (title, seconds, onFinished)
        public float FakeAdSeconds = 3f;

        public bool IsRewardedReady(AdPlacement placement) => true;

        public void ShowRewarded(AdPlacement placement, Action<bool> onDone)
        {
            if (ShowOverlay != null) ShowOverlay("Rewarded ad (fake) — " + placement, FakeAdSeconds, () => onDone?.Invoke(true));
            else onDone?.Invoke(true);
        }

        public void ShowInterstitial(Action onDone)
        {
            if (ShowOverlay != null) ShowOverlay("Interstitial (fake)", FakeAdSeconds, () => onDone?.Invoke());
            else onDone?.Invoke();
        }
    }
}
