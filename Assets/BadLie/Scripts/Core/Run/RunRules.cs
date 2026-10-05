using System.Collections.Generic;
using BadLie.Core;
using BadLie.Sim;
using UnityEngine;

namespace BadLie.Run
{
    public struct ShotReport
    {
        public bool Holed;
        public bool Hazard;
        public bool WaterHazard;
        public int Restored;
        public bool UnderPar;
        public bool RunWon;
        public bool RunLost;
        public bool LastChance;
        public string Headline;
        public string Detail;
    }

    /// <summary>
    /// The stroke-budget rules of a run, kept free of presentation so they can be tuned and
    /// tested on their own. Values here are tuning values (see Docs/Tuning.md).
    /// </summary>
    public static class RunRules
    {
        public static int StartStrokes = 11;
        public static int RestorePerHole = 3;
        public static int UnderParBonus = 1;
        public const int HazardPenalty = 1;
        public const int HolesPerRun = 5;

        public static RunState NewRun(long seed, IList<int> altCupCounts)
        {
            var s = new RunState { Seed = seed, Strokes = StartStrokes, Phase = RunPhase.HoleIntro };
            s.HoleOrder = new int[HolesPerRun];
            s.CupVariants = new int[HolesPerRun];
            var rng = new DetRandom((ulong)seed, 7);
            for (int i = 0; i < HolesPerRun; i++)
            {
                s.HoleOrder[i] = i;
                int alts = i < altCupCounts.Count ? altCupCounts[i] : 0;
                // Pin variation: the authored pin or one of the verified alternates.
                s.CupVariants[i] = alts > 0 ? rng.Range(-1, alts) : -1;
            }
            return s;
        }

        public static bool IsLastHole(RunState s) { return s.HoleIndex >= HolesPerRun - 1; }

        public static void BeginHole(RunState s, Vector2 tee, float teeHeight)
        {
            s.HoleStrokes = 0;
            s.HolePenalties = 0;
            s.HasLie = true;
            s.LieX = tee.x;
            s.LieZ = tee.y;
            s.LieY = teeHeight;
            s.UndoAvailable = false;
            s.PendingShot = false;
            s.Phase = RunPhase.Playing;
            s.LastEvent = "";
        }

        public static bool CanShoot(RunState s) { return s.Phase == RunPhase.Playing && s.Strokes > 0 && !s.PendingShot; }

        /// <summary>Spends a stroke. The shot is remembered until it resolves (for resume).</summary>
        public static void ReleaseShot(RunState s, ShotInput input)
        {
            s.UndoX = s.LieX;
            s.UndoZ = s.LieZ;
            s.UndoY = s.LieY;
            s.UndoStrokes = s.Strokes;
            s.UndoHoleStrokes = s.HoleStrokes;
            s.UndoPenalties = s.HolePenalties;
            s.UndoTotal = s.TotalStrokes;
            s.Strokes -= 1;
            s.HoleStrokes += 1;
            s.TotalStrokes += 1;
            s.PendingShot = true;
            s.PendDirX = input.Direction.x;
            s.PendDirZ = input.Direction.y;
            s.PendPower = input.Power;
            s.UndoAvailable = false;
        }

        public static ShotInput PendingInput(RunState s)
        {
            return new ShotInput(new Vector2(s.PendDirX, s.PendDirZ), s.PendPower);
        }

        public static ShotReport ResolveShot(RunState s, SimResult r, int par, string holeName, Vector2 cup)
        {
            var rep = new ShotReport();
            s.PendingShot = false;
            if (r.Outcome == ShotOutcome.Holed)
            {
                rep.Holed = true;
                rep.UnderPar = s.HoleStrokes <= par - 1;
                rep.Restored = RestorePerHole + (rep.UnderPar ? UnderParBonus : 0);
                s.Strokes += rep.Restored;
                s.History.Add(new HoleRecord { Name = holeName, Par = par, Strokes = s.HoleStrokes, Penalties = s.HolePenalties, Restored = rep.Restored });
                s.UndoAvailable = false;
                s.HasLie = false;
                rep.Headline = s.HoleStrokes == 1 ? "HOLE IN ONE" : ScoreName(s.HoleStrokes - par);
                rep.Detail = string.Format("In {0} · +{1} strokes restored{2}", Plural(s.HoleStrokes, "stroke"), rep.Restored, rep.UnderPar ? " (under-par bonus)" : "");
                if (IsLastHole(s))
                {
                    s.Phase = RunPhase.Won;
                    rep.RunWon = true;
                    s.EndReason = string.Format("All five holes cleared with {0} to spare.", Plural(s.Strokes, "stroke"));
                }
                else
                {
                    s.Phase = RunPhase.ChoosingUpgrade;
                    s.Offer = MakeOffer(s);
                }
                s.LastEvent = rep.Headline;
                return rep;
            }

            if (r.IsHazard)
            {
                rep.Hazard = true;
                rep.WaterHazard = r.Outcome == ShotOutcome.Water;
                s.Strokes -= HazardPenalty;
                s.HolePenalties += 1;
                s.HoleStrokes += 1;
                s.TotalStrokes += 1;
                rep.Headline = rep.WaterHazard ? "WATER" : "OUT OF BOUNDS";
                rep.Detail = "+1 penalty stroke · ball returned to its last lie";
            }
            else
            {
                s.LieX = r.FinalPosition.x;
                s.LieZ = r.FinalPosition.y;
                s.LieY = r.FinalHeight;
            }
            s.UndoAvailable = true;
            s.LastEvent = rep.Hazard ? rep.Headline : "";

            if (s.Strokes <= 0)
            {
                string why = rep.Hazard
                    ? (rep.WaterHazard ? "your last shot found the water" : "your last shot left the course")
                    : string.Format("your last shot stopped {0:F1} m from the cup", (r.FinalPosition - cup).magnitude);
                s.EndReason = string.Format("Out of strokes on {0}: {1}.", holeName, why);
                if (CanUndo(s))
                {
                    s.Phase = RunPhase.LastChance;
                    rep.LastChance = true;
                }
                else
                {
                    Lose(s, holeName, par);
                    rep.RunLost = true;
                }
            }
            return rep;
        }

        public static void Lose(RunState s, string holeName, int par)
        {
            s.Phase = RunPhase.Lost;
            s.UndoAvailable = false;
            s.History.Add(new HoleRecord { Name = holeName, Par = par, Strokes = s.HoleStrokes, Penalties = s.HolePenalties, Restored = 0 });
            if (s.Strokes < 0) s.Strokes = 0;
        }

        public static bool CanUndo(RunState s)
        {
            return s.Owns(UpgradeId.SecondChance) && !s.SecondChanceUsed && s.UndoAvailable;
        }

        /// <summary>Second Chance: put the ball back and refund the stroke and any penalty.</summary>
        public static void Undo(RunState s)
        {
            if (!CanUndo(s)) return;
            s.LieX = s.UndoX;
            s.LieZ = s.UndoZ;
            s.LieY = s.UndoY;
            s.Strokes = s.UndoStrokes;
            s.HoleStrokes = s.UndoHoleStrokes;
            s.HolePenalties = s.UndoPenalties;
            s.TotalStrokes = s.UndoTotal;
            s.SecondChanceUsed = true;
            s.UndoAvailable = false;
            s.Phase = RunPhase.Playing;
            s.EndReason = "";
            s.LastEvent = "SECOND CHANCE";
        }

        public static List<int> MakeOffer(RunState s)
        {
            var pool = new List<int>();
            foreach (var u in RunUpgrades.All) if (!s.Owns(u.Id)) pool.Add((int)u.Id);
            var rng = new DetRandom((ulong)s.Seed ^ (ulong)(s.HoleIndex * 2654435761L), 11);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.Range(0, i + 1);
                int t = pool[i];
                pool[i] = pool[j];
                pool[j] = t;
            }
            if (pool.Count > 3) pool.RemoveRange(3, pool.Count - 3);
            return pool;
        }

        public static void Choose(RunState s, int upgradeId)
        {
            if (s.Phase != RunPhase.ChoosingUpgrade || !s.Offer.Contains(upgradeId)) return;
            if (!s.Upgrades.Contains(upgradeId)) s.Upgrades.Add(upgradeId);
            s.Offer.Clear();
            AdvanceHole(s);
        }

        public static void AdvanceHole(RunState s)
        {
            s.HoleIndex++;
            s.Phase = RunPhase.HoleIntro;
            s.HasLie = false;
        }

        public static string ScoreName(int relative)
        {
            switch (relative)
            {
                case -3: return "ALBATROSS";
                case -2: return "EAGLE";
                case -1: return "BIRDIE";
                case 0: return "PAR";
                case 1: return "BOGEY";
                case 2: return "DOUBLE BOGEY";
            }
            return relative < 0 ? "UNDER PAR" : "HOLED";
        }

        public static string Plural(int n, string word) { return n + " " + word + (n == 1 ? "" : "s"); }
    }
}
