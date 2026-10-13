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
    }
}
