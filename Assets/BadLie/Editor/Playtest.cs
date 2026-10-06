using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BadLie.Bot;
using BadLie.Core;
using BadLie.Course;
using BadLie.Holes;
using BadLie.Run;
using BadLie.Sim;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BadLie.EditorTools
{
    /// <summary>
    /// Bot playtests (see Docs/Tuning.md).
    /// Part "holes": the shot planner plays every hole, pin and upgrade loadout at three
    /// execution-skill levels. Part "runs": whole runs through the real run rules with an
    /// unlimited budget, recording strokes per hole; those runs are then scored offline against
    /// a grid of stroke-budget settings. Writes Logs/playtest.md and Logs/playtest_runs.csv.
    ///   Unity -batchmode -nographics -quit -executeMethod BadLie.EditorTools.Playtest.Run
    ///         [-playtestParts quick|holes,runs] [-playtestRuns 48]
    /// </summary>
    public static class Playtest
    {
        struct Skill
        {
            public string Name;
            /// <summary>Uniform aim error, degrees either side.</summary>
            public float AngleError;
            /// <summary>Uniform power error, as a fraction either side.</summary>
            public float PowerError;
            public int Seeds;
        }

        static readonly Skill[] Skills =
        {
            new Skill { Name = "precise", AngleError = 0f, PowerError = 0f, Seeds = 1 },
            new Skill { Name = "good", AngleError = 1.5f, PowerError = 0.04f, Seeds = 3 },
            new Skill { Name = "casual", AngleError = 3.5f, PowerError = 0.09f, Seeds = 4 },
        };

        struct Loadout
        {
            public string Name;
            public UpgradeId[] Ids;
        }

        static readonly Loadout[] Loadouts =
        {
            new Loadout { Name = "none", Ids = new UpgradeId[0] },
            new Loadout { Name = "skip", Ids = new[] { UpgradeId.SkipStone } },
            new Loadout { Name = "bank", Ids = new[] { UpgradeId.BankShot } },
            new Loadout { Name = "rough", Ids = new[] { UpgradeId.RoughRider } },
            new Loadout { Name = "heavy", Ids = new[] { UpgradeId.HeavyCore } },
            new Loadout { Name = "magnet", Ids = new[] { UpgradeId.CupMagnet } },
            new Loadout { Name = "ricochet", Ids = new[] { UpgradeId.SkipStone, UpgradeId.BankShot } },
            new Loadout { Name = "groundbr.", Ids = new[] { UpgradeId.HeavyCore, UpgradeId.RoughRider } },
        };

        /// <summary>A hole the bot has not finished in this many strokes counts as stuck.</summary>
        const int HoleCap = 14;

        sealed class Pin
        {
            public int Hole, Variant, Par;
            public string Name;
            public DistanceField Field;
            public float CupWalk;
            public string Label { get { return "H" + (Hole + 1) + (Variant < 0 ? " main" : " alt " + (char)('A' + Variant)); } }
        }

        sealed class HoleJob
        {
            public Pin Pin;
            public int Loadout, Skill, Seed;
            public BotPlayer.HoleRun Result;
        }

        sealed class RunLog
        {
            public string Skill;
            public long Seed;
            public int[] Strokes = new int[RunRules.HolesPerRun];
            public int[] Par = new int[RunRules.HolesPerRun];
            public string[] Pins = new string[RunRules.HolesPerRun];
            public string Upgrades = "";
            public int Hazards, Undos, Shots;
            public int StuckAt = -1;
        }

        static List<Pin> pins;
        static readonly ThreadLocal<Dictionary<int, ShotPlanner>> planners = new ThreadLocal<Dictionary<int, ShotPlanner>>(() => new Dictionary<int, ShotPlanner>());
        static long simCount;

        static int Key(int hole, int variant) { return hole * 8 + variant + 1; }

        /// <summary>One planner (and course model) per thread and pin: course models keep
        /// per-query scratch state, so they are never shared between threads.</summary>
        static ShotPlanner PlannerFor(Pin pin)
        {
            var map = planners.Value;
            ShotPlanner p;
            if (!map.TryGetValue(Key(pin.Hole, pin.Variant), out p))
            {
                var course = new CourseModel(HoleLibrary.Get(pin.Hole), null, pin.Variant);
                p = new ShotPlanner(course, pin.Field);
                map[Key(pin.Hole, pin.Variant)] = p;
            }
            return p;
        }

        static Pin FindPin(int hole, int variant)
        {
            foreach (var p in pins) if (p.Hole == hole && p.Variant == variant) return p;
            return null;
        }

        static ShotModifiers ModsFor(UpgradeId[] ids) { return RunUpgrades.ModifiersFor(new List<UpgradeId>(ids)); }

        [MenuItem("BAD LIE/Playtest (bots)")]
        public static void Run()
        {
            string parts = Arg("-playtestParts", "holes,runs");
            int runs = int.Parse(Arg("-playtestRuns", "48"), CultureInfo.InvariantCulture);
            var report = new StringBuilder();
            var total = Stopwatch.StartNew();
            try
            {
                BuildPins();
                report.AppendLine("# BAD LIE bot playtest");
                report.AppendLine();
                report.AppendLine(string.Format(CultureInfo.InvariantCulture, "Generated {0:yyyy-MM-dd HH:mm} UTC on {1} threads. Budget under test: start {2}, restore {3}/hole, under-par bonus {4}, hazard penalty {5}.",
                    DateTime.UtcNow, Environment.ProcessorCount, RunRules.StartStrokes, RunRules.RestorePerHole, RunRules.UnderParBonus, RunRules.HazardPenalty));
                report.AppendLine();
                report.AppendLine("Skill models (uniform execution error, the planner does not know about it): " +
                    string.Join("; ", Array.ConvertAll(Skills, s => string.Format(CultureInfo.InvariantCulture, "{0} ±{1}° aim, ±{2:P0} power", s.Name, s.AngleError, s.PowerError))) + ".");
                report.AppendLine();
                if (parts.Contains("quick")) Quick(report);
                if (parts.Contains("holes")) Holes(report);
                if (parts.Contains("runs")) Runs(report, runs);
                report.AppendLine(string.Format(CultureInfo.InvariantCulture, "_{0:N0} bot shots played (each planned from about 1,100 trial simulations) in {1:F0} s._", Interlocked.Read(ref simCount), total.Elapsed.TotalSeconds));
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/playtest.md", report.ToString());
                Debug.Log("[BadLie] Playtest written to Logs/playtest.md\n" + report);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        static string Arg(string name, string fallback)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return fallback;
        }

        static void BuildPins()
        {
            pins = new List<Pin>();
            for (int h = 0; h < HoleLibrary.Available; h++)
            {
                var def = HoleLibrary.Get(h);
                for (int v = -1; v < def.AltCups.Count; v++) pins.Add(new Pin { Hole = h, Variant = v, Par = def.Par, Name = def.Name });
            }
            Parallel.ForEach(pins, pin =>
            {
                var course = new CourseModel(HoleLibrary.Get(pin.Hole), null, pin.Variant);
                pin.Field = new DistanceField(course);
                pin.CupWalk = pin.Field.Distance(course.Tee);
            });
        }

        // ------------------------------------------------------------------ quick
        static void Quick(StringBuilder report)
        {
            report.AppendLine("## Quick check: precise bot, no upgrades");
            report.AppendLine();
            report.AppendLine("| Pin | Par | Walk to cup (m) | Strokes | Time (s) |");
            report.AppendLine("|---|---|---|---|---|");
            foreach (var pin in pins)
            {
                var sw = Stopwatch.StartNew();
                var planner = PlannerFor(pin);
                var r = BotPlayer.Play(planner, planner.Course, ShotModifiers.None, HoleCap, 0f, 0f, 1);
                Interlocked.Add(ref simCount, r.Strokes + r.Penalties);
                report.AppendLine(string.Format(CultureInfo.InvariantCulture, "| {0} | {1} | {2:F1} | {3}{4} | {5:F1} |", pin.Label, pin.Par, pin.CupWalk, r.Strokes + r.Penalties, r.Holed ? "" : " (stuck)", sw.Elapsed.TotalSeconds));
            }
            report.AppendLine();
        }

        // ------------------------------------------------------------------ holes
        static void Holes(StringBuilder report)
        {
            // -playtestNoisy none: noisy bots only play without upgrades (much faster).
            bool noisyAll = Arg("-playtestNoisy", "all") == "all";
            var jobs = new List<HoleJob>();
            foreach (var pin in pins)
                for (int l = 0; l < Loadouts.Length; l++)
                    for (int s = 0; s < Skills.Length; s++)
                    {
                        if (s > 0 && l > 0 && !noisyAll) continue;
                        for (int seed = 0; seed < Skills[s].Seeds; seed++)
                            jobs.Add(new HoleJob { Pin = pin, Loadout = l, Skill = s, Seed = seed });
                    }
            var sw = Stopwatch.StartNew();
            Parallel.ForEach(jobs, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, job =>
            {
                var planner = PlannerFor(job.Pin);
                var sk = Skills[job.Skill];
                ulong seed = (ulong)(job.Pin.Hole * 7919 + (job.Pin.Variant + 1) * 104729 + job.Loadout * 1299709 + job.Seed * 15485863 + 17);
                job.Result = BotPlayer.Play(planner, planner.Course, ModsFor(Loadouts[job.Loadout].Ids), HoleCap, sk.AngleError, sk.PowerError, seed);
                Interlocked.Add(ref simCount, job.Result.Strokes);
            });
            Debug.Log(string.Format("[BadLie] Playtest holes: {0} hole plays in {1:F0} s", jobs.Count, sw.Elapsed.TotalSeconds));

            // Precise bot: one deterministic play per pin and loadout.
            report.AppendLine("## Every pin and loadout, precise bot");
            report.AppendLine();
            report.AppendLine("Strokes including penalties. `x` = not holed within " + HoleCap + ". Bold = under par.");
            report.AppendLine();
            var head = new StringBuilder("| Pin | Par | Walk (m) |");
            var rule = new StringBuilder("|---|---|---|");
            foreach (var l in Loadouts)
            {
                head.Append(' ').Append(l.Name).Append(" |");
                rule.Append("---|");
            }
            report.AppendLine(head.ToString());
            report.AppendLine(rule.ToString());
            int stuck = 0;
            foreach (var pin in pins)
            {
                var row = new StringBuilder(string.Format(CultureInfo.InvariantCulture, "| {0} | {1} | {2:F0} |", pin.Label, pin.Par, pin.CupWalk));
                for (int l = 0; l < Loadouts.Length; l++)
                {
                    var job = jobs.Find(j => j.Pin == pin && j.Loadout == l && j.Skill == 0);
                    var r = job.Result;
                    int n = r.Strokes + r.Penalties;
                    if (!r.Holed) stuck++;
                    string cell = !r.Holed ? "x" : (n < pin.Par ? "**" + n + "**" : n.ToString());
                    if (r.Skips > 0) cell += "ˢ";
                    if (r.Banks > 0) cell += "ᵇ";
                    row.Append(' ').Append(cell).Append(" |");
                }
                report.AppendLine(row.ToString());
            }
            report.AppendLine();
            report.AppendLine("ˢ a water skip was used · ᵇ a Bank Shot rebound was used. Stuck plays: " + stuck + ".");
            report.AppendLine();

            // Noisy bots: mean strokes per hole (over pins and seeds) by loadout.
            for (int s = 1; s < Skills.Length; s++)
            {
                report.AppendLine("## Mean strokes per hole, " + Skills[s].Name + " bot");
                report.AppendLine();
                report.AppendLine("Mean over all pins and " + Skills[s].Seeds + " seeds; penalties included; the hazard rate is penalties per play.");
                report.AppendLine();
                report.AppendLine(head + " hazards/play |");
                report.AppendLine(rule + "---|");
                for (int h = 0; h < HoleLibrary.Available; h++)
                {
                    var hpins = pins.FindAll(p => p.Hole == h);
                    var row = new StringBuilder(string.Format(CultureInfo.InvariantCulture, "| H{0} {1} | {2} | {3:F0} |", h + 1, hpins[0].Name, hpins[0].Par, hpins[0].CupWalk));
                    float pen = 0f;
                    int plays = 0;
                    for (int l = 0; l < Loadouts.Length; l++)
                    {
                        if (l > 0 && !noisyAll)
                        {
                            row.Append(" – |");
                            continue;
                        }
                        float sum = 0f;
                        int n = 0, fails = 0;
                        foreach (var job in jobs)
                        {
                            if (job.Pin.Hole != h || job.Loadout != l || job.Skill != s) continue;
                            sum += job.Result.Strokes + job.Result.Penalties;
                            pen += job.Result.Penalties;
                            plays++;
                            if (!job.Result.Holed) fails++;
                            n++;
                        }
                        row.Append(string.Format(CultureInfo.InvariantCulture, " {0:F2}{1} |", sum / Mathf.Max(1, n), fails > 0 ? " (" + fails + "x)" : ""));
                    }
                    row.Append(string.Format(CultureInfo.InvariantCulture, " {0:F2} |", pen / Mathf.Max(1, plays)));
                    report.AppendLine(row.ToString());
                }
                report.AppendLine();
            }
        }

        // ------------------------------------------------------------------ runs
        static void Runs(StringBuilder report, int runsPerSkill)
        {
            int saved = RunRules.StartStrokes;
            var logs = new List<RunLog>();
            var sw = Stopwatch.StartNew();
            try
            {
                // Unlimited budget: record how many strokes each hole takes, judge budgets afterwards.
                RunRules.StartStrokes = 999;
                var work = new List<KeyValuePair<int, long>>();
                for (int s = 0; s < Skills.Length; s++)
                    for (int i = 0; i < (s == 0 ? Mathf.Max(8, runsPerSkill / 4) : runsPerSkill); i++)
                        work.Add(new KeyValuePair<int, long>(s, 1000 + i));
                var results = new RunLog[work.Count];
                Parallel.For(0, work.Count, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
                {
                    results[i] = PlayRun(Skills[work[i].Key], work[i].Value);
                });
                logs.AddRange(results);
            }
            finally
            {
                RunRules.StartStrokes = saved;
            }
            Debug.Log(string.Format("[BadLie] Playtest runs: {0} runs in {1:F0} s", logs.Count, sw.Elapsed.TotalSeconds));

            var csv = new StringBuilder("skill,seed,upgrades,hazards,undos,stuck_at");
            for (int k = 0; k < RunRules.HolesPerRun; k++) csv.Append(",pin").Append(k + 1).Append(",par").Append(k + 1).Append(",strokes").Append(k + 1);
            csv.AppendLine();
            foreach (var r in logs)
            {
                csv.Append(r.Skill).Append(',').Append(r.Seed).Append(',').Append(r.Upgrades).Append(',').Append(r.Hazards).Append(',').Append(r.Undos).Append(',').Append(r.StuckAt);
                for (int k = 0; k < RunRules.HolesPerRun; k++) csv.Append(',').Append(r.Pins[k]).Append(',').Append(r.Par[k]).Append(',').Append(r.Strokes[k]);
                csv.AppendLine();
            }
            File.WriteAllText("Logs/playtest_runs.csv", csv.ToString());

            report.AppendLine("## Whole runs");
            report.AppendLine();
            report.AppendLine("Runs played through `RunRules` with random pins, the first offered upgrade taken after each hole, and Second Chance used on the first hazard. The budget was unlimited while playing; each table cell is the share of those runs that a given budget would have let through.");
            report.AppendLine();
            foreach (var sk in Skills)
            {
                var mine = logs.FindAll(l => l.Skill == sk.Name);
                float mean = 0f, par = 0f;
                int finished = 0;
                foreach (var l in mine)
                {
                    if (l.StuckAt >= 0) continue;
                    finished++;
                    for (int k = 0; k < RunRules.HolesPerRun; k++)
                    {
                        mean += l.Strokes[k];
                        par += l.Par[k];
                    }
                }
                report.AppendLine(string.Format(CultureInfo.InvariantCulture, "**{0}** — {1} runs, mean {2:F1} strokes for par {3:F0} (stuck: {4}).", sk.Name, mine.Count, mean / Mathf.Max(1, finished), par / Mathf.Max(1, finished), mine.Count - finished));
                report.AppendLine();
                report.AppendLine("| start \\ restore | 2 | 3 | 4 |");
                report.AppendLine("|---|---|---|---|");
                for (int start = 8; start <= 14; start++)
                {
                    var row = new StringBuilder("| " + start + " |");
                    for (int restore = 2; restore <= 4; restore++)
                    {
                        float rate = WinRate(mine, start, restore, RunRules.UnderParBonus);
                        bool current = start == RunRules.StartStrokes && restore == RunRules.RestorePerHole;
                        row.Append(string.Format(CultureInfo.InvariantCulture, current ? " **{0:P0}** |" : " {0:P0} |", rate));
                    }
                    report.AppendLine(row.ToString());
                }
                report.AppendLine();
                report.AppendLine("Where runs end at the current budget: " + LossProfile(mine, RunRules.StartStrokes, RunRules.RestorePerHole, RunRules.UnderParBonus) + ".");
                report.AppendLine();
            }
        }

        static RunLog PlayRun(Skill skill, long seed)
        {
            var alts = new List<int>();
            for (int i = 0; i < RunRules.HolesPerRun; i++) alts.Add(HoleLibrary.Get(i).AltCups.Count);
            var s = RunRules.NewRun(seed, alts);
            var log = new RunLog { Skill = skill.Name, Seed = seed };
            var rng = new DetRandom((ulong)seed, 1234);
            var sim = new BallSimulator();
            var result = new SimResult();
            ShotPlanner planner = null;
            Pin pin = null;
            int guard = 0;
            while (!s.Finished && guard++ < 600)
            {
                switch (s.Phase)
                {
                    case RunPhase.HoleIntro:
                        pin = FindPin(s.HoleOrder[s.HoleIndex], s.CupVariants[s.HoleIndex]);
                        planner = PlannerFor(pin);
                        log.Pins[s.HoleIndex] = pin.Label.Replace(' ', '-');
                        log.Par[s.HoleIndex] = pin.Par;
                        RunRules.BeginHole(s, planner.Course.Tee, planner.Course.TeeHeight);
                        break;
                    case RunPhase.Playing:
                    {
                        if (s.HoleStrokes >= HoleCap)
                        {
                            log.StuckAt = s.HoleIndex;
                            RunRules.Lose(s, pin.Name, pin.Par);
                            break;
                        }
                        var mods = RunUpgrades.ModifiersFor(s.OwnedUpgrades());
                        planner.Mods = mods;
                        var lie = new Vector2(s.LieX, s.LieZ);
                        float score;
                        var plan = planner.Plan(lie, s.LieY, out score);
                        var dir = Geo2D.Rotate(plan.Direction, rng.Signed() * skill.AngleError);
                        float power = Mathf.Clamp01(plan.Power * (1f + rng.Signed() * skill.PowerError));
                        var input = new ShotInput(dir, power);
                        RunRules.ReleaseShot(s, input);
                        sim.Simulate(planner.Course, new BallStart(lie, s.LieY), input, mods, SimOptions.Fast, result);
                        log.Shots++;
                        var rep = RunRules.ResolveShot(s, result, pin.Par, pin.Name, planner.Course.Cup);
                        if (rep.Holed) log.Strokes[s.History.Count - 1] = s.History[s.History.Count - 1].Strokes;
                        if (rep.Hazard) log.Hazards++;
                        if ((rep.Hazard || s.Phase == RunPhase.LastChance) && RunRules.CanUndo(s))
                        {
                            RunRules.Undo(s);
                            log.Undos++;
                        }
                        break;
                    }
                    case RunPhase.ChoosingUpgrade:
                        RunRules.Choose(s, s.Offer[0]);
                        break;
                    case RunPhase.LastChance:
                        RunRules.Lose(s, pin.Name, pin.Par);
                        break;
                }
            }
            if (!s.Finished) log.StuckAt = s.HoleIndex;
            Interlocked.Add(ref simCount, log.Shots);
            var names = new List<string>();
            foreach (var u in s.OwnedUpgrades()) names.Add(u.ToString());
            log.Upgrades = string.Join("+", names);
            return log;
        }

        /// <summary>Would this run have survived the given budget? Mirrors RunRules: a hole needs
        /// a stroke in hand for every shot, penalties included, and restores after holing out.</summary>
        static int EndHole(RunLog l, int start, int restore, int bonus)
        {
            if (l.StuckAt >= 0) return l.StuckAt;
            int strokes = start;
            for (int k = 0; k < RunRules.HolesPerRun; k++)
            {
                strokes -= l.Strokes[k];
                if (strokes < 0) return k;
                strokes += restore + (l.Strokes[k] <= l.Par[k] - 1 ? bonus : 0);
            }
            return -1;
        }

        static float WinRate(List<RunLog> logs, int start, int restore, int bonus)
        {
            if (logs.Count == 0) return 0f;
            int wins = 0;
            foreach (var l in logs) if (EndHole(l, start, restore, bonus) < 0) wins++;
            return wins / (float)logs.Count;
        }

        static string LossProfile(List<RunLog> logs, int start, int restore, int bonus)
        {
            var counts = new int[RunRules.HolesPerRun + 1];
            foreach (var l in logs)
            {
                int k = EndHole(l, start, restore, bonus);
                counts[k < 0 ? RunRules.HolesPerRun : k]++;
            }
            var parts = new List<string>();
            for (int k = 0; k < RunRules.HolesPerRun; k++) if (counts[k] > 0) parts.Add("lost on H" + (k + 1) + ": " + counts[k]);
            parts.Add("won: " + counts[RunRules.HolesPerRun]);
            return string.Join(", ", parts);
        }
    }
}
