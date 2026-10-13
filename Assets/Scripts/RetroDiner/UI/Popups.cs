using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RetroDiner.Core;
using RetroDiner.Data;

namespace RetroDiner.UI
{
    public class PanelButton
    {
        public string label; public Action onClick; public Color color; public bool interactable = true; public bool secondary;
        public PanelButton(string label, Action onClick, Color color) { this.label = label; this.onClick = onClick; this.color = color; }
    }

    /// <summary>
    /// One global popup layer above HUD and menu. Modal frames are content-driven: cream body, bevelled
    /// chrome edge, a short gingham header (procedural, or ui/rd3d_ui_modal_frame_v002 when present),
    /// one uniform dim layer behind, a 200 ms fade/scale with no long overshoot, and click-through blocked.
    /// </summary>
    public static class Popups
    {
        public static RectTransform Layer { get; private set; }
        static RectTransform current;
        static GameConfig cfg;
        static Sprite frameSprite, btnRed, btnMint, btnGold, btnGreen;

        public static bool IsOpen => current != null;

        public static void Init(Canvas canvas, GameConfig config)
        {
            cfg = config;
            Layer = UIBuilder.Rect(canvas.transform, "PopupLayer");
            UIBuilder.Stretch(Layer);
            Layer.gameObject.AddComponent<SafeAreaFitter>();
            frameSprite = ArtCatalog.Load("ui/rd3d_ui_modal_frame_v002");
            btnRed = ArtCatalog.Pick(cfg.buttonRed, ArtCatalog.ButtonRed);
            btnMint = ArtCatalog.Pick(cfg.buttonMint, ArtCatalog.ButtonMint);
            btnGold = ArtCatalog.Pick(cfg.buttonGold, ArtCatalog.ButtonGold);
            btnGreen = ArtCatalog.Load("ui/rd3d_ui_button_green_v001");
        }

        public static void Close()
        {
            if (current != null) UnityEngine.Object.Destroy(current.gameObject);
            current = null;
        }

        public static Sprite ButtonSprite(Color c)
        {
            if (c == UIBuilder.Red || c == Style.Cherry) return btnRed;
            if (c == UIBuilder.Mint || c == Style.Mint) return btnMint;
            if (c == UIBuilder.Gold || c == Style.Gold) return btnGold;
            if (c == Style.Teal) return btnGreen;
            return null;
        }

        /// <summary>Content-driven modal. Height = header + content + buttons; width ~86% of the safe viewport.</summary>
        public static RectTransform Show(string title, string body, List<PanelButton> buttons, bool dim = true, Action<RectTransform> decorate = null, float contentHeight = 320f, float width = 930f)
        {
            Close();
            var overlay = UIBuilder.Panel(Layer, "Overlay", new Color(0.1f, 0.05f, 0.06f, dim ? 0.6f : 0f), null, true);
            UIBuilder.Stretch(overlay.rectTransform);
            current = overlay.rectTransform;

            const float header = 96f, pad = 26f, btnH = 118f, btnGap = 18f, bodyPad = 20f;
            int n = buttons != null ? buttons.Count : 0;
            float bodyH = string.IsNullOrEmpty(body) ? 0f : 90f;
            float panelH = header + bodyPad + bodyH + contentHeight + pad + n * (btnH + btnGap) + pad;
            float maxH = UIBuilder.RefHeight * 0.78f;
            if (panelH > maxH) { contentHeight -= (panelH - maxH); panelH = maxH; }

            var holder = UIBuilder.Rect(overlay.transform, "Panel");
            UIBuilder.Place(holder, new Vector2(0.5f, 0.5f), new Vector2(width, panelH), new Vector2(0f, 30f));

            var shadow = UIBuilder.Pill(holder, "Shadow", new Color(0f, 0f, 0f, 0.4f), 44);
            UIBuilder.Stretch(shadow.rectTransform, -4, -8, -4, -22);

            Image panel;
            if (frameSprite != null)
            {
                panel = UIBuilder.Sliced(holder, "Frame", frameSprite, Style.Cream, 40, true);
                UIBuilder.Stretch(panel.rectTransform);
            }
            else
            {
                var edge = UIBuilder.Pill(holder, "Edge", Style.Chrome, 40, true);
                UIBuilder.Stretch(edge.rectTransform);
                var edgeSheen = UIBuilder.Panel(edge.transform, "Sheen", new Color(1f, 1f, 1f, 0.15f), ProcSprites.Rounded(40), false); edgeSheen.type = Image.Type.Sliced;
                UIBuilder.Stretch(edgeSheen.rectTransform, 0, 0, 0, panelH * 0.5f);
                panel = UIBuilder.Pill(holder, "Body", Style.Cream, 34, true);
                UIBuilder.Stretch(panel.rectTransform, 8, 8, 8, 8);
                var gingham = UIBuilder.Panel(panel.transform, "Header", Color.white, ProcSprites.Gingham(22), false);
                gingham.type = Image.Type.Tiled;
                gingham.rectTransform.anchorMin = new Vector2(0f, 1f); gingham.rectTransform.anchorMax = new Vector2(1f, 1f);
                gingham.rectTransform.pivot = new Vector2(0.5f, 1f); gingham.rectTransform.sizeDelta = new Vector2(0f, header); gingham.rectTransform.anchoredPosition = Vector2.zero;
                var headerMask = gingham.gameObject.AddComponent<Mask>(); headerMask.showMaskGraphic = true;
                var headerRound = UIBuilder.Panel(panel.transform, "HeaderRound", Style.Cream, ProcSprites.Rounded(34), false);   // keeps the top corners rounded
                headerRound.type = Image.Type.Sliced; headerRound.fillCenter = false;
                UIBuilder.Stretch(headerRound.rectTransform, -1, -1, -1, -1);
                var divider = UIBuilder.Panel(panel.transform, "Divider", Style.Chrome, null, false);
                divider.rectTransform.anchorMin = new Vector2(0f, 1f); divider.rectTransform.anchorMax = new Vector2(1f, 1f);
                divider.rectTransform.pivot = new Vector2(0.5f, 1f); divider.rectTransform.sizeDelta = new Vector2(0f, 4f); divider.rectTransform.anchoredPosition = new Vector2(0f, -header);
                for (int r = 0; r < 4; r++)
                {
                    var rivet = UIBuilder.Panel(panel.transform, "Rivet" + r, Style.ChromeLight, ProcSprites.Circle(32), false);
                    UIBuilder.Place(rivet.rectTransform, new Vector2(r % 2 == 0 ? 0f : 1f, r < 2 ? 1f : 0f), new Vector2(14f, 14f), new Vector2(r % 2 == 0 ? 18f : -18f, r < 2 ? -18f : 18f));
                }
            }

            // Cherry ribbon behind the title so it reads on the gingham header.
            var ribbon = UIBuilder.Pill(holder, "TitleRibbon", Style.Cherry, 30);
            UIBuilder.Place(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(Mathf.Clamp(title.Length * 30f + 120f, 360f, width - 120f), 70f), new Vector2(0f, -header * 0.5f - 4f));
            UIBuilder.Outline(ribbon, Style.ChromeLight, 3f);
            UIBuilder.Shadow(ribbon, 4f);
            var titleLbl = UIBuilder.Label(holder, "Title", title, 46, Style.Cream);
            titleLbl.rectTransform.anchorMin = new Vector2(0f, 1f); titleLbl.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleLbl.rectTransform.pivot = new Vector2(0.5f, 1f); titleLbl.rectTransform.sizeDelta = new Vector2(-60f, header);
            titleLbl.rectTransform.anchoredPosition = new Vector2(0f, -4f);
            var tsh = titleLbl.gameObject.AddComponent<Shadow>(); tsh.effectColor = new Color(0.3f, 0.05f, 0.08f, 0.9f); tsh.effectDistance = new Vector2(2f, -2.5f);

            float y = -(header + 8f + bodyPad);
            if (!string.IsNullOrEmpty(body))
            {
                var bodyLbl = UIBuilder.Label(holder, "Body", body, 34, Style.Ink, TextAnchor.UpperCenter, FontStyle.Normal);
                bodyLbl.rectTransform.anchorMin = new Vector2(0f, 1f); bodyLbl.rectTransform.anchorMax = new Vector2(1f, 1f);
                bodyLbl.rectTransform.pivot = new Vector2(0.5f, 1f); bodyLbl.rectTransform.sizeDelta = new Vector2(-90f, bodyH);
                bodyLbl.rectTransform.anchoredPosition = new Vector2(0f, y);
                y -= bodyH;
            }
            var content = UIBuilder.Rect(holder, "Content");
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(-70f, contentHeight); content.anchoredPosition = new Vector2(0f, y);
            decorate?.Invoke(content);
            y -= contentHeight + pad;

            if (buttons != null)
                foreach (var b in buttons)
                {
                    var sprite = ButtonSprite(b.color);
                    float h = b.secondary ? btnH * 0.85f : btnH;
                    var btn = UIBuilder.Button(holder, "Btn_" + b.label, b.label, b.onClick, b.color, sprite, new Vector2(sprite != null ? 620f : 580f, sprite != null ? h * 1.35f : h), b.secondary ? 32 : (b.label.Length > 16 ? 30 : b.label.Length > 11 ? 36 : 40),
                        b.color == UIBuilder.Gold || b.color == UIBuilder.Mint || b.color == Style.Gold || b.color == Style.Mint ? Style.Ink : Color.white);
                    btn.image.rectTransform.anchorMin = new Vector2(0.5f, 1f); btn.image.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                    btn.image.rectTransform.pivot = new Vector2(0.5f, 1f);
                    btn.image.rectTransform.anchoredPosition = new Vector2(0f, y);
                    btn.interactable = b.interactable;
                    if (b.secondary) { var cg = btn.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 0.88f; }
                    y -= h + btnGap;
                }

            // Appearance: 200 ms fade + scale, no long overshoot; unscaled so it animates while gameplay is paused.
            var cg2 = holder.gameObject.AddComponent<CanvasGroup>();
            cg2.alpha = 0f;
            holder.localScale = Vector3.one * 0.92f;
            Tween.To(Style.ModalIn, t =>
            {
                if (holder == null) return;
                cg2.alpha = t;
                holder.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, Tween.Evaluate(Ease.OutCubic, t));
            }, Ease.Linear, null, true);
            overlay.color = new Color(0.1f, 0.05f, 0.06f, 0f);
            float dimA = dim ? 0.6f : 0f;
            Tween.To(Style.ModalIn, t => { if (overlay != null) overlay.color = new Color(0.1f, 0.05f, 0.06f, dimA * t); }, Ease.Linear, null, true);
            return holder;
        }

        /// <summary>Full-screen fake ad with a countdown; used by FakeAdService.</summary>
        public static void FakeAd(string title, float seconds, Action onFinished)
        {
            Close();
            var overlay = UIBuilder.Panel(Layer, "FakeAd", new Color(0.05f, 0.05f, 0.08f, 0.97f), null, true);
            UIBuilder.Stretch(overlay.rectTransform);
            current = overlay.rectTransform;
            var t = UIBuilder.Label(overlay.transform, "Title", title, 44, Color.white);
            UIBuilder.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(900f, 200f), new Vector2(0f, 120f));
            var count = UIBuilder.Label(overlay.transform, "Count", "", 120, Style.Gold);
            UIBuilder.Place(count.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(400f, 200f), new Vector2(0f, -80f));
            var sub = UIBuilder.Label(overlay.transform, "Sub", "Real ad SDK is swapped in behind IAdService before soft launch.", 26, new Color(0.7f, 0.7f, 0.75f), TextAnchor.MiddleCenter, FontStyle.Normal);
            UIBuilder.Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(900f, 100f), new Vector2(0f, -260f));
            var ov = overlay.rectTransform;
            Tween.To(seconds, p => { if (count != null) count.text = Mathf.CeilToInt(seconds * (1f - p)).ToString(); }, Ease.Linear, () =>
            {
                if (current == ov) Close();
                onFinished?.Invoke();
            }, true);
        }

        /// <summary>Small "Remove ads" link shown for a few seconds after an interstitial closes.</summary>
        public static void RemoveAdsLink(float seconds, Action onClick)
        {
            var btn = UIBuilder.Button(Layer, "RemoveAdsLink", "Remove ads?", () => { onClick?.Invoke(); }, new Color(0.15f, 0.15f, 0.18f, 0.9f), null, new Vector2(300f, 70f), 28);
            UIBuilder.Place(btn.image.rectTransform, new Vector2(0.5f, 1f), new Vector2(300f, 70f), new Vector2(0f, -200f));
            var rt = btn.image.rectTransform;
            Tween.Delay(seconds, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); }, true);
        }

        /// <summary>Cream wipe: fades to cream, runs <paramref name="atPeak"/>, fades back.</summary>
        public static void Fade(Action atPeak, float half = 0.22f)
        {
            if (Style.ReducedMotion) half = 0.1f;
            var cover = UIBuilder.Panel(Layer, "Fade", new Color(1f, 0.957f, 0.863f, 0f), null, true);
            UIBuilder.Stretch(cover.rectTransform);
            cover.transform.SetAsLastSibling();
            Tween.To(half, t => { if (cover != null) { var c = cover.color; c.a = t; cover.color = c; } }, Ease.InQuad, () =>
            {
                atPeak?.Invoke();
                if (cover != null) cover.transform.SetAsLastSibling();
                Tween.To(half, t => { if (cover != null) { var c = cover.color; c.a = 1f - t; cover.color = c; } }, Ease.OutQuad,
                    () => { if (cover != null) UnityEngine.Object.Destroy(cover.gameObject); }, true);
            }, true);
        }

        /// <summary>A thin stub banner at the bottom of a page (menu / level select only).</summary>
        public static Image Banner(Transform parent)
        {
            var strip = UIBuilder.Panel(parent, "BannerStub", new Color(0.1f, 0.1f, 0.12f, 0.85f), null, false);
            strip.rectTransform.anchorMin = new Vector2(0f, 0f); strip.rectTransform.anchorMax = new Vector2(1f, 0f);
            strip.rectTransform.pivot = new Vector2(0.5f, 0f); strip.rectTransform.sizeDelta = new Vector2(0f, 100f); strip.rectTransform.anchoredPosition = Vector2.zero;
            var lbl = UIBuilder.Label(strip.transform, "Lbl", "AD BANNER (stub — hidden with Remove Ads)", 24, new Color(0.8f, 0.8f, 0.85f), TextAnchor.MiddleCenter, FontStyle.Normal);
            UIBuilder.Stretch(lbl.rectTransform);
            return strip;
        }

        public static string FormatTime(TimeSpan t)
        {
            if (t.TotalHours >= 1) return string.Format("{0}h {1:00}m", (int)t.TotalHours, t.Minutes);
            return string.Format("{0}:{1:00}", t.Minutes, t.Seconds);
        }
    }
}
