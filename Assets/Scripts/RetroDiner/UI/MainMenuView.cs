using System;
using UnityEngine;
using UnityEngine.UI;
using RetroDiner.Core;
using RetroDiner.Data;

namespace RetroDiner.UI
{
    /// <summary>Main menu + level select grid + settings, all on one overlay canvas.</summary>
    public class MainMenuView : MonoBehaviour
    {
        RectTransform root, menuPage, levelPage, settingsPage;
        Image background;
        Text coinsText, tokensText;
        GameConfig cfg;
        Action<int> onPlayLevel;
        Sprite btnRed, btnMint, btnGold;

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
            background = UIBuilder.Panel(root, "Background", bgSprite != null ? Color.white : new Color(0.16f, 0.12f, 0.2f), bgSprite, true);
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
            ShowMenu();
            GameEvents.ProfileChanged += RefreshCurrency;
        }

        void OnDestroy() { GameEvents.ProfileChanged -= RefreshCurrency; }

        void BuildMenuPage()
        {
            menuPage = UIBuilder.Rect(root, "MenuPage");
            UIBuilder.Stretch(menuPage);

            var title = UIBuilder.Label(menuPage, "Title", "RETRO DINER 3D", 96, UIBuilder.Cream);
            UIBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(1000f, 140f), new Vector2(0f, -260f));
            UIBuilder.Outline(title, UIBuilder.Red, 5f);
            var sub = UIBuilder.Label(menuPage, "Sub", "ORDER UP!", 64, UIBuilder.Gold);
            UIBuilder.Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(1000f, 100f), new Vector2(0f, -380f));
            UIBuilder.Outline(sub, UIBuilder.Dark, 4f);

            var play = UIBuilder.Button(menuPage, "Play", "PLAY", () => onPlayLevel(Mathf.Clamp(GameServices.Profile.highestLevelUnlocked, 1, cfg.LevelCount)), UIBuilder.Red, btnRed, new Vector2(600f, 190f), 64);
            UIBuilder.Place(play.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(600f, 190f), new Vector2(0f, -120f));

            var levels = UIBuilder.Button(menuPage, "Levels", "LEVEL SELECT", ShowLevels, UIBuilder.Mint, btnMint, new Vector2(520f, 160f), 44, UIBuilder.Dark);
            UIBuilder.Place(levels.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(520f, 160f), new Vector2(0f, -330f));

            var settings = UIBuilder.Button(menuPage, "Settings", "SETTINGS", ShowSettings, UIBuilder.Gold, btnGold, new Vector2(520f, 160f), 44, UIBuilder.Dark);
            UIBuilder.Place(settings.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(520f, 160f), new Vector2(0f, -510f));

            // currency strip
            var strip = UIBuilder.Panel(menuPage, "Currency", new Color(0f, 0f, 0f, 0.45f), null, false);
            strip.rectTransform.anchorMin = new Vector2(0f, 1f); strip.rectTransform.anchorMax = new Vector2(1f, 1f);
            strip.rectTransform.pivot = new Vector2(0.5f, 1f); strip.rectTransform.sizeDelta = new Vector2(0f, 110f); strip.rectTransform.anchoredPosition = new Vector2(0f, -40f);
            var coinIcon = UIBuilder.Icon(strip.transform, "CoinIcon", ArtCatalog.Pick(cfg.coinIcon, ArtCatalog.Coin), UIBuilder.Gold, new Vector2(70f, 70f));
            UIBuilder.Place(coinIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(70f, 70f), new Vector2(80f, 0f));
            coinsText = UIBuilder.Label(strip.transform, "Coins", "0", 40, Color.white, TextAnchor.MiddleLeft);
            UIBuilder.Place(coinsText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(260f, 80f), new Vector2(130f, 0f));
            var tokenIcon = UIBuilder.Icon(strip.transform, "TokenIcon", ArtCatalog.Pick(cfg.tokenIcon, ArtCatalog.Token), UIBuilder.Purple, new Vector2(70f, 70f));
            UIBuilder.Place(tokenIcon.rectTransform, new Vector2(1f, 0.5f), new Vector2(70f, 70f), new Vector2(-300f, 0f));
            tokensText = UIBuilder.Label(strip.transform, "Tokens", "0", 40, Color.white, TextAnchor.MiddleLeft);
            UIBuilder.Place(tokensText.rectTransform, new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(240f, 80f), new Vector2(-250f, 0f));

            var ver = UIBuilder.Label(menuPage, "Version", "pre-alpha M1-M3 build  |  v" + Application.version, 24, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleCenter, FontStyle.Normal);
            UIBuilder.Place(ver.rectTransform, new Vector2(0.5f, 0f), new Vector2(900f, 50f), new Vector2(0f, 40f));
            RefreshCurrency();
        }

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
            var title = UIBuilder.Label(levelPage, "Title", "PICK A SHIFT", 72, UIBuilder.Cream);
            UIBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(1000f, 120f), new Vector2(0f, -140f));
            UIBuilder.Outline(title, UIBuilder.Dark, 4f);

            var grid = UIBuilder.Rect(levelPage, "Grid");
            UIBuilder.Place(grid, new Vector2(0.5f, 0.5f), new Vector2(960f, 1300f), new Vector2(0f, 20f));
            var back = UIBuilder.Button(levelPage, "Back", "BACK", ShowMenu, UIBuilder.Red, btnRed, new Vector2(400f, 130f), 40);
            UIBuilder.Place(back.image.rectTransform, new Vector2(0.5f, 0f), new Vector2(400f, 130f), new Vector2(0f, 110f));
            levelGrid = grid;
        }

        RectTransform levelGrid;

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
                var btn = UIBuilder.Button(holder, "Btn", level.ToString(), unlocked ? (Action)(() => onPlayLevel(captured)) : null,
                    unlocked ? (stars > 0 ? UIBuilder.Mint : UIBuilder.Cream) : new Color(0.35f, 0.35f, 0.38f), null, new Vector2(cell - 14f, 80f), 40, UIBuilder.Dark);
                UIBuilder.Place(btn.image.rectTransform, new Vector2(0.5f, 1f), new Vector2(cell - 14f, 80f), new Vector2(0f, -40f));
                btn.interactable = unlocked;
                var starLbl = UIBuilder.Label(holder, "Stars", stars > 0 ? new string('*', stars) : (unlocked ? "" : "locked"), 26, UIBuilder.Gold);
                UIBuilder.Place(starLbl.rectTransform, new Vector2(0.5f, 0f), new Vector2(cell, 30f), new Vector2(0f, 12f));
                UIBuilder.Outline(starLbl, UIBuilder.Dark, 2f);
            }
        }

        void BuildSettingsPage()
        {
            settingsPage = UIBuilder.Rect(root, "SettingsPage");
            UIBuilder.Stretch(settingsPage);
            var dim = UIBuilder.Panel(settingsPage, "Dim", new Color(0f, 0f, 0f, 0.6f), null, true);
            UIBuilder.Stretch(dim.rectTransform);
            var title = UIBuilder.Label(settingsPage, "Title", "SETTINGS", 72, UIBuilder.Cream);
            UIBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(1000f, 120f), new Vector2(0f, -200f));

            Button soundBtn = null, musicBtn = null, hapticBtn = null;
            Action refresh = () =>
            {
                var p = GameServices.Profile;
                soundBtn.GetComponentInChildren<Text>().text = "SOUND: " + (p.soundOn ? "ON" : "OFF");
                musicBtn.GetComponentInChildren<Text>().text = "MUSIC: " + (p.musicOn ? "ON" : "OFF");
                hapticBtn.GetComponentInChildren<Text>().text = "HAPTICS: " + (p.hapticsOn ? "ON" : "OFF");
            };
            soundBtn = UIBuilder.Button(settingsPage, "Sound", "", () => { var p = GameServices.Profile; p.soundOn = !p.soundOn; GameServices.Audio.SfxEnabled = p.soundOn; GameServices.SaveProfile(); refresh(); }, UIBuilder.Mint, btnMint, new Vector2(560f, 140f), 40, UIBuilder.Dark);
            UIBuilder.Place(soundBtn.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(560f, 140f), new Vector2(0f, 200f));
            musicBtn = UIBuilder.Button(settingsPage, "Music", "", () => { var p = GameServices.Profile; p.musicOn = !p.musicOn; GameServices.Audio.MusicEnabled = p.musicOn; GameServices.SaveProfile(); refresh(); }, UIBuilder.Mint, btnMint, new Vector2(560f, 140f), 40, UIBuilder.Dark);
            UIBuilder.Place(musicBtn.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(560f, 140f), new Vector2(0f, 30f));
            hapticBtn = UIBuilder.Button(settingsPage, "Haptics", "", () => { var p = GameServices.Profile; p.hapticsOn = !p.hapticsOn; GameServices.Haptics.Enabled = p.hapticsOn; GameServices.SaveProfile(); refresh(); }, UIBuilder.Mint, btnMint, new Vector2(560f, 140f), 40, UIBuilder.Dark);
            UIBuilder.Place(hapticBtn.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(560f, 140f), new Vector2(0f, -140f));
            var reset = UIBuilder.Button(settingsPage, "Reset", "RESET PROGRESS (DEV)", () => { GameServices.ResetProfile(); GameEvents.RaiseToast("Progress reset"); RefreshCurrency(); }, new Color(0.4f, 0.4f, 0.45f), null, new Vector2(560f, 110f), 32);
            UIBuilder.Place(reset.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(560f, 110f), new Vector2(0f, -330f));
            var back = UIBuilder.Button(settingsPage, "Back", "BACK", ShowMenu, UIBuilder.Red, btnRed, new Vector2(400f, 130f), 40);
            UIBuilder.Place(back.image.rectTransform, new Vector2(0.5f, 0f), new Vector2(400f, 130f), new Vector2(0f, 110f));
            settingsPage.gameObject.SetActive(false);
            settingsRefresh = refresh;
        }

        Action settingsRefresh;

        public void ShowMenu()
        {
            root.gameObject.SetActive(true);
            menuPage.gameObject.SetActive(true);
            levelPage.gameObject.SetActive(false);
            settingsPage.gameObject.SetActive(false);
            RefreshCurrency();
        }

        public void ShowLevels()
        {
            PopulateLevels();
            menuPage.gameObject.SetActive(false);
            levelPage.gameObject.SetActive(true);
            settingsPage.gameObject.SetActive(false);
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
        }
    }
}
