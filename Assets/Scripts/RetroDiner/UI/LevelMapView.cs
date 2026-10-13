using System;
using UnityEngine;
using UnityEngine.UI;
using RetroDiner.Core;
using RetroDiner.Data;

namespace RetroDiner.UI
{
    /// <summary>
    /// Scrollable "roadside map": level nodes on a serpentine path with dashed connectors, stars
    /// under cleared shifts and a pulsing halo on the current one. Scrolls to the current level.
    /// </summary>
    public class LevelMapView : MonoBehaviour
    {
        RectTransform root, content;
        ScrollRect scroll;
        GameConfig cfg;
        Action<int> onPlay;
        Sprite starFilled, starEmpty;

        const int Cols = 5;
        const float ColStep = 190f, RowStep = 200f, Node = 118f, TopPad = 80f, BottomPad = 120f;

        public void Build(Transform parent, GameConfig config, Action<int> play)
        {
            cfg = config; onPlay = play;
            starFilled = ArtCatalog.Pick(cfg.starFilled, ArtCatalog.StarFilled);
            starEmpty = ArtCatalog.Pick(cfg.starEmpty, ArtCatalog.StarEmpty);

            root = UIBuilder.Rect(parent, "LevelMap");
            UIBuilder.Stretch(root, 0f, 230f, 0f, 240f);
            var viewport = UIBuilder.Panel(root, "Viewport", new Color(1f, 1f, 1f, 0.01f), null, true);
            UIBuilder.Stretch(viewport.rectTransform);
            viewport.gameObject.AddComponent<RectMask2D>();
            content = UIBuilder.Rect(viewport.transform, "Content");
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content; scroll.viewport = viewport.rectTransform;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 90f;
            scroll.inertia = true;
        }

        Vector2 NodePos(int index)
        {
            int row = index / Cols, col = index % Cols;
            if (row % 2 == 1) col = Cols - 1 - col;              // serpentine
            float x = -ColStep * (Cols - 1) * 0.5f + col * ColStep;
            float y = -TopPad - row * RowStep - Node * 0.5f;
            return new Vector2(x, y);
        }

        public void Populate()
        {
            foreach (Transform c in content) Destroy(c.gameObject);
            var p = GameServices.Profile;
            int count = cfg.LevelCount;
            int rows = (count + Cols - 1) / Cols;
            content.sizeDelta = new Vector2(0f, TopPad + rows * RowStep + BottomPad);

            // Connectors first (under the nodes).
            for (int i = 0; i < count - 1; i++)
            {
                Vector2 a = NodePos(i), b = NodePos(i + 1);
                bool lit = i + 1 < p.highestLevelUnlocked;
                DrawDashes(a, b, lit ? new Color(1f, 0.96f, 0.86f, 0.95f) : new Color(0f, 0f, 0f, 0.35f));
            }

            for (int i = 0; i < count; i++)
            {
                int level = i + 1;
                bool unlocked = level <= p.highestLevelUnlocked;
                bool current = level == p.highestLevelUnlocked;
                int stars = p.GetStars(level);
                Vector2 pos = NodePos(i);

                var holder = UIBuilder.Rect(content, "L" + level);
                UIBuilder.Place(holder, new Vector2(0.5f, 1f), new Vector2(Node + 60f, Node + 70f), pos);

                if (current)
                {
                    var halo = UIBuilder.Panel(holder, "Halo", new Color(1f, 0.85f, 0.3f, 0.9f), ProcSprites.Ring(128, 12f), false);
                    UIBuilder.Place(halo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(Node + 34f, Node + 34f), new Vector2(0f, 20f));
                    UIBuilder.Pulse(halo.rectTransform, 0.08f, 1.1f);
                    var glow = UIBuilder.Panel(holder, "Glow", new Color(1f, 0.85f, 0.3f, 0.45f), ProcSprites.Glow(128, 1.5f), false);
                    UIBuilder.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(Node * 2.2f, Node * 2.2f), new Vector2(0f, 20f));
                    glow.transform.SetAsFirstSibling();
                }

                var shadow = UIBuilder.Panel(holder, "Shadow", new Color(0f, 0f, 0f, 0.35f), ProcSprites.Circle(128), false);
                UIBuilder.Place(shadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(Node, Node), new Vector2(0f, 12f));

                Color face = !unlocked ? new Color(0.38f, 0.36f, 0.38f) : stars > 0 ? UIBuilder.Mint : UIBuilder.Cream;
                int captured = level;
                var btn = UIBuilder.Button(holder, "Node", null, unlocked ? (Action)(() => onPlay(captured)) : null, face, ProcSprites.Circle(128), new Vector2(Node, Node));
                UIBuilder.Place(btn.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(Node, Node), new Vector2(0f, 20f));
                btn.image.preserveAspect = true;
                btn.interactable = unlocked;
                var rim = UIBuilder.Panel(btn.transform, "Rim", unlocked ? new Color(0.84f, 0.15f, 0.24f) : new Color(0.2f, 0.2f, 0.22f), ProcSprites.Ring(128, 9f), false);
                UIBuilder.Stretch(rim.rectTransform, -2, -2, -2, -2);
                var num = UIBuilder.Label(btn.transform, "Num", level.ToString(), 44, unlocked ? UIBuilder.Dark : new Color(0.7f, 0.7f, 0.72f));
                UIBuilder.Stretch(num.rectTransform);

                // Stars under the node.
                for (int k = 0; k < 3; k++)
                {
                    bool lit = k < stars;
                    var sp = lit ? starFilled : starEmpty;
                    var st = UIBuilder.Panel(holder, "Star" + k, sp != null ? (unlocked ? Color.white : new Color(1f, 1f, 1f, 0.35f)) : (lit ? UIBuilder.Gold : new Color(0f, 0f, 0f, 0.3f)), sp != null ? sp : ProcSprites.Circle(32), false);
                    st.preserveAspect = true;
                    UIBuilder.Place(st.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(34f, 34f), new Vector2((k - 1) * 34f, -52f + (k == 1 ? 6f : 0f)));
                }
            }
        }

        void DrawDashes(Vector2 a, Vector2 b, Color color)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            int dashes = Mathf.Max(2, Mathf.RoundToInt(len / 26f));
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            for (int i = 0; i < dashes; i++)
            {
                float t = (i + 0.5f) / dashes;
                Vector2 pos = a + d * t;
                var dash = UIBuilder.Pill(content, "Dash", color, 5);
                UIBuilder.Place(dash.rectTransform, new Vector2(0.5f, 1f), new Vector2(14f, 10f), pos + new Vector2(0f, 20f));
                dash.rectTransform.localRotation = Quaternion.Euler(0f, 0f, ang);
            }
        }

        /// <summary>Scroll so the current level sits around the middle of the viewport.</summary>
        public void FocusCurrent()
        {
            var p = GameServices.Profile;
            int idx = Mathf.Clamp(p.highestLevelUnlocked - 1, 0, cfg.LevelCount - 1);
            float y = -NodePos(idx).y;
            float viewH = scroll.viewport.rect.height;
            float maxY = Mathf.Max(0f, content.sizeDelta.y - viewH);
            content.anchoredPosition = new Vector2(0f, Mathf.Clamp(y - viewH * 0.45f, 0f, maxY));
        }
    }
}
