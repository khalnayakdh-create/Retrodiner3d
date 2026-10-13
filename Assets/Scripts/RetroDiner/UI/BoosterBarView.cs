using System;
using UnityEngine;
using UnityEngine.UI;
using RetroDiner.Core;
using RetroDiner.Data;
using RetroDiner.Rules;

namespace RetroDiner.UI
{
    /// <summary>
    /// Three boosters on one common circular chassis (chrome rim, enamel face, top-left highlight),
    /// tool icon on top, a small round red quantity badge at the same spot on each, and a clear
    /// zero-inventory / locked look. Art: ui/rd3d_ui_booster_chassis_{blue,purple,orange,grey}_v001,
    /// icons/rd3d_icon_{spatula,bell,coaster}_v001; falls back to procedural chassis + legacy icons.
    /// </summary>
    public class BoosterBarView : MonoBehaviour
    {
        class Slot
        {
            public BoosterType type;
            public Button button;
            public Image chassis, face, icon, badge, lockOverlay;
            public Text badgeText, label;
            public Color faceColor;
        }

        RectTransform root;
        readonly Slot[] slots = new Slot[3];
        GameConfig cfg;
        Action<BoosterType> onUse;

        const float Size = 158f;

        public void Build(Transform parent, GameConfig config, Action<BoosterType> useCallback)
        {
            cfg = config;
            onUse = useCallback;
            root = UIBuilder.Rect(parent, "Boosters");
            UIBuilder.Place(root, new Vector2(0.5f, 0f), new Vector2(1080f, 250f), new Vector2(0f, Style.BoosterY));

            Make(0, BoosterType.GoldenSpatula, "Spatula", UIBuilder.Sky, "blue", "icons/rd3d_icon_spatula_v001", ArtCatalog.Pick(cfg.boosterSpatula, ArtCatalog.BoosterSpatula), -300f);
            Make(1, BoosterType.OrderUpBell, "Bell", UIBuilder.Purple, "purple", "icons/rd3d_icon_bell_v001", ArtCatalog.Pick(cfg.boosterBell, ArtCatalog.BoosterBell), 0f);
            Make(2, BoosterType.RewindCoaster, "Rewind", UIBuilder.Orange, "orange", "icons/rd3d_icon_coaster_v001", ArtCatalog.Pick(cfg.boosterRewind, ArtCatalog.BoosterRewind), 300f);
        }

        void Make(int i, BoosterType type, string label, Color color, string chassisColorName, string iconPath, Sprite legacy, float x)
        {
            var s = new Slot { type = type, faceColor = color };
            var holder = UIBuilder.Rect(root, "Booster_" + type);
            UIBuilder.Place(holder, new Vector2(0.5f, 0.5f), new Vector2(220f, 250f), new Vector2(x, 0f));

            var chassisSprite = ArtCatalog.Load("ui/rd3d_ui_booster_chassis_" + chassisColorName + "_v001");
            var toolIcon = ArtCatalog.Load(iconPath);

            // Shadow under the button.
            var shadow = UIBuilder.Panel(holder, "Shadow", new Color(0f, 0f, 0f, 0.35f), ProcSprites.Circle(128), false);
            UIBuilder.Place(shadow.rectTransform, new Vector2(0.5f, 1f), new Vector2(Size + 4f, Size + 4f), new Vector2(0f, -Size * 0.5f - 24f));

            if (chassisSprite != null)
            {
                s.button = UIBuilder.Button(holder, "Button", null, () => onUse?.Invoke(type), Color.white, chassisSprite, new Vector2(Size, Size));
                s.chassis = s.button.image;
                s.face = s.chassis;
            }
            else
            {
                s.button = UIBuilder.Button(holder, "Button", null, () => onUse?.Invoke(type), Style.Chrome, ProcSprites.Circle(128), new Vector2(Size, Size));
                s.chassis = s.button.image;
                s.chassis.color = Style.Chrome;
                var rimDark = UIBuilder.Panel(s.chassis.transform, "RimDark", new Color(0.35f, 0.4f, 0.45f, 0.9f), ProcSprites.Ring(128, 7f), false);
                UIBuilder.Stretch(rimDark.rectTransform, 0, 2, 0, -2);
                s.face = UIBuilder.Panel(s.chassis.transform, "Face", color, ProcSprites.Circle(128), false);
                UIBuilder.Stretch(s.face.rectTransform, 11, 11, 11, 11);
                var faceShade = UIBuilder.Panel(s.face.transform, "Shade", new Color(0f, 0f, 0f, 0.28f), ProcSprites.VerticalGradient(0f, 1f), false);
                UIBuilder.Stretch(faceShade.rectTransform); faceShade.sprite = ProcSprites.Circle(128); faceShade.color = new Color(0f, 0f, 0f, 0.0f);
                var shade2 = UIBuilder.Panel(s.face.transform, "Shade2", new Color(0f, 0f, 0f, 0.22f), ProcSprites.Glow(128, 0.7f), false);
                UIBuilder.Place(shade2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(Size * 1.1f, Size * 1.1f), new Vector2(10f, -14f));
                var highlight = UIBuilder.Panel(s.face.transform, "Highlight", new Color(1f, 1f, 1f, 0.42f), ProcSprites.Glow(128, 1.4f), false);
                UIBuilder.Place(highlight.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(Size * 0.8f, Size * 0.55f), new Vector2(-18f, 30f));
            }
            UIBuilder.Place(s.chassis.rectTransform, new Vector2(0.5f, 1f), new Vector2(Size, Size), new Vector2(0f, -Size * 0.5f - 18f));
            s.chassis.preserveAspect = true;

            // Tool icon (never covered by the badge).
            Sprite iconSprite = toolIcon != null ? toolIcon : legacy;
            s.icon = UIBuilder.Panel(s.chassis.transform, "Icon", iconSprite != null ? Color.white : Style.Cream, iconSprite != null ? iconSprite : ProcSprites.Circle(64), false);
            s.icon.preserveAspect = true;
            float iconSize = toolIcon != null ? Size * 0.66f : Size * 0.92f;   // legacy art already has a button baked in
            UIBuilder.Place(s.icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(iconSize, iconSize), new Vector2(0f, 2f));
            if (iconSprite == null)
            {
                var glyph = UIBuilder.Label(s.icon.transform, "Glyph", type == BoosterType.GoldenSpatula ? "S" : type == BoosterType.OrderUpBell ? "B" : "R", 56, Style.Ink);
                UIBuilder.Stretch(glyph.rectTransform);
            }

            // Round quantity badge, bottom-right, same spot on each.
            var badgeSprite = ArtCatalog.Load("ui/rd3d_ui_badge_round_red_v001");
            s.badge = UIBuilder.Panel(holder, "Badge", badgeSprite != null ? Color.white : Style.Cherry, badgeSprite != null ? badgeSprite : ProcSprites.Circle(64), false);
            UIBuilder.Place(s.badge.rectTransform, new Vector2(0.5f, 1f), new Vector2(54f, 54f), new Vector2(Size * 0.36f, -Size - 2f));
            if (badgeSprite == null) UIBuilder.Outline(s.badge, Style.ChromeLight, 3f);
            s.badgeText = UIBuilder.Label(s.badge.transform, "Count", "0", 30, Style.Cream);
            UIBuilder.Stretch(s.badgeText.rectTransform);

            s.lockOverlay = UIBuilder.Panel(holder, "Lock", new Color(0.1f, 0.1f, 0.12f, 0.7f), ProcSprites.Circle(128), false);
            UIBuilder.Place(s.lockOverlay.rectTransform, new Vector2(0.5f, 1f), new Vector2(Size, Size), new Vector2(0f, -Size * 0.5f - 18f));
            var lockLbl = UIBuilder.Label(s.lockOverlay.transform, "LockLbl", "LOCKED", 22, Style.Cream);
            UIBuilder.Stretch(lockLbl.rectTransform);

            s.label = UIBuilder.Label(holder, "Label", label, 28, Style.Cream);
            UIBuilder.Place(s.label.rectTransform, new Vector2(0.5f, 1f), new Vector2(220f, 40f), new Vector2(0f, -Size - 40f));
            var sh = s.label.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0f, 0f, 0f, 0.55f); sh.effectDistance = new Vector2(1.5f, -2f);
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
                // Zero inventory: desaturated face + grey badge; the button stays tappable (offer flow), nothing is consumed.
                bool empty = count <= 0;
                if (s.face != s.chassis) s.face.color = empty ? Color.Lerp(s.faceColor, new Color(0.45f, 0.45f, 0.48f), 0.75f) : s.faceColor;
                s.icon.color = empty ? new Color(0.75f, 0.75f, 0.75f, 0.85f) : Color.white;
                s.badge.color = empty ? new Color(0.5f, 0.5f, 0.52f) : (ArtCatalog.Load("ui/rd3d_ui_badge_round_red_v001") != null ? Color.white : Style.Cherry);
            }
        }

        public void SetInteractable(bool on)
        {
            foreach (var s in slots) if (s != null) s.button.interactable = on && !s.lockOverlay.gameObject.activeSelf;
        }

        public void Flash(BoosterType type)
        {
            foreach (var s in slots) if (s != null && s.type == type) UIBuilder.Punch(s.chassis.rectTransform, 0.2f, 0.3f);
        }

        public RectTransform Root => root;
    }
}
