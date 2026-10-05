using System.Collections.Generic;
using BadLie.Core;
using BadLie.Course;
using UnityEngine;

namespace BadLie.Geometry
{
    /// <summary>A circular or elongated footprint that darkens the ground around it.</summary>
    public struct AOCaster
    {
        public Vector2 A, B;
        public float Radius;
        public float Strength;
        public float Falloff;
    }

    /// <summary>
    /// Builds the ground meshes from a CourseModel: pad tops (marching squares on the same
    /// fields the physics uses), thick slab faces down to whatever lies below, water sheets,
    /// and the cup cut into the green.
    /// </summary>
    public sealed class TerrainMesher
    {
        readonly CourseModel course;
        readonly List<AOCaster> casters = new List<AOCaster>();
        readonly List<Pad> waterPads = new List<Pad>();

        public TerrainMesher(CourseModel course, IEnumerable<AOCaster> extraCasters)
        {
            this.course = course;
            foreach (var p in course.Pads) if (p.Water) waterPads.Add(p);
            // Walls, posts and blocks cast contact shade.
            foreach (var s in course.Segments)
            {
                if (s.Material == WallMaterial.Ledge) continue;
                float h = Mathf.Clamp(s.YTop - course.HeightAt((s.A + s.B) * 0.5f), 0.1f, 2f);
                casters.Add(new AOCaster { A = s.A, B = s.B, Radius = s.Radius, Strength = Mathf.Lerp(0.25f, 0.55f, h / 1.2f), Falloff = 0.18f + 0.22f * h });
            }
            foreach (var c in course.Circles)
            {
                float h = Mathf.Clamp(c.YTop - course.HeightAt(c.C), 0.1f, 2f);
                casters.Add(new AOCaster { A = c.C, B = c.C, Radius = c.Radius, Strength = 0.5f, Falloff = 0.25f + 0.15f * h });
            }
            if (extraCasters != null) casters.AddRange(extraCasters);
        }

        // -------------------------------------------------------------------- tops

        public MeshData BuildPadTop(Pad pad, bool cutCup)
        {
            var m = new MeshData();
            if (pad.Water) return m;
            FieldGrid field = pad.Field;
            Vector2 cup = course.Cup;
            float holeR = course.Tuning.CupRadius + 0.24f;
            bool hasCup = cutCup && pad.Shape.Distance(cup) < 0f && course.TopPadIndex(cup) == pad.Index;
            if (hasCup)
            {
                field = new FieldGrid(pad.Shape.Bounds, CourseModel.GridCell, p =>
                {
                    float d = pad.VisibleDistance(p);
                    float hole = holeR - (p - cup).magnitude;
                    return Mathf.Max(d, hole);
                });
            }
            var map = new Dictionary<int, int>();
            var idx = new List<int>(8);
            MarchingSquares.CellPolygons(field, (keys, pts) =>
            {
                idx.Clear();
                for (int k = 0; k < keys.Count; k++)
                {
                    int vi;
                    if (!map.TryGetValue(keys[k], out vi))
                    {
                        vi = AddTopVertex(m, pad, pts[k]);
                        map[keys[k]] = vi;
                    }
                    idx.Add(vi);
                }
                // Polygons are CCW in (x,z); reverse for Unity's clockwise front faces.
                for (int k = 1; k < idx.Count - 1; k++) m.Tri(idx[0], idx[k + 1], idx[k]);
            });
            if (hasCup) StitchCup(m, pad, field, holeR);
            return m;
        }

        int AddTopVertex(MeshData m, Pad pad, Vector2 p)
        {
            Vector2 g;
            float h = pad.HeightAt(p, out g);
            Vector3 n = new Vector3(-g.x, 1f, -g.y).normalized;
            float cell = CourseModel.GridCell;
            Color cover = new Color(
                Cov(pad.CoverageDistance(SurfaceType.Fairway, p), cell),
                Cov(pad.CoverageDistance(SurfaceType.Green, p), cell),
                Cov(pad.CoverageDistance(SurfaceType.Sand, p), cell),
                Cov(pad.CoverageDistance(SurfaceType.Stone, p), cell));
            float runnel = Cov(pad.CoverageDistance(SurfaceType.Runnel, p), cell);
            float greenEdge = Mathf.Clamp(pad.CoverageDistance(SurfaceType.Green, p), -1f, 1f);
            float edge = LedgeDistance(pad, p, h);
            float ao = GroundAO(p, h);
            Vector2 flow = Vector2.zero;
            ZoneDef zone;
            if (course.InZone(p, out zone) && zone.Kind == ZoneKind.Flow) flow = CourseModel.FlowDirection(zone, p);
            float wet = Wetness(p);
            return m.Add(new Vector3(p.x, h, p.y), n, cover,
                new Vector4(runnel, ao, greenEdge, edge),
                new Vector4(0f, 0f, 0f, 0f),
                new Vector4(flow.x, flow.y, wet, 0f));
        }

        static float Cov(float sd, float cell)
        {
            return Mathf.Clamp01(0.5f - sd / (2f * cell));
        }

        float LedgeDistance(Pad pad, Vector2 p, float h)
        {
            float vis = -pad.VisibleDistance(p);
            if (vis > 1f) return 1f;
            float best = 1f;
            foreach (var s in course.Segments)
            {
                if (s.Material != WallMaterial.Ledge || s.Owner != pad.Index) continue;
                float d = Geo2D.DistanceToSegment(p, s.A, s.B);
                if (d < best) best = d;
            }
            return best;
        }

        float GroundAO(Vector2 p, float h)
        {
            float ao = 1f;
            for (int i = 0; i < casters.Count; i++)
            {
                var c = casters[i];
                float d = Geo2D.DistanceToSegment(p, c.A, c.B) - c.Radius;
                if (d > c.Falloff * 4f) continue;
                d = Mathf.Max(0f, d);
                ao *= 1f - c.Strength * Mathf.Exp(-d / c.Falloff);
            }
            // Foot of a higher neighbouring slab.
            foreach (var s in course.Segments)
            {
                if (s.Material != WallMaterial.Ledge) continue;
                if (s.YTop < h + 0.12f) continue;
                if (Vector2.Dot(p - s.A, s.OuterNormal) < 0f) continue;
                float d = Geo2D.DistanceToSegment(p, s.A, s.B);
                if (d > 1.2f) continue;
                float k = Mathf.Clamp01((s.YTop - h) / 0.8f);
                ao *= 1f - 0.5f * k * Mathf.Exp(-d / 0.32f);
            }
            return Mathf.Clamp01(ao);
        }

        float Wetness(Vector2 p)
        {
            float best = 1f;
            foreach (var w in waterPads)
            {
                Rect b = w.Shape.Bounds;
                if (p.x < b.xMin - 1f || p.x > b.xMax + 1f || p.y < b.yMin - 1f || p.y > b.yMax + 1f) continue;
                float d = w.VisibleDistance(p);
                if (d < best) best = d;
            }
            return 1f - Mathf.Clamp01(best / 0.55f);
        }

        // -------------------------------------------------------------------- cup

        void StitchCup(MeshData m, Pad pad, FieldGrid field, float holeR)
        {
            Vector2 cup = course.Cup;
            List<Vector2> loop = null;
            foreach (var l in MarchingSquares.Contours(field))
            {
                Vector2 c = Vector2.zero;
                foreach (var q in l) c += q;
                c /= l.Count;
                if ((c - cup).magnitude < 0.25f) { loop = l; break; }
            }
            if (loop == null) return;
            float rim = course.Tuning.CupRadius;
            const int N = 40;
            var outer = new List<KeyValuePair<float, int>>();
            foreach (var q in loop)
            {
                float a = Mathf.Atan2(q.y - cup.y, q.x - cup.x);
                outer.Add(new KeyValuePair<float, int>(a, AddTopVertex(m, pad, q)));
            }
            outer.Sort((a, b) => a.Key.CompareTo(b.Key));
            var inner = new List<KeyValuePair<float, int>>();
            for (int i = 0; i < N; i++)
            {
                float a = -Mathf.PI + (i + 0.5f) * Mathf.PI * 2f / N;
                Vector2 q = cup + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rim;
                inner.Add(new KeyValuePair<float, int>(a, AddTopVertex(m, pad, q)));
            }
            int iO = 0, iI = 0;
            int nO = outer.Count, nI = inner.Count;
            while (iO < nO || iI < nI)
            {
                var o0 = outer[iO % nO];
                var i0 = inner[iI % nI];
                float oNext = outer[(iO + 1) % nO].Key + ((iO + 1) >= nO ? Mathf.PI * 2f : 0f);
                float iNext = inner[(iI + 1) % nI].Key + ((iI + 1) >= nI ? Mathf.PI * 2f : 0f);
                bool advanceOuter = iI >= nI || (iO < nO && oNext <= iNext);
                if (advanceOuter)
                {
                    var o1 = outer[(iO + 1) % nO];
                    // Angles increase CCW; outer then inner gives clockwise from above.
                    m.Tri(o0.Value, i0.Value, o1.Value);
                    iO++;
                }
                else
                {
                    var i1 = inner[(iI + 1) % nI];
                    m.Tri(o0.Value, i0.Value, i1.Value);
                    iI++;
                }
            }
        }

        /// <summary>Cup liner (dark), a pale painted rim, for the Lit shader.</summary>
        public MeshData BuildCupLiner()
        {
            var m = new MeshData();
            Vector2 cup = course.Cup;
            float h = course.CupHeight;
            float r = course.Tuning.CupRadius;
            const int N = 40;
            const float depth = 0.38f;
            Color wall = Palette.Hex("#2a211f");
            Color rimC = Palette.Hex("#efe9dc");
            var top = new int[N + 1];
            var mid = new int[N + 1];
            var bot = new int[N + 1];
            for (int i = 0; i <= N; i++)
            {
                float a = i * Mathf.PI * 2f / N;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 c = new Vector3(cup.x, h, cup.y);
                top[i] = m.Add(c + dir * r + Vector3.down * 0.002f, -dir, rimC, new Vector4(1f, 0, 0, 0));
                mid[i] = m.Add(c + dir * r + Vector3.down * 0.05f, -dir, rimC, new Vector4(0.9f, 0, 0, 0));
                bot[i] = m.Add(c + dir * r * 0.96f + Vector3.down * depth, -dir, wall, new Vector4(0.15f, 0, 0, 0));
            }
            for (int i = 0; i < N; i++)
            {
                // Inner surface faces the axis: wind so normals point inward.
                m.Quad(top[i], mid[i], mid[i + 1], top[i + 1]);
                int m0 = m.Add(m.Positions[mid[i]], m.Normals[mid[i]], wall, new Vector4(0.6f, 0, 0, 0));
                int m1 = m.Add(m.Positions[mid[i + 1]], m.Normals[mid[i + 1]], wall, new Vector4(0.6f, 0, 0, 0));
                m.Quad(m0, bot[i], bot[i + 1], m1);
            }
            int centre = m.Add(new Vector3(cup.x, h - depth, cup.y), Vector3.up, wall, new Vector4(0.1f, 0, 0, 0));
            for (int i = 0; i < N; i++) m.Tri(centre, bot[i + 1], bot[i]);
            return m;
        }

        // -------------------------------------------------------------------- faces

        public MeshData BuildPadSides(Pad pad)
        {
            var m = new MeshData();
            if (pad.Water) return m;
            float style = (float)pad.Def.Edge;
            foreach (var loop in pad.Contours)
            {
                int n = loop.Points.Count;
                if (n < 3) continue;
                var col = new int[n, 3];
                var has = new bool[n];
                float u = 0f;
                for (int i = 0; i < n; i++)
                {
                    if (i > 0) u += (loop.Points[i] - loop.Points[i - 1]).magnitude;
                    float top = loop.Top[i];
                    float nb = loop.Neighbour[i];
                    float drop = top - nb;
                    if (drop < 0.012f) continue;
                    int np = loop.NeighbourPad[i];
                    bool water = np < 0 || course.Pads[np].Water;
                    float bottom = nb - (np < 0 ? 0.9f : (course.Pads[np].Water ? course.Pads[np].Def.WaterDepth : 0.03f));
                    Vector2 p = loop.Points[i];
                    Vector2 o = loop.Normals[i];
                    Vector3 o3 = new Vector3(o.x, 0f, o.y);
                    float wet = water ? 1f : 0f;
                    float lip = Mathf.Min(0.05f, drop * 0.4f);
                    Vector3 p0 = new Vector3(p.x, top, p.y);
                    Vector3 p1 = new Vector3(p.x + o.x * 0.03f, top - lip, p.y + o.y * 0.03f);
                    Vector3 p2 = new Vector3(p.x + o.x * 0.06f, bottom, p.y + o.y * 0.06f);
                    float aoTop = 0.95f, aoBot = Mathf.Lerp(0.95f, 0.45f, Mathf.Clamp01(drop / 0.9f));
                    col[i, 0] = m.Add(p0, (o3 * 0.55f + Vector3.up * 0.85f).normalized, Color.clear, new Vector4(0, aoTop, 1, 1), new Vector4(1f, u, 0f, style), new Vector4(0, 0, wet, 0));
                    col[i, 1] = m.Add(p1, (o3 + Vector3.up * 0.15f).normalized, Color.clear, new Vector4(0, aoTop, 1, 1), new Vector4(1f, u, lip, style), new Vector4(0, 0, wet, 0));
                    col[i, 2] = m.Add(p2, o3, Color.clear, new Vector4(0, aoBot, 1, 1), new Vector4(1f, u, top - bottom, style), new Vector4(0, 0, wet, 0));
                    has[i] = true;
                }
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    if (!has[i] || !has[j]) continue;
                    // Loop is CCW seen from above with outside on the right; faces look outward.
                    m.Quad(col[i, 0], col[j, 0], col[j, 1], col[i, 1]);
                    m.Quad(col[i, 1], col[j, 1], col[j, 2], col[i, 2]);
                }
            }
            return m;
        }

        // -------------------------------------------------------------------- water

        public MeshData BuildWater(Pad pad)
        {
            var m = new MeshData();
            if (!pad.Water) return m;
            float level = pad.HeightAt(pad.Shape.Bounds.center);
            var map = new Dictionary<int, int>();
            var idx = new List<int>(8);
            MarchingSquares.CellPolygons(pad.Field, (keys, pts) =>
            {
                idx.Clear();
                for (int k = 0; k < keys.Count; k++)
                {
                    int vi;
                    if (!map.TryGetValue(keys[k], out vi))
                    {
                        Vector2 p = pts[k];
                        float shore = Mathf.Max(0f, -pad.VisibleDistance(p));
                        float flowSpeed = 0f;
                        Vector2 flow = Vector2.zero;
                        ZoneDef zone;
                        if (course.InZone(p, out zone) && zone.Kind == ZoneKind.Flow)
                        {
                            flow = CourseModel.FlowDirection(zone, p);
                            flowSpeed = Mathf.Clamp01(zone.Strength / 2f);
                        }
                        vi = m.Add(new Vector3(p.x, level, p.y), Vector3.up, new Color(Mathf.Clamp01(shore / 1.4f), flowSpeed, 0, 1), new Vector4(flow.x, flow.y, shore, 0));
                        map[keys[k]] = vi;
                    }
                    idx.Add(vi);
                }
                for (int k = 1; k < idx.Count - 1; k++) m.Tri(idx[0], idx[k + 1], idx[k]);
            });
            return m;
        }

        /// <summary>The flooded lower estate: a wide water sheet under everything.</summary>
        public MeshData BuildLowerWater(float margin, float cell)
        {
            var m = new MeshData();
            Rect b = course.Bounds;
            float x0 = b.xMin - margin, x1 = b.xMax + margin, z0 = b.yMin - margin, z1 = b.yMax + margin;
            int nx = Mathf.CeilToInt((x1 - x0) / cell), nz = Mathf.CeilToInt((z1 - z0) / cell);
            float level = course.Def.LowerWaterLevel;
            var ids = new int[nx + 1, nz + 1];
            for (int j = 0; j <= nz; j++)
            {
                for (int i = 0; i <= nx; i++)
                {
                    Vector2 p = new Vector2(x0 + i * cell, z0 + j * cell);
                    float shore = float.MaxValue;
                    foreach (var pad in course.Pads)
                    {
                        if (pad.Water) continue;
                        shore = Mathf.Min(shore, pad.Shape.Distance(p));
                    }
                    shore = Mathf.Max(0f, shore);
                    ids[i, j] = m.Add(new Vector3(p.x, level, p.y), Vector3.up, new Color(Mathf.Clamp01(shore / 6f), 0, 0, 1), new Vector4(0, 0, shore, 0));
                }
            }
            for (int j = 0; j < nz; j++)
            {
                for (int i = 0; i < nx; i++)
                {
                    m.Quad(ids[i, j], ids[i, j + 1], ids[i + 1, j + 1], ids[i + 1, j]);
                }
            }
            return m;
        }
    }
}
