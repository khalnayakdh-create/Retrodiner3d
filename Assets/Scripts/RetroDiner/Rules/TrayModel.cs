using System.Collections.Generic;

namespace RetroDiner.Rules
{
    public struct TrayEntry
    {
        public string id;
        public bool charred;
        public TrayEntry(string id, bool charred) { this.id = id; this.charred = charred; }
    }

    public struct InsertResult
    {
        public int slotIndex;
        public bool triplet;
        public string clearedId;
        public bool anyCharred;
        public int[] clearedIndices;   // indices in the tray BEFORE removal
    }

    /// <summary>
    /// The 7-slot (8 with Extra Plate) holding tray. Pure rules, no Unity dependency.
    /// Items of the same type are kept adjacent (inserted after the last same-type entry), and a
    /// triplet clears on the exact insert that completes it.
    /// </summary>
    public class TrayModel
    {
        readonly List<TrayEntry> slots = new List<TrayEntry>(8);

        public int Capacity { get; private set; }
        public IReadOnlyList<TrayEntry> Slots => slots;
        public int Count => slots.Count;
        public int FreeSlots => Capacity - slots.Count;
        public bool IsFull => slots.Count >= Capacity;

        public TrayModel(int capacity) { Capacity = capacity; }

        public void SetCapacity(int capacity)
        {
            Capacity = capacity < slots.Count ? slots.Count : capacity;
        }

        public int CountOf(string id)
        {
            int c = 0;
            for (int i = 0; i < slots.Count; i++) if (slots[i].id == id) c++;
            return c;
        }

        public bool HasTriplet()
        {
            for (int i = 0; i < slots.Count; i++) if (CountOf(slots[i].id) >= 3) return true;
            return false;
        }

        public InsertResult Insert(string id, bool charred)
        {
            int insertAt = slots.Count;
            for (int i = slots.Count - 1; i >= 0; i--)
                if (slots[i].id == id) { insertAt = i + 1; break; }
            slots.Insert(insertAt, new TrayEntry(id, charred));

            var result = new InsertResult { slotIndex = insertAt };
            if (CountOf(id) >= 3)
            {
                var idx = new List<int>(3);
                bool anyCharred = false;
                for (int i = 0; i < slots.Count && idx.Count < 3; i++)
                    if (slots[i].id == id) { idx.Add(i); anyCharred |= slots[i].charred; }
                for (int k = idx.Count - 1; k >= 0; k--) slots.RemoveAt(idx[k]);
                result.triplet = true;
                result.clearedId = id;
                result.anyCharred = anyCharred;
                result.clearedIndices = idx.ToArray();
            }
            return result;
        }

        /// <summary>
        /// Continue offer: remove up to <paramref name="count"/> items, singles first (they block the
        /// most space), then the rarest types. Returns removed entries.
        /// </summary>
        public List<TrayEntry> RemoveLeastUseful(int count)
        {
            var removed = new List<TrayEntry>();
            while (removed.Count < count && slots.Count > 0)
            {
                int bestIndex = 0, bestCount = int.MaxValue;
                for (int i = 0; i < slots.Count; i++)
                {
                    int c = CountOf(slots[i].id);
                    if (c < bestCount) { bestCount = c; bestIndex = i; }
                }
                removed.Add(slots[bestIndex]);
                slots.RemoveAt(bestIndex);
            }
            return removed;
        }

        public void Clear() { slots.Clear(); }
    }
}
