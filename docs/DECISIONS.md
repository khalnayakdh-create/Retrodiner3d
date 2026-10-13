# DECISIONS

Decisions made where the bible / build prompt left room, or where the prompt was deliberately deviated from. Each one is reversible; the "why" says what it would cost to flip.

| # | Decision | Why |
|---|----------|-----|
| 1 | **Everything is built from code; the only scene object is `GameBootstrap`.** No prefabs, no hand-authored UI hierarchy. | The first drop was authored without the Unity Editor open. Code-built hierarchies compile-check and diff cleanly; YAML does not. Prefabs can be extracted later from the running hierarchy if designers need them. |
| 2 | **Legacy uGUI `Text` instead of TextMeshPro** (single swap point: `UIBuilder.Label`). | TMP needs its "Essential Resources" imported through a dialog on first use; with no one at the Editor that would have left every label blank. Swap = edit one helper. |
| 3 | **Legacy Input (`Input.GetMouseButtonDown`)** instead of the new Input System package. | Fresh projects default to the old Input Manager; the Input System package would also require flipping `activeInputHandler`. Only `TapInput.cs` touches input. |
| 4 | **Built-in render pipeline for now, URP later.** | URP needs a pipeline asset wired into Graphics Settings. All materials are created via `PlaceholderMaterials.FindShader()`, which prefers the URP Lit shader when present, so flipping to URP is a settings change, not a code change. |
| 5 | **Own Catmull-Rom spline (`TrackSpline`) instead of the Splines package.** | Removes a package dependency and gives a cheap arc-length table. Shapes are code presets (ring / teardrop / figure-8). |
| 6 | **Level data in `Resources/RetroDiner` + JSON export, Addressables deferred to M7.** | `LevelDefinition.ToJson/FromJson` already exists; moving the 50 assets into an Addressables group is mechanical once the Addressables package is added. |
| 7 | **Items are billboarded sprites from the PNG pack** (3D placeholder primitives when no sprite). | Real FBX dishes come later (Meshy/Tripo from the same PNGs); `ItemDefinition.prefab` is already honored when set. |
| 8 | **Belt camera renders only the middle viewport band (30 %–76 %)**; a second orthographic camera draws the diner background full-screen. | Keeps the belt framed between the Order Rail and the tray on any aspect ratio without lens-shift tricks. |
| 9 | **Items on the belt are capped (`maxItemsOnBelt`); the rest queue and spawn at distance 0 as space frees.** | With a looping belt and no removal, "all items visible at once" would make every level trivially solvable. The visible-window cap is also what the validator simulates. |
| 10 | **All procedural audio.** Every SFX is a synthesized sine/noise burst built at startup. | The loop has sound on day one; real recordings drop into `ItemDefinition.pickupSfx` or replace entries in `AudioService.BuildBank`. |
| 11 | **Booster with 0 count shows a toast** instead of opening the store. | Store is M5. |
| 12 | **Extra Plate is only obtainable through the continue offer** (per the design review: 3 in-shift boosters). | Mirrors the doc's fix list; the mockup's 4th "Freeze Items" button is folded into the Bell. |
| 13 | **Validator model**: greedy player picks (a) the type that completes a tray triple, else (b) the type with the most copies in tray + visible window, never opening a type with no visible way to finish. Random tapper = uniform pick from the window. A level is accepted when greedy wins and random wins < 65 % (levels 7–10, the intended first-fail ramp) or < 50 % (level 11+). | Simple, deterministic, and runs in milliseconds for all 50 levels. Tune `ChooseGreedy` if real players beat the curve too easily. |
| 14 | **Star 2 = ≥3 free tray slots at finish; star 3 = star 2 + no charred item matched.** Coins = 50 + 25·stars. | Straight from the prompt. |
| 15 | Energy decrements on loss and is stored, but **nothing blocks play yet**. | Energy gating is an M5 meta decision that needs the punch-card and refill offers alongside it. |
| 16 | **Levels 7+ always use ≥4 item types.** | Found by the validator on first run: with 3 types the 7-slot tray can hold 2 of each (6) and never lock, so levels 7–10 were unlosable even by random tapping. |
| 17 | **Unity 6000.6 specifics**: `com.unity.modules.vr` no longer exists (removed from the manifest); `FindFirstObjectByType` is deprecated there (use `FindAnyObjectByType`). | Hit on the first editor open. |
