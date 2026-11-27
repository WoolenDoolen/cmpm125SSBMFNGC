"""Generate the project's original, small mono PCM sound effects (Python standard library).

Run from the repository root: python scripts/generate_sfx.py
No downloaded samples, external sound libraries, or runtime synthesis are used.
"""
import math
from pathlib import Path
import random
import struct
import wave

RATE = 22050
OUTPUT = Path(__file__).resolve().parents[1] / "Assets/_Project/Audio/Sfx"


def sweep(t, start, end, duration):
    ratio = end / start
    if abs(ratio - 1) < 0.001:
        phase = start * t
    else:
        phase = start * duration / math.log(ratio) * (ratio ** (t / duration) - 1)
    return math.sin(2 * math.pi * phase)


def synth(name, duration, sound, seed):
    rng = random.Random(seed)
    noise = 0.0
    samples = []
    for i in range(int(duration * RATE)):
        t = i / RATE
        grit = rng.uniform(-1, 1)
        noise = 0.75 * noise + 0.25 * grit
        # Short attack and release avoid discontinuities at either end of the PCM clip.
        edge = min(1, t / 0.004, (duration - t) / 0.025)
        samples.append(sound(t, noise, grit) * edge)
    peak = max(abs(s) for s in samples)
    gain = 0.72 / max(peak, 0.001)
    pcm = b"".join(struct.pack("<h", round(s * gain * 32767)) for s in samples)
    path = OUTPUT / (name + ".wav")
    with wave.open(str(path), "wb") as file:
        file.setnchannels(1)
        file.setsampwidth(2)
        file.setframerate(RATE)
        file.writeframes(pcm)
    print(f"{name}: {len(samples) / RATE:.2f}s, {len(pcm)} bytes PCM")


def chime(t, notes, spacing):
    result = 0.0
    for i, frequency in enumerate(notes):
        age = t - i * spacing
        if age >= 0:
            result += (math.sin(2 * math.pi * frequency * age) +
                       0.18 * math.sin(4 * math.pi * frequency * age)) * math.exp(-age * 10)
    return result


if __name__ == "__main__":
    OUTPUT.mkdir(parents=True, exist_ok=True)
    synth("Cannon", .32, lambda t, n, g: (sweep(t, 150, 38, .32) * .65 + n * 1.3 +
          g * .12 * math.exp(-t * 70)) * math.exp(-t * 15), 11)
    synth("StoneHit", .22, lambda t, n, g: (sweep(t, 620, 130, .22) * .22 +
          math.sin(t * 2 * math.pi * 940) * .12 + g * .6 + n * .3) * math.exp(-t * 26), 12)
    synth("KingdomHit", .32, lambda t, n, g: (sweep(t, 100, 43, .32) * .7 +
          n * 1.1 + math.sin(t * 2 * math.pi * 230) * .15) * math.exp(-t * 15), 13)
    synth("Footstep", .21, lambda t, n, g: (sweep(t, 75, 36, .21) * .7 +
          n * .9 + g * .08) * math.exp(-t * 23), 14)
    synth("MountainWake", .58, lambda t, n, g: (sweep(t, 90, 165, .58) * .7 +
          sweep(t, 135, 247.5, .58) * .2 + n * .15) * math.sin(math.pi * t / .58), 15)
    synth("Upgrade", .52, lambda t, n, g: chime(t, [523.25, 659.25, 783.99], .09), 16)
    synth("Victory", .95, lambda t, n, g: chime(t, [392, 523.25, 659.25, 783.99], .14), 17)
