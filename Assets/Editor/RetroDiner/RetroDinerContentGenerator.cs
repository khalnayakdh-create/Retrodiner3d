using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using RetroDiner.Data;
using RetroDiner.Rules;

namespace RetroDiner.EditorTools
{
    /// <summary>
    /// Editor menu: copies the PNG pack into Resources, writes the ItemDefinition / LevelDefinition /
    /// GameConfig assets from ContentLibrary, generates + validates every spawn list, and exports JSON.
    /// </summary>
    public static class RetroDinerContentGenerator
    {
        const string ResRoot = "Assets/Resources/RetroDiner";
        const string ArtRoot = ResRoot + "/Art";
        const string ItemsRoot = ResRoot + "/Items";
        const string LevelsRoot = ResRoot + "/Levels";
        const string ConfigPath = ResRoot + "/GameConfig.asset";
        const string SourceArtFolder = "Chat Gpt Images";

        [MenuItem("Retro Diner/Setup Project (import art + generate content + validate)", false, 0)]
        public static void SetupAll()
        {
            ImportArt();
            GenerateContent();
            ValidateAllLevels();
            EditorUtility.DisplayDialog("Retro Diner", "Project setup complete.\nOpen Assets/Scenes/Boot.unity and press Play.", "OK");
        }

        // ------------------------------------------------------------------ art

        [MenuItem("Retro Diner/1. Import Art from 'Chat Gpt Images'", false, 20)]
        public static void ImportArt()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string src = Path.Combine(projectRoot, SourceArtFolder);
            if (!Directory.Exists(src))
            {
                Debug.LogWarning("[RetroDiner] Source art folder not found: " + src + "\nPlace the PNG pack at <project>/" + SourceArtFolder + " and run again. The game runs with placeholders until then.");
                return;
            }
            int copied = 0;
            foreach (var file in Directory.GetFiles(src, "*.png", SearchOption.AllDirectories))
            {
                string rel = file.Substring(src.Length).TrimStart('\\', '/').Replace('\\', '/');
                string dest = Path.Combine(projectRoot, ArtRoot, rel).Replace('\\', '/');
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                if (!File.Exists(dest) || File.GetLastWriteTimeUtc(file) > File.GetLastWriteTimeUtc(dest))
                {
                    File.Copy(file, dest, true);
                    copied++;
                }
            }
            // Fonts: the art pack drops TTFs in Assets/Fonts; UIBuilder loads them from Resources by name.
            string fontSrc = Path.Combine(Application.dataPath, "Fonts");
            string fontDst = Path.Combine(projectRoot, "Assets/Resources/RetroDiner/Fonts");
            int fonts = 0;
            if (Directory.Exists(fontSrc))
            {
                Directory.CreateDirectory(fontDst);
                foreach (var ttf in Directory.GetFiles(fontSrc, "*.ttf"))
                {
                    string dest = Path.Combine(fontDst, Path.GetFileName(ttf));
                    if (!File.Exists(dest) || File.GetLastWriteTimeUtc(ttf) > File.GetLastWriteTimeUtc(dest)) { File.Copy(ttf, dest, true); fonts++; }
                }
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[RetroDiner] Art import done. Copied " + copied + " PNGs into " + ArtRoot + (fonts > 0 ? " and " + fonts + " font(s) into Resources/RetroDiner/Fonts" : ""));
        }

        // ------------------------------------------------------------------ content

        static bool relinkPending;

        /// <summary>After a food texture is re-imported with a trimmed sprite rect its Sprite sub-asset changes
        /// identity, so item icons are re-resolved by path (debounced; levels are untouched).</summary>
        public static void RequestRelink()
        {
            if (relinkPending) return;
            relinkPending = true;
            EditorApplication.delayCall += () =>
            {
                relinkPending = false;
                ArtCatalog.ClearCache();
                int n = 0;
                foreach (var spec in ContentLibrary.Items)
                {
                    var existing = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemsRoot + "/Item_" + spec.id + ".asset");
                    if (existing == null) continue;
                    var fresh = ContentLibrary.CreateItem(spec);
                    if (existing.icon != fresh.icon || existing.charredIcon != fresh.charredIcon)
                    {
                        existing.icon = fresh.icon; existing.charredIcon = fresh.charredIcon;
                        EditorUtility.SetDirty(existing); n++;
                    }
                    Object.DestroyImmediate(fresh);
                }
                if (n > 0) { AssetDatabase.SaveAssets(); Debug.Log("[RetroDiner] Re-linked " + n + " item icon(s) after sprite trim."); }
            };
        }

        [MenuItem("Retro Diner/2. Generate Items + 50 Levels + GameConfig", false, 21)]
        public static void GenerateContent()
        {
            ArtCatalog.ClearCache();
            EnsureFolder("Assets/Resources");
            EnsureFolder(ResRoot);
            EnsureFolder(ItemsRoot);
            EnsureFolder(LevelsRoot);

            // Items
            var items = new List<ItemDefinition>();
            foreach (var spec in ContentLibrary.Items)
            {
                var fresh = ContentLibrary.CreateItem(spec);
                string path = ItemsRoot + "/Item_" + spec.id + ".asset";
                items.Add(WriteAsset(fresh, path));
            }

            // Levels (+ validated spawn lists)
            var levels = new List<LevelDefinition>();
            var sb = new StringBuilder("[RetroDiner] Level generation report\n");
            int rejected = 0;
            for (int i = 1; i <= ContentLibrary.LevelCount; i++)
            {
                var fresh = ContentLibrary.CreateLevel(i);
                ValidationReport report;
                var spawn = SpawnListGenerator.GenerateValidated(fresh, out report);
                fresh.spawnList = spawn.ToArray();
                if (!report.Accepted) rejected++;
                sb.AppendLine(report.ToString());
                string path = LevelsRoot + "/Level_" + i.ToString("000") + ".asset";
                levels.Add(WriteAsset(fresh, path));
                EditorUtility.DisplayProgressBar("Retro Diner", "Generating level " + i, i / (float)ContentLibrary.LevelCount);
            }
            EditorUtility.ClearProgressBar();

            // Config
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<GameConfig>();
                AssetDatabase.CreateAsset(cfg, ConfigPath);
            }
            cfg.items = items.ToArray();
            cfg.levels = levels.ToArray();
            var catalog = AssetDatabase.LoadAssetAtPath<StoreCatalog>(ResRoot + "/StoreCatalog.asset");
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<StoreCatalog>();
                catalog.offers = StoreCatalog.DefaultOffers();
                AssetDatabase.CreateAsset(catalog, ResRoot + "/StoreCatalog.asset");
            }
            var policy = AssetDatabase.LoadAssetAtPath<AdPolicy>(ResRoot + "/AdPolicy.asset");
            if (policy == null)
            {
                policy = ScriptableObject.CreateInstance<AdPolicy>();
                AssetDatabase.CreateAsset(policy, ResRoot + "/AdPolicy.asset");
            }
            cfg.storeCatalog = catalog;
            cfg.adPolicy = policy;
            AssignArt(cfg);
            EditorUtility.SetDirty(cfg);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ExportLevelsJson(levels);
            Debug.Log(sb.ToString() + (rejected == 0 ? "All levels accepted by LevelValidator." : rejected + " level(s) NOT accepted — see lines above."));
        }

        static T WriteAsset<T>(T fresh, string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(fresh, path);
                return fresh;
            }
            EditorUtility.CopySerialized(fresh, existing);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(fresh);
            return existing;
        }

        static void AssignArt(GameConfig cfg)
        {
            // Item icons by file name.
            foreach (var item in cfg.items)
            {
                if (item == null) continue;
                item.icon = ArtCatalog.Food(item.id, false);
                item.charredIcon = ArtCatalog.Food(item.id, true);
                EditorUtility.SetDirty(item);
            }
            cfg.gameplayBackground = ArtCatalog.GameplayBackground;
            cfg.mainMenuBackground = ArtCatalog.MainMenuBackground;
            cfg.levelSelectBackground = ArtCatalog.LevelSelectBackground;
            cfg.tray7 = ArtCatalog.Tray7; cfg.tray8 = ArtCatalog.Tray8;
            cfg.ticketNormal = ArtCatalog.TicketNormal; cfg.ticketActive = ArtCatalog.TicketActive; cfg.ticketComplete = ArtCatalog.TicketComplete;
            cfg.buttonRed = ArtCatalog.ButtonRed; cfg.buttonMint = ArtCatalog.ButtonMint; cfg.buttonGold = ArtCatalog.ButtonGold;
            cfg.popupPanel = ArtCatalog.PopupPanel; cfg.pauseButton = ArtCatalog.PauseButton; cfg.closeButton = ArtCatalog.CloseButton; cfg.confirmButton = ArtCatalog.ConfirmButton;
            cfg.starFilled = ArtCatalog.StarFilled; cfg.starEmpty = ArtCatalog.StarEmpty;
            cfg.coinIcon = ArtCatalog.Coin; cfg.tokenIcon = ArtCatalog.Token; cfg.heartIcon = ArtCatalog.Heart;
            cfg.tipJar10 = ArtCatalog.TipJar10; cfg.tipJar50 = ArtCatalog.TipJar50; cfg.tipJar100 = ArtCatalog.TipJar100;
            cfg.boosterSpatula = ArtCatalog.BoosterSpatula; cfg.boosterBell = ArtCatalog.BoosterBell; cfg.boosterRewind = ArtCatalog.BoosterRewind; cfg.boosterExtraPlate = ArtCatalog.BoosterExtraPlate;
            cfg.clocheClosed = ArtCatalog.ClocheClosed; cfg.clocheOpen = ArtCatalog.ClocheOpen; cfg.napkin = ArtCatalog.Napkin;
            cfg.syrupSpill = ArtCatalog.SyrupSpill; cfg.ketchupSpill = ArtCatalog.KetchupSpill;
            cfg.shiftFinishedBanner = ArtCatalog.ShiftFinishedBanner; cfg.confettiSheet = ArtCatalog.ConfettiSheet;
        }

        // ------------------------------------------------------------------ validation

        [MenuItem("Retro Diner/3. Validate All Levels", false, 22)]
        public static void ValidateAllLevels()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (cfg == null) { Debug.LogWarning("[RetroDiner] No GameConfig yet — run step 2 first."); return; }
            var sb = new StringBuilder("[RetroDiner] LevelValidator\n");
            int bad = 0;
            foreach (var lv in cfg.levels)
            {
                if (lv == null) continue;
                List<string> spawn = lv.spawnList != null && lv.spawnList.Length > 0 ? new List<string>(lv.spawnList) : SpawnListGenerator.Generate(lv, lv.seed);
                var r = LevelValidator.Validate(lv, spawn, 30, cfg.trayCapacity);
                if (!r.Accepted) bad++;
                sb.AppendLine(r.ToString());
            }
            if (bad > 0) Debug.LogWarning(sb.ToString() + bad + " level(s) rejected."); else Debug.Log(sb.ToString() + "All " + cfg.levels.Length + " levels pass.");
        }

        // ------------------------------------------------------------------ json

        static void ExportLevelsJson(List<LevelDefinition> levels)
        {
            string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "docs", "levels");
            Directory.CreateDirectory(dir);
            foreach (var lv in levels)
                File.WriteAllText(Path.Combine(dir, "level_" + lv.levelIndex.ToString("000") + ".json"), lv.ToJson());
            Debug.Log("[RetroDiner] Exported " + levels.Count + " level JSON files to docs/levels");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
