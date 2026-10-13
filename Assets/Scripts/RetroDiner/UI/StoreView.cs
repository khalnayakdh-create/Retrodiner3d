using System;
using UnityEngine;
using UnityEngine.UI;
using RetroDiner.Core;
using RetroDiner.Data;
using RetroDiner.Meta;

namespace RetroDiner.UI
{
    /// <summary>
    /// Scrollable storefront. Order follows the bible's choice architecture: anchor pack first, then
    /// the target bundle, the decoy, the small bundle, Remove Ads, Early Bird (while it lasts), the
    /// Tip Jar crack when ready, and coin-priced booster refills at the bottom.
    /// </summary>
    public class StoreView : MonoBehaviour
    {
        RectTransform root, content;
        Text coinsText, tokensText;
        Action onBack;

        public bool IsVisible => root != null && root.gameObject.activeSelf;

        public void Build(Transform parent, GameConfig cfg, Action back)
        {
            onBack = back;
            root = UIBuilder.Rect(parent, "StorePage");
            UIBuilder.Stretch(root);
            var dim = UIBuilder.Panel(root, "Dim", new Color(0.08f, 0.06f, 0.08f, 0.92f), null, true);
            UIBuilder.Stretch(dim.rectTransform);

            var title = UIBuilder.Label(root, "Title", "DINER STORE", 72, UIBuilder.Cream);
            UIBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(1000f, 110f), new Vector2(0f, -110f));
            UIBuilder.Outline(title, UIBuilder.Red, 4f);

            var strip = UIBuilder.Rect(root, "Currency");
            UIBuilder.Place(strip, new Vector2(0.5f, 1f), new Vector2(1000f, 80f), new Vector2(0f, -210f));
            var coinIcon = UIBuilder.Icon(strip, "CoinIcon", ArtCatalog.Pick(cfg.coinIcon, ArtCatalog.Coin), UIBuilder.Gold, new Vector2(60f, 60f));
            UIBuilder.Place(coinIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(60f, 60f), new Vector2(-220f, 0f));
            coinsText = UIBuilder.Label(strip, "Coins", "0", 38, Color.white, TextAnchor.MiddleLeft);
            UIBuilder.Place(coinsText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(200f, 70f), new Vector2(-180f, 0f));
            var tokenIcon = UIBuilder.Icon(strip, "TokenIcon", ArtCatalog.Pick(cfg.tokenIcon, ArtCatalog.Token), UIBuilder.Purple, new Vector2(60f, 60f));
            UIBuilder.Place(tokenIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(60f, 60f), new Vector2(60f, 0f));
            tokensText = UIBuilder.Label(strip, "Tokens", "0", 38, Color.white, TextAnchor.MiddleLeft);
            UIBuilder.Place(tokensText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(200f, 70f), new Vector2(100f, 0f));

            // Scroll view
            var viewport = UIBuilder.Panel(root, "Viewport", new Color(1f, 1f, 1f, 0.01f), null, true);
            viewport.rectTransform.anchorMin = new Vector2(0f, 0f); viewport.rectTransform.anchorMax = new Vector2(1f, 1f);
            viewport.rectTransform.offsetMin = new Vector2(40f, 200f); viewport.rectTransform.offsetMax = new Vector2(-40f, -260f);
            viewport.gameObject.AddComponent<RectMask2D>();
            content = UIBuilder.Rect(viewport.transform, "Content");
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 18f; layout.padding = new RectOffset(0, 0, 10, 40);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content; scroll.viewport = viewport.rectTransform;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 90f;

            var backBtn = UIBuilder.Button(root, "Back", "BACK", () => onBack?.Invoke(), UIBuilder.Red, Popups.ButtonSprite(UIBuilder.Red), new Vector2(400f, 130f), 40);
            UIBuilder.Place(backBtn.image.rectTransform, new Vector2(0.5f, 0f), new Vector2(400f, 130f), new Vector2(0f, 110f));
            root.gameObject.SetActive(false);
        }

        public void Show()
        {
            root.gameObject.SetActive(true);
            Refresh();
        }

        public void Hide() { root.gameObject.SetActive(false); }

        public void Refresh()
        {
            var p = GameServices.Profile;
            coinsText.text = p.coins.ToString();
            tokensText.text = p.tokens.ToString();
            foreach (Transform c in content) Destroy(c.gameObject);
            var eco = GameServices.Economy;
            bool refillHeader = false;
            foreach (var o in eco.Catalog.offers)
            {
                if (!eco.IsOfferVisible(o)) continue;
                if (o.kind == OfferKind.BoosterRefill && !refillHeader)
                {
                    refillHeader = true;
                    var h = UIBuilder.Label(content, "RefillHeader", "BOOSTER REFILLS", 36, UIBuilder.Gold);
                    h.gameObject.AddComponent<LayoutElement>().preferredHeight = 70f;
                }
                BuildCard(o);
            }
        }

        void BuildCard(StoreOffer o)
        {
            var eco = GameServices.Economy;
            bool big = o.highlight;
            var card = UIBuilder.Panel(content, "Card_" + o.sku, big ? new Color(1f, 0.96f, 0.86f) : new Color(1f, 1f, 1f, 0.92f), null, true);
            var le = card.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = big ? 230f : 170f;
            UIBuilder.Shadow(card, 6f);
            if (big) UIBuilder.Outline(card, UIBuilder.Gold, 5f);

            var title = UIBuilder.Label(card.transform, "Title", o.title, big ? 44 : 36, UIBuilder.Dark, TextAnchor.UpperLeft);
            UIBuilder.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(640f, 60f), new Vector2(30f, -14f));
            string sub = o.subtitle;
            if (o.limitedHours > 0) sub += "\nEnds in " + Popups.FormatTime(eco.OfferTimeLeft(o));
            if (o.kind == OfferKind.TipJarCrack) sub = GameServices.Profile.tipJarTokens + " tokens waiting in the jar\n" + o.subtitle;
            var subLbl = UIBuilder.Label(card.transform, "Sub", sub, 26, new Color(0.3f, 0.28f, 0.28f), TextAnchor.UpperLeft, FontStyle.Normal);
            UIBuilder.Place(subLbl.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(640f, 110f), new Vector2(30f, big ? -80f : -64f));

            if (o.kind == OfferKind.BoosterRefill)
            {
                var coinBtn = UIBuilder.Button(card.transform, "Coins", o.coinPrice + " c", () => Buy(o, false), UIBuilder.Gold, null, new Vector2(150f, 70f), 28, UIBuilder.Dark);
                UIBuilder.Place(coinBtn.image.rectTransform, new Vector2(1f, 0.5f), new Vector2(150f, 70f), new Vector2(-270f, 0f));
                coinBtn.interactable = eco.CanAffordCoins(o.coinPrice);
                var tokBtn = UIBuilder.Button(card.transform, "Tokens", o.tokenPrice + " t", () => Buy(o, true), UIBuilder.Purple, null, new Vector2(150f, 70f), 28);
                UIBuilder.Place(tokBtn.image.rectTransform, new Vector2(1f, 0.5f), new Vector2(150f, 70f), new Vector2(-100f, 0f));
                tokBtn.interactable = eco.CanAffordTokens(o.tokenPrice);
            }
            else
            {
                var buy = UIBuilder.Button(card.transform, "Buy", o.priceLabel, () => Purchase(o), big ? UIBuilder.Red : UIBuilder.Mint, null, new Vector2(220f, 84f), 34, big ? Color.white : UIBuilder.Dark);
                UIBuilder.Place(buy.image.rectTransform, new Vector2(1f, 0.5f), new Vector2(220f, 84f), new Vector2(-135f, 0f));
            }
        }

        void Buy(StoreOffer o, bool withTokens)
        {
            if (GameServices.Economy.BuyRefill(o, withTokens)) { GameServices.Audio.Play("coin"); GameEvents.RaiseToast("+1 " + o.title); }
            else GameEvents.RaiseToast("Not enough " + (withTokens ? "tokens" : "coins"));
            Refresh();
        }

        void Purchase(StoreOffer o)
        {
            GameServices.IAP.Purchase(o.sku, ok =>
            {
                if (!ok) { GameEvents.RaiseToast("Purchase cancelled"); return; }
                int jar = GameServices.Profile.tipJarTokens;
                GameServices.Economy.ApplyPurchase(o);
                GameServices.Audio.Play("win");
                GameEvents.RaiseToast(o.kind == OfferKind.TipJarCrack ? "+" + jar + " tokens from the jar!" : "Thanks! " + o.title + " added");
                Refresh();
            });
        }
    }
}
