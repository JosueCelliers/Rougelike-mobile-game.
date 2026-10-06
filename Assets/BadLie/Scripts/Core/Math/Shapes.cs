using System;
using System.Collections.Generic;
using UnityEngine;

namespace BadLie.Core
{
    /// <summary>
    /// A 2D region on the course plane (world X,Z mapped to x,y) described by a signed
    /// distance function: negative inside, positive outside. Every shape is a distance
    /// bound (Lipschitz 1), so it can be used for coverage, contouring and queries.
    /// </summary>
    public abstract class Shape2D
    {
        /// <summary>Bounds of the inside region (where Distance &lt;= 0).</summary>
        public Rect Bounds { get; protected set; }

        public abstract float Distance(Vector2 p);

        public virtual Vector2 Gradient(Vector2 p)
        {
            const float e = 0.0025f;
            float dx = Distance(new Vector2(p.x + e, p.y)) - Distance(new Vector2(p.x - e, p.y));
            float dy = Distance(new Vector2(p.x, p.y + e)) - Distance(new Vector2(p.x, p.y - e));
            Vector2 g = new Vector2(dx, dy);
            float m = g.magnitude;
            return m > 1e-6f ? g / m : Vector2.up;
        }

        public bool Contains(Vector2 p) { return Distance(p) <= 0f; }

        protected static Rect Inflate(Rect r, float d)
        {
            return Rect.MinMaxRect(r.xMin - d, r.yMin - d, r.xMax + d, r.yMax + d);
        }

        protected static Rect BoundsOf(IList<Vector2> pts)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            for (int i = 0; i < pts.Count; i++)
            {
                Vector2 p = pts[i];
                if (p.x < x0) x0 = p.x;
                if (p.y < y0) y0 = p.y;
                if (p.x > x1) x1 = p.x;
                if (p.y > y1) y1 = p.y;
            }
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }
    }

    public sealed class CircleShape : Shape2D
    {
        public readonly Vector2 Center;
        public readonly float Radius;

        public CircleShape(Vector2 center, float radius)
        {
            Center = center;
            Radius = radius;
            Bounds = new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f);
        }

        public override float Distance(Vector2 p) { return (p - Center).magnitude - Radius; }

        public override Vector2 Gradient(Vector2 p)
        {
            Vector2 d = p - Center;
            float m = d.magnitude;
            return m > 1e-6f ? d / m : Vector2.up;
        }
    }

    /// <summary>Ellipse approximated by a scaled circle distance (a conservative bound).</summary>
    public sealed class EllipseShape : Shape2D
    {
        readonly Vector2 center;
        readonly Vector2 radii;
        readonly float cos, sin;

        public EllipseShape(Vector2 center, Vector2 radii, float angleDeg = 0f)
        {
            this.center = center;
            this.radii = radii;
            float a = angleDeg * Mathf.Deg2Rad;
            cos = Mathf.Cos(a);
            sin = Mathf.Sin(a);
            float r = Mathf.Max(radii.x, radii.y);
            Bounds = new Rect(center.x - r, center.y - r, r * 2f, r * 2f);
        }

        public override float Distance(Vector2 p)
        {
            Vector2 d = p - center;
            Vector2 l = new Vector2(d.x * cos + d.y * sin, -d.x * sin + d.y * cos);
            Vector2 q = new Vector2(l.x / radii.x, l.y / radii.y);
            float k = q.magnitude;
            // Scale the normalised distance by the smaller radius: never overestimates.
            return (k - 1f) * Mathf.Min(radii.x, radii.y);
        }
    }

    /// <summary>
    /// Simple polygon (any winding, may be concave) optionally inflated by a corner radius,
    /// which rounds convex corners.
    /// </summary>
    public sealed class PolygonShape : Shape2D
    {
        readonly Vector2[] v;
        readonly float round;

        public IReadOnlyList<Vector2> Points { get { return v; } }

        /// <summary>
        /// The outline passes through the given points; convex corners are rounded with
        /// cornerRadius (the polygon is inset by the radius and the distance offset back out).
        /// </summary>
        public PolygonShape(IList<Vector2> points, float cornerRadius = 0f)
        {
            round = Mathf.Max(0f, cornerRadius);
            v = round > 0f ? Inset(points, round) : Copy(points);
            Bounds = Inflate(BoundsOf(v), round);
        }

        static Vector2[] Copy(IList<Vector2> pts)
        {
            var a = new Vector2[pts.Count];
            for (int i = 0; i < a.Length; i++) a[i] = pts[i];
            return a;
        }

        static Vector2[] Inset(IList<Vector2> pts, float d)
        {
            int n = pts.Count;
            float area = Geo2D.SignedArea(pts);
            float s = area >= 0f ? 1f : -1f; // CCW: interior on the left
            var result = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 prev = pts[(i - 1 + n) % n], cur = pts[i], next = pts[(i + 1) % n];
                Vector2 e0 = (cur - prev).normalized, e1 = (next - cur).normalized;
                Vector2 n0 = new Vector2(-e0.y, e0.x) * s, n1 = new Vector2(-e1.y, e1.x) * s;
                Vector2 a0 = prev + n0 * d, a1 = cur + n1 * d;
                float denom = Geo2D.Cross(e0, e1);
                if (Mathf.Abs(denom) < 1e-4f)
                {
                    result[i] = cur + n0 * d;
                    continue;
                }
                float t = Geo2D.Cross(a1 - a0, e1) / denom;
                result[i] = a0 + e0 * t;
            }
            return result;
        }

        public override float Distance(Vector2 p)
        {
            // Inigo Quilez, exact signed distance to a polygon.
            int n = v.Length;
            Vector2 d0 = p - v[0];
            float d = Vector2.Dot(d0, d0);
            float s = 1f;
            for (int i = 0, j = n - 1; i < n; j = i, i++)
            {
                Vector2 e = v[j] - v[i];
                Vector2 w = p - v[i];
                float t = Mathf.Clamp01(Vector2.Dot(w, e) / Mathf.Max(Vector2.Dot(e, e), 1e-12f));
                Vector2 b = w - e * t;
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c0 = p.y >= v[i].y;
                bool c1 = p.y < v[j].y;
                bool c2 = e.x * w.y > e.y * w.x;
                if ((c0 && c1 && c2) || (!c0 && !c1 && !c2)) s = -s;
            }
            return s * Mathf.Sqrt(d) - round;
        }

        public override Vector2 Gradient(Vector2 p)
        {
            // Direction from the closest boundary point, signed by inside/outside.
            int n = v.Length;
            float best = float.MaxValue;
            Vector2 closest = v[0];
            for (int i = 0, j = n - 1; i < n; j = i, i++)
            {
                Vector2 e = v[j] - v[i];
                Vector2 w = p - v[i];
                float t = Mathf.Clamp01(Vector2.Dot(w, e) / Mathf.Max(Vector2.Dot(e, e), 1e-12f));
                Vector2 c = v[i] + e * t;
                float dd = (p - c).sqrMagnitude;
                if (dd < best) { best = dd; closest = c; }
            }
            Vector2 dir = p - closest;
            float m = dir.magnitude;
            if (m < 1e-5f) return base.Gradient(p);
            dir /= m;
            return Distance(p) + round < 0f ? -dir : dir;
        }
    }

    /// <summary>A thick polyline (union of capsules). Good for paths, fairways and walls.</summary>
    public sealed class CapsuleChainShape : Shape2D
    {
        readonly Vector2[] pts;
        readonly float[] radii;
        readonly bool closed;

        public IReadOnlyList<Vector2> Points { get { return pts; } }

        public CapsuleChainShape(IList<Vector2> points, float radius, bool closed = false)
            : this(points, Uniform(points.Count, radius), closed) { }

        /// <summary>Per-point radius, linearly interpolated along each segment.</summary>
        public CapsuleChainShape(IList<Vector2> points, IList<float> pointRadii, bool closed = false)
        {
            pts = new Vector2[points.Count];
            radii = new float[points.Count];
            float maxR = 0f;
            for (int i = 0; i < pts.Length; i++)
            {
                pts[i] = points[i];
                radii[i] = pointRadii[i];
                maxR = Mathf.Max(maxR, radii[i]);
            }
            this.closed = closed;
            Bounds = Inflate(BoundsOf(pts), maxR);
        }

        static float[] Uniform(int n, float r)
        {
            var a = new float[n];
            for (int i = 0; i < n; i++) a[i] = r;
            return a;
        }

        int SegmentCount { get { return closed ? pts.Length : pts.Length - 1; } }

        public override float Distance(Vector2 p)
        {
            if (pts.Length == 1) return (p - pts[0]).magnitude - radii[0];
            float best = float.MaxValue;
            int segs = SegmentCount;
            for (int i = 0; i < segs; i++)
            {
                int j = (i + 1) % pts.Length;
                Vector2 a = pts[i], b = pts[j];
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(Vector2.Dot(ab, ab), 1e-12f));
                float r = Mathf.Lerp(radii[i], radii[j], t);
                float d = (p - (a + ab * t)).magnitude - r;
                if (d < best) best = d;
            }
            return best;
        }
    }

    /// <summary>Rotated box with rounded corners. Size is the full outer size.</summary>
    public sealed class BoxShape : Shape2D
    {
        public readonly Vector2 Center;
        public readonly Vector2 HalfSize;
        public readonly float AngleDeg;
        public readonly float Corner;
        readonly float cos, sin;

        public BoxShape(Vector2 center, Vector2 size, float angleDeg = 0f, float corner = 0f)
        {
            Center = center;
            HalfSize = size * 0.5f;
            AngleDeg = angleDeg;
            Corner = Mathf.Min(corner, Mathf.Min(HalfSize.x, HalfSize.y));
            float a = angleDeg * Mathf.Deg2Rad;
            cos = Mathf.Cos(a);
            sin = Mathf.Sin(a);
            float ex = Mathf.Abs(HalfSize.x * cos) + Mathf.Abs(HalfSize.y * sin);
            float ey = Mathf.Abs(HalfSize.x * sin) + Mathf.Abs(HalfSize.y * cos);
            Bounds = Rect.MinMaxRect(center.x - ex, center.y - ey, center.x + ex, center.y + ey);
        }

        public Vector2 ToLocal(Vector2 p)
        {
            Vector2 d = p - Center;
            return new Vector2(d.x * cos + d.y * sin, -d.x * sin + d.y * cos);
        }

        public Vector2 ToWorld(Vector2 l)
        {
            return Center + new Vector2(l.x * cos - l.y * sin, l.x * sin + l.y * cos);
        }

        public override float Distance(Vector2 p)
        {
            Vector2 l = ToLocal(p);
            Vector2 b = HalfSize - new Vector2(Corner, Corner);
            Vector2 q = new Vector2(Mathf.Abs(l.x) - b.x, Mathf.Abs(l.y) - b.y);
            Vector2 qm = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
            return qm.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - Corner;
        }

        /// <summary>Corner points of the inner (un-rounded) rectangle, counter-clockwise.</summary>
        public Vector2[] InnerCorners()
        {
            Vector2 b = HalfSize - new Vector2(Corner, Corner);
            return new[]
            {
                ToWorld(new Vector2(-b.x, -b.y)), ToWorld(new Vector2(b.x, -b.y)),
                ToWorld(new Vector2(b.x, b.y)), ToWorld(new Vector2(-b.x, b.y))
            };
        }
    }

    /// <summary>A circular arc band (curved wall or path). Angles in degrees, CCW from +x.</summary>
    public sealed class ArcShape : Shape2D
    {
        public readonly Vector2 Center;
        public readonly float Radius, From, To, HalfWidth;

        public ArcShape(Vector2 center, float radius, float fromDeg, float toDeg, float halfWidth)
        {
            Center = center;
            Radius = radius;
            From = fromDeg;
            To = toDeg < fromDeg ? toDeg + 360f : toDeg;
            HalfWidth = halfWidth;
            var samples = SamplePoints(24);
            Bounds = Inflate(BoundsOf(samples), halfWidth);
        }

        public Vector2 PointAt(float deg)
        {
            float a = deg * Mathf.Deg2Rad;
            return Center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Radius;
        }

        public List<Vector2> SamplePoints(int segments)
        {
            var list = new List<Vector2>(segments + 1);
            for (int i = 0; i <= segments; i++) list.Add(PointAt(Mathf.Lerp(From, To, i / (float)segments)));
            return list;
        }

        public override float Distance(Vector2 p)
        {
            Vector2 d = p - Center;
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            float rel = Mathf.Repeat(ang - From, 360f);
            float span = To - From;
            float dist;
            if (rel <= span) dist = Mathf.Abs(d.magnitude - Radius);
            else dist = Mathf.Min((p - PointAt(From)).magnitude, (p - PointAt(To)).magnitude);
            return dist - HalfWidth;
        }
    }

    public sealed class UnionShape : Shape2D
    {
        readonly Shape2D[] parts;

        public UnionShape(params Shape2D[] shapes)
        {
            parts = shapes;
            Rect r = shapes[0].Bounds;
            for (int i = 1; i < shapes.Length; i++)
            {
                Rect b = shapes[i].Bounds;
                r = Rect.MinMaxRect(Mathf.Min(r.xMin, b.xMin), Mathf.Min(r.yMin, b.yMin), Mathf.Max(r.xMax, b.xMax), Mathf.Max(r.yMax, b.yMax));
            }
            Bounds = r;
        }

        public override float Distance(Vector2 p)
        {
            float d = float.MaxValue;
            for (int i = 0; i < parts.Length; i++)
            {
                Shape2D s = parts[i];
                Rect b = s.Bounds;
                // Cheap bound: distance to the part's AABB never exceeds its true distance.
                float bx = Mathf.Max(b.xMin - p.x, 0f, p.x - b.xMax);
                float by = Mathf.Max(b.yMin - p.y, 0f, p.y - b.yMax);
                float lower = Mathf.Sqrt(bx * bx + by * by);
                if (lower >= d) continue;
                float sd = s.Distance(p);
                if (sd < d) d = sd;
            }
            return d;
        }
    }

    /// <summary>A minus B.</summary>
    public sealed class SubtractShape : Shape2D
    {
        readonly Shape2D a, b;

        public SubtractShape(Shape2D a, Shape2D b)
        {
            this.a = a;
            this.b = b;
            Bounds = a.Bounds;
        }

        public override float Distance(Vector2 p) { return Mathf.Max(a.Distance(p), -b.Distance(p)); }
    }

    public sealed class IntersectShape : Shape2D
    {
        readonly Shape2D a, b;

        public IntersectShape(Shape2D a, Shape2D b)
        {
            this.a = a;
            this.b = b;
            Rect ra = a.Bounds, rb = b.Bounds;
            Bounds = Rect.MinMaxRect(Mathf.Max(ra.xMin, rb.xMin), Mathf.Max(ra.yMin, rb.yMin), Mathf.Min(ra.xMax, rb.xMax), Mathf.Min(ra.yMax, rb.yMax));
        }

        public override float Distance(Vector2 p) { return Mathf.Max(a.Distance(p), b.Distance(p)); }
    }

    /// <summary>Grows (positive) or shrinks (negative) a shape.</summary>
    public sealed class OffsetShape : Shape2D
    {
        readonly Shape2D s;
        readonly float delta;

        public OffsetShape(Shape2D shape, float delta)
        {
            s = shape;
            this.delta = delta;
            Bounds = Inflate(shape.Bounds, Mathf.Max(delta, 0f));
        }

        public override float Distance(Vector2 p) { return s.Distance(p) - delta; }
    }

    /// <summary>
    /// An organic outline: the edge of a shape pushed in and out by smooth value noise, for
    /// bunkers and beds that should not look drawn with a compass. Scaled down so the result
    /// stays a distance bound.
    /// </summary>
    public sealed class WobbleShape : Shape2D
    {
        readonly Shape2D s;
        readonly float amp, freq, norm;
        readonly int seed;

        public WobbleShape(Shape2D shape, float amplitude, float wavelength, int seed)
        {
            s = shape;
            amp = amplitude;
            freq = 1f / wavelength;
            this.seed = seed;
            // Value noise changes by at most ~2.2 per cell; keep the gradient at or below 1.
            norm = 1f / (1f + 2f * amp * 2.2f * freq);
            Bounds = Inflate(shape.Bounds, amp);
        }

        public override float Distance(Vector2 p)
        {
            float n = Noise.Value2(p.x * freq, p.y * freq, seed) * 2f - 1f;
            return (s.Distance(p) + amp * n) * norm;
        }
    }

    public static class Geo2D
    {
        public static Vector2 XZ(Vector3 v) { return new Vector2(v.x, v.z); }
        public static Vector3 X0Z(Vector2 v, float y = 0f) { return new Vector3(v.x, y, v.y); }
        public static Vector2 Perp(Vector2 v) { return new Vector2(-v.y, v.x); }
        public static float Cross(Vector2 a, Vector2 b) { return a.x * b.y - a.y * b.x; }

        public static Vector2 Rotate(Vector2 v, float deg)
        {
            float a = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        public static Vector2 FromAngle(float deg)
        {
            float a = deg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        /// <summary>Closest point on segment ab to p, with parameter t.</summary>
        public static Vector2 ClosestOnSegment(Vector2 p, Vector2 a, Vector2 b, out float t)
        {
            Vector2 ab = b - a;
            float len2 = Vector2.Dot(ab, ab);
            t = len2 > 1e-12f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2) : 0f;
            return a + ab * t;
        }

        public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            float t;
            return (p - ClosestOnSegment(p, a, b, out t)).magnitude;
        }

        /// <summary>Catmull-Rom resampling of a polyline into a smooth curve.</summary>
        public static List<Vector2> SmoothPath(IList<Vector2> pts, int samplesPerSegment)
        {
            var result = new List<Vector2>();
            if (pts.Count < 3)
            {
                for (int i = 0; i < pts.Count; i++) result.Add(pts[i]);
                return result;
            }
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector2 p0 = pts[Mathf.Max(i - 1, 0)];
                Vector2 p1 = pts[i];
                Vector2 p2 = pts[i + 1];
                Vector2 p3 = pts[Mathf.Min(i + 2, pts.Count - 1)];
                for (int s = 0; s < samplesPerSegment; s++)
                {
                    float t = s / (float)samplesPerSegment;
                    result.Add(CatmullRom(p0, p1, p2, p3, t));
                }
            }
            result.Add(pts[pts.Count - 1]);
            return result;
        }

        public static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        public static float PathLength(IList<Vector2> pts)
        {
            float l = 0f;
            for (int i = 1; i < pts.Count; i++) l += (pts[i] - pts[i - 1]).magnitude;
            return l;
        }

        /// <summary>Resample a polyline at a fixed spacing (keeps end points).</summary>
        public static List<Vector2> Resample(IList<Vector2> pts, float spacing)
        {
            var outPts = new List<Vector2>();
            if (pts.Count == 0) return outPts;
            outPts.Add(pts[0]);
            float carry = 0f;
            for (int i = 1; i < pts.Count; i++)
            {
                Vector2 a = pts[i - 1], b = pts[i];
                float seg = (b - a).magnitude;
                float pos = spacing - carry;
                while (pos < seg)
                {
                    outPts.Add(Vector2.Lerp(a, b, pos / seg));
                    pos += spacing;
                }
                carry = seg - (pos - spacing);
            }
            if ((outPts[outPts.Count - 1] - pts[pts.Count - 1]).sqrMagnitude > 1e-6f) outPts.Add(pts[pts.Count - 1]);
            return outPts;
        }

        /// <summary>Douglas-Peucker simplification.</summary>
        public static List<Vector2> Simplify(IList<Vector2> pts, float tolerance)
        {
            if (pts.Count < 3) return new List<Vector2>(pts);
            var keep = new bool[pts.Count];
            keep[0] = keep[pts.Count - 1] = true;
            var stack = new Stack<KeyValuePair<int, int>>();
            stack.Push(new KeyValuePair<int, int>(0, pts.Count - 1));
            while (stack.Count > 0)
            {
                var range = stack.Pop();
                float maxD = 0f;
                int idx = -1;
                for (int i = range.Key + 1; i < range.Value; i++)
                {
                    float d = DistanceToSegment(pts[i], pts[range.Key], pts[range.Value]);
                    if (d > maxD) { maxD = d; idx = i; }
                }
                if (idx >= 0 && maxD > tolerance)
                {
                    keep[idx] = true;
                    stack.Push(new KeyValuePair<int, int>(range.Key, idx));
                    stack.Push(new KeyValuePair<int, int>(idx, range.Value));
                }
            }
            var result = new List<Vector2>();
            for (int i = 0; i < pts.Count; i++) if (keep[i]) result.Add(pts[i]);
            return result;
        }

        public static float SignedArea(IList<Vector2> poly)
        {
            float a = 0f;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i, i++) a += Cross(poly[j], poly[i]);
            return a * 0.5f;
        }
    }

    public static class Ease
    {
        public static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        public static float SmoothDeriv01(float t)
        {
            if (t <= 0f || t >= 1f) return 0f;
            return 6f * t * (1f - t);
        }

        public static float OutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1f - t;
            return 1f - u * u * u;
        }

        public static float InOutSine(float t)
        {
            return -(Mathf.Cos(Mathf.PI * Mathf.Clamp01(t)) - 1f) * 0.5f;
        }
    }
}
