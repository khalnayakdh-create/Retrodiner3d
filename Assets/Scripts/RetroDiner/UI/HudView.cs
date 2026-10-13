using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RetroDiner.Core;
using RetroDiner.Data;

namespace RetroDiner.UI
{
    /// <summary>
    /// Gameplay HUD: top bar (level, star bar, tip jar, pause), order rail, tray, booster bar,
    /// near-miss vignette, toasts and all popup panels (pause / win / lose / continue / fake ad).
    /// </summary>
    public class HudView : MonoBehaviour
    {
        public TicketRailView Tickets { get; private set; }
        public TrayView Tray { get; private set; }
        public BoosterBarView Boosters { get; private set; }
        public RectTransform FlightLayer { get; private set; }
        public RectTransform Root { get; private set; }

        Text levelText, tipsText, freezeText;
        RectTransform tipJarRect;
        int shownTips;
        Image starFill;
        Image[] stars = new Image[3];
        Image vignette;
        RectTransform panelLayer;
        Text toastText;
        RectTransform toastRect;
        TweenHandle toastTween;
        GameConfig cfg;
        Sprite starFilledSprite, starEmptySprite;
        float vignetteTarget;

        public void Build(Canvas canvas, GameConfig config, Func<string, ItemDefinition> lookup, Action onPause, Action<BoosterType> onBooster)
        {
            cfg = config;
            Root = UIBuilder.Rect(canvas.transform, "HUD");
            UIBuilder.Stretch(Root);
            Root.gameObject.AddComponent<SafeAreaFitter>();

            starFilledSprite = ArtCatalog.Pick(cfg.starFilled, ArtCatalog.StarFilled);
            starEmptySprite = ArtCatalog.Pick(cfg.starEmpty, ArtCatalog.StarEmpty);

            // ---- vignette (below everything else in the HUD) ----
            vignette = UIBuilder.Panel(Root, "Vignette", new Color(0.9f, 0.1f, 0.1f, 0f), null, false);
            UIBuilder.Stretch(vignette.rectTransform);

            // ---- depth scrims at the top and bottom of the belt band ----
            var topScrim = UIBuilder.Panel(Root, "TopScrim", new Color(0.08f, 0.04f, 0.05f, 0.55f), ProcSprites.VerticalGradient(1f, 0f), false);
            topScrim.rectTransform.anchorMin = new Vector2(0f, cfg.cameraViewportBand.y - 0.07f); topScrim.rectTransform.anchorMax = new Vector2(1f, cfg.cameraViewportBand.y + 0.02f);
            topScrim.rectTransform.offsetMin = Vector2.zero; topScrim.rectTransform.offsetMax = Vector2.zero;
            var bottomScrim = UIBuilder.Panel(Root, "BottomScrim", new Color(0.08f, 0.04f, 0.05f, 0.55f), ProcSprites.VerticalGradient(0f, 1f), false);
            bottomScrim.rectTransform.anchorMin = new Vector2(0f, cfg.cameraViewportBand.x - 0.02f); bottomScrim.rectTransform.anchorMax = new Vector2(1f, cfg.cameraViewportBand.x + 0.06f);
            bottomScrim.rectTransform.offsetMin = Vector2.zero; bottomScrim.rectTransform.offsetMax = Vector2.zero;

            // ---- order rail ----
            Tickets = Root.gameObject.AddComponent<TicketRailView>();
            Tickets.Build(Root, cfg, lookup);

            // ---- tray ----
            FlightLayer = UIBuilder.Rect(Root, "FlightLayer");
            UIBuilder.Stretch(FlightLayer);
            Tray = Root.gameObject.AddComponent<TrayView>();
            Tray.Build(Root, FlightLayer, cfg, lookup);
            FlightLayer.SetAsLastSibling();

            // ---- boosters ----
            Boosters = Root.gameObject.AddComponent<BoosterBarView>();
            Boosters.Build(Root, cfg, onBooster);

            // ---- top bar: one chrome plate (ui/rd3d_ui_topbar_plate_v001 or procedural) holding
            //      level pill · progress bar + stars · tip jar · pause ----
            var top = UIBuilder.Rect(Root, "TopBar");
            top.anchorMin = new Vector2(0f, 1f); top.anchorMax = new Vector2(1f, 1f); top.pivot = new Vector2(0.5f, 1f);
            top.sizeDelta = new Vector2(-24f, 128f); top.anchoredPosition = new Vector2(0f, -16f);

            var plateSprite = ArtCatalog.Load("ui/rd3d_ui_topbar_plate_v001");
            var plate = UIBuilder.Sliced(top, "Plate", plateSprite, Style.Chrome, 34, false);
            UIBuilder.Stretch(plate.rectTransform);
            if (plateSprite == null)
            {
                UIBuilder.Shadow(plate, 6f);
                var plateInner = UIBuilder.Pill(plate.transform, "Inner", new Color(0.12f, 0.1f, 0.12f, 0.82f), 28);
                UIBuilder.Stretch(plateInner.rectTransform, 7, 7, 7, 7);
                var plateSheen = UIBuilder.Panel(plate.transform, "Sheen", new Color(1f, 1f, 1f, 0.16f), ProcSprites.Rounded(28), false);
                plateSheen.type = Image.Type.Sliced;
                UIBuilder.Stretch(plateSheen.rectTransform, 7, 7, 7, 64f);
            }

            var levelPill = UIBuilder.Pill(top, "LevelPill", Style.Cherry, 30);
            UIBuilder.Place(levelPill.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(210f, 76f), new Vector2(22f, 0f));
            UIBuilder.Outline(levelPill, Style.ChromeLight, 3f);
            levelText = UIBuilder.Label(levelPill.transform, "LevelText", "Level 1", 36, Style.Cream);
            UIBuilder.Stretch(levelText.rectTransform);

            var trackSprite = ArtCatalog.Load("ui/rd3d_ui_bar_track_v001");
            var fillSprite = ArtCatalog.Load("ui/rd3d_ui_bar_fill_gold_v001");
            var barBack = UIBuilder.Sliced(top, "StarBar", trackSprite, new Color(0.08f, 0.07f, 0.09f), 16, false);
            UIBuilder.Place(barBack.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(400f, 34f), new Vector2(-60f, -6f));
            if (trackSprite == null) UIBuilder.Outline(barBack, Style.ChromeShadow, 2f);
            starFill = UIBuilder.Sliced(barBack.transform, "Fill", fillSprite, Style.Gold, 14, false);
            starFill.rectTransform.anchorMin = new Vector2(0f, 0f); starFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            starFill.rectTransform.pivot = new Vector2(0f, 0.5f); starFill.rectTransform.anchoredPosition = new Vector2(3f, 0f);
            starFill.rectTransform.sizeDelta = new Vector2(0f, -6f);
            float[] starX = { 0.4f, 0.7f, 1f };
            for (int i = 0; i < 3; i++)
            {
                var st = UIBuilder.Panel(barBack.transform, "Star" + i, starEmptySprite != null ? Color.white : new Color(0.45f, 0.45f, 0.5f), starEmptySprite, false);
                st.preserveAspect = true;
                UIBuilder.Place(st.rectTransform, new Vector2(starX[i], 0.5f), new Vector2(64f, 64f), new Vector2(-6f, 6f));
                stars[i] = st;
            }

            var jar = UIBuilder.Rect(top, "TipJar");
            UIBuilder.Place(jar, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(150f, 110f), new Vector2(-118f, 0f));
            var jarSprite = ArtCatalog.Pick(cfg.tipJar50, ArtCatalog.TipJar50);
            var jarIcon = UIBuilder.Icon(jar, "JarIcon", jarSprite, Style.Gold, new Vector2(84f, 84f));
            UIBuilder.Place(jarIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(84f, 84f), new Vector2(0f, 12f));
            tipJarRect = jarIcon.rectTransform;
            tipsText = UIBuilder.Label(jar, "Tips", "0", 30, Style.Cream);
            UIBuilder.Place(tipsText.rectTransform, new Vector2(0.5f, 0f), new Vector2(150f, 36f), new Vector2(0f, 2f));
            UIBuilder.Outline(tipsText, new Color(0f, 0f, 0f, 0.7f), 2f);

            var pauseSprite = ArtCatalog.Pick(cfg.pauseButton, ArtCatalog.PauseButton);
            var pauseBtn = UIBuilder.Button(top, "Pause", pauseSprite == null ? "II" : null, onPause, Style.Teal, pauseSprite, new Vector2(84f, 84f), 36);
            UIBuilder.Place(pauseBtn.image.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(84f, 84f), new Vector2(-16f, 0f));

            freezeText = UIBuilder.Label(Root, "FreezeText", "", 44, UIBuilder.Sky);
            UIBuilder.Place(freezeText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(600f, 80f), new Vector2(0f, 330f));
            UIBuilder.Outline(freezeText, new Color(0f, 0f, 0f, 0.8f), 3f);
            freezeText.gameObject.SetActive(false);

            // ---- toast ----
            toastRect = UIBuilder.Rect(Root, "Toast");
            UIBuilder.Place(toastRect, new Vector2(0.5f, 0.5f), new Vector2(800f, 90f), new Vector2(0f, -250f));
            var toastBack = UIBuilder.Panel(toastRect, "Back", new Color(0f, 0f, 0f, 0.7f), null, false);
            UIBuilder.Stretch(toastBack.rectTransform);
            toastText = UIBuilder.Label(toastRect, "Text", "", 36, Color.white);
            UIBuilder.Stretch(toastText.rectTransform, 20, 0, 20, 0);
            toastRect.gameObject.SetActive(false);

            // ---- panel layer (always on top) ----
            panelLayer = UIBuilder.Rect(Root, "Panels");
            UIBuilder.Stretch(panelLayer);
            panelLayer.SetAsLastSibling();

            GameEvents.Toast += ShowToast;
        }

        void OnDestroy() { GameEvents.Toast -= ShowToast; }

        void Update()
        {
            float a = vignette.color.a;
            float target = vignetteTarget > 0f ? vignetteTarget * (0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f))) : 0f;
            a = Mathf.MoveTowards(a, target, Time.unscaledDeltaTime * 2f);
            var c = vignette.color; c.a = a; vignette.color = c;
        }

        public void SetNearMiss(bool on) { vignetteTarget = on ? 0.45f : 0f; }

        public void SetLevel(int index) { levelText.text = "Level " + index; }

        public void SetTips(int tipJar) { shownTips = tipJar; tipsText.text = tipJar.ToString(); }

        public Vector2 TipJarScreenPosition => tipJarRect != null ? UIBuilder.LocalToScreen(tipJarRect) : new Vector2(Screen.width - 100f, Screen.height - 60f);

        /// <summary>13A: coins fly from a screen point into the Tip Jar; the counter ticks up to the authoritative
        /// value as they land (reflects the actual award, never a guessed one).</summary>
        public void CoinsToTipJar(Vector2 fromScreen, int actualTotal, int award)
        {
            int n = Mathf.Clamp(award, 1, 6);
            Vector2 to = UIBuilder.ScreenToLocal(FlightLayer, TipJarScreenPosition);
            Vector2 from = UIBuilder.ScreenToLocal(FlightLayer, fromScreen);
            int startShown = shownTips;
            if (Style.ReducedMotion)
            {
                SetTips(actualTotal);
                UIBuilder.Punch(tipJarRect, 0.15f, 0.25f);
                return;
            }
            for (int i = 0; i < n; i++)
            {
                var coin = UIBuilder.Panel(FlightLayer, "Coin", Style.Gold, ProcSprites.Circle(64), false);
                UIBuilder.Place(coin.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(44f, 44f), from);
                UIBuilder.Outline(coin, new Color(0.6f, 0.4f, 0.05f), 3f);
                var rt = coin.rectTransform;
                int idx = i;
                Vector2 ctrl = (from + to) * 0.5f + new Vector2(UnityEngine.Random.Range(-120f, 120f), 160f + UnityEngine.Random.Range(0f, 120f));
                Tween.To(0.45f, t =>
                {
                    if (rt == null) return;
                    float u = 1f - t;
                    rt.anchoredPosition = u * u * from + 2f * u * t * ctrl + t * t * to;
                    rt.localScale = Vector3.one * (1f - 0.35f * t);
                }, Ease.InOutQuad, () =>
                {
                    if (rt != null) Destroy(rt.gameObject);
                    int shown = Mathf.RoundToInt(Mathf.Lerp(startShown, actualTotal, (idx + 1) / (float)n));
                    SetTips(shown);
                    if (tipJarRect != null) UIBuilder.Punch(tipJarRect, 0.12f, 0.18f);
                    if (idx == n - 1) { SetTips(actualTotal); GameServices.Audio.Play("coin", 0.7f); }
                }, true, 0.06f * i);
            }
        }

        public void SetFreeze(float remaining)
        {
            bool on = remaining > 0f;
            if (freezeText.gameObject.activeSelf != on) freezeText.gameObject.SetActive(on);
            if (on) freezeText.text = "BELT PAUSED " + Mathf.CeilToInt(remaining) + "s";
        }

        /// <summary>progress 0..1 of ticket items done; starsEarnedPreview lights stars at 40/70/100%.</summary>
        public void SetProgress(float progress)
        {
            float w0 = starFill.rectTransform.sizeDelta.x, w1 = (400f - 6f) * Mathf.Clamp01(progress);
            Tween.To(Style.ReducedMotion ? 0.05f : 0.3f, t => { if (starFill != null) starFill.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(w0, w1, t), -6f); }, Ease.OutQuad, null, true);
            float[] th = { 0.4f, 0.7f, 1f };
            for (int i = 0; i < 3; i++)
            {
                bool lit = progress >= th[i] - 0.001f;
                if (starFilledSprite != null && starEmptySprite != null) stars[i].sprite = lit ? starFilledSprite : starEmptySprite;
                else stars[i].color = lit ? UIBuilder.Gold : new Color(0.45f, 0.45f, 0.5f);
            }
        }

        public void ShowToast(string message)
        {
            toastText.text = message;
            toastRect.gameObject.SetActive(true);
            toastRect.localScale = Vector3.one * 0.8f;
            if (toastTween != null) toastTween.Kill();
            Tween.Scale(toastRect, Vector3.one, 0.2f, Ease.OutBack, null, true);
            toastTween = Tween.Delay(1.6f, () => { if (toastRect != null) toastRect.gameObject.SetActive(false); }, true);
        }

        public void Shake(float pixels = 2f, float duration = 0.1f)
        {
            Vector2 origin = Vector2.zero;
            Tween.To(duration, t =>
            {
                if (Root == null) return;
                float s = (1f - t) * pixels * 2f;
                Root.anchoredPosition = origin + new Vector2(UnityEngine.Random.Range(-s, s), UnityEngine.Random.Range(-s, s));
            }, Ease.Linear, () => { if (Root != null) Root.anchoredPosition = origin; }, true);
        }

        // ------------------------------------------------------------------ panels

        public void ClosePanel() { Popups.Close(); }

        public RectTransform ShowPanel(string title, string body, List<PanelButton> buttons, bool dim = true, Action<RectTransform> decorate = null, float contentHeight = 10f)
        {
            return Popups.Show(title, body, buttons, dim, decorate, contentHeight);
        }

        public void ShowFakeAd(string title, float seconds, Action onFinished) { Popups.FakeAd(title, seconds, onFinished); }

        // ------------------------------------------------------------------ flourishes

        /// <summary>Big centre banner (uses the neon banner art when present): "ORDER UP!", "NICE!", etc.</summary>
        public void Flash(string text, float hold = 0.6f, Color? tint = null)
        {
            var sprite = ArtCatalog.Pick(cfg.shiftFinishedBanner, ArtCatalog.ShiftFinishedBanner);
            var holder = UIBuilder.Rect(FlightLayer, "Flash");
            UIBuilder.Place(holder, new Vector2(0.5f, 0.5f), new Vector2(760f, 260f), new Vector2(0f, 120f));
            Image back;
            if (sprite != null) { back = UIBuilder.Panel(holder, "Back", Color.white, sprite, false); back.preserveAspect = true; }
            else { back = UIBuilder.Pill(holder, "Back", UIBuilder.Red, 40); UIBuilder.Outline(back, UIBuilder.Chrome, 5f); }
            UIBuilder.Stretch(back.rectTransform);
            var lbl = UIBuilder.Label(holder, "Text", text, 64, tint ?? UIBuilder.Cream);
            UIBuilder.Stretch(lbl.rectTransform, 40, 20, 40, 20);
            UIBuilder.Outline(lbl, UIBuilder.Dark, 4f);
            holder.localScale = Vector3.zero;
            Tween.Scale(holder, Vector3.one, 0.28f, Ease.OutBack, () =>
            {
                Tween.Delay(hold, () =>
                {
                    Tween.To(0.25f, t =>
                    {
                        if (holder == null) return;
                        holder.localScale = Vector3.one * (1f + t * 0.3f);
                        var c = lbl.color; c.a = 1f - t; lbl.color = c;
                        var b = back.color; b.a = 1f - t; back.color = b;
                    }, Ease.InQuad, () => { if (holder != null) Destroy(holder.gameObject); }, true);
                }, true);
            }, true);
        }

        /// <summary>Small rising text ("+3 tokens", "x2") at a screen position.</summary>
        public void FloatText(Vector2 screen, string text, Color color, int size = 40)
        {
            var lbl = UIBuilder.Label(FlightLayer, "Float", text, size, color);
            UIBuilder.Place(lbl.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(400f, 80f), UIBuilder.ScreenToLocal(FlightLayer, screen));
            UIBuilder.Outline(lbl, UIBuilder.Dark, 3f);
            var rt = lbl.rectTransform;
            Vector2 start = rt.anchoredPosition;
            rt.localScale = Vector3.one * 0.6f;
            Tween.To(0.9f, t =>
            {
                if (rt == null) return;
                rt.anchoredPosition = start + new Vector2(0f, 120f * t);
                rt.localScale = Vector3.one * (0.6f + 0.5f * Tween.Evaluate(Ease.OutBack, Mathf.Min(1f, t * 3f)));
                var c = lbl.color; c.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f; lbl.color = c;
            }, Ease.Linear, () => { if (rt != null) Destroy(rt.gameObject); }, true);
        }

        /// <summary>Expanding ring where the finger tapped.</summary>
        public void TapRipple(Vector2 screen, Color color)
        {
            var ring = UIBuilder.Panel(FlightLayer, "Ripple", color, ProcSprites.Ring(128, 9f), false);
            UIBuilder.Place(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(120f, 120f), UIBuilder.ScreenToLocal(FlightLayer, screen));
            var rt = ring.rectTransform;
            Tween.To(0.35f, t =>
            {
                if (rt == null) return;
                rt.localScale = Vector3.one * (0.4f + 1.4f * t);
                var c = ring.color; c.a = 1f - t; ring.color = c;
            }, Ease.OutQuad, () => { if (rt != null) Destroy(rt.gameObject); }, true);
        }

        /// <summary>"SHIFT 12 · 3 ORDERS" slides in from the right, holds, slides out left.</summary>
        public void LevelIntro(int level, int orders, Action done)
        {
            var holder = UIBuilder.Rect(FlightLayer, "Intro");
            UIBuilder.Place(holder, new Vector2(0.5f, 0.5f), new Vector2(1080f, 230f), new Vector2(1300f, 140f));
            var band = UIBuilder.Panel(holder, "Band", new Color(0.84f, 0.15f, 0.24f, 0.95f), null, false);
            UIBuilder.Stretch(band.rectTransform);
            var stripe = UIBuilder.Panel(holder, "Stripe", new Color(1f, 0.96f, 0.86f, 0.9f), null, false);
            stripe.rectTransform.anchorMin = new Vector2(0f, 0f); stripe.rectTransform.anchorMax = new Vector2(1f, 0f);
            stripe.rectTransform.pivot = new Vector2(0.5f, 0f); stripe.rectTransform.sizeDelta = new Vector2(0f, 10f); stripe.rectTransform.anchoredPosition = Vector2.zero;
            var t1 = UIBuilder.Label(holder, "T1", "SHIFT " + level, 84, UIBuilder.Cream);
            UIBuilder.Place(t1.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1000f, 110f), new Vector2(0f, 35f));
            UIBuilder.Outline(t1, UIBuilder.Dark, 4f);
            var t2 = UIBuilder.Label(holder, "T2", orders + (orders == 1 ? " ORDER" : " ORDERS") + "  ·  ORDER UP!", 40, UIBuilder.Gold);
            UIBuilder.Place(t2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1000f, 60f), new Vector2(0f, -50f));
            UIBuilder.Outline(t2, UIBuilder.Dark, 3f);
            GameServices.Audio.Play("bell", 0.6f, 1.2f);
            Tween.AnchoredMove(holder, new Vector2(0f, 140f), 0.35f, Ease.OutCubic, () =>
            {
                Tween.Delay(0.75f, () =>
                {
                    Tween.AnchoredMove(holder, new Vector2(-1300f, 140f), 0.3f, Ease.InQuad, () =>
                    {
                        if (holder != null) Destroy(holder.gameObject);
                        done?.Invoke();
                    }, true);
                }, true);
            }, true);
        }

        /// <summary>Win panel content: star row, reward line, tip-jar line. Content height ~250.</summary>
        public void DecorateStars(RectTransform content, int earned, string rewardLine = null, string tipLine = null)
        {
            for (int i = 0; i < 3; i++)
            {
                bool lit = i < earned;
                var sp = lit ? starFilledSprite : starEmptySprite;
                var st = UIBuilder.Panel(content, "WinStar" + i, sp != null ? Color.white : (lit ? Style.Gold : new Color(0.5f, 0.5f, 0.55f)), sp != null ? sp : ProcSprites.Circle(64), false);
                st.preserveAspect = true;
                UIBuilder.Place(st.rectTransform, new Vector2(0.5f, 1f), new Vector2(140f, 140f), new Vector2((i - 1) * 165f, -84f + (i == 1 ? 12f : 0f)));
                st.rectTransform.localScale = Vector3.zero;
                var rt = st.rectTransform; float delay = Style.ReducedMotion ? 0f : 0.15f + i * 0.2f;
                Tween.To(Style.ReducedMotion ? 0.12f : 0.35f, p => { if (rt != null) rt.localScale = Vector3.one * Tween.Evaluate(Ease.OutBack, p); }, Ease.Linear, null, true, delay);
            }
            if (!string.IsNullOrEmpty(rewardLine))
            {
                var r = UIBuilder.Label(content, "Reward", rewardLine, 44, Style.Cherry);
                UIBuilder.Place(r.rectTransform, new Vector2(0.5f, 1f), new Vector2(800f, 60f), new Vector2(0f, -190f));
            }
            if (!string.IsNullOrEmpty(tipLine))
            {
                var t = UIBuilder.Label(content, "Tip", tipLine, 28, Style.Ink, TextAnchor.MiddleCenter, FontStyle.Normal);
                UIBuilder.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(800f, 40f), new Vector2(0f, -236f));
            }
        }
    }
}
