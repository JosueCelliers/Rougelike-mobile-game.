using BadLie.Core;
using BadLie.Course;
using UnityEngine;

namespace BadLie.Sim
{
    /// <summary>
    /// Deterministic ball simulation. A shot is fully simulated at release and played back;
    /// the aiming preview runs the same code with an early cut-off, so the preview is always a
    /// prefix of the real shot. There is no randomness anywhere in here.
    /// </summary>
    public sealed class BallSimulator
    {
        const float Skin = 0.0005f;

        CourseModel course;
        SimTuning tun;
        ShotModifiers mods;
        SimOptions opts;
        SimResult res;

        Vector2 p, v;
        float y, vy;
        bool grounded;
        float time, dist;
        bool skipUsed, bankArmed, lipped, magnetAnnounced, inFlow;
        float distAtFirstHit;
        bool done;
        SurfaceType surface;
        int stepCount;

        public static float LaunchSpeed(SimTuning t, ShotModifiers m, float power)
        {
            float k = Mathf.Sqrt(Mathf.Clamp01(power));
            return (t.MinLaunchSpeed + (t.MaxLaunchSpeed - t.MinLaunchSpeed) * k) * m.LaunchSpeedScale;
        }

        public SimResult Simulate(CourseModel model, BallStart start, ShotInput shot, ShotModifiers modifiers, SimOptions options, SimResult reuse = null)
        {
            course = model;
            tun = model.Tuning;
            mods = modifiers;
            opts = options;
            res = reuse ?? new SimResult();
            res.Clear();

            p = start.Position;
            y = start.Height;
            float speed = LaunchSpeed(tun, mods, shot.Power);
            v = shot.Direction.normalized * speed;
            vy = 0f;
            grounded = true;
            time = dist = 0f;
            skipUsed = lipped = magnetAnnounced = inFlow = false;
            bankArmed = mods.BankShot;
            distAtFirstHit = -1f;
            done = false;
            stepCount = 0;
            res.LaunchSpeed = speed;

            var g0 = course.Ground(p);
            surface = g0.Surface;
            if (!g0.IsVoid) y = g0.Height;
            Emit(SimEventType.Launch, speed);
            Record(true);

            float maxTime = opts.MaxTime > 0f ? opts.MaxTime : tun.MaxShotTime;
            float dt = tun.Dt;
            while (!done)
            {
                Vector2 before = p;
                if (grounded) StepGround(dt);
                else StepAir(dt);
                time += dt;
                dist += (p - before).magnitude;
                stepCount++;
                if (done) break;
                Record(false);

                if (opts.MaxDistance > 0f && dist >= opts.MaxDistance) Finish(ShotOutcome.PreviewEnd);
                else if (opts.AfterFirstHit > 0f && distAtFirstHit >= 0f && dist - distAtFirstHit >= opts.AfterFirstHit) Finish(ShotOutcome.PreviewEnd);
                else if (time >= maxTime) Finish(ShotOutcome.Timeout);
            }
            res.Time = time;
            res.Distance = dist;
            res.Skipped = skipUsed;
            Record(true);
            return res;
        }

        // ---------------------------------------------------------------- ground

        SurfaceProps Props(SurfaceType s)
        {
            SurfaceProps sp = tun.Surface(s);
            if (s == SurfaceType.Rough)
            {
                sp.RollDecel *= mods.RoughScale;
                sp.LinearDrag *= mods.RoughScale;
                if (mods.RoughLandingKeep > 0f) sp.LandingKeep = mods.RoughLandingKeep;
            }
            else if (s == SurfaceType.Sand)
            {
                sp.RollDecel *= mods.SandScale;
                sp.LinearDrag *= mods.SandScale;
            }
            return sp;
        }

        void StepGround(float dt)
        {
            GroundSample g = course.Ground(p);
            if (g.IsVoid)
            {
                grounded = false;
                vy = 0f;
                Emit(SimEventType.Airborne, v.magnitude);
                return;
            }
            if (g.Surface != surface)
            {
                surface = g.Surface;
                Emit(SimEventType.SurfaceChange, v.magnitude);
            }
            SurfaceProps sp = Props(g.Surface);

            // External accelerations: slope, flowing water / vents, cup magnet.
            Vector2 G = g.Gradient;
            Vector2 aSlope = -tun.SlopeFactor * tun.Gravity * G / (1f + G.sqrMagnitude);
            float zonePush;
            Vector2 aZone = course.ZoneAcceleration(p, v, mods.ForceCoupling, out zonePush);
            if (zonePush > 0f && !inFlow)
            {
                inFlow = true;
                Emit(SimEventType.EnterFlow, zonePush);
            }
            else if (zonePush <= 0f) inFlow = false;

            Vector2 aMagnet = Vector2.zero;
            if (mods.CupMagnet && g.Surface == SurfaceType.Green)
            {
                Vector2 toCup = course.Cup - p;
                float d = toCup.magnitude;
                float sp0 = v.magnitude;
                if (d < tun.MagnetPullRadius && d > 0.02f && sp0 < tun.MagnetMaxSpeed)
                {
                    aMagnet = toCup / d * (tun.MagnetPull * (1f - d / tun.MagnetPullRadius));
                    if (!magnetAnnounced)
                    {
                        magnetAnnounced = true;
                        Emit(SimEventType.MagnetPull, d);
                    }
                }
            }

            v += (aSlope + aZone + aMagnet) * dt;

            float speed = v.magnitude;
            float decel = sp.RollDecel + sp.LinearDrag * speed;
            float newSpeed = speed - decel * dt;
            if (newSpeed <= 0f) v = Vector2.zero;
            else v *= newSpeed / speed;

            if (v.magnitude < tun.StopSpeed)
            {
                float external = aSlope.magnitude + zonePush;
                if (external <= sp.RollDecel * tun.StaticFactor)
                {
                    v = Vector2.zero;
                    if (CheckCupAtRest()) return;
                    Finish(ShotOutcome.Stopped);
                    return;
                }
            }

            Vector2 prev = p;
            Move(v * dt);

            GroundSample g2 = course.Ground(p);
            if (g2.IsVoid || g2.Height < y - tun.StepDown)
            {
                grounded = false;
                // Leaving a slope keeps the vertical component of motion along it.
                vy = Mathf.Min(0f, Vector2.Dot(v, G));
                Emit(SimEventType.Airborne, v.magnitude);
                return;
            }
            if (g2.Height > y + tun.StepUp * 3f)
            {
                // Should be prevented by ledge segments; recover gracefully.
                p = prev;
                v = -v * 0.3f;
                return;
            }
            y = g2.Height;

            if (g2.Water)
            {
                WaterContact();
                return;
            }
            if (g2.OutOfBounds)
            {
                res.HazardPoint = p;
                Emit(SimEventType.Fell, v.magnitude);
                Finish(ShotOutcome.OutOfBounds);
                return;
            }
            CheckCupPass(prev, p);
        }

        // ---------------------------------------------------------------- air

        void StepAir(float dt)
        {
            vy -= tun.Gravity * dt;
            Vector2 prev = p;
            Move(v * dt);
            y += vy * dt;

            GroundSample g = course.Ground(p);
            if (!g.IsVoid && y <= g.Height)
            {
                float impact = -vy;
                y = g.Height;
                if (g.Water)
                {
                    WaterContact();
                    return;
                }
                if (g.OutOfBounds)
                {
                    res.HazardPoint = p;
                    Emit(SimEventType.Fell, impact);
                    Finish(ShotOutcome.OutOfBounds);
                    return;
                }
                surface = g.Surface;
                SurfaceProps sp = Props(g.Surface);
                float keep = Mathf.Lerp(1f, sp.LandingKeep, Mathf.Clamp01(impact / 4f));
                v *= keep;
                if (impact > tun.BounceThreshold && sp.LandingBounce > 0f)
                {
                    vy = impact * sp.LandingBounce;
                    Emit(SimEventType.Bounce, impact, g.Surface);
                }
                else
                {
                    vy = 0f;
                    grounded = true;
                    Emit(SimEventType.Land, impact, g.Surface);
                }
                CheckCupPass(prev, p);
                return;
            }
            if (g.IsVoid && y < tun.VoidLevel)
            {
                res.HazardPoint = p;
                Emit(SimEventType.Fell, -vy);
                Finish(ShotOutcome.OutOfBounds);
            }
        }

        // ---------------------------------------------------------------- collisions

        void Move(Vector2 delta)
        {
            float r = tun.BallRadius;
            Vector2 remaining = delta;
            for (int iter = 0; iter < 5; iter++)
            {
                float len = remaining.magnitude;
                if (len < 1e-7f) break;
                SweepHit hit;
                if (!course.Sweep(p, remaining, y, r, out hit))
                {
                    p += remaining;
                    break;
                }
                float tSafe = Mathf.Max(0f, hit.T - Skin / len);
                p += remaining * tSafe;
                float rest = len * (1f - tSafe);

                Vector2 n = hit.Normal;
                float vn = Vector2.Dot(v, n);
                float before = v.magnitude;
                if (vn < 0f)
                {
                    WallProps wp = tun.Wall(hit.Material);
                    bool boosted = false;
                    if (bankArmed)
                    {
                        wp = tun.BankShotWall;
                        bankArmed = false;
                        boosted = true;
                        res.BankUsed = true;
                    }
                    Vector2 vN = n * vn;
                    Vector2 vT = v - vN;
                    v = vT * wp.TangentKeep - vN * wp.Restitution;
                    res.WallHits++;
                    if (distAtFirstHit < 0f) distAtFirstHit = dist;
                    var e = MakeEvent(boosted ? SimEventType.BankWall : SimEventType.Wall, -vn);
                    e.Material = hit.Material;
                    e.Normal = n;
                    e.Position = new Vector3(p.x, y, p.y);
                    res.Events.Add(e);
                    // Airborne balls also lose some vertical energy against walls.
                    if (!grounded) vy *= 0.85f;
                }
                float after = v.magnitude;
                if (before < 1e-6f || after < 1e-6f) break;
                remaining = v / after * (rest * (after / before));
            }
            Vector2 pushN;
            course.Depenetrate(ref p, y, r, out pushN);
        }

        // ---------------------------------------------------------------- water

        void WaterContact()
        {
            float speed = v.magnitude;
            if (mods.SkipStone && !skipUsed && speed >= tun.SkipMinSpeed)
            {
                skipUsed = true;
                vy = Mathf.Min(tun.SkipVerticalFactor * speed, tun.SkipMaxVertical);
                v *= tun.SkipHorizontalKeep;
                grounded = false;
                y += 0.002f;
                if (mods.Ricochet && mods.BankShot) bankArmed = true;
                Emit(SimEventType.Skip, speed);
                return;
            }
            res.HazardPoint = p;
            Emit(SimEventType.Splash, speed);
            Finish(ShotOutcome.Water);
        }

        // ---------------------------------------------------------------- cup

        float CaptureRadius(float speed)
        {
            if (mods.CupMagnet && speed < tun.MagnetMaxSpeed) return tun.MagnetCaptureRadius;
            return tun.CaptureRadius;
        }

        bool CheckCupAtRest()
        {
            float d = (p - course.Cup).magnitude;
            if (d <= CaptureRadius(0f) && Mathf.Abs(y - course.CupHeight) < 0.2f)
            {
                Hole();
                return true;
            }
            return false;
        }

        void CheckCupPass(Vector2 a, Vector2 b)
        {
            if (Mathf.Abs(y - course.CupHeight) > 0.2f) return;
            float t;
            Vector2 c = Geo2D.ClosestOnSegment(course.Cup, a, b, out t);
            float d = (c - course.Cup).magnitude;
            float speed = v.magnitude;
            float R = CaptureRadius(speed);
            if (d > R)
            {
                if (d > R + 0.06f) lipped = false;
                return;
            }
            float u = d / R;
            float vmax = tun.CaptureSpeedCentre * Mathf.Sqrt(Mathf.Max(0f, 1f - u * u)) + tun.CaptureSpeedFloor;
            if (speed <= vmax)
            {
                p = c;
                Hole();
                return;
            }
            if (!lipped)
            {
                lipped = true;
                // Deterministic lip-out: deflect away from the side the ball passed on.
                Vector2 toCup = course.Cup - c;
                float side = Geo2D.Cross(v, toCup);
                float deflect = tun.LipOutMaxDeflectDeg * (1f - u) * (side > 0f ? -1f : 1f);
                v = Geo2D.Rotate(v, deflect) * tun.LipOutSpeedKeep;
                Emit(SimEventType.LipOut, speed);
            }
        }

        void Hole()
        {
            v = Vector2.zero;
            Emit(SimEventType.Holed, 0f);
            res.FinalPosition = course.Cup;
            Finish(ShotOutcome.Holed);
        }

        // ---------------------------------------------------------------- bookkeeping

        void Finish(ShotOutcome outcome)
        {
            if (done) return;
            done = true;
            res.Outcome = outcome;
            if (outcome != ShotOutcome.Holed) res.FinalPosition = p;
            res.FinalHeight = outcome == ShotOutcome.Holed ? course.CupHeight : y;
            res.FinalSurface = surface;
            if (outcome == ShotOutcome.Stopped) Emit(SimEventType.Stop, 0f);
        }

        SimEvent MakeEvent(SimEventType type, float strength)
        {
            return new SimEvent
            {
                Type = type,
                Time = time,
                Position = new Vector3(p.x, y, p.y),
                Strength = strength,
                Surface = surface,
            };
        }

        void Emit(SimEventType type, float strength)
        {
            res.Events.Add(MakeEvent(type, strength));
        }

        void Emit(SimEventType type, float strength, SurfaceType s)
        {
            var e = MakeEvent(type, strength);
            e.Surface = s;
            res.Events.Add(e);
        }

        void Record(bool force)
        {
            if (!opts.RecordPath) return;
            int every = Mathf.Max(1, opts.RecordEvery);
            if (!force && stepCount % every != 0) return;
            var path = res.Path;
            if (path.Count > 0 && path[path.Count - 1].Time >= time && !force) return;
            if (force && path.Count > 0 && Mathf.Approximately(path[path.Count - 1].Time, time)) path.RemoveAt(path.Count - 1);
            path.Add(new PathSample
            {
                Time = time,
                Position = new Vector3(p.x, y, p.y),
                Speed = Mathf.Sqrt(v.sqrMagnitude + (grounded ? 0f : vy * vy)),
                Grounded = grounded,
            });
        }
    }
}
