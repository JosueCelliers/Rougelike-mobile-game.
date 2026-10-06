using System.Collections.Generic;
using BadLie.Core;
using BadLie.Course;
using BadLie.Sim;
using UnityEngine;

namespace BadLie.Bot
{
    /// <summary>
    /// Walking distance to the cup over playable ground (Dijkstra on a grid). Drops are
    /// one-way, walls block, water and the void are excluded. Used to score bot shots and to
    /// prove that every hole and variation can be completed.
    /// </summary>
    public sealed class DistanceField
    {
        public const float Cell = 0.25f;
        readonly CourseModel course;
        readonly Vector2 origin;
        readonly int nx, ny;
        readonly float[] dist;
        readonly float[] height;
        readonly bool[] walkable;

        public DistanceField(CourseModel course)
        {
            this.course = course;
            Rect b = course.Bounds;
            origin = new Vector2(b.xMin - Cell, b.yMin - Cell);
            nx = Mathf.CeilToInt((b.width + 2 * Cell) / Cell) + 1;
            ny = Mathf.CeilToInt((b.height + 2 * Cell) / Cell) + 1;
            dist = new float[nx * ny];
            height = new float[nx * ny];
            walkable = new bool[nx * ny];
            float r = course.Tuning.BallRadius;
            for (int j = 0; j < ny; j++)
            {
                for (int i = 0; i < nx; i++)
                {
                    int k = j * nx + i;
                    Vector2 p = Point(i, j);
                    var g = course.Ground(p);
                    height[k] = g.Height;
                    walkable[k] = !g.IsVoid && !g.Water && !g.OutOfBounds && course.Clearance(p, g.Height, r, 0.2f) > 0.01f;
                    dist[k] = float.MaxValue;
                }
            }
            Solve();
        }

        Vector2 Point(int i, int j) { return origin + new Vector2(i * Cell, j * Cell); }

        bool Cell2(Vector2 p, out int i, out int j)
        {
            i = Mathf.RoundToInt((p.x - origin.x) / Cell);
            j = Mathf.RoundToInt((p.y - origin.y) / Cell);
            return i >= 0 && j >= 0 && i < nx && j < ny;
        }

        /// <summary>Can the ball roll from cell a to neighbouring cell b?</summary>
        bool Step(int a, int b)
        {
            if (!walkable[a] || !walkable[b]) return false;
            float ha = height[a], hb = height[b];
            if (hb > ha + 0.12f) return false; // would need a ramp: ramps change gradually
            Vector2 pa = Point(a % nx, a / nx), pb = Point(b % nx, b / nx);
            SweepHit hit;
            return !course.Sweep(pa, pb - pa, Mathf.Min(ha, hb), 0.02f, out hit);
        }

        void Solve()
        {
            int ci, cj;
            if (!Cell2(course.Cup, out ci, out cj)) return;
            var open = new SortedSet<KeyValuePair<float, int>>(Comparer<KeyValuePair<float, int>>.Create((x, y) =>
            {
                int c = x.Key.CompareTo(y.Key);
                return c != 0 ? c : x.Value.CompareTo(y.Value);
            }));
            int start = cj * nx + ci;
            dist[start] = 0f;
            open.Add(new KeyValuePair<float, int>(0f, start));
            int[] di = { 1, -1, 0, 0, 1, 1, -1, -1 };
            int[] dj = { 0, 0, 1, -1, 1, -1, 1, -1 };
            while (open.Count > 0)
            {
                var cur = open.Min;
                open.Remove(cur);
                int k = cur.Value;
                if (cur.Key > dist[k]) continue;
                int i = k % nx, j = k / nx;
                for (int n = 0; n < 8; n++)
                {
                    int i2 = i + di[n], j2 = j + dj[n];
                    if (i2 < 0 || j2 < 0 || i2 >= nx || j2 >= ny) continue;
                    int k2 = j2 * nx + i2;
                    // Reverse search: the ball must be able to go from k2 to k.
                    if (!Step(k2, k)) continue;
                    float nd = dist[k] + (n < 4 ? Cell : Cell * 1.41421f);
                    if (nd < dist[k2])
                    {
                        if (dist[k2] < float.MaxValue) open.Remove(new KeyValuePair<float, int>(dist[k2], k2));
                        dist[k2] = nd;
                        open.Add(new KeyValuePair<float, int>(nd, k2));
                    }
                }
            }
        }

        /// <summary>Walking distance to the cup, or +inf if unreachable.</summary>
        public float Distance(Vector2 p)
        {
            int i, j;
            if (!Cell2(p, out i, out j)) return float.MaxValue;
            float best = dist[j * nx + i];
            // Tolerate the exact cell being blocked (ball resting against a wall).
            if (best == float.MaxValue)
            {
                for (int dj2 = -1; dj2 <= 1; dj2++)
                {
                    for (int di2 = -1; di2 <= 1; di2++)
                    {
                        int i2 = i + di2, j2 = j + dj2;
                        if (i2 < 0 || j2 < 0 || i2 >= nx || j2 >= ny) continue;
                        best = Mathf.Min(best, dist[j2 * nx + i2] + Cell);
                    }
                }
            }
            return best;
        }
    }

    /// <summary>A shot-choosing bot for playtests: coarse search then local refinement.</summary>
    public sealed class ShotPlanner
    {
        readonly CourseModel course;
        readonly DistanceField field;
        readonly BallSimulator sim = new BallSimulator();
        readonly SimResult scratch = new SimResult();
        public ShotModifiers Mods = ShotModifiers.None;

        public ShotPlanner(CourseModel course) : this(course, new DistanceField(course)) { }

        /// <summary>Shares a distance field (read-only once built) between planners.</summary>
        public ShotPlanner(CourseModel course, DistanceField field)
        {
            this.course = course;
            this.field = field;
        }

        public DistanceField Field { get { return field; } }
        public CourseModel Course { get { return course; } }

        public float Score(SimResult r)
        {
            switch (r.Outcome)
            {
                case ShotOutcome.Holed: return -1000f;
                case ShotOutcome.Water:
                case ShotOutcome.OutOfBounds: return 1000f;
                case ShotOutcome.Timeout: return 500f;
            }
            float d = field.Distance(r.FinalPosition);
            if (d == float.MaxValue) return 800f;
            // Prefer good lies: rough and sand cost a little extra.
            if (r.FinalSurface == SurfaceType.Sand) d += 2.5f;
            else if (r.FinalSurface == SurfaceType.Rough) d += 1f;
            return d;
        }

        public float Evaluate(Vector2 lie, float h, float angle, float power)
        {
            sim.Simulate(course, new BallStart(lie, h), new ShotInput(Geo2D.FromAngle(angle), power), Mods, SimOptions.Fast, scratch);
            return Score(scratch);
        }

        public ShotInput Plan(Vector2 lie, float h, out float bestScore)
        {
            var candidates = new List<KeyValuePair<float, Vector2>>(); // score, (angle, power)
            for (int a = 0; a < 72; a++)
            {
                float ang = a * 5f;
                for (int p = 1; p <= 12; p++)
                {
                    float pw = p / 12f;
                    float s = Evaluate(lie, h, ang, pw * pw * 0.97f + 0.03f);
                    candidates.Add(new KeyValuePair<float, Vector2>(s, new Vector2(ang, pw * pw * 0.97f + 0.03f)));
                }
            }
            candidates.Sort((x, y) => x.Key.CompareTo(y.Key));
            float best = candidates[0].Key;
            Vector2 bestParams = candidates[0].Value;
            int refine = Mathf.Min(4, candidates.Count);
            for (int c = 0; c < refine; c++)
            {
                Vector2 basis = candidates[c].Value;
                for (int da = -4; da <= 4; da++)
                {
                    for (int dp = -3; dp <= 3; dp++)
                    {
                        float ang = basis.x + da * 1.0f;
                        float pw = Mathf.Clamp01(basis.y + dp * 0.018f);
                        float s = Evaluate(lie, h, ang, pw);
                        if (s < best)
                        {
                            best = s;
                            bestParams = new Vector2(ang, pw);
                        }
                    }
                }
            }
            bestScore = best;
            return new ShotInput(Geo2D.FromAngle(bestParams.x), bestParams.y);
        }
    }

    /// <summary>Plays whole holes with the planner, optionally with human-like execution error.</summary>
    public static class BotPlayer
    {
        public struct HoleRun
        {
            public int Strokes;
            public int Penalties;
            public bool Holed;
            public int Skips;
            public int Banks;
        }

        public static HoleRun Play(CourseModel course, ShotModifiers mods, int maxStrokes, float angleError, float powerError, ulong seed)
        {
            return Play(new ShotPlanner(course), course, mods, maxStrokes, angleError, powerError, seed);
        }

        public static HoleRun Play(ShotPlanner planner, CourseModel course, ShotModifiers mods, int maxStrokes, float angleError, float powerError, ulong seed)
        {
            planner.Mods = mods;
            var sim = new BallSimulator();
            var rng = new DetRandom(seed);
            Vector2 lie = course.Tee;
            float h = course.TeeHeight;
            var run = new HoleRun();
            while (run.Strokes + run.Penalties < maxStrokes)
            {
                float score;
                ShotInput plan = planner.Plan(lie, h, out score);
                Vector2 dir = Geo2D.Rotate(plan.Direction, rng.Signed() * angleError);
                float power = Mathf.Clamp01(plan.Power * (1f + rng.Signed() * powerError));
                var r = sim.Simulate(course, new BallStart(lie, h), new ShotInput(dir, power), mods, SimOptions.Fast);
                run.Strokes++;
                if (r.Skipped) run.Skips++;
                if (r.BankUsed) run.Banks++;
                if (r.Outcome == ShotOutcome.Holed)
                {
                    run.Holed = true;
                    break;
                }
                if (r.IsHazard)
                {
                    run.Penalties++;
                    continue; // replay from the same lie
                }
                lie = r.FinalPosition;
                h = r.FinalHeight;
            }
            return run;
        }
    }
}
