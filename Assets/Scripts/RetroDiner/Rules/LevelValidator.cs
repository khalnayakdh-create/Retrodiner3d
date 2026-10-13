using System.Collections.Generic;
using System.Text;
using RetroDiner.Core;
using RetroDiner.Data;

namespace RetroDiner.Rules
{
    public class ValidationReport
    {
        public int levelIndex;
        public bool greedySolvable;
        public int greedyPeakTray;
        public float randomWinRate;
        public int totalItems;
        public bool countsAreTriples;
        public bool Accepted => greedySolvable && countsAreTriples && (levelIndex <= 6 || randomWinRate < 0.5f);
        public float Score => (greedySolvable ? 10f : 0f) + (countsAreTriples ? 5f : 0f) + (levelIndex <= 6 ? 0f : (1f - randomWinRate));

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("L").Append(levelIndex).Append(": ");
            sb.Append(Accepted ? "OK" : "REJECT");
            sb.Append(" items=").Append(totalItems);
            sb.Append(" greedy=").Append(greedySolvable ? "win" : "FAIL").Append(" (peak tray ").Append(greedyPeakTray).Append(")");
            sb.Append(" randomWin=").Append((randomWinRate * 100f).ToString("0")).Append("%");
            if (!countsAreTriples) sb.Append(" COUNTS-NOT-MULTIPLE-OF-3");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Simulates players against a spawn list. The belt shows at most <c>maxItemsOnBelt</c> items;
    /// the rest queue and spawn as items are picked. A "greedy" player (picks what completes or
    /// builds the biggest group) must win; a random tapper should usually lose past level 6.
    /// Obstacles are ignored here (they only delay picks, never change solvability).
    /// </summary>
    public static class LevelValidator
    {
        public static ValidationReport Validate(LevelDefinition level, List<string> spawn, int randomRuns = 20, int trayCapacity = 7)
        {
            var r = new ValidationReport { levelIndex = level.levelIndex, totalItems = spawn.Count };
            var counts = new Dictionary<string, int>();
            foreach (var id in spawn) { int c; counts.TryGetValue(id, out c); counts[id] = c + 1; }
            r.countsAreTriples = true;
            foreach (var kv in counts) if (kv.Value % 3 != 0) r.countsAreTriples = false;

            int peak;
            r.greedySolvable = SimulateGreedy(spawn, level.maxItemsOnBelt, trayCapacity, out peak);
            r.greedyPeakTray = peak;

            int wins = 0;
            for (int i = 0; i < randomRuns; i++) if (SimulateRandom(spawn, level.maxItemsOnBelt, trayCapacity, level.seed * 31 + i)) wins++;
            r.randomWinRate = randomRuns == 0 ? 0f : wins / (float)randomRuns;
            return r;
        }

        public static bool SimulateGreedy(List<string> spawn, int window, int capacity, out int peakTray)
        {
            var belt = new List<string>();
            var queue = new Queue<string>(spawn);
            var tray = new TrayModel(capacity);
            peakTray = 0;
            while (belt.Count < window && queue.Count > 0) belt.Add(queue.Dequeue());

            int guard = spawn.Count * 2 + 10;
            while ((belt.Count > 0 || queue.Count > 0) && guard-- > 0)
            {
                string pick = ChooseGreedy(belt, tray);
                if (pick == null) break;
                belt.Remove(pick);
                var res = tray.Insert(pick, false);
                if (tray.Count > peakTray) peakTray = tray.Count;
                if (!res.triplet && tray.IsFull && !tray.HasTriplet()) return false;
                if (queue.Count > 0) belt.Add(queue.Dequeue());
            }
            return belt.Count == 0 && queue.Count == 0 && tray.Count == 0;
        }

        static string ChooseGreedy(List<string> belt, TrayModel tray)
        {
            if (belt.Count == 0) return null;
            // Priority: completes a triple now > builds the group with the most copies (tray + visible),
            // but never start a new type when the tray would be left without a way to finish it.
            string best = null; int bestScore = int.MinValue;
            foreach (var id in belt)
            {
                int inTray = tray.CountOf(id);
                int visible = 0; foreach (var b in belt) if (b == id) visible++;
                int score;
                if (inTray == 2) score = 1000;
                else if (inTray + visible >= 3) score = 100 + inTray * 10 + visible;
                else score = inTray * 10 + visible - 50;   // would open a new pending type with no visible finish
                if (score > bestScore) { bestScore = score; best = id; }
            }
            return best;
        }

        public static bool SimulateRandom(List<string> spawn, int window, int capacity, int seed)
        {
            var rng = new SeededRandom(seed);
            var belt = new List<string>();
            var queue = new Queue<string>(spawn);
            var tray = new TrayModel(capacity);
            while (belt.Count < window && queue.Count > 0) belt.Add(queue.Dequeue());
            int guard = spawn.Count * 2 + 10;
            while (belt.Count > 0 && guard-- > 0)
            {
                string pick = belt[rng.Next(belt.Count)];
                belt.Remove(pick);
                var res = tray.Insert(pick, false);
                if (!res.triplet && tray.IsFull && !tray.HasTriplet()) return false;
                if (queue.Count > 0) belt.Add(queue.Dequeue());
            }
            return belt.Count == 0 && queue.Count == 0 && tray.Count == 0;
        }
    }
}
