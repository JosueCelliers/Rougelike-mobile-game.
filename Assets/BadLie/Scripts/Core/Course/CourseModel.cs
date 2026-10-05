using System;
using System.Collections.Generic;
using BadLie.Core;
using BadLie.Geometry;
using BadLie.Sim;
using UnityEngine;

namespace BadLie.Course
{
    public struct GroundSample
    {
        public int Pad;              // -1 = nothing underneath (the flooded lower estate)
        public float Height;
        public Vector2 Gradient;
        public SurfaceType Surface;
        public bool Water;
        public bool OutOfBounds;

        public bool IsVoid { get { return Pad < 0; } }
    }

    public struct CollisionSegment
    {
        public Vector2 A, B;
        public float Radius;
        public float YBottom, YTop;
        public WallMaterial Material;
        public int Owner;
        /// <summary>Ledges only block balls whose centre is on the lower (outer) side.</summary>
        public bool OneSided;
        public Vector2 OuterNormal;
    }

    public struct CollisionCircle
    {
        public Vector2 C;
        public float Radius;
        public float YBottom, YTop;
        public WallMaterial Material;
        public int Owner;
    }

    public struct SweepHit
    {
        public float T;
        public Vector2 Normal;
        public Vector2 Point;
        public WallMaterial Material;
        public int Owner;
        public bool IsSegment;
        public int Index;
    }

    /// <summary>A closed outline of the visible part of a pad, with heights on both sides.</summary>
    public sealed class ContourLoop
    {
        public readonly List<Vector2> Points = new List<Vector2>();
        public readonly List<Vector2> Normals = new List<Vector2>();
        public readonly List<float> Top = new List<float>();
        public readonly List<float> Neighbour = new List<float>();
        public readonly List<int> NeighbourPad = new List<int>();
    }

    public sealed class Pad
    {
        public int Index;
        public PadDef Def;
        public readonly List<PaintDef> Paints = new List<PaintDef>();
        public readonly List<Pad> Above = new List<Pad>();
        public FieldGrid Field;
        public readonly List<ContourLoop> Contours = new List<ContourLoop>();

        public Shape2D Shape { get { return Def.Shape; } }
        public bool Water { get { return Def.Water; } }

        public float HeightAt(Vector2 p, out Vector2 grad)
        {
            float h = Def.Height.Eval(p, out grad);
            var feats = Def.Features;
            for (int i = 0; i < feats.Count; i++)
            {
                Vector2 g;
                h += feats[i].Eval(p, out g);
                grad += g;
            }
            return h;
        }

        public float HeightAt(Vector2 p)
        {
            Vector2 g;
            return HeightAt(p, out g);
        }

        public SurfaceType SurfaceAt(Vector2 p)
        {
            if (Def.Water) return SurfaceType.Water;
            for (int i = 0; i < Paints.Count; i++)
            {
                if (Paints[i].Shape.Distance(p) <= 0f) return Paints[i].Surface;
            }
            return Def.Base;
        }

        /// <summary>Signed distance of the region where this pad is the top pad.</summary>
        public float VisibleDistance(Vector2 p)
        {
            float d = Def.Shape.Distance(p);
            for (int i = 0; i < Above.Count; i++)
            {
                float a = -Above[i].Def.Shape.Distance(p);
                if (a > d) d = a;
            }
            return d;
        }

        /// <summary>Signed distance of the union of all regions painted with surface s (base included).</summary>
        public float CoverageDistance(SurfaceType s, Vector2 p)
        {
            if (Def.Base == s) return -10f;
            float d = 10f;
            for (int i = 0; i < Paints.Count; i++)
            {
                if (Paints[i].Surface != s) continue;
                float pd = Paints[i].Shape.Distance(p);
                if (pd < d) d = pd;
            }
            return d;
        }
    }

    /// <summary>
    /// Compiled, queryable form of a hole: ground heights, surfaces, collision primitives and
    /// force zones. The ball simulation and the mesh builders read the same model, so what
    /// is drawn is what is simulated.
    /// </summary>
    public sealed class CourseModel
    {
        public const float GridCell = 0.25f;
        const float BroadCell = 2f;

        public readonly HoleDef Def;
        public readonly SimTuning Tuning;
        public readonly Pad[] Pads;
        public readonly List<CollisionSegment> Segments = new List<CollisionSegment>();
        public readonly List<CollisionCircle> Circles = new List<CollisionCircle>();
        public readonly Vector2 Cup;
        public readonly float CupHeight;
        public readonly Vector2 Tee;
        public readonly float TeeHeight;
        public readonly Rect Bounds;

        readonly int bx0, by0, bnx, bny;
        readonly List<int>[] cellPads;
        readonly List<int>[] cellSegs;
        readonly List<int>[] cellCircles;
        readonly List<int>[] cellZones;
        readonly List<ZoneDef> zones;
        // Query scratch (single-threaded use per model instance).
        readonly List<int> scratchSegs = new List<int>(64);
        readonly List<int> scratchCircles = new List<int>(16);
        readonly HashSet<int> scratchSeen = new HashSet<int>();
        int queryStamp;
        int[] segStamp;
        int[] circleStamp;

        public CourseModel(HoleDef def, SimTuning tuning = null, int cupVariant = -1)
        {
            Def = def;
            Tuning = tuning ?? SimTuning.Default;

            // Pads sorted by priority (desc), stable by definition order (later wins ties).
            var list = new List<Pad>();
            for (int i = 0; i < def.Pads.Count; i++) list.Add(new Pad { Def = def.Pads[i] });
            var order = new List<int>();
            for (int i = 0; i < list.Count; i++) order.Add(i);
            order.Sort((a, b) =>
            {
                int pa = def.Pads[a].Priority, pb = def.Pads[b].Priority;
                if (pa != pb) return pb.CompareTo(pa);
                return b.CompareTo(a);
            });
            Pads = new Pad[list.Count];
            for (int k = 0; k < order.Count; k++)
            {
                Pads[k] = list[order[k]];
                Pads[k].Index = k;
            }

            // Paints per pad, highest surface priority first.
            for (int k = 0; k < Pads.Length; k++)
            {
                Pad pad = Pads[k];
                foreach (var paint in def.Paints)
                {
                    if (paint.PadId != null && paint.PadId != pad.Def.Id) continue;
                    if (!Overlaps(paint.Shape.Bounds, pad.Shape.Bounds)) continue;
                    pad.Paints.Add(paint);
                }
                pad.Paints.Sort((a, b) => ((int)b.Surface).CompareTo((int)a.Surface));
                for (int j = 0; j < k; j++)
                {
                    if (Overlaps(Pads[j].Shape.Bounds, pad.Shape.Bounds)) pad.Above.Add(Pads[j]);
                }
            }

            // Bounds of everything.
            Rect b0 = Pads.Length > 0 ? Pads[0].Shape.Bounds : new Rect(-10, -10, 20, 20);
            foreach (var p in Pads) b0 = Union(b0, p.Shape.Bounds);
            Bounds = b0;

            bx0 = Mathf.FloorToInt(Bounds.xMin / BroadCell) - 1;
            by0 = Mathf.FloorToInt(Bounds.yMin / BroadCell) - 1;
            bnx = Mathf.CeilToInt(Bounds.xMax / BroadCell) - bx0 + 2;
            bny = Mathf.CeilToInt(Bounds.yMax / BroadCell) - by0 + 2;
            int nCells = bnx * bny;
            cellPads = new List<int>[nCells];
            cellSegs = new List<int>[nCells];
            cellCircles = new List<int>[nCells];
            cellZones = new List<int>[nCells];
            for (int i = 0; i < nCells; i++)
            {
                cellPads[i] = new List<int>(4);
                cellSegs[i] = new List<int>(4);
                cellCircles[i] = new List<int>(2);
                cellZones[i] = new List<int>(1);
            }
            for (int k = 0; k < Pads.Length; k++) AddToCells(cellPads, Pads[k].Shape.Bounds, k);

            zones = def.Zones;
            for (int z = 0; z < zones.Count; z++) AddToCells(cellZones, zones[z].Shape.Bounds, z);

            Vector2 cupPos = def.Cup;
            if (cupVariant >= 0 && cupVariant < def.AltCups.Count) cupPos = def.AltCups[cupVariant];
            Cup = cupPos;
            Tee = def.Tee;
            CupHeight = Ground(Cup).Height;
            TeeHeight = Ground(Tee).Height;

            BuildContours();
            BuildObstacles();

            segStamp = new int[Segments.Count];
            circleStamp = new int[Circles.Count];
            for (int i = 0; i < Segments.Count; i++) AddToCells(cellSegs, SegBounds(Segments[i]), i);
            for (int i = 0; i < Circles.Count; i++)
            {
                var c = Circles[i];
                AddToCells(cellCircles, new Rect(c.C.x - c.Radius, c.C.y - c.Radius, c.Radius * 2f, c.Radius * 2f), i);
            }
        }

        static bool Overlaps(Rect a, Rect b)
        {
            return a.xMin <= b.xMax && b.xMin <= a.xMax && a.yMin <= b.yMax && b.yMin <= a.yMax;
        }

        static Rect Union(Rect a, Rect b)
        {
            return Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
        }

        static Rect SegBounds(CollisionSegment s)
        {
            float r = s.Radius;
            return Rect.MinMaxRect(Mathf.Min(s.A.x, s.B.x) - r, Mathf.Min(s.A.y, s.B.y) - r, Mathf.Max(s.A.x, s.B.x) + r, Mathf.Max(s.A.y, s.B.y) + r);
        }

        int CellIndex(int cx, int cy)
        {
            cx -= bx0;
            cy -= by0;
            if (cx < 0 || cy < 0 || cx >= bnx || cy >= bny) return -1;
            return cy * bnx + cx;
        }

        void AddToCells(List<int>[] cells, Rect r, int item)
        {
            int x0 = Mathf.FloorToInt(r.xMin / BroadCell), x1 = Mathf.FloorToInt(r.xMax / BroadCell);
            int y0 = Mathf.FloorToInt(r.yMin / BroadCell), y1 = Mathf.FloorToInt(r.yMax / BroadCell);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    int ci = CellIndex(x, y);
                    if (ci >= 0) cells[ci].Add(item);
                }
            }
        }

        // ------------------------------------------------------------------ ground

        public int TopPadIndex(Vector2 p)
        {
            int ci = CellIndex(Mathf.FloorToInt(p.x / BroadCell), Mathf.FloorToInt(p.y / BroadCell));
            if (ci < 0) return -1;
            var pads = cellPads[ci];
            // cellPads lists are built in priority order, so the first hit is the top pad.
            for (int i = 0; i < pads.Count; i++)
            {
                if (Pads[pads[i]].Shape.Distance(p) <= 0f) return pads[i];
            }
            return -1;
        }

        public GroundSample Ground(Vector2 p)
        {
            var g = new GroundSample();
            int pi = TopPadIndex(p);
            g.Pad = pi;
            if (pi < 0)
            {
                g.Height = Def.LowerWaterLevel;
                g.Surface = SurfaceType.Water;
                g.Water = true;
                return g;
            }
            Pad pad = Pads[pi];
            g.Height = pad.HeightAt(p, out g.Gradient);
            g.Surface = pad.SurfaceAt(p);
            g.Water = pad.Water;
            g.OutOfBounds = pad.Def.OutOfBounds;
            return g;
        }

        public float HeightAt(Vector2 p)
        {
            var g = Ground(p);
            return g.Height;
        }

        // ------------------------------------------------------------------ zones

        /// <summary>Acceleration from flowing water and vents at p. coupling scales it (Heavy Core).</summary>
        public Vector2 ZoneAcceleration(Vector2 p, Vector2 v, float coupling, out float staticPush)
        {
            Vector2 a = Vector2.zero;
            staticPush = 0f;
            int ci = CellIndex(Mathf.FloorToInt(p.x / BroadCell), Mathf.FloorToInt(p.y / BroadCell));
            if (ci < 0) return a;
            var list = cellZones[ci];
            for (int i = 0; i < list.Count; i++)
            {
                ZoneDef z = zones[list[i]];
                if (z.Shape.Distance(p) > 0f) continue;
                if (z.Kind == ZoneKind.Flow)
                {
                    Vector2 flow = FlowDirection(z, p) * z.Strength;
                    float k = Tuning.FlowCoupling * coupling;
                    a += (flow - v) * k;
                    staticPush += flow.magnitude * k;
                }
                else
                {
                    Vector2 push = z.PushDir * (z.Strength * coupling);
                    a += push;
                    staticPush += push.magnitude;
                }
            }
            return a;
        }

        public static Vector2 FlowDirection(ZoneDef z, Vector2 p)
        {
            var path = z.FlowPath;
            if (path == null || path.Count < 2) return Vector2.zero;
            float best = float.MaxValue;
            Vector2 dir = Vector2.zero;
            for (int i = 0; i < path.Count - 1; i++)
            {
                float d = Geo2D.DistanceToSegment(p, path[i], path[i + 1]);
                if (d < best)
                {
                    best = d;
                    dir = (path[i + 1] - path[i]).normalized;
                }
            }
            return dir;
        }

        public bool InZone(Vector2 p, out ZoneDef zone)
        {
            zone = null;
            int ci = CellIndex(Mathf.FloorToInt(p.x / BroadCell), Mathf.FloorToInt(p.y / BroadCell));
            if (ci < 0) return false;
            var list = cellZones[ci];
            for (int i = 0; i < list.Count; i++)
            {
                if (zones[list[i]].Shape.Distance(p) <= 0f)
                {
                    zone = zones[list[i]];
                    return true;
                }
            }
            return false;
        }

        // ------------------------------------------------------------------ contours

        void BuildContours()
        {
            for (int k = 0; k < Pads.Length; k++)
            {
                Pad pad = Pads[k];
                Rect r = pad.Shape.Bounds;
                pad.Field = new FieldGrid(r, GridCell, pad.VisibleDistance);
                var loops = MarchingSquares.Contours(pad.Field);
                foreach (var pts in loops)
                {
                    var loop = new ContourLoop();
                    int n = pts.Count;
                    for (int i = 0; i < n; i++)
                    {
                        Vector2 prev = pts[(i - 1 + n) % n], next = pts[(i + 1) % n];
                        Vector2 t = (next - prev);
                        if (t.sqrMagnitude < 1e-10f) t = Vector2.right;
                        t.Normalize();
                        Vector2 outward = new Vector2(t.y, -t.x);
                        loop.Points.Add(pts[i]);
                        loop.Normals.Add(outward);
                        loop.Top.Add(pad.HeightAt(pts[i]));
                        float nh;
                        int np = NeighbourAt(pad, pts[i], outward, out nh);
                        loop.Neighbour.Add(nh);
                        loop.NeighbourPad.Add(np);
                    }
                    pad.Contours.Add(loop);
                }
            }
        }

        int NeighbourAt(Pad pad, Vector2 p, Vector2 outward, out float height)
        {
            float[] steps = { 0.06f, 0.14f, 0.3f };
            for (int s = 0; s < steps.Length; s++)
            {
                Vector2 q = p + outward * steps[s];
                int pi = TopPadIndex(q);
                if (pi == pad.Index) continue;
                if (pi < 0)
                {
                    height = Def.LowerWaterLevel;
                    return -1;
                }
                height = Pads[pi].HeightAt(q);
                return pi;
            }
            height = pad.HeightAt(p);
            return pad.Index;
        }

        // ------------------------------------------------------------------ obstacles

        void BuildObstacles()
        {
            float step = Tuning.StepUp;
            // Ledges: contour stretches where this pad stands above its neighbour.
            for (int k = 0; k < Pads.Length; k++)
            {
                Pad pad = Pads[k];
                foreach (var loop in pad.Contours)
                {
                    int n = loop.Points.Count;
                    var isLedge = new bool[n];
                    int start = -1, ledgeCount = 0;
                    for (int i = 0; i < n; i++)
                    {
                        isLedge[i] = loop.Top[i] - loop.Neighbour[i] > step;
                        if (isLedge[i]) ledgeCount++;
                        else if (start < 0) start = i;
                    }
                    if (ledgeCount == 0) continue;
                    bool closed = start < 0;
                    if (closed) start = 0;
                    var run = new List<Vector2>();
                    float runRef = 0f, runTop = 0f, runBottom = 0f;
                    int count = closed ? n + 1 : n;
                    for (int c = 0; c < count; c++)
                    {
                        int i = (start + c) % n;
                        if (!isLedge[i])
                        {
                            FlushLedge(run, runTop, runBottom, k);
                            continue;
                        }
                        if (run.Count > 0 && Mathf.Abs(loop.Top[i] - runRef) > 0.025f)
                        {
                            // Height changes along ramps: split, sharing the joint so no gap opens.
                            Vector2 joint = run[run.Count - 1];
                            FlushLedge(run, runTop, runBottom, k);
                            run.Add(joint);
                            runRef = loop.Top[i];
                            runTop = loop.Top[i];
                            runBottom = loop.Neighbour[i];
                        }
                        if (run.Count == 0)
                        {
                            runRef = runTop = loop.Top[i];
                            runBottom = loop.Neighbour[i];
                        }
                        runTop = Mathf.Min(runTop, loop.Top[i]);
                        runBottom = Mathf.Min(runBottom, loop.Neighbour[i]);
                        run.Add(loop.Points[i]);
                    }
                    FlushLedge(run, runTop, runBottom, k);
                }
            }

            // Authored walls.
            for (int w = 0; w < Def.Walls.Count; w++)
            {
                WallDef wall = Def.Walls[w];
                if (wall.Path.Count < 2) continue;
                float baseY = float.IsNaN(wall.BaseY) ? GroundUnder(wall.Path) : wall.BaseY;
                float top = baseY + wall.Height;
                int segs = wall.Closed ? wall.Path.Count : wall.Path.Count - 1;
                for (int i = 0; i < segs; i++)
                {
                    Segments.Add(new CollisionSegment
                    {
                        A = wall.Path[i],
                        B = wall.Path[(i + 1) % wall.Path.Count],
                        Radius = wall.Thickness * 0.5f,
                        YBottom = -100f,
                        YTop = top,
                        Material = wall.Material,
                        Owner = 1000 + w,
                    });
                }
            }

            for (int p = 0; p < Def.Posts.Count; p++)
            {
                PostDef post = Def.Posts[p];
                float baseY = float.IsNaN(post.BaseY) ? HeightAt(post.Center) : post.BaseY;
                Circles.Add(new CollisionCircle
                {
                    C = post.Center,
                    Radius = post.Radius,
                    YBottom = -100f,
                    YTop = baseY + post.Height,
                    Material = post.Style == PostStyle.Trunk ? WallMaterial.Wood : (post.Style == PostStyle.Machine ? WallMaterial.Metal : (post.Style == PostStyle.Topiary ? WallMaterial.Hedge : WallMaterial.Stone)),
                    Owner = 2000 + p,
                });
            }

            for (int b = 0; b < Def.Blocks.Count; b++)
            {
                BlockDef block = Def.Blocks[b];
                float baseY = float.IsNaN(block.BaseY) ? HeightAt(block.Box.Center) : block.BaseY;
                Vector2[] c = block.Box.InnerCorners();
                for (int i = 0; i < 4; i++)
                {
                    Segments.Add(new CollisionSegment
                    {
                        A = c[i],
                        B = c[(i + 1) % 4],
                        Radius = block.Box.Corner,
                        YBottom = -100f,
                        YTop = baseY + block.Height,
                        Material = block.Material,
                        Owner = 3000 + b,
                    });
                }
            }
        }

        float GroundUnder(List<Vector2> path)
        {
            float h = float.MinValue;
            for (int i = 0; i < path.Count; i++) h = Mathf.Max(h, HeightAt(path[i]));
            return h;
        }

        void FlushLedge(List<Vector2> run, float top, float bottom, int padIndex)
        {
            if (run.Count < 2)
            {
                run.Clear();
                return;
            }
            var simple = Geo2D.Simplify(run, 0.006f);
            for (int i = 0; i < simple.Count - 1; i++)
            {
                Vector2 dir = simple[i + 1] - simple[i];
                if (dir.sqrMagnitude < 1e-10f) continue;
                dir.Normalize();
                Segments.Add(new CollisionSegment
                {
                    A = simple[i],
                    B = simple[i + 1],
                    Radius = 0f,
                    YBottom = bottom - 0.02f,
                    YTop = top,
                    Material = WallMaterial.Ledge,
                    Owner = padIndex,
                    OneSided = true,
                    // Contours run counter-clockwise around the pad, so outside is on the right.
                    OuterNormal = new Vector2(dir.y, -dir.x),
                });
            }
            run.Clear();
        }

        // ------------------------------------------------------------------ sweeps

        /// <summary>
        /// Horizontal contact radius of the ball against an obstacle whose top edge is at yTop:
        /// a ball partly above the edge touches it with a smaller circle, so balls roll over
        /// edges smoothly. Returns a negative value when the obstacle is not touched at all.
        /// </summary>
        static float EffectiveRadius(float yTop, float yBottom, float ballY, float r)
        {
            if (ballY + 2f * r <= yBottom) return -1f;
            float dy = ballY + r - yTop;
            if (dy <= 0f) return r;
            if (dy >= r) return -1f;
            return Mathf.Sqrt(r * r - dy * dy);
        }

        static bool OnInnerSide(in CollisionSegment s, Vector2 p)
        {
            return s.OneSided && Vector2.Dot(p - s.A, s.OuterNormal) < -1e-4f;
        }

        void Gather(Vector2 p, Vector2 delta, float margin)
        {
            queryStamp++;
            if (queryStamp == int.MaxValue)
            {
                queryStamp = 1;
                Array.Clear(segStamp, 0, segStamp.Length);
                Array.Clear(circleStamp, 0, circleStamp.Length);
            }
            scratchSegs.Clear();
            scratchCircles.Clear();
            Vector2 q = p + delta;
            float x0 = Mathf.Min(p.x, q.x) - margin, x1 = Mathf.Max(p.x, q.x) + margin;
            float y0 = Mathf.Min(p.y, q.y) - margin, y1 = Mathf.Max(p.y, q.y) + margin;
            int cx0 = Mathf.FloorToInt(x0 / BroadCell), cx1 = Mathf.FloorToInt(x1 / BroadCell);
            int cy0 = Mathf.FloorToInt(y0 / BroadCell), cy1 = Mathf.FloorToInt(y1 / BroadCell);
            for (int cy = cy0; cy <= cy1; cy++)
            {
                for (int cx = cx0; cx <= cx1; cx++)
                {
                    int ci = CellIndex(cx, cy);
                    if (ci < 0) continue;
                    var s = cellSegs[ci];
                    for (int i = 0; i < s.Count; i++)
                    {
                        int id = s[i];
                        if (segStamp[id] == queryStamp) continue;
                        segStamp[id] = queryStamp;
                        scratchSegs.Add(id);
                    }
                    var c = cellCircles[ci];
                    for (int i = 0; i < c.Count; i++)
                    {
                        int id = c[i];
                        if (circleStamp[id] == queryStamp) continue;
                        circleStamp[id] = queryStamp;
                        scratchCircles.Add(id);
                    }
                }
            }
        }

        /// <summary>
        /// Pushes the ball out of anything it overlaps (numerical safety). Returns true if moved.
        /// </summary>
        public bool Depenetrate(ref Vector2 p, float ballY, float r, out Vector2 normal)
        {
            normal = Vector2.zero;
            bool moved = false;
            for (int iter = 0; iter < 4; iter++)
            {
                Gather(p, Vector2.zero, r + 0.6f);
                float worst = -1e-4f;
                Vector2 push = Vector2.zero;
                for (int k = 0; k < scratchSegs.Count; k++)
                {
                    var s = Segments[scratchSegs[k]];
                    float re = EffectiveRadius(s.YTop, s.YBottom, ballY, r);
                    if (re < 1e-4f || OnInnerSide(s, p)) continue;
                    float t;
                    Vector2 c = Geo2D.ClosestOnSegment(p, s.A, s.B, out t);
                    Vector2 d = p - c;
                    float dist = d.magnitude;
                    float pen = dist - (s.Radius + re);
                    if (pen < worst)
                    {
                        worst = pen;
                        Vector2 n = dist > 1e-6f ? d / dist : Geo2D.Perp((s.B - s.A).normalized);
                        push = n * (-pen + 0.0008f);
                    }
                }
                for (int k = 0; k < scratchCircles.Count; k++)
                {
                    var c = Circles[scratchCircles[k]];
                    float re = EffectiveRadius(c.YTop, c.YBottom, ballY, r);
                    if (re < 1e-4f) continue;
                    Vector2 d = p - c.C;
                    float dist = d.magnitude;
                    float pen = dist - (c.Radius + re);
                    if (pen < worst)
                    {
                        worst = pen;
                        Vector2 n = dist > 1e-6f ? d / dist : Vector2.up;
                        push = n * (-pen + 0.0008f);
                    }
                }
                if (push == Vector2.zero) break;
                p += push;
                normal = push.normalized;
                moved = true;
            }
            return moved;
        }

        /// <summary>
        /// Continuous sweep of a ball (radius r, contact height ballY) from p by delta.
        /// Returns the earliest hit with t in [0,1].
        /// </summary>
        public bool Sweep(Vector2 p, Vector2 delta, float ballY, float r, out SweepHit hit)
        {
            hit = new SweepHit { T = 2f };
            float len = delta.magnitude;
            if (len < 1e-9f) return false;
            Gather(p, delta, r + 0.6f);
            for (int k = 0; k < scratchSegs.Count; k++)
            {
                int id = scratchSegs[k];
                var s = Segments[id];
                float re = EffectiveRadius(s.YTop, s.YBottom, ballY, r);
                if (re < 1e-4f || OnInnerSide(s, p)) continue;
                float t;
                Vector2 n;
                if (SweepCapsule(p, delta, s.A, s.B, s.Radius + re, out t, out n) && t < hit.T)
                {
                    hit.T = t;
                    hit.Normal = n;
                    hit.Material = s.Material;
                    hit.Owner = s.Owner;
                    hit.IsSegment = true;
                    hit.Index = id;
                }
            }
            for (int k = 0; k < scratchCircles.Count; k++)
            {
                int id = scratchCircles[k];
                var c = Circles[id];
                float re = EffectiveRadius(c.YTop, c.YBottom, ballY, r);
                if (re < 1e-4f) continue;
                float t;
                if (SweepCircle(p, delta, c.C, c.Radius + re, out t) && t < hit.T)
                {
                    Vector2 x = p + delta * t;
                    hit.T = t;
                    hit.Normal = (x - c.C).normalized;
                    hit.Material = c.Material;
                    hit.Owner = c.Owner;
                    hit.IsSegment = false;
                    hit.Index = id;
                }
            }
            if (hit.T <= 1f)
            {
                hit.Point = p + delta * hit.T;
                return true;
            }
            return false;
        }

        /// <summary>Earliest time a moving point enters a circle; only when approaching.</summary>
        public static bool SweepCircle(Vector2 p, Vector2 d, Vector2 c, float R, out float t)
        {
            t = 0f;
            Vector2 m = p - c;
            float a = Vector2.Dot(d, d);
            float b = Vector2.Dot(m, d);
            float cc = Vector2.Dot(m, m) - R * R;
            if (b >= 0f) return false;          // moving away or tangent
            if (cc <= 0f) { t = 0f; return true; } // already touching/overlapping and approaching
            float disc = b * b - a * cc;
            if (disc < 0f) return false;
            t = (-b - Mathf.Sqrt(disc)) / a;
            return t >= 0f && t <= 1f;
        }

        /// <summary>Earliest time a moving point comes within R of segment ab (approaching only).</summary>
        public static bool SweepCapsule(Vector2 p, Vector2 d, Vector2 a, Vector2 b, float R, out float t, out Vector2 normal)
        {
            t = 2f;
            normal = Vector2.zero;
            Vector2 ab = b - a;
            float L = ab.magnitude;
            bool found = false;
            if (L > 1e-6f)
            {
                Vector2 u = ab / L;
                Vector2 n = new Vector2(-u.y, u.x);
                float side = Vector2.Dot(p - a, n);
                float dn = Vector2.Dot(d, n);
                float sgn = side >= 0f ? 1f : -1f;
                if (dn * sgn < 0f)
                {
                    float tt = (sgn * R - side) / dn;
                    if (Mathf.Abs(side) <= R) tt = 0f; // overlapping the slab already
                    if (tt >= 0f && tt <= 1f)
                    {
                        Vector2 x = p + d * tt;
                        float s = Vector2.Dot(x - a, u);
                        if (s >= 0f && s <= L)
                        {
                            t = tt;
                            normal = n * sgn;
                            found = true;
                        }
                    }
                }
            }
            float tc;
            if (SweepCircle(p, d, a, R, out tc) && tc < t)
            {
                t = tc;
                normal = (p + d * tc - a).normalized;
                found = true;
            }
            if (SweepCircle(p, d, b, R, out tc) && tc < t)
            {
                t = tc;
                normal = (p + d * tc - b).normalized;
                found = true;
            }
            return found;
        }

        /// <summary>Closest distance from p to an active obstacle surface (minus ball radius).</summary>
        public float Clearance(Vector2 p, float ballY, float r, float range)
        {
            Gather(p, Vector2.zero, r + range);
            float best = range;
            for (int k = 0; k < scratchSegs.Count; k++)
            {
                var s = Segments[scratchSegs[k]];
                float re = EffectiveRadius(s.YTop, s.YBottom, ballY, r);
                if (re < 1e-4f || OnInnerSide(s, p)) continue;
                float d = Geo2D.DistanceToSegment(p, s.A, s.B) - s.Radius - re;
                if (d < best) best = d;
            }
            for (int k = 0; k < scratchCircles.Count; k++)
            {
                var c = Circles[scratchCircles[k]];
                float re = EffectiveRadius(c.YTop, c.YBottom, ballY, r);
                if (re < 1e-4f) continue;
                float d = (p - c.C).magnitude - c.Radius - re;
                if (d < best) best = d;
            }
            return best;
        }
    }
}
