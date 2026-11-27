# Mohalla Hero: UI Layout Plan

**Style**: "city app". The interface looks like the apps young players already use: rounded cards, a status bar and a smartphone menu, in a bright Indian-festival palette. All UI is built from code (`Scripts/UI`) with uGUI. The reference resolution is **1920×1080**, scaled with *Scale With Screen Size* (match 0.5), so it fits phones, tablets and monitors. The game is landscape only.

## Palette (`Theme` in `UIFactory.cs`)

| Role | Colour | Hex |
|---|---|---|
| Panels (night-indigo) | ![](https://placehold.co/16x16/2b1255/2b1255) | `#2B1255` (95% opaque) |
| Accent: marigold | ![](https://placehold.co/16x16/ffc20e/ffc20e) | `#FFC20E` |
| Saffron (buttons, wallet) | ![](https://placehold.co/16x16/ff7f11/ff7f11) | `#FF7F11` |
| Magenta (headers, primary buttons) | ![](https://placehold.co/16x16/e5157a/e5157a) | `#E5157A` |
| Teal (positive, trust, "Why" card) | ![](https://placehold.co/16x16/00a6a6/00a6a6) | `#00A6A6` |
| Cream cards | ![](https://placehold.co/16x16/fff8e7/fff8e7) | `#FFF8E7` |

Each karma stat has its own colour: Civic teal, Green leaf-green, Courage saffron and Honesty magenta-pink.

## Language and text

* Hinglish everywhere ("Samajh gaya!", "Saboot dikhao"), with English explanations in the "Why it matters" cards so every age group follows.
* Body text is 26–30 px at the reference resolution and up to 34 px with **Bigger dialogue text**.
* Buttons are at least 64 px tall and 74–90 px for touch.
* **Subtitles**:
  * Every spoken line is shown in the dialogue box.
  * Ambient speech (loudspeaker rickshaw, crowd chants, Pintu's hints) appears in a subtitle bar above the bottom HUD. Settings → Subtitles can switch it off.

## 1. HUD (always on)

```
┌──────────────────────────────────────────────────────────────────────────────────────┐
│ [pin] School Area        │ CHAPTER 1: BIJLI BACHAO                  │ Din 1 · 9:00 AM ₹200 │  top bar
│       Shanti Nagar·Ward 7│ Switch off wasted lights & fans (1/4)    │                      │
├──────────────────────────┴──────────────────────────────────────────┴──────────────────┤
│ (C) Civic Sense 15          +5 Civic Sense                          ┌ MOHALLA RATING ─┐ │
│     Level 1 · Naya Padosi ▬▬▬                                       │ ★★☆☆☆   34/100  │ │
│ (G) Green Score 22                                                  │ ┌─────────────┐ │ │
│ (!) Courage 10                                                      │ │  minimap    │ │ │
│ (=) Honesty 10                                                      │ └─────────────┘ │ │
│                                                                     └─────────────────┘ │
│                                     ◆  (objective marker over the target)              │
│                                                                                         │
│                       ┌──────────────────────────────────────┐                           │
│                       │ [E]  Bulb jal raha hai (switch off?) │   interaction prompt      │
│                       └──────────────────────────────────────┘                           │
│                                    WASD chalo · Shift daudo · E baat · Tab phone · M map · H hint │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

* **Karma chips**: an icon, the XP, the level and rank title, and a progress bar. Changes float up as "+5 Civic Sense".
* **Mohalla Rating card**: 1–5 stars, the score out of 100 and a minimap. On the minimap, red is you, yellow is a resident and orange is someone with work for you.
* **Guidance**:
  * a golden diamond over the next objective, plus an edge arrow when it is off-screen;
  * "!" bubbles over people with something for you.
* **Notifications** (toasts and big chapter banners) wait while any window is open, so they never cover it.

## 2. Touch controls (phones; auto-detected)

```
│                                [⚙]                                                     │
│                                                                                         │
│                                                                    (MAP)                │
│   ╭──────╮                                                   (PINTU)                    │
│   │  ●   │  floating joystick                                         ╭────────╮        │
│   ╰──────╯  (edge = run)                                   (PHONE)    │ ACTION │        │
```

* The joystick appears wherever the thumb lands in the lower-left 40% of the screen.
* ACTION is 190 px; the other buttons are 110–120 px.
* Buttons send virtual key presses through `GameInput`, so every screen works the same with touch and keyboard.

## 3. Dialogue (bottom 37% of the screen)

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀ marigold stripe ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀│
│ ┌────────┐  ┌ Asha Ma'am  School Teacher ┐                                        │
│ │portrait│  Is mahine school ka bijli ka bill ₹18,000    ┌ 1. Main madad karunga! ┐ │
│ │        │  aaya! Khaali classrooms mein pankhe...       ├ 2. Bill toh school...  ┤ │
│ └────────┘                                               └ 3. Kya paise milenge?  ┘ │
└─────────────────────────────────────────────────────────────────────────────────┘
```

* The name plate is magenta for residents and teal when *you* speak. Narration has no plate and is set in italics.
* Each page shows 2–4 choices (tests enforce this). Use keys 1–4, E for the first, or tap.
* A single-option page shows one big saffron button ("Aage", "Theek hai").

## 4. "Why it matters" card (after every karma choice)

```
            ┌──────────────────────────────────────────────────────┐
            │ (💡) Kyun zaroori hai? (Why it matters)        teal │  red header for violence
            │ You chose: "Switch off karo"                          │
            │ [+4 Green Score] [+2 Civic Sense] [-10 Raju's trust]  │  stat chips
            │ A tubelight left on all year wastes about ₹1,000...   │  explanation
            │ ┌──────────────── Samajh gaya! (E) ────────────────┐ │
            └──────────────────────────────────────────────────────┘
```

## 5. Mohalla Phone (Tab / PHONE)

```
        ┌────────────────────┐
        │ 9:00 AM ShantiNet 4G Din 1 ₹200 │
        │   MOHALLA PHONE    │
        │ (Missions)(Karma)(Saboot) │
        │ (Bharosa)(Map)  (Pintu)   │
        │ (Settings)(Save)(Result)  │
        │        ▭           │  home button
        └────────────────────┘
```

| App | Shows |
|---|---|
| Missions | All 6 chapters (done, now or locked), the steps of the current chapter with checkboxes, and each chapter's lesson |
| Karma | The Mohalla Rating, the 4 stats with rank, XP bar and description, and totals gained and lost |
| Saboot | Evidence and errand items with descriptions (and a warning if your photo was edited) |
| Bharosa | All 12 residents with a trust bar, a label ("Shak hai" … "Pakka saath") and SAATH HAI for supporters |
| Pintu | The current hint plus a meme of the day |
| Settings | Voices, subtitles, touch controls (Auto/On/Off), always-run and bigger text |
| Result | The ending screen, after the vote |

## 6. Full-screen windows

| Window | Layout |
|---|---|
| Map | The city texture at 13 px per tile, building labels, resident dots, the objective diamond and a legend |
| Waste sorting | The item card (icon and name) at the top, feedback, three bins (Gila green / Sukha blue / Khatarnak red), then Next |
| Fake news | A WhatsApp-style bubble with the sender and headline, a "Source check" clue, SACH / AFWAAH buttons, then an explanation |
| Slogan workshop | A placard preview at the top, two columns of fragments, feedback and a counter with placard icons |
| Polling booth | Officer and ID check, then the finger with an ink line that animates, then the EVM (4 rows with blue buttons and a red lamp) and the VVPAT slip, then "Vote ho gaya" |
| Ending | The winner card with symbol, the ending title and story, rating stars, 4 stat cards with ranks, choices made, and lessons |
| Main menu | A city skyline at dusk with bunting, "MOHALLA HERO", Continue / New Game / Quit, and the name and gender form |
| Pause | Resume, Phone, Map, Save, Voices, Save & Main Menu |

## 7. In-world UI

* Shop signboards and stall names sit on dark rounded nameplates (`WorldObjects.Label`), so they are readable on any wall.
* Bubbles: "!" (work for you), "z" (asleep) and a speech bubble on the loudspeaker rickshaw.
* Leaking taps drip, garbage has flies and lit bulbs glow, so problems are visible before you read anything.
