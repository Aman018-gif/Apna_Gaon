# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Mohalla Hero: Choices That Count** is a 2D top-down civic-education story game in **Unity 6 / C#** (all ages, Hinglish).
* It is a separate Unity project nested inside the *Apna Gaon* folder. Don't modify the parent project (`../Assets/ApnaGaon`).
* `Docs/DESIGN_DECISIONS.md` records every decision: chapters, rating formula, endings and map. Keep it in sync with the code; its mission table mirrors `MissionDatabase.cs`.
* Other docs: `Docs/SETUP_GUIDE.md`, `Docs/UI_LAYOUT_PLAN.md`, `Docs/ASSET_LIST.md`.

## Running and testing

* **Play**: open this folder in Unity 6 and press Play in any scene. `GameManager.Boot` (`[RuntimeInitializeOnLoadMethod]`) builds the camera, UI and world. The editor script creates `Assets/Scenes/Main.unity`.
* `Unity` is not on PATH. On this machine it is `/Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/MacOS/Unity`. Batchmode fails while the project is open in the editor.
* **Tests**:
  * EditMode: `<Unity> -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml`
  * PlayMode: the same with `-testPlatform PlayMode`. Set `MH_SHOTS=<dir>` to save screenshots of every step.
  * Single test: add `-testFilter MohallaHero.Tests.StoryPlaythroughTests.BestPath_ReachesTheMohallaHeroEnding`.
* PlayMode tests set `SaveSystem.PathOverride`, `VoiceSystem.Muted` and `Settings.TouchOverride`, so they never touch the real save or PlayerPrefs.
* Editor menu **Mohalla Hero**: Setup Main Scene, Delete Save File, Open Save Folder, Create Karma Stat Assets, Validate Story (Ink).

## Architecture

**Everything is built from code.** There are no prefabs and no art assets.
* `SpriteFactory*` draws every sprite with `PixelCanvas`.
* `UIFactory` builds uGUI with the built-in font.
* World text uses `WorldObjects.Label`: a TextMesh on a nameplate.

**The story is Ink.**
* `Resources/Stories/mohalla.txt` INCLUDEs `m1_…`–`m6_…`. They use `.txt` so that Unity loads them as TextAssets.
* `Story/InkLite.cs` parses and runs a tested subset of Ink:
  * knots, diverts, once-only `*` and sticky `+` choices, conditions, gathers, tags, VAR, `~` code, `{cond: a|b}` and `{~shuffle}`;
  * no nested choices, stitches or tunnels.
* `DialogueManager` is the `IStoryHost`. It implements the EXTERNAL functions (`has`, `give`, `trust`, `add_trust`, `joined`, `chapter`, `rating`, `resolve`, `minigame`, `unlock`, `protest`, `vote`, `sleep`, `save`…).
  * Keep `StoryPlaythroughTests.Sim.Call` in sync with it.
* Choice tags (`#civic:+5 #trust:+15 #why:… #violence`) are parsed and applied by `ChoiceTags` (shared with the tests). Every choice with |karma| ≥ 5 needs `#why`.
* At most **4 visible choices** per page (the spec allows 2–4). Put evidence choices behind a "Saboot dikhao" sub-knot.
* World objects that repeat (lights, taps, saplings, garbage, voter doors) must use **sticky `+` choices**: once-only choices are global per knot.
* Talking to an NPC runs `npc_<id>`, which routes by `chapter()`. A `StoryObject` runs its `Knot`, and `resolve()` sets `obj_<id>`.

**State.**
* `StoryState` holds every variable: Ink VARs, mission counters, `item_*` evidence, `trust_*`, `obj_*` and remembered once-only choices (`__chosen:knot#n`).
* Saves store only the values that differ from their defaults.
* `MissionSystem` reads progress from variables. A chapter is done when its `doneVar` is set by Ink. `joined` is computed from trust.
* `KarmaSystem` uses `KarmaStatAsset` ScriptableObjects (runtime copies; authored overrides go in `Resources/Karma`).
* `EndingEvaluator`:
  * rating = 70 × the karma share (per-stat caps in `Balance.RatingCap`) + 10 × the supporter share + 20 × the informed-voter share − 25 per violence;
  * the winner depends on the rating and the number of informed voters, never on the player's vote.

**Session** (`GameManager.StartSession`).
* Pure systems first.
* Then the world: WorldMap → TimeManager → WorldBuilder → AreaManager (barricades) → NavGrid.
* Then the player → ProtestManager / ElectionManager (+ LoudspeakerRickshaw) → NPCManager → CityVisuals.
* `CheckProgress()` runs after every conversation and mini-game: it announces chapters, re-places NPCs and saves.

**NPCs** (`AI/NPC`).
* `Think()` picks, in order: follow in the march (`ProtestManager.FollowerIndex`), a story spot (`NPCManager.TryGetSpecialSpot`), sleep, work, home.
* The FSM states are GoTo, Work, Home, Sleep, Talk, Special and Follow (the trail-following procession). Movement uses A* on `NavGrid`, where paved tiles are cheaper.
* `Guidance` decides "!" markers, orange map dots and the golden objective marker. It mirrors the routing in `mohalla.txt`.

**The world reacts.** `CityVisuals` changes the scene with the rating and story variables: dim facades, posters or murals, bunting, potholes, broken street lights and repaired swings. `CameraRig.Tint` blends day/night with a gloom haze that lifts as the rating rises.

**UI.**
* Screens derive from `UIScreen`. `UIManager` owns modal state, toasts, banners (held while a window is open), subtitles, fades and hotkeys.
* Input guards: `UIManager.JustOpened` / `ClosedThisFrame`, plus per-screen frame guards, so one key press never acts twice.
* Touch buttons call `GameInput.Press`, which reads as "down" on exactly the next frame.

**Rendering order.**
* The camera Y-sorts with a custom axis, and sprites use `SpriteSortPoint.Pivot` with pivots at the feet.
* Orders: ground −100, objects 0, markers 30, building labels 34, story labels 35–40, mood overlay 200, glows 210.

## Conventions

* Tunable numbers go in `Balance.cs`.
* Coordinates go in `WorldLayout.cs`. Run the EditMode tests after moving anything: they check reachability and overlaps.
* Characters and parties are fictional. Never use real politicians, party names, symbols or colours.
* Never write content that could read as rude or unsafe for children (`SpriteFactory.Finger` was redrawn for this reason).
* The story stays linear across 6 chapters, with branches inside chapters. Endings come from the rating and informed voters.
