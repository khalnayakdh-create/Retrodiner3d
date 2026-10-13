using System.Collections.Generic;
using RetroDiner.Data;

namespace RetroDiner.Rules
{
    public class Ticket
    {
        public string itemId;
        public int total;
        public int remaining;
        public int railSlot = -1;          // which of the visible ticket cards shows it
        public bool IsComplete => remaining <= 0;
        public float Progress => total <= 0 ? 1f : 1f - (float)remaining / total;
    }

    /// <summary>Order Rail model: a queue of tickets with up to N active at once.</summary>
    public class TicketBoard
    {
        readonly Queue<Ticket> queue = new Queue<Ticket>();
        readonly List<Ticket> active = new List<Ticket>();
        readonly List<Ticket> completed = new List<Ticket>();
        readonly int maxActive;

        public IReadOnlyList<Ticket> Active => active;
        public IReadOnlyList<Ticket> Completed => completed;
        public int QueuedCount => queue.Count;
        public bool AllComplete => queue.Count == 0 && active.Count == 0;

        public TicketBoard(IEnumerable<TicketSpec> specs, int maxActive)
        {
            this.maxActive = maxActive;
            foreach (var s in specs)
            {
                if (s == null || s.count <= 0) continue;
                queue.Enqueue(new Ticket { itemId = s.itemId, total = s.count, remaining = s.count });
            }
            FillActive();
        }

        void FillActive()
        {
            while (active.Count < maxActive && queue.Count > 0)
            {
                var t = queue.Dequeue();
                t.railSlot = FirstFreeSlot();
                active.Add(t);
            }
        }

        int FirstFreeSlot()
        {
            for (int s = 0; s < maxActive; s++)
            {
                bool used = false;
                foreach (var t in active) if (t.railSlot == s) { used = true; break; }
                if (!used) return s;
            }
            return active.Count;
        }

        public bool HasActiveTicketFor(string itemId)
        {
            foreach (var t in active) if (t.itemId == itemId && !t.IsComplete) return true;
            return false;
        }

        public int RemainingItemsOnRail
        {
            get { int n = 0; foreach (var t in active) n += t.remaining; return n; }
        }

        public int RemainingItemsTotal
        {
            get { int n = RemainingItemsOnRail; foreach (var t in queue) n += t.remaining; return n; }
        }

        /// <summary>Credit one triplet of <paramref name="itemId"/>. Returns the ticket touched, or null (filler).</summary>
        public Ticket Credit(string itemId, out bool completedTicket)
        {
            completedTicket = false;
            for (int i = 0; i < active.Count; i++)
            {
                var t = active[i];
                if (t.itemId != itemId || t.IsComplete) continue;
                t.remaining--;
                if (t.IsComplete)
                {
                    completedTicket = true;
                    active.RemoveAt(i);
                    completed.Add(t);
                    FillActive();
                }
                return t;
            }
            return null;
        }

        /// <summary>The ticket that was queued into a rail slot most recently (for slide-in animation).</summary>
        public Ticket TicketInSlot(int slot)
        {
            foreach (var t in active) if (t.railSlot == slot) return t;
            return null;
        }
    }
}
