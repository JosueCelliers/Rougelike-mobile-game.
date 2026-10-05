using System.Collections.Generic;
using BadLie.Core;
using UnityEngine;

namespace BadLie.Geometry
{
    /// <summary>One kit piece split by material family.</summary>
    public sealed class KitMesh
    {
        public readonly MeshData Lit = new MeshData();
        public readonly MeshData Foliage = new MeshData();
        public readonly MeshData Glow = new MeshData();
        public readonly MeshData GlowAmber = new MeshData();

        public void Append(KitMesh other, Matrix4x4 tr)
        {
            Lit.Append(other.Lit, tr);
            Foliage.Append(other.Foliage, tr);
            Glow.Append(other.Glow, tr);
            GlowAmber.Append(other.GlowAmber, tr);
        }

        public int VertexCount { get { return Lit.Count + Foliage.Count + Glow.Count + GlowAmber.Count; } }
    }

    /// <summary>Low-level shape builders for the procedural environment kit.</summary>
    public static class KitPrims
    {
        public static Matrix4x4 TRS(Vector3 p, Quaternion r, Vector3 s) { return Matrix4x4.TRS(p, r, s); }

        /// <summary>
        /// Box with chamfered edges (stone blocks, slabs). Pivot at the bottom centre.
        /// aoBottom darkens the lower vertices.
        /// </summary>
        public static void BevelBox(MeshData m, Matrix4x4 tr, Vector3 size, float bevel, Color c, float aoTop = 1f, float aoBottom = 0.7f)
        {
            Vector3 h = size * 0.5f;
            bevel = Mathf.Min(bevel, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.9f);
            float[] gx = { -h.x, -h.x + bevel, h.x - bevel, h.x };
            float[] gy = { 0f, bevel, size.y - bevel, size.y };
            float[] gz = { -h.z, -h.z + bevel, h.z - bevel, h.z };
            Matrix4x4 nm = tr.inverse.transpose;
            // Six faces; each face is a 4x4 grid. Positions are pulled inwards on the bevel rows.
            for (int face = 0; face < 6; face++)
            {
                int axis = face >> 1;
                float sign = (face & 1) == 0 ? -1f : 1f;
                var ids = new int[16];
                for (int j = 0; j < 4; j++)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 p;
                        int a = i, b = j;
                        switch (axis)
                        {
                            case 0: p = new Vector3(sign < 0 ? gx[0] : gx[3], gy[b], gz[a]); break;
                            case 1: p = new Vector3(gx[a], sign < 0 ? gy[0] : gy[3], gz[b]); break;
                            default: p = new Vector3(gx[a], gy[b], sign < 0 ? gz[0] : gz[3]); break;
                        }
                        // Pull border rows in so edges become chamfers.
                        Vector3 inner = new Vector3(
                            Mathf.Clamp(p.x, gx[1], gx[2]),
                            Mathf.Clamp(p.y, gy[1], gy[2]),
                            Mathf.Clamp(p.z, gz[1], gz[2]));
                        Vector3 dir = p - inner;
                        Vector3 n = dir.sqrMagnitude > 1e-10f ? dir.normalized : FaceNormal(axis, sign);
                        bool edgeA = (a == 0 || a == 3), edgeB = (b == 0 || b == 3);
                        if (edgeA || edgeB) p = inner + n * bevel;
                        float ao = Mathf.Lerp(aoBottom, aoTop, Mathf.Clamp01(p.y / Mathf.Max(size.y, 0.001f)));
                        ids[j * 4 + i] = m.Add(tr.MultiplyPoint3x4(p), nm.MultiplyVector(n).normalized, c, new Vector4(ao, 0, 0, 0));
                    }
                }
                bool flip = (axis == 0 && sign > 0) || (axis == 1 && sign > 0) || (axis == 2 && sign < 0);
                if (tr.determinant < 0f) flip = !flip;
                for (int j = 0; j < 3; j++)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int a = ids[j * 4 + i], b = ids[j * 4 + i + 1], cc = ids[(j + 1) * 4 + i + 1], d = ids[(j + 1) * 4 + i];
                        if (flip) m.Quad(a, d, cc, b);
                        else m.Quad(a, b, cc, d);
                    }
                }
            }
        }

        static Vector3 FaceNormal(int axis, float sign)
        {
            switch (axis)
            {
                case 0: return new Vector3(sign, 0, 0);
                case 1: return new Vector3(0, sign, 0);
                default: return new Vector3(0, 0, sign);
            }
        }

        /// <summary>Surface of revolution around +Y. profile = (radius, height) bottom to top.</summary>
        public static void Lathe(MeshData m, Matrix4x4 tr, IList<Vector2> profile, int sides, Color c, float aoBottom = 0.6f, float aoTop = 1f, bool capTop = true, float glowMask = 0f)
        {
            Matrix4x4 nm = tr.inverse.transpose;
            int rows = profile.Count;
            float yMin = profile[0].y, yMax = profile[rows - 1].y;
            var ids = new int[rows, sides + 1];
            for (int r = 0; r < rows; r++)
            {
                // Profile normal from neighbouring points.
                Vector2 prev = profile[Mathf.Max(r - 1, 0)], next = profile[Mathf.Min(r + 1, rows - 1)];
                Vector2 t = (next - prev);
                if (t.sqrMagnitude < 1e-10f) t = Vector2.up;
                t.Normalize();
                Vector2 pn = new Vector2(t.y, -t.x); // outward in (r,y)
                float ao = Mathf.Lerp(aoBottom, aoTop, Mathf.InverseLerp(yMin, yMax, profile[r].y));
                for (int s = 0; s <= sides; s++)
                {
                    float a = s * Mathf.PI * 2f / sides;
                    float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                    Vector3 p = new Vector3(ca * profile[r].x, profile[r].y, sa * profile[r].x);
                    Vector3 n = new Vector3(ca * pn.x, pn.y, sa * pn.x);
                    ids[r, s] = m.Add(tr.MultiplyPoint3x4(p), nm.MultiplyVector(n).normalized, c, new Vector4(ao, glowMask, 0, 0));
                }
            }
            bool flip = tr.determinant < 0f;
            for (int r = 0; r < rows - 1; r++)
            {
                for (int s = 0; s < sides; s++)
                {
                    int a = ids[r, s], b = ids[r, s + 1], cc = ids[r + 1, s + 1], d = ids[r + 1, s];
                    if (flip) m.Quad(a, b, cc, d);
                    else m.Quad(a, d, cc, b);
                }
            }
            if (capTop && profile[rows - 1].x > 0.001f)
            {
                Vector3 top = tr.MultiplyPoint3x4(new Vector3(0, yMax, 0));
                int centre = m.Add(top, nm.MultiplyVector(Vector3.up).normalized, c, new Vector4(aoTop, glowMask, 0, 0));
                var ring = new int[sides + 1];
                for (int s = 0; s <= sides; s++)
                {
                    ring[s] = m.Add(m.Positions[ids[rows - 1, s]], nm.MultiplyVector(Vector3.up).normalized, c, new Vector4(aoTop, glowMask, 0, 0));
                }
                for (int s = 0; s < sides; s++)
                {
                    if (flip) m.Tri(centre, ring[s], ring[s + 1]);
                    else m.Tri(centre, ring[s + 1], ring[s]);
                }
            }
        }

        /// <summary>Tube along a 3D path with per-point radius (roots, branches, poles).</summary>
        public static void Tube(MeshData m, IList<Vector3> path, IList<float> radius, int sides, Color c, float aoStart = 0.6f, float aoEnd = 1f, float sway = 0f)
        {
            int n = path.Count;
            if (n < 2) return;
            var ids = new int[n, sides + 1];
            Vector3 prevNormal = Vector3.zero;
            for (int i = 0; i < n; i++)
            {
                Vector3 t = (path[Mathf.Min(i + 1, n - 1)] - path[Mathf.Max(i - 1, 0)]).normalized;
                Vector3 up = Mathf.Abs(Vector3.Dot(t, Vector3.up)) > 0.95f ? Vector3.right : Vector3.up;
                Vector3 nx = prevNormal == Vector3.zero ? Vector3.Cross(up, t).normalized : Vector3.ProjectOnPlane(prevNormal, t).normalized;
                if (nx.sqrMagnitude < 1e-6f) nx = Vector3.Cross(up, t).normalized;
                prevNormal = nx;
                Vector3 ny = Vector3.Cross(t, nx);
                float ao = Mathf.Lerp(aoStart, aoEnd, i / (float)(n - 1));
                float sw = sway * (i / (float)(n - 1));
                for (int s = 0; s <= sides; s++)
                {
                    float a = s * Mathf.PI * 2f / sides;
                    Vector3 dir = nx * Mathf.Cos(a) + ny * Mathf.Sin(a);
                    ids[i, s] = m.Add(path[i] + dir * radius[i], dir, c, new Vector4(ao, sw, 0, 0));
                }
            }
            for (int i = 0; i < n - 1; i++)
            {
                for (int s = 0; s < sides; s++)
                {
                    m.Quad(ids[i, s], ids[i, s + 1], ids[i + 1, s + 1], ids[i + 1, s]);
                }
            }
        }

        /// <summary>Unit icosphere (subdiv 0..3) with optional noise displacement.</summary>
        public static void Sphere(MeshData m, Matrix4x4 tr, int subdiv, Color c, float aoBottom, float aoTop, float noise = 0f, int seed = 0, float glowMask = 0f, float sway = 0f)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            Icosphere(subdiv, verts, tris);
            Matrix4x4 nm = tr.inverse.transpose;
            int baseIndex = m.Count;
            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 v = verts[i];
                float k = 1f;
                if (noise > 0f) k += (Noise.Fbm3(v * 2.1f + new Vector3(seed * 3.1f, seed * 1.7f, 0), 3, seed) - 0.5f) * 2f * noise;
                float ao = Mathf.Lerp(aoBottom, aoTop, v.y * 0.5f + 0.5f);
                m.Add(tr.MultiplyPoint3x4(v * k), nm.MultiplyVector(v).normalized, c, new Vector4(ao, sway, 0, 0));
            }
            bool flip = tr.determinant < 0f;
            for (int i = 0; i < tris.Count; i += 3)
            {
                if (flip) m.Tri(baseIndex + tris[i], baseIndex + tris[i + 2], baseIndex + tris[i + 1]);
                else m.Tri(baseIndex + tris[i], baseIndex + tris[i + 1], baseIndex + tris[i + 2]);
            }
        }

        static readonly Dictionary<int, KeyValuePair<List<Vector3>, List<int>>> icoCache = new Dictionary<int, KeyValuePair<List<Vector3>, List<int>>>();

        public static void Icosphere(int subdiv, List<Vector3> outVerts, List<int> outTris)
        {
            KeyValuePair<List<Vector3>, List<int>> cached;
            if (!icoCache.TryGetValue(subdiv, out cached))
            {
                var v = new List<Vector3>();
                var t = new List<int>();
                float g = (1f + Mathf.Sqrt(5f)) * 0.5f;
                Vector3[] bv =
                {
                    new Vector3(-1, g, 0), new Vector3(1, g, 0), new Vector3(-1, -g, 0), new Vector3(1, -g, 0),
                    new Vector3(0, -1, g), new Vector3(0, 1, g), new Vector3(0, -1, -g), new Vector3(0, 1, -g),
                    new Vector3(g, 0, -1), new Vector3(g, 0, 1), new Vector3(-g, 0, -1), new Vector3(-g, 0, 1),
                };
                foreach (var p in bv) v.Add(p.normalized);
                int[] bt =
                {
                    0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                    3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
                };
                t.AddRange(bt);
                for (int s = 0; s < subdiv; s++)
                {
                    var mid = new Dictionary<long, int>();
                    var nt = new List<int>();
                    for (int i = 0; i < t.Count; i += 3)
                    {
                        int a = t[i], b = t[i + 1], c = t[i + 2];
                        int ab = Mid(v, mid, a, b), bc = Mid(v, mid, b, c), ca = Mid(v, mid, c, a);
                        nt.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                    }
                    t = nt;
                }
                // The base winding already faces outwards (cross(b-a, c-a) points out).
                cached = new KeyValuePair<List<Vector3>, List<int>>(v, t);
                icoCache[subdiv] = cached;
            }
            outVerts.AddRange(cached.Key);
            outTris.AddRange(cached.Value);
        }

        static int Mid(List<Vector3> v, Dictionary<long, int> cache, int a, int b)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            int idx;
            if (cache.TryGetValue(key, out idx)) return idx;
            v.Add(((v[a] + v[b]) * 0.5f).normalized);
            idx = v.Count - 1;
            cache[key] = idx;
            return idx;
        }

        /// <summary>
        /// One pointed, folded leaf flake. pos = attachment point, outward = surface normal,
        /// tipDir = direction the tip points. Every vertex takes the mass normal so a clump
        /// shades as one sculpted form; only the flake base is darkened, which leaves a soft
        /// scale rhythm inside the mass while the silhouette stays serrated.
        /// </summary>
        public static void Flake(MeshData m, Vector3 pos, Vector3 outward, Vector3 tipDir, Vector3 massNormal, float length, float width, Color c, float ao, float sway, float rnd)
        {
            Vector3 f = Vector3.ProjectOnPlane(tipDir, outward).normalized;
            if (f.sqrMagnitude < 1e-6f) f = Vector3.Cross(outward, Vector3.right).normalized;
            Vector3 side = Vector3.Cross(outward, f).normalized;
            Vector3 lift = outward * (length * 0.3f);
            Vector3 p0 = pos - f * (length * 0.18f);
            Vector3 p1 = pos - side * (width * 0.5f) + f * (length * 0.28f) + outward * (length * 0.05f);
            Vector3 p2 = pos + f * (length * 0.36f) + lift * 0.5f;
            Vector3 p3 = pos + side * (width * 0.5f) + f * (length * 0.28f) + outward * (length * 0.05f);
            Vector3 p4 = pos + f * length + outward * (length * 0.1f);
            Vector3 n = massNormal.sqrMagnitude > 1e-6f ? massNormal.normalized : outward;
            Vector4 uvBase = new Vector4(ao * 0.78f, sway * 0.8f, rnd, 0f);
            Vector4 uvSide = new Vector4(ao * 0.92f, sway, rnd, 0f);
            Vector4 uvTip = new Vector4(ao, sway * 1.25f, rnd, 0f);
            int i0 = m.Add(p0, n, c, uvBase);
            int i1 = m.Add(p1, n, c, uvSide);
            int i2 = m.Add(p2, n, c, uvTip);
            int i3 = m.Add(p3, n, c, uvSide);
            int i4 = m.Add(p4, n, c, uvTip);
            m.Tri(i0, i1, i2);
            m.Tri(i0, i2, i3);
            m.Tri(i1, i4, i2);
            m.Tri(i2, i4, i3);
        }

        /// <summary>Extrudes a closed 2D profile (x across, y up) along a 3D path.</summary>
        public static void Sweep(MeshData m, IList<Vector3> path, IList<Vector2> profile, Color c, float ao = 0.9f, bool closedPath = false)
        {
            int n = path.Count;
            int k = profile.Count;
            var ids = new int[n, k + 1];
            for (int i = 0; i < n; i++)
            {
                Vector3 prev = path[closedPath ? (i - 1 + n) % n : Mathf.Max(i - 1, 0)];
                Vector3 next = path[closedPath ? (i + 1) % n : Mathf.Min(i + 1, n - 1)];
                Vector3 t = next - prev;
                t.y = 0f;
                if (t.sqrMagnitude < 1e-8f) t = Vector3.forward;
                t.Normalize();
                Vector3 across = Vector3.Cross(Vector3.up, t);
                for (int j = 0; j <= k; j++)
                {
                    Vector2 a = profile[j % k];
                    Vector2 b = profile[(j + 1) % k];
                    Vector2 pa = profile[(j - 1 + k) % k];
                    Vector2 e = (b - pa);
                    Vector2 pn = new Vector2(e.y, -e.x).normalized;
                    Vector3 pos = path[i] + across * a.x + Vector3.up * a.y;
                    Vector3 nrm = (across * pn.x + Vector3.up * pn.y).normalized;
                    ids[i, j] = m.Add(pos, nrm, c, new Vector4(ao, 0, 0, 0));
                }
            }
            int segs = closedPath ? n : n - 1;
            for (int i = 0; i < segs; i++)
            {
                int i1 = (i + 1) % n;
                for (int j = 0; j < k; j++)
                {
                    m.Quad(ids[i, j], ids[i, j + 1], ids[i1, j + 1], ids[i1, j]);
                }
            }
        }
    }
}
