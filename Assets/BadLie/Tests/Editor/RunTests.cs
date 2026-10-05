using System.Collections.Generic;
using BadLie.Course;
using BadLie.Holes;
using BadLie.Run;
using BadLie.Sim;
using NUnit.Framework;
using UnityEngine;

namespace BadLie.Tests
{
    public class RunTests
    {
        static RunState Fresh(long seed = 42)
        {
            var alts = new List<int>();
            for (int i = 0; i < RunRules.HolesPerRun; i++) alts.Add(i < HoleLibrary.Available ? HoleLibrary.Get(i).AltCups.Count : 0);
            var s = RunRules.NewRun(seed, alts);
            RunRules.BeginHole(s, Vector2.zero, 0f);
            return s;
        }

        static SimResult Result(ShotOutcome o, Vector2 final)
        {
            var r = new SimResult { Outcome = o, FinalPosition = final, FinalHeight = 0f };
            return r;
        }

        [Test]
        public void NewRunHasBudgetAndValidPins()
        {
            var s = Fresh();
            Assert.AreEqual(RunRules.StartStrokes, s.Strokes);
            Assert.AreEqual(RunRules.HolesPerRun, s.HoleOrder.Length);
            for (int i = 0; i < s.CupVariants.Length && i < HoleLibrary.Available; i++)
            {
                Assert.GreaterOrEqual(s.CupVariants[i], -1);
                Assert.Less(s.CupVariants[i], Mathf.Max(0, HoleLibrary.Get(i).AltCups.Count));
            }
        }

        [Test]
        public void HoleOutRestoresAndAdvances()
        {
            var s = Fresh();
            RunRules.ReleaseShot(s, new ShotInput(Vector2.up, 0.5f));
            RunRules.ReleaseShot(s, new ShotInput(Vector2.up, 0.5f)); // second shot, as if the first stopped
            int before = s.Strokes;
            var rep = RunRules.ResolveShot(s, Result(ShotOutcome.Holed, Vector2.zero), 4, "Test", Vector2.zero);
            Assert.IsTrue(rep.Holed);
            Assert.IsTrue(rep.UnderPar);
            Assert.AreEqual(before + RunRules.RestorePerHole + RunRules.UnderParBonus, s.Strokes);
            Assert.AreEqual(RunPhase.ChoosingUpgrade, s.Phase);
            Assert.AreEqual(3, s.Offer.Count);
            Assert.AreEqual(1, s.History.Count);
        }

        [Test]
        public void HazardCostsAPenaltyAndKeepsTheLie()
        {
            var s = Fresh();
            s.LieX = 3f;
            s.LieZ = 4f;
            RunRules.ReleaseShot(s, new ShotInput(Vector2.up, 0.9f));
            var rep = RunRules.ResolveShot(s, Result(ShotOutcome.Water, new Vector2(3f, 12f)), 4, "Test", new Vector2(0, 30));
            Assert.IsTrue(rep.Hazard);
            Assert.AreEqual(RunRules.StartStrokes - 2, s.Strokes);
            Assert.AreEqual(3f, s.LieX);
            Assert.AreEqual(4f, s.LieZ);
            Assert.AreEqual(2, s.HoleStrokes);
            Assert.AreEqual(RunPhase.Playing, s.Phase);
        }

        [Test]
        public void FinalStrokeResolvesBeforeTheRunEnds()
        {
            var s = Fresh();
            s.Strokes = 1;
            RunRules.ReleaseShot(s, new ShotInput(Vector2.up, 0.4f));
            Assert.AreEqual(0, s.Strokes);
            Assert.AreEqual(RunPhase.Playing, s.Phase, "the shot is still rolling: no verdict yet");
            var rep = RunRules.ResolveShot(s, Result(ShotOutcome.Holed, Vector2.zero), 3, "Test", Vector2.zero);
            Assert.IsFalse(rep.RunLost);
            Assert.Greater(s.Strokes, 0);

            var t = Fresh();
            t.Strokes = 1;
            RunRules.ReleaseShot(t, new ShotInput(Vector2.up, 0.4f));
            var lost = RunRules.ResolveShot(t, Result(ShotOutcome.Stopped, new Vector2(0f, 2.4f)), 3, "The Lantern Gate", Vector2.zero);
            Assert.IsTrue(lost.RunLost);
            Assert.AreEqual(RunPhase.Lost, t.Phase);
            StringAssert.Contains("Out of strokes on The Lantern Gate", t.EndReason);
            StringAssert.Contains("2.4 m from the cup", t.EndReason);

            var u = Fresh();
            u.Strokes = 1;
            RunRules.ReleaseShot(u, new ShotInput(Vector2.up, 0.4f));
            RunRules.ResolveShot(u, Result(ShotOutcome.Water, Vector2.zero), 3, "The Lantern Gate", Vector2.zero);
            Assert.AreEqual(RunPhase.Lost, u.Phase);
            StringAssert.Contains("water", u.EndReason);
            Assert.AreEqual(0, u.Strokes, "displayed budget never goes negative");
        }

        [Test]
        public void SecondChanceUndoesOnceAndCatchesTheFinalShot()
        {
            var s = Fresh();
            s.Upgrades.Add((int)UpgradeId.SecondChance);
            s.Strokes = 1;
            s.LieX = 1f;
            RunRules.ReleaseShot(s, new ShotInput(Vector2.up, 0.4f));
            var rep = RunRules.ResolveShot(s, Result(ShotOutcome.Water, Vector2.zero), 3, "Test", Vector2.zero);
            Assert.IsTrue(rep.LastChance);
            Assert.AreEqual(RunPhase.LastChance, s.Phase);
            Assert.IsTrue(RunRules.CanUndo(s));
            RunRules.Undo(s);
            Assert.AreEqual(RunPhase.Playing, s.Phase);
            Assert.AreEqual(1, s.Strokes);
            Assert.AreEqual(1f, s.LieX);
            Assert.AreEqual(0, s.HoleStrokes);
            Assert.IsTrue(s.SecondChanceUsed);
            RunRules.ReleaseShot(s, new ShotInput(Vector2.up, 0.4f));
            RunRules.ResolveShot(s, Result(ShotOutcome.Stopped, new Vector2(0, 5)), 3, "Test", Vector2.zero);
            Assert.IsFalse(RunRules.CanUndo(s), "only once per run");
            Assert.AreEqual(RunPhase.Lost, s.Phase);
        }

        [Test]
        public void OffersAreDistinctUnownedAndDeterministic()
        {
            var a = Fresh(7);
            a.Upgrades.Add((int)UpgradeId.BankShot);
            var o1 = RunRules.MakeOffer(a);
            var o2 = RunRules.MakeOffer(a);
            CollectionAssert.AreEqual(o1, o2);
            Assert.AreEqual(3, o1.Count);
            CollectionAssert.AllItemsAreUnique(o1);
            CollectionAssert.DoesNotContain(o1, (int)UpgradeId.BankShot);
            // After four picks every upgrade has been offered from a shrinking pool.
            var s = Fresh(9);
            for (int i = 0; i < 4; i++)
            {
                s.Phase = RunPhase.ChoosingUpgrade;
                s.Offer = RunRules.MakeOffer(s);
                Assert.AreEqual(3, s.Offer.Count, "pick " + i);
                RunRules.Choose(s, s.Offer[0]);
            }
            Assert.AreEqual(4, s.Upgrades.Count);
        }

        [Test]
        public void RunStateRoundTripsThroughJson()
        {
            var s = Fresh(1234);
            s.Upgrades.Add(2);
            RunRules.ReleaseShot(s, new ShotInput(new Vector2(0.6f, 0.8f), 0.73f));
            s.History.Add(new HoleRecord { Name = "X", Par = 3, Strokes = 4, Restored = 3 });
            string json = JsonUtility.ToJson(s);
            var r = JsonUtility.FromJson<RunState>(json);
            Assert.AreEqual(s.Seed, r.Seed);
            Assert.AreEqual(s.Strokes, r.Strokes);
            Assert.IsTrue(r.PendingShot);
            Assert.AreEqual(s.PendPower, r.PendPower);
            CollectionAssert.AreEqual(s.Upgrades, r.Upgrades);
            Assert.AreEqual(1, r.History.Count);
        }

        [Test]
        public void InterruptedShotReplaysIdentically()
        {
            var course = new CourseModel(HoleLibrary.Get(0));
            var s = Fresh(5);
            RunRules.BeginHole(s, course.Tee, course.TeeHeight);
            var input = new ShotInput(new Vector2(0.15f, 1f), 0.81f);
            var sim = new BallSimulator();
            var live = sim.Simulate(course, new BallStart(course.Tee, course.TeeHeight), input, ShotModifiers.None, SimOptions.Full);
            RunRules.ReleaseShot(s, input);
            var restored = JsonUtility.FromJson<RunState>(JsonUtility.ToJson(s));
            var replay = new BallSimulator().Simulate(course, new BallStart(new Vector2(restored.LieX, restored.LieZ), restored.LieY), RunRules.PendingInput(restored), ShotModifiers.None, SimOptions.Full);
            Assert.AreEqual(live.Outcome, replay.Outcome);
            Assert.AreEqual(live.FinalPosition.x, replay.FinalPosition.x, 1e-5f);
            Assert.AreEqual(live.FinalPosition.y, replay.FinalPosition.y, 1e-5f);
        }

        [Test]
        public void CombosActivateTogether()
        {
            var ricochet = RunUpgrades.ModifiersFor(new[] { UpgradeId.SkipStone, UpgradeId.BankShot });
            Assert.IsTrue(ricochet.Ricochet);
            var ground = RunUpgrades.ModifiersFor(new[] { UpgradeId.HeavyCore, UpgradeId.RoughRider });
            Assert.Less(ground.SandScale, 1f);
            Assert.Greater(ground.LaunchSpeedScale, RunUpgrades.HeavyCoreSpeed);
            Assert.AreEqual(2, RunUpgrades.ActiveCombos(new[] { UpgradeId.SkipStone, UpgradeId.BankShot, UpgradeId.HeavyCore, UpgradeId.RoughRider }).Count);
        }
    }
}
