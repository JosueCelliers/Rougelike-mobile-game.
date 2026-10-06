# BAD LIE bot playtest

Generated 2026-10-06 00:02 UTC on 4 threads. Budget under test: start 11, restore 3/hole, under-par bonus 1, hazard penalty 1.

Skill models (uniform execution error, the planner does not know about it): precise ±0° aim, ±0 % power; good ±1.5° aim, ±4 % power; casual ±3.5° aim, ±9 % power.

## Every pin and loadout, precise bot

Strokes including penalties. `x` = not holed within 14. Bold = under par.

| Pin | Par | Walk (m) | none | skip | bank | rough | heavy | magnet | ricochet | groundbr. |
|---|---|---|---|---|---|---|---|---|---|---|
| H1 main | 4 | 41 | 4 | **3**ˢ | 4ᵇ | 4 | 4 | 4 | **3**ˢ | 4 |
| H1 alt A | 4 | 38 | 4 | **3**ˢ | 4ᵇ | **3** | 4 | 4 | **3**ˢ | 4 |
| H1 alt B | 4 | 41 | 4 | **3**ˢ | 4ᵇ | 4 | 4 | 4 | **3**ˢ | 4 |
| H2 main | 4 | 40 | **3** | **3** | **3** | **3** | **3** | **3** | **3** | **3** |
| H2 alt A | 4 | 40 | **3** | **3** | **3** | **3** | **3** | **3** | **3** | **3** |
| H2 alt B | 4 | 39 | **3** | **3** | **3** | **3** | **3** | **3** | **3** | **3** |
| H3 main | 4 | 40 | 4 | 4 | 4 | 4 | **3** | 4 | 4 | **3** |
| H3 alt A | 4 | 41 | 4 | 4 | 4ᵇ | 4 | **3** | 4 | 4ᵇ | **3** |
| H3 alt B | 4 | 39 | 4 | 4 | 4 | 4 | **3** | 4 | 4 | **3** |
| H4 main | 4 | 32 | 5 | **3**ˢ | 4ᵇ | 5 | 4 | 5 | **3**ˢᵇ | 5 |
| H4 alt A | 4 | 33 | 4 | **3**ˢ | 4ᵇ | 4 | 4 | 4 | **3**ˢᵇ | 4 |
| H4 alt B | 4 | 31 | 5 | **3**ˢ | 4ᵇ | 5 | 4 | 5 | **3**ˢᵇ | 5 |
| H5 main | 5 | 39 | **4** | **4** | **4**ᵇ | **4** | 5 | **4** | **4**ᵇ | **4** |
| H5 alt A | 5 | 38 | **4** | **4** | **3**ᵇ | **4** | 5 | **4** | **3**ᵇ | **4** |
| H5 alt B | 5 | 39 | **4** | **4** | **4**ᵇ | **4** | 5 | **4** | **4**ᵇ | **4** |

ˢ a water skip was used · ᵇ a Bank Shot rebound was used. Stuck plays: 0.

## Mean strokes per hole, good bot

Mean over all pins and 3 seeds; penalties included; the hazard rate is penalties per play.

| Pin | Par | Walk (m) | none | skip | bank | rough | heavy | magnet | ricochet | groundbr. | hazards/play |
|---|---|---|---|---|---|---|---|---|---|---|---|
| H1 The Lantern Gate | 4 | 41 | 4.89 | 3.33 | 6.56 (1x) | 7.56 (3x) | 4.33 | 6.44 (2x) | 3.22 | 5.44 | 0.31 |
| H2 The Sunken Parterre | 4 | 40 | 3.22 | 3.22 | 3.00 | 3.22 | 3.44 | 3.33 | 3.67 | 3.22 | 0.00 |
| H3 The Sluice Walk | 4 | 40 | 4.33 | 4.11 | 4.33 | 4.22 | 3.56 | 4.56 | 4.44 | 3.44 | 0.00 |
| H4 The Flooded Cloister | 4 | 32 | 8.44 (2x) | 6.44 | 7.44 (1x) | 8.00 (1x) | 4.67 | 7.67 | 6.00 (1x) | 7.67 | 1.44 |
| H5 The Orrery | 5 | 39 | 5.44 | 8.22 (2x) | 6.11 (1x) | 4.67 | 7.11 (1x) | 9.78 (4x) | 6.00 (1x) | 5.67 | 0.71 |

## Mean strokes per hole, casual bot

Mean over all pins and 4 seeds; penalties included; the hazard rate is penalties per play.

| Pin | Par | Walk (m) | none | skip | bank | rough | heavy | magnet | ricochet | groundbr. | hazards/play |
|---|---|---|---|---|---|---|---|---|---|---|---|
| H1 The Lantern Gate | 4 | 41 | 6.50 (2x) | 3.58 | 6.08 | 6.67 (2x) | 6.00 (1x) | 5.25 | 4.08 | 8.92 (4x) | 0.35 |
| H2 The Sunken Parterre | 4 | 40 | 3.58 | 4.00 | 3.50 | 4.00 | 4.83 | 3.92 | 5.08 (1x) | 4.00 | 0.00 |
| H3 The Sluice Walk | 4 | 40 | 5.08 | 5.58 (1x) | 5.83 (1x) | 7.50 (3x) | 4.25 | 6.75 (2x) | 5.67 (1x) | 4.08 | 0.02 |
| H4 The Flooded Cloister | 4 | 32 | 8.25 (2x) | 11.00 (4x) | 7.92 (1x) | 8.58 (2x) | 5.17 | 7.42 | 7.67 (1x) | 7.42 | 1.90 |
| H5 The Orrery | 5 | 39 | 7.83 (2x) | 7.33 (2x) | 9.75 (5x) | 8.00 (4x) | 6.50 | 9.00 (3x) | 9.08 (5x) | 8.67 (2x) | 0.91 |

## Whole runs

Runs played through `RunRules` with random pins, the first offered upgrade taken after each hole, and Second Chance used on the first hazard. The budget was unlimited while playing; each table cell is the share of those runs that a given budget would have let through.

**precise** — 10 runs, mean 18.3 strokes for par 21 (stuck: 0).

| start \ restore | 2 | 3 | 4 |
|---|---|---|---|
| 8 | 60 % | 100 % | 100 % |
| 9 | 70 % | 100 % | 100 % |
| 10 | 100 % | 100 % | 100 % |
| 11 | 100 % | **100 %** | 100 % |
| 12 | 100 % | 100 % | 100 % |
| 13 | 100 % | 100 % | 100 % |
| 14 | 100 % | 100 % | 100 % |

Where runs end at the current budget: won: 10.

**good** — 40 runs, mean 22.4 strokes for par 21 (stuck: 3).

| start \ restore | 2 | 3 | 4 |
|---|---|---|---|
| 8 | 3 % | 38 % | 80 % |
| 9 | 5 % | 50 % | 88 % |
| 10 | 10 % | 63 % | 90 % |
| 11 | 23 % | **70 %** | 90 % |
| 12 | 38 % | 83 % | 90 % |
| 13 | 50 % | 90 % | 90 % |
| 14 | 63 % | 90 % | 90 % |

Where runs end at the current budget: lost on H1: 1, lost on H4: 3, lost on H5: 8, won: 28.

**casual** — 40 runs, mean 26.7 strokes for par 21 (stuck: 14).

| start \ restore | 2 | 3 | 4 |
|---|---|---|---|
| 8 | 0 % | 3 % | 25 % |
| 9 | 3 % | 8 % | 28 % |
| 10 | 3 % | 18 % | 35 % |
| 11 | 3 % | **20 %** | 40 % |
| 12 | 3 % | 25 % | 48 % |
| 13 | 8 % | 28 % | 50 % |
| 14 | 18 % | 35 % | 50 % |

Where runs end at the current budget: lost on H1: 2, lost on H3: 2, lost on H4: 12, lost on H5: 16, won: 8.

_6,981 bot shots played (each planned from about 1,100 trial simulations) in 6067 s._
