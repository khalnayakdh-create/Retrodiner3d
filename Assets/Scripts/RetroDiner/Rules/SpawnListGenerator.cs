using System.Collections.Generic;
using RetroDiner.Core;
using RetroDiner.Data;

namespace RetroDiner.Rules
{
    /// <summary>
    /// Deterministic spawn-order generation. Rules:
    ///  - every item type count is a multiple of 3 (built from triples);
    ///  - total = 3 * (ticket triples + filler triples);
    ///  - the first 9 items always contain at least one complete triple;
    ///  - the result is re-rolled (seed+1, seed+2, ...) until LevelValidator accepts it.
    /// </summary>
    public static class SpawnListGenerator
    {
        public const int MaxAttempts = 60;

        public static List<string> Generate(LevelDefinition level, int seed)
        {
            var rng = new SeededRandom(seed);
            var triples = new List<string>();
            if (level.tickets != null)
                foreach (var t in level.tickets)
                    for (int i = 0; i < t.count; i++) triples.Add(t.itemId);
            for (int i = 0; i < level.fillerTriples; i++)
                triples.Add(level.itemPool[rng.Next(level.itemPool.Length)]);

            var items = new List<string>(triples.Count * 3);
            foreach (var id in triples) { items.Add(id); items.Add(id); items.Add(id); }
            rng.Shuffle(items);
            EnsureEarlyTriple(items, 9);
            return items;
        }

        /// <summary>Generate and validate; returns the first spawn list the validator accepts (or the best attempt).</summary>
        public static List<string> GenerateValidated(LevelDefinition level, out ValidationReport report)
        {
            List<string> best = null;
            ValidationReport bestReport = null;
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var list = Generate(level, level.seed + attempt);
                var r = LevelValidator.Validate(level, list);
                if (bestReport == null || r.Score > bestReport.Score) { best = list; bestReport = r; }
                if (r.Accepted) break;
            }
            report = bestReport;
            return best;
        }

        static void EnsureEarlyTriple(List<string> items, int window)
        {
            if (items.Count < 3) return;
            window = System.Math.Min(window, items.Count);
            var counts = new Dictionary<string, int>();
            for (int i = 0; i < window; i++) { int c; counts.TryGetValue(items[i], out c); counts[items[i]] = c + 1; }
            string bestId = null; int bestCount = 0;
            foreach (var kv in counts) if (kv.Value > bestCount) { bestCount = kv.Value; bestId = kv.Key; }
            if (bestCount >= 3) return;

            // Pull extra copies of bestId from beyond the window into window slots of other ids.
            int need = 3 - bestCount;
            for (int j = window; j < items.Count && need > 0; j++)
            {
                if (items[j] != bestId) continue;
                for (int k = 0; k < window; k++)
                {
                    if (items[k] == bestId) continue;
                    string tmp = items[k]; items[k] = items[j]; items[j] = tmp;
                    need--;
                    break;
                }
            }
        }
    }
}
