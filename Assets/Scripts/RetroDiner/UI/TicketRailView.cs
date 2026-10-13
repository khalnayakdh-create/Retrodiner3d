using System;
using UnityEngine;
using UnityEngine.UI;
using RetroDiner.Core;
using RetroDiner.Data;
using RetroDiner.Rules;

namespace RetroDiner.UI
{
    /// <summary>
    /// Order Rail: up to three compact ticket cards. Each card = red clip, cream body with a bevelled
    /// chrome edge, large food icon, the remaining-orders count INSIDE the card, an inset progress bar.
    /// Count semantics (documented in DECISIONS): a ticket's number is the remaining triplets ("orders")
    /// of that dish; the bar shows completed/required.
    /// Uses ui/rd3d_ui_ticket_frame_v002 (9-slice) when present, else a procedural card.
    /// </summary>
    public class TicketRailView : MonoBehaviour
    {
        class Card
        {
            public RectTransform rect;
            public Image back, edge, clip, icon, fill, well, check, glow, outline;
            public Text count, caption;
            public Vector2 homePos;
            public bool animating;
            public Ticket shown;
        }

        RectTransform root;
        Card[] cards = new Card[0];
        GameConfig cfg;
        Func<string, ItemDefinition> lookup;
        Sprite frameSprite, checkSprite, trackSprite, fillSprite;

        const float CardW = 262f, CardH = 236f, Gap = 34f;

        public void Build(Transform parent, GameConfig config, Func<string, ItemDefinition> defLookup)
        {
            cfg = config;
            lookup = defLookup;
            frameSprite = ArtCatalog.Load("ui/rd3d_ui_ticket_frame_v002");
            checkSprite = ArtCatalog.Load("ui/rd3d_ui_check_stamp_v001");
            trackSprite = ArtCatalog.Load("ui/rd3d_ui_bar_track_v001");
            fillSprite = ArtCatalog.Load("ui/rd3d_ui_bar_fill_green_v001");

            root = UIBuilder.Rect(parent, "OrderRail");
            UIBuilder.Place(root, new Vector2(0.5f, 1f), new Vector2(1080f, 300f), new Vector2(0f, Style.TicketRailY));

            int n = cfg.ticketsVisible;
            cards = new Card[n];
            float totalW = n * CardW + (n - 1) * Gap;
            for (int i = 0; i < n; i++)
            {
                var c = new Card();
                var holder = UIBuilder.Rect(root, "Ticket" + i);
                c.rect = holder;
                c.homePos = new Vector2(-totalW * 0.5f + CardW * 0.5f + i * (CardW + Gap), 0f);
                UIBuilder.Place(holder, new Vector2(0.5f, 0.5f), new Vector2(CardW, CardH), c.homePos);

                // Destination glow (behind the card) for the 13A anticipation state.
                c.glow = UIBuilder.Panel(holder, "Glow", new Color(1f, 0.85f, 0.35f, 0f), ProcSprites.Glow(128, 1.6f), false);
                UIBuilder.Place(c.glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(CardW * 1.7f, CardH * 1.8f), Vector2.zero);

                if (frameSprite != null)
                {
                    c.back = UIBuilder.Sliced(holder, "Frame", frameSprite, Style.Cream, 26, false);
                    UIBuilder.Stretch(c.back.rectTransform);
                }
                else
                {
                    var shadow = UIBuilder.Pill(holder, "Shadow", new Color(0f, 0f, 0f, 0.3f), 26);
                    UIBuilder.Stretch(shadow.rectTransform, -2, -6, -2, -14);
                    c.edge = UIBuilder.Pill(holder, "Edge", Style.Chrome, 26);
                    UIBuilder.Stretch(c.edge.rectTransform, 0, 0, 0, 0);
                    var edgeSheen = UIBuilder.Panel(c.edge.transform, "Sheen", new Color(1f, 1f, 1f, 0.55f), ProcSprites.Sheen(64), false);
                    UIBuilder.Stretch(edgeSheen.rectTransform); edgeSheen.sprite = ProcSprites.Rounded(26); edgeSheen.type = Image.Type.Sliced; edgeSheen.color = new Color(1f, 1f, 1f, 0.12f);
                    c.back = UIBuilder.Pill(holder, "Body", Style.Cream, 22);
                    UIBuilder.Stretch(c.back.rectTransform, 6, 6, 6, 6);
                    var bodyShade = UIBuilder.Panel(c.back.transform, "Shade", new Color(0.55f, 0.35f, 0.2f, 0.12f), ProcSprites.VerticalGradient(0f, 1f), false);
                    bodyShade.rectTransform.anchorMin = new Vector2(0f, 0f); bodyShade.rectTransform.anchorMax = new Vector2(1f, 0.35f);
                    bodyShade.rectTransform.offsetMin = Vector2.zero; bodyShade.rectTransform.offsetMax = Vector2.zero;
                    c.clip = UIBuilder.Pill(holder, "Clip", Style.Cherry, 14);
                    UIBuilder.Place(c.clip.rectTransform, new Vector2(0.5f, 1f), new Vector2(86f, 36f), new Vector2(0f, 10f));
                    UIBuilder.Outline(c.clip, Style.ChromeLight, 2f);
                    var clipPin = UIBuilder.Panel(c.clip.transform, "Pin", Style.ChromeLight, ProcSprites.Circle(32), false);
                    UIBuilder.Place(clipPin.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(14f, 14f), new Vector2(0f, 2f));
                }

                // Gold outline ring used during delivery (anticipation).
                c.outline = UIBuilder.Pill(holder, "Outline", new Color(1f, 0.8f, 0.3f, 0f), 28);
                UIBuilder.Stretch(c.outline.rectTransform, -5, -5, -5, -5);
                c.outline.type = Image.Type.Sliced;
                c.outline.fillCenter = false;
                var outlineRing = c.outline.gameObject.AddComponent<Outline>(); outlineRing.effectColor = new Color(1f, 0.8f, 0.3f, 0f); outlineRing.effectDistance = new Vector2(3f, -3f);

                c.icon = UIBuilder.Panel(holder, "Icon", Color.white, null, false);
                c.icon.preserveAspect = true;
                UIBuilder.Place(c.icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(128f, 128f), new Vector2(24f, 18f));

                c.count = UIBuilder.Label(holder, "Count", "3", 62, Style.Ink, TextAnchor.MiddleCenter);
                UIBuilder.Place(c.count.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(74f, 80f), new Vector2(18f, 26f));
                c.caption = UIBuilder.Label(holder, "Caption", "left", 20, new Color(0.22f, 0.15f, 0.11f, 0.75f), TextAnchor.MiddleCenter, FontStyle.Normal);
                UIBuilder.Place(c.caption.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(74f, 26f), new Vector2(18f, -14f));

                // Inset progress well + fill.
                c.well = trackSprite != null ? UIBuilder.Sliced(holder, "Well", trackSprite, new Color(0.2f, 0.2f, 0.22f), 10, false)
                                             : UIBuilder.Pill(holder, "Well", new Color(0.16f, 0.17f, 0.19f, 0.9f), 10);
                UIBuilder.Place(c.well.rectTransform, new Vector2(0.5f, 0f), new Vector2(CardW - 58f, 22f), new Vector2(0f, 28f));
                if (trackSprite == null) UIBuilder.Outline(c.well, new Color(1f, 1f, 1f, 0.25f), 1.5f);
                c.fill = fillSprite != null ? UIBuilder.Sliced(c.well.transform, "Fill", fillSprite, new Color(0.35f, 0.78f, 0.35f), 8, false)
                                            : UIBuilder.Pill(c.well.transform, "Fill", new Color(0.38f, 0.8f, 0.4f), 8);
                c.fill.rectTransform.anchorMin = new Vector2(0f, 0f); c.fill.rectTransform.anchorMax = new Vector2(0f, 1f);
                c.fill.rectTransform.pivot = new Vector2(0f, 0.5f); c.fill.rectTransform.anchoredPosition = new Vector2(3f, 0f);
                c.fill.rectTransform.sizeDelta = new Vector2(0f, -6f);

                c.check = UIBuilder.Panel(holder, "Check", checkSprite != null ? Color.white : new Color(0.2f, 0.75f, 0.3f), checkSprite != null ? checkSprite : ProcSprites.Circle(128), false);
                c.check.preserveAspect = true;
                UIBuilder.Place(c.check.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(120f, 120f), new Vector2(24f, 18f));
                if (checkSprite == null)
                {
                    UIBuilder.Outline(c.check, Style.ChromeLight, 3f);
                    var mark = UIBuilder.Label(c.check.transform, "Mark", "OK", 44, Style.Cream);
                    UIBuilder.Stretch(mark.rectTransform);
                }
                c.check.gameObject.SetActive(false);
                cards[i] = c;
            }
        }

        public Vector2 SlotScreenPosition(int railSlot)
        {
            if (cards.Length == 0) return new Vector2(Screen.width * 0.5f, Screen.height * 0.85f);
            railSlot = Mathf.Clamp(railSlot, 0, cards.Length - 1);
            return UIBuilder.LocalToScreen(cards[railSlot].icon.rectTransform);
        }

        public float FillWidth => CardW - 58f - 6f;

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
            c.shown = t;
            if (t == null) { c.rect.gameObject.SetActive(false); return; }
            c.rect.gameObject.SetActive(true);
            var def = lookup(t.itemId);
            TrayView.ApplyIcon(c.icon, def, false);
            c.icon.rectTransform.localScale = Vector3.one;
            c.count.text = t.remaining.ToString();
            c.caption.text = t.remaining == 1 ? "order" : "orders";
            c.fill.rectTransform.sizeDelta = new Vector2(FillWidth * t.Progress, -6f);
            c.check.gameObject.SetActive(false);
            SetOutline(c, 0f);
        }

        void SetOutline(Card c, float alpha)
        {
            var col = c.outline.color; col.a = alpha; c.outline.color = col;
            var g = c.glow.color; g.a = alpha * 0.6f; c.glow.color = g;
        }

        /// <summary>Freeze a card's visuals until AnimateComplete runs (model already moved on).</summary>
        public void Hold(int railSlot)
        {
            if (railSlot >= 0 && railSlot < cards.Length) cards[railSlot].animating = true;
        }

        // ---------------------------------------------------------------- 13A reactions

        /// <summary>Delivery starting: subtle gold outline so the player sees where the dishes go.</summary>
        public void Anticipate(int railSlot, bool on)
        {
            if (railSlot < 0 || railSlot >= cards.Length) return;
            var c = cards[railSlot];
            float from = c.outline.color.a, to = on ? 0.95f : 0f;
            Tween.To(0.18f, t => SetOutline(c, Mathf.Lerp(from, to, t)), Ease.OutQuad, null, true);
        }

        /// <summary>Food arrived: icon pulse, small sparkle, progress bar animates to the authoritative value.</summary>
        public void Arrive(int railSlot, Ticket ticket, Action<RectTransform> sparkle)
        {
            if (railSlot < 0 || railSlot >= cards.Length || ticket == null) return;
            var c = cards[railSlot];
            var rt = c.icon.rectTransform;
            rt.localScale = Vector3.one * 1.22f;
            Tween.Scale(rt, Vector3.one, Style.TicketArrive, Ease.OutBack, null, true);
            sparkle?.Invoke(rt);
            float w0 = c.fill.rectTransform.sizeDelta.x, w1 = FillWidth * ticket.Progress;
            Tween.To(0.25f, t => { if (c.fill != null) c.fill.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(w0, w1, t), -6f); }, Ease.OutQuad, null, true);
            c.count.text = ticket.remaining.ToString();
            c.caption.text = ticket.remaining == 1 ? "order" : "orders";
            UIBuilder.Punch(c.count.rectTransform, 0.25f, 0.25f);
        }

        /// <summary>Order complete: bar fills, checkmark reveals, short bounce; the acknowledgement stays visible, then the next ticket slides in.</summary>
        public void AnimateComplete(int railSlot, TicketBoard board, Action onDone)
        {
            if (railSlot < 0 || railSlot >= cards.Length) { onDone?.Invoke(); return; }
            var c = cards[railSlot];
            c.animating = true;
            c.count.text = "0";
            c.caption.text = "done";
            c.fill.rectTransform.sizeDelta = new Vector2(FillWidth, -6f);
            SetOutline(c, 0f);
            c.check.gameObject.SetActive(true);
            c.check.rectTransform.localScale = Vector3.one * 1.6f;
            Tween.Scale(c.check.rectTransform, Vector3.one, 0.22f, Ease.OutBack, null, true);
            UIBuilder.Punch(c.rect, 0.1f, 0.3f);
            var g = c.glow.color; g.a = 0.8f; c.glow.color = g;
            Tween.To(0.5f, t => { var gg = c.glow.color; gg.a = 0.8f * (1f - t); c.glow.color = gg; }, Ease.OutQuad, null, true);

            float hold = Style.ReducedMotion ? 0.35f : 0.75f;
            Tween.Delay(hold, () =>
            {
                Tween.AnchoredMove(c.rect, c.homePos + new Vector2(0f, 360f), 0.28f, Ease.InQuad, () =>
                {
                    c.animating = false;
                    ApplyTicket(c, board.TicketInSlot(railSlot));
                    c.rect.anchoredPosition = c.homePos + new Vector2(0f, 360f);
                    Tween.AnchoredMove(c.rect, c.homePos, 0.32f, Ease.OutCubic, null, true);
                    onDone?.Invoke();
                }, true);
            }, true);
        }

        public void Pulse(int railSlot)
        {
            if (railSlot < 0 || railSlot >= cards.Length) return;
            UIBuilder.Punch(cards[railSlot].rect, 0.1f, 0.3f);
        }
    }
}
