using System;
using UnityEngine;

namespace RetroDiner.Data
{
    public enum OfferKind { TokenBundle, RemoveAds, EarlyBird, TipJarCrack, BoosterRefill }

    [Serializable]
    public class StoreOffer
    {
        public string sku;
        public OfferKind kind = OfferKind.TokenBundle;
        public string title;
        public string subtitle;
        [Tooltip("Display price for the fake IAP layer, e.g. $8.99. Real prices come from the store SDK.")]
        public string priceLabel;
        [Tooltip("Soft-currency price for BoosterRefill offers (0 = IAP only).")]
        public int coinPrice;
        [Tooltip("Token price for BoosterRefill offers (0 = not purchasable with tokens).")]
        public int tokenPrice;
        public int tokens;
        public int coins;
        public int spatula, bell, rewind, extraPlate;
        public bool consumable = true;
        [Tooltip("Drawn larger / first — the price anchor or the target choice.")]
        public bool highlight;
        [Tooltip("Hours after first launch while this offer is visible (0 = always).")]
        public int limitedHours;
    }

    /// <summary>
    /// Storefront choice architecture from the bible (5.5): anchor at the top, decoy, target bundle,
    /// small bundle, Remove Ads pack, Early Bird hook, and coin-priced booster refills.
    /// </summary>
    [CreateAssetMenu(menuName = "Retro Diner/Store Catalog", fileName = "StoreCatalog")]
    public class StoreCatalog : ScriptableObject
    {
        public const string SkuRemoveAds = "rd3d_remove_ads";
        public const string SkuEarlyBird = "rd3d_early_bird";
        public const string SkuTipJar = "rd3d_tip_jar_crack";

        public StoreOffer[] offers = new StoreOffer[0];

        public StoreOffer Find(string sku)
        {
            foreach (var o in offers) if (o != null && o.sku == sku) return o;
            return null;
        }

        public static StoreOffer[] DefaultOffers()
        {
            return new[]
            {
                new StoreOffer { sku = "rd3d_pantry_49", kind = OfferKind.TokenBundle, title = "Executive Chef Pantry", subtitle = "6,000 Golden Tokens + 10 of every booster", priceLabel = "$49.99", tokens = 6000, spatula = 10, bell = 10, rewind = 10, extraPlate = 10, highlight = false },
                new StoreOffer { sku = "rd3d_platter_8", kind = OfferKind.TokenBundle, title = "Deluxe Jumbo Platter", subtitle = "1,000 Tokens + 3 boosters  — BEST VALUE", priceLabel = "$8.99", tokens = 1000, spatula = 1, bell = 1, rewind = 1, highlight = true },
                new StoreOffer { sku = "rd3d_box_7", kind = OfferKind.TokenBundle, title = "Medium Breakfast Box", subtitle = "700 Golden Tokens", priceLabel = "$7.99", tokens = 700 },
                new StoreOffer { sku = "rd3d_coffee_2", kind = OfferKind.TokenBundle, title = "Small Coffee Bundle", subtitle = "250 Golden Tokens", priceLabel = "$2.99", tokens = 250 },
                new StoreOffer { sku = SkuRemoveAds, kind = OfferKind.RemoveAds, title = "Remove Ads", subtitle = "Remove forced ads. Keep bonus rewards.\n+200 Tokens + 2 of each booster", priceLabel = "$4.99", tokens = 200, spatula = 2, bell = 2, rewind = 2, extraPlate = 2, consumable = false },
                new StoreOffer { sku = SkuEarlyBird, kind = OfferKind.EarlyBird, title = "Grand Opening Early Bird", subtitle = "500 Tokens + 3 of each booster + Neon Sign\n600% value — first 48 hours only", priceLabel = "$0.99", tokens = 500, spatula = 3, bell = 3, rewind = 3, extraPlate = 3, consumable = false, highlight = true, limitedHours = 48 },
                new StoreOffer { sku = SkuTipJar, kind = OfferKind.TipJarCrack, title = "Crack the Tip Jar", subtitle = "Collect the tokens you earned (store rate $11.99)", priceLabel = "$2.99" },
                new StoreOffer { sku = "rd3d_refill_spatula", kind = OfferKind.BoosterRefill, title = "Golden Spatula", subtitle = "+1 Spatula", coinPrice = 150, tokenPrice = 15, spatula = 1 },
                new StoreOffer { sku = "rd3d_refill_bell", kind = OfferKind.BoosterRefill, title = "Order Up! Bell", subtitle = "+1 Bell", coinPrice = 120, tokenPrice = 12, bell = 1 },
                new StoreOffer { sku = "rd3d_refill_rewind", kind = OfferKind.BoosterRefill, title = "Rewind Coaster", subtitle = "+1 Rewind", coinPrice = 100, tokenPrice = 10, rewind = 1 },
                new StoreOffer { sku = "rd3d_refill_plate", kind = OfferKind.BoosterRefill, title = "Extra Plate", subtitle = "+1 Extra Plate (pre-shift)", coinPrice = 200, tokenPrice = 20, extraPlate = 1 },
            };
        }
    }
}
