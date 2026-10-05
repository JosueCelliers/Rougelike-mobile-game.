using System.Collections.Generic;
using BadLie.Core;
using BadLie.Holes;
using BadLie.Run;
using BadLie.Course;
using BadLie.Sim;
using NUnit.Framework;
using UnityEngine;

namespace BadLie.Tests
{
    public class SimTests
    {
        static readonly SimTuning T = SimTuning.Default;

        static HoleDef FlatHole(SurfaceType surface, float size = 40f)
        {
            var h = new HoleDef { Id = "test", Name = "Test", Tee = Vector2.zero, Cup = new Vector2(0f, 200f) };
            h.Pad("base", new BoxShape(new Vector2(0f, size * 0.5f - 5f), new Vector2(size, size), 0f, 0.5f), 0, HeightDef.Flat(0f), surface);
            return h;
        }

        static SimResult Shoot(CourseModel m, Vector2 from, Vector2 dir, float power, ShotModifiers mods, SimOptions? opts = null)
        {
            var sim = new BallSimulator();
            return sim.Simulate(m, new BallStart(from, m.HeightAt(from)), new ShotInput(dir, power), mods, opts ?? SimOptions.Full);
        }

        static float AnalyticRoll(float v, SurfaceProps sp)
        {
            // distance = integral v dv / (a + k v)
            float a = sp.RollDecel, k = sp.LinearDrag;
            if (k < 1e-6f) return v * v / (2f * a);
            return (1f / k) * (v - (a / k) * Mathf.Log(1f + k * v / a));
        }

        [Test]
        public void FlatRollMatchesAnalyticDistance()
        {
            foreach (var s in new[] { SurfaceType.Green, SurfaceType.Fairway, SurfaceType.Rough, SurfaceType.Stone })
            {
                var m = new CourseModel(FlatHole(s, 80f));
                var r = Shoot(m, Vector2.zero, Vector2.up, 0.6f, ShotModifiers.None);
                Assert.AreEqual(ShotOutcome.Stopped, r.Outcome, s.ToString());
                float expected = AnalyticRoll(r.LaunchSpeed, T.Surface(s));
                Assert.AreEqual(expected, r.FinalPosition.y, expected * 0.03f + 0.05f, "roll distance on " + s);
            }
        }

        [Test]
        public void SimulationIsDeterministic()
        {
            var m = new CourseModel(HoleLibrary.Get(0));
            var a = Shoot(m, m.Tee, new Vector2(0.3f, 1f), 0.83f, ShotModifiers.None);
            var b = Shoot(m, m.Tee, new Vector2(0.3f, 1f), 0.83f, ShotModifiers.None);
            Assert.AreEqual(a.Outcome, b.Outcome);
            Assert.AreEqual(a.FinalPosition, b.FinalPosition);
            Assert.AreEqual(a.Path.Count, b.Path.Count);
            Assert.AreEqual(a.Events.Count, b.Events.Count);
        }

        [Test]
        public void PreviewIsPrefixOfRealShot()
        {
            var m = new CourseModel(HoleLibrary.Get(0));
            for (int i = 0; i < 12; i++)
            {
                Vector2 dir = Geo2D.FromAngle(60f + i * 6f);
                float power = 0.3f + 0.05f * i;
                var full = Shoot(m, m.Tee, dir, power, ShotModifiers.None);
                var prev = Shoot(m, m.Tee, dir, power, ShotModifiers.None, SimOptions.Preview(6f, 1.2f));
                int n = Mathf.Min(prev.Path.Count - 1, full.Path.Count);
                for (int k = 0; k < n; k++)
                {
                    Assert.AreEqual(full.Path[k].Time, prev.Path[k].Time, 1e-5f);
                    Assert.Less((full.Path[k].Position - prev.Path[k].Position).magnitude, 1e-4f, "sample " + k + " shot " + i);
                }
            }
        }

        [Test]
        public void FullSpeedNeverTunnelsThroughThinWall()
        {
            var h = FlatHole(SurfaceType.Stone, 60f);
            h.Wall(WallStyle.Sandstone, 0.6f, 0.05f, new Vector2(-10f, 6f), new Vector2(10f, 6f));
            var m = new CourseModel(h);
            for (int i = 0; i < 40; i++)
            {
                Vector2 dir = Geo2D.FromAngle(50f + i * 2f);
                var r = Shoot(m, Vector2.zero, dir, 1f, ShotModifiers.None);
                foreach (var s in r.Path)
                {
                    if (Mathf.Abs(s.Position.x) < 9.5f) Assert.Less(s.Position.z, 6f - 0.025f - T.BallRadius + 0.002f, "tunnelled at angle " + (50 + i * 2));
                }
                Assert.GreaterOrEqual(r.WallHits, 1);
            }
        }

        [Test]
        public void WallReboundReflectsAndLosesNormalSpeed()
        {
            var h = FlatHole(SurfaceType.Stone, 60f);
            h.Wall(WallStyle.Sandstone, 0.8f, 0.3f, new Vector2(-20f, 5f), new Vector2(20f, 5f));
            var m = new CourseModel(h);
            var r = Shoot(m, Vector2.zero, new Vector2(1f, 1f), 0.5f, ShotModifiers.None);
            var hit = r.Events.Find(e => e.Type == SimEventType.Wall);
            Assert.AreEqual(SimEventType.Wall, hit.Type);
            Assert.Greater(r.FinalPosition.x, 5f, "ball keeps travelling along +x after banking");
            Assert.Less(r.FinalPosition.y, 5f - 0.15f, "ball comes back down off the wall");
        }

        [Test]
        public void BankShotPreservesMoreSpeedOnFirstRebound()
        {
            var h = FlatHole(SurfaceType.Stone, 80f);
            h.Wall(WallStyle.Sandstone, 0.8f, 0.3f, new Vector2(-30f, 5f), new Vector2(30f, 5f));
            var m = new CourseModel(h);
            var plain = Shoot(m, Vector2.zero, new Vector2(0.2f, 1f), 0.5f, ShotModifiers.None);
            var mods = ShotModifiers.None;
            mods.BankShot = true;
            var bank = Shoot(m, Vector2.zero, new Vector2(0.2f, 1f), 0.5f, mods);
            Assert.IsTrue(bank.BankUsed);
            Assert.IsTrue(bank.Events.Exists(e => e.Type == SimEventType.BankWall));
            Assert.Less(bank.FinalPosition.y, plain.FinalPosition.y - 1f, "banked ball travels further back");
        }

        [Test]
        public void SlowBallThroughCupCentreIsHoled()
        {
            var h = FlatHole(SurfaceType.Green);
            h.Cup = new Vector2(0f, 3f);
            var m = new CourseModel(h);
            var r = Shoot(m, Vector2.zero, Vector2.up, 0.12f, ShotModifiers.None);
            Assert.AreEqual(ShotOutcome.Holed, r.Outcome);
        }

        [Test]
        public void FastBallLipsOutInsteadOfDropping()
        {
            var h = FlatHole(SurfaceType.Green);
            h.Cup = new Vector2(0.18f, 2f);
            var m = new CourseModel(h);
            var r = Shoot(m, Vector2.zero, Vector2.up, 1f, ShotModifiers.None);
            Assert.AreNotEqual(ShotOutcome.Holed, r.Outcome);
            Assert.IsTrue(r.Events.Exists(e => e.Type == SimEventType.LipOut));
        }

        [Test]
        public void CupMagnetCapturesWiderAtLowSpeed()
        {
            var h = FlatHole(SurfaceType.Green);
            h.Cup = new Vector2(0.38f, 3f);
            var m = new CourseModel(h);
            float power = FindPowerToStopNear(m, 3.05f);
            var plain = Shoot(m, Vector2.zero, Vector2.up, power, ShotModifiers.None);
            var mods = ShotModifiers.None;
            mods.CupMagnet = true;
            var magnet = Shoot(m, Vector2.zero, Vector2.up, power, mods);
            Assert.AreNotEqual(ShotOutcome.Holed, plain.Outcome, "0.38 m wide is a miss without the magnet");
            Assert.AreEqual(ShotOutcome.Holed, magnet.Outcome, "magnet captures it");
        }

        static float FindPowerToStopNear(CourseModel m, float targetY)
        {
            float lo = 0f, hi = 1f;
            for (int i = 0; i < 30; i++)
            {
                float mid = (lo + hi) * 0.5f;
                var r = Shoot(m, Vector2.zero, Vector2.up, mid, ShotModifiers.None, SimOptions.Fast);
                if (r.FinalPosition.y < targetY) lo = mid; else hi = mid;
            }
            return (lo + hi) * 0.5f;
        }

        static HoleDef ChannelHole(float gap)
        {
            var h = new HoleDef { Id = "chan", Name = "Channel", Cup = new Vector2(0, 100f) };
            h.Pad("near", new BoxShape(new Vector2(0f, 0f), new Vector2(20f, 20f), 0f, 0.2f), 2, HeightDef.Flat(0f), SurfaceType.Fairway);
            h.Pad("far", new BoxShape(new Vector2(0f, 10f + gap + 10f), new Vector2(20f, 20f), 0f, 0.2f), 2, HeightDef.Flat(0f), SurfaceType.Fairway);
            h.WaterPad("water", new BoxShape(new Vector2(0f, 10f + gap * 0.5f), new Vector2(20f, gap + 1f), 0f, 0f), 1, -0.1f);
            return h;
        }

        [Test]
        public void WaterIsAHazardWithoutSkipStone()
        {
            var m = new CourseModel(ChannelHole(2.4f));
            var r = Shoot(m, new Vector2(0f, 4f), Vector2.up, 0.9f, ShotModifiers.None);
            Assert.AreEqual(ShotOutcome.Water, r.Outcome);
            Assert.Greater(r.HazardPoint.y, 9.9f);
        }

        [Test]
        public void SkipStoneCrossesShortGapOnce()
        {
            var m = new CourseModel(ChannelHole(2.4f));
            var mods = ShotModifiers.None;
            mods.SkipStone = true;
            var r = Shoot(m, new Vector2(0f, 4f), Vector2.up, 0.9f, mods);
            Assert.IsTrue(r.Skipped);
            Assert.AreEqual(ShotOutcome.Stopped, r.Outcome, "should land on the far bank");
            Assert.Greater(r.FinalPosition.y, 12.4f);

            // A wide gap needs two skips: the second contact sinks.
            var wide = new CourseModel(ChannelHole(9f));
            var r2 = Shoot(wide, new Vector2(0f, 4f), Vector2.up, 1f, mods);
            Assert.AreEqual(ShotOutcome.Water, r2.Outcome);
            Assert.AreEqual(1, r2.Events.FindAll(e => e.Type == SimEventType.Skip).Count);

            // Too slow to skip.
            var r3 = Shoot(m, new Vector2(0f, 8.5f), Vector2.up, 0.08f, mods);
            Assert.AreEqual(ShotOutcome.Water, r3.Outcome);
        }

        [Test]
        public void BallDropsOffLedgeAndKeepsRolling()
        {
            var h = new HoleDef { Id = "ledge", Name = "Ledge", Cup = new Vector2(0, 100f) };
            h.Pad("low", new BoxShape(new Vector2(0f, 10f), new Vector2(20f, 30f), 0f, 0.2f), 1, HeightDef.Flat(0f), SurfaceType.Fairway);
            h.Pad("high", new BoxShape(new Vector2(0f, 0f), new Vector2(10f, 8f), 0f, 0.2f), 2, HeightDef.Flat(0.8f), SurfaceType.Fairway);
            var m = new CourseModel(h);
            var r = Shoot(m, new Vector2(0f, 0f), Vector2.up, 0.7f, ShotModifiers.None);
            Assert.AreEqual(ShotOutcome.Stopped, r.Outcome);
            Assert.IsTrue(r.Events.Exists(e => e.Type == SimEventType.Airborne));
            Assert.IsTrue(r.Events.Exists(e => e.Type == SimEventType.Land || e.Type == SimEventType.Bounce));
            Assert.Greater(r.FinalPosition.y, 5f);
            Assert.AreEqual(0f, r.FinalHeight, 0.01f);

            // From below, the same edge is a wall.
            var up = Shoot(m, new Vector2(0f, 8f), Vector2.down, 0.6f, ShotModifiers.None);
            Assert.GreaterOrEqual(up.WallHits, 1);
            Assert.Greater(up.FinalPosition.y, 4f + T.BallRadius - 0.01f);
        }

        [Test]
        public void RampNeedsSpeedToClimb()
        {
            var h = new HoleDef { Id = "ramp", Name = "Ramp", Cup = new Vector2(0, 100f) };
            h.Pad("low", new BoxShape(new Vector2(0f, 0f), new Vector2(12f, 14f), 0f, 0.2f), 1, HeightDef.Flat(0f), SurfaceType.Stone);
            h.Pad("ramp", new BoxShape(new Vector2(0f, 9f), new Vector2(3f, 4f), 0f, 0f), 2, HeightDef.Ramp(new Vector2(0f, 7f), 0f, new Vector2(0f, 11f), 0.6f), SurfaceType.Stone);
            h.Pad("top", new BoxShape(new Vector2(0f, 15f), new Vector2(12f, 8f), 0f, 0.2f), 3, HeightDef.Flat(0.6f), SurfaceType.Stone);
            var m = new CourseModel(h);
            var soft = Shoot(m, new Vector2(0f, 3f), Vector2.up, 0.12f, ShotModifiers.None);
            Assert.Less(soft.FinalHeight, 0.05f, "soft shot rolls back down");
            var firm = Shoot(m, new Vector2(0f, 3f), Vector2.up, 0.5f, ShotModifiers.None);
            Assert.AreEqual(0.6f, firm.FinalHeight, 0.02f, "firm shot reaches the top");
        }

        [Test]
        public void RoughRiderReducesRoughSlowdown()
        {
            var m = new CourseModel(FlatHole(SurfaceType.Rough, 80f));
            var plain = Shoot(m, Vector2.zero, Vector2.up, 0.8f, ShotModifiers.None);
            var mods = RunUpgrades.ModifiersFor(new[] { UpgradeId.RoughRider });
            var rider = Shoot(m, Vector2.zero, Vector2.up, 0.8f, mods);
            Assert.Greater(rider.FinalPosition.y, plain.FinalPosition.y * 1.6f);
        }

        [Test]
        public void HeavyCoreResistsFlowButHitsShorter()
        {
            var h = FlatHole(SurfaceType.Stone, 80f);
            h.Paint(SurfaceType.Runnel, new BoxShape(new Vector2(0f, 6f), new Vector2(40f, 3f)));
            h.Flow(new BoxShape(new Vector2(0f, 6f), new Vector2(40f, 3f)), 1.6f, "runnel", new Vector2(-20f, 6f), new Vector2(20f, 6f));
            var m = new CourseModel(h);
            var plain = Shoot(m, Vector2.zero, Vector2.up, 0.45f, ShotModifiers.None);
            var heavy = Shoot(m, Vector2.zero, Vector2.up, 0.45f, RunUpgrades.ModifiersFor(new[] { UpgradeId.HeavyCore }));
            Assert.Greater(Mathf.Abs(plain.FinalPosition.x), Mathf.Abs(heavy.FinalPosition.x) * 2f, "flow pushes the plain ball sideways much more");
            float flat = BallSimulator.LaunchSpeed(T, ShotModifiers.None, 1f);
            float hv = BallSimulator.LaunchSpeed(T, RunUpgrades.ModifiersFor(new[] { UpgradeId.HeavyCore }), 1f);
            Assert.Less(hv, flat);
        }

        [Test]
        public void RollingIntoVoidIsOutOfBounds()
        {
            var m = new CourseModel(FlatHole(SurfaceType.Fairway, 20f));
            var r = Shoot(m, Vector2.zero, Vector2.left, 1f, ShotModifiers.None);
            Assert.AreEqual(ShotOutcome.OutOfBounds, r.Outcome);
        }

        [Test]
        public void SandKillsTheBall()
        {
            var fair = new CourseModel(FlatHole(SurfaceType.Fairway, 80f));
            var sand = new CourseModel(FlatHole(SurfaceType.Sand, 80f));
            var a = Shoot(fair, Vector2.zero, Vector2.up, 1f, ShotModifiers.None);
            var b = Shoot(sand, Vector2.zero, Vector2.up, 1f, ShotModifiers.None);
            Assert.Less(b.FinalPosition.y, a.FinalPosition.y * 0.25f);
        }

        [Test]
        public void EveryShotEndsUnambiguously()
        {
            var m = new CourseModel(HoleLibrary.Get(0));
            for (int i = 0; i < 36; i++)
            {
                var r = Shoot(m, m.Tee, Geo2D.FromAngle(i * 10f), 1f, ShotModifiers.None);
                Assert.AreNotEqual(ShotOutcome.Timeout, r.Outcome, "shot at " + (i * 10) + " deg timed out");
            }
        }
    }
}
