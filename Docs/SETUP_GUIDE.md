# Mohalla Hero: Setup Guide for Beginners

This guide assumes you have never used Unity before. Every step is a click or a key press.

## 1. Install Unity (one time, about 30 minutes)

1. Download **Unity Hub** from <https://unity.com/download> and install it.
2. Open Unity Hub and sign in with a free Unity account. The Personal licence is free.
3. Go to **Installs → Install Editor** and choose **Unity 6** (6000.0 LTS or newer). This project was made with **6000.6.3f1**; any Unity 6 version works.
4. In "Add modules", tick what you need:
   * **Android Build Support** (with OpenJDK and Android SDK & NDK), to run the game on an Android phone.
   * **iOS Build Support**, only on a Mac and only for iPhones.
   * Nothing extra is needed to play on your computer.

## 2. Open the project

1. In Unity Hub, click **Add → Add project from disk**.
2. Select the folder **`Aapna Gaon/MohallaHero`** (the folder that contains `Assets`, `Packages` and `ProjectSettings`).

   > Don't select `Aapna Gaon` itself: that is the separate *Apna Gaon* project.
3. Click the project to open it. The first time, Unity imports everything, which takes 2–5 minutes.
4. An editor script creates **`Assets/Scenes/Main.unity`**, adds it to the build and opens it. If that didn't happen, use the menu **Mohalla Hero → Setup Main Scene**.
5. If Unity asks about "the new Input System" and restarting, either answer is fine. The game supports both.

## 3. Play

1. Press the **Play** button (▶) at the top of the editor.
2. Click **New Game**, type a name, choose Ladka/Ladki and click **Chalo!**
3. Controls:

| Action | Keyboard | Touch (phone) |
|---|---|---|
| Walk / run | WASD or arrows, Shift to run | Joystick (bottom-left); push it to the edge to run |
| Talk / use | **E** or Space | **ACTION** button |
| Pick a choice | 1–4 (E for the first) | Tap it |
| Mohalla Phone (missions, karma, evidence, trust, settings) | Tab | **PHONE** |
| Map | M | **MAP** |
| Pintu's hint | H | **PINTU** |
| Pause | Esc | Gear button (top) |
| Quick save | F5 | Phone → Save |

The game saves automatically on every new chapter, every time you rest at your door, and when the app goes to the background on a phone.

## 4. Run the tests (optional)

* **Window → General → Test Runner**.
  * **EditMode**: 31 tests, including two complete playthroughs of the story through the real Ink files.
  * **PlayMode**: boots the real game and plays it through its UI.
* From a terminal (close the Unity editor first):

  ```
  <Unity> -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml
  <Unity> -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults results.xml
  ```

  On a Mac, `<Unity>` is `/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity`.
* To also get screenshots of every step of the PlayMode run, set the environment variable `MH_SHOTS=/some/folder`.

## 5. Build for a phone or a computer

1. **File → Build Profiles** (or **Build Settings** in older Unity 6 versions).
2. Choose a platform:
   * **Windows / macOS**: press **Build**, then pick a folder.
   * **Android**:
     1. Click **Switch Platform**.
     2. Plug in your phone with USB debugging enabled (Settings → About → tap "Build number" 7 times → Developer options → USB debugging).
     3. Press **Build And Run**.
   * **iOS** (Mac only): **Switch Platform**, then **Build**. Open the generated Xcode project and run it on your iPhone.
3. The game is landscape-only and scales its UI for any screen. Touch controls appear automatically on phones.

## 6. Change the story (no programming needed)

All dialogue is in **Ink** files in `Assets/MohallaHero/Resources/Stories/`. They use the `.txt` extension so that Unity loads them as text.

* `mohalla.txt`: variables, the intro, who says what in each chapter, everyday chats, world objects (lights, taps, saplings, garbage, your home)
* `m1_bijli_bachao.txt` … `m6_election_mela.txt`: one file per chapter

A line looks like this:

```ink
Asha Ma'am: Bill toh school bharega? School humara hai, beta!
* [Switch off karo] #green:+4 #civic:+2 #why:A tubelight left on all year wastes about ₹1,000...
    ~ lights_off += 1
* [Chhodo, mera bill thodi hai] #green:-3 #why:Public buildings are paid for with everyone's taxes...
```

* `Name: text` is a spoken line. `You:` is the player.
* `* [choice]` is a choice that disappears once picked; `+ [choice]` stays.
* Tags after a choice change karma: `#civic` `#green` `#courage` `#honesty`.
  * `#trust:+15` changes the trust of the person you are talking to.
  * `#why:` is the "Why it matters" card. Every big karma choice needs one; a test checks this.
  * `#violence` is a heavy rating penalty.
* `~ variable = 1` sets story progress. `{condition: -> knot}` jumps.

After editing, use **Mohalla Hero → Validate Story (Ink)** to check for mistakes, then run the EditMode tests.

**Tip:** the free Ink editor **Inky** (<https://github.com/inkle/inky/releases>) gives syntax colouring and a preview window. Open the `.txt` file directly.

## 7. Tweak karma with ScriptableObjects

1. Use the menu **Mohalla Hero → Create Karma Stat Assets**.
2. Four assets appear in `Assets/MohallaHero/Resources/Karma/` (Civic, Green, Courage, Honesty).
3. Click one and edit its name, Hindi name, colour, description or starting XP in the Inspector. The game uses them automatically.
4. Delete them to go back to the built-in defaults.

All other numbers (speeds, thresholds, rating weights, how many voters to inform…) are in `Assets/MohallaHero/Scripts/Core/Balance.cs`.

## 8. Optional: switch to the official Ink runtime

The story files are standard Ink, so you can use inkle's own runtime instead of the built-in InkLite reader:

1. **Window → Package Manager → + → Add package from git URL**, then paste `https://github.com/inkle/ink-unity-integration.git#upm`.
2. Copy the `.txt` files to `.ink` files. The plugin compiles them to JSON automatically.
3. In `DialogueManager`, replace `InkRunner` with `Ink.Runtime.Story`. The API maps one-to-one:

   | InkLite | Ink runtime |
   |---|---|
   | `runner.Next()` | `story.Continue()` / `story.currentChoices` |
   | `runner.Choose(i)` | `story.ChooseChoiceIndex(i)` |
   | `IStoryHost.Call` | `story.BindExternalFunction("name", …)` |

## 9. Troubleshooting

| Problem | Fix |
|---|---|
| Pressing Play shows nothing | Check the Console (Window → General → Console) for red errors. Use **Mohalla Hero → Setup Main Scene**. |
| No voices | Voices use the computer's text-to-speech (macOS `say`, Windows Speech). They are off on other platforms. Toggle them in Phone → Settings. |
| "Continue" is greyed out | No save yet. Use **Mohalla Hero → Open Save Folder** to see the save, or **Delete Save File** to start fresh. |
| Touch controls on a laptop with a touchscreen | Phone → Settings → Touch controls: Auto / On / Off |
| Text too small on a phone | Phone → Settings → Bigger dialogue text |
