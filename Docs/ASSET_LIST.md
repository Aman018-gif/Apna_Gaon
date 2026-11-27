# Mohalla Hero: Asset List (free sources)

**The game ships with zero imported assets.** Every sprite, tile, icon and backdrop is drawn in code (`Scripts/Art/SpriteFactory*.cs`), voices use the operating system's text-to-speech, and text uses Unity's built-in font. That keeps the project license-clean and offline.

The table below lists free, legally reusable assets you can swap in for a more polished look. Each `SpriteFactory` method returns a `Sprite`, so replacing art means returning a loaded sprite (for example `Resources.Load<Sprite>("Art/school")`) instead of the drawn one.

> Always re-check a licence on the asset page before shipping. CC0 needs no credit, CC-BY needs credit in the game's credits, and some "free" itch.io packs forbid redistribution of the raw files.

## Art (top-down 2D)

| What it replaces | Suggested free source | Licence | Notes |
|---|---|---|---|
| City tiles: roads, sidewalks, zebra crossings | **Kenney: "Roguelike City Pack"**, **"RPG Urban Pack"** (kenney.nl) | CC0 | Top-down 16 px tiles, a close match to the current 16 px grid |
| Buildings, shops, apartments | Kenney "RPG Urban Pack"; **"Modern exteriors" free demo** by LimeZu (itch.io) | CC0 / free demo licence | LimeZu's style is closest to an Indian street; check the demo terms |
| Characters (12 residents + player) | Kenney "Toon Characters 1"; **"Ninja Adventure"** by Pixel-boy & AAA (itch.io) | CC0 | Recolour clothes for kurta, saree and uniform; the 3-direction × 2-frame walk fits `NPC.Animate` |
| Park: trees, benches, pond, flowers | Kenney "Tiny Town"; OpenGameArt **"LPC Base Assets"** | CC0 / CC-BY-SA 3.0 | LPC needs attribution, and its share-alike terms apply |
| Festival props: bunting, diyas, stalls, ferris wheel | OpenGameArt search "market stall", "lantern", "carnival" | various (filter CC0) | Keep the bright palette from UI_LAYOUT_PLAN.md |
| Auto-rickshaw | OpenGameArt "tuk tuk"/"auto rickshaw" pixel art; or keep the procedural one | various | |
| UI icons: karma, apps, evidence | **game-icons.net** | CC-BY 3.0 | Huge set (scales, leaf, shield, megaphone, ballot box); credit the authors |
| UI panels and buttons | Kenney **"UI Pack"** and **"UI Pack: RPG Expansion"** | CC0 | Rounded panels match the city-app look |
| Waste items (banana peel, battery…) | Kenney "Food Kit" 2D icons; game-icons.net | CC0 / CC-BY | |
| EVM / VVPAT | Keep the procedural UI. | n/a | Avoid photos of real election material, logos or real party symbols |

## Fonts (Hindi-friendly, for a Devanagari version)

| Font | Source | Licence | Use |
|---|---|---|---|
| **Baloo 2** | Google Fonts | OFL | Playful headings ("MOHALLA HERO"), supports Devanagari and Latin |
| **Mukta** | Google Fonts | OFL | Body text and dialogue; very readable on phones |
| **Noto Sans Devanagari** | Google Fonts | OFL | A fallback for full Hindi text |

To use one: import the `.ttf` into `Assets/MohallaHero/Resources/Fonts/` and make `UIFactory.Font` load it (`Resources.Load<Font>("Fonts/Mukta-Regular")`).

## Audio (the game is silent apart from voices; these would add life)

| Sound | Source | Licence |
|---|---|---|
| Street ambience, horns, chai stall, crowd chants | **freesound.org** (filter: CC0) | CC0 / CC-BY |
| Festival music (dhol, shehnai loops) | **Pixabay Music**; **incompetech.com** (Kevin MacLeod) | Pixabay licence / CC-BY |
| UI clicks, karma "ding", wrong-answer "buzz" | Kenney **"Interface Sounds"**, **"UI Audio"** | CC0 |
| EVM beep | Kenney "Digital Audio" or a 0.6 s 2 kHz sine (generate in Audacity) | CC0 |

## Voices

* **Current**: OS text-to-speech (macOS Rishi, Aman, Tara and Lekha voices, which also read Hinglish; Windows System.Speech).
* **Better (free)**: record classmates! A smartphone and **Audacity** (free) are enough. Name files by knot and line, then play them from `DialogueManager` instead of `VoiceSystem.Say`.
* **Cloud TTS**: services such as Google Cloud TTS (hi-IN/en-IN voices) have free tiers, but need an account and an API key. Don't ship keys inside the game.

## Story tooling

| Tool | Source | Licence |
|---|---|---|
| **Inky**, the Ink editor with preview | github.com/inkle/inky | MIT |
| **ink-unity-integration**, the official runtime (optional) | github.com/inkle/ink-unity-integration | MIT |

## Palette reference

* **Lospec palette list** (lospec.com/palette-list). Search "festival" or "vibrant"; the game's colours are listed in UI_LAYOUT_PLAN.md.

## Content rules for any replacement asset

* No real politicians, real party names, symbols, flags or colours used as party branding. The game's parties are Rocket, See-saw and Lantern.
* No religious symbols tied to a party.
* Keep everything suitable for all ages.
