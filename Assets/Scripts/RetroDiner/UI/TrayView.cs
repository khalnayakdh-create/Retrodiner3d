using System;
using UnityEngine;
using UnityEngine.UI;
using RetroDiner.Core;
using RetroDiner.Data;
using RetroDiner.Rules;

namespace RetroDiner.UI
{
    /// <summary>
    /// The 7-slot (8 with Extra Plate) holding tray at the bottom of the screen plus the item-flight
    /// animations into and out of it. The model is always the truth; this view re-syncs on landing.
    /// </summary>
    public class TrayView : MonoBehaviour
    {
        RectTransform root;
        RectTransform flightLayer;
        Image trayImage;
        RectTransform slotsRow;
        RectTransform[] slotRects = new RectTransform[0];
        Image[] slotIcons = new Image[0];
        Image[] slotBacks = new Image[0];
        GameConfig cfg;
        int capacity;
        Func<string, ItemDefinition> lookup;

        const float SlotSize = 118f;
        const float SlotGap = 10f;

        public RectTransform Root => root;

        public void Build(Transform parent, RectTransform flightLayer, GameConfig config, Func<string, ItemDefinition> defLookup)
        {
            cfg = config;
            lookup = defLookup;
            this.flightLayer = flightLayer;
            root = UIBuilder.Rect(parent, "Tray");
            UIBuilder.Place(root, new Vector2(0.5f, 0f), new Vector2(1040f, 230f), new Vector2(0f, 420f));

            var traySprite = ArtCatalog.Pick(cfg.tray7, ArtCatalog.Tray7);
            trayImage = UIBuilder.Panel(root, "TrayArt", traySprite != null ? Color.white : UIBuilder.Chrome, traySprite, false);
            UIBuilder.Stretch(trayImage.rectTransform);
            trayImage.preserveAspect = false;
            if (traySprite == null)
            {
                UIBuilder.Shadow(trayImage, 8f);
                var inner = UIBuilder.Panel(root, "TrayInner", new Color(0.55f, 0.57f, 0.6f), null, false);
                UIBuilder.Stretch(inner.rectTransform, 14, 14, 14, 14);
            }

            slotsRow = UIBuilder.Rect(root, "Slots");
            UIBuilder.Stretch(slotsRow, 20, 20, 20, 20);
            SetCapacity(cfg.trayCapacity);
        }

        public void SetCapacity(int newCapacity)
        {
            if (newCapacity == capacity && slotRects.Length == newCapacity) return;
            capacity = newCapacity;
            foreach (var r in slotRects) if (r != null) Destroy(r.gameObject);
            slotRects = new RectTransform[capacity];
            slotIcons = new Image[capacity];
            slotBacks = new Image[capacity];
            float totalW = capacity * SlotSize + (capacity - 1) * SlotGap;
            float scale = Mathf.Min(1f, 1000f / totalW);
            float size = SlotSize * scale, gap = SlotGap * scale;
            float startX = -(capacity * size + (capacity - 1) * gap) * 0.5f + size * 0.5f;
            for (int i = 0; i < capacity; i++)
            {
                var back = UIBuilder.Panel(slotsRow, "Slot" + i, new Color(0f, 0f, 0f, 0.22f), null, false);
                UIBuilder.Place(back.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(size, size), new Vector2(startX + i * (size + gap), 0f));
                slotRects[i] = back.rectTransform;
                slotBacks[i] = back;
                var icon = UIBuilder.Panel(back.transform, "Icon", Color.white, null, false);
                UIBuilder.Stretch(icon.rectTransform, 6, 6, 6, 6);
                icon.preserveAspect = true;
                icon.enabled = false;
                slotIcons[i] = icon;
            }
            var traySprite = capacity >= 8 ? ArtCatalog.Pick(cfg.tray8, ArtCatalog.Tray8) : ArtCatalog.Pick(cfg.tray7, ArtCatalog.Tray7);
            if (traySprite != null) { trayImage.sprite = traySprite; trayImage.color = Color.white; }
        }

        public Vector2 SlotScreenPosition(int index)
        {
            if (index < 0 || index >= slotRects.Length) index = Mathf.Clamp(index, 0, slotRects.Length - 1);
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
                    var def = lookup(e.id);
                    ApplyIcon(slotIcons[i], def, e.charred);
                    slotIcons[i].enabled = true;
                    slotBacks[i].color = new Color(0f, 0f, 0f, 0.1f);
                }
                else
                {
                    slotIcons[i].enabled = false;
                    slotBacks[i].color = new Color(0f, 0f, 0f, 0.22f);
                }
            }
            // Warning tint when nearly full.
            float fill = model.Count / (float)model.Capacity;
            if (trayImage.sprite == null) trayImage.color = Color.Lerp(UIBuilder.Chrome, new Color(0.95f, 0.45f, 0.4f), Mathf.Clamp01((fill - 0.6f) * 2.5f));
        }

        public static void ApplyIcon(Image img, ItemDefinition def, bool charred)
        {
            if (def == null) { img.sprite = null; img.color = Color.magenta; return; }
            var s = charred && def.charredIcon != null ? def.charredIcon : def.icon;
            img.sprite = s;
            img.color = s != null ? (charred && def.charredIcon == null ? new Color(0.4f, 0.35f, 0.33f) : Color.white)
                                  : (charred ? Color.Lerp(def.placeholderColor, Color.black, 0.6f) : def.placeholderColor);
        }

        /// <summary>Fly an icon from a screen position into a slot. Calls onLanded when it arrives.</summary>
        public void FlyIn(Vector2 fromScreen, ItemDefinition def, bool charred, int slotIndex, Action onLanded)
        {
            var flyer = UIBuilder.Panel(flightLayer, "Flyer", Color.white, null, false);
            flyer.preserveAspect = true;
            ApplyIcon(flyer, def, charred);
            flyer.rectTransform.sizeDelta = new Vector2(150f, 150f);
            Vector2 start = UIBuilder.ScreenToLocal(flightLayer, fromScreen);
            Vector2 end = UIBuilder.ScreenToLocal(flightLayer, SlotScreenPosition(slotIndex));
            flyer.rectTransform.anchoredPosition = start;
            Vector2 ctrl = (start + end) * 0.5f + Vector2.up * 220f;
            Tween.To(0.25f, t =>
            {
                if (flyer == null) return;
                Vector2 p = Bezier(start, ctrl, end, t);
                flyer.rectTransform.anchoredPosition = p;
                flyer.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.85f, t);
            }, Ease.OutQuad, () =>
            {
                if (flyer != null) Destroy(flyer.gameObject);
                if (slotIndex >= 0 && slotIndex < slotRects.Length) UIBuilder.Punch(slotRects[slotIndex], 0.2f, 0.2f);
                onLanded?.Invoke();
            }, true);
        }

        /// <summary>Shrink three ghost icons at the given (pre-removal) slot indices and fly them to a target.</summary>
        public void FlyOutTriplet(int[] slotIndices, ItemDefinition def, bool charred, Vector2 targetScreen, Action onDone)
        {
            int remaining = slotIndices.Length;
            Vector2 end = UIBuilder.ScreenToLocal(flightLayer, targetScreen);
            // Merge point: center of the three slots.
            Vector2 merge = Vector2.zero;
            foreach (var i in slotIndices) merge += UIBuilder.ScreenToLocal(flightLayer, SlotScreenPosition(i));
            merge /= Mathf.Max(1, slotIndices.Length);

            foreach (var idx in slotIndices)
            {
                var ghost = UIBuilder.Panel(flightLayer, "Ghost", Color.white, null, false);
                ghost.preserveAspect = true;
                ApplyIcon(ghost, def, charred);
                ghost.rectTransform.sizeDelta = new Vector2(110f, 110f);
                Vector2 start = UIBuilder.ScreenToLocal(flightLayer, SlotScreenPosition(idx));
                ghost.rectTransform.anchoredPosition = start;
                var g = ghost;
                // Phase 1: merge (0.15s). Phase 2: fly to ticket (0.25s).
                Tween.To(0.15f, t =>
                {
                    if (g == null) return;
                    g.rectTransform.anchoredPosition = Vector2.LerpUnclamped(start, merge, t);
                    g.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.7f, t);
                }, Ease.InQuad, () =>
                {
                    Vector2 ctrl = (merge + end) * 0.5f + Vector2.right * 120f;
                    Tween.To(0.25f, t =>
                    {
                        if (g == null) return;
                        g.rectTransform.anchoredPosition = Bezier(merge, ctrl, end, t);
                        g.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.7f, 0.3f, t);
                    }, Ease.InQuad, () =>
                    {
                        if (g != null) Destroy(g.gameObject);
                        remaining--;
                        if (remaining <= 0) onDone?.Invoke();
                    }, true);
                }, true);
            }
            if (slotIndices.Length == 0) onDone?.Invoke();
        }

        /// <summary>Generic icon flight between two screen points (used by the Golden Spatula).</summary>
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
            Tween.To(duration, t =>
            {
                if (flyer == null) return;
                flyer.rectTransform.anchoredPosition = Bezier(start, ctrl, end, t);
                flyer.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.1f, 0.4f, t);
            }, Ease.InOutQuad, () =>
            {
                if (flyer != null) Destroy(flyer.gameObject);
                onDone?.Invoke();
            }, true);
        }

        public static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
        }
    }
}
