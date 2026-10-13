using System.Collections.Generic;
using UnityEngine;

namespace RetroDiner.UI
{
    /// <summary>
    /// Procedural sprites (rounded rects, circles, soft glows, rings) generated once at runtime so the
    /// UI gets pills, cards and halos without extra art. All are white; tint with Image.color.
    /// </summary>
    public static class ProcSprites
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// <summary>9-sliced rounded rectangle. Use with Image.type = Sliced.</summary>
        public static Sprite Rounded(int radius = 24)
        {
            string key = "round" + radius;
            Sprite s;
            if (cache.TryGetValue(key, out s)) return s;
            int size = radius * 2 + 8;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius)));
                    float dy = Mathf.Max(0f, Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius)));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels(px); tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            cache[key] = s;
            return s;
        }

        /// <summary>Anti-aliased filled circle.</summary>
        public static Sprite Circle(int size = 128)
        {
            string key = "circle" + size;
            Sprite s;
            if (cache.TryGetValue(key, out s)) return s;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            float r = size * 0.5f - 1f, c = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                    px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d + 0.5f));
                }
            tex.SetPixels(px); tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            cache[key] = s;
            return s;
        }

        /// <summary>Radial soft glow (alpha falls off with a smooth curve). Also used as a blob shadow.</summary>
        public static Sprite Glow(int size = 128, float power = 2f)
        {
            string key = "glow" + size + "_" + power;
            Sprite s;
            if (cache.TryGetValue(key, out s)) return s;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            float c = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c)) / c;
                    float a = Mathf.Pow(Mathf.Clamp01(1f - d), power);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels(px); tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            cache[key] = s;
            return s;
        }

        /// <summary>Ring (stroke) — for selection halos and the pulsing "current level" marker.</summary>
        public static Sprite Ring(int size = 128, float thickness = 10f)
        {
            string key = "ring" + size + "_" + thickness;
            Sprite s;
            if (cache.TryGetValue(key, out s)) return s;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            float r = size * 0.5f - 1f, c = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                    float outer = Mathf.Clamp01(r - d + 0.5f);
                    float inner = Mathf.Clamp01(r - thickness - d + 0.5f);
                    px[y * size + x] = new Color(1f, 1f, 1f, outer - inner);
                }
            tex.SetPixels(px); tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            cache[key] = s;
            return s;
        }

        /// <summary>Red-and-white gingham tile (modal headers). Tiles seamlessly.</summary>
        public static Sprite Gingham(int cell = 24, bool tileable = true)
        {
            string key = "gingham" + cell;
            Sprite s;
            if (cache.TryGetValue(key, out s)) return s;
            int size = cell * 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat };
            var px = new Color[size * size];
            Color red = new Color(0.86f, 0.2f, 0.27f), cream = new Color(1f, 0.94f, 0.81f), mix = new Color(0.93f, 0.57f, 0.54f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool cx = (x / cell) % 2 == 0, cy = (y / cell) % 2 == 0;
                    px[y * size + x] = cx && cy ? red : (cx || cy) ? mix : cream;
                }
            tex.SetPixels(px); tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            cache[key] = s;
            return s;
        }

        /// <summary>Horizontal chrome sheen: light-dark-light bands, for rims and plates.</summary>
        public static Sprite Sheen(int height = 64)
        {
            string key = "sheen" + height;
            Sprite s;
            if (cache.TryGetValue(key, out s)) return s;
            var tex = new Texture2D(4, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[4 * height];
            for (int y = 0; y < height; y++)
            {
                float t = y / (float)(height - 1);
                float v = 0.55f + 0.45f * Mathf.Pow(Mathf.Sin(t * Mathf.PI), 0.6f) * (t > 0.5f ? 1f : 0.8f) + (t > 0.78f ? 0.15f : 0f);
                v = Mathf.Clamp01(v);
                for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color(v, v, v * 1.03f, 1f);
            }
            tex.SetPixels(px); tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, 4, height), new Vector2(0.5f, 0.5f), 100f);
            cache[key] = s;
            return s;
        }

        /// <summary>Vertical gradient strip (top alpha → bottom alpha), for vignettes and scrims.</summary>
        public static Sprite VerticalGradient(float topAlpha, float bottomAlpha, int height = 64)
        {
            string key = "vgrad" + topAlpha + "_" + bottomAlpha + "_" + height;
            Sprite s;
            if (cache.TryGetValue(key, out s)) return s;
            var tex = new Texture2D(4, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[4 * height];
            for (int y = 0; y < height; y++)
            {
                float t = y / (float)(height - 1);
                float a = Mathf.Lerp(bottomAlpha, topAlpha, t);
                for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px); tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, 4, height), new Vector2(0.5f, 0.5f), 100f);
            cache[key] = s;
            return s;
        }
    }
}
