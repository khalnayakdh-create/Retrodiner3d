using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RetroDiner.Core;
using RetroDiner.Data;
using RetroDiner.Rules;

namespace RetroDiner.UI
{
    /// <summary>
    /// Seven-slot (eight with Extra Plate) holding tray: rounded chrome frame, teal counter trim, dark
    /// recessed wells, pair glow, landing settle, and the 13A match-to-order flight. The TrayModel is
    /// always the truth; this view presents it and re-syncs on every landing.
    /// Art: ui/rd3d_ui_tray_frame_v002 (9-slice) + ui/rd3d_ui_tray_well_v001 when present, else procedural.
    /// </summary>
    public class TrayView : MonoBehaviour
    {
        RectTransform root, flightLayer, slotsRow;
        Image frame;
        RectTransform[] slotRects = new RectTransform[0];
        Image[] slotIcons = new Image[0];
        Image[] slotWells = new Image[0];
        Image[] slotGlows = new Image[0];
        GameConfig cfg;
        int capacity;
        Func<string, ItemDefinition> lookup;
        Sprite frameSprite, wellSprite, glowSprite, sparkleSprite;
        readonly List<Image> trailPool = new List<Image>();

        const float TrayW = 1016f;     // ~94% of the 1080 reference width
        const float TrayH = 190f;

        public RectTransform Root => root;

        public void Build(Transform parent, RectTransform flightLayer, GameConfig config, Func<string, ItemDefinition> defLookup)
        {
            cfg = config;
            lookup = defLookup;
            this.flightLayer = flightLayer;
            frameSprite = ArtCatalog.Load("ui/rd3d_ui_tray_frame_v002");
            wellSprite = ArtCatalog.Load("ui/rd3d_ui_tray_well_v001");
            glowSprite = ArtCatalog.Load("ui/rd3d_ui_slot_well_glow_v001");
            sparkleSprite = ArtCatalog.Load("fx/rd3d_fx_sparkle_gold_v001");

            root = UIBuilder.Rect(parent, "Tray");
            UIBuilder.Place(root, new Vector2(0.5f, 0f), new Vector2(TrayW, TrayH), new Vector2(0f, Style.TrayY));

            // Drop shadow under the whole tray so it sits on the counter.
            var shadow = UIBuilder.Pill(root, "Shadow", new Color(0f, 0f, 0f, 0.38f), 40);
            UIBuilder.Stretch(shadow.rectTransform, -6, -4, -6, -16);

            if (frameSprite != null)
            {
                // The chrome frame art has an open centre: a brushed-steel inner panel sits behind it.
                var inner = UIBuilder.Pill(root, "Inner", new Color(0.52f, 0.57f, 0.62f), 28);
                UIBuilder.Stretch(inner.rectTransform, 12, 12, 12, 12);
                var innerShade = UIBuilder.Panel(inner.transform, "Shade", new Color(0f, 0f, 0f, 0.25f), ProcSprites.VerticalGradient(1f, 0f), false);
                UIBuilder.Stretch(innerShade.rectTransform); innerShade.sprite = ProcSprites.Rounded(28); innerShade.type = Image.Type.Sliced; innerShade.color = new Color(0f, 0f, 0f, 0.18f);
                frame = UIBuilder.Sliced(root, "Frame", frameSprite, Style.Chrome, 38, false);
                UIBuilder.Stretch(frame.rectTransform);
            }
            else
            {
                frame = UIBuilder.Pill(root, "Frame", Style.Chrome, 38);
                UIBuilder.Stretch(frame.rectTransform);
                var sheen = UIBuilder.Panel(frame.transform, "Sheen", new Color(1f, 1f, 1f, 0.35f), ProcSprites.Sheen(64), false);
                UIBuilder.Stretch(sheen.rectTransform); sheen.sprite = ProcSprites.Rounded(38); sheen.type = Image.Type.Sliced; sheen.color = new Color(1f, 1f, 1f, 0.18f);
                var trim = UIBuilder.Pill(frame.transform, "Trim", Style.Teal, 6);
                trim.rectTransform.anchorMin = new Vector2(0f, 1f); trim.rectTransform.anchorMax = new Vector2(1f, 1f);
                trim.rectTransform.pivot = new Vector2(0.5f, 1f); trim.rectTransform.sizeDelta = new Vector2(-70f, 8f); trim.rectTransform.anchoredPosition = new Vector2(0f, -8f);
                var inner = UIBuilder.Pill(frame.transform, "Inner", new Color(0.6f, 0.65f, 0.7f), 28);
                UIBuilder.Stretch(inner.rectTransform, 14, 22, 14, 14);
                var innerShade = UIBuilder.Panel(inner.transform, "Shade", new Color(0f, 0f, 0f, 0.25f), ProcSprites.VerticalGradient(1f, 0f), false);
                UIBuilder.Stretch(innerShade.rectTransform); innerShade.sprite = ProcSprites.Rounded(28); innerShade.type = Image.Type.Sliced; innerShade.color = new Color(0f, 0f, 0f, 0.12f);
                for (int r = 0; r < 2; r++)
                {
                    var rivet = UIBuilder.Panel(frame.transform, "Rivet" + r, Style.ChromeLight, ProcSprites.Circle(32), false);
                    UIBuilder.Place(rivet.rectTransform, new Vector2(r == 0 ? 0f : 1f, 0.5f), new Vector2(14f, 14f), new Vector2(r == 0 ? 16f : -16f, 0f));
                }
            }

            slotsRow = UIBuilder.Rect(root, "Slots");
            UIBuilder.Stretch(slotsRow, 24, 28, 24, 22);
            SetCapacity(cfg.trayCapacity);
        }

        public void SetCapacity(int newCapacity)
        {
            if (newCapacity == capacity && slotRects.Length == newCapacity) return;
            capacity = newCapacity;
            foreach (var r in slotRects) if (r != null) Destroy(r.gameObject);
            slotRects = new RectTransform[capacity];
            slotIcons = new Image[capacity];
            slotWells = new Image[capacity];
            slotGlows = new Image[capacity];
            float usable = TrayW - 48f;
            float gap = 10f;
            float size = Mathf.Min(TrayH - 50f, (usable - gap * (capacity - 1)) / capacity);
            float startX = -(capacity * size + (capacity - 1) * gap) * 0.5f + size * 0.5f;
            for (int i = 0; i < capacity; i++)
            {
                Image well = wellSprite != null ? UIBuilder.Panel(slotsRow, "Slot" + i, Color.white, wellSprite, false)
                                                : UIBuilder.Pill(slotsRow, "Slot" + i, new Color(0.27f, 0.29f, 0.32f), 14);
                UIBuilder.Place(well.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(size, size), new Vector2(startX + i * (size + gap), 0f));
                if (wellSprite == null)
                {
                    var innerShadow = UIBuilder.Panel(well.transform, "InnerShadow", new Color(0f, 0f, 0f, 0.45f), ProcSprites.VerticalGradient(1f, 0f), false);
                    UIBuilder.Stretch(innerShadow.rectTransform); innerShadow.sprite = ProcSprites.Rounded(14); innerShadow.type = Image.Type.Sliced; innerShadow.color = new Color(0f, 0f, 0f, 0.28f);
                    var reflect = UIBuilder.Panel(well.transform, "Reflect", new Color(1f, 1f, 1f, 0.1f), ProcSprites.VerticalGradient(0f, 1f), false);
                    reflect.rectTransform.anchorMin = new Vector2(0.1f, 0f); reflect.rectTransform.anchorMax = new Vector2(0.9f, 0.35f);
                    reflect.rectTransform.offsetMin = Vector2.zero; reflect.rectTransform.offsetMax = Vector2.zero;
                    UIBuilder.Outline(well, new Color(0.85f, 0.88f, 0.92f, 0.5f), 1.5f);
                }
                slotRects[i] = well.rectTransform;
                slotWells[i] = well;

                var glow = UIBuilder.Panel(well.transform, "Glow", new Color(1f, 0.85f, 0.3f, 0f), glowSprite != null ? glowSprite : ProcSprites.Glow(128, 1.6f), false);
                UIBuilder.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(size * 1.9f, size * 1.9f), Vector2.zero);
                UIBuilder.Pulse(glow.rectTransform, 0.08f, 0.9f);
                slotGlows[i] = glow;

                var icon = UIBuilder.Panel(well.transform, "Icon", Color.white, null, false);
                UIBuilder.Stretch(icon.rectTransform, 4, 2, 4, 6);
                icon.preserveAspect = true;
                icon.enabled = false;
                slotIcons[i] = icon;
            }
        }

        public Vector2 SlotScreenPosition(int index)
        {
            if (slotRects.Length == 0) return new Vector2(Screen.width * 0.5f, Screen.height * 0.2f);
            index = Mathf.Clamp(index, 0, slotRects.Length - 1);
            return UIBuilder.LocalToScreen(slotRects[index]);
        }

        public void Refresh(TrayModel model)
        {
            if (model.Capacity != capacity) SetCapacity(model.Capacity);
            for (int i = 0; i < capacity; i++)
            {
                if (i < model.Count)
                {
                    var e = model.Slots[i];
                    ApplyIcon(slotIcons[i], lookup(e.id), e.charred);
                    slotIcons[i].enabled = true;
                }
                else slotIcons[i].enabled = false;
                bool pair = i < model.Count && model.CountOf(model.Slots[i].id) == 2;
                var c = slotGlows[i].color; c.a = pair ? 0.7f : 0f; slotGlows[i].color = c;
            }
        }

        public static void ApplyIcon(Image img, ItemDefinition def, bool charred)
        {
            if (def == null) { img.sprite = null; img.color = Color.magenta; return; }
            var s = charred && def.charredIcon != null ? def.charredIcon : def.icon;
            img.sprite = s;
            img.color = s != null ? (charred && def.charredIcon == null ? new Color(0.4f, 0.35f, 0.33f) : Color.white)
                                  : (charred ? Color.Lerp(def.placeholderColor, Color.black, 0.6f) : def.placeholderColor);
        }

        // ---------------------------------------------------------------- belt -> tray

        /// <summary>Belt-to-tray transfer: short arc to the actual receiving slot, settle bounce on landing.</summary>
        public void FlyIn(Vector2 fromScreen, ItemDefinition def, bool charred, int slotIndex, Action onLanded)
        {
            if (Style.ReducedMotion)
            {
                Tween.Delay(0.08f, () => { if (slotIndex >= 0 && slotIndex < slotRects.Length) UIBuilder.Punch(slotRects[slotIndex], 0.1f, 0.15f); onLanded?.Invoke(); }, true);
                return;
            }
            var flyer = UIBuilder.Panel(flightLayer, "Flyer", Color.white, null, false);
            flyer.preserveAspect = true;
            ApplyIcon(flyer, def, charred);
            flyer.rectTransform.sizeDelta = new Vector2(150f, 150f);
            Vector2 start = UIBuilder.ScreenToLocal(flightLayer, fromScreen);
            Vector2 end = UIBuilder.ScreenToLocal(flightLayer, SlotScreenPosition(slotIndex));
            flyer.rectTransform.anchoredPosition = start;
            Vector2 ctrl = (start + end) * 0.5f + Vector2.up * 200f;
            Tween.To(Style.TrayLanding, t =>
            {
                if (flyer == null) return;
                flyer.rectTransform.anchoredPosition = Bezier(start, ctrl, end, t);
                flyer.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.8f, t);
            }, Ease.OutQuad, () =>
            {
                if (flyer != null) Destroy(flyer.gameObject);
                if (slotIndex >= 0 && slotIndex < slotRects.Length)
                {
                    // Settle bounce: squash then relax.
                    var rt = slotRects[slotIndex];
                    rt.localScale = new Vector3(1.14f, 0.88f, 1f);
                    Tween.To(0.22f, t => { if (rt != null) rt.localScale = Vector3.LerpUnclamped(new Vector3(1.14f, 0.88f, 1f), Vector3.one, t); }, Ease.OutBack, null, true);
                }
                onLanded?.Invoke();
            }, true);
        }

        // ---------------------------------------------------------------- 13A: match -> order

        /// <summary>
        /// Match-to-order: (1) confirm — matched slots glow gold, icons scale to ~110% with a few sparkles
        /// (~150 ms); (2) three dish visuals lift and launch, staggered ~60 ms; (3) each follows a golden
        /// arc with a fading trail to the ticket icon (~420 ms, slight rotation, shrinking); (4) onLaunch
        /// fires when the first dish leaves (ticket anticipation), onDone when the last arrives.
        /// The game state was already resolved by the caller; this only presents it.
        /// </summary>
        public void FlyOutTriplet(int[] slotIndices, ItemDefinition def, bool charred, Vector2 targetScreen, Action onDone, Action onLaunch = null)
        {
            if (slotIndices == null || slotIndices.Length == 0) { onDone?.Invoke(); return; }
            Vector2 end = UIBuilder.ScreenToLocal(flightLayer, targetScreen);
            var starts = new Vector2[slotIndices.Length];
            for (int i = 0; i < slotIndices.Length; i++) starts[i] = UIBuilder.ScreenToLocal(flightLayer, SlotScreenPosition(slotIndices[i]));

            // Temporary dish visuals (non-interactive) over the matched slots.
            var ghosts = new Image[slotIndices.Length];
            for (int i = 0; i < slotIndices.Length; i++)
            {
                var g = UIBuilder.Panel(flightLayer, "MatchDish", Color.white, null, false);
                g.preserveAspect = true;
                ApplyIcon(g, def, charred);
                g.rectTransform.sizeDelta = new Vector2(118f, 118f);
                g.rectTransform.anchoredPosition = starts[i];
                ghosts[i] = g;
            }

            // (1) Confirm.
            for (int i = 0; i < slotIndices.Length; i++)
            {
                int idx = slotIndices[i];
                if (idx >= 0 && idx < slotGlows.Length)
                {
                    var gl = slotGlows[idx];
                    Tween.To(Style.MatchConfirm, t => { if (gl != null) { var c = gl.color; c.a = Mathf.Lerp(c.a, 1f, t); gl.color = c; } }, Ease.OutQuad, null, true);
                }
                var ghost = ghosts[i];
                Tween.To(Style.MatchConfirm, t => { if (ghost != null) ghost.rectTransform.localScale = Vector3.one * (1f + 0.1f * Mathf.Sin(t * Mathf.PI)); }, Ease.Linear, null, true);
                Sparkle(starts[i] + new Vector2(UnityEngine.Random.Range(-40f, 40f), 50f), 0.9f);
            }

            if (Style.ReducedMotion)
            {
                // Brief fade in place + destination pulse; same result, no long flight.
                Tween.Delay(Style.MatchConfirm, () =>
                {
                    onLaunch?.Invoke();
                    foreach (var g in ghosts) if (g != null) Tween.To(0.2f, t => { if (g != null) { var c = g.color; c.a = 1f - t; g.color = c; } }, Ease.Linear, () => { if (g != null) Destroy(g.gameObject); }, true);
                    Tween.Delay(0.22f, () => onDone?.Invoke(), true);
                }, true);
                return;
            }

            // (2)+(3) Lift, stagger, arc.
            int remaining = slotIndices.Length;
            Vector2 mean = Vector2.zero; foreach (var s in starts) mean += s; mean /= starts.Length;
            float dir = end.x < mean.x ? -1f : 1f;      // curve outward along the side the ticket is on
            for (int i = 0; i < slotIndices.Length; i++)
            {
                var g = ghosts[i];
                Vector2 start = starts[i];
                float delay = Style.MatchConfirm + i * Style.MatchStagger;
                bool first = i == 0;
                Tween.Delay(delay, () =>
                {
                    if (first)
                    {
                        onLaunch?.Invoke();
                        foreach (var si in slotIndices)
                            if (si >= 0 && si < slotGlows.Length) { var gl = slotGlows[si]; Tween.To(0.25f, t => { if (gl != null) { var c = gl.color; c.a = Mathf.Lerp(c.a, 0f, t); gl.color = c; } }, Ease.OutQuad, null, true); }
                    }
                    if (g == null) { if (--remaining <= 0) onDone?.Invoke(); return; }
                    Vector2 lifted = start + new Vector2(0f, 46f);
                    // Control point pushed outward and up so the path arcs along the side of the playfield.
                    Vector2 ctrl = new Vector2(Mathf.Lerp(lifted.x, end.x, 0.35f) + dir * 140f, Mathf.Lerp(lifted.y, end.y, 0.55f) + 180f);
                    float trailClock = 0f;
                    float rot = UnityEngine.Random.Range(-8f, 8f);
                    Tween.To(0.07f, t => { if (g != null) g.rectTransform.anchoredPosition = Vector2.Lerp(start, lifted, t); }, Ease.OutQuad, () =>
                    {
                        Tween.To(Style.MatchFlight, t =>
                        {
                            if (g == null) return;
                            Vector2 p = Bezier(lifted, ctrl, end, t);
                            g.rectTransform.anchoredPosition = p;
                            g.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.05f, 0.45f, t);
                            g.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rot * Mathf.Sin(t * Mathf.PI));
                            trailClock += Time.unscaledDeltaTime;
                            if (trailClock >= 0.025f) { trailClock = 0f; Trail(p, 1f - t * 0.6f); if (UnityEngine.Random.value < 0.18f) Sparkle(p + UnityEngine.Random.insideUnitCircle * 30f, 0.6f); }
                        }, Ease.InOutQuad, () =>
                        {
                            if (g != null) Destroy(g.gameObject);
                            if (--remaining <= 0) onDone?.Invoke();
                        }, true);
                    }, true);
                }, true);
            }
        }

        /// <summary>Generic icon flight between two screen points (Golden Spatula: belt → ticket, per the bible).</summary>
        public void FlyIcon(Vector2 fromScreen, ItemDefinition def, bool charred, Vector2 toScreen, float duration, Action onDone)
        {
            var flyer = UIBuilder.Panel(flightLayer, "Flyer", Color.white, null, false);
            flyer.preserveAspect = true;
            ApplyIcon(flyer, def, charred);
            flyer.rectTransform.sizeDelta = new Vector2(130f, 130f);
            Vector2 start = UIBuilder.ScreenToLocal(flightLayer, fromScreen);
            Vector2 end = UIBuilder.ScreenToLocal(flightLayer, toScreen);
            flyer.rectTransform.anchoredPosition = start;
            Vector2 ctrl = (start + end) * 0.5f + Vector2.right * 160f;
            float clock = 0f;
            Tween.To(duration, t =>
            {
                if (flyer == null) return;
                Vector2 p = Bezier(start, ctrl, end, t);
                flyer.rectTransform.anchoredPosition = p;
                flyer.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.1f, 0.45f, t);
                clock += Time.unscaledDeltaTime;
                if (!Style.ReducedMotion && clock >= 0.03f) { clock = 0f; Trail(p, 1f - t * 0.5f); }
            }, Ease.InOutQuad, () =>
            {
                if (flyer != null) Destroy(flyer.gameObject);
                onDone?.Invoke();
            }, true);
        }

        // ---------------------------------------------------------------- fx helpers

        /// <summary>Thin golden trail with a cream centre: a short-lived glow blob at the flyer's position.</summary>
        void Trail(Vector2 localPos, float strength)
        {
            Image img = null;
            foreach (var t in trailPool) if (!t.gameObject.activeSelf) { img = t; break; }
            if (img == null)
            {
                img = UIBuilder.Panel(flightLayer, "Trail", Color.white, ProcSprites.Glow(64, 1.8f), false);
                trailPool.Add(img);
            }
            img.gameObject.SetActive(true);
            img.transform.SetAsFirstSibling();
            img.rectTransform.anchoredPosition = localPos;
            float size = 54f * strength;
            img.rectTransform.sizeDelta = new Vector2(size, size);
            var g = img;
            Tween.To(0.26f, t =>
            {
                if (g == null) return;
                g.color = Color.Lerp(new Color(1f, 0.97f, 0.8f, 0.95f * strength), new Color(1f, 0.78f, 0.25f, 0f), t);
                g.rectTransform.localScale = Vector3.one * (1f - 0.5f * t);
            }, Ease.OutQuad, () => { if (g != null) g.gameObject.SetActive(false); }, true);
        }

        /// <summary>A short-lived gold star particle.</summary>
        public void Sparkle(Vector2 localPos, float scale)
        {
            var img = UIBuilder.Panel(flightLayer, "Sparkle", new Color(1f, 0.9f, 0.5f, 1f), sparkleSprite != null ? sparkleSprite : ProcSprites.Glow(64, 3f), false);
            img.preserveAspect = true;
            img.rectTransform.sizeDelta = new Vector2(46f * scale, 46f * scale);
            img.rectTransform.anchoredPosition = localPos;
            img.rectTransform.localScale = Vector3.zero;
            var rt = img.rectTransform;
            Vector2 drift = new Vector2(UnityEngine.Random.Range(-18f, 18f), UnityEngine.Random.Range(10f, 36f));
            Tween.To(0.38f, t =>
            {
                if (rt == null) return;
                rt.localScale = Vector3.one * Mathf.Sin(t * Mathf.PI);
                rt.anchoredPosition = localPos + drift * t;
                rt.localRotation = Quaternion.Euler(0f, 0f, 90f * t);
            }, Ease.Linear, () => { if (rt != null) Destroy(rt.gameObject); }, true);
        }

        public void SparkleAtScreen(Vector2 screen, int count)
        {
            Vector2 local = UIBuilder.ScreenToLocal(flightLayer, screen);
            for (int i = 0; i < count; i++) Sparkle(local + UnityEngine.Random.insideUnitCircle * 40f, UnityEngine.Random.Range(0.6f, 1f));
        }

        public static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
        }
    }
}
