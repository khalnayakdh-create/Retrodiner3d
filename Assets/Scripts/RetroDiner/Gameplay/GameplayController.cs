using System.Collections.Generic;
using UnityEngine;
using RetroDiner.Belt;
using RetroDiner.Core;
using RetroDiner.Data;
using RetroDiner.Rules;
using RetroDiner.UI;

namespace RetroDiner.Gameplay
{
    public enum ShiftState { None, Playing, NearMiss, Paused, Continue, Won, Lost }

    /// <summary>
    /// Orchestrates one shift: wires rules (TrayModel, TicketBoard) to views (HUD, belt), handles
    /// taps, boosters, near-miss / continue, win and lose. Rules stay in plain C#; this class only
    /// sequences them and drives animation callbacks.
    /// </summary>
    public class GameplayController : MonoBehaviour
    {
        public ShiftState State { get; private set; } = ShiftState.None;
        public LevelDefinition Level => level;

        GameConfig cfg;
        Camera cam;
        HudView hud;
        BeltController belt;
        ItemFactory factory;
        MainMenuView menu;
        Vfx vfx;
        TapInput input;
        CameraRig rig;
        ReplayRecorder replay;
        bool closeCall;

        LevelDefinition level;
        TrayModel tray;
        TicketBoard tickets;
        readonly Dictionary<string, ItemDefinition> defs = new Dictionary<string, ItemDefinition>();

        int lastLevelIndex = -1;
        int failsThisLevel;
        bool charredMatched;
        float nearMissTimer;
        int ticketItemsTotal, ticketItemsDone;
        TweenHandle timeScaleTween;

        public void Setup(GameConfig config, Camera beltCamera, CameraRig cameraRig, HudView hudView, BeltController beltController,
            ItemFactory itemFactory, MainMenuView mainMenu, Vfx effects, TapInput tapInput, ReplayRecorder replayRecorder)
        {
            cfg = config; cam = beltCamera; rig = cameraRig; hud = hudView; belt = beltController; factory = itemFactory;
            menu = mainMenu; vfx = effects; input = tapInput; replay = replayRecorder;
            defs.Clear();
            foreach (var d in cfg.items) if (d != null && !string.IsNullOrEmpty(d.id)) defs[d.id] = d;
            belt.ItemCharred += OnItemCharred;
        }

        public ItemDefinition Lookup(string id)
        {
            ItemDefinition d;
            return defs.TryGetValue(id, out d) ? d : null;
        }

        void OnEnable() { GameEvents.ItemTapped += OnItemTapped; }
        void OnDisable() { GameEvents.ItemTapped -= OnItemTapped; }

        // ------------------------------------------------------------------ level lifecycle

        public void StartLevel(int index)
        {
            if (cfg.GetLevel(index) == null) { GameEvents.RaiseToast("No level " + index); return; }
            Popups.Fade(() => StartLevelNow(index));
        }

        void StartLevelNow(int index)
        {
            level = cfg.GetLevel(index);
            if (level == null) return;
            if (index != lastLevelIndex) { failsThisLevel = 0; lastLevelIndex = index; }

            KillTimeScaleTween();
            Time.timeScale = 1f;
            hud.ClosePanel();
            menu.Hide();
            hud.Root.gameObject.SetActive(true);
            input.Enabled = true;

            GrantBoosterIntros(index);

            tray = new TrayModel(cfg.trayCapacity);
            tickets = new TicketBoard(level.tickets, cfg.ticketsVisible);
            ticketItemsTotal = tickets.RemainingItemsTotal;
            ticketItemsDone = 0;
            charredMatched = false;

            List<string> spawn;
            if (level.spawnList != null && level.spawnList.Length > 0) spawn = new List<string>(level.spawnList);
            else
            {
                ValidationReport report;
                spawn = SpawnListGenerator.GenerateValidated(level, out report);
                Debug.Log("[Level] Runtime-generated spawn list. " + report);
            }

            belt.Setup(cfg, level, spawn, factory, Lookup);
            rig.Frame(belt.Track.GetBounds());

            hud.SetLevel(index);
            hud.Tray.SetCapacity(cfg.trayCapacity);
            hud.Tray.Refresh(tray);
            hud.Tickets.Refresh(tickets);
            hud.Boosters.Refresh(index);
            hud.Boosters.SetInteractable(true);
            hud.SetProgress(0f);
            hud.SetTips(GameServices.Profile.tipJarTokens);
            hud.SetNearMiss(false);
            State = ShiftState.Playing;
            if (replay != null) replay.Clear();
            comboCount = 0;
            closeCall = false;

            // Intro banner, then the pre-shift Extra Plate pick, then the belt rolls.
            belt.Running = false;
            input.Enabled = false;
            hud.LevelIntro(index, level.tickets != null ? level.tickets.Length : 0, () =>
            {
                if (State != ShiftState.Playing) return;
                belt.Running = true;
                input.Enabled = true;
                OfferPreShiftExtraPlate();
            });
        }

        /// <summary>Pre-shift pick: spend an owned Extra Plate for an 8th slot this shift (level 7+).</summary>
        void OfferPreShiftExtraPlate()
        {
            var p = GameServices.Profile;
            if (level.levelIndex < cfg.rewindUnlockLevel || p.extraPlate <= 0) return;
            belt.Running = false;
            input.Enabled = false;
            int owned = p.extraPlate;
            Popups.Show("EXTRA PLATE?", "Start this shift with an 8th tray slot.",
                new List<PanelButton>
                {
                    new PanelButton("USE 1", () =>
                    {
                        p.AddBooster(BoosterType.ExtraPlate, -1);
                        GameServices.SaveProfile();
                        tray.SetCapacity(cfg.trayCapacity + 1);
                        hud.Tray.SetCapacity(tray.Capacity);
                        hud.Tray.Refresh(tray);
                        ResumeAfterPreShift();
                    }, Style.Teal),
                    new PanelButton("NOT NOW", ResumeAfterPreShift, Style.Cherry) { secondary = true },
                }, true, content => DecorateExtraPlate(content, owned), 300f);
        }

        void ResumeAfterPreShift()
        {
            Popups.Close();
            if (State != ShiftState.Playing) return;
            belt.Running = true;
            input.Enabled = true;
        }

        /// <summary>Extra Plate modal content: plate-stack illustration (ui/rd3d_illo_extra_plate_stack_v001 or
        /// procedural plates), a 7 -> 8 slot diagram with the 8th slot highlighted, and the owned count line.</summary>
        void DecorateExtraPlate(RectTransform content, int owned)
        {
            var illo = ArtCatalog.Load("ui/rd3d_illo_extra_plate_stack_v001");
            var illoHolder = UIBuilder.Rect(content, "Illo");
            UIBuilder.Place(illoHolder, new Vector2(0.5f, 1f), new Vector2(300f, 150f), new Vector2(0f, -78f));
            if (illo != null)
            {
                var img = UIBuilder.Panel(illoHolder, "Plates", Color.white, illo, false);
                img.preserveAspect = true;
                UIBuilder.Stretch(img.rectTransform);
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    var plate = UIBuilder.Panel(illoHolder, "Plate" + i, Style.Cream, ProcSprites.Circle(128), false);
                    UIBuilder.Place(plate.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(190f, 58f), new Vector2(0f, -40f + i * 26f));
                    UIBuilder.Outline(plate, Style.ChromeShadow, 2f);
                    var rim = UIBuilder.Panel(plate.transform, "Rim", Style.Cherry, ProcSprites.Ring(128, 6f), false);
                    UIBuilder.Stretch(rim.rectTransform, 14, 4, 14, 4);
                }
                var shine = UIBuilder.Panel(illoHolder, "Shine", new Color(1f, 1f, 1f, 0.55f), ProcSprites.Glow(64, 1.2f), false);
                UIBuilder.Place(shine.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(90f, 40f), new Vector2(-40f, 24f));
            }

            // 7 -> 8 slot diagram: eight small wells, the eighth gold with a "+1" tag.
            var diagram = UIBuilder.Rect(content, "Diagram");
            UIBuilder.Place(diagram, new Vector2(0.5f, 1f), new Vector2(760f, 70f), new Vector2(0f, -200f));
            const float w = 74f, gap = 10f;
            float startX = -(8 * w + 7 * gap) * 0.5f + w * 0.5f;
            for (int i = 0; i < 8; i++)
            {
                bool extra = i == 7;
                var well = UIBuilder.Pill(diagram, "Well" + i, extra ? Style.Gold : new Color(0.86f, 0.82f, 0.74f), 14);
                UIBuilder.Place(well.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(w, 60f), new Vector2(startX + i * (w + gap), 0f));
                UIBuilder.Outline(well, extra ? Style.Cherry : Style.ChromeShadow, extra ? 3f : 1.5f);
                if (extra)
                {
                    var tag = UIBuilder.Label(well.transform, "Plus", "+1", 28, Style.Ink);
                    UIBuilder.Stretch(tag.rectTransform);
                    if (!Style.ReducedMotion) UIBuilder.Pulse(well.rectTransform, 0.06f, 0.9f);
                }
            }

            var count = UIBuilder.Label(content, "Owned", "You have " + owned + " Extra Plate" + (owned == 1 ? "" : "s") + ".", 30, Style.Ink, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIBuilder.Place(count.rectTransform, new Vector2(0.5f, 1f), new Vector2(800f, 44f), new Vector2(0f, -268f));
        }

        void GrantBoosterIntros(int index)
        {
            var p = GameServices.Profile;
            bool changed = false;
            if (index >= cfg.spatulaUnlockLevel && !p.introSpatula) { p.introSpatula = true; p.AddBooster(BoosterType.GoldenSpatula, cfg.boostersGrantedOnUnlock); GameEvents.RaiseToast("New booster: Golden Spatula!"); changed = true; }
            if (index >= cfg.bellUnlockLevel && !p.introBell) { p.introBell = true; p.AddBooster(BoosterType.OrderUpBell, cfg.boostersGrantedOnUnlock); GameEvents.RaiseToast("New booster: Order Up! Bell"); changed = true; }
            if (index >= cfg.rewindUnlockLevel && !p.introRewind) { p.introRewind = true; p.AddBooster(BoosterType.RewindCoaster, cfg.boostersGrantedOnUnlock); GameEvents.RaiseToast("New booster: Rewind Coaster"); changed = true; }
            if (changed) GameServices.SaveProfile();
        }

        public void Restart() { if (level != null) StartLevel(level.levelIndex); }

        public void NextLevel()
        {
            int next = level != null ? level.levelIndex + 1 : 1;
            if (cfg.GetLevel(next) == null) { ToMenu(); GameEvents.RaiseToast("More shifts coming soon!"); return; }
            StartLevel(next);
        }

        public void ToMenu()
        {
            KillTimeScaleTween();
            Time.timeScale = 1f;
            State = ShiftState.None;
            hud.ClosePanel();
            Popups.Fade(() =>
            {
                belt.ClearLevel();
                hud.Root.gameObject.SetActive(false);
                menu.ShowMenu();
            });
        }

        void Update()
        {
            if (State == ShiftState.NearMiss)
            {
                nearMissTimer -= Time.unscaledDeltaTime;
                if (!tray.IsFull || tray.HasTriplet()) ExitNearMiss();
                else if (nearMissTimer <= 0f) ShowContinuePanel();
            }
            if (State == ShiftState.Playing || State == ShiftState.NearMiss)
            {
                starvationCheck += Time.unscaledDeltaTime;
                if (starvationCheck > 1f) { starvationCheck = 0f; EnsureBeltNotStarved(); }
                hud.SetFreeze(belt.FreezeRemaining);
                GameServices.Audio.SetMusicPitch(tickets != null && tickets.RemainingItemsTotal <= 3 ? 1.05f : 1f);
            }
        }

        float starvationCheck;

        /// <summary>
        /// Safety net: if the belt and its queue are both empty while orders are still open, the level
        /// cannot be finished. Re-supply exactly what is needed (complete the tray's partial groups and
        /// any open ticket) and log it, so a data bug degrades to a longer shift instead of a soft-lock.
        /// </summary>
        void EnsureBeltNotStarved()
        {
            if (belt.Items.Count > 0 || belt.QueuedCount > 0 || tickets == null || tickets.AllComplete) return;
            var need = new List<string>();
            var counted = new HashSet<string>();
            foreach (var e in tray.Slots)
            {
                if (!counted.Add(e.id)) continue;
                int mod = tray.CountOf(e.id) % 3;
                if (mod != 0) for (int i = 0; i < 3 - mod; i++) need.Add(e.id);
            }
            foreach (var t in tickets.Active)
            {
                int covered = tray.CountOf(t.itemId) % 3 != 0 ? 1 : 0;   // the partial group above already completes one triple
                for (int k = covered; k < t.remaining; k++) { need.Add(t.itemId); need.Add(t.itemId); need.Add(t.itemId); }
            }
            foreach (var t in tickets.Queued)
                for (int k = 0; k < t.remaining; k++) { need.Add(t.itemId); need.Add(t.itemId); need.Add(t.itemId); }
            if (need.Count == 0) return;
            Debug.LogWarning("[Shift] Belt starved with open orders on level " + level.levelIndex + " — re-supplying " + need.Count + " items.");
            belt.Enqueue(need);
        }

        // ------------------------------------------------------------------ taps

        void OnItemTapped(BeltItem item)
        {
            if (State != ShiftState.Playing && State != ShiftState.NearMiss) return;
            if (item == null || !item.gameObject.activeSelf || item.InFlight) return;

            if (item.Obstacle == ItemObstacle.Cloche)
            {
                item.OpenCloche();
                GameServices.Audio.Play("open");
                GameServices.Haptics.Play(HapticType.Light);
                vfx.Burst(item.transform.position + Vector3.up * 0.4f, new Color(0.9f, 0.92f, 1f), 10);
                return;
            }
            if (item.Obstacle == ItemObstacle.Napkin)
            {
                item.Untie();
                GameServices.Audio.Play("untie");
                GameServices.Haptics.Play(HapticType.Light);
                return;
            }
            if (tray.IsFull) { hud.Shake(3f, 0.12f); return; }
            hud.TapRipple(cam.WorldToScreenPoint(item.transform.position + Vector3.up * 0.3f), new Color(1f, 0.95f, 0.75f, 0.9f));
            PickItem(item);
        }

        void PickItem(BeltItem item)
        {
            var def = item.Definition;
            bool charred = item.IsCharred;
            belt.Remove(item);
            item.InFlight = true;
            Vector2 fromScreen = cam.WorldToScreenPoint(item.transform.position + Vector3.up * 0.3f);

            // ---- model (exact frame) ----
            var res = tray.Insert(def.id, charred);
            GameEvents.RaiseItemPicked(item);
            GameEvents.RaiseTrayChanged();

            if (def.pickupSfx != null) GameServices.Audio.PlayClip(def.pickupSfx);
            else GameServices.Audio.Play(def.pickupSfxKey, 1f, Random.Range(0.95f, 1.05f));
            GameServices.Haptics.Play(def.pickupHaptic);
            vfx.Burst(item.transform.position + Vector3.up * 0.3f, def.particleColor, 8);

            var it = item;
            Tween.Scale(it.transform, Vector3.zero, 0.12f, Ease.InQuad, () => factory.Release(it));

            Ticket ticket = null; bool completed = false; int railSlot = -1;
            if (res.triplet)
            {
                ticket = ApplyTripletToModel(res.clearedId, res.anyCharred, out completed, out railSlot);
            }
            else if (tray.IsFull && !tray.HasTriplet())
            {
                EnterNearMiss();
            }

            // ---- view ----
            var clearedIdx = res.clearedIndices;
            string clearedId = res.clearedId;
            bool anyCharred = res.anyCharred;
            hud.Tray.FlyIn(fromScreen, def, charred, res.slotIndex, () =>
            {
                if (res.triplet)
                {
                    Vector2 target = ticket != null ? hud.Tickets.SlotScreenPosition(railSlot) : new Vector2(Screen.width * 0.5f, Screen.height + 120f);
                    // 13A: confirm -> staggered lift -> golden arc to the ticket icon. The model already resolved
                    // the match; the view only reflects it. One whoosh at launch, one bell only on completion.
                    hud.Tray.FlyOutTriplet(clearedIdx, Lookup(clearedId), anyCharred, target,
                        () => OnTripletLanded(ticket, completed, railSlot),
                        () => { if (ticket != null) hud.Tickets.Anticipate(railSlot, true); GameServices.Audio.Play("whoosh", 0.8f, Random.Range(0.95f, 1.05f)); });
                    hud.Tray.Refresh(tray);
                    GameServices.Audio.Play("clear", 0.8f);
                    GameServices.Haptics.Play(HapticType.Medium);
                    hud.Shake(2f, 0.1f);
                    rig.Punch(1.6f, 0.22f);
                    comboCount++;
                    if (comboCount >= 3 && comboCount % 3 == 0) hud.Flash(comboCount >= 9 ? "ON FIRE!" : comboCount >= 6 ? "GREAT!" : "NICE!", 0.35f);
                }
                else hud.Tray.Refresh(tray);
            });
        }

        /// <summary>Credits a triplet against the tickets. Returns the ticket touched (null = filler).</summary>
        Ticket ApplyTripletToModel(string id, bool anyCharred, out bool completed, out int railSlot)
        {
            if (anyCharred) charredMatched = true;
            var ticket = tickets.Credit(id, out completed);
            railSlot = ticket != null ? ticket.railSlot : -1;
            GameEvents.RaiseTripletCleared(id, anyCharred);
            if (ticket != null)
            {
                ticketItemsDone++;
                hud.SetProgress(ticketItemsTotal <= 0 ? 1f : ticketItemsDone / (float)ticketItemsTotal);
                GameEvents.RaiseTicketProgressed(ticket);
                if (completed)
                {
                    hud.Tickets.Hold(railSlot);
                    var p = GameServices.Profile;
                    p.tipJarTokens += cfg.tokensPerTicket;
                    GameEvents.RaiseTicketCompleted(ticket);
                }
            }
            if (tickets.AllComplete)
            {
                State = ShiftState.Won;
                belt.Running = false;
                input.Enabled = false;
                hud.Boosters.SetInteractable(false);
                KillTimeScaleTween();
                Time.timeScale = 1f;
                hud.SetNearMiss(false);
                Tween.Delay(1.0f, WinSequence, true);
            }
            return ticket;
        }

        int comboCount;

        void OnTripletLanded(Ticket ticket, bool completed, int railSlot)
        {
            if (ticket == null) return;
            hud.Tickets.Anticipate(railSlot, false);
            Vector2 iconScreen = hud.Tickets.SlotScreenPosition(railSlot);
            if (completed)
            {
                // Full completion only: bar fills, checkmark, bounce, single bell, coins to the Tip Jar.
                GameServices.Audio.Play("bell");
                hud.Tray.SparkleAtScreen(iconScreen, Style.ReducedMotion ? 0 : 8);
                hud.Tickets.AnimateComplete(railSlot, tickets, null);
                hud.CoinsToTipJar(iconScreen, GameServices.Profile.tipJarTokens, cfg.tokensPerTicket);
                hud.Flash("ORDER UP!", 0.5f);
                hud.FloatText(iconScreen + new Vector2(0f, -40f), "+" + cfg.tokensPerTicket + " tokens", UIBuilder.Gold, 36);
                // Bible: medium double-pulse when a triplet clears into a ticket.
                GameServices.Haptics.Play(HapticType.Medium);
                Tween.Delay(0.12f, () => GameServices.Haptics.Play(HapticType.Medium), true);
            }
            else
            {
                // Partial progress: icon pulse + small sparkle + count/progress animate to the authoritative value.
                hud.Tickets.Arrive(railSlot, ticket, rt => hud.Tray.SparkleAtScreen(iconScreen, Style.ReducedMotion ? 0 : 4));
                GameServices.Audio.Play("pickup_chime", 0.5f, 1.3f);
                GameServices.Haptics.Play(HapticType.Light);
            }
        }

        void OnItemCharred(BeltItem item)
        {
            vfx.Burst(item.transform.position + Vector3.up * 0.5f, new Color(0.25f, 0.2f, 0.2f), 12);
        }

        // ------------------------------------------------------------------ near miss / continue

        void EnterNearMiss()
        {
            if (State != ShiftState.Playing) return;
            State = ShiftState.NearMiss;
            nearMissTimer = cfg.nearMissSeconds;
            KillTimeScaleTween();
            timeScaleTween = Tween.Float(Time.timeScale, cfg.nearMissTimeScale, 0.3f, v => Time.timeScale = v, Ease.OutQuad, null, true);
            hud.SetNearMiss(true);
            GameServices.Audio.Play("siren");
            GameServices.Haptics.Play(HapticType.Heavy);
            GameEvents.RaiseNearMissStarted();
        }

        void ExitNearMiss()
        {
            if (State != ShiftState.NearMiss) return;
            State = ShiftState.Playing;
            KillTimeScaleTween();
            timeScaleTween = Tween.Float(Time.timeScale, 1f, 0.25f, v => Time.timeScale = v, Ease.OutQuad, null, true);
            hud.SetNearMiss(false);
            closeCall = true;
            hud.Flash("CLOSE CALL!", 0.5f, UIBuilder.Gold);
            GameEvents.RaiseNearMissEnded();
        }

        void ShowContinuePanel()
        {
            State = ShiftState.Continue;
            KillTimeScaleTween();
            Time.timeScale = 0f;
            failsThisLevel++;
            var p = GameServices.Profile;
            var buttons = new List<PanelButton>();
            string body;
            if (failsThisLevel == 1)
            {
                body = "Short on counter space?\n\nGrab an Extra Plate (8th slot)\n+ clear " + cfg.continueClearItems + " items.";
                buttons.Add(new PanelButton(cfg.continueCostCoins + " COINS  (have " + p.coins + ")", () => ApplyContinue(true, false), UIBuilder.Gold) { interactable = p.coins >= cfg.continueCostCoins });
                buttons.Add(new PanelButton(cfg.continueCostTokens + " TOKENS  (have " + p.tokens + ")", () => ApplyContinue(false, true), UIBuilder.Mint) { interactable = p.tokens >= cfg.continueCostTokens });
            }
            else
            {
                body = "Counter's full again!\n\nWatch a short ad for an Extra Plate\n+ clear " + cfg.continueClearItems + " items.";
                buttons.Add(new PanelButton("WATCH AD", () =>
                {
                    GameServices.Ads.ShowRewarded(AdPlacement.RewardedContinue, ok =>
                    {
                        GameServices.AdPolicy.RewardedWatchedThisLevelEnd = true;
                        if (ok) ApplyContinue(false, false); else Lose();
                    });
                }, UIBuilder.Gold));
            }
            buttons.Add(new PanelButton("NO THANKS", Lose, UIBuilder.Red));
            hud.ShowPanel("COUNTER FULL!", body, buttons);
        }

        void ApplyContinue(bool payCoins, bool payTokens)
        {
            var p = GameServices.Profile;
            if (payCoins) { if (p.coins < cfg.continueCostCoins) return; p.coins -= cfg.continueCostCoins; }
            if (payTokens) { if (p.tokens < cfg.continueCostTokens) return; p.tokens -= cfg.continueCostTokens; }
            GameServices.SaveProfile();

            tray.SetCapacity(cfg.trayCapacity + 1);
            // "Clear 3 items": they go back to the kitchen (belt queue), never out of the level, so
            // every type stays a multiple of 3 and the shift remains winnable.
            var cleared = tray.RemoveLeastUseful(cfg.continueClearItems);
            var ids = new List<string>(cleared.Count);
            foreach (var e in cleared) ids.Add(e.id);
            belt.Enqueue(ids);
            hud.Tray.SetCapacity(tray.Capacity);
            hud.Tray.Refresh(tray);
            hud.ClosePanel();
            hud.SetNearMiss(false);
            Time.timeScale = 1f;
            State = ShiftState.Playing;
            GameServices.Audio.Play("bell");
            GameEvents.RaiseBoosterUsed(BoosterType.ExtraPlate);
            GameEvents.RaiseTrayChanged();
        }

        // ------------------------------------------------------------------ win / lose / pause

        void WinSequence()
        {
            int stars = ShiftRules.ComputeStars(level, tray.FreeSlots, charredMatched);
            int coins = ShiftRules.ComputeCoins(cfg, stars);
            var p = GameServices.Profile;
            p.coins += coins;
            p.SetStars(level.levelIndex, stars);
            if (level.levelIndex + 1 > p.highestLevelUnlocked) p.highestLevelUnlocked = Mathf.Min(level.levelIndex + 1, Mathf.Max(1, cfg.LevelCount));
            GameServices.SaveProfile();

            GameServices.Audio.Play("win");
            GameServices.Haptics.Play(HapticType.Heavy);

            // Sweep the remaining belt items away with a stagger.
            var items = belt.SnapshotItems();
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                float delay = i * 0.05f;
                Tween.To(0.3f, t => { if (it != null) it.transform.localScale = Vector3.one * (1f - t); }, Ease.InQuad, () =>
                {
                    if (it == null) return;
                    vfx.Burst(it.transform.position + Vector3.up * 0.3f, it.Definition.particleColor, 10);
                    belt.Remove(it);
                    factory.Release(it);
                }, true, delay);
            }
            Vfx.Confetti(hud.FlightLayer, 60);

            var result = new ShiftResult { levelIndex = level.levelIndex, stars = stars, coins = coins, freeSlotsAtFinish = tray.FreeSlots, charredMatched = charredMatched };
            GameEvents.RaiseLevelWon(result);

            Tween.Delay(0.9f, () => MaybeInterstitial(() => ShowWinPanel(stars, coins, false)), true);
        }

        void ShowWinPanel(int stars, int coins, bool doubled)
        {
            var buttons = new List<PanelButton>();
            if (!doubled && GameServices.Ads.IsRewardedReady(AdPlacement.RewardedDoubleCoins))
                buttons.Add(new PanelButton("DOUBLE COINS  (watch ad)", () =>
                {
                    GameServices.Ads.ShowRewarded(AdPlacement.RewardedDoubleCoins, ok =>
                    {
                        if (!ok) { ShowWinPanel(stars, coins, false); return; }
                        GameServices.Economy.Grant(coins, 0);
                        GameServices.Audio.Play("coin");
                        ShowWinPanel(stars, coins * 2, true);
                    });
                }, UIBuilder.Mint));
            buttons.Add(new PanelButton("NEXT SHIFT", NextLevel, UIBuilder.Red));
            if (replay != null && replay.HasFootage)
                buttons.Add(new PanelButton(closeCall ? "SHARE CLOSE-CALL REPLAY" : "SHARE REPLAY", () =>
                {
                    Popups.Close();
                    hud.Flash("REPLAY", 0.4f, UIBuilder.Sky);
                    replay.PlayLast(true, () =>
                    {
                        if (replay.LastSavedFolder != null) GameServices.Share.ShareFile(replay.LastSavedFolder, "Retro Diner 3D — shift " + level.levelIndex);
                        ShowWinPanel(stars, coins, doubled);
                    });
                }, UIBuilder.Sky));
            buttons.Add(new PanelButton(doubled ? "MENU" : "PLAY AGAIN", doubled ? (System.Action)ToMenu : Restart, UIBuilder.Gold));
            hud.ShowPanel("SHIFT FINISHED!", "", buttons, true,
                c => hud.DecorateStars(c, stars, "+" + coins + " coins" + (doubled ? "  (doubled!)" : ""), "Tip jar: " + GameServices.Profile.tipJarTokens + " tokens"), 260f);
        }

        /// <summary>Interstitial gate for Win/Lose screens: shows one only when AdPolicy allows, then continues.</summary>
        void MaybeInterstitial(System.Action then)
        {
            var policy = GameServices.AdPolicy;
            bool show = policy.ShouldShowInterstitial(level.levelIndex, false, false);
            policy.RecordLevelEnd(show);
            if (!show) { then(); return; }
            GameServices.Ads.ShowInterstitial(() =>
            {
                then();
                Popups.RemoveAdsLink(policy.Policy.removeAdsLinkSeconds, () => { ToMenu(); menu.ShowStore(); });
            });
        }

        void Lose()
        {
            if (State == ShiftState.Lost) return;
            State = ShiftState.Lost;
            KillTimeScaleTween();
            Time.timeScale = 1f;
            belt.Running = false;
            input.Enabled = false;
            hud.SetNearMiss(false);
            GameServices.Economy.ConsumeEnergy();
            GameServices.Audio.Play("lose");
            GameEvents.RaiseLevelLost();
            MaybeInterstitial(() =>
                hud.ShowPanel("SHIFT OVER", "The counter locked up.\n\nTip: keep an eye on how many\ndifferent dishes are waiting on the tray.",
                    new List<PanelButton>
                    {
                        new PanelButton("RETRY", () => { if (GameServices.Economy.HasEnergy) Restart(); else { ToMenu(); menu.ShowEnergyPanel(); } }, UIBuilder.Red),
                        new PanelButton("MENU", ToMenu, UIBuilder.Gold),
                    }));
        }

        public void TogglePause()
        {
            if (State == ShiftState.Playing || State == ShiftState.NearMiss) Pause();
            else if (State == ShiftState.Paused) Resume();
        }

        ShiftState stateBeforePause;
        float timeScaleBeforePause = 1f;

        void Pause()
        {
            stateBeforePause = State;
            timeScaleBeforePause = Time.timeScale;
            KillTimeScaleTween();
            State = ShiftState.Paused;
            Time.timeScale = 0f;
            input.Enabled = false;
            hud.ShowPanel("PAUSED", "Level " + level.levelIndex, new List<PanelButton>
            {
                new PanelButton("RESUME", Resume, UIBuilder.Red),
                new PanelButton("RESTART", Restart, UIBuilder.Mint),
                new PanelButton("MENU", ToMenu, UIBuilder.Gold),
            });
        }

        void Resume()
        {
            if (State != ShiftState.Paused) return;
            hud.ClosePanel();
            State = stateBeforePause;
            Time.timeScale = stateBeforePause == ShiftState.NearMiss ? cfg.nearMissTimeScale : 1f;
            input.Enabled = true;
        }

        void KillTimeScaleTween()
        {
            if (timeScaleTween != null) { timeScaleTween.Kill(); timeScaleTween = null; }
        }

        // ------------------------------------------------------------------ boosters

        public void UseBooster(BoosterType type)
        {
            if (State != ShiftState.Playing && State != ShiftState.NearMiss) return;
            if (!ShiftRules.IsBoosterUnlocked(cfg, type, level.levelIndex)) return;
            var p = GameServices.Profile;
            if (p.GetBooster(type) <= 0)
            {
                OfferBoosterRefill(type);
                return;
            }

            switch (type)
            {
                case BoosterType.GoldenSpatula:
                    if (!UseSpatula()) { GameEvents.RaiseToast("No full triple on the belt right now"); return; }
                    break;
                case BoosterType.OrderUpBell:
                    belt.Freeze(cfg.bellFreezeSeconds);
                    GameServices.Audio.Play("bell");
                    GameServices.Audio.Play("freeze");
                    break;
                case BoosterType.RewindCoaster:
                    belt.Rewind(cfg.rewindDistance, cfg.rewindDuration);
                    GameServices.Audio.Play("rewind");
                    break;
                default: return;
            }
            p.AddBooster(type, -1);
            GameServices.SaveProfile();
            hud.Boosters.Refresh(level.levelIndex);
            hud.Boosters.Flash(type);
            GameServices.Haptics.Play(HapticType.Heavy);
            GameEvents.RaiseBoosterUsed(type);
        }

        bool UseSpatula()
        {
            string chosen = null;
            foreach (var t in tickets.Active) if (belt.CountPickable(t.itemId) >= 3) { chosen = t.itemId; break; }
            if (chosen == null) foreach (var id in level.itemPool) if (belt.CountPickable(id) >= 3) { chosen = id; break; }
            if (chosen == null) return false;

            var items = belt.TakeTriple(chosen);
            if (items == null) return false;
            bool anyCharred = false;
            foreach (var it in items) anyCharred |= it.IsCharred;
            var def = Lookup(chosen);

            bool completed; int railSlot;
            var ticket = ApplyTripletToModel(chosen, anyCharred, out completed, out railSlot);
            Vector2 target = ticket != null ? hud.Tickets.SlotScreenPosition(railSlot) : new Vector2(Screen.width * 0.5f, Screen.height + 120f);

            int remaining = items.Count;
            foreach (var item in items)
            {
                item.InFlight = true;
                Vector2 from = cam.WorldToScreenPoint(item.transform.position + Vector3.up * 0.3f);
                var it = item;
                vfx.Burst(it.transform.position + Vector3.up * 0.3f, UIBuilder.Gold, 14);
                Tween.Scale(it.transform, Vector3.zero, 0.15f, Ease.InQuad, () => factory.Release(it));
                hud.Tray.FlyIcon(from, def, it.IsCharred, target, 0.45f, () =>
                {
                    remaining--;
                    if (remaining <= 0)
                    {
                        GameServices.Audio.Play("clear");
                        OnTripletLanded(ticket, completed, railSlot);
                    }
                });
            }
            GameServices.Audio.Play("spatula");
            return true;
        }

        /// <summary>Out of a booster mid-shift: buy +1 with coins/tokens or watch a rewarded ad. Pauses the belt meanwhile.</summary>
        void OfferBoosterRefill(BoosterType type)
        {
            var eco = GameServices.Economy;
            var offer = eco.RefillOfferFor(type);
            if (offer == null) { GameEvents.RaiseToast("No refill available"); return; }
            var p = GameServices.Profile;
            bool wasRunning = belt.Running;
            belt.Running = false;
            System.Action resume = () => { Popups.Close(); belt.Running = wasRunning; hud.Boosters.Refresh(level.levelIndex); };
            var buttons = new List<PanelButton>
            {
                new PanelButton(offer.coinPrice + " COINS  (have " + p.coins + ")", () => { if (eco.BuyRefill(offer, false)) { GameServices.Audio.Play("coin"); resume(); UseBooster(type); } }, UIBuilder.Gold) { interactable = eco.CanAffordCoins(offer.coinPrice) },
                new PanelButton(offer.tokenPrice + " TOKENS  (have " + p.tokens + ")", () => { if (eco.BuyRefill(offer, true)) { GameServices.Audio.Play("coin"); resume(); UseBooster(type); } }, UIBuilder.Mint) { interactable = eco.CanAffordTokens(offer.tokenPrice) },
            };
            if (GameServices.Ads.IsRewardedReady(AdPlacement.RewardedBoosterRefill))
                buttons.Add(new PanelButton("WATCH AD  (+1)", () =>
                {
                    GameServices.Ads.ShowRewarded(AdPlacement.RewardedBoosterRefill, ok =>
                    {
                        if (ok) { p.AddBooster(type, 1); GameServices.SaveProfile(); }
                        resume();
                        if (ok) UseBooster(type);
                    });
                }, UIBuilder.Mint));
            buttons.Add(new PanelButton("NOT NOW", resume, UIBuilder.Red));
            Popups.Show("OUT OF " + Name(type).ToUpper(), "Grab one more " + offer.title + "?", buttons, true, null, 10f);
        }

        static string Name(BoosterType t)
        {
            switch (t)
            {
                case BoosterType.GoldenSpatula: return "Golden Spatulas";
                case BoosterType.OrderUpBell: return "Order Up! Bells";
                case BoosterType.RewindCoaster: return "Rewind Coasters";
                default: return "Extra Plates";
            }
        }
    }
}
