# Pocket pet — Unity Tamagotchi-inspired assignment

Open this folder in **Unity 6000.2.2f1**, load `Assets/Scenes/Monster.unity`, and press Play. The mobile design is **1080 × 2400 portrait**. Use the matching Game view preset; PhoneViewport fits the whole design inside other display shapes and device safe areas.

- Four illustrated meters: Stamina, Study, Food, Happiness.
- Whole-number labels and matching fills; no decimal flicker.
- Food and stamina lose one point per **60 seconds**. Empty Food doubles stamina loss on subsequent care ticks.
- Play costs 2 stamina and 1 food, adds exactly 0.5 happiness, then opens an eight-second left/right guessing round. Tap a choice for the result. Each round resolves once; wins do not add extra happiness.
- Study costs 2 stamina and 1 food and subtracts exactly 0.5 happiness once. A 12-second two-choice addition/subtraction question awards 10 Study progress for a correct answer. Wrong answers and timeouts reveal the answer without awarding progress.
- Eat adds 15 stamina, 5 happiness and 25 food. Sleep restores stamina immediately to 100, followed by a short resting animation.
- Play and Study require stamina >= 20; Sleep requires stamina < 20. Actions cannot overlap.
- Happiness retains the assignment's internal 0.01-per-second decay. Exact 100 selects Happy; <= 50 selects Sad; otherwise the current action or Idle plays.
- Seven sprite animations use a normalized head width and common floor; props do not determine the pet's apparent size.
- Care messages and guessing choices appear inside a wood-and-parchment pixel frame.
- Device builds autosave every 30 game seconds and save on pause/quit, including care values, guessing wins and partial care-tick time. Editor sessions use Inspector defaults. No offline decay is applied. App suspension freezes care and quiz deadlines.

The original supplied character sheet is **Animal.png**, not Animal.jpg. The artwork is kept intact. Sprite import data and animation curves handle slicing, alignment and scale. Press Start 2P is bundled with its SIL Open Font License in `Assets/Fonts`.

[Scene, validation, Android and submission guide](Docs/SETUP.md)

[Rendered phone preview](Builds/Previews/Monster-1080x2400.png)

The care/attention loop and guessing-game idea are adapted from [Bandai's Original Tamagotchi manual](https://tamagotchi-official.com/manual/toy/original/Original_webmanual.pdf). This assignment preserves its own stamina, study and happiness rules rather than attempting a complete reproduction of the toy.

Retro sound effects and crossfaded chiptune background music are included. The mix is set to 35% music and 90% effects, with louder action cues and a distinct wrong-answer error beep. There are no Music/SFX text controls above the HUD. Sleep temporarily softens the music. [Audio sources and licenses](Assets/Audio/CREDITS.md).

The latest update replaces all four action cues and the wrong-answer cue with distinct, normalized **Pixel*.wav** assets from the CC0 Chip Sounds collection. [Direct sound preview](Builds/Previews/Pixel-Sound-Preview.wav), in order: Eat, Play, Study, Sleep, Wrong. These new assets are explicitly assigned and checked in Play Mode.


Current audio update: wrong answers and timeouts use the supplied `soundshelfstudio-ui-error-pop-515668.mp3`. Background music is set to 45% (up from 35%); effects remain at 90%. The earlier comparison WAV still demonstrates the previous generated wrong-answer cue, not this supplied MP3.


The game audit added visible quiz countdowns, deferred care alerts, accurate capped Study rewards and safer save recovery. [Audit report](Docs/AUDIT.md) documents verified behavior, checks, fixes and platform limits.
