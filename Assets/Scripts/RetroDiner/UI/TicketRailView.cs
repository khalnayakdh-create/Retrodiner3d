using System;
using UnityEngine;
using UnityEngine.UI;
using RetroDiner.Core;
using RetroDiner.Data;
using RetroDiner.Rules;

namespace RetroDiner.UI
{
    /// <summary>Up to three ticket cards hanging from clips at the top of the screen.</summary>
    public class TicketRailView : MonoBehaviour
    {
        class Card
        {
            public RectTransform rect;
            public Image back, icon, fill, check;
            public Text count;
            public Vector2 homePos;
            public bool animating;
        }

        RectTransform root;
        Card[] cards = new Card[0];
        GameConfig cfg;
        Func<string, ItemDefinition> lookup;
        Sprite normalSprite, activeSprite, completeSprite;

        const float CardW = 250f, CardH = 320f, Gap = 40f;

        public void Build(Transform parent, GameConfig config, Func<string, ItemDefinition> defLookup)
        {
            cfg = config;
            lookup = defLookup;
            normalSprite = ArtCatalog.Pick(cfg.ticketNormal, ArtCatalog.TicketNormal);
            activeSprite = ArtCatalog.Pick(cfg.ticketActive, ArtCatalog.TicketActive);
            completeSprite = ArtCatalog.Pick(cfg.ticketComplete, ArtCatalog.TicketComplete);

            root = UIBuilder.Rect(parent, "OrderRail");
            UIBuilder.Place(root, new Vector2(0.5f, 1f), new Vector2(1080f, 360f), new Vector2(0f, -330f));

            int n = cfg.ticketsVisible;
            cards = new Card[n];
            float totalW = n * CardW + (n - 1) * Gap;
            for (int i = 0; i < n; i++)
            {
                var c = new Card();
                var back = UIBuilder.Panel(root, "Ticket" + i, normalSprite != null ? Color.white : UIBuilder.Cream, normalSprite, false);
                back.preserveAspect = true;
                c.rect = back.rectTransform;
                c.homePos = new Vector2(-totalW * 0.5f + CardW * 0.5f + i * (CardW + Gap), 0f);
                UIBuilder.Place(c.rect, new Vector2(0.5f, 0.5f), new Vector2(CardW, CardH), c.homePos);
                if (normalSprite == null)
                {
                    UIBuilder.Shadow(back, 6f);
                    var clip = UIBuilder.Panel(back.transform, "Clip", UIBuilder.Red, null, false);
                    UIBuilder.Place(clip.rectTransform, new Vector2(0.5f, 1f), new Vector2(70f, 40f), new Vector2(0f, 10f));
                }
                c.back = back;

                c.icon = UIBuilder.Panel(back.transform, "Icon", Color.white, null, false);
                c.icon.preserveAspect = true;
                UIBuilder.Place(c.icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(150f, 150f), new Vector2(20f, 10f));

                c.count = UIBuilder.Label(back.transform, "Count", "3", 72, UIBuilder.Dark, TextAnchor.MiddleLeft);
                UIBuilder.Place(c.count.rectTransform, new Vector2(0f, 0.5f), new Vector2(90f, 100f), new Vector2(60f, 10f));
                UIBuilder.Outline(c.count, new Color(1f, 1f, 1f, 0.7f), 2f);

                var well = UIBuilder.Panel(back.transform, "Well", new Color(0f, 0f, 0f, 0.15f), null, false);
                UIBuilder.Place(well.rectTransform, new Vector2(0.5f, 0f), new Vector2(170f, 18f), new Vector2(0f, 48f));
                c.fill = UIBuilder.Panel(well.transform, "Fill", new Color(0.35f, 0.78f, 0.35f), null, false);
                c.fill.rectTransform.anchorMin = new Vector2(0f, 0f);
                c.fill.rectTransform.anchorMax = new Vector2(0f, 1f);
                c.fill.rectTransform.pivot = new Vector2(0f, 0.5f);
                c.fill.rectTransform.anchoredPosition = Vector2.zero;
                c.fill.rectTransform.sizeDelta = new Vector2(0f, 0f);

                c.check = UIBuilder.Panel(back.transform, "Check", new Color(0.2f, 0.75f, 0.3f), null, false);
                UIBuilder.Place(c.check.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(160f, 160f), new Vector2(0f, 0f));
                var checkLbl = UIBuilder.Label(c.check.transform, "Mark", "OK", 60, Color.white);
                UIBuilder.Stretch(checkLbl.rectTransform);
                c.check.gameObject.SetActive(false);
                cards[i] = c;
            }
        }

        public Vector2 SlotScreenPosition(int railSlot)
        {
            if (cards.Length == 0) return new Vector2(Screen.width * 0.5f, Screen.height * 0.85f);
            railSlot = Mathf.Clamp(railSlot, 0, cards.Length - 1);
            return UIBuilder.LocalToScreen(cards[railSlot].rect);
        }

        public void Refresh(TicketBoard board)
        {
            for (int s = 0; s < cards.Length; s++)
            {
                if (cards[s].animating) continue;
                ApplyTicket(cards[s], board.TicketInSlot(s));
            }
        }

        void ApplyTicket(Card c, Ticket t)
        {
            if (t == null)
            {
                c.rect.gameObject.SetActive(false);
                return;
            }
            c.rect.gameObject.SetActive(true);
            var def = lookup(t.itemId);
            TrayView.ApplyIcon(c.icon, def, false);
            c.count.text = t.remaining.ToString();
            c.fill.rectTransform.sizeDelta = new Vector2(170f * t.Progress, 0f);
            c.check.gameObject.SetActive(false);
            bool active = t.remaining < t.total;
            var sprite = active ? (activeSprite != null ? activeSprite : normalSprite) : normalSprite;
            if (sprite != null) { c.back.sprite = sprite; c.back.color = Color.white; }
            else c.back.color = active ? Color.Lerp(UIBuilder.Cream, UIBuilder.Gold, 0.35f) : UIBuilder.Cream;
        }

        /// <summary>Freeze a card's visuals until AnimateComplete runs (model already moved on).</summary>
        public void Hold(int railSlot)
        {
            if (railSlot >= 0 && railSlot < cards.Length) cards[railSlot].animating = true;
        }

        public void Pulse(int railSlot)
        {
            if (railSlot < 0 || railSlot >= cards.Length) return;
            UIBuilder.Punch(cards[railSlot].rect, 0.12f, 0.3f);
        }

        /// <summary>Stamp a checkmark, slide out, then show whatever the board now has in that slot.</summary>
        public void AnimateComplete(int railSlot, TicketBoard board, Action onDone)
        {
            if (railSlot < 0 || railSlot >= cards.Length) { onDone?.Invoke(); return; }
            var c = cards[railSlot];
            c.animating = true;
            c.count.text = "0";
            c.fill.rectTransform.sizeDelta = new Vector2(170f, 0f);
            if (completeSprite != null) { c.back.sprite = completeSprite; c.back.color = Color.white; }
            c.check.gameObject.SetActive(true);
            c.check.rectTransform.localScale = Vector3.one * 1.8f;
            Tween.Scale(c.check.rectTransform, Vector3.one, 0.25f, Ease.OutBack, null, true);
            UIBuilder.Punch(c.rect, 0.15f, 0.3f);

            Tween.Delay(0.6f, () =>
            {
                Tween.AnchoredMove(c.rect, c.homePos + new Vector2(0f, 420f), 0.3f, Ease.InQuad, () =>
                {
                    c.animating = false;
                    ApplyTicket(c, board.TicketInSlot(railSlot));
                    c.rect.anchoredPosition = c.homePos + new Vector2(0f, 420f);
                    Tween.AnchoredMove(c.rect, c.homePos, 0.35f, Ease.OutBack, null, true);
                    onDone?.Invoke();
                }, true);
            }, true);
        }
    }
}
