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
                fit.aspectRatio = 1080f / 1920f;
            }

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

            // currency + energy strip
            var strip = UIBuilder.Panel(menuPage, "Currency", new Color(0f, 0f, 0f, 0.45f), null, false);
            strip.rectTransform.anchorMin = new Vector2(0f, 1f); strip.rectTransform.anchorMax = new Vector2(1f, 1f);
            strip.rectTransform.pivot = new Vector2(0.5f, 1f); strip.rectTransform.sizeDelta = new Vector2(0f, 110f); strip.rectTransform.anchoredPosition = new Vector2(0f, -40f);
            var coinIcon = UIBuilder.Icon(strip.transform, "CoinIcon", ArtCatalog.Pick(cfg.coinIcon, ArtCatalog.Coin), UIBuilder.Gold, new Vector2(64f, 64f));
            UIBuilder.Place(coinIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(64f, 64f), new Vector2(60f, 0f));
            coinsText = UIBuilder.Label(strip.transform, "Coins", "0", 36, Color.white, TextAnchor.MiddleLeft);
            UIBuilder.Place(coinsText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(200f, 80f), new Vector2(100f, 0f));
            var tokenIcon = UIBuilder.Icon(strip.transform, "TokenIcon", ArtCatalog.Pick(cfg.tokenIcon, ArtCatalog.Token), UIBuilder.Purple, new Vector2(64f, 64f));
            UIBuilder.Place(tokenIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(64f, 64f), new Vector2(330f, 0f));
            tokensText = UIBuilder.Label(strip.transform, "Tokens", "0", 36, Color.white, TextAnchor.MiddleLeft);
            UIBuilder.Place(tokensText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(200f, 80f), new Vector2(370f, 0f));

            var heartSprite = ArtCatalog.Pick(cfg.heartIcon, ArtCatalog.Heart);
            for (int i = 0; i < hearts.Length; i++)
            {
                hearts[i] = UIBuilder.Icon(strip.transform, "Heart" + i, heartSprite, UIBuilder.Red, new Vector2(44f, 44f));
                UIBuilder.Place(hearts[i].rectTransform, new Vector2(1f, 0.5f), new Vector2(44f, 44f), new Vector2(-300f + i * 46f, 14f));
            }
            energyTimerText = UIBuilder.Label(strip.transform, "EnergyTimer", "", 22, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleCenter, FontStyle.Normal);
            UIBuilder.Place(energyTimerText.rectTransform, new Vector2(1f, 0.5f), new Vector2(240f, 30f), new Vector2(-208f, -28f));
            var plusEnergy = UIBuilder.Button(strip.transform, "PlusEnergy", "+", ShowEnergyPanel, UIBuilder.Mint, null, new Vector2(54f, 54f), 36, UIBuilder.Dark);
            UIBuilder.Place(plusEnergy.image.rectTransform, new Vector2(1f, 0.5f), new Vector2(54f, 54f), new Vector2(-50f, 0f));
            energyText = UIBuilder.Label(strip.transform, "EnergyText", "", 1, Color.clear);   // unused placeholder for API symmetry
            energyText.gameObject.SetActive(false);

            var title = UIBuilder.Label(menuPage, "Title", "RETRO DINER 3D", 96, UIBuilder.Cream);
            UIBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(1000f, 140f), new Vector2(0f, -260f));
            UIBuilder.Outline(title, UIBuilder.Red, 5f);
            var sub = UIBuilder.Label(menuPage, "Sub", "ORDER UP!", 64, UIBuilder.Gold);
            UIBuilder.Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(1000f, 100f), new Vector2(0f, -380f));
            UIBuilder.Outline(sub, UIBuilder.Dark, 4f);

            // Tip jar widget (left) + punch card / spin (right)
            var jarHolder = UIBuilder.Rect(menuPage, "TipJar");
            UIBuilder.Place(jarHolder, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(240f, 300f), new Vector2(30f, 120f));
            tipJarIcon = UIBuilder.Icon(jarHolder, "JarIcon", ArtCatalog.Pick(cfg.tipJar10, ArtCatalog.TipJar10), UIBuilder.Gold, new Vector2(170f, 170f));
            UIBuilder.Place(tipJarIcon.rectTransform, new Vector2(0.5f, 1f), new Vector2(170f, 170f), new Vector2(0f, -85f));
            tipJarText = UIBuilder.Label(jarHolder, "JarText", "", 26, Color.white);
            UIBuilder.Place(tipJarText.rectTransform, new Vector2(0.5f, 1f), new Vector2(240f, 60f), new Vector2(0f, -200f));
            UIBuilder.Outline(tipJarText, UIBuilder.Dark, 2f);
            crackButton = UIBuilder.Button(jarHolder, "Crack", "CRACK $2.99", ShowStore, UIBuilder.Gold, null, new Vector2(220f, 64f), 26, UIBuilder.Dark);
            UIBuilder.Place(crackButton.image.rectTransform, new Vector2(0.5f, 1f), new Vector2(220f, 64f), new Vector2(0f, -268f));

            var punch = UIBuilder.Button(menuPage, "PunchCard", "DAILY\nPUNCH-CARD", ShowPunchCard, UIBuilder.Purple, null, new Vector2(230f, 110f), 28);
            UIBuilder.Place(punch.image.rectTransform, new Vector2(1f, 0.5f), new Vector2(230f, 110f), new Vector2(-145f, 200f));
            punchBadge = UIBuilder.Label(punch.transform, "Badge", "!", 40, UIBuilder.Gold);
            UIBuilder.Place(punchBadge.rectTransform, new Vector2(1f, 1f), new Vector2(50f, 50f), new Vector2(0f, 0f));
            UIBuilder.Outline(punchBadge, UIBuilder.Dark, 3f);

            var spin = UIBuilder.Button(menuPage, "Spin", "JUKEBOX\nSPIN", DailySpin, UIBuilder.Sky, null, new Vector2(230f, 110f), 28);
            UIBuilder.Place(spin.image.rectTransform, new Vector2(1f, 0.5f), new Vector2(230f, 110f), new Vector2(-145f, 60f));
            spinBadge = UIBuilder.Label(spin.transform, "Badge", "!", 40, UIBuilder.Gold);
            UIBuilder.Place(spinBadge.rectTransform, new Vector2(1f, 1f), new Vector2(50f, 50f), new Vector2(0f, 0f));
            UIBuilder.Outline(spinBadge, UIBuilder.Dark, 3f);

            var play = UIBuilder.Button(menuPage, "Play", "PLAY", () => TryPlay(Mathf.Clamp(GameServices.Profile.highestLevelUnlocked, 1, cfg.LevelCount)), UIBuilder.Red, btnRed, new Vector2(600f, 190f), 64);
            UIBuilder.Place(play.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(600f, 190f), new Vector2(0f, -140f));

            var levels = UIBuilder.Button(menuPage, "Levels", "LEVEL SELECT", ShowLevels, UIBuilder.Mint, btnMint, new Vector2(520f, 150f), 42, UIBuilder.Dark);
            UIBuilder.Place(levels.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(520f, 150f), new Vector2(0f, -330f));

            var storeBtn = UIBuilder.Button(menuPage, "Store", "STORE", ShowStore, UIBuilder.Gold, btnGold, new Vector2(520f, 150f), 42, UIBuilder.Dark);
            UIBuilder.Place(storeBtn.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(520f, 150f), new Vector2(0f, -490f));

            var settings = UIBuilder.Button(menuPage, "Settings", "SETTINGS", ShowSettings, new Color(0.35f, 0.35f, 0.4f), null, new Vector2(360f, 100f), 34);
            UIBuilder.Place(settings.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(360f, 100f), new Vector2(0f, -640f));

            var ver = UIBuilder.Label(menuPage, "Version", "pre-alpha M5 build  |  v" + Application.version, 24, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleCenter, FontStyle.Normal);
            UIBuilder.Place(ver.rectTransform, new Vector2(0.5f, 0f), new Vector2(900f, 50f), new Vector2(0f, 120f));

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
            Popups.Show("ENERGY", body, buttons, true, null, 1000f);
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
                for (int d = 1; d <= 7; d++)
                {
                    var r = eco.RewardForDay(d);
                    bool done = !canClaim ? d <= p.punchCardDay : d < today;
                    bool isToday = canClaim && d == today;
                    var cell = UIBuilder.Panel(content, "Day" + d, done ? UIBuilder.Mint : isToday ? UIBuilder.Gold : new Color(1f, 1f, 1f, 0.5f), null, false);
                    int col = (d - 1) % 4, row = (d - 1) / 4;
                    UIBuilder.Place(cell.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(170f, 150f), new Vector2(row == 1 ? 95f + col * 185f : col * 185f, -row * 165f));
                    var lbl = UIBuilder.Label(cell.transform, "Lbl", "Day " + d + "\n" + (r.coins > 0 ? r.coins + " c" : r.tokens > 0 ? r.tokens + " tok" : "boosters"), 26, UIBuilder.Dark);
                    UIBuilder.Stretch(lbl.rectTransform);
                    if (done) { var ok = UIBuilder.Label(cell.transform, "Done", "OK", 30, UIBuilder.Red); UIBuilder.Place(ok.rectTransform, new Vector2(1f, 1f), new Vector2(50f, 40f), new Vector2(-5f, -5f)); }
                }
                var freeze = UIBuilder.Label(content, "Freeze", "Streak freeze this week: " + (p.streakFreezesLeft > 0 ? "available" : "used"), 24, UIBuilder.Dark, TextAnchor.MiddleCenter, FontStyle.Normal);
                UIBuilder.Place(freeze.rectTransform, new Vector2(0.5f, 0f), new Vector2(700f, 40f), new Vector2(0f, -10f));
            }, 1250f);
        }

        void DailySpin()
        {
            var eco = GameServices.Economy;
            if (!eco.CanSpinToday) { GameEvents.RaiseToast("The jukebox already played today"); return; }
            var r = eco.DailySpin();
            GameServices.Audio.Play("win");
            string got = r.booster.HasValue ? "a " + r.booster.Value : "+" + r.coins + " coins";
            Popups.Show("JUKEBOX SPIN", "\n\nThe jukebox plays your song:\n\n" + got + "!", new List<PanelButton> { new PanelButton("NICE", Popups.Close, UIBuilder.Gold) }, true, null, 900f);
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

            levelGrid = UIBuilder.Rect(levelPage, "Grid");
            UIBuilder.Place(levelGrid, new Vector2(0.5f, 0.5f), new Vector2(960f, 1300f), new Vector2(0f, 20f));
            var back = UIBuilder.Button(levelPage, "Back", "BACK", ShowMenu, UIBuilder.Red, btnRed, new Vector2(400f, 130f), 40);
            UIBuilder.Place(back.image.rectTransform, new Vector2(0.5f, 0f), new Vector2(400f, 130f), new Vector2(0f, 130f));
            levelBanner = Popups.Banner(levelPage);
        }

        void PopulateLevels()
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

            Button soundBtn = null, musicBtn = null, hapticBtn = null, removeAdsBtn = null;
            Action refresh = () =>
            {
                var p = GameServices.Profile;
                soundBtn.GetComponentInChildren<Text>().text = "SOUND: " + (p.soundOn ? "ON" : "OFF");
                musicBtn.GetComponentInChildren<Text>().text = "MUSIC: " + (p.musicOn ? "ON" : "OFF");
                hapticBtn.GetComponentInChildren<Text>().text = "HAPTICS: " + (p.hapticsOn ? "ON" : "OFF");
                removeAdsBtn.GetComponentInChildren<Text>().text = p.noAds ? "ADS REMOVED  (thank you!)" : "REMOVE ADS  $4.99";
                removeAdsBtn.interactable = !p.noAds;
            };
            float y = 320f;
            Func<string, Action, Color, Button> row = (label, act, color) =>
            {
                var b = UIBuilder.Button(settingsPage, label, "", act, color, Popups.ButtonSprite(color), new Vector2(600f, 150f), 36, color == UIBuilder.Red ? Color.white : UIBuilder.Dark);
                UIBuilder.Place(b.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(600f, 150f), new Vector2(0f, y));
                b.GetComponentInChildren<Text>().text = label;
                y -= 140f;
                return b;
            };
            soundBtn = row("SOUND", () => { var p = GameServices.Profile; p.soundOn = !p.soundOn; GameServices.Audio.SfxEnabled = p.soundOn; GameServices.SaveProfile(); refresh(); }, UIBuilder.Mint);
            musicBtn = row("MUSIC", () => { var p = GameServices.Profile; p.musicOn = !p.musicOn; GameServices.Audio.MusicEnabled = p.musicOn; GameServices.SaveProfile(); refresh(); }, UIBuilder.Mint);
            hapticBtn = row("HAPTICS", () => { var p = GameServices.Profile; p.hapticsOn = !p.hapticsOn; GameServices.Haptics.Enabled = p.hapticsOn; GameServices.SaveProfile(); refresh(); }, UIBuilder.Mint);
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
            energyTimerText.text = p.energy >= eco.EnergyMax ? "FULL" : "next in " + Popups.FormatTime(eco.TimeToNextEnergy());
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
