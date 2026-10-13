# PROGRESS

## 2026-10-13 — M1 + M2 + most of M3 authored (first drop)
- Project scaffolded at repo root (Unity 6, built-in RP for now, uGUI only). Boot scene holds a single `GameBootstrap`; everything else is built from code.
- M1 done in code: ScriptableObject data (Item/Level/GameConfig), static `GameEvents` bus, `GameServices` locator, in-house `Tween`, closed Catmull-Rom belt (ring / teardrop / figure-8) with arc-length lookup, pooled placeholder items (sprite billboard when art exists, colored primitive + label otherwise), scrolling roller texture, chrome rails.
- M2 done in code: tap raycast → fly into 7-slot tray, grouping insert, instant triplet clear, ghost icons fly to the ticket, Order Rail with 3 cards + queue, star progress bar, tip jar counter, win sweep + confetti + stars, lose on 7/7 with no triplet, pause/resume/restart, main menu + 50-level grid + settings.
- M3 done in code: cloche (tap to open), napkin pairs (tap to untie both), burn after N laps (charred sprite/tint, blocks 3rd star), spill segments (1.8x speed), Golden Spatula / Order Up! Bell / Rewind Coaster with profile counts + unlock levels, near-miss slow-mo (5 real seconds, 0.2 timescale, siren + vignette), continue panel (first fail: 90 coins / 10 tokens; later: fake rewarded ad), IAdService / IIAPService / IHapticService / IShareService stubs.
- M4 partially done: deterministic spawn generator (multiples of 3, early triple guarantee), `LevelValidator` (greedy player must win; random tapper should lose past L6), 50-level curve in `ContentLibrary`, Editor menu to write assets + validate + export JSON.
- **Verified in Unity 6000.6.4f1 on the dev PC**: compiles with 0 errors / 0 warnings; `Retro Diner > Setup Project` imported the 45 PNGs and wrote 7 items + 50 levels + GameConfig; Boot scene plays. Played level 1 end to end (tap → tray → triplet → ticket → Shift Finished, 3 stars, +125 coins), progress persisted to level 2, level select + pause/win panels render on the real art. Fixed on the way: ParticleSystem duration-while-playing error, tray/ticket/popup layouts calibrated to the PNG pack, `com.unity.modules.vr` removed (gone in 6000.6), `FindAnyObjectByType`.
- Difficulty pass after first playtest: steeper type/obstacle/window ramp from L4 (DECISIONS #18); docs/levels/*.json is the committed source of truth for the curve.
- Level curve fix from the validator: with 3 item types a 7-slot tray can never lock, so levels 7+ now use 4+ types; `docs/validate_levels.py` is a Python mirror of LevelValidator for quick checks outside Unity (all 50 accepted).

## Next
- Play L3–L12 by hand and tune belt speed / spacing / obstacle chances; replace the level-select grid with a path on the map art; verify cloche/napkin/burn/spill visuals at L8/L11/L15/L20.
- M5: punch-card, energy regen, Tip Jar crack offer, Store stub + AdPolicy + Remove Ads pack.
- M6: replay ring buffer, music loop, richer particles per item.
- M7: pooling audit (UI flyers currently allocate), profiler pass, URP switch, Android build.
