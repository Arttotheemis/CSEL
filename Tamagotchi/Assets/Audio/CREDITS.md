# Audio sources and license credits

## Active pixel action/error effects

The current scene uses **new WAV assets** from the CC0 [Chip Sounds collection by Subsoap](https://github.com/subsoap/chip-sounds). These replace the former generic Kenney action/error clips in the scene's cue array.

| Cue | Active project file | Chip Sounds source | Adaptation |
| --- | --- | --- | --- |
| Eat | SFX/PixelEat.wav | chip-sounds/bubble_3.wav | Short pixel pop; trimmed to 0.22 seconds, edge fades, level normalization; four chewing pulses during Eat. |
| Play | SFX/PixelPlay.wav | chip-sounds/jump_cute_1.wav | Bouncy rising jump tone; level normalization and edge fades. |
| Study | SFX/PixelStudy.wav | chip-sounds/signal.wav | Distinct retro study signal; normalized phrase, played once when the question opens. |
| Sleep | SFX/PixelSleep.wav | chip-sounds/phase_down.wav | Descending power-down tone transposed up for phone-speaker audibility; three spaced pulses. |
| Wrong | SFX/soundshelfstudio-ui-error-pop-515668.mp3 | User-provided local MP3 | Copied without changing its audio; used for wrong answers and timeouts. |

The source recordings are kept in Sources/ChipSounds/. The author's CC0 license is copied to Licenses/Chip-Sounds-CC0.txt. `Tools/prepare_pixel_audio.py` reproducibly prepares the adapted assets, a SHA-256/level manifest, and the music-free sound comparison at `Builds/Previews/Pixel-Sound-Preview.wav`.

Preview order: **Eat, Play, Study, Sleep, Wrong**. The Eat and Sleep sections reproduce their action-pulse rhythm.

The files have new asset names/GUIDs. Runtime validation checks that each event uses the corresponding Pixel*.wav clip and that the imported samples meet the intended audible level, rather than only checking an event name. Music is briefly ducked for action and result cues.

## Background music

**DURU — Arcade Vibe**, original composition and recording by DURU (uncle-sheepsky), CC0 1.0.

- Source: https://github.com/uncle-sheepsky/duru-cc0-bgm
- Original: mp3/duru-arcade-vibe.mp3 on master.
- Active project file: Music/ArcadeVibe.mp3, renamed with no audio changes.
- About 62.6 seconds, with a 1.5-second crossfade on repeats.
- License: Licenses/DURU-CC0.txt. Download checksum was verified against the author's manifest.

## Retained Kenney feedback effects

[Kenney Digital Audio](https://kenney.nl/assets/digital-audio), CC0:

- Correct.ogg: Audio/powerUp3.ogg, the rising success chime.
- Attention.ogg: Audio/threeTone1.ogg, new care warning.

[Kenney Interface Sounds](https://kenney.nl/assets/interface-sounds), CC0:

- Message.ogg: Audio/click_002.ogg, gentle message pop.
- Select.ogg: Audio/select_001.ogg, reserved selection cue.

Kenney's included license files are in Licenses/. Earlier Eat/Play/Study/Sleep/Wrong OGG files remain as unused source history; they are **not** the clips assigned to those five events in the current scene. The correct chime is separate from the new descending wrong-answer buzzer.


## User-supplied wrong-answer replacement

The active wrong-answer clip is now `soundshelfstudio-ui-error-pop-515668.mp3`, supplied directly by the user from Downloads. The project copy is byte-identical to that file. Its license has not been independently verified; the CC0 statements above apply to the identified Chip Sounds, Kenney and DURU material. PixelWrong.wav remains an unused earlier alternative. Background music's default mix level is now 45% (previously 35%); the 90% effects setting is unchanged.
