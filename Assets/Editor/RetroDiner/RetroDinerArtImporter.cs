using UnityEditor;
using UnityEngine;

namespace RetroDiner.EditorTools
{
    /// <summary>
    /// Every PNG under Assets/Resources/RetroDiner/Art is imported as an sRGB sprite with straight
    /// alpha, no mipmaps, centred pivot. The 4x4 FX sheet is sliced into 16 cells (FX_SPRITE_ATLAS.json).
    /// </summary>
    public class RetroDinerArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            string p = assetPath.Replace('\\', '/');
            if (!p.Contains("/Resources/RetroDiner/Art/")) return;
            var imp = (TextureImporter)assetImporter;
            bool sheet = p.Contains("sheet_4x4");
            bool background = p.Contains("/backgrounds/");

            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = sheet ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 1024f;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = !background;
            imp.sRGBTexture = true;
            imp.maxTextureSize = background ? 2048 : (p.Contains("/food/") || p.Contains("/obstacles/") || p.Contains("/boosters/") || p.Contains("/currency/") ? 512 : 1024);
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.spritePivot = new Vector2(0.5f, 0.5f);
            imp.wrapMode = TextureWrapMode.Clamp;

            // Food/obstacle art that was trimmed by OnPostprocessTexture keeps its single tight rect.
            if (!sheet && imp.userData != null && imp.userData.StartsWith("trim:"))
            {
                var parts = imp.userData.Substring(5).Split(',');
                if (parts.Length == 4)
                {
#pragma warning disable 618
                    imp.spriteImportMode = SpriteImportMode.Multiple;
                    imp.spritesheet = new[] { new SpriteMetaData
                    {
                        name = System.IO.Path.GetFileNameWithoutExtension(p),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                        rect = new Rect(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]))
                    } };
#pragma warning restore 618
                }
            }

            if (sheet)
            {
#pragma warning disable 618
                var metas = new SpriteMetaData[16];
                string[] names = {
                    "gold_star", "gold_four_point_sparkle", "red_confetti_square", "red_confetti_ribbon",
                    "mint_confetti_square", "mint_confetti_ribbon", "syrup_droplet", "syrup_splash",
                    "steam_puff_small", "steam_puff_large", "tiny_bell", "sugar_sparkle",
                    "cream_confetti", "gold_coin_glint", "soft_glow", "star_burst" };
                for (int i = 0; i < 16; i++)
                {
                    int col = i % 4, row = i / 4;
                    metas[i] = new SpriteMetaData
                    {
                        name = names[i],
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                        // Unity rects are bottom-left origin; the atlas JSON is top-left origin.
                        rect = new Rect(col * 256, (3 - row) * 256, 256, 256)
                    };
                }
                imp.spritesheet = metas;
#pragma warning restore 618
            }
        }

        /// <summary>Food is the hero: trim empty margins so every dish fills its tray well / ticket icon at the
        /// same visual size. Runs once per texture (marker in userData), then re-imports with the tight rect.</summary>
        void OnPostprocessTexture(Texture2D tex)
        {
            string p = assetPath.Replace('\\', '/');
            if (!p.Contains("/Resources/RetroDiner/Art/")) return;
            if (!(p.Contains("/food/") || p.Contains("/obstacles/"))) return;
            var imp = (TextureImporter)assetImporter;
            if (imp.userData != null && imp.userData.StartsWith("trim:")) return;
            // Sprite rects are in SOURCE pixels, so measure the PNG itself, not the (possibly downsized) import.
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try { if (!src.LoadImage(System.IO.File.ReadAllBytes(assetPath))) return; } catch { return; }
            int w = src.width, h = src.height;
            Color32[] px = src.GetPixels32();
            Object.DestroyImmediate(src);
            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (px[y * w + x].a > 12)
                    {
                        if (x < minX) minX = x; if (x > maxX) maxX = x;
                        if (y < minY) minY = y; if (y > maxY) maxY = y;
                    }
            if (maxX < 0) return;
            const int pad = 8;
            minX = Mathf.Max(0, minX - pad); minY = Mathf.Max(0, minY - pad);
            maxX = Mathf.Min(w - 1, maxX + pad); maxY = Mathf.Min(h - 1, maxY + pad);
            int tw = maxX - minX + 1, th = maxY - minY + 1;
            int side = Mathf.Max(tw, th);
            int cx = minX + tw / 2, cy = minY + th / 2;
            int rx = Mathf.Clamp(cx - side / 2, 0, Mathf.Max(0, w - side));
            int ry = Mathf.Clamp(cy - side / 2, 0, Mathf.Max(0, h - side));
            side = Mathf.Min(side, Mathf.Min(w - rx, h - ry));
            string marker = side >= Mathf.Min(w, h) - 2 * pad ? "trim:full" : "trim:" + rx + "," + ry + "," + side + "," + side;
            string path = assetPath;
            EditorApplication.delayCall += () =>
            {
                var later = AssetImporter.GetAtPath(path) as TextureImporter;
                if (later == null || (later.userData != null && later.userData.StartsWith("trim:"))) return;
                later.userData = marker;
                later.SaveAndReimport();
                RetroDinerContentGenerator.RequestRelink();
            };
        }
    }
}
