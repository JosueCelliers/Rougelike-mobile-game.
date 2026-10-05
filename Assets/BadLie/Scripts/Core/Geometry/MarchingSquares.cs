using System;
using System.Collections.Generic;
using UnityEngine;

namespace BadLie.Geometry
{
    /// <summary>Scalar field sampled on a regular grid aligned to a global lattice.</summary>
    public sealed class FieldGrid
    {
        public readonly Vector2 Origin;
        public readonly float Cell;
        public readonly int NX, NY;
        public readonly float[] Values;

        public FieldGrid(Rect area, float cell, Func<Vector2, float> field, int pad = 2)
        {
            Cell = cell;
            int x0 = Mathf.FloorToInt(area.xMin / cell) - pad;
            int y0 = Mathf.FloorToInt(area.yMin / cell) - pad;
            int x1 = Mathf.CeilToInt(area.xMax / cell) + pad;
            int y1 = Mathf.CeilToInt(area.yMax / cell) + pad;
            Origin = new Vector2(x0 * cell, y0 * cell);
            NX = x1 - x0 + 1;
            NY = y1 - y0 + 1;
            Values = new float[NX * NY];
            for (int j = 0; j < NY; j++)
            {
                for (int i = 0; i < NX; i++)
                {
                    float v;
                    if (i == 0 || j == 0 || i == NX - 1 || j == NY - 1) v = cell; // force closure
                    else v = field(Point(i, j));
                    if (v == 0f) v = 1e-6f;
                    Values[j * NX + i] = v;
                }
            }
        }

        public Vector2 Point(int i, int j) { return new Vector2(Origin.x + i * Cell, Origin.y + j * Cell); }
        public float this[int i, int j] { get { return Values[j * NX + i]; } }
    }

    /// <summary>
    /// Marching squares over a FieldGrid. Inside is value &lt;= 0. Produces closed contours
    /// (counter-clockwise around inside regions) and convex cell polygons for meshing.
    /// </summary>
    public static class MarchingSquares
    {
        // Segment table: per case, pairs of (fromEdge, toEdge), inside on the left.
        // Edges: 0 bottom (c0-c1), 1 right (c1-c2), 2 top (c2-c3), 3 left (c3-c0).
        static readonly int[][] SegTable =
        {
            new int[0],            // 0
            new[] { 0, 3 },        // 1
            new[] { 1, 0 },        // 2
            new[] { 1, 3 },        // 3
            new[] { 2, 1 },        // 4
            null,                  // 5 saddle
            new[] { 2, 0 },        // 6
            new[] { 2, 3 },        // 7
            new[] { 3, 2 },        // 8
            new[] { 0, 2 },        // 9
            null,                  // 10 saddle
            new[] { 1, 2 },        // 11
            new[] { 3, 1 },        // 12
            new[] { 0, 1 },        // 13
            new[] { 3, 0 },        // 14
            new int[0],            // 15
        };

        public static int EdgeId(FieldGrid g, int i, int j, int edge)
        {
            switch (edge)
            {
                case 0: return 2 * (j * g.NX + i);           // horizontal edge at row j
                case 1: return 2 * (j * g.NX + i + 1) + 1;   // vertical edge at column i+1
                case 2: return 2 * ((j + 1) * g.NX + i);     // horizontal edge at row j+1
                default: return 2 * (j * g.NX + i) + 1;      // vertical edge at column i
            }
        }

        public static Vector2 EdgePoint(FieldGrid g, int i, int j, int edge)
        {
            int ai, aj, bi, bj;
            switch (edge)
            {
                case 0: ai = i; aj = j; bi = i + 1; bj = j; break;
                case 1: ai = i + 1; aj = j; bi = i + 1; bj = j + 1; break;
                case 2: ai = i + 1; aj = j + 1; bi = i; bj = j + 1; break;
                default: ai = i; aj = j + 1; bi = i; bj = j; break;
            }
            float va = g[ai, aj], vb = g[bi, bj];
            float t = va / (va - vb);
            t = Mathf.Clamp(t, 0.001f, 0.999f);
            return Vector2.Lerp(g.Point(ai, aj), g.Point(bi, bj), t);
        }

        public static int CaseIndex(FieldGrid g, int i, int j)
        {
            int c = 0;
            if (g[i, j] <= 0f) c |= 1;
            if (g[i + 1, j] <= 0f) c |= 2;
            if (g[i + 1, j + 1] <= 0f) c |= 4;
            if (g[i, j + 1] <= 0f) c |= 8;
            return c;
        }

        static bool CentreInside(FieldGrid g, int i, int j)
        {
            return (g[i, j] + g[i + 1, j] + g[i + 1, j + 1] + g[i, j + 1]) * 0.25f <= 0f;
        }

        static void CellSegments(FieldGrid g, int i, int j, int c, List<int> outPairs)
        {
            outPairs.Clear();
            if (c == 5)
            {
                if (CentreInside(g, i, j)) { outPairs.Add(0); outPairs.Add(1); outPairs.Add(2); outPairs.Add(3); }
                else { outPairs.Add(0); outPairs.Add(3); outPairs.Add(2); outPairs.Add(1); }
                return;
            }
            if (c == 10)
            {
                if (CentreInside(g, i, j)) { outPairs.Add(3); outPairs.Add(0); outPairs.Add(1); outPairs.Add(2); }
                else { outPairs.Add(1); outPairs.Add(0); outPairs.Add(3); outPairs.Add(2); }
                return;
            }
            int[] s = SegTable[c];
            for (int k = 0; k < s.Length; k++) outPairs.Add(s[k]);
        }

        /// <summary>Closed contour loops, counter-clockwise around inside regions.</summary>
        public static List<List<Vector2>> Contours(FieldGrid g)
        {
            var next = new Dictionary<int, int>();
            var points = new Dictionary<int, Vector2>();
            var pairs = new List<int>(4);
            for (int j = 0; j < g.NY - 1; j++)
            {
                for (int i = 0; i < g.NX - 1; i++)
                {
                    int c = CaseIndex(g, i, j);
                    if (c == 0 || c == 15) continue;
                    CellSegments(g, i, j, c, pairs);
                    for (int k = 0; k < pairs.Count; k += 2)
                    {
                        int ea = EdgeId(g, i, j, pairs[k]);
                        int eb = EdgeId(g, i, j, pairs[k + 1]);
                        if (!points.ContainsKey(ea)) points[ea] = EdgePoint(g, i, j, pairs[k]);
                        if (!points.ContainsKey(eb)) points[eb] = EdgePoint(g, i, j, pairs[k + 1]);
                        next[ea] = eb;
                    }
                }
            }
            var loops = new List<List<Vector2>>();
            var visited = new HashSet<int>();
            foreach (var kv in next)
            {
                if (visited.Contains(kv.Key)) continue;
                var loop = new List<Vector2>();
                int cur = kv.Key;
                int guard = 0;
                while (!visited.Contains(cur) && guard++ < 1000000)
                {
                    visited.Add(cur);
                    loop.Add(points[cur]);
                    int nxt;
                    if (!next.TryGetValue(cur, out nxt)) break;
                    cur = nxt;
                }
                if (loop.Count >= 3) loops.Add(loop);
            }
            return loops;
        }

        /// <summary>
        /// Emits the inside part of every cell as a convex polygon. Vertices are identified by
        /// keys so meshes can share them: corner keys are non-negative, edge keys negative.
        /// </summary>
        public static void CellPolygons(FieldGrid g, Action<List<int>, List<Vector2>> emit)
        {
            var keys = new List<int>(8);
            var pts = new List<Vector2>(8);
            for (int j = 0; j < g.NY - 1; j++)
            {
                for (int i = 0; i < g.NX - 1; i++)
                {
                    int c = CaseIndex(g, i, j);
                    if (c == 0) continue;
                    keys.Clear();
                    pts.Clear();
                    bool saddleSplit = (c == 5 || c == 10) && !CentreInside(g, i, j);
                    if (saddleSplit)
                    {
                        if (c == 5)
                        {
                            EmitTri(g, i, j, Corner(g, i, j, 0), 0, 3, keys, pts, emit);
                            EmitTri(g, i, j, Corner(g, i, j, 2), 2, 1, keys, pts, emit);
                        }
                        else
                        {
                            EmitTri(g, i, j, Corner(g, i, j, 1), 1, 0, keys, pts, emit);
                            EmitTri(g, i, j, Corner(g, i, j, 3), 3, 2, keys, pts, emit);
                        }
                        continue;
                    }
                    // Walk the cell boundary CCW: c0, e0, c1, e1, c2, e2, c3, e3.
                    for (int k = 0; k < 4; k++)
                    {
                        bool inA = (c & (1 << k)) != 0;
                        bool inB = (c & (1 << ((k + 1) & 3))) != 0;
                        if (inA)
                        {
                            int ci, cj;
                            CornerIJ(i, j, k, out ci, out cj);
                            keys.Add(cj * g.NX + ci);
                            pts.Add(g.Point(ci, cj));
                        }
                        if (inA != inB)
                        {
                            keys.Add(-1 - EdgeId(g, i, j, k));
                            pts.Add(EdgePoint(g, i, j, k));
                        }
                    }
                    if (keys.Count >= 3) emit(keys, pts);
                }
            }
        }

        static int Corner(FieldGrid g, int i, int j, int k) { return k; }

        static void CornerIJ(int i, int j, int k, out int ci, out int cj)
        {
            switch (k)
            {
                case 0: ci = i; cj = j; break;
                case 1: ci = i + 1; cj = j; break;
                case 2: ci = i + 1; cj = j + 1; break;
                default: ci = i; cj = j + 1; break;
            }
        }

        static void EmitTri(FieldGrid g, int i, int j, int corner, int edgeOut, int edgeIn, List<int> keys, List<Vector2> pts, Action<List<int>, List<Vector2>> emit)
        {
            keys.Clear();
            pts.Clear();
            int ci, cj;
            CornerIJ(i, j, corner, out ci, out cj);
            keys.Add(cj * g.NX + ci);
            pts.Add(g.Point(ci, cj));
            keys.Add(-1 - EdgeId(g, i, j, edgeOut));
            pts.Add(EdgePoint(g, i, j, edgeOut));
            keys.Add(-1 - EdgeId(g, i, j, edgeIn));
            pts.Add(EdgePoint(g, i, j, edgeIn));
            emit(keys, pts);
        }
    }
}
