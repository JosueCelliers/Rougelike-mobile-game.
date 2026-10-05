#!/usr/bin/env python3.12
"""Synthesises every sound in BAD LIE (original, deterministic, no samples).

Output: Assets/BadLie/Audio/*.wav (44.1 kHz, 16-bit mono).
Run:    python3.12 Tools/audio/make_audio.py
"""
import os
import numpy as np
from scipy.signal import butter, sosfilt, fftconvolve

SR = 44100
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets", "BadLie", "Audio")
rng = np.random.default_rng(20261005)


# ---------------------------------------------------------------- helpers

def t_axis(dur):
    return np.arange(int(SR * dur)) / SR


def noise(dur):
    return rng.standard_normal(int(SR * dur))


def lp(x, f, order=2):
    return sosfilt(butter(order, f, "low", fs=SR, output="sos"), x)


def hp(x, f, order=2):
    return sosfilt(butter(order, f, "high", fs=SR, output="sos"), x)


def bp(x, lo, hi, order=2):
    return sosfilt(butter(order, [lo, hi], "band", fs=SR, output="sos"), x)


def decay(dur, tau, attack=0.0015):
    t = t_axis(dur)
    env = np.exp(-t / tau)
    if attack > 0:
        env *= np.clip(t / attack, 0, 1)
    return env


def mode(freq, dur, tau, amp=1.0, phase=0.0):
    t = t_axis(dur)
    return amp * np.sin(2 * np.pi * freq * t + phase) * decay(dur, tau, 0.0008)


def place(buf, x, at):
    i = int(at * SR)
    end = min(len(buf), i + len(x))
    buf[i:end] += x[: end - i]
    return buf


def reverb(x, seconds=1.8, mix=0.25, damp=3500):
    n = int(SR * seconds)
    ir = rng.standard_normal(n) * np.exp(-np.arange(n) / (SR * seconds / 6.0))
    ir = lp(ir, damp)
    ir[: int(0.012 * SR)] = 0
    ir /= np.sqrt(np.sum(ir ** 2)) + 1e-9
    wet = fftconvolve(x, ir)
    wet = np.pad(wet, (0, max(0, len(x) + n - len(wet))))[: len(x) + n]
    dry = np.concatenate([x, np.zeros(n)])
    return dry * (1 - mix) + wet * mix


def normalize(x, peak=0.9):
    m = np.max(np.abs(x)) + 1e-9
    return x / m * peak


def fade(x, fin=0.002, fout=0.02):
    a, b = int(fin * SR), int(fout * SR)
    if a > 0:
        x[:a] *= np.linspace(0, 1, a)
    if b > 0:
        x[-b:] *= np.linspace(1, 0, b)
    return x


def write(name, x, peak=0.9):
    import wave
    x = normalize(fade(np.asarray(x, dtype=np.float64)), peak)
    data = (np.clip(x, -1, 1) * 32767).astype("<i2")
    os.makedirs(OUT, exist_ok=True)
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())


def bell(freq, dur, tau, partials=((1, 1), (2.0, 0.45), (2.76, 0.3), (5.4, 0.12)), bright=1.0):
    out = np.zeros(int(SR * dur))
    for ratio, amp in partials:
        out += mode(freq * ratio, dur, tau / (1 + (ratio - 1) * 0.6 * bright), amp)
    return out


# ---------------------------------------------------------------- impacts

def strike(power):
    dur = 0.35
    x = np.zeros(int(SR * dur))
    click = hp(noise(0.004), 3000) * decay(0.004, 0.0012, 0)
    place(x, click * 0.9, 0)
    p = 0.94 + 0.12 * power
    x += mode(2150 * p, dur, 0.016, 0.9)
    x += mode(3420 * p, dur, 0.009, 0.55)
    x += mode(980 * p, dur, 0.03, 0.45)
    x += mode(185, dur, 0.045, 0.25 + 0.5 * power)
    x += lp(noise(dur), 1800) * decay(dur, 0.02) * 0.15
    return x


def stone(seed_shift):
    dur = 0.4
    s = 1.0 + seed_shift
    x = mode(1150 * s, dur, 0.04, 0.8) + mode(2640 * s, dur, 0.022, 0.55) + mode(4120 * s, dur, 0.013, 0.35)
    x += mode(310 * s, dur, 0.05, 0.35)
    x += bp(noise(dur), 1800, 6500) * decay(dur, 0.018, 0) * 0.7
    return x


def hedge():
    dur = 0.45
    x = bp(noise(dur), 500, 3200) * decay(dur, 0.09, 0.006)
    crack = np.zeros(int(SR * dur))
    for _ in range(14):
        place(crack, hp(noise(0.004), 2000) * decay(0.004, 0.001, 0) * rng.uniform(0.2, 0.6), rng.uniform(0.0, 0.25))
    x += crack + mode(160, dur, 0.06, 0.4)
    return lp(x, 4500)


def cup_drop():
    dur = 1.8
    x = np.zeros(int(SR * dur))
    place(x, mode(3150, 0.12, 0.012, 0.7), 0.0)
    place(x, hp(noise(0.003), 3000) * 0.5 * decay(0.003, 0.001, 0), 0.0)
    place(x, mode(2400, 0.12, 0.01, 0.5), 0.075)
    place(x, mode(2050, 0.1, 0.009, 0.35), 0.13)
    clunk = mode(320, 0.5, 0.11, 0.9) + mode(645, 0.5, 0.07, 0.5) + bp(noise(0.5), 200, 1400) * decay(0.5, 0.05, 0.002) * 0.6
    place(x, clunk, 0.2)
    chime = bell(880, 1.4, 0.55, ((1, 1), (2.0, 0.3), (3.0, 0.12))) * 0.22 + bell(1320, 1.4, 0.5, ((1, 1), (2.0, 0.3))) * 0.15
    place(x, chime, 0.38)
    return reverb(x, 1.2, 0.18)


def splash():
    dur = 1.1
    t = t_axis(dur)
    body = noise(dur) * decay(dur, 0.22, 0.004)
    # Sweep the cutoff down by mixing two filtered layers.
    hi = lp(body, 5000) * np.exp(-t / 0.12)
    lo = lp(body, 900)
    x = hi * 0.9 + lo * 0.8
    for _ in range(9):
        f0 = rng.uniform(500, 1400)
        d = rng.uniform(0.04, 0.09)
        tt = t_axis(d)
        blip = np.sin(2 * np.pi * (f0 * tt + 2200 * tt * tt)) * decay(d, d / 3, 0.002)
        place(x, blip * rng.uniform(0.08, 0.2), rng.uniform(0.12, 0.6))
    return reverb(x, 0.9, 0.15)


def skip():
    dur = 0.7
    tt = t_axis(0.07)
    chirp = np.sin(2 * np.pi * (600 * tt + 6000 * tt * tt)) * decay(0.07, 0.025, 0.002)
    x = np.zeros(int(SR * dur))
    place(x, chirp * 0.9, 0)
    place(x, hp(noise(0.12), 1500) * decay(0.12, 0.03, 0.002) * 0.4, 0)
    place(x, bell(1760, 0.6, 0.25, ((1, 1), (2.01, 0.3))) * 0.25, 0.02)
    return reverb(x, 0.8, 0.2)


def land_grass():
    dur = 0.25
    return lp(noise(dur), 900) * decay(dur, 0.035, 0.002) + mode(140, dur, 0.05, 0.6)


def land_sand():
    dur = 0.35
    x = lp(noise(dur), 2200) * decay(dur, 0.06, 0.003)
    for _ in range(20):
        place(x, hp(noise(0.002), 3000) * rng.uniform(0.05, 0.15), rng.uniform(0, 0.12))
    return x + mode(120, dur, 0.04, 0.4)


def lip_out():
    dur = 0.6
    x = mode(2310, dur, 0.15, 0.8) + mode(3720, dur, 0.09, 0.5) + mode(5130, dur, 0.06, 0.3)
    place(x, hp(noise(0.004), 3000) * 0.4 * decay(0.004, 0.001, 0), 0.0)
    return reverb(x, 0.6, 0.15)


# ---------------------------------------------------------------- interface

def ui_tap():
    dur = 0.12
    return mode(1250, dur, 0.018, 0.8) + mode(2500, dur, 0.008, 0.25) + hp(noise(dur), 2500) * decay(dur, 0.004, 0) * 0.2


def ui_select():
    x = np.zeros(int(SR * 0.9))
    place(x, bell(659.3, 0.8, 0.35), 0)
    place(x, bell(987.8, 0.7, 0.3), 0.09)
    return reverb(x, 0.9, 0.2)


def penalty():
    dur = 1.0
    x = mode(220, dur, 0.22, 0.8) + mode(330, dur, 0.15, 0.4) + mode(196, dur, 0.25, 0.4)
    return lp(reverb(x, 1.0, 0.2), 1800)


def chord_arp(freqs, gap, tau, total):
    x = np.zeros(int(SR * total))
    for i, f in enumerate(freqs):
        place(x, bell(f, total - i * gap, tau), i * gap)
    return x


def hole_complete():
    return reverb(chord_arp([587.3, 698.5, 880.0, 1174.7], 0.11, 0.7, 2.2), 1.6, 0.3)


def run_won():
    x = chord_arp([293.7, 440.0, 587.3, 698.5, 880.0, 1174.7], 0.16, 1.1, 4.0)
    t = t_axis(4.0)
    pad = (np.sin(2 * np.pi * 146.8 * t) + 0.5 * np.sin(2 * np.pi * 220.0 * t)) * np.clip(t / 0.8, 0, 1) * np.exp(-t / 2.0) * 0.25
    return reverb(x + pad, 2.2, 0.35)


def run_lost():
    x = bell(146.8, 4.0, 1.6, ((1, 1), (2.0, 0.5), (2.76, 0.35), (5.4, 0.15), (8.9, 0.05)), 0.7)
    return lp(reverb(x, 2.5, 0.35), 2600)


def magnet():
    dur = 0.9
    t = t_axis(dur)
    env = np.sin(np.pi * np.clip(t / dur, 0, 1)) ** 2
    x = np.zeros_like(t)
    for f in (2093, 2637, 3136, 3951):
        x += np.sin(2 * np.pi * f * t + rng.uniform(0, 6.28)) * (0.6 + 0.4 * np.sin(2 * np.pi * 13 * t + f))
    x = x * env * 0.25 + bp(noise(dur), 1500, 6000) * env * 0.15
    return reverb(x, 0.8, 0.3)


# ---------------------------------------------------------------- beds

def loopable(x, xfade):
    """Crossfade the tail into the head so the clip loops seamlessly."""
    n = int(xfade * SR)
    head, tail = x[:n].copy(), x[-n:].copy()
    ramp = np.linspace(0, 1, n)
    x[:n] = head * ramp + tail * (1 - ramp)
    return x[:-n]


def ambience():
    dur = 52.0
    t = t_axis(dur)
    # Wind: brown-ish noise with slow swells.
    w = np.cumsum(rng.standard_normal(len(t)))
    w = hp(w, 30)
    w = lp(w, 420)
    w = w / (np.max(np.abs(w)) + 1e-9)
    swell = 0.55 + 0.45 * np.sin(2 * np.pi * t / 17.0) * np.sin(2 * np.pi * t / 7.3 + 1.0)
    x = w * swell * 0.5
    # Leaves rustling in gusts.
    for _ in range(14):
        at = rng.uniform(0, dur - 3)
        d = rng.uniform(1.0, 2.6)
        tt = t_axis(d)
        g = bp(noise(d), 1500, 6500) * np.sin(np.pi * tt / d) ** 2 * rng.uniform(0.04, 0.09)
        place(x, g, at)
    # Water lapping against stone.
    lap = lp(noise(dur), 600) * (0.5 + 0.5 * np.sin(2 * np.pi * t / 2.7) ** 6) * 0.12
    x += lap
    # Distant birds.
    for _ in range(11):
        at = rng.uniform(0.5, dur - 2)
        f0 = rng.uniform(2600, 4200)
        for k in range(rng.integers(2, 5)):
            d = rng.uniform(0.06, 0.14)
            tt = t_axis(d)
            chirp = np.sin(2 * np.pi * (f0 * tt + rng.uniform(-9000, 9000) * tt * tt)) * np.sin(np.pi * tt / d) ** 2
            place(x, chirp * rng.uniform(0.015, 0.035), at + k * rng.uniform(0.12, 0.2))
    x = reverb(x, 1.8, 0.25, 3000)
    return loopable(x[: int(SR * dur)], 3.0)


def harp(freq, dur, amp):
    t = t_axis(dur)
    out = np.zeros_like(t)
    for h in range(1, 9):
        out += np.sin(2 * np.pi * freq * h * t) * np.exp(-t * (1.6 + 1.9 * h)) / h ** 1.3
    attack = np.clip(t / 0.004, 0, 1)
    return out * attack * amp


def celesta(freq, dur, amp):
    t = t_axis(dur)
    out = np.sin(2 * np.pi * freq * t) * np.exp(-t * 2.2) + 0.35 * np.sin(2 * np.pi * freq * 4.02 * t) * np.exp(-t * 6.0)
    out += 0.15 * np.sin(2 * np.pi * freq * 2.0 * t) * np.exp(-t * 3.5)
    return out * np.clip(t / 0.002, 0, 1) * amp


def music():
    bpm = 66.0
    beat = 60.0 / bpm
    bar = 3 * beat
    bars = 24
    dur = bars * bar
    x = np.zeros(int(SR * (dur + 4)))
    note = lambda m: 440.0 * 2 ** ((m - 69) / 12.0)
    # D dorian progression: Dm9 - Bbmaj7 - Gm7 - A7sus4 (x6, with variations).
    chords = [
        [50, 57, 62, 64, 65, 69],   # Dm9
        [46, 53, 57, 62, 65, 69],   # Bbmaj7
        [43, 50, 55, 58, 62, 65],   # Gm7
        [45, 52, 57, 62, 64, 67],   # A7sus4
    ]
    pattern = [0, 2, 3, 4, 3, 2]   # arpeggio in eighths over three beats
    melody = {1: [74, 72], 3: [69], 5: [77, 76, 74], 7: [72, 69], 9: [74], 11: [76, 77], 13: [81, 79], 15: [77, 76],
              17: [74, 72], 19: [69, 67], 21: [72, 74], 23: [69]}
    for b in range(bars):
        ch = chords[b % 4]
        start = b * bar
        # Bass note on the downbeat.
        place(x, harp(note(ch[0]), 3.5, 0.55), start)
        for i, p in enumerate(pattern):
            m = ch[1 + p % (len(ch) - 1)]
            if b % 8 >= 4 and i % 2 == 1:
                m += 12
            place(x, harp(note(m), 2.2, 0.26 if i else 0.32), start + i * beat / 2)
        if b in melody:
            for k, m in enumerate(melody[b]):
                place(x, celesta(note(m), 2.4, 0.22), start + k * beat * (1.5 if len(melody[b]) == 2 else 1.0))
    # Soft drone pad on D and A.
    t = t_axis(len(x) / SR)
    pad = np.zeros_like(t)
    for f, a in ((73.42, 0.5), (110.0, 0.35), (146.83, 0.25), (220.0, 0.12)):
        for det in (-0.6, 0.0, 0.7):
            pad += np.sin(2 * np.pi * (f + det) * t) * a / 3
    pad *= 0.10 * (0.8 + 0.2 * np.sin(2 * np.pi * t / 11.0))
    x = lp(x, 6000) + lp(pad, 900)
    x = reverb(x, 2.8, 0.38, 3200)
    total = int(SR * dur)
    # Fold the reverb tail back onto the start for a seamless loop.
    tail = x[total:]
    out = x[:total].copy()
    out[: len(tail)] += tail
    return out


def main():
    jobs = {
        "strike_soft": lambda: strike(0.2),
        "strike_mid": lambda: strike(0.55),
        "strike_hard": lambda: strike(0.95),
        "wall_stone_a": lambda: stone(0.0),
        "wall_stone_b": lambda: stone(0.07),
        "wall_hedge": hedge,
        "cup_drop": cup_drop,
        "splash": splash,
        "skip": skip,
        "land_grass": land_grass,
        "land_sand": land_sand,
        "lip_out": lip_out,
        "ui_tap": ui_tap,
        "ui_select": ui_select,
        "penalty": penalty,
        "hole_complete": hole_complete,
        "run_won": run_won,
        "run_lost": run_lost,
        "magnet": magnet,
    }
    for name, fn in jobs.items():
        write(name, fn())
    write("ambience_garden", ambience(), 0.7)
    write("music_estate", music(), 0.75)
    print("wrote", len(jobs) + 2, "clips to", OUT)


if __name__ == "__main__":
    main()
