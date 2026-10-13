using System;
using System.Collections.Generic;
using UnityEngine;

namespace RetroDiner.Data
{
    public enum TrackShape { Ring, Teardrop, FigureEight }

    [Serializable]
    public class TicketSpec
    {
        public string itemId;
        [Tooltip("Number of triplets of this item the ticket needs.")]
        public int count = 1;
    }

    [Serializable]
    public class ObstacleSettings
    {
        [Range(0f, 1f)] public float clocheChance = 0f;
        [Range(0f, 1f)] public float napkinPairChance = 0f;
        public int spillSegments = 0;
        public bool burnEnabled = false;
        public int loopsToChar = 2;
    }

    [Serializable]
    public class StarThresholds
    {
        [Tooltip("Free tray slots required at finish for the 2nd star.")]
        public int freeSlotsForTwoStars = 3;
    }

    /// <summary>
    /// One shift. Serializable to JSON through <see cref="ToJson"/> / <see cref="FromJson"/> so the
    /// same data can later be shipped through Addressables or a remote config.
    /// </summary>
    [CreateAssetMenu(menuName = "Retro Diner/Level Definition", fileName = "Level_")]
    public class LevelDefinition : ScriptableObject
    {
        public int levelIndex = 1;
        public TrackShape trackShape = TrackShape.Ring;
        [Tooltip("Belt speed in world units per second.")]
        public float beltSpeed = 1f;
        public string[] itemPool = new string[0];
        public TicketSpec[] tickets = new TicketSpec[0];
        [Tooltip("Extra triplets drawn from the pool that no ticket needs.")]
        public int fillerTriples = 0;
        public int maxItemsOnBelt = 10;
        public ObstacleSettings obstacles = new ObstacleSettings();
        public StarThresholds stars = new StarThresholds();
        public int seed = 1;
        [Tooltip("Generated + validated spawn order. Empty = generate at runtime from the seed.")]
        public string[] spawnList = new string[0];

        public int TotalTicketTriples
        {
            get { int n = 0; if (tickets != null) foreach (var t in tickets) n += t.count; return n; }
        }

        public int TotalItems => (TotalTicketTriples + fillerTriples) * 3;

        // ---------- JSON ----------
        [Serializable]
        public class Dto
        {
            public int levelIndex; public int trackShape; public float beltSpeed; public string[] itemPool;
            public TicketSpec[] tickets; public int fillerTriples; public int maxItemsOnBelt;
            public ObstacleSettings obstacles; public StarThresholds stars; public int seed; public string[] spawnList;
        }

        public string ToJson()
        {
            var d = new Dto
            {
                levelIndex = levelIndex, trackShape = (int)trackShape, beltSpeed = beltSpeed, itemPool = itemPool,
                tickets = tickets, fillerTriples = fillerTriples, maxItemsOnBelt = maxItemsOnBelt, obstacles = obstacles,
                stars = stars, seed = seed, spawnList = spawnList
            };
            return JsonUtility.ToJson(d, true);
        }

        public void FromJson(string json)
        {
            var d = JsonUtility.FromJson<Dto>(json);
            levelIndex = d.levelIndex; trackShape = (TrackShape)d.trackShape; beltSpeed = d.beltSpeed; itemPool = d.itemPool;
            tickets = d.tickets; fillerTriples = d.fillerTriples; maxItemsOnBelt = d.maxItemsOnBelt; obstacles = d.obstacles;
            stars = d.stars; seed = d.seed; spawnList = d.spawnList;
        }
    }
}
