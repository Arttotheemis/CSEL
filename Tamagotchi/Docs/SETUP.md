# Project setup, gameplay and submission

## Open the completed project

In Unity Hub use **Add > Add project from disk**, then select `~/Downloads/Tamagotchi`. Open with Unity **6000.2.2f1**. Do not drag the entire folder into another project's Assets panel.

Load `Assets/Scenes/Monster.unity`, then press Play. In the Game panel select **Phone 1080 x 2400** and reduce the Scale slider until the entire screen fits. The layout preserves the background proportions and fits into device safe areas. Select the 2D toggle in Scene view to inspect the Canvas comfortably.

The Editor menu **Monster Simulator > Build Scene** regenerates sprites, clips and the scene; it replaces scene customizations. **Apply 1080 x 2400 Phone Layout** updates the existing layout and saves it. The latest layout also applies once automatically after script reload, outside Play Mode.

## Asset integration and scene wiring

All source art is kept intact. Sheets use Sprite (2D and UI), Multiple, Point filtering, no mipmaps and no compression. The character sheet remains readable for Editor normalization. Background.png is a single sprite. Manual import instructions are in [Unity's Sprite Editor reference](https://docs.unity.com/en-us/engine/6000.0/manual/unity2d/sprite/use-editor).

| Asset | Assignment |
| --- | --- |
| Action-Buttons.png | Top left Play, top right Sleep, bottom left Study, bottom right Eat. Entire illustration/label is included in each slice. |
| Animal.png (requested originally as Animal.jpg) | Top row groups: Idle, Happy, Sad. Middle row middle group: Eating. Bottom row groups: Playing, Studying, Sleeping. Each clip has three distinct sprite frames, looping at 6 FPS. |
| Background.png | Full-screen room art with aspect-preserving cover scaling. |
| Frame-Bar.png | Lightning = Stamina; book/paw = Study; bowl = Food; hearts = Happiness. PixelMeterFill renders the tinted fill, stepped caps and highlights inside each decorated frame. |
| Fonts/PressStart2P-Regular.ttf | All live UI labels, messages and guessing choices. OFL.txt contains the font license. |

`MonsterSceneBuilder` slices the sheets, creates persistent Button OnClick events, builds the Animator and saves the scene. Each Animator clip animates **Image.sprite** and the Image RectTransform's size/offset. Visible head width is normalized to 300 design units and opaque feet/props sit on the same floor. Thus different transparent padding and desks/balls cannot shrink or stretch the character. The original poses remain distinct: a curled sleeping pet naturally has a lower silhouette than a standing pet.

Canvas is Screen Space - Overlay with a Canvas Scaler reference of 1080 x 2400. PhoneViewport fits the design as a whole. Monster has Image + Animator with seven states on Base Layer. No transitions or parameters are required: the controller selects the state when it changes.

MonsterManager holds MonsterController. Its references connect all four fills, four Legacy Text value labels, four buttons, the message text, guessing choices and Animator. The four action Buttons have exactly one matching persistent listener each. Left and Right inside MessagePanel/GuessingChoices call ChooseLeft and ChooseRight. Non-interactive Images and labels have Raycast Target disabled. The EventSystem uses StandaloneInputModule.

## Current game rules

All stats clamp to 0..100. Defaults are Stamina 100, Happiness 75, Food 75, Study 0.

| Event | Result |
| --- | --- |
| Play | -2 Stamina, -1 Food, **+0.5 Happiness**; opens a left/right guessing round. |
| Study | -2 Stamina, -1 Food, **-0.5 Happiness** once; +10 Study only for a correct quiz answer. |
| Eat | +15 Stamina, +25 Food, +5 Happiness. |
| Sleep | Restores Stamina to 100 immediately, then plays a 3-second rest. |
| Every elapsed game second | **-0.01 Happiness**, using Update and Time.deltaTime. |
| Every full 60 seconds | -1 Food and -1 Stamina; stamina loss is 2 when Food was already empty. No launch-time food/energy drain. |

Play and Study are enabled at **20 or more** stamina. Sleep is available only **strictly below 20**. Validation also exists in the public methods. Eat lasts 2 seconds; Study allows 12 seconds for an answer and then shows a 2.5-second result. Play allows up to 8 seconds for a guess; a guess or timeout produces a 2-second result. Other actions cannot interrupt a round. Each round resolves once. Play's +0.5 happiness happens once at round start, with no extra happiness award for guessing correctly.

Values remain floats internally to preserve the assignment. UI values round to whole numbers, and fill amounts use those same displayed values. For example, tiny passive happiness changes do not make text flicker each frame. Exact 100 happiness selects Happy; <= 50 selects Sad. These mood thresholds take priority over action clips, preserving the assignment. Happy can be brief because required passive decay resumes on the next Update.

Messages identify tiredness, hunger, sadness, actions and guessing results. Every message uses the layered wood/gold/parchment panel and animates in gently when its text changes. Sleep is a manual assignment action rather than an automatic real-world bedtime schedule.

Device builds autosave every 30 game seconds and save the four stats, guessing wins and partial care-minute timer on pause/quit. Editor Play sessions always start with Inspector values for repeatable grading. No offline losses are simulated; the partial minute timer is restored between sessions.

The care-meter/attention and guessing-game design is informed by [Bandai's Original Tamagotchi manual](https://tamagotchi-official.com/manual/toy/original/Original_webmanual.pdf). Illness, toilet care, ageing and evolution are outside the current four-action assignment. Font source: [Google Fonts Press Start 2P](https://github.com/google/fonts/tree/main/ofl/pressstart2p).

## Verify before recording

1. Enter Play with defaults. Food/Stamina do not drain during the first 59 seconds; at 60 seconds each loses one point. Happiness retains its exact required float decay while labels remain whole numbers.
2. Play and select Left or Right. Confirm one result, consistent sprite size, and control re-enabling. Let another round time out. Rapid extra guesses must not count twice.
3. Study asks a short two-choice addition/subtraction question and plays the desk animation. A correct answer raises Study progress by 10; a wrong answer or timeout shows the correct answer without awarding progress. Eat increases Food, energy and happiness. Observe the framed messages.
4. Outside Play Mode set starting stamina to 20: Play/Study work and cost two, taking stamina to 18. After the action, Play/Study are locked and Sleep is available. Set starting stamina to 19 to demonstrate Sleep immediately restoring 100. At exactly 20, Sleep is rejected.
5. Set happiness to 50 for Sad. Use an Eat action from at least 95 happiness to reach Happy at exactly 100. Inspect the clip in Animation preview if you need to see the full happy loop; the exact-100 gameplay state is brief by requirement.
6. Try several portrait phone sizes and wider Game views: the whole design should fit, with no clipped action buttons. Check both left/right touch targets on Android.
7. Restore defaults (100 Stamina, 75 Happiness, 75 Food, 0 Study) before submission.

Editor validation is available through CLI:

```sh
/Applications/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath "$PWD" -executeMethod MonsterSceneBuilder.Validate -quit -logFile Logs/validation.log
```

Run real Play Mode checks with the Editor closed (this runner exits after finishing; do not add -quit):

```sh
/Applications/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "$PWD" -executeMethod MonsterPlayModeChecks.Run -logFile Logs/playmode-check.log
```

The real-scene checks exercise button events, guessing choices, all seven sampled animation poses and their normalized sizing, whole-number labels and minute care ticks. `Builds/Previews/Monster-1080x2400.png` is a rendered visual reference.

## Build an Android APK

1. In Hub > Installs > Editor > Add Modules install Android Build Support, SDK & NDK Tools, and OpenJDK. Use Unity's bundled tools in Settings > External Tools.
2. File > Build Profiles > Add Build Profile > Android, then Switch Profile. Older Unity uses Build Settings > Android > Switch Platform.
3. Include `Assets/Scenes/Monster.unity` in the enabled Scene List.
4. Set Company Name to your name, Product Name to your game name, package identifier to lowercase `com.yoursurname.monster`, and orientation to Portrait. Unity 6 requires at least Android 6.0/API 23; use the minimum supported by both your Editor and device.
5. Select appropriate architecture (ARM64/IL2CPP for modern ARM64 phones). Disable Build App Bundle and Export Project when producing an APK. Keep the Editor-supported target API.
6. Build to `Builds/Android/SURNAME_MONSTER.apk`. Install/test on your device, including touch, guessing choices, safe areas and saved care. No APK is generated by the source-only validation steps above.

[Unity Android build reference](https://docs.unity.com/en-us/engine/6000.0/manual/platform-specific/android/building-and-delivering/build-process).

## Video and Git submission

Record as `SURNAME_MONSTER.mp4` (or the instructor's requested format). Demonstrate Play/guessing, Study, Eat, Sleep, stamina locks and mood states. Prepare Inspector starting values between takes to show restrictions and moods without waiting for long natural decay. Keep recordings in Recordings/ and APKs in Builds/; both are ignored by Git. Submit them through the class's required upload channel.

To preserve the existing GitHub repository's history, clone it and add this Unity project as a subfolder:

```sh
cd ~/Downloads
git clone https://github.com/Arttotheemis/CSEL.git CSEL-submission
cd CSEL-submission
git switch -c assignment/monster-simulator
mkdir -p Tamagotchi
rsync -av --exclude='Library/' --exclude='Temp/' --exclude='Obj/' --exclude='Logs/' --exclude='UserSettings/' --exclude='Build/' --exclude='Builds/' --exclude='Recordings/' --exclude='.git/' --exclude='.DS_Store' ../Tamagotchi/ Tamagotchi/
git status --short
git add Tamagotchi/Assets Tamagotchi/Packages Tamagotchi/ProjectSettings Tamagotchi/Docs Tamagotchi/README.md Tamagotchi/.gitignore
git diff --cached --stat
git commit -m "Add mobile virtual pet with care meters and sprite animations"
git push -u origin assignment/monster-simulator
```

Commit generated .meta files alongside their assets. Inspect staged files before committing; do not include private material or credentials. Authenticate through your GitHub account. Open a pull request if the class requires it. These instructions do not automatically commit or push anything.

## Audio

`GameAudio` contains GameAudioController and three child AudioSources: two for crossfaded background music and one for short effects. Main Camera has the scene's single AudioListener. `MonsterAudioSetup` imports music for streaming and effects as preloaded mono PCM, connects all nine cues, and removes the old Music/SFX text controls from the top of the HUD.

Default mix levels are **35% music and 90% effects**. Ordinary action cues use a stronger 0.78 gain; correct and incorrect result cues use 0.9. Wrong answers use the new two-part descending `PixelWrong.wav` error cue; correct answers keep Kenney's rising `powerUp3` chime. The original CC0 DURU chiptune track is retained at the higher music level. Sleep still softens the music, app suspension pauses audio, and message sounds are suppressed immediately after action cues to avoid double beeps. There are no in-game audio text controls; the component's Inspector exposes mix levels and the runtime volume setters remain available for future settings.

Sources and license copies are documented in `Assets/Audio/CREDITS.md`. For recording, make sure Unity's Game view Mute Audio option is off and the recorder captures system audio. Listen on the target phone's speaker/headphones to confirm the preferred volume. The real-scene checks cover music playback/crossfades, imported effects, removed header controls and the updated default mix.

## Fitted frame bars

Each fill is calibrated to its own dark cavity in the original Frame-Bar.png source coordinates. `PixelMeterFill` draws a yellow, blue, green or pink bar with pixel-stepped moving end caps, top highlights and a shaded lower edge. It inherits Image, so the existing whole-number fillAmount updates and controller references continue to work. The bars shrink geometrically as values fall; they do not crop a rounded sprite into a flat right edge. Fill geometry stays inside its calibrated slot and decorative artwork remains visible.


## Study questions and action sound sequences

Study uses the same framed two-choice panel as Play. It randomly chooses a small addition/subtraction question with non-negative whole-number answers. The buttons show the two candidate numbers; their listeners remain ChooseLeft/ChooseRight and the controller routes the tap according to the active round. Answers resolve once, overlapping actions are rejected, and the required -0.5 happiness and stamina cost happen once when Study starts. After 12 seconds, an unanswered question times out. A correct answer adds 10 Study (capped at 100); wrong/expired answers add none. Play switches the labels back to LEFT/RIGHT.

Eating now has four short retro pops, studying a distinct retro signal phrase, and sleeping three spaced descending tones. They use the CC0 Kenney Digital Audio pack. Quiz result cues stop any remaining action pulses. Correct and Wrong use distinct success/error sounds at equal result gain. The SFX mix level governs both action sequences and result cues.

`Builds/Previews/Study-Quiz-1080x2400.png` is captured from the actual scene during the Play Mode checks. Those checks cover correct, incorrect, duplicate and timed-out Study answers, numeric choice labels, exact happiness changes, and action sound pulse cancellation.

## Current pixel audio replacement

The active scene now assigns **PixelEat.wav, PixelPlay.wav, PixelStudy.wav, PixelSleep.wav and PixelWrong.wav**, rather than the earlier action/error OGG clips. These are adapted CC0 Chip Sounds recordings, with raw sources and license included. The sound-preparation script adds fades and normalizes levels; Sleep is transposed into a more audible register, and Wrong uses two falling negative notes. Runtime checks verify the actual clip names and imported sample levels for all five events.

Listen to `Builds/Previews/Pixel-Sound-Preview.wav` directly to compare the effects without background music or Unity's mute settings. Its order is Eat, Play, Study, Sleep, Wrong. The eating preview includes four pops and the sleeping preview three spaced tones. Music briefly dips when action/result cues fire. New audio preference keys reset the previously hidden saved mix to the current 35% music/90% SFX defaults once.

If an already-open Editor still displays older assignments, stop Play Mode and let Unity finish importing; the current layout upgrade applies the new cue mapping once after compilation. Open the saved Monster scene from this project. GameAudio's effects array should show PixelPlay, PixelStudy, PixelEat, PixelSleep, Correct, PixelWrong, Attention, Message, Select in that order. Make sure Game view Mute Audio is off.


## Supplied error-pop update

The active Wrong cue now references `Assets/Audio/SFX/soundshelfstudio-ui-error-pop-515668.mp3`, copied unchanged from the user's Downloads. Both Play and Study wrong answers/timeouts use it through the shared cue. Runtime checks assert this exact clip name. Music is raised from 35% to 45%, with a new music preference key so the old saved level does not mask the change. Effects remain at 90%. The prior sound-comparison WAV contains the older generated wrong-answer effect.


## Audit improvements

Play and Study show a whole-second countdown in the same parchment frame. The separate timer label updates without replaying the message popup/sound each second. Application pause freezes care ticks, quiz deadlines and answer input, then resumes them together. Stamina uses whole-number floor display so 19.99 energy cannot look like 20 while Sleep is enabled; happiness retains its slow rounded display and exact float-based assignment logic.

Device builds autosave every 30 game seconds as well as on pause/quit. Saves now include the partial minute timer, so reopening cannot indefinitely postpone hunger/energy decay. Legacy saves remain supported; malformed, incomplete and unsupported future save data are rejected without replacing current stats. Data round trips are checked in Editor without touching device saves. No offline decay is added.

Care alerts raised during an action are delivered when that action finishes. Correct Study answers award the actual remaining capacity and show a full-meter message at 100. Repeated sleep sounds no longer extend music ducking beyond the intended rest, and muting effects stops an already-playing effect immediately. The runtime error collector fails Play Mode checks if an unexpected Error/Exception/Assert is logged.

The macOS standalone can be built through MonsterBuildChecks.BuildMac. [AUDIT.md](AUDIT.md) records the audit outcome. Android Build Support is not installed in the current Editor; APK and physical-phone verification remain pending.
