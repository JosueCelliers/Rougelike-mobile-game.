#!/usr/bin/env python3
"""Original equipment illustrations and HUD icons for BAD LIE's upgrades.

Writes SVG sources to Tools/art/upgrades and renders PNGs into Assets/BadLie/UI/Upgrades
with rsvg-convert. Style: dark ink medallion, warm gold line work, a single accent colour.
Run: python3 Tools/art/make_upgrade_art.py
"""
import os
import subprocess

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, "Tools", "art", "upgrades")
OUT = os.path.join(ROOT, "Assets", "BadLie", "UI", "Upgrades")

INK = "#1d171c"
INK2 = "#2a2128"
GOLD = "#dcb573"
GOLD_DEEP = "#9c7440"
IVORY = "#f3e6cf"
CYAN = "#9bf3ea"
AMBER = "#ffc56b"
VERMILION = "#e0442f"
STONE = "#cfb084"
STONE_D = "#9c7a5a"
OLIVE = "#6f7a33"
OLIVE_L = "#9aa349"

DEFS = f"""
<defs>
  <radialGradient id="plate" cx="50%" cy="42%" r="62%">
    <stop offset="0%" stop-color="#3a2d33"/>
    <stop offset="100%" stop-color="{INK}"/>
  </radialGradient>
  <radialGradient id="glowC" cx="50%" cy="50%" r="50%">
    <stop offset="0%" stop-color="{CYAN}" stop-opacity="0.55"/>
    <stop offset="100%" stop-color="{CYAN}" stop-opacity="0"/>
  </radialGradient>
  <radialGradient id="glowA" cx="50%" cy="50%" r="50%">
    <stop offset="0%" stop-color="{AMBER}" stop-opacity="0.55"/>
    <stop offset="100%" stop-color="{AMBER}" stop-opacity="0"/>
  </radialGradient>
  <linearGradient id="steel" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#e8e0d4"/>
    <stop offset="55%" stop-color="#a8a094"/>
    <stop offset="100%" stop-color="#5f5852"/>
  </linearGradient>
  <linearGradient id="pebble" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0%" stop-color="#c3cbb8"/>
    <stop offset="100%" stop-color="#6f7c70"/>
  </linearGradient>
  <linearGradient id="stoneG" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0%" stop-color="#e2c79b"/>
    <stop offset="100%" stop-color="{STONE_D}"/>
  </linearGradient>
  <linearGradient id="brass" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#ffe1a0"/>
    <stop offset="100%" stop-color="#a87632"/>
  </linearGradient>
  <linearGradient id="paper" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0%" stop-color="#f6ead2"/>
    <stop offset="100%" stop-color="#d9c6a2"/>
  </linearGradient>
</defs>
"""


def medallion(inner):
    """Hexagonal ink medallion with a double gold rule (echoes the HUD pips)."""
    return f"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512">
{DEFS}
<polygon points="256,14 466,135 466,377 256,498 46,377 46,135" fill="url(#plate)" stroke="{GOLD}" stroke-width="6"/>
<polygon points="256,38 445,147 445,365 256,474 67,365 67,147" fill="none" stroke="{GOLD_DEEP}" stroke-width="2.5" stroke-opacity="0.8"/>
{inner}
</svg>
"""


def skip_stone():
    return medallion(f"""
<ellipse cx="256" cy="372" rx="168" ry="40" fill="none" stroke="{CYAN}" stroke-width="4" stroke-opacity="0.35"/>
<ellipse cx="256" cy="372" rx="118" ry="27" fill="none" stroke="{CYAN}" stroke-width="5" stroke-opacity="0.6"/>
<ellipse cx="256" cy="372" rx="64" ry="14" fill="none" stroke="{CYAN}" stroke-width="6"/>
<ellipse cx="256" cy="372" rx="140" ry="60" fill="url(#glowC)"/>
<path d="M92,318 Q174,170 256,352 Q312,250 380,300 Q412,268 436,282" fill="none" stroke="{IVORY}" stroke-width="5" stroke-dasharray="2 16" stroke-linecap="round"/>
<g transform="translate(256,214) rotate(-14)">
  <ellipse cx="0" cy="18" rx="112" ry="32" fill="{INK2}" opacity="0.7"/>
  <ellipse cx="0" cy="0" rx="112" ry="48" fill="url(#pebble)" stroke="{IVORY}" stroke-width="4"/>
  <ellipse cx="-16" cy="-12" rx="76" ry="22" fill="#e4eadb" opacity="0.5"/>
  <path d="M-70,10 Q-10,26 64,8" stroke="#5c6658" stroke-width="3" fill="none" opacity="0.7"/>
  <circle cx="44" cy="-18" r="5" fill="#ffffff" opacity="0.85"/>
</g>
<path d="M380,300 l14,-26 M380,300 l26,-8" stroke="{CYAN}" stroke-width="5" stroke-linecap="round"/>
""")


def bank_shot():
    return medallion(f"""
<g>
  <path d="M120,150 L300,150 L300,196 L166,196 L166,392 L120,392 Z" fill="url(#stoneG)" stroke="{IVORY}" stroke-width="4" stroke-linejoin="round"/>
  <path d="M120,190 L300,190 M120,240 L166,240 M120,290 L166,290 M120,340 L166,340 M210,150 L210,190" stroke="{STONE_D}" stroke-width="3"/>
  <path d="M166,196 L218,196 L218,214 L184,214 L184,248 L166,248 Z" fill="url(#brass)" stroke="#fff0c8" stroke-width="3" stroke-linejoin="round"/>
  <circle cx="176" cy="206" r="4" fill="{INK}"/>
  <circle cx="208" cy="205" r="3.5" fill="{INK}"/>
  <circle cx="175" cy="238" r="3.5" fill="{INK}"/>
</g>
<path d="M410,404 L196,226" stroke="{IVORY}" stroke-width="6" stroke-dasharray="3 18" stroke-linecap="round"/>
<path d="M196,226 L398,150" stroke="{AMBER}" stroke-width="7" stroke-dasharray="3 18" stroke-linecap="round"/>
<circle cx="196" cy="226" r="46" fill="url(#glowA)"/>
<path d="M398,150 l-30,-4 M398,150 l-18,24" stroke="{AMBER}" stroke-width="7" stroke-linecap="round"/>
<circle cx="410" cy="404" r="22" fill="{IVORY}" stroke="{INK}" stroke-width="3"/>
<path d="M226,232 l22,6 M222,250 l18,16 M206,262 l6,22" stroke="{AMBER}" stroke-width="4" stroke-linecap="round"/>
""")


def rough_rider():
    blades = []
    for i, x in enumerate(range(96, 432, 22)):
        h = 140 + (i * 37) % 70
        lean = ((i * 13) % 30) - 15
        col = OLIVE if i % 2 else OLIVE_L
        cut = 250 if 190 < x < 360 else None
        top = 404 - h
        if cut is not None and top < cut:
            top = cut + 6
        blades.append(f'<path d="M{x},404 Q{x + lean / 2},{(404 + top) / 2} {x + lean},{top} Q{x + lean / 2 + 8},{(404 + top) / 2} {x + 14},404 Z" fill="{col}" stroke="#3e4520" stroke-width="2"/>')
    chips = ''.join(f'<path d="M{x},{y} l10,-16 l6,4 z" fill="{OLIVE_L}" stroke="#3e4520" stroke-width="1.5"/>' for x, y in [(212, 214), (262, 196), (318, 208), (350, 186)])
    return medallion(f"""
{''.join(blades)}
{chips}
<g transform="translate(256,232) rotate(-8)">
  <path d="M-170,6 Q-20,-56 168,-18 Q178,-12 168,0 Q-10,-20 -168,30 Z" fill="url(#steel)" stroke="{IVORY}" stroke-width="4" stroke-linejoin="round"/>
  <path d="M-150,10 Q-10,-38 150,-12" stroke="#ffffff" stroke-width="3" fill="none" opacity="0.7"/>
  <rect x="-200" y="4" width="46" height="26" rx="6" fill="{INK2}" stroke="{GOLD}" stroke-width="3"/>
</g>
<path d="M96,404 L432,404" stroke="#3e4520" stroke-width="4"/>
""")


def heavy_core():
    return medallion(f"""
<ellipse cx="256" cy="420" rx="150" ry="22" fill="#000000" opacity="0.45"/>
<path d="M256,110 A150,150 0 0 1 256,410 Z" fill="{IVORY}" stroke="{INK}" stroke-width="4"/>
<path d="M256,110 A150,150 0 0 0 256,410" fill="none" stroke="{IVORY}" stroke-width="5" stroke-dasharray="1 12" opacity="0.7"/>
<circle cx="256" cy="260" r="150" fill="none" stroke="{IVORY}" stroke-width="4"/>
<g fill="#d8cdbb">
  <circle cx="318" cy="170" r="9"/><circle cx="352" cy="214" r="9"/><circle cx="370" cy="268" r="9"/>
  <circle cx="354" cy="322" r="9"/><circle cx="316" cy="366" r="9"/><circle cx="306" cy="222" r="9"/><circle cx="318" cy="292" r="9"/>
</g>
<path d="M256,110 L256,410" stroke="{INK}" stroke-width="5"/>
<path d="M256,150 A110,110 0 0 0 256,370 Z" fill="#3b3438"/>
<path d="M256,180 A80,80 0 0 0 256,340 Z" fill="#2a2427" stroke="{GOLD}" stroke-width="5"/>
<path d="M256,212 A48,48 0 0 0 256,308 Z" fill="#1c181a" stroke="{GOLD}" stroke-width="4"/>
<path d="M232,248 a18,18 0 0 0 0,24" stroke="#6e6468" stroke-width="5" fill="none"/>
<path d="M150,440 L150,462 M200,446 L200,470 M256,448 L256,474 M312,446 L312,470 M362,440 L362,462" stroke="{GOLD}" stroke-width="5" stroke-linecap="round" opacity="0.8"/>
""")


def cup_magnet():
    return medallion(f"""
<ellipse cx="256" cy="380" rx="120" ry="34" fill="{OLIVE}" opacity="0.55"/>
<ellipse cx="256" cy="380" rx="56" ry="17" fill="#141012" stroke="{IVORY}" stroke-width="5"/>
<path d="M256,382 L256,116" stroke="{IVORY}" stroke-width="7" stroke-linecap="round"/>
<path d="M256,118 L344,146 L256,174 Z" fill="{VERMILION}"/>
<g transform="translate(256,262)">
  <path d="M-58,-30 A58,58 0 1 0 58,-30" fill="none" stroke="#3b3438" stroke-width="26" stroke-linecap="butt"/>
  <path d="M-58,-30 A58,58 0 1 0 58,-30" fill="none" stroke="{GOLD}" stroke-width="4" stroke-dasharray="0" opacity="0.9" transform="scale(1.24)"/>
  <rect x="-72" y="-58" width="28" height="30" fill="{VERMILION}" stroke="{IVORY}" stroke-width="3"/>
  <rect x="44" y="-58" width="28" height="30" fill="#c9c2b8" stroke="{IVORY}" stroke-width="3"/>
</g>
<path d="M120,330 Q170,300 214,366" fill="none" stroke="{CYAN}" stroke-width="5" stroke-dasharray="4 12" stroke-linecap="round"/>
<path d="M392,330 Q342,300 298,366" fill="none" stroke="{CYAN}" stroke-width="5" stroke-dasharray="4 12" stroke-linecap="round"/>
<path d="M150,420 Q200,396 230,392" fill="none" stroke="{CYAN}" stroke-width="5" stroke-dasharray="4 12" stroke-linecap="round"/>
<circle cx="256" cy="376" r="70" fill="url(#glowC)"/>
<circle cx="138" cy="410" r="18" fill="{IVORY}" stroke="{INK}" stroke-width="3"/>
""")


def second_chance():
    lines = ''.join(f'<path d="M150,{y} L388,{y - 30}" stroke="#b9a37c" stroke-width="2" opacity="0.6"/>' for y in range(222, 400, 26))
    return medallion(f"""
<g>
  <path d="M120,200 L380,150 L404,368 L146,420 Z" fill="url(#paper)" stroke="{IVORY}" stroke-width="4" stroke-linejoin="round"/>
  <path d="M262,174 L276,394" stroke="#a88d63" stroke-width="3"/>
  {lines}
</g>
<g transform="translate(262,288) rotate(-38)">
  <path d="M0,-170 C60,-120 64,40 8,150 C-50,40 -56,-110 0,-170 Z" fill="#eef7f4" stroke="{CYAN}" stroke-width="4"/>
  <path d="M2,-160 L4,176" stroke="{GOLD_DEEP}" stroke-width="5" stroke-linecap="round"/>
  <path d="M4,-110 L40,-130 M4,-70 L46,-92 M4,-30 L48,-50 M4,10 L44,-8 M4,50 L36,34 M4,-110 L-34,-128 M4,-70 L-42,-90 M4,-30 L-44,-48 M4,10 L-40,-6 M4,50 L-32,36" stroke="{CYAN}" stroke-width="3" opacity="0.8"/>
</g>
<path d="M380,120 A64,64 0 1 1 312,82" fill="none" stroke="{GOLD}" stroke-width="7" stroke-linecap="round"/>
<path d="M312,82 l30,-14 M312,82 l18,26" stroke="{GOLD}" stroke-width="7" stroke-linecap="round"/>
""")


def icon(inner):
    return f"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128" width="128" height="128">
<g fill="none" stroke="{IVORY}" stroke-width="7" stroke-linecap="round" stroke-linejoin="round">
{inner}
</g>
</svg>
"""


ICONS = {
    "skip_stone": icon(f'<ellipse cx="64" cy="54" rx="40" ry="18"/><path d="M28,96 Q64,110 100,96" stroke="{CYAN}"/>'),
    "bank_shot": icon(f'<path d="M30,26 L30,102 M30,26 L98,26"/><path d="M104,104 L40,40 L104,66" stroke="{AMBER}"/>'),
    "rough_rider": icon(f'<path d="M22,58 Q64,30 108,46"/><path d="M34,108 L40,74 M58,108 L60,80 M82,108 L84,74 M100,108 L96,82" stroke="{OLIVE_L}"/>'),
    "heavy_core": icon(f'<circle cx="64" cy="60" r="38"/><circle cx="64" cy="60" r="16" fill="{GOLD}" stroke="{GOLD}"/><path d="M34,112 L94,112" stroke="{GOLD}"/>'),
    "cup_magnet": icon(f'<path d="M40,30 L40,62 A24,24 0 0 0 88,62 L88,30"/><path d="M30,100 Q64,84 98,100" stroke="{CYAN}"/>'),
    "second_chance": icon(f'<path d="M36,104 C30,64 60,30 98,22 C96,62 70,94 36,104 Z"/><path d="M36,104 L74,58" stroke="{CYAN}"/>'),
}

ART = {
    "skip_stone": skip_stone,
    "bank_shot": bank_shot,
    "rough_rider": rough_rider,
    "heavy_core": heavy_core,
    "cup_magnet": cup_magnet,
    "second_chance": second_chance,
}


def main():
    os.makedirs(SRC, exist_ok=True)
    os.makedirs(OUT, exist_ok=True)
    for name, fn in ART.items():
        svg = os.path.join(SRC, name + ".svg")
        with open(svg, "w") as f:
            f.write(fn())
        subprocess.check_call(["rsvg-convert", "-w", "512", "-h", "512", svg, "-o", os.path.join(OUT, name + ".png")])
    for name, svgtext in ICONS.items():
        svg = os.path.join(SRC, name + "_icon.svg")
        with open(svg, "w") as f:
            f.write(svgtext)
        subprocess.check_call(["rsvg-convert", "-w", "128", "-h", "128", svg, "-o", os.path.join(OUT, name + "_icon.png")])
    print("wrote", len(ART), "illustrations and", len(ICONS), "icons to", OUT)


if __name__ == "__main__":
    main()
