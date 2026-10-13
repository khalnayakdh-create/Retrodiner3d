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
        public string label; public Action onClick; public Color color; public bool interactable = true;
        public PanelButton(string label, Action onClick, Color color) { this.label = label; this.onClick = onClick; this.color = color; }
    }

    /// <summary>
    /// One global popup layer (its own canvas above HUD and menu) so panels, fake ads and the
    /// energy/booster offers look the same wherever they are opened from. Only one popup at a time.
    /// </summary>
    public static class Popups
    {
        public static RectTransform Layer { get; private set; }
        static RectTransform current;
        static GameConfig cfg;
        static Sprite panelSprite, btnRed, btnMint, btnGold;

        public static bool IsOpen => current != null;

        public static void Init(Canvas canvas, GameConfig config)
        {
            cfg = config;
            Layer = UIBuilder.Rect(canvas.transform, "PopupLayer");
            UIBuilder.Stretch(Layer);
            panelSprite = ArtCatalog.Pick(cfg.popupPanel, ArtCatalog.PopupPanel);
            btnRed = ArtCatalog.Pick(cfg.buttonRed, ArtCatalog.ButtonRed);
            btnMint = ArtCatalog.Pick(cfg.buttonMint, ArtCatalog.ButtonMint);
            btnGold = ArtCatalog.Pick(cfg.buttonGold, ArtCatalog.ButtonGold);
        }

        public static void Close()
        {
            if (current != null) UnityEngine.Object.Destroy(current.gameObject);
            current = null;
        }

        public static Sprite ButtonSprite(Color c) => c == UIBuilder.Red ? btnRed : c == UIBuilder.Mint ? btnMint : c == UIBuilder.Gold ? btnGold : null;

        public static RectTransform Show(string title, string body, List<PanelButton> buttons, bool dim = true, Action<RectTransform> decorate = null, float panelHeight = 1150f)
        {
            Close();
            var overlay = UIBuilder.Panel(Layer, "Overlay", new Color(0f, 0f, 0f, dim ? 0.55f : 0f), null, true);
            UIBuilder.Stretch(overlay.rectTransform);
            current = overlay.rectTransform;

            var panel = UIBuilder.Panel(overlay.transform, "Panel", panelSprite != null ? Color.white : UIBuilder.Cream, panelSprite, true);
            panel.preserveAspect = panelSprite != null;
            UIBuilder.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(920f, panelHeight), new Vector2(0f, 40f));
            if (panelSprite == null)
            {
                UIBuilder.Shadow(panel, 10f);
                UIBuilder.Outline(panel, UIBuilder.Chrome, 6f);
                var header = UIBuilder.Panel(panel.transform, "Header", UIBuilder.Red, null, false);
                header.rectTransform.anchorMin = new Vector2(0f, 1f); header.rectTransform.anchorMax = new Vector2(1f, 1f);
                header.rectTransform.pivot = new Vector2(0.5f, 1f); header.rectTransform.sizeDelta = new Vector2(0f, 110f);
                header.rectTransform.anchoredPosition = Vector2.zero;
            }

            var titleLbl = UIBuilder.Label(panel.transform, "Title", title, 56, Color.white);
            titleLbl.rectTransform.anchorMin = new Vector2(0f, 1f); titleLbl.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleLbl.rectTransform.pivot = new Vector2(0.5f, 1f); titleLbl.rectTransform.sizeDelta = new Vector2(-80f, 110f);
            titleLbl.rectTransform.anchoredPosition = new Vector2(0f, panelSprite != null ? -80f : -40f);
            UIBuilder.Outline(titleLbl, new Color(0f, 0f, 0f, 0.6f), 3f);

            var content = UIBuilder.Rect(panel.transform, "Content");
            UIBuilder.Place(content, new Vector2(0.5f, 0.5f), new Vector2(760f, 420f), new Vector2(0f, 120f));
            if (!string.IsNullOrEmpty(body))
            {
                var bodyLbl = UIBuilder.Label(content, "Body", body, 38, UIBuilder.Dark, TextAnchor.UpperCenter, FontStyle.Normal);
                UIBuilder.Stretch(bodyLbl.rectTransform);
            }
            decorate?.Invoke(content);

            float y = panelSprite != null ? -120f : -170f;
            if (buttons != null)
                foreach (var b in buttons)
                {
                    var sprite = ButtonSprite(b.color);
                    Vector2 bsize = sprite != null ? new Vector2(600f, 180f) : new Vector2(560f, 130f);
                    var btn = UIBuilder.Button(panel.transform, "Btn_" + b.label, b.label, b.onClick, b.color, sprite, bsize, 36, b.color == UIBuilder.Gold || b.color == UIBuilder.Mint ? UIBuilder.Dark : Color.white);
                    UIBuilder.Place(btn.image.rectTransform, new Vector2(0.5f, 0.5f), bsize, new Vector2(0f, y));
                    btn.interactable = b.interactable;
                    y -= 150f;
                }

            panel.rectTransform.localScale = Vector3.one * 0.7f;
            Tween.Scale(panel.rectTransform, Vector3.one, 0.3f, Ease.OutBack, null, true);
            return panel.rectTransform;
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
            var count = UIBuilder.Label(overlay.transform, "Count", "", 120, UIBuilder.Gold);
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
