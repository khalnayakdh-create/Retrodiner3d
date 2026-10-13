# Retro Diner 3D: Order Up!

Hybrid-casual 3D sorting puzzle (Unity 6, portrait, iOS/Android). Project codename **Blue Plate**.

## Open it

1. Unity Hub → **Add** → pick this folder (the repo root is the Unity project). Any Unity **6000.x** works; accept the version switch if Hub asks.
2. Let it import. Expect console errors on the very first import if an API nit slipped through — this drop was authored outside the Editor. Paste them back and they get fixed.
3. Menu **Retro Diner → Setup Project**. This copies `Chat Gpt Images/**/*.png` into `Assets/Resources/RetroDiner/Art`, writes the 7 `ItemDefinition`s, 50 `LevelDefinition`s and `GameConfig`, validates every level, and exports `docs/levels/*.json`.
4. Open `Assets/Scenes/Boot.unity` and press **Play**. (If the scene failed to import: **Retro Diner → Recreate Boot Scene**.)

The game also runs *without* step 3: `GameBootstrap` falls back to in-code content and colored placeholder dishes.

## Layout

```
Assets/
  Scenes/Boot.unity              one GameObject: GameBootstrap
  Scripts/RetroDiner/
    Core/      GameEvents (static bus), GameServices (locator), Tween, SeededRandom, service interfaces + stubs
    Data/      ItemDefinition, LevelDefinition (+JSON), GameConfig, ArtCatalog, ContentLibrary (7 items, 50-level curve)
    Rules/     TrayModel, TicketBoard, SpawnListGenerator, LevelValidator, ShiftRules   (plain C#, no Unity objects)
    Belt/      TrackSpline, BeltController, BeltItem, ItemFactory
    Gameplay/  GameplayController (state machine), TapInput, CameraRig, Vfx
    UI/        UIBuilder, HudView, TrayView, TicketRailView, BoosterBarView, MainMenuView
    Meta/      PlayerProfile, SaveService
    Audio/     AudioService (procedural clip bank)
    Boot/      GameBootstrap
  Editor/RetroDiner/             art importer, content generator, project settings
  Resources/RetroDiner/          generated: GameConfig, Items/, Levels/, Art/   (created by the menu)
Chat Gpt Images/                 source PNG pack + manifests (not imported directly)
docs/                            PROGRESS.md, DECISIONS.md, levels/*.json
```

## Controls (Editor)
Left click = tap. Pause button top-right. Boosters unlock at levels 4 / 5 / 7 (3 of each granted on unlock).
