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

            starFilledSprite = ArtCatalog.Pick(cfg.starFilled, ArtCatalog.StarFilled);
            starEmptySprite = ArtCatalog.Pick(cfg.starEmpty, ArtCatalog.StarEmpty);

            // ---- vignette (below everything else in the HUD) ----
            vignette = UIBuilder.Panel(Root, "Vignette", new Color(0.9f, 0.1f, 0.1f, 0f), null, false);
            UIBuilder.Stretch(vignette.rectTransform);

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

            // ---- top bar ----
            var top = UIBuilder.Rect(Root, "TopBar");
            top.anchorMin = new Vector2(0f, 1f); top.anchorMax = new Vector2(1f, 1f); top.pivot = new Vector2(0.5f, 1f);
            top.sizeDelta = new Vector2(0f, 140f); top.anchoredPosition = new Vector2(0f, -30f);

            var levelPill = UIBuilder.Panel(top, "LevelPill", new Color(0.18f, 0.36f, 0.62f), null, false);
            UIBuilder.Place(levelPill.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(230f, 80f), new Vector2(30f, 0f));
            UIBuilder.Outline(levelPill, UIBuilder.Chrome, 3f);
            levelText = UIBuilder.Label(levelPill.transform, "LevelText", "Level 1", 40, Color.white);
            UIBuilder.Stretch(levelText.rectTransform);

            var barBack = UIBuilder.Panel(top, "StarBar", new Color(0.2f, 0.2f, 0.22f), null, false);
            UIBuilder.Place(barBack.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(430f, 34f), new Vector2(-20f, 0f));
            UIBuilder.Outline(barBack, UIBuilder.Chrome, 3f);
            starFill = UIBuilder.Panel(barBack.transform, "Fill", new Color(0.36f, 0.8f, 0.3f), null, false);
            starFill.rectTransform.anchorMin = new Vector2(0f, 0f); starFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            starFill.rectTransform.pivot = new Vector2(0f, 0.5f); starFill.rectTransform.anchoredPosition = Vector2.zero;
            starFill.rectTransform.sizeDelta = new Vector2(0f, 0f);
            float[] starX = { 0.4f, 0.7f, 1f };
            for (int i = 0; i < 3; i++)
            {
                var st = UIBuilder.Panel(barBack.transform, "Star" + i, starEmptySprite != null ? Color.white : new Color(0.45f, 0.45f, 0.5f), starEmptySprite, false);
                st.preserveAspect = true;
                UIBuilder.Place(st.rectTransform, new Vector2(starX[i], 0.5f), new Vector2(76f, 76f), new Vector2(-10f, 6f));
                stars[i] = st;
            }

            var jar = UIBuilder.Rect(top, "TipJar");
            UIBuilder.Place(jar, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(150f, 110f), new Vector2(-150f, 0f));
            var jarSprite = ArtCatalog.Pick(cfg.tipJar50, ArtCatalog.TipJar50);
            var jarIcon = UIBuilder.Icon(jar, "JarIcon", jarSprite, UIBuilder.Gold, new Vector2(90f, 90f));
            UIBuilder.Place(jarIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(90f, 90f), new Vector2(0f, 10f));
            tipsText = UIBuilder.Label(jar, "Tips", "0", 32, Color.white);
            UIBuilder.Place(tipsText.rectTransform, new Vector2(0.5f, 0f), new Vector2(150f, 40f), new Vector2(0f, 6f));
            UIBuilder.Outline(tipsText, new Color(0f, 0f, 0f, 0.7f), 2f);

            var pauseSprite = ArtCatalog.Pick(cfg.pauseButton, ArtCatalog.PauseButton);
            var pauseBtn = UIBuilder.Button(top, "Pause", pauseSprite == null ? "II" : null, onPause, UIBuilder.Sky, pauseSprite, new Vector2(90f, 90f), 40);
            UIBuilder.Place(pauseBtn.image.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(90f, 90f), new Vector2(-20f, 0f));

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

        public void SetTips(int tipJar) { tipsText.text = tipJar.ToString(); }

        public void SetFreeze(float remaining)
        {
            bool on = remaining > 0f;
            if (freezeText.gameObject.activeSelf != on) freezeText.gameObject.SetActive(on);
            if (on) freezeText.text = "BELT PAUSED " + Mathf.CeilToInt(remaining) + "s";
        }

        /// <summary>progress 0..1 of ticket items done; starsEarnedPreview lights stars at 40/70/100%.</summary>
        public void SetProgress(float progress)
        {
            starFill.rectTransform.sizeDelta = new Vector2(430f * Mathf.Clamp01(progress), 0f);
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

        public RectTransform ShowPanel(string title, string body, List<PanelButton> buttons, bool dim = true, Action<RectTransform> decorate = null)
        {
            return Popups.Show(title, body, buttons, dim, decorate);
        }

        public void ShowFakeAd(string title, float seconds, Action onFinished) { Popups.FakeAd(title, seconds, onFinished); }

        /// <summary>Star row used on the win panel.</summary>
        public void DecorateStars(RectTransform content, int earned)
        {
            for (int i = 0; i < 3; i++)
            {
                bool lit = i < earned;
                var sp = lit ? starFilledSprite : starEmptySprite;
                var st = UIBuilder.Panel(content, "WinStar" + i, sp != null ? Color.white : (lit ? UIBuilder.Gold : new Color(0.5f, 0.5f, 0.55f)), sp, false);
                st.preserveAspect = true;
                UIBuilder.Place(st.rectTransform, new Vector2(0.5f, 1f), new Vector2(150f, 150f), new Vector2((i - 1) * 170f, -60f + (i == 1 ? 20f : 0f)));
                st.rectTransform.localScale = Vector3.zero;
                var rt = st.rectTransform; float delay = 0.15f + i * 0.2f;
                Tween.To(0.35f, p => { if (rt != null) rt.localScale = Vector3.one * Tween.Evaluate(Ease.OutBack, p); }, Ease.Linear, null, true, delay);
            }
        }
    }
}
