# BAD LIE bot playtest

Generated 2026-10-06 01:51 UTC on 4 threads. Budget under test: start 11, restore 3/hole, under-par bonus 1, hazard penalty 1.

Skill models (uniform execution error, the planner does not know about it): precise ±0° aim, ±0 % power; good ±1.5° aim, ±4 % power; casual ±3.5° aim, ±9 % power.

## Every pin and loadout, precise bot

Strokes including penalties. `x` = not holed within 14. Bold = under par.

| Pin | Par | Walk (m) | none | skip | bank | rough | heavy | magnet | ricochet | groundbr. |
|---|---|---|---|---|---|---|---|---|---|---|
| H1 main | 4 | 41 | 4 | **3**ˢ | 4ᵇ | 4 | 4 | 4 | **3**ˢ | 4 |
| H1 alt A | 4 | 38 | 4 | **3**ˢ | 4ᵇ | **3** | 4 | 4 | **3**ˢ | 4 |
| H1 alt B | 4 | 41 | 4 | **3**ˢ | 4ᵇ | 4 | 4 | 4 | **3**ˢ | 4 |
| H2 main | 3 | 40 | 3 | 3 | 3 | 3 | 3 | 3 | 3 | 3 |
| H2 alt A | 3 | 40 | 3 | 3 | 3 | 3 | 3 | 3 | 3 | 3 |
| H2 alt B | 3 | 39 | 3 | 3 | 3ᵇ | 3 | 3 | 3 | 3ᵇ | 3 |
| H3 main | 4 | 40 | 4 | 4 | 4 | 4 | **3** | 4 | 4 | **3** |
| H3 alt A | 4 | 41 | 4 | 4 | 4ᵇ | 4 | **3** | 4 | 4ᵇ | **3** |
| H3 alt B | 4 | 39 | 4 | 4 | 4 | 4 | **3** | 4 | 4 | **3** |
| H4 main | 4 | 33 | 4 | 4 | **3**ᵇ | 4 | 4 | 4 | **3**ᵇ | 4 |
| H4 alt A | 4 | 34 | 4 | 4 | **3**ᵇ | 4 | 4 | 4 | **3**ᵇ | 4 |
| H4 alt B | 4 | 32 | 4 | 4 | 4ᵇ | 4 | 4 | 4 | 4ᵇ | 4 |
| H5 main | 5 | 39 | **4** | **4** | **4**ᵇ | **4** | **4** | **4** | **4**ᵇ | **4** |
| H5 alt A | 5 | 38 | **3** | **3** | **3**ᵇ | **3** | **4** | **3** | **3**ᵇ | **4** |
| H5 alt B | 5 | 39 | **4** | **4** | **3**ᵇ | **4** | **4** | **4** | **3**ᵇ | **4** |

ˢ a water skip was used · ᵇ a Bank Shot rebound was used. Stuck plays: 0.

## Mean strokes per hole, good bot

Mean over all pins and 3 seeds; penalties included; the hazard rate is penalties per play.

| Pin | Par | Walk (m) | none | skip | bank | rough | heavy | magnet | ricochet | groundbr. | hazards/play |
|---|---|---|---|---|---|---|---|---|---|---|---|
| H1 The Lantern Gate | 4 | 41 | 4.89 | – | – | – | – | – | – | – | 0.33 |
| H2 The Sunken Parterre | 3 | 40 | 4.44 (1x) | – | – | – | – | – | – | – | 0.00 |
| H3 The Sluice Walk | 4 | 40 | 4.33 | – | – | – | – | – | – | – | 0.00 |
| H4 The Flooded Cloister | 4 | 33 | 5.33 (1x) | – | – | – | – | – | – | – | 0.56 |
| H5 The Orrery | 5 | 39 | 7.22 (2x) | – | – | – | – | – | – | – | 1.22 |

## Mean strokes per hole, casual bot

Mean over all pins and 4 seeds; penalties included; the hazard rate is penalties per play.

| Pin | Par | Walk (m) | none | skip | bank | rough | heavy | magnet | ricochet | groundbr. | hazards/play |
|---|---|---|---|---|---|---|---|---|---|---|---|
| H1 The Lantern Gate | 4 | 41 | 6.50 (2x) | – | – | – | – | – | – | – | 0.17 |
| H2 The Sunken Parterre | 3 | 40 | 3.58 | – | – | – | – | – | – | – | 0.00 |
| H3 The Sluice Walk | 4 | 40 | 5.08 | – | – | – | – | – | – | – | 0.00 |
| H4 The Flooded Cloister | 4 | 33 | 4.58 | – | – | – | – | – | – | – | 0.08 |
| H5 The Orrery | 5 | 39 | 7.58 (3x) | – | – | – | – | – | – | – | 0.17 |

## Whole runs

Runs played through `RunRules` with random pins, the first offered upgrade taken after each hole, and Second Chance used on the first hazard. The budget was unlimited while playing; each table cell is the share of those runs that a given budget would have let through.

**precise** — 10 runs, mean 18.3 strokes for par 20 (stuck: 0).

| start \ restore | 2 | 3 | 4 |
|---|---|---|---|
| 8 | 10 % | 100 % | 100 % |
| 9 | 60 % | 100 % | 100 % |
| 10 | 60 % | 100 % | 100 % |
| 11 | 100 % | **100 %** | 100 % |
| 12 | 100 % | 100 % | 100 % |
| 13 | 100 % | 100 % | 100 % |
| 14 | 100 % | 100 % | 100 % |

Where runs end at the current budget: won: 10.

**good** — 40 runs, mean 21.6 strokes for par 20 (stuck: 3).

| start \ restore | 2 | 3 | 4 |
|---|---|---|---|
| 8 | 0 % | 40 % | 78 % |
| 9 | 3 % | 63 % | 80 % |
| 10 | 8 % | 73 % | 83 % |
| 11 | 20 % | **75 %** | 88 % |
| 12 | 40 % | 78 % | 88 % |
| 13 | 63 % | 80 % | 90 % |
| 14 | 73 % | 83 % | 93 % |

Where runs end at the current budget: lost on H1: 1, lost on H4: 3, lost on H5: 6, won: 30.

**casual** — 40 runs, mean 24.7 strokes for par 20 (stuck: 8).

| start \ restore | 2 | 3 | 4 |
|---|---|---|---|
| 8 | 0 % | 3 % | 40 % |
| 9 | 0 % | 13 % | 50 % |
| 10 | 0 % | 25 % | 58 % |
| 11 | 0 % | **40 %** | 60 % |
| 12 | 3 % | 40 % | 70 % |
| 13 | 13 % | 50 % | 78 % |
| 14 | 25 % | 58 % | 78 % |

Where runs end at the current budget: lost on H1: 2, lost on H3: 1, lost on H4: 11, lost on H5: 10, won: 16.

_2,988 bot shots played (each planned from about 1,100 trial simulations) in 3056 s._
