using System.Collections.Generic;
using BadLie.Course;
using BadLie.Sim;
using UnityEngine;

namespace BadLie.Game
{
    /// <summary>
    /// In-world aim feedback: a dotted preview of the first part of the real simulated path
    /// (marking banks, skips and hazards), a segmented power ring around the ball and a short
    /// pull tail showing the drag. Drawn on top so it is never hidden.
    /// </summary>
    public sealed class AimVisuals : MonoBehaviour
    {
        Mesh mesh;
        MeshRenderer mr;
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Color> cols = new List<Color>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<int> tris = new List<int>();

        static readonly Color Dot = new Color(1f, 0.95f, 0.8f, 1f);
        static readonly Color Bank = new Color(1f, 0.82f, 0.45f, 1f);
        static readonly Color SkipC = new Color(0.62f, 1f, 0.95f, 1f);
        static readonly Color Hazard = new Color(0.95f, 0.36f, 0.27f, 1f);
        static readonly Color RingLow = new Color(1f, 0.92f, 0.7f, 0.95f);
        static readonly Color RingHigh = new Color(0.98f, 0.42f, 0.24f, 0.95f);
        static readonly Color RingEmpty = new Color(0.1f, 0.07f, 0.1f, 0.55f);

        public Vector3 LastPreviewEnd { get; private set; }

        public static AimVisuals Create(GameConfig cfg, Transform parent)
        {
            var go = new GameObject("Aim Visuals");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<AimVisuals>();
            v.mesh = new Mesh { name = "Aim" };
            v.mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = v.mesh;
            v.mr = go.AddComponent<MeshRenderer>();
            v.mr.sharedMaterial = cfg.AimLine;
            v.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            v.mr.receiveShadows = false;
            v.mr.enabled = false;
            return v;
        }

        public void Hide()
        {
            if (mr != null) mr.enabled = false;
        }

        public void Draw(CourseModel course, Vector3 ball, Vector2 dir, float power, bool cancel, SimResult preview)
        {
            verts.Clear();
            cols.Clear();
            uvs.Clear();
            tris.Clear();
            float alpha = cancel ? 0.3f : 1f;

            // Power ring.
            const int segs = 18;
            float r0 = 0.5f, r1 = 0.78f;
            int filled = Mathf.CeilToInt(power * segs - 0.001f);
            for (int i = 0; i < segs; i++)
            {
                // Segments start behind the ball (pull side) and wrap round.
                float a0 = Mathf.Atan2(-dir.y, -dir.x) + (i + 0.12f) / segs * Mathf.PI * 2f;
                float a1 = Mathf.Atan2(-dir.y, -dir.x) + (i + 0.88f) / segs * Mathf.PI * 2f;
                Color c = i < filled ? Color.Lerp(RingLow, RingHigh, i / (float)(segs - 1)) : RingEmpty;
                if (cancel) c = new Color(c.r * 0.6f, c.g * 0.6f, c.b * 0.6f, c.a * 0.5f);
                ArcQuad(course, ball, r0, r1, a0, a1, c);
            }

            if (!cancel)
            {
                // Pull tail behind the ball.
                Vector2 back = -dir;
                float tail = 0.5f + 1.3f * power;
                for (float s = 0.82f; s < tail + 0.8f; s += 0.2f)
                {
                    Vector2 p = new Vector2(ball.x, ball.z) + back * s;
                    float k = 1f - (s - 0.82f) / (tail + 0.01f);
                    Disc(course, p, 0.05f, new Color(1f, 0.95f, 0.85f, 0.5f * Mathf.Clamp01(k)));
                }
            }

            // Preview path.
            LastPreviewEnd = ball;
            if (preview != null && preview.Path.Count > 1)
            {
                float spacing = 0.3f;
                float acc = spacing * 0.6f;
                var path = preview.Path;
                float total = 0f;
                for (int i = 1; i < path.Count; i++) total += Flat(path[i].Position - path[i - 1].Position);
                float walked = 0f;
                for (int i = 1; i < path.Count; i++)
                {
                    Vector3 a = path[i - 1].Position, b = path[i].Position;
                    float seg = Flat(b - a);
                    while (acc <= seg && seg > 1e-5f)
                    {
                        Vector3 p = Vector3.Lerp(a, b, acc / seg);
                        float along = (walked + acc) / Mathf.Max(total, 0.01f);
                        float fade = Mathf.Clamp01(1.15f - along) * alpha;
                        float size = Mathf.Lerp(0.13f, 0.085f, along);
                        DiscAt(p, size, new Color(Dot.r, Dot.g, Dot.b, Dot.a * fade));
                        acc += spacing;
                    }
                    acc -= seg;
                    walked += seg;
                }
                LastPreviewEnd = path[path.Count - 1].Position;
                foreach (var e in preview.Events)
                {
                    switch (e.Type)
                    {
                        case SimEventType.Wall: Diamond(e.Position, 0.13f, new Color(Dot.r, Dot.g, Dot.b, alpha)); break;
                        case SimEventType.BankWall: Diamond(e.Position, 0.18f, new Color(Bank.r, Bank.g, Bank.b, alpha)); break;
                        case SimEventType.Skip: RingAt(e.Position, 0.22f, 0.3f, new Color(SkipC.r, SkipC.g, SkipC.b, alpha)); break;
                        case SimEventType.Splash:
                        case SimEventType.Fell:
                            Cross(e.Position, 0.2f, new Color(Hazard.r, Hazard.g, Hazard.b, alpha));
                            break;
                        case SimEventType.Holed: RingAt(e.Position, 0.3f, 0.38f, new Color(1f, 1f, 1f, alpha)); break;
                    }
                }
            }

            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0, false);
            mesh.RecalculateBounds();
            mesh.bounds = new Bounds(ball, Vector3.one * 200f);
            mr.enabled = true;
        }

        static float Flat(Vector3 v) { return Mathf.Sqrt(v.x * v.x + v.z * v.z); }

        float GroundY(CourseModel course, Vector2 p, float fallback)
        {
            var g = course.Ground(p);
            return g.IsVoid ? fallback : g.Height;
        }

        void Disc(CourseModel course, Vector2 p, float r, Color c)
        {
            DiscAt(new Vector3(p.x, GroundY(course, p, 0f), p.y), r, c);
        }

        void DiscAt(Vector3 p, float r, Color c)
        {
            int b = verts.Count;
            p.y += 0.03f;
            verts.Add(p + new Vector3(-r, 0, -r));
            verts.Add(p + new Vector3(-r, 0, r));
            verts.Add(p + new Vector3(r, 0, r));
            verts.Add(p + new Vector3(r, 0, -r));
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(1, 0));
            for (int i = 0; i < 4; i++) cols.Add(c);
            tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
            tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
        }

        void Solid(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color col)
        {
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            for (int k = 0; k < 4; k++)
            {
                uvs.Add(new Vector2(0.5f, 0.5f));
                cols.Add(col);
            }
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }

        void ArcQuad(CourseModel course, Vector3 centre, float r0, float r1, float a0, float a1, Color c)
        {
            const int steps = 3;
            for (int s = 0; s < steps; s++)
            {
                float t0 = Mathf.Lerp(a0, a1, s / (float)steps), t1 = Mathf.Lerp(a0, a1, (s + 1) / (float)steps);
                Vector3 d0 = new Vector3(Mathf.Cos(t0), 0, Mathf.Sin(t0)), d1 = new Vector3(Mathf.Cos(t1), 0, Mathf.Sin(t1));
                Vector3 lift = Vector3.up * 0.035f;
                Solid(centre + d0 * r1 + lift, centre + d0 * r0 + lift, centre + d1 * r0 + lift, centre + d1 * r1 + lift, c);
            }
        }

        void Diamond(Vector3 p, float r, Color c)
        {
            p.y += 0.04f;
            Solid(p + new Vector3(0, 0, -r), p + new Vector3(-r, 0, 0), p + new Vector3(0, 0, r), p + new Vector3(r, 0, 0), c);
        }

        void RingAt(Vector3 p, float r0, float r1, Color c)
        {
            const int n = 16;
            for (int i = 0; i < n; i++)
            {
                float a0 = i / (float)n * Mathf.PI * 2f, a1 = (i + 1) / (float)n * Mathf.PI * 2f;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                Vector3 q = p + Vector3.up * 0.04f;
                Solid(q + d0 * r1, q + d0 * r0, q + d1 * r0, q + d1 * r1, c);
            }
        }

        void Cross(Vector3 p, float r, Color c)
        {
            p.y += 0.05f;
            float w = r * 0.28f;
            Vector3 a = new Vector3(1, 0, 1).normalized, b = new Vector3(1, 0, -1).normalized;
            Solid(p - a * r - b * w, p - a * r + b * w, p + a * r + b * w, p + a * r - b * w, c);
            Solid(p - b * r + a * w, p - b * r - a * w, p + b * r - a * w, p + b * r + a * w, c);
        }
    }
}
