using System;
using UnityEngine;
using UnityEngine.UI;
using RetroDiner.Core;
using RetroDiner.Data;
using RetroDiner.Rules;

namespace RetroDiner.UI
{
    /// <summary>Three round booster buttons with count badges below the tray.</summary>
    public class BoosterBarView : MonoBehaviour
    {
        class Slot
        {
            public BoosterType type;
            public Button button;
            public Image face, icon, badge, lockOverlay;
            public Text badgeText, label;
        }

        RectTransform root;
        readonly Slot[] slots = new Slot[3];
        GameConfig cfg;
        Action<BoosterType> onUse;

        public void Build(Transform parent, GameConfig config, Action<BoosterType> useCallback)
        {
            cfg = config;
            onUse = useCallback;
            root = UIBuilder.Rect(parent, "Boosters");
            UIBuilder.Place(root, new Vector2(0.5f, 0f), new Vector2(1080f, 260f), new Vector2(0f, 170f));

            Make(0, BoosterType.GoldenSpatula, "Spatula\nFlip", UIBuilder.Sky, ArtCatalog.Pick(cfg.boosterSpatula, ArtCatalog.BoosterSpatula), -300f);
            Make(1, BoosterType.OrderUpBell, "Bell\nPause", UIBuilder.Purple, ArtCatalog.Pick(cfg.boosterBell, ArtCatalog.BoosterBell), 0f);
            Make(2, BoosterType.RewindCoaster, "Rewind\nCoaster", UIBuilder.Orange, ArtCatalog.Pick(cfg.boosterRewind, ArtCatalog.BoosterRewind), 300f);
        }

        void Make(int i, BoosterType type, string label, Color color, Sprite sprite, float x)
        {
            var s = new Slot { type = type };
            var holder = UIBuilder.Rect(root, "Booster_" + type);
            UIBuilder.Place(holder, new Vector2(0.5f, 0.5f), new Vector2(220f, 260f), new Vector2(x, 0f));

            s.button = UIBuilder.Button(holder, "Button", null, () => onUse?.Invoke(type), color, sprite, new Vector2(170f, 170f));
            UIBuilder.Place(s.button.image.rectTransform, new Vector2(0.5f, 1f), new Vector2(170f, 170f), new Vector2(0f, -85f));
            s.face = s.button.image;
            if (sprite == null)
            {
                var glyph = UIBuilder.Label(s.face.transform, "Glyph", type == BoosterType.GoldenSpatula ? "S" : type == BoosterType.OrderUpBell ? "B" : "R", 80, Color.white);
                UIBuilder.Stretch(glyph.rectTransform);
            }

            s.badge = UIBuilder.Panel(holder, "Badge", UIBuilder.Gold, null, false);
            UIBuilder.Place(s.badge.rectTransform, new Vector2(0.5f, 1f), new Vector2(60f, 60f), new Vector2(60f, -150f));
            UIBuilder.Outline(s.badge, Color.white, 3f);
            s.badgeText = UIBuilder.Label(s.badge.transform, "Count", "0", 34, UIBuilder.Dark);
            UIBuilder.Stretch(s.badgeText.rectTransform);

            s.lockOverlay = UIBuilder.Panel(holder, "Lock", new Color(0.1f, 0.1f, 0.1f, 0.65f), null, false);
            UIBuilder.Place(s.lockOverlay.rectTransform, new Vector2(0.5f, 1f), new Vector2(170f, 170f), new Vector2(0f, -85f));
            var lockLbl = UIBuilder.Label(s.lockOverlay.transform, "LockLbl", "LOCKED", 26, Color.white);
            UIBuilder.Stretch(lockLbl.rectTransform);

            s.label = UIBuilder.Label(holder, "Label", label, 30, Color.white);
            UIBuilder.Place(s.label.rectTransform, new Vector2(0.5f, 0f), new Vector2(220f, 80f), new Vector2(0f, 40f));
            UIBuilder.Outline(s.label, new Color(0f, 0f, 0f, 0.7f), 2f);
            slots[i] = s;
        }

        public void Refresh(int levelIndex)
        {
            var profile = GameServices.Profile;
            foreach (var s in slots)
            {
                if (s == null) continue;
                bool unlocked = ShiftRules.IsBoosterUnlocked(cfg, s.type, levelIndex);
                int count = profile != null ? profile.GetBooster(s.type) : 0;
                s.lockOverlay.gameObject.SetActive(!unlocked);
                s.button.interactable = unlocked;
                s.badgeText.text = count.ToString();
                s.badge.color = count > 0 ? UIBuilder.Gold : new Color(0.6f, 0.6f, 0.6f);
            }
        }

        public void SetInteractable(bool on)
        {
            foreach (var s in slots) if (s != null) s.button.interactable = on && !s.lockOverlay.gameObject.activeSelf;
        }

        public void Flash(BoosterType type)
        {
            foreach (var s in slots) if (s != null && s.type == type) UIBuilder.Punch(s.face.rectTransform, 0.25f, 0.3f);
        }

        public RectTransform Root => root;
    }
}
