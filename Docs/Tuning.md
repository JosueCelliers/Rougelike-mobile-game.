# BAD LIE — tuning

Every number that shapes how the game plays, and the playtest data behind it. Values live
in code next to what they control:
- Physics: `Scripts/Core/Sim/SimTuning.cs`
- Upgrades: `Scripts/Core/Run/Upgrades.cs`
- Run economy: `Scripts/Core/Run/RunRules.cs`

## 1. Ball physics

The ball is simulated deterministically at 240 Hz, in fixed steps. The shot preview runs the
same code for a shorter distance, so it is always a prefix of the real shot. No value below
is random.

| Constant | Value | Why |
|---|---|---|
| Launch speed | 0.45–9.6 m/s, `min + (max − min)·√power` | The square root gives fine control on short putts |
| Ball radius | 0.14 m | Readable at phone zoom; the cup is 0.32 m |
| Slope factor | 5/7 | Rolling solid sphere |
| Step up / down | 0.035 m | Height changes below this are rolled over; above it is a ledge |
| Cup capture | 0.30 m radius. The speed limit is 2.35 m/s through the centre and shrinks with the chord across the cup, with a 0.35 m/s floor so a dying ball at the rim always drops | Forgiving but not magnetic; fast balls lip out (keep 82%, deflect ≤ 24°) |
| Walls (restitution / tangent keep) | stone 0.56/0.90, hedge 0.26/0.80, metal 0.62/0.92, ledge 0.42/0.86 | Hedges swallow shots, stone and metal rebound |
| Runnel and vent coupling | 2.6 | Visible but beatable push |

Surfaces (rolling deceleration a + b·v):

| Surface | a (m/s²) | b (1/s) | Landing keeps | Full-power roll on the flat | Half-power roll |
|---|---|---|---|---|---|
| Green | 1.30 | 0.08 | 93% | 25.7 m | 14.4 m |
| Runnel | 1.20 | 0.10 | 80% | 25.5 m (plus the flow) | 14.6 m |
| Stone | 1.60 | 0.10 | 95% | 20.8 m | 11.7 m |
| Fairway | 2.15 | 0.12 | 90% | 16.0 m | 8.9 m |
| Rough | 5.40 | 0.48 | 62% | 5.6 m | 3.2 m |
| Sand | 9.50 | 1.45 | 30% | 2.6 m | 1.5 m |

The five playable surfaces differ by a factor of six in roll distance, so the choice of lie
matters on every shot. Water is a hazard; Skip Stone is the only exception.

## 2. Upgrades

| Upgrade | Values | Effect on a full-power fairway roll |
|---|---|---|
| Skip Stone | Skips at ≥ 3 m/s; keeps 86% horizontal speed; vertical 0.42 × speed (max 3.4 m/s); once per shot | — |
| Bank Shot | First rebound: restitution 0.92, tangent 0.985 (stone is 0.56 / 0.90) | — |
| Rough Rider | Rough deceleration × 0.42; rough landings keep 85% | Rough roll 5.6 m → 13.2 m |
| Heavy Core | Flow and vent coupling × 0.25; launch speed × 0.88 | 16.0 m → 12.7 m |
| Cup Magnet | On the green below 1.5 m/s: capture radius 0.43 m, pull 0.9 m/s² within 1.15 m | — |
| Second Chance | One undo per run (stroke and penalty refunded) | — |
| Ricochet (Skip + Bank) | A skip re-arms Bank Shot | — |
| Groundbreaker (Heavy + Rough) | Sand deceleration × 0.6; Heavy Core speed × 0.94 instead of 0.88 | 16.0 m → 14.2 m |

## 3. Playtesting method

The editor tool `BadLie.EditorTools.Playtest` (menu **BAD LIE ▸ Playtest (bots)**) uses a
bot that plans every shot properly:
- It builds a walking-distance field to the cup: Dijkstra over the course, where drops are
  one-way.
- It simulates about 1,100 candidate shots (72 directions × 12 powers, then local
  refinement) with the real simulation.
- It picks the shot that leaves the ball closest to the cup by walking distance. Rough and
  sand cost a little extra; water or out of bounds is very costly.

Execution error is then added to the chosen shot. The planner does not know about this
error, so noisy bots play greedy lines a careful human would avoid. Their numbers are a
pessimistic stand-in for a player.

| Skill | Aim error | Power error |
|---|---|---|
| precise | 0 | 0 |
| good | ±1.5° | ±4% |
| casual | ±3.5° | ±9% |

The tool has two parts.
- **Holes:** every pin (3 per hole) with every loadout (none, each upgrade, both combos) and
  every skill level.
- **Runs:** whole runs through the real `RunRules`, with random pins, the first offered
  upgrade taken each time, and Second Chance used on the first hazard. The budget is
  unlimited while playing; the recorded strokes per hole are then scored against a grid of
  budgets.

Reports: `Docs/Playtest/pass1_report.md` and `Docs/Playtest/pass2_report.md`. Raw run data:
the matching `*_runs.csv` files.

## 4. Pass 1: what the bots found

6,981 bot shots, about 7.7 million simulated trial shots. Full report:
`Docs/Playtest/pass1_report.md`.

- **Every hole is completable.** The precise bot holed all 15 pins with all 8 loadouts, with no
  stuck plays. The `PlaytestTests` EditMode tests now guard this: every pin has a rolling
  route from the tee, and the bot holes every hole within par + 1.
- **Upgrades open real routes.** Skip Stone and Ricochet turn holes 1 and 4 into birdies by
  skipping the canal and court. Heavy Core and Groundbreaker birdie the Sluice Walk by
  ignoring the runnels. Bank Shot finds a 3 on an Orrery pin.
- **The Sunken Parterre was a free birdie.** Every bot, with every loadout, took about three on
  a par 4 (precise 3, good 3.2, casual 3.6). The drop off the terrace carries a long way.
- **The Flooded Cloister and the Orrery punished small errors too hard.** Precise play takes
  4–5, but good bots averaged 8.4 and 5.4 strokes, and casual bots 8.3 and 7.8, with 1.4–1.9
  water hazards per play on the cloister. The causes:
  - The cloister walkway and the Orrery dais had no lip, so a shot a degree off rolled
    straight into the water.
  - The bridges were narrower than the aim error at that distance.
- **Run win rates at the original budget** (start 11, +3 per hole, +1 under par): precise
  100%, good 70%, casual 20%. Good-bot runs were mostly lost on holes 4 and 5.

### Changes made from pass 1
1. **Hole 2 becomes the run's par 3.** The par now matches what the hole asks for, and it no
   longer hands out a free under-par bonus.
2. **Hole 4:** a low coping runs along the court edge, open only at the bridge and in front of
   the stepping stone (the skip route), and the bridge is wider (1.8 → 2.4 m). Missing the
   line now costs a rebound instead of a penalty, unless you go for the skip.
3. **Hole 5:** a low coping rings the moat, open at both bridges and at a skip gap facing the
   ramp, and the bridges are wider (1.7 → 2.3 m).

## 5. Pass 2 and the stroke budget

The same tool was run again after the changes: the precise bot with every loadout, and the
noisy bots with no upgrades. Full report: `Docs/Playtest/pass2_report.md`.

Mean strokes with no upgrades, over every pin. "(n×)" means n plays failed to hole out
within 14 strokes; these stuck plays inflate the mean.

| Hole | Par | Precise | Good: pass 1 → 2 | Casual: pass 1 → 2 |
|---|---|---|---|---|
| 1 The Lantern Gate | 4 | 4 | 4.89 → 4.89 | 6.50 (2×) → 6.50 (2×) |
| 2 The Sunken Parterre | 4 → **3** | 3 | 3.22 → 4.44 (1×) | 3.58 → 3.58 |
| 3 The Sluice Walk | 4 | 4 | 4.33 → 4.33 | 5.08 → 5.08 |
| 4 The Flooded Cloister | 4 | 4–5 → 4 | 8.44 (2×) → **5.33** (1×) | 8.25 (2×) → **4.58** |
| 5 The Orrery | 5 | 3–4 | 5.44 → 7.22 (2×) | 7.83 (2×) → 7.58 (3×) |

Notes on the table:
- **Holes 1 and 3 did not change,** and their numbers match pass 1 exactly. Same seeds give
  the same shots: a determinism check that came for free.
- **Hole 4 now plays like a par 4.** Water penalties per good-bot play fell from 1.4 to 0.6.
- **Hole 2's good-bot mean** includes one stuck play. The apron ramp was made steeper in the
  visual pass to remove a seam, and a slightly short putt now rolls back down it. A greedy bot
  repeats that putt; a person adjusts.
- **The Orrery is the run's hardest hole for imprecise play.** Its skip gap faces the
  natural line from the ramp. It is the last hole, so the run's tension holds to the end.

Whole runs at the shipped budget (start 11, +3 per hole, +1 under par), pass 1 → pass 2:

| Bot | Mean strokes (par 21 → 20) | Run win rate |
|---|---|---|
| precise | 18.3 → 18.3 | 100% → 100% |
| good | 22.4 → 21.6 | 70% → **75%** |
| casual | 26.7 → 24.7 | 20% → **40%** |

Budget grid for pass 2 (share of runs that each budget would let through):

| Start \ restore | good: +2 | good: +3 | good: +4 | casual: +2 | casual: +3 | casual: +4 |
|---|---|---|---|---|---|---|
| 9 | 3% | 63% | 80% | 0% | 13% | 50% |
| 11 | 20% | **75%** | 88% | 0% | **40%** | 60% |
| 13 | 63% | 80% | 90% | 13% | 50% | 78% |

**Decision: keep start 11, +3 per hole and +1 under par.**
- A precise player always wins, a good player wins about three runs in four, and a casual
  player about two in five.
- New players start near casual, so a first win is reachable, and the bar rises with skill
  rather than luck. There is no randomness in the shots.
- Restoring +4 would make good play near-certain (88%). Restoring +2 makes even good play a
  coin toss or worse.
- Losses cluster on holes 4 and 5, so runs stay tense to the end. A run is not decided at
  hole 2.

**Run length.** The good bot takes about 22 shots per run. The ball travels for about 3.5 s
per shot in playback. Adding about 4 s to aim each shot, five hole cards and four upgrade
choices gives roughly 5–6 minutes per run, inside the 5–10 minute target. This is an
estimate from bot stroke counts; it has not been timed with people.

## 6. Scene cost per hole (desktop measurement)

Measured in the Linux player at 1080×2340. These are whole-hole totals before frustum
culling; the camera sees a part of each. Every material is a custom URP shader with vertex
colour and no texture arrays, and ground, walls and dressing are static-batched.

| Hole | Triangles | Renderers (shadow casters) | Materials |
|---|---|---|---|
| The Lantern Gate | 360,696 | 557 (540) | 7 |
| The Sunken Parterre | 316,022 | 604 (592) | 6 |
| The Sluice Walk | 209,780 | 266 (254) | 6 |
| The Flooded Cloister | 219,358 | 197 (187) | 7 |
| The Orrery | 228,480 | 439 (419) | 7 |

**Not measured on a phone.** Frame time, draw calls after batching, thermals and memory need
a device. The most likely costs are:
- Terrain tops, which are meshed at 0.25 m, and the full-screen terrain and water shaders.
- The 2048 px shadow map.

If a target device struggles, these are the levers:
- Mesh out-of-bounds pads at 0.5 m.
- Lower the shadow resolution or distance per hole.
- Set the URP asset render scale to 0.8.

## 7. Re-running

```bash
Tools/unity/unity.sh playtest -nographics -quit -executeMethod BadLie.EditorTools.Playtest.Run \
  -playtestParts holes,runs -playtestRuns 40 [-playtestNoisy none]
# -> Logs/playtest.md and Logs/playtest_runs.csv
```
A full pass takes about an hour on 4 cores. `-playtestNoisy none` limits the noisy bots to the
no-upgrade loadout, which takes about 20 minutes.
