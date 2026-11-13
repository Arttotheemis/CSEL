"""Prepare the CC0 Chip Sounds selections and a direct comparison preview."""
from pathlib import Path
import array
import hashlib
import json
import math
import wave

ROOT = Path(__file__).resolve().parents[1]
SOURCES = ROOT / 'Assets/Audio/Sources/ChipSounds'
OUT = ROOT / 'Assets/Audio/SFX'
RATE = 44100


def read(name):
    with wave.open(str(SOURCES / (name + '.wav')), 'rb') as stream:
        assert stream.getsampwidth() == 2 and stream.getnchannels() == 1 and stream.getframerate() == RATE
        return [v / 32768 for v in array.array('h', stream.readframes(stream.getnframes()))]


def transpose(samples, speed):
    result = []
    for index in range(int((len(samples) - 1) / speed)):
        source = index * speed
        left = int(source)
        fraction = source - left
        result.append(samples[left] * (1 - fraction) + samples[left + 1] * fraction)
    return result


def finish(samples, target=0.18, peak_limit=0.78):
    # Short edge fades avoid digital clicks after cuts/reversals.
    edge = min(int(RATE * .008), len(samples) // 4)
    samples = list(samples)
    for i in range(edge):
        samples[i] *= i / edge
        samples[-1 - i] *= i / edge
    rms = math.sqrt(sum(v * v for v in samples) / len(samples))
    peak = max(abs(v) for v in samples)
    gain = min(target / max(rms, 1e-9), peak_limit / max(peak, 1e-9))
    return [v * gain for v in samples]


def write(path, samples):
    with wave.open(str(path), 'wb') as stream:
        stream.setparams((1, 2, RATE, 0, 'NONE', 'not compressed'))
        stream.writeframes(array.array('h', [round(max(-1, min(1, v)) * 32767) for v in samples]).tobytes())


def main():
    selections = {
        'PixelEat': finish(read('bubble_3')[:int(RATE * .22)], .2),
        'PixelPlay': finish(read('jump_cute_1'), .18),
        'PixelStudy': finish(read('signal'), .18),
        'PixelSleep': finish(transpose(read('phase_down'), 4), .18),
    }
    failed = list(reversed(read('flub_1')))
    # Two falling, rough-edged notes deliberately contrast with the rising success cue.
    selections['PixelWrong'] = finish(transpose(failed, 2) + [0.] * int(RATE * .07) + transpose(failed, 1.5), .2)
    report = {}
    preview = []
    for name, samples in selections.items():
        path = OUT / (name + '.wav')
        write(path, samples)
        report[name] = {'seconds': round(len(samples) / RATE, 3),
                        'rms': round(math.sqrt(sum(v * v for v in samples) / len(samples)), 4),
                        'peak': round(max(abs(v) for v in samples), 4),
                        'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}
        # Match the actual action rhythm in the downloadable, music-free preview.
        count = 4 if name == 'PixelEat' else 3 if name == 'PixelSleep' else 1
        for _ in range(count):
            preview.extend(samples)
            preview.extend([0.] * int(RATE * (.15 if name == 'PixelEat' else .65 if name == 'PixelSleep' else .2)))
        preview.extend([0.] * int(RATE * .9))
    write(ROOT / 'Builds/Previews/Pixel-Sound-Preview.wav', preview)
    (ROOT / 'Assets/Audio/PixelSoundManifest.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
