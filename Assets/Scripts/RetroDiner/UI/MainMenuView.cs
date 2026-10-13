using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RetroDiner.Core;
using RetroDiner.Data;
using RetroDiner.Meta;

namespace RetroDiner.UI
{
    /// <summary>
    /// Main menu + level select + settings + store entry + meta widgets (energy hearts, punch-card,
    /// Tip Jar, daily spin). All pages live on one overlay canvas; popups go through Popups.
    /// </summary>
    public class MainMenuView : MonoBehaviour
    {
        RectTransform root, menuPage, levelPage, settingsPage, levelGrid;
        Text coinsText, tokensText, energyText, energyTimerText, tipJarText, punchBadge, spinBadge;
        Image tipJarIcon;
        Image[] hearts = new Image[5];
        Image menuBanner, levelBanner;
        Button crackButton;
        StoreView store;
        GameConfig cfg;
        Action<int> onPlayLevel;
        Action settingsRefresh;
        Sprite btnRed, btnMint, btnGold;
        float tick;

        public bool IsVisible => root != null && root.gameObject.activeSelf;

        public void Build(Canvas canvas, GameConfig config, Action<int> playLevel)
        {
            cfg = config;
            onPlayLevel = playLevel;
            btnRed = ArtCatalog.Pick(cfg.buttonRed, ArtCatalog.ButtonRed);
            btnMint = ArtCatalog.Pick(cfg.buttonMint, ArtCatalog.ButtonMint);
            btnGold = ArtCatalog.Pick(cfg.buttonGold, ArtCatalog.ButtonGold);

            root = UIBuilder.Rect(canvas.transform, "MainMenu");
            UIBuilder.Stretch(root);

            var bgSprite = ArtCatalog.Pick(cfg.mainMenuBackground, ArtCatalog.MainMenuBackground);
            var background = UIBuilder.Panel(root, "Background", bgSprite != null ? Color.white : new Color(0.16f, 0.12f, 0.2f), bgSprite, true);
            UIBuilder.Stretch(background.rectTransform);
            if (bgSprite != null)
            {
                var fit = background.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = bgSprite.rect.width / Mathf.Max(1f, bgSprite.rect.height);
                background.rectTransform.pivot = new Vector2(0.5f, 0f);   // keep the diner entrance in frame on tall phones
                background.rectTransform.anchorMin = new Vector2(0.5f, 0f); background.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                background.rectTransform.anchoredPosition = Vector2.zero;
            }
            var bgTint = UIBuilder.Panel(root, "BgTint", new Color(0.05f, 0.02f, 0.04f, 0.25f), ProcSprites.VerticalGradient(0f, 1f), false);
            UIBuilder.Stretch(bgTint.rectTransform);

            BuildMenuPage();
            BuildLevelPage();
            BuildSettingsPage();
            store = root.gameObject.AddComponent<StoreView>();
            store.Build(root, cfg, ShowMenu);
            ShowMenu();
            GameEvents.ProfileChanged += RefreshCurrency;
        }

        void OnDestroy() { GameEvents.ProfileChanged -= RefreshCurrency; }

        void Update()
        {
            if (!IsVisible || !menuPage.gameObject.activeSelf) return;
            tick += Time.unscaledDeltaTime;
            if (tick < 1f) return;
            tick = 0f;
            RefreshEnergy();
        }

        // ------------------------------------------------------------------ menu page

        void BuildMenuPage()
        {
            menuPage = UIBuilder.Rect(root, "MenuPage");
            UIBuilder.Stretch(menuPage);
            menuPage.gameObject.AddComponent<SafeAreaFitter>();

            // ---- resource chips (top-left) + compact gear (top-right) ----
            var strip = UIBuilder.Rect(menuPage, "Currency");
            strip.anchorMin = new Vector2(0f, 1f); strip.anchorMax = new Vector2(1f, 1f);
            strip.pivot = new Vector2(0.5f, 1f); strip.sizeDelta = new Vector2(0f, 90f); strip.anchoredPosition = new Vector2(0f, -24f);
            var chipSprite = ArtCatalog.Load("ui/rd3d_ui_chip_chrome_v001");
            Func<string, float, float, Image> chip = (name, x, w) =>
            {
                var c = UIBuilder.Sliced(strip, name, chipSprite, new Color(0.06f, 0.05f, 0.07f, 0.72f), 30, false);
                UIBuilder.Place(c.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(w, 64f), new Vector2(x, 0f));
                if (chipSprite == null) UIBuilder.Outline(c, Style.ChromeShadow, 2f);
                return c;
            };
            var coinPill = chip("CoinPill", 24f, 200f);
            var tokenPill = chip("TokenPill", 236f, 200f);
            var heartPill = chip("HeartPill", 448f, 330f);
            var coinIcon = UIBuilder.Icon(coinPill.transform, "CoinIcon", ArtCatalog.Pick(cfg.coinIcon, ArtCatalog.Coin), Style.Gold, new Vector2(50f, 50f));
            UIBuilder.Place(coinIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(50f, 50f), new Vector2(32f, 0f));
            coinsText = UIBuilder.Label(coinPill.transform, "Coins", "0", 30, Style.Cream, TextAnchor.MiddleLeft);
            UIBuilder.Place(coinsText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(130f, 60f), new Vector2(64f, 0f));
            var tokenIcon = UIBuilder.Icon(tokenPill.transform, "TokenIcon", ArtCatalog.Pick(cfg.tokenIcon, ArtCatalog.Token), UIBuilder.Purple, new Vector2(50f, 50f));
            UIBuilder.Place(tokenIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(50f, 50f), new Vector2(32f, 0f));
            tokensText = UIBuilder.Label(tokenPill.transform, "Tokens", "0", 30, Style.Cream, TextAnchor.MiddleLeft);
            UIBuilder.Place(tokensText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(130f, 60f), new Vector2(64f, 0f));

            var heartSprite = ArtCatalog.Pick(cfg.heartIcon, ArtCatalog.Heart);
            for (int i = 0; i < hearts.Length; i++)
            {
                hearts[i] = UIBuilder.Icon(heartPill.transform, "Heart" + i, heartSprite, Style.Cherry, new Vector2(40f, 40f));
                UIBuilder.Place(hearts[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(40f, 40f), new Vector2(34f + i * 44f, 0f));
            }
            energyTimerText = UIBuilder.Label(heartPill.transform, "EnergyTimer", "", 18, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleLeft, FontStyle.Normal);
            UIBuilder.Place(energyTimerText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(240f, 24f), new Vector2(14f, -4f));
            var plusSprite = ArtCatalog.Load("icons/rd3d_icon_heart_plus_v001");
            var plusEnergy = UIBuilder.Button(heartPill.transform, "PlusEnergy", plusSprite != null ? null : "+", ShowEnergyPanel, Style.Mint, plusSprite != null ? plusSprite : ProcSprites.Circle(64), new Vector2(48f, 48f), 34, Style.Ink);
            UIBuilder.Place(plusEnergy.image.rectTransform, new Vector2(1f, 0.5f), new Vector2(48f, 48f), new Vector2(-30f, 0f));
            if (plusSprite == null) plusEnergy.image.color = Style.Mint;
            energyText = UIBuilder.Label(heartPill.transform, "EnergyText", "", 1, Color.clear);
            energyText.gameObject.SetActive(false);

            var gearSprite = ArtCatalog.Load("icons/rd3d_icon_gear_v001");
            var settings = UIBuilder.Button(strip, "Settings", gearSprite != null ? null : "*", ShowSettings, Style.Chrome, gearSprite != null ? gearSprite : ProcSprites.Circle(64), new Vector2(72f, 72f), 44, Style.Ink);
            UIBuilder.Place(settings.image.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(72f, 72f), new Vector2(-24f, 0f));
            if (gearSprite == null) { settings.image.color = Style.Chrome; UIBuilder.Outline(settings.image, Style.ChromeShadow, 2f); }

            // ---- logo: diner sign (ui/rd3d_logo_retro_diner_3d_v001) or layered neon text ----
            var logoSprite = ArtCatalog.Load("ui/rd3d_logo_retro_diner_3d_v001");
            var titleGlow = UIBuilder.Panel(menuPage, "TitleGlow", new Color(1f, 0.25f, 0.3f, 0.5f), ProcSprites.Glow(128, 2.5f), false);
            UIBuilder.Place(titleGlow.rectTransform, new Vector2(0.5f, 1f), new Vector2(1300f, 520f), new Vector2(0f, -330f));
            if (logoSprite != null)
            {
                var logo = UIBuilder.Panel(menuPage, "Logo", Color.white, logoSprite, false);
                logo.preserveAspect = true;
                UIBuilder.Place(logo.rectTransform, new Vector2(0.5f, 1f), new Vector2(880f, 400f), new Vector2(0f, -300f));
                if (!Style.ReducedMotion) UIBuilder.Pulse(logo.rectTransform, 0.012f, 2.4f);
            }
            else
            {
                var sign = UIBuilder.Pill(menuPage, "Sign", Style.Cherry, 36);
                UIBuilder.Place(sign.rectTransform, new Vector2(0.5f, 1f), new Vector2(880f, 300f), new Vector2(0f, -330f));
                UIBuilder.Outline(sign, Style.ChromeLight, 8f);
                UIBuilder.Shadow(sign, 10f);
                var signInner = UIBuilder.Pill(sign.transform, "Inner", Style.DeepRed, 30);
                UIBuilder.Stretch(signInner.rectTransform, 16, 16, 16, 16);
                var signSheen = UIBuilder.Panel(sign.transform, "Sheen", new Color(1f, 1f, 1f, 0.12f), ProcSprites.Rounded(30), false);
                signSheen.type = Image.Type.Sliced; UIBuilder.Stretch(signSheen.rectTransform, 16, 16, 16, 150f);
                for (int b = 0; b < 14; b++)
                {
                    var bulb = UIBuilder.Panel(sign.transform, "Bulb" + b, Style.Gold, ProcSprites.Glow(32, 1.4f), false);
                    float t = b / 13f;
                    UIBuilder.Place(bulb.rectTransform, new Vector2(t, b % 2 == 0 ? 1f : 0f), new Vector2(26f, 26f), new Vector2(Mathf.Lerp(28f, -28f, t), b % 2 == 0 ? -8f : 8f));
                }
                var titleShadow = UIBuilder.Label(sign.transform, "TitleShadow", "RETRO DINER", 96, new Color(0.25f, 0.02f, 0.06f, 0.9f));
                UIBuilder.Place(titleShadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(860f, 130f), new Vector2(5f, 42f));
                var title = UIBuilder.Label(sign.transform, "Title", "RETRO DINER", 96, Style.Cream);
                UIBuilder.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(860f, 130f), new Vector2(0f, 48f));
                UIBuilder.Outline(title, Style.Gold, 2f);
                var threeD = UIBuilder.Label(sign.transform, "ThreeD", "3D", 70, Style.Mint);
                UIBuilder.Place(threeD.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(200f, 90f), new Vector2(-240f, -50f));
                UIBuilder.Outline(threeD, Style.Ink, 3f);
                var subPill = UIBuilder.Pill(sign.transform, "SubPill", Style.Mint, 26);
                UIBuilder.Place(subPill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(360f, 66f), new Vector2(80f, -50f));
                UIBuilder.Outline(subPill, Style.ChromeLight, 3f);
                var sub = UIBuilder.Label(subPill.transform, "Sub", "ORDER UP!", 42, Style.Ink);
                UIBuilder.Stretch(sub.rectTransform);
                if (!Style.ReducedMotion) UIBuilder.Pulse(sign.rectTransform, 0.012f, 2.4f);
            }

            // ---- small side widgets: tip jar (left), punch card + jukebox (right) ----
            var jarHolder = UIBuilder.Pill(menuPage, "TipJar", new Color(0.05f, 0.04f, 0.06f, 0.55f), 28).rectTransform;
            UIBuilder.Place(jarHolder, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(200f, 250f), new Vector2(24f, 60f));
            tipJarIcon = UIBuilder.Icon(jarHolder, "JarIcon", ArtCatalog.Pick(cfg.tipJar10, ArtCatalog.TipJar10), Style.Gold, new Vector2(130f, 130f));
            UIBuilder.Place(tipJarIcon.rectTransform, new Vector2(0.5f, 1f), new Vector2(130f, 130f), new Vector2(0f, -70f));
            tipJarText = UIBuilder.Label(jarHolder, "JarText", "", 22, Style.Cream);
            UIBuilder.Place(tipJarText.rectTransform, new Vector2(0.5f, 1f), new Vector2(190f, 50f), new Vector2(0f, -158f));
            UIBuilder.Outline(tipJarText, UIBuilder.Dark, 2f);
            crackButton = UIBuilder.Button(jarHolder, "Crack", "CRACK $2.99", ShowStore, Style.Gold, null, new Vector2(176f, 52f), 22, Style.Ink);
            UIBuilder.Place(crackButton.image.rectTransform, new Vector2(0.5f, 1f), new Vector2(176f, 52f), new Vector2(0f, -214f));

            Func<string, string, string, Action, Color, float, Button> sideBtn = (name, iconPath, caption, act, color, y) =>
            {
                var holder = UIBuilder.Pill(menuPage, name, new Color(0.05f, 0.04f, 0.06f, 0.55f), 28, true).rectTransform;
                UIBuilder.Place(holder, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(200f, 118f), new Vector2(-24f, y));
                var icon = ArtCatalog.Load(iconPath);
                var b = UIBuilder.Button(holder, "Btn", icon != null ? null : caption.Substring(0, 1), act, color, icon != null ? icon : ProcSprites.Circle(64), new Vector2(70f, 70f), 34, Style.Cream);
                UIBuilder.Place(b.image.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(70f, 70f), new Vector2(16f, 0f));
                if (icon == null) b.image.color = color;
                var cap = UIBuilder.Label(holder, "Cap", caption, 22, Style.Cream, TextAnchor.MiddleLeft);
                UIBuilder.Place(cap.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(110f, 100f), new Vector2(96f, 0f));
                var hit = holder.gameObject.AddComponent<Button>();
                hit.onClick.AddListener(() => act());
                return b;
            };
            var punch = sideBtn("PunchCard", "icons/rd3d_icon_punchcard_v001", "DAILY\nPUNCH", ShowPunchCard, UIBuilder.Purple, 120f);
            punchBadge = UIBuilder.Label(punch.transform.parent, "Badge", "!", 34, Style.Gold);
            UIBuilder.Place(punchBadge.rectTransform, new Vector2(1f, 1f), new Vector2(44f, 44f), new Vector2(4f, 4f));
            UIBuilder.Outline(punchBadge, UIBuilder.Dark, 3f);
            var spin = sideBtn("Spin", "icons/rd3d_icon_jukebox_v001", "JUKEBOX\nSPIN", DailySpin, UIBuilder.Sky, -10f);
            spinBadge = UIBuilder.Label(spin.transform.parent, "Badge", "!", 34, Style.Gold);
            UIBuilder.Place(spinBadge.rectTransform, new Vector2(1f, 1f), new Vector2(44f, 44f), new Vector2(4f, 4f));
            UIBuilder.Outline(spinBadge, UIBuilder.Dark, 3f);

            // ---- PLAY: the one dominant control ----
            var btnGreen = ArtCatalog.Load("ui/rd3d_ui_button_green_v001");
            var play = UIBuilder.Button(menuPage, "Play", "PLAY", () => TryPlay(Mathf.Clamp(GameServices.Profile.highestLevelUnlocked, 1, cfg.LevelCount)),
                btnGreen != null ? Color.white : Style.Teal, btnGreen, new Vector2(640f, 200f), 72, Style.Cream);
            UIBuilder.Place(play.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(640f, 200f), new Vector2(0f, -300f));
            if (btnGreen == null) UIBuilder.Outline(play.image, Style.ChromeLight, 5f);
            UIBuilder.Shadow(play.image, 10f);
            if (!Style.ReducedMotion) UIBuilder.Pulse(play.image.rectTransform, 0.03f, 1.4f);

            // ---- secondary row: Level Select · Store (small, icon + label) ----
            Func<string, string, string, Action, Sprite, Color, float, Button> smallBtn = (name, iconPath, label, act, sprite, color, x) =>
            {
                var b = UIBuilder.Button(menuPage, name, null, act, sprite != null ? Color.white : color, sprite, new Vector2(300f, 110f), 34, Style.Ink);
                UIBuilder.Place(b.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(300f, 110f), new Vector2(x, -470f));
                var icon = ArtCatalog.Load(iconPath);
                float lx = 0f;
                if (icon != null)
                {
                    var ic = UIBuilder.Panel(b.transform, "Icon", Color.white, icon, false); ic.preserveAspect = true;
                    UIBuilder.Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(62f, 62f), new Vector2(22f, 0f));
                    lx = 36f;
                }
                var lbl = UIBuilder.Label(b.transform, "Label", label, 32, Style.Ink);
                UIBuilder.Place(lbl.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(240f, 100f), new Vector2(lx, 0f));
                return b;
            };
            smallBtn("Levels", "icons/rd3d_icon_map_v001", "LEVELS", ShowLevels, btnMint, Style.Mint, -170f);
            smallBtn("Store", "icons/rd3d_icon_shop_v001", "STORE", ShowStore, btnGold, Style.Gold, 170f);

            bool showBuild = cfg.showBuildLabel || (GameServices.Profile != null && GameServices.Profile.devMode);
            if (showBuild)
            {
                var ver = UIBuilder.Label(menuPage, "Version", "build v" + Application.version, 20, new Color(1f, 1f, 1f, 0.55f), TextAnchor.MiddleCenter, FontStyle.Normal);
                UIBuilder.Place(ver.rectTransform, new Vector2(0.5f, 0f), new Vector2(900f, 40f), new Vector2(0f, 110f));
            }

            menuBanner = Popups.Banner(menuPage);
            RefreshCurrency();
        }

        void TryPlay(int level)
        {
            if (!GameServices.Economy.HasEnergy) { ShowEnergyPanel(); return; }
            onPlayLevel(level);
        }

        // ------------------------------------------------------------------ meta popups

        public void ShowEnergyPanel()
        {
            var eco = GameServices.Economy;
            var p = GameServices.Profile;
            eco.TickEnergy();
            string body = p.energy >= eco.EnergyMax
                ? "You're fully rested: " + p.energy + "/" + eco.EnergyMax + " hearts."
                : "Hearts: " + p.energy + "/" + eco.EnergyMax + "\nNext heart in " + Popups.FormatTime(eco.TimeToNextEnergy()) + "\n\nA heart is spent only when a shift is lost.";
            var buttons = new List<PanelButton>();
            if (p.energy < eco.EnergyMax)
            {
                buttons.Add(new PanelButton("REFILL  (" + eco.EnergyRefillTokenCost + " tokens)", () =>
                {
                    if (eco.SpendTokens(eco.EnergyRefillTokenCost)) { eco.RefillEnergy(); GameServices.Audio.Play("coin"); Popups.Close(); RefreshEnergy(); }
                    else GameEvents.RaiseToast("Not enough tokens");
                }, UIBuilder.Gold) { interactable = eco.CanAffordTokens(eco.EnergyRefillTokenCost) });
                buttons.Add(new PanelButton("WATCH AD  (+1 heart)", () =>
                {
                    GameServices.Ads.ShowRewarded(AdPlacement.RewardedBoosterRefill, ok =>
                    {
                        if (ok) { p.energy = Mathf.Min(eco.EnergyMax, p.energy + 1); GameServices.SaveProfile(); }
                        RefreshEnergy();
                    });
                }, UIBuilder.Mint));
            }
            buttons.Add(new PanelButton("CLOSE", Popups.Close, UIBuilder.Red));
            Popups.Show("ENERGY", body, buttons, true, null, 10f);
        }

        void ShowPunchCard()
        {
            var eco = GameServices.Economy;
            var p = GameServices.Profile;
            int today = eco.CurrentPunchDay;
            bool canClaim = eco.CanClaimPunchToday;
            Popups.Show("SHIFT PUNCH-CARD", "", new List<PanelButton>
            {
                new PanelButton(canClaim ? "PUNCH DAY " + today : "COME BACK TOMORROW", () =>
                {
                    var r = eco.ClaimPunch();
                    if (!r.HasValue) return;
                    GameServices.Audio.Play("win");
                    string got = r.Value.coins > 0 ? "+" + r.Value.coins + " coins" : r.Value.tokens > 0 ? "+" + r.Value.tokens + " tokens" : "+" + r.Value.boosters + " of each booster";
                    GameEvents.RaiseToast("Punched! " + got);
                    Popups.Close();
                    RefreshCurrency();
                }, UIBuilder.Gold) { interactable = canClaim },
                new PanelButton("CLOSE", Popups.Close, UIBuilder.Red),
            }, true, content =>
            {
                // Content rect is 860 wide, pivot top-centre. 4 + 3 cells, 190 wide / 150 tall, 16 gap.
                const float cw = 190f, ch = 150f, gap = 16f;
                for (int d = 1; d <= 7; d++)
                {
                    var r = eco.RewardForDay(d);
                    bool done = !canClaim ? d <= p.punchCardDay : d < today;
                    bool isToday = canClaim && d == today;
                    int col = (d - 1) % 4, row = (d - 1) / 4;
                    int inRow = row == 0 ? 4 : 3;
                    float rowW = inRow * cw + (inRow - 1) * gap;
                    float x = -rowW * 0.5f + cw * 0.5f + col * (cw + gap);
                    var cell = UIBuilder.Pill(content, "Day" + d, done ? Style.Mint : isToday ? Style.Gold : new Color(1f, 1f, 1f, 0.75f), 18);
                    UIBuilder.Place(cell.rectTransform, new Vector2(0.5f, 1f), new Vector2(cw, ch), new Vector2(x, -ch * 0.5f - row * (ch + gap)));
                    UIBuilder.Outline(cell, isToday ? Style.Cherry : Style.Chrome, isToday ? 4f : 2f);
                    var lbl = UIBuilder.Label(cell.transform, "Lbl", "Day " + d + "\n" + (r.coins > 0 ? r.coins + " c" : r.tokens > 0 ? r.tokens + " tok" : "boosters"), 26, Style.Ink);
                    UIBuilder.Stretch(lbl.rectTransform);
                    if (done) { var ok = UIBuilder.Label(cell.transform, "Done", "OK", 26, Style.Cherry); UIBuilder.Place(ok.rectTransform, new Vector2(1f, 1f), new Vector2(50f, 40f), new Vector2(-5f, -5f)); }
                    if (isToday) UIBuilder.Pulse(cell.rectTransform, 0.04f, 0.9f);
                }
                var freeze = UIBuilder.Label(content, "Freeze", "Streak freeze this week: " + (p.streakFreezesLeft > 0 ? "available" : "used"), 24, Style.Ink, TextAnchor.MiddleCenter, FontStyle.Normal);
                UIBuilder.Place(freeze.rectTransform, new Vector2(0.5f, 1f), new Vector2(700f, 40f), new Vector2(0f, -(2f * ch + gap) - 30f));
            }, 2f * 150f + 16f + 60f);
        }

        void DailySpin()
        {
            var eco = GameServices.Economy;
            if (!eco.CanSpinToday) { GameEvents.RaiseToast("The jukebox already played today"); return; }
            var r = eco.DailySpin();
            GameServices.Audio.Play("win");
            string got = r.booster.HasValue ? "a " + r.booster.Value : "+" + r.coins + " coins";
            Popups.Show("JUKEBOX SPIN", "The jukebox plays your song:", new List<PanelButton> { new PanelButton("NICE", Popups.Close, UIBuilder.Gold) }, true, content =>
            {
                var prize = UIBuilder.Label(content, "Prize", got + "!", 48, Style.Cherry);
                UIBuilder.Place(prize.rectTransform, new Vector2(0.5f, 1f), new Vector2(800f, 80f), new Vector2(0f, -40f));
                UIBuilder.Punch(prize.rectTransform, 0.25f, 0.5f);
            }, 90f);
            RefreshCurrency();
        }

        public void ShowStore()
        {
            menuPage.gameObject.SetActive(false);
            levelPage.gameObject.SetActive(false);
            settingsPage.gameObject.SetActive(false);
            root.gameObject.SetActive(true);
            store.Show();
        }

        // ------------------------------------------------------------------ level page

        void BuildLevelPage()
        {
            levelPage = UIBuilder.Rect(root, "LevelPage");
            UIBuilder.Stretch(levelPage);
            var bgSprite = ArtCatalog.Pick(cfg.levelSelectBackground, ArtCatalog.LevelSelectBackground);
            var bg = UIBuilder.Panel(levelPage, "Bg", bgSprite != null ? Color.white : new Color(0.12f, 0.2f, 0.18f), bgSprite, true);
            UIBuilder.Stretch(bg.rectTransform);
            if (bgSprite != null)
            {
                var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = 1080f / 1920f;
            }
            var dim = UIBuilder.Panel(levelPage, "Dim", new Color(0f, 0f, 0f, 0.35f), null, false);
            UIBuilder.Stretch(dim.rectTransform);
            var title = UIBuilder.Label(levelPage, "Title", "PICK A SHIFT", 72, UIBuilder.Cream);
            UIBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(1000f, 120f), new Vector2(0f, -140f));
            UIBuilder.Outline(title, UIBuilder.Dark, 4f);

            levelMap = levelPage.gameObject.AddComponent<LevelMapView>();
            levelMap.Build(levelPage, cfg, TryPlay);
            var back = UIBuilder.Button(levelPage, "Back", "BACK", ShowMenu, UIBuilder.Red, btnRed, new Vector2(400f, 130f), 40);
            UIBuilder.Place(back.image.rectTransform, new Vector2(0.5f, 0f), new Vector2(400f, 130f), new Vector2(0f, 130f));
            levelBanner = Popups.Banner(levelPage);
        }

        LevelMapView levelMap;

        void PopulateLevels()
        {
            levelMap.Populate();
            Canvas.ForceUpdateCanvases();
            levelMap.FocusCurrent();
        }

        void PopulateLevelsGridLegacy()
        {
            foreach (Transform c in levelGrid) Destroy(c.gameObject);
            int count = cfg.LevelCount;
            const int cols = 5;
            float cell = 960f / cols;
            float rowH = 125f;
            var profile = GameServices.Profile;
            for (int i = 0; i < count; i++)
            {
                int level = i + 1;
                int col = i % cols, row = i / cols;
                bool unlocked = level <= profile.highestLevelUnlocked;
                int stars = profile.GetStars(level);
                var holder = UIBuilder.Rect(levelGrid, "L" + level);
                UIBuilder.Place(holder, new Vector2(0f, 1f), new Vector2(cell - 14f, rowH - 10f), new Vector2(cell * col + cell * 0.5f, -rowH * row - rowH * 0.5f));
                int captured = level;
                var btn = UIBuilder.Button(holder, "Btn", level.ToString(), unlocked ? (Action)(() => TryPlay(captured)) : null,
                    unlocked ? (stars > 0 ? UIBuilder.Mint : UIBuilder.Cream) : new Color(0.35f, 0.35f, 0.38f), null, new Vector2(cell - 14f, 80f), 40, UIBuilder.Dark);
                UIBuilder.Place(btn.image.rectTransform, new Vector2(0.5f, 1f), new Vector2(cell - 14f, 80f), new Vector2(0f, -40f));
                btn.interactable = unlocked;
                var starLbl = UIBuilder.Label(holder, "Stars", stars > 0 ? new string('*', stars) : (unlocked ? "" : "locked"), 26, UIBuilder.Gold);
                UIBuilder.Place(starLbl.rectTransform, new Vector2(0.5f, 0f), new Vector2(cell, 30f), new Vector2(0f, 12f));
                UIBuilder.Outline(starLbl, UIBuilder.Dark, 2f);
            }
        }

        // ------------------------------------------------------------------ settings

        void BuildSettingsPage()
        {
            settingsPage = UIBuilder.Rect(root, "SettingsPage");
            UIBuilder.Stretch(settingsPage);
            var dim = UIBuilder.Panel(settingsPage, "Dim", new Color(0f, 0f, 0f, 0.7f), null, true);
            UIBuilder.Stretch(dim.rectTransform);
            var title = UIBuilder.Label(settingsPage, "Title", "SETTINGS", 72, UIBuilder.Cream);
            UIBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(1000f, 120f), new Vector2(0f, -180f));

            Button soundBtn = null, musicBtn = null, hapticBtn = null, motionBtn = null, removeAdsBtn = null;
            Action refresh = () =>
            {
                var p = GameServices.Profile;
                soundBtn.GetComponentInChildren<Text>().text = "SOUND: " + (p.soundOn ? "ON" : "OFF");
                musicBtn.GetComponentInChildren<Text>().text = "MUSIC: " + (p.musicOn ? "ON" : "OFF");
                hapticBtn.GetComponentInChildren<Text>().text = "HAPTICS: " + (p.hapticsOn ? "ON" : "OFF");
                motionBtn.GetComponentInChildren<Text>().text = "REDUCED MOTION: " + (p.reducedMotion ? "ON" : "OFF");
                removeAdsBtn.GetComponentInChildren<Text>().text = p.noAds ? "ADS REMOVED  (thank you!)" : "REMOVE ADS  $4.99";
                removeAdsBtn.interactable = !p.noAds;
            };
            float y = 360f;
            Func<string, Action, Color, Button> row = (label, act, color) =>
            {
                var b = UIBuilder.Button(settingsPage, label, label, act, color, Popups.ButtonSprite(color), new Vector2(600f, 140f), 34, color == UIBuilder.Red ? Color.white : UIBuilder.Dark);
                UIBuilder.Place(b.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(600f, 140f), new Vector2(0f, y));
                y -= 125f;
                return b;
            };
            soundBtn = row("SOUND", () => { var p = GameServices.Profile; p.soundOn = !p.soundOn; GameServices.Audio.SfxEnabled = p.soundOn; GameServices.SaveProfile(); refresh(); }, UIBuilder.Mint);
            musicBtn = row("MUSIC", () => { var p = GameServices.Profile; p.musicOn = !p.musicOn; GameServices.Audio.MusicEnabled = p.musicOn; GameServices.SaveProfile(); refresh(); }, UIBuilder.Mint);
            hapticBtn = row("HAPTICS", () => { var p = GameServices.Profile; p.hapticsOn = !p.hapticsOn; GameServices.Haptics.Enabled = p.hapticsOn; GameServices.SaveProfile(); refresh(); }, UIBuilder.Mint);
            motionBtn = row("REDUCED MOTION", () => { var p = GameServices.Profile; p.reducedMotion = !p.reducedMotion; GameServices.SaveProfile(); refresh(); }, UIBuilder.Mint);
            removeAdsBtn = row("REMOVE ADS", () =>
            {
                var offer = GameServices.Economy.Catalog.Find(StoreCatalog.SkuRemoveAds);
                if (offer == null) return;
                GameServices.IAP.Purchase(offer.sku, ok => { if (ok) { GameServices.Economy.ApplyPurchase(offer); GameEvents.RaiseToast("Ads removed. Enjoy!"); refresh(); RefreshBanners(); } });
            }, UIBuilder.Gold);
            row("RESTORE PURCHASES", () =>
            {
                GameServices.IAP.RestorePurchases(ok =>
                {
                    int n = ok ? GameServices.Economy.RestoreFromReceipts(GameServices.IAP) : 0;
                    GameEvents.RaiseToast(n > 0 ? "Restored " + n + " purchase(s)" : "Nothing new to restore");
                    refresh(); RefreshBanners();
                });
            }, UIBuilder.Gold);
            var reset = UIBuilder.Button(settingsPage, "Reset", "RESET PROGRESS (DEV)", () => { GameServices.ResetProfile(); GameEvents.RaiseToast("Progress reset"); refresh(); RefreshCurrency(); }, new Color(0.4f, 0.4f, 0.45f), null, new Vector2(560f, 100f), 30);
            UIBuilder.Place(reset.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(560f, 100f), new Vector2(0f, y - 20f));
            var back = UIBuilder.Button(settingsPage, "Back", "BACK", ShowMenu, UIBuilder.Red, btnRed, new Vector2(400f, 130f), 40);
            UIBuilder.Place(back.image.rectTransform, new Vector2(0.5f, 0f), new Vector2(400f, 130f), new Vector2(0f, 110f));
            settingsPage.gameObject.SetActive(false);
            settingsRefresh = refresh;
        }

        // ------------------------------------------------------------------ navigation + refresh

        public void ShowMenu()
        {
            root.gameObject.SetActive(true);
            if (store != null) store.Hide();
            menuPage.gameObject.SetActive(true);
            levelPage.gameObject.SetActive(false);
            settingsPage.gameObject.SetActive(false);
            RefreshCurrency();
        }

        public void ShowLevels()
        {
            PopulateLevels();
            if (store != null) store.Hide();
            menuPage.gameObject.SetActive(false);
            levelPage.gameObject.SetActive(true);
            settingsPage.gameObject.SetActive(false);
            RefreshBanners();
        }

        void ShowSettings()
        {
            settingsPage.gameObject.SetActive(true);
            settingsRefresh?.Invoke();
        }

        public void Hide() { root.gameObject.SetActive(false); }

        public void RefreshCurrency()
        {
            var p = GameServices.Profile;
            if (p == null || coinsText == null) return;
            coinsText.text = p.coins.ToString();
            tokensText.text = p.tokens.ToString();
            RefreshEnergy();
            RefreshTipJar();
            RefreshBanners();
            var eco = GameServices.Economy;
            if (eco != null)
            {
                punchBadge.gameObject.SetActive(eco.CanClaimPunchToday);
                spinBadge.gameObject.SetActive(eco.CanSpinToday);
            }
        }

        void RefreshEnergy()
        {
            var eco = GameServices.Economy;
            var p = GameServices.Profile;
            if (eco == null || p == null || hearts[0] == null) return;
            eco.TickEnergy();
            for (int i = 0; i < hearts.Length; i++)
            {
                hearts[i].gameObject.SetActive(i < eco.EnergyMax);
                hearts[i].color = i < p.energy ? Color.white : new Color(0.25f, 0.25f, 0.3f, 0.8f);
                if (hearts[i].sprite == null) hearts[i].color = i < p.energy ? UIBuilder.Red : new Color(0.3f, 0.3f, 0.35f);
            }
            energyTimerText.text = p.energy >= eco.EnergyMax ? "" : "next in " + Popups.FormatTime(eco.TimeToNextEnergy());
        }

        void RefreshTipJar()
        {
            var eco = GameServices.Economy;
            var p = GameServices.Profile;
            if (eco == null || tipJarIcon == null) return;
            float f = eco.TipJarFill;
            var s = f >= 0.99f ? ArtCatalog.Pick(cfg.tipJar100, ArtCatalog.TipJar100) : f >= 0.4f ? ArtCatalog.Pick(cfg.tipJar50, ArtCatalog.TipJar50) : ArtCatalog.Pick(cfg.tipJar10, ArtCatalog.TipJar10);
            if (s != null) { tipJarIcon.sprite = s; tipJarIcon.color = Color.white; }
            tipJarText.text = p.tipJarTokens + " / " + eco.TipJarThreshold + "\ntip tokens";
            crackButton.gameObject.SetActive(eco.TipJarReady);
        }

        void RefreshBanners()
        {
            var policy = GameServices.AdPolicy;
            bool allowed = policy != null && policy.BannersAllowed;
            if (menuBanner != null) menuBanner.gameObject.SetActive(allowed && policy.Policy.bannerOnMainMenu);
            if (levelBanner != null) levelBanner.gameObject.SetActive(allowed && policy.Policy.bannerOnLevelSelect);
        }
    }
}
