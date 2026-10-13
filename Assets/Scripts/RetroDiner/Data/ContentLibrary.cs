using System.Collections.Generic;
using UnityEngine;
using RetroDiner.Core;

namespace RetroDiner.Data
{
    /// <summary>
    /// Code-defined content: the 7 locked items and the 50-level difficulty curve.
    /// The Editor generator writes these out as ScriptableObject assets; at runtime, if the assets
    /// are missing, the same definitions are instantiated in memory so the game is always playable.
    /// </summary>
    public static class ContentLibrary
    {
        public const int LevelCount = 50;

        public struct ItemSpec
        {
            public string id, name; public Color color; public bool canBurn; public string sfx;
            public ItemSpec(string id, string name, Color color, bool canBurn, string sfx)
            { this.id = id; this.name = name; this.color = color; this.canBurn = canBurn; this.sfx = sfx; }
        }

        public static readonly ItemSpec[] Items =
        {
            new ItemSpec("pancakes",      "Pancake Stack",   new Color(0.96f, 0.69f, 0.23f), true,  "pickup_soft"),
            new ItemSpec("skillet_eggs",  "Skillet Eggs",    new Color(0.20f, 0.20f, 0.22f), true,  "pickup_clink"),
            new ItemSpec("coffee_mug",    "Coffee Mug",      new Color(0.84f, 0.15f, 0.24f), false, "pickup_thud"),
            new ItemSpec("milkshake",     "Milkshake",       new Color(0.98f, 0.72f, 0.80f), false, "pickup_chime"),
            new ItemSpec("bacon",         "Wavy Bacon",      new Color(0.70f, 0.25f, 0.18f), true,  "pickup_snap"),
            new ItemSpec("waffle",        "Belgian Waffle",  new Color(0.93f, 0.78f, 0.42f), false, "pickup_tap"),
            new ItemSpec("cinnamon_roll", "Cinnamon Roll",   new Color(0.85f, 0.60f, 0.38f), false, "pickup_plop"),
        };

        public static ItemDefinition CreateItem(ItemSpec spec)
        {
            var def = ScriptableObject.CreateInstance<ItemDefinition>();
            def.name = "Item_" + spec.id;
            def.id = spec.id;
            def.displayName = spec.name;
            def.placeholderColor = spec.color;
            def.canBurn = spec.canBurn;
            def.pickupSfxKey = spec.sfx;
            def.particleColor = spec.color;
            def.icon = ArtCatalog.Food(spec.id, false);
            def.charredIcon = ArtCatalog.Food(spec.id, true);
            return def;
        }

        /// <summary>Difficulty curve from the build prompt, section 15.</summary>
        public static LevelDefinition CreateLevel(int index)
        {
            var lv = ScriptableObject.CreateInstance<LevelDefinition>();
            lv.name = "Level_" + index.ToString("000");
            lv.levelIndex = index;
            lv.seed = 1000 + index * 7919;
            float t = Mathf.Clamp01((index - 1) / 49f);
            var rng = new SeededRandom(lv.seed);

            // Track shape rotates; teaching levels stay on the simple ring.
            lv.trackShape = index <= 3 ? TrackShape.Ring : (TrackShape)(index % 3);

            // Belt speed: 0.8 -> 1.0 by level 5, then up to 1.6x by level 50.
            lv.beltSpeed = index <= 5 ? Mathf.Lerp(0.5f, 1.0f, (index - 1) / 4f) : Mathf.Lerp(1.0f, 1.6f, (index - 5) / 45f);

            // Type count: 1,2,3 teach; 3 until 10; 4 until 20; 5 until 35; 6 after.
            int typeCount = index == 1 ? 1 : index == 2 ? 2 : index <= 10 ? 3 : index <= 20 ? 4 : index <= 35 ? 5 : 6;
            var ids = new List<string>();
            foreach (var it in Items) ids.Add(it.id);
            if (index > 3) rng.Shuffle(ids); else { /* teaching levels use pancakes, eggs, coffee in order */ }
            lv.itemPool = ids.GetRange(0, typeCount).ToArray();

            // Ticket triples: 1,2,3 for teach levels then grow to 16.
            int ticketTriples = index <= 3 ? index : Mathf.RoundToInt(Mathf.Lerp(3f, 16f, t));
            int ticketCount = index <= 3 ? index : Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(3f, 6f, t)), 1, typeCount + 2);
            var tickets = new List<TicketSpec>();
            for (int i = 0; i < ticketCount; i++)
                tickets.Add(new TicketSpec { itemId = lv.itemPool[i % lv.itemPool.Length], count = 0 });
            for (int i = 0; i < ticketTriples; i++) tickets[i % ticketCount].count++;
            // Later levels: shuffle ticket order so the first ticket isn't always the first pool item.
            if (index > 3) rng.Shuffle(tickets);
            lv.tickets = tickets.ToArray();

            lv.fillerTriples = index <= 3 ? 0 : Mathf.RoundToInt(ticketTriples * 0.5f) + index / 10;
            lv.maxItemsOnBelt = index <= 3 ? 9 : index <= 10 ? 10 : index <= 30 ? 12 : 14;

            // Obstacles per the curve.
            lv.obstacles = new ObstacleSettings
            {
                clocheChance = index >= 8 ? Mathf.Lerp(0.12f, 0.30f, t) : 0f,
                napkinPairChance = index >= 11 ? Mathf.Lerp(0.08f, 0.18f, t) : 0f,
                burnEnabled = index >= 15,
                loopsToChar = index >= 26 ? 2 : 3,
                spillSegments = index >= 35 ? 2 : index >= 20 ? 1 : 0,
            };
            lv.stars = new StarThresholds { freeSlotsForTwoStars = 3 };
            return lv;
        }

        public static GameConfig CreateDefaultConfig()
        {
            var cfg = ScriptableObject.CreateInstance<GameConfig>();
            cfg.name = "GameConfig (runtime default)";
            var items = new List<ItemDefinition>();
            foreach (var spec in Items) items.Add(CreateItem(spec));
            cfg.items = items.ToArray();
            var levels = new List<LevelDefinition>();
            for (int i = 1; i <= LevelCount; i++) levels.Add(CreateLevel(i));
            cfg.levels = levels.ToArray();
            return cfg;
        }
    }
}
