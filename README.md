# Mohalla Hero: Choices That Count

A 2D top-down story game for all ages, built with **Unity 6** and **C#**. You are a newcomer to *Shanti Nagar*, an Indian city neighbourhood. Over six chapters you learn civic sense, honesty, care for the environment, safety and anti-bullying, and voting, through choices with consequences. Every choice moves four karma stats, explains *why it matters* and changes how the mohalla looks. Together they decide who wins the ward election and which of four endings you get.

All characters are fictional. There are no real politicians, parties or party symbols.

* Setup for beginners, building for Android/iOS and editing the story: **Docs/SETUP_GUIDE.md**
* Every design decision, the mission table, the rating formula and the map: **Docs/DESIGN_DECISIONS.md**
* Screen-by-screen UI layouts and the palette: **Docs/UI_LAYOUT_PLAN.md**
* Free assets to replace the procedural art, plus fonts and audio: **Docs/ASSET_LIST.md**
* The sample Ink story for one mission: **Assets/MohallaHero/Resources/Stories/m1_bijli_bachao.txt**. All six chapters are written in Ink.

## Quick start

1. Unity Hub → **Add project from disk** → select this `MohallaHero` folder → open it with Unity 6.
2. Press **Play** (any scene works; the game builds itself at runtime).
3. **New Game** → name → **Chalo!**

The controls are WASD/arrows, Shift to run, **E** to talk or use, **1–4** for choices, **Tab** for the phone, **M** for the map, **H** for Pintu's hint and **Esc** for the menu. On phones there is a joystick plus ACTION / PHONE / MAP / PINTU buttons.

## What's in the game

| Feature | Where |
|---|---|
| **Karma system**: Civic Sense, Green Score, Courage, Honesty. Each gains and loses XP per choice, has levels and rank titles, and is stored in ScriptableObjects (`KarmaStatAsset`) | `Systems/KarmaSystem.cs`, `KarmaStatAsset.cs` |
| **"Why it matters" card** after every karma choice | `UI/WhyItMattersScreen.cs`, the `#why:` tags in the Ink files |
| **Branching dialogue in Ink**: 2–4 choices per page, each tagged with stat effects (`#civic:+5 #trust:+15 #why:…`) | `Story/InkLite.cs` (built-in Ink reader), `Systems/DialogueManager.cs`, `Resources/Stories/*.txt` |
| **Trust system**: 0–100 per resident. Honest arguments raise it, lies and threats lower it; residents at 60+ join the protest | `Systems/TrustSystem.cs` |
| **Peaceful protest**: placards (slogan game), gathering at dusk, a candle march where supporters follow you, a petition to the election officer. Violence costs 25 rating points. | `Systems/ProtestManager.cs`, `m5_shanti_march.txt` |
| **Election Mela finale**: party stalls, a loudspeaker rickshaw, a street play, door-to-door campaigning, rumour fact-checking, bribe offers, and the polling booth with an ink-on-finger animation, EVM and VVPAT | `Systems/ElectionManager.cs`, `UI/MiniGames/VotingScreen.cs`, `m6_election_mela.txt` |
| **The world reacts**: gloomy, potholed and graffiti-covered at a low rating; bright, with murals and festival bunting at a high one. Fixed lights, closed taps and planted trees stay fixed. | `World/CityVisuals.cs`, `World/CameraRig.cs` |
| **Mohalla Rating and 4 endings**: *Mohalla Hero!*, *Jagruk Nagrik*, *Aadha Sach* and *Jugaad Raj Returns*, decided by your stats and how many voters you informed (never by your secret vote) | `Systems/EndingEvaluator.cs` |
| **Mini-games**: waste sorting, fake-news spotting, slogan making | `UI/MiniGames/*` |
| **Save/load**: one JSON file that stores only the story variables that changed; autosaves | `Core/SaveSystem.cs` |
| **City-app UI**: top status bar, karma chips, Mohalla Rating card with minimap, "Mohalla Phone" menu, festival palette, Hinglish text, subtitles, touch controls | `UI/*` |
| **Voices**: every character speaks through the OS text-to-speech, with subtitles | `Systems/VoiceSystem.cs` |

## The six chapters

| # | Mission | Learn |
|---|---|---|
| 1 | **Bijli Bachao** (Asha Ma'am) | Switch off wasted lights and fans, fix taps, plant trees, sort garbage. Wasting costs points. |
| 2 | **Bully Buster** (Pintu) | Stop a fight safely, report it, support the victim, give the bully a way back |
| 3 | **Imandar Bazaar** (Sharma Aunty) | Return extra change, refuse scams and OTP-sharing, don't bribe to jump the queue |
| 4 | **Sach ki Awaaz** (Meera Didi) | Collect evidence (photo, bill, RTI reply) against Netaji's park scam and convince neighbours honestly |
| 5 | **Shanti March** (Meera Didi) | Placards, slogans, a candle march and a petition. Stay peaceful. |
| 6 | **Election Mela** (Officer Fernandes) | Compare candidates, fact-check rumours, refuse bribes, inform voters, then vote |

The cast: **Pintu** (meme kid, hints), **Chacha Gossip** (fake-news tea stall), **Netaji Jugaad Singh** (free WiFi on the Moon), **Dr. Vaada Verma** (flip-flops), **Asha Ma'am**, **Lala Kishorilal**, **Sharma Aunty**, **Raju Rickshawala**, **Golu**, **Bunty**, **Officer Fernandes** and **Meera Didi**.

## Project folder structure

```
MohallaHero/                         ← open this folder in Unity Hub
├── Assets/MohallaHero/
│   ├── Scripts/                     game code (assembly "MohallaHero", namespace MohallaHero)
│   │   ├── Core/        GameManager (boot, sessions, central state) · Balance (all numbers) · SaveSystem (+ Settings)
│   │   │                GameInput (keyboard + touch) · GameEvents · Enums · Compat
│   │   ├── Story/       InkLite (Ink parser + runner) · StoryLibrary (loads mohalla.txt + INCLUDEs) · StoryState (variables)
│   │   ├── Systems/     KarmaSystem + KarmaStatAsset (ScriptableObject) · TrustSystem · DialogueManager (Ink host)
│   │   │                ChoiceTags · MissionSystem · ProtestManager · ElectionManager · EndingEvaluator · Guidance · VoiceSystem
│   │   ├── Data/        MissionDatabase (6 chapters) · NpcDatabase (12 residents) · MiniGameData (waste, news, slogans, items)
│   │   ├── World/       WorldLayout (all coordinates) · WorldMap (tiles/blocking) · WorldBuilder (tilemaps, buildings, objects)
│   │   │                Interactable + StoryObject · AreaManager (barricades) · TimeManager · CameraRig (day/night + mood tint) · CityVisuals
│   │   ├── AI/          NPC (schedule brain) · NPCStates (FSM incl. FollowState for the march) · NPCManager · LoudspeakerRickshaw
│   │   │                Pathfinding/ AStar + NavGrid
│   │   ├── Player/      PlayerController
│   │   ├── Art/         PixelCanvas · SpriteFactory (tiles, characters) · .City (buildings, props) · .Icons (UI icons)
│   │   └── UI/          UIManager · UIFactory (+ Theme) · HudScreen · TouchControls · DialogueScreen · WhyItMattersScreen
│   │                    PhoneScreen · MapScreen · MapTexture · MainMenuScreen (+ PauseScreen)
│   │                    MiniGames/ WasteSortingScreen · FakeNewsScreen · SloganScreen · VotingScreen (+ EndingScreen)
│   ├── Resources/Stories/           the Ink story: mohalla.txt + m1_bijli_bachao … m6_election_mela (.txt = Ink)
│   ├── Editor/                      first-open scene setup + "Mohalla Hero" menu (validate story, karma assets, save tools)
│   └── Tests/
│       ├── EditMode/                InkLite, full story playthroughs, world reachability, logic
│       └── PlayMode/                boots the real game and plays it through the UI (optional screenshots)
├── Docs/                            DESIGN_DECISIONS · SETUP_GUIDE · UI_LAYOUT_PLAN · ASSET_LIST
├── Packages/  ProjectSettings/      Unity project files
└── README.md
```

The scripts named in the brief map to these classes:

| Brief | Class |
|---|---|
| GameManager | `GameManager` |
| KarmaSystem | `KarmaSystem` (+ `KarmaStatAsset`) |
| DialogueManager | `DialogueManager` (+ `InkLite`) |
| TrustSystem | `TrustSystem` |
| ProtestManager | `ProtestManager` |
| ElectionManager | `ElectionManager` |
| SaveSystem | `SaveSystem` |

## Tests

* **EditMode** (31 tests):
  * The Ink reader.
  * Two complete playthroughs of all six chapters through the real story files: the best path must reach *Mohalla Hero*, and a bad-choices-first path must still finish without soft-locks and with a much lower rating.
  * Every big karma choice must have a `#why`, and every page at most 4 choices.
  * The map: gates seal their areas, everything is reachable, nothing overlaps.
  * Karma, trust, the rating, endings and the mini-games.
* **PlayMode** (1 test): starts the actual game and plays it through its UI: the intro, a mission conversation, world objects, all three mini-games, the phone, the map, the candle march, the Election Mela, the polling booth, the ending, then save and continue. Any exception fails the test.

Run them from **Window → General → Test Runner**, or see Docs/SETUP_GUIDE.md §4 for the command line.

## Relationship to Apna Gaon

This project sits inside the *Apna Gaon* folder but is a **separate Unity project**. Apna Gaon (the B.Tech village game in the parent folder) is untouched and still opens and plays as before. Mohalla Hero reused its engine: procedural pixel art, tilemaps, A* NPC schedules, voices and the code-built UI. The village systems (farming, fishing, wildlife, development) were replaced by the civic story systems.
