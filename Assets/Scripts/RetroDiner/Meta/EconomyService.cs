using System;
using System.Collections.Generic;
using UnityEngine;
using RetroDiner.Core;
using RetroDiner.Data;

namespace RetroDiner.Meta
{
    /// <summary>
    /// Plain C# rules for the meta economy: currencies, energy regen, the 7-day punch-card with a
    /// weekly streak freeze, the Tip Jar crack threshold, store purchases and booster refills.
    /// Persists through GameServices.SaveProfile(); never touches UI.
    /// </summary>
    public class EconomyService
    {
        readonly GameConfig cfg;
        readonly StoreCatalog catalog;
        PlayerProfile P => GameServices.Profile;

        public EconomyService(GameConfig config, StoreCatalog storeCatalog)
        {
            cfg = config;
            catalog = storeCatalog;
            if (P.firstLaunchTicks == 0) { P.firstLaunchTicks = DateTime.UtcNow.Ticks; GameServices.SaveProfile(); }
        }

        // ------------------------------------------------------------ currency

        public bool CanAffordCoins(int amount) => P.coins >= amount;
        public bool CanAffordTokens(int amount) => P.tokens >= amount;

        public bool SpendCoins(int amount)
        {
            if (!CanAffordCoins(amount)) return false;
            P.coins -= amount; GameServices.SaveProfile(); return true;
        }

        public bool SpendTokens(int amount)
        {
            if (!CanAffordTokens(amount)) return false;
            P.tokens -= amount; GameServices.SaveProfile(); return true;
        }

        public void Grant(int coins, int tokens)
        {
            P.coins += Mathf.Max(0, coins); P.tokens += Mathf.Max(0, tokens);
            GameServices.SaveProfile();
        }

        // ------------------------------------------------------------ energy

        public int EnergyMax => cfg.energyMax;

        /// <summary>Regenerates 1 heart per cfg.energyRegenMinutes since the last tick. Call before reading energy.</summary>
        public void TickEnergy()
        {
            if (P.energy >= cfg.energyMax) { P.lastEnergyTicks = DateTime.UtcNow.Ticks; return; }
            if (P.lastEnergyTicks == 0) { P.lastEnergyTicks = DateTime.UtcNow.Ticks; return; }
            double minutes = (DateTime.UtcNow - new DateTime(P.lastEnergyTicks, DateTimeKind.Utc)).TotalMinutes;
            int regen = (int)(minutes / cfg.energyRegenMinutes);
            if (regen <= 0) return;
            P.energy = Mathf.Min(cfg.energyMax, P.energy + regen);
            P.lastEnergyTicks = P.energy >= cfg.energyMax ? DateTime.UtcNow.Ticks : P.lastEnergyTicks + (long)(regen * cfg.energyRegenMinutes * TimeSpan.TicksPerMinute);
            GameServices.SaveProfile();
        }

        public TimeSpan TimeToNextEnergy()
        {
            TickEnergy();
            if (P.energy >= cfg.energyMax) return TimeSpan.Zero;
            var next = new DateTime(P.lastEnergyTicks, DateTimeKind.Utc).AddMinutes(cfg.energyRegenMinutes);
            var left = next - DateTime.UtcNow;
            return left < TimeSpan.Zero ? TimeSpan.Zero : left;
        }

        public bool HasEnergy { get { TickEnergy(); return P.energy > 0; } }

        public void ConsumeEnergy()
        {
            TickEnergy();
            if (P.energy >= cfg.energyMax) P.lastEnergyTicks = DateTime.UtcNow.Ticks;
            P.energy = Mathf.Max(0, P.energy - 1);
            GameServices.SaveProfile();
        }

        public void RefillEnergy()
        {
            P.energy = cfg.energyMax; P.lastEnergyTicks = DateTime.UtcNow.Ticks; GameServices.SaveProfile();
        }

        public int EnergyRefillTokenCost => cfg.energyRefillTokens;

        // ------------------------------------------------------------ punch-card (daily streak)

        public struct PunchReward { public int coins, tokens, boosters; }

        public PunchReward RewardForDay(int day /* 1..7 */)
        {
            switch (Mathf.Clamp(day, 1, 7))
            {
                case 1: return new PunchReward { coins = 50 };
                case 2: return new PunchReward { coins = 75 };
                case 3: return new PunchReward { boosters = 1 };           // booster pack: 1 of each
                case 4: return new PunchReward { coins = 100 };
                case 5: return new PunchReward { coins = 125 };
                case 6: return new PunchReward { boosters = 2 };
                default: return new PunchReward { tokens = 150 };
            }
        }

        static string Today => DateTime.UtcNow.ToString("yyyy-MM-dd");

        /// <summary>Day (1..7) the player would claim right now, applying the streak/grace rules.</summary>
        public int CurrentPunchDay
        {
            get
            {
                if (string.IsNullOrEmpty(P.lastPunchCardDate)) return 1;
                int gap = DaysSinceLastPunch();
                if (gap == 0) return P.punchCardDay;              // already claimed today
                if (gap == 1) return P.punchCardDay % 7 + 1;      // consecutive day
                if (gap == 2 && P.streakFreezesLeft > 0) return P.punchCardDay % 7 + 1; // one missed day, freeze covers it
                return 1;                                          // streak lost
            }
        }

        public bool CanClaimPunchToday => DaysSinceLastPunch() != 0;

        int DaysSinceLastPunch()
        {
            if (string.IsNullOrEmpty(P.lastPunchCardDate)) return 99;
            DateTime last;
            if (!DateTime.TryParse(P.lastPunchCardDate, out last)) return 99;
            return (int)(DateTime.UtcNow.Date - last.Date).TotalDays;
        }

        /// <summary>Claims today's punch. Returns the reward granted (or null if already claimed).</summary>
        public PunchReward? ClaimPunch()
        {
            if (!CanClaimPunchToday) return null;
            RefreshWeeklyFreeze();
            int gap = DaysSinceLastPunch();
            int day = CurrentPunchDay;
            if (gap == 2 && P.streakFreezesLeft > 0 && !string.IsNullOrEmpty(P.lastPunchCardDate)) P.streakFreezesLeft--;
            var r = RewardForDay(day);
            P.coins += r.coins; P.tokens += r.tokens;
            if (r.boosters > 0)
            {
                P.AddBooster(BoosterType.GoldenSpatula, r.boosters);
                P.AddBooster(BoosterType.OrderUpBell, r.boosters);
                P.AddBooster(BoosterType.RewindCoaster, r.boosters);
            }
            P.punchCardDay = day;
            P.lastPunchCardDate = Today;
            GameServices.SaveProfile();
            return r;
        }

        void RefreshWeeklyFreeze()
        {
            // One free "Off-Day Freeze" per ISO week.
            int week = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.UtcNow, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            int key = DateTime.UtcNow.Year * 100 + week;
            if (P.lastStreakFreezeWeek != key) { P.lastStreakFreezeWeek = key; P.streakFreezesLeft = 1; }
        }

        // ------------------------------------------------------------ daily jukebox spin

        public bool CanSpinToday => P.lastDailySpinDate != Today;

        public struct SpinResult { public int coins; public BoosterType? booster; }

        public SpinResult DailySpin()
        {
            var rng = new System.Random(unchecked((int)DateTime.UtcNow.Ticks));
            var r = new SpinResult();
            int roll = rng.Next(100);
            if (roll < 60) r.coins = 25 + rng.Next(4) * 25;            // 25..100
            else if (roll < 75) r.booster = BoosterType.GoldenSpatula;
            else if (roll < 90) r.booster = BoosterType.OrderUpBell;
            else r.booster = BoosterType.RewindCoaster;
            P.coins += r.coins;
            if (r.booster.HasValue) P.AddBooster(r.booster.Value, 1);
            P.lastDailySpinDate = Today;
            GameServices.SaveProfile();
            return r;
        }

        // ------------------------------------------------------------ tip jar

        public int TipJarThreshold => cfg.tipJarCrackThreshold;
        public bool TipJarReady => P.tipJarTokens >= cfg.tipJarCrackThreshold;
        public float TipJarFill => Mathf.Clamp01(P.tipJarTokens / (float)Mathf.Max(1, cfg.tipJarCrackThreshold));

        /// <summary>Called after a successful (fake) purchase of the crack SKU.</summary>
        public int CrackTipJar()
        {
            int tokens = P.tipJarTokens;
            P.tokens += tokens;
            P.tipJarTokens = 0;
            GameServices.SaveProfile();
            return tokens;
        }

        // ------------------------------------------------------------ store

        public StoreCatalog Catalog => catalog;

        public bool IsOfferVisible(StoreOffer o)
        {
            if (o == null) return false;
            if (!o.consumable && P.purchasedSkus.Contains(o.sku)) return false;
            if (o.kind == OfferKind.RemoveAds && P.noAds) return false;
            if (o.kind == OfferKind.TipJarCrack && !TipJarReady) return false;
            if (o.limitedHours > 0)
            {
                double hours = (DateTime.UtcNow - new DateTime(P.firstLaunchTicks, DateTimeKind.Utc)).TotalHours;
                if (hours > o.limitedHours) return false;
            }
            return true;
        }

        public TimeSpan OfferTimeLeft(StoreOffer o)
        {
            if (o == null || o.limitedHours <= 0) return TimeSpan.Zero;
            var end = new DateTime(P.firstLaunchTicks, DateTimeKind.Utc).AddHours(o.limitedHours);
            var left = end - DateTime.UtcNow;
            return left < TimeSpan.Zero ? TimeSpan.Zero : left;
        }

        /// <summary>Applies an offer's contents after the IAP layer confirmed the purchase.</summary>
        public void ApplyPurchase(StoreOffer o)
        {
            if (o == null) return;
            P.coins += o.coins; P.tokens += o.tokens;
            P.AddBooster(BoosterType.GoldenSpatula, o.spatula);
            P.AddBooster(BoosterType.OrderUpBell, o.bell);
            P.AddBooster(BoosterType.RewindCoaster, o.rewind);
            P.AddBooster(BoosterType.ExtraPlate, o.extraPlate);
            if (o.kind == OfferKind.RemoveAds) P.noAds = true;
            if (o.kind == OfferKind.EarlyBird) P.neonSignOwned = true;
            if (o.kind == OfferKind.TipJarCrack) CrackTipJar();
            if (!o.consumable && !P.purchasedSkus.Contains(o.sku)) P.purchasedSkus.Add(o.sku);
            P.lastPurchaseTicks = DateTime.UtcNow.Ticks;
            GameServices.SaveProfile();
        }

        /// <summary>Buy a BoosterRefill with coins (or tokens). Returns false when unaffordable.</summary>
        public bool BuyRefill(StoreOffer o, bool withTokens)
        {
            if (o == null || o.kind != OfferKind.BoosterRefill) return false;
            if (withTokens) { if (o.tokenPrice <= 0 || !SpendTokens(o.tokenPrice)) return false; }
            else { if (o.coinPrice <= 0 || !SpendCoins(o.coinPrice)) return false; }
            P.AddBooster(BoosterType.GoldenSpatula, o.spatula);
            P.AddBooster(BoosterType.OrderUpBell, o.bell);
            P.AddBooster(BoosterType.RewindCoaster, o.rewind);
            P.AddBooster(BoosterType.ExtraPlate, o.extraPlate);
            GameServices.SaveProfile();
            return true;
        }

        public StoreOffer RefillOfferFor(BoosterType b)
        {
            foreach (var o in catalog.offers)
            {
                if (o == null || o.kind != OfferKind.BoosterRefill) continue;
                if (b == BoosterType.GoldenSpatula && o.spatula > 0) return o;
                if (b == BoosterType.OrderUpBell && o.bell > 0) return o;
                if (b == BoosterType.RewindCoaster && o.rewind > 0) return o;
                if (b == BoosterType.ExtraPlate && o.extraPlate > 0) return o;
            }
            return null;
        }

        /// <summary>Restore: re-apply non-consumables the IAP layer reports as owned.</summary>
        public int RestoreFromReceipts(IIAPService iap)
        {
            int restored = 0;
            foreach (var o in catalog.offers)
            {
                if (o == null || o.consumable) continue;
                if (iap.Owns(o.sku) && !P.purchasedSkus.Contains(o.sku))
                {
                    P.purchasedSkus.Add(o.sku);
                    if (o.kind == OfferKind.RemoveAds) P.noAds = true;
                    if (o.kind == OfferKind.EarlyBird) P.neonSignOwned = true;
                    restored++;
                }
            }
            if (P.purchasedSkus.Contains(StoreCatalog.SkuRemoveAds)) P.noAds = true;
            GameServices.SaveProfile();
            return restored;
        }
    }
}
