# Mohalla Hero: Design Decisions

The brief ("Mohalla Hero: Choices That Count") left many things open. This document records every decision taken while building the game. Keep it in sync with the code. The mission table mirrors `Scripts/Data/MissionDatabase.cs`, and every number lives in `Scripts/Core/Balance.cs`.

## 1. Big decisions

| Question | Decision | Why |
|---|---|---|
| New project or change Apna Gaon? | **A separate Unity project** (`Aapna Gaon/MohallaHero/`). It reuses Apna Gaon's engine: procedural pixel art, tilemaps, A* NPC schedules, OS voices and code-built UI. | Apna Gaon (the B.Tech project) stays untouched and playable. The new game reuses the parts that already worked. |
| "Make it like city UI" | A **city neighbourhood** ("Shanti Nagar", Ward 7) replaces the village. The UI is a **city-app** style: a top status bar, rounded "app cards" and a **Mohalla Phone** (smartphone menu) with apps. | Fits the urban civic theme, and phones are what players already know. |
| Ink or Dialogue System? | **Ink syntax, run by the built-in `InkLite` reader** (`Scripts/Story/InkLite.cs`). | The official ink-unity-integration needs a package download and its compiler. InkLite needs nothing and is unit-tested. Every story file is valid Ink, so the project can switch to the official runtime later (see SETUP_GUIDE.md). |
| ScriptableObjects for karma | Each stat is a `KarmaStatAsset` ScriptableObject (name, Hindi name, colour, description, start XP, live XP). Defaults are created in code; **Mohalla Hero → Create Karma Stat Assets** writes editable assets to `Resources/Karma`, which the game then loads. | Meets the spec without breaking the "everything is built from code" rule: the game runs with zero assets. |
| Art | Everything is drawn procedurally (`SpriteFactory*`). Docs/ASSET_LIST.md lists free packs that can replace it. | No licensing problems, and it works offline for a college demo. |
| Platforms | Desktop (keyboard) and mobile (touch: joystick plus buttons, landscape only). | The spec asks for a mobile-friendly UI. |
| Real politicians | None. Netaji Jugaad Singh, Dr. Vaada Verma and Meera Didi are fictional. Party symbols are a Rocket, a See-saw and a Lantern (not real party symbols), and no real party colours or names are used. | The spec forbids real politicians. |
| Who wins the election? | **Not the player's vote.** It is secret and never judged. The result depends on the Mohalla Rating and on how many voters the player informed. | Teaches that one vote matters, but informing others matters more. |

## 2. Story (6 chapters, linear)

| Ch | Mission | Giver | Steps (story variable) | Teaches |
|---|---|---|---|---|
| 1 | **Bijli Bachao** | Asha Ma'am | 4 wasted lights/fans (`lights_off`), 3 leaking taps (`taps_closed`), 3 saplings (`saplings`), 2 garbage heaps plus the waste-sorting game (`piles_cleaned`), report back (`m1_done`) | Saving electricity and water, planting, sorting waste. Wasting costs points. |
| 2 | **Bully Buster** | Pintu | Stop the bullying safely (`m2_stopped`), report it (`m2_reported`), support Golu (`m2_supported`), talk to Bunty (`m2_done`) | Calm intervention, involving a trusted adult, reporting, supporting the victim, and letting the bully change. |
| 3 | **Imandar Bazaar** | Sharma Aunty | Buy groceries (`m3_bought`, with extra change to return or keep), the "Lucky Offer" scam stall (`m3_lucky`), the ration queue tout (`m3_ration`), deliver (`m3_done`) | Returning extra change, refusing scams and OTP sharing, not bribing to skip queues |
| 4 | **Sach ki Awaaz** | Meera Didi | Photo of the "Renovation complete" board (`item_photo`), the golden-dustbin bill (`item_receipt`), an RTI reply the next day (`item_rti`), 4 residents convinced (`joined`), report to Meera (`m4_done`) | Evidence, RTI, honest persuasion |
| 5 | **Shanti March** | Meera Didi | 3 placards via the slogan game (`placards`), gather in the park (`m5_gathered`), candle march through 4 diya checkpoints (`march_checkpoints`), petition to the officer (`m5_done`) | Peaceful protest. Violence is heavily penalised. |
| 6 | **Election Mela** | Officer Fernandes | Visit 3 stalls (`stalls_visited`), the street play (`mela_play`), fact-check with Chacha (`rumours_checked`), inform 5 voters door to door (`voters_informed`), vote (`m6_done`) | Informed, unbribed and secret voting; spotting rumours |

The current chapter is the first chapter whose `doneVar` is 0. Chapter 7 means the election is over.
Story files live in `Resources/Stories`: `mohalla.txt` (main: variables, intro, character routing, chats, world objects) INCLUDEs `m1_…` to `m6_…`.

### Branches and consequences that carry over

* **Returning Lala ji's extra ₹100 (Ch3)**: Lala trusts you (+15). In Ch4 he hands over the fake bill himself and joins the march (+25). If you kept the money, you dig the bill out of the raddi pile behind Netaji's office instead.
* **How you stopped the bullying (Ch2)**: pushing Bunty (`m2_fought`) changes later dialogue (Asha, Bunty) and gives Ch4 a different way to win Bunty over (an apology).
* **The doctored photo (Ch4)**: an edited park photo lowers trust when you show it ("yeh toh app se lagaya hua lagta hai").
* **Netaji's bribe (Ch4)**: refusing gives Courage and Honesty, accepting costs Honesty −15 and Civic −10, and reporting him is the third option.
* **The march incident (Ch5)**: joining the stone-throwing counts as **violence** (−25 rating) and weakens the petition (the officer comments on it).
* **Door to door (Ch6)**: only honest information counts as "informed". Pressuring people or spreading rumours closes the door without informing anyone.
* **Soft-lock protection**: every persuasion conversation has a repeatable "patient, honest" option (+5 trust), so a player who lied early can still recover by being honest consistently.

## 3. Karma, trust and the rating

* **Karma stats**: Civic Sense, Green Score, Courage and Honesty, each starting at 10 XP and never below 0. Each has a level (1–5, one per 40 XP) with a fun rank title such as "Prithvi ka Dost" or "Raja Harishchandra 2.0".
* **Choice tags** in Ink: `#civic:+5 #green:-3 #courage:+2 #honesty:+10`, `#trust:+15` (the person you're talking to), `#trust_raju:+10`, `#why:…` and `#violence`.
* **Why it matters**: every choice that moves karma by 5 or more carries a `#why` (a test enforces this). The card shows the choice, the stat chips and the explanation.
* **Trust**: 0–100 per resident (stored as `trust_<id>` story variables). 60 or more means they join the march, and 4 of the 6 residents (Sharma, Raju, Lala, Chacha, Asha, Bunty) are needed. Evidence gives +15/+20, honest appeals +10, lies −20, threats −15.
* **Mohalla Rating (0–100, 1–5 stars)**:
  * 70 × the average karma share, where each stat's XP is divided by its cap (Civic 150, Green 75, Courage 90, Honesty 75; a near-perfect run reaches about the cap).
  * Plus 10 × the share of supporters, plus 20 × the share of informed voters (10 doors).
  * Minus 25 per act of violence.
* **Endings**:
  * The winner is **Meera** with 5+ informed voters and rating ≥ 55, **Dr. Vaada** with 3+ informed voters or rating ≥ 45, and **Netaji** otherwise.
  * Meera winning gives **Mohalla Hero!** (rating ≥ 75) or **Jagruk Nagrik**. Vaada winning gives **Aadha Sach**. Netaji winning gives **Jugaad Raj Returns**.
  * Tests check that the best path reaches *Mohalla Hero* and that bad choices give a much lower rating.

## 4. The world reacts (`CityVisuals`, `CameraRig.Tint`)

| Rating | Change |
|---|---|
| < 70 | A grey-brown haze over everything, strongest at 0 |
| < 45 | Dim, washed-out building facades |
| < 40 | Every other street light is broken at night |
| potholes | Disappear one by one as the rating rises (25 + 12 per pothole) |
| ≥ 60 | Graffiti and Netaji posters become murals |
| ≥ 70 or Election Mela | Festival bunting over the streets |
| Hero / Jagruk ending | Gulmohar Park's swings are repaired |

Fixed lights, closed taps, planted trees and cleared garbage stay visibly fixed (`obj_*` variables).

## 5. Mini-games

| Game | Where | Rules | Reward |
|---|---|---|---|
| Waste sorting | Any garbage heap | 8 random items out of 14, three bins (Gila/Sukha/Khatarnak), an explanation per item | Green +1 per correct item, Civic +2 if at most 1 is wrong |
| Fake-news spotting | Chacha's Mela stall | 6 of 9 forwards (some real!), with an optional "check source" clue | Civic +1 per correct call, Honesty +3 if at most 1 is wrong |
| Slogan making | Community Hall table | Opening plus ending. Peaceful or funny slogans become placards, angry ones are accepted with a warning, violent ones are refused (Civic −5). 3 different placards are needed. | Civic and Courage per placard |

Mistakes in the mini-games never cost points; they explain instead. Only choices cost points.

## 6. Systems and controls

* **Time**: a day runs 07:00 → 23:00 in about 9.6 real minutes. At 23:00 you go home automatically. Resting at your door skips to the next morning, which the RTI reply needs, and saves. The clock stops whenever a menu or dialogue is open.
* **Saves**: a single JSON file (`mohallahero_save.json`). It stores only the story variables that differ from their defaults, plus karma, money, position, time, unlocked areas and the result. It saves on every chapter change, on rest, on F5 and when the app is paused on mobile.
* **Guidance for young players**:
  * "!" over anyone with something for you.
  * A golden diamond over the next goal, with an arrow at the screen edge when the goal is off-screen.
  * Orange dots on the map.
  * Pintu's hint (H / PINTU button).
  * A tracker in the top bar.
* **Voices**: the OS text-to-speech, each character with their own voice. The dialogue text is the subtitle, and ambient speech (loudspeaker, crowd) has a separate subtitle bar that can be switched off.
* **Mobile**:
  * A floating joystick (push to the edge to run) and ACTION/PHONE/MAP/PINTU/menu buttons.
  * Touch controls appear automatically on touch screens.
  * Big choice buttons, an optional bigger dialogue text and landscape-only orientation.
  * The UI scales from 1920×1080.
* **Escape** can leave a real conversation only before the first choice, so a decision is never cut off half-way.

## 6a. Growing up with the city calendar

* **Decision:** age follows the city calendar only. The calendar is the city's day count plus a story time-skip at the start of each chapter. Playing for minutes never ages the player; sleeping (a new city day) and new chapters do.
* **Timeline:** arrive on Monday 3 June 2024 (Din 1), aged 11. Cumulative skips at the start of chapters 2–6: 399, 798, 1197, 1897 and 2646 days (whole weeks, so weekdays stay consistent), which makes the player 12, 13, 14 (Sach ki Awaaz), 16 and 18. The Election Mela is the player's first vote, so the vote is legal.
* **Stages:** Bachpan (<13, ages 11-12), Kishor/teen (13–17), Naujawan (18–29), adult (30–59), Buzurg (60+). The body grows every birthday until 18. Adult and elder looks appear in the ending's *Saalon baad* epilogue (ages 30, 45 and 65, worded by the ending).
* **Story touches:** a "N mahine baad…" fade with the date between chapters, birthday and milestone toasts (teen at 13, board exams at 16, voter at 18), and one age-aware greeting per resident for each stage. The six-chapter story itself is unchanged.
* **Saves:** version 2 stores the skip, years, stage and greetings. Version 1 saves get the skip that belongs to their chapter, so nobody reloads younger than the story says.

## 6b. Yoga Kendra (wellness activity, outside the story)

* A building in the south of the mohalla (tiles 41,5, 5×3; door 43,4), open from the start. The instructor is **Yogacharya Shanti Devi** (fictional).
* She asks three questions: type (Beginner, Relaxation, Flexibility, Full Body), duration (5, 10, 15, 20 or 30 real minutes) and experience (longer holds).
* **"Guru Ji kehte hain" (Simon Says):** she demonstrates each pose (4 s) and explains it; the player joins with KARO (6 s window) and holds it with a timer ring and breathing cues. Flexibility and Full Body sessions include trap instructions without "Guru Ji kehte hain", which the player should ignore. Nothing is lost by missing one.
* 12 poses, from Tadasana to Sukhasana. Each later round holds 2 s longer, and the closing rest stretches so the session ends exactly on time.
* **Daily rule:** from Monday to Saturday one session of at least 5 minutes is compulsory each city day (Din 1 is a Monday, Din 7 a Sunday, and so on). Until it is done, resting at home to skip the night is refused with a reminder, and the HUD shows "Aaj ka yoga: baaki / ho gaya". A day that ends without yoga (for example at 11 PM) resets the streak but costs no karma. Sunday is a rest day; sessions that day are an optional bonus.
* **Rules:** the screen is modal, so city time, NPCs, karma, money and the story are untouched (only the yoga streak is saved). Generated tanpura-style music and chimes play. The completion card shows time practised, poses completed and Simon Says traps caught, with options to repeat or go back to the city.

## 7. Map (96 × 64 tiles)

* **Main Road** (y 28–31) with sidewalks and zebra crossings, plus **Gali No. 1/2** and three vertical streets.
* Top half:
  * Gulmohar Apartments (your home and Sharma Aunty's), the school with its playground and Gulmohar Park (hedge, pond, broken swings, garbage, renovation board).
  * The Ward Office, Seva Clinic and notice board.
  * A row of houses (homes of residents and 10 voters).
* Bottom half:
  * Chacha's tea stall with the auto stand.
  * **Bazaar Gali** (walled; it opens in Chapter 3): Lala Kirana, the Lucky Offer stall and the Ration Office.
  * The party offices (Jugaad, Vaada) with the raddi pile, and the Community Hall (placard table).
  * The **Mela Maidan** (walled; it opens after the petition): three candidate stalls, Chacha's stall, the nukkad stage, a ferris wheel and the polling booth.
* Tests flood-fill the map to check that gates seal their areas, that everything is reachable once opened, that nothing overlaps, and that A* prefers streets.
