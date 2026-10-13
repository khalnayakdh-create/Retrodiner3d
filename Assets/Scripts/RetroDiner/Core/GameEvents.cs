using System;
using RetroDiner.Belt;
using RetroDiner.Rules;

namespace RetroDiner.Core
{
    /// <summary>
    /// Single static event bus for cross-system communication.
    /// Views subscribe; rules/controllers raise. No MonoBehaviour dependency.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<BeltItem> ItemTapped;
        public static event Action<BeltItem> ItemPicked;
        public static event Action<string, bool> TripletCleared;      // itemId, anyCharred
        public static event Action<Ticket> TicketProgressed;
        public static event Action<Ticket> TicketCompleted;
        public static event Action TrayChanged;
        public static event Action NearMissStarted;
        public static event Action NearMissEnded;
        public static event Action<ShiftResult> LevelWon;
        public static event Action LevelLost;
        public static event Action<BoosterType> BoosterUsed;
        public static event Action ProfileChanged;
        public static event Action<string> Toast;

        public static void RaiseItemTapped(BeltItem item) => ItemTapped?.Invoke(item);
        public static void RaiseItemPicked(BeltItem item) => ItemPicked?.Invoke(item);
        public static void RaiseTripletCleared(string id, bool charred) => TripletCleared?.Invoke(id, charred);
        public static void RaiseTicketProgressed(Ticket t) => TicketProgressed?.Invoke(t);
        public static void RaiseTicketCompleted(Ticket t) => TicketCompleted?.Invoke(t);
        public static void RaiseTrayChanged() => TrayChanged?.Invoke();
        public static void RaiseNearMissStarted() => NearMissStarted?.Invoke();
        public static void RaiseNearMissEnded() => NearMissEnded?.Invoke();
        public static void RaiseLevelWon(ShiftResult r) => LevelWon?.Invoke(r);
        public static void RaiseLevelLost() => LevelLost?.Invoke();
        public static void RaiseBoosterUsed(BoosterType b) => BoosterUsed?.Invoke(b);
        public static void RaiseProfileChanged() => ProfileChanged?.Invoke();
        public static void RaiseToast(string msg) => Toast?.Invoke(msg);

        /// <summary>Drop every subscriber (used when rebuilding the gameplay scene).</summary>
        public static void ClearAll()
        {
            ItemTapped = null; ItemPicked = null; TripletCleared = null; TicketProgressed = null;
            TicketCompleted = null; TrayChanged = null; NearMissStarted = null; NearMissEnded = null;
            LevelWon = null; LevelLost = null; BoosterUsed = null; ProfileChanged = null; Toast = null;
        }
    }

    public enum BoosterType { GoldenSpatula, OrderUpBell, RewindCoaster, ExtraPlate }

    public struct ShiftResult
    {
        public int levelIndex;
        public int stars;
        public int coins;
        public int freeSlotsAtFinish;
        public bool charredMatched;
    }
}
