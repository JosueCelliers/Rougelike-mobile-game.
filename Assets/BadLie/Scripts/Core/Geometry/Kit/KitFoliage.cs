using System.Collections.Generic;
using BadLie.Core;
using UnityEngine;

namespace BadLie.Geometry
{
    /// <summary>
    /// Sculpted vegetation. Every mass is a dark body covered by overlapping pointed leaf
    /// flakes whose normals follow the overall mass, giving a serrated silhouette with soft,
    /// readable shading (the Death's Door topiary language).
    /// </summary>
    public static class KitFoliage
    {
        public static readonly Color HedgeLit = Palette.Hex("#6f7a33");
        public static readonly Color HedgeDeep = Palette.Hex("#3f4a24");
        public static readonly Color HedgeBody = Palette.Hex("#3a4520");
        public static readonly Color Amber = Palette.Hex("#e3a33b");
        public static readonly Color Saffron = Palette.Hex("#e8b94a");
        public static readonly Color Rust = Palette.Hex("#c6512f");
        public static readonly Color Crimson = Palette.Hex("#a93a2c");
        public static readonly Color Bark = Palette.Hex("#3b2a24");
        public static readonly Color Root = Palette.Hex("#4a3328");
        public static readonly Color WildA = Palette.Hex("#56622b");
        public static readonly Color WildB = Palette.Hex("#3d4a26");

        // ------------------------------------------------------------------ topiary cone

        public static KitMesh TopiaryCone(int seed, float height = 2.4f, float radius = 0.78f)
        {
            var k = new KitMesh();
            var rng = new DetRandom((ulong)(seed * 7919 + 13));
            var m = k.Foliage;
            // Body: a slightly smaller cone so gaps never show through.
            var prof = new List<Vector2>();
            for (int i = 0; i <= 8; i++)
            {
                float t = i / 8f;
                prof.Add(new Vector2(ConeRadius(t, radius) * 0.82f, t * height));
            }
            KitPrims.Lathe(m, Matrix4x4.identity, prof, 10, HedgeBody, 0.35f, 0.7f, true);
            float flakeLen = 0.24f, flakeW = 0.2f;
            float rowStep = flakeLen * 0.42f;
            int rows = Mathf.CeilToInt(height / rowStep);
            for (int r = 0; r < rows; r++)
            {
                float y = 0.08f + r * rowStep;
                float t = y / height;
                if (t > 0.97f) break;
                float R = ConeRadius(t, radius);
                float slope = radius / height;
                int count = Mathf.Max(3, Mathf.RoundToInt(2f * Mathf.PI * R / (flakeW * 0.72f)));
                float offset = (r % 2) * 0.5f + rng.Value() * 0.2f;
                for (int i = 0; i < count; i++)
                {
                    float a = (i + offset) / count * Mathf.PI * 2f;
                    Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    Vector3 outward = (dir + Vector3.up * slope * 0.9f).normalized;
                    Vector3 pos = dir * R + Vector3.up * y;
                    Vector3 tip = (Vector3.down + rng.Signed() * 0.25f * Vector3.Cross(Vector3.up, dir)).normalized;
                    float ao = Mathf.Lerp(0.5f, 1f, Mathf.Pow(t, 0.7f)) * (0.9f + 0.1f * rng.Value());
                    Color c = Color.Lerp(HedgeDeep, HedgeLit, 0.4f + 0.6f * t);
                    c = Palette.Jitter(c, 0.03f, ref rng);
                    float s = 0.9f + 0.25f * rng.Value();
                    KitPrims.Flake(m, pos, outward, tip, outward, flakeLen * s, flakeW * s, c, ao, 0.25f * t, rng.Value());
                }
            }
            // A crown tuft of upward flakes.
            Vector3 crown = Vector3.up * height * 0.97f;
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2f;
                Vector3 d = new Vector3(Mathf.Cos(a), 2.2f, Mathf.Sin(a)).normalized;
                KitPrims.Flake(m, crown - d * 0.05f, d, Vector3.up, Vector3.up, 0.2f, 0.14f, Palette.Jitter(HedgeLit, 0.06f, ref rng), 1f, 0.35f, rng.Value());
            }
            return k;
        }

        static float ConeRadius(float t, float radius)
        {
            // Slight belly near the base, sharp tip.
            float r = radius * (1f - t);
            r *= 1f + 0.12f * Mathf.Sin(t * Mathf.PI * 0.9f);
            return Mathf.Max(r, 0.02f);
        }

        // ------------------------------------------------------------------ hedge block

        /// <summary>Clipped hedge block, flat topped, notched edges. Pivot bottom-centre, long axis +X.</summary>
        public static KitMesh HedgeBlock(int seed, Vector3 size, float wildness = 0f, bool roots = false)
        {
            var k = new KitMesh();
            AddHedgeBlock(k.Foliage, Matrix4x4.identity, size, seed, wildness);
            if (roots) AddRoots(k.Foliage, size, seed);
            return k;
        }

        public static void AddHedgeBlock(MeshData m, Matrix4x4 tr, Vector3 size, int seed, float wildness)
        {
            var rng = new DetRandom((ulong)(seed * 104729 + 7));
            var local = new MeshData();
            Vector3 h = size * 0.5f;
            float corner = Mathf.Min(0.22f, Mathf.Min(h.x, h.z) * 0.6f);
            KitPrims.BevelBox(local, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one), new Vector3(size.x - 0.12f, size.y - 0.08f, size.z - 0.12f), corner, HedgeBody, 0.7f, 0.25f);
            float flakeLen = 0.3f, flakeW = 0.26f;
            // Sides: rows of downward flakes.
            float rowStep = flakeLen * 0.45f;
            int rows = Mathf.Max(1, Mathf.FloorToInt((size.y - 0.1f) / rowStep));
            float perim = 2f * (size.x + size.z);
            int perRow = Mathf.CeilToInt(perim / (flakeW * 0.7f));
            for (int r = 0; r < rows; r++)
            {
                float y = 0.06f + r * rowStep;
                float t = y / size.y;
                float off = (r % 2) * 0.5f;
                for (int i = 0; i < perRow; i++)
                {
                    float s = (i + off + rng.Signed() * 0.15f) / perRow * perim;
                    Vector3 pos, outward;
                    PerimeterPoint(s, size, corner, out pos, out outward);
                    pos.y = y;
                    Vector3 massN = (outward + Vector3.up * Mathf.Max(0f, (t - 0.85f) * 3f)).normalized;
                    Vector3 tip = (Vector3.down * 0.9f + Vector3.Cross(Vector3.up, outward) * rng.Signed() * 0.3f).normalized;
                    float ao = Mathf.Lerp(0.42f, 0.95f, Mathf.Pow(t, 0.8f));
                    Color c = Palette.Jitter(Color.Lerp(HedgeDeep, HedgeLit, 0.35f + 0.55f * t), 0.04f, ref rng);
                    float sc = 0.9f + 0.2f * rng.Value();
                    KitPrims.Flake(local, pos + outward * 0.01f, outward, tip, massN, flakeLen * sc, flakeW * sc, c, ao, 0.08f * t, rng.Value());
                }
            }
            // Notched edging along the top rim: flakes leaning outward.
            int edge = Mathf.CeilToInt(perim / (flakeW * 0.8f));
            for (int i = 0; i < edge; i++)
            {
                float s = (i + rng.Value() * 0.3f) / edge * perim;
                Vector3 pos, outward;
                PerimeterPoint(s, size, corner, out pos, out outward);
                pos.y = size.y - 0.05f;
                Vector3 n = (outward + Vector3.up).normalized;
                Color c = Palette.Jitter(HedgeLit, 0.04f, ref rng);
                KitPrims.Flake(local, pos, n, outward + Vector3.up * 0.2f, n, flakeLen * 1.05f, flakeW, c, 0.98f, 0.12f, rng.Value());
            }
            // Top: a mown plane of flakes in random directions.
            float area = (size.x - 0.1f) * (size.z - 0.1f);
            int topCount = Mathf.CeilToInt(area / (flakeW * flakeLen * 0.45f));
            for (int i = 0; i < topCount; i++)
            {
                Vector3 pos = new Vector3(rng.Range(-h.x + 0.08f, h.x - 0.08f), size.y - 0.02f + rng.Value() * 0.025f, rng.Range(-h.z + 0.08f, h.z - 0.08f));
                Vector3 tip = new Vector3(rng.Signed(), 0f, rng.Signed());
                Color c = Palette.Jitter(Color.Lerp(HedgeLit, Palette.Hex("#7d873a"), rng.Value() * 0.4f), 0.04f, ref rng);
                KitPrims.Flake(local, pos, Vector3.up, tip, Vector3.up, flakeLen * 0.95f, flakeW, c, 1f, 0.05f, rng.Value());
            }
            // Wild growth breaking out of the clipped shape.
            if (wildness > 0f)
            {
                int sprouts = Mathf.RoundToInt(size.x * 2f * wildness);
                for (int i = 0; i < sprouts; i++)
                {
                    Vector3 basePos = new Vector3(rng.Range(-h.x * 0.8f, h.x * 0.8f), size.y, rng.Range(-h.z * 0.6f, h.z * 0.6f));
                    AddSprout(local, basePos, rng.Range(0.3f, 0.75f) * wildness + 0.2f, ref rng);
                }
            }
            m.Append(local, tr);
        }

        static void PerimeterPoint(float s, Vector3 size, float corner, out Vector3 pos, out Vector3 outward)
        {
            // Walk a rounded rectangle perimeter (approximated by straight sides + corner arcs).
            float hx = size.x * 0.5f, hz = size.z * 0.5f;
            float lx = size.x, lz = size.z;
            float perim = 2f * (lx + lz);
            s = Mathf.Repeat(s, perim);
            if (s < lx) { pos = new Vector3(-hx + s, 0, -hz); outward = Vector3.back; }
            else if (s < lx + lz) { pos = new Vector3(hx, 0, -hz + (s - lx)); outward = Vector3.right; }
            else if (s < 2 * lx + lz) { pos = new Vector3(hx - (s - lx - lz), 0, hz); outward = Vector3.forward; }
            else { pos = new Vector3(-hx, 0, hz - (s - 2 * lx - lz)); outward = Vector3.left; }
            // Round the corners: pull points near corners inward and blend normals.
            float cx = Mathf.Clamp(pos.x, -hx + corner, hx - corner);
            float cz = Mathf.Clamp(pos.z, -hz + corner, hz - corner);
            Vector3 d = new Vector3(pos.x - cx, 0, pos.z - cz);
            if (d.sqrMagnitude > 1e-6f)
            {
                outward = d.normalized;
                pos = new Vector3(cx, 0, cz) + outward * corner;
            }
        }

        static void AddSprout(MeshData m, Vector3 basePos, float height, ref DetRandom rng)
        {
            Vector3 lean = new Vector3(rng.Signed() * 0.4f, 1f, rng.Signed() * 0.4f).normalized;
            int leaves = 3 + rng.Range(0, 4);
            for (int i = 0; i < leaves; i++)
            {
                float t = (i + 1f) / leaves;
                Vector3 p = basePos + lean * height * t;
                Vector3 dir = new Vector3(rng.Signed(), 0.6f, rng.Signed()).normalized;
                Color c = Palette.Jitter(Color.Lerp(WildA, Palette.Hex("#8a8a3a"), rng.Value() * 0.4f), 0.1f, ref rng);
                KitPrims.Flake(m, p, dir, dir + Vector3.up * 0.5f, (dir + Vector3.up).normalized, 0.2f, 0.12f, c, 0.95f, 0.6f, rng.Value());
            }
        }

        /// <summary>Roots draped over a block: the garden reclaiming its order.</summary>
        static void AddRoots(MeshData m, Vector3 size, int seed)
        {
            var rng = new DetRandom((ulong)(seed * 31337 + 5));
            int count = 2 + rng.Range(0, 3);
            float hx = size.x * 0.5f, hz = size.z * 0.5f;
            for (int i = 0; i < count; i++)
            {
                float x = rng.Range(-hx * 0.8f, hx * 0.8f);
                float side = rng.Chance(0.5f) ? 1f : -1f;
                var path = new List<Vector3>();
                var radius = new List<float>();
                float r0 = rng.Range(0.035f, 0.07f);
                // Over the top, down one face, along the ground.
                path.Add(new Vector3(x + rng.Signed() * 0.3f, size.y + 0.03f, -side * hz * 0.4f));
                path.Add(new Vector3(x, size.y + 0.05f, side * (hz - 0.05f)));
                path.Add(new Vector3(x + rng.Signed() * 0.15f, size.y * 0.6f, side * (hz + 0.06f)));
                path.Add(new Vector3(x + rng.Signed() * 0.2f, size.y * 0.25f, side * (hz + 0.1f)));
                path.Add(new Vector3(x + rng.Signed() * 0.4f, 0.03f, side * (hz + 0.35f + rng.Value() * 0.3f)));
                var smooth = new List<Vector3>();
                for (int s = 0; s < path.Count - 1; s++)
                {
                    for (int q = 0; q < 4; q++) smooth.Add(Vector3.Lerp(path[s], path[s + 1], q / 4f));
                }
                smooth.Add(path[path.Count - 1]);
                for (int s = 0; s < smooth.Count; s++) radius.Add(r0 * Mathf.Lerp(1.1f, 0.5f, s / (float)(smooth.Count - 1)));
                KitPrims.Tube(m, smooth, radius, 5, Root, 0.85f, 0.55f);
            }
        }

        // ------------------------------------------------------------------ dome bush

        public static KitMesh DomeBush(int seed, float radius = 0.65f, int flowers = 0)
        {
            var k = new KitMesh();
            var m = k.Foliage;
            var rng = new DetRandom((ulong)(seed * 6007 + 3));
            float hRatio = 0.78f;
            KitPrims.Sphere(m, Matrix4x4.TRS(Vector3.up * radius * 0.35f, Quaternion.identity, new Vector3(radius, radius * hRatio, radius) * 0.86f), 2, HedgeBody, 0.3f, 0.7f, 0.08f, seed);
            int count = Mathf.CeilToInt(radius * radius * 165f);
            float golden = Mathf.PI * (3f - Mathf.Sqrt(5f));
            for (int i = 0; i < count; i++)
            {
                float y = 1f - (i + 0.5f) / count * 1.25f;
                if (y < -0.3f) continue;
                float r = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
                float a = i * golden;
                Vector3 n = new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                float lump = 1f + (Noise.Fbm3(n * 1.8f + Vector3.one * seed, 2, seed) - 0.5f) * 0.32f;
                Vector3 pos = new Vector3(n.x * radius, radius * 0.35f + n.y * radius * hRatio, n.z * radius) * lump;
                Vector3 outward = new Vector3(n.x, n.y / hRatio, n.z).normalized;
                Vector3 tip = (Vector3.down + new Vector3(rng.Signed(), 0, rng.Signed()) * 0.4f).normalized;
                float ao = Mathf.Lerp(0.4f, 1f, Mathf.Clamp01(y * 0.6f + 0.55f));
                Color c = Palette.Jitter(Color.Lerp(HedgeDeep, HedgeLit, 0.35f + 0.6f * Mathf.Clamp01(y + 0.3f)), 0.035f, ref rng);
                float sc = 0.85f + 0.3f * rng.Value();
                KitPrims.Flake(m, pos, outward, tip, outward, 0.21f * sc, 0.18f * sc, c, ao, 0.18f, rng.Value());
            }
            for (int i = 0; i < flowers; i++)
            {
                float a = rng.Value() * Mathf.PI * 2f;
                float e = rng.Range(0.35f, 1.15f);
                Vector3 n = new Vector3(Mathf.Cos(a) * Mathf.Cos(e), Mathf.Sin(e), Mathf.Sin(a) * Mathf.Cos(e));
                Vector3 pos = new Vector3(n.x * radius, radius * 0.35f + n.y * radius * hRatio, n.z * radius) * 1.04f;
                int kind = (seed + i) % 3;
                if (kind == 0) AddHyacinth(m, pos, ref rng);
                else if (kind == 1) AddGlowFlower(k.GlowAmber, pos, n, ref rng);
                else AddBloom(m, pos, n, ref rng);
            }
            return k;
        }

        static void AddHyacinth(MeshData m, Vector3 pos, ref DetRandom rng)
        {
            Color blue = Palette.Jitter(Palette.Hex("#7f8fd6"), 0.08f, ref rng);
            float h = rng.Range(0.28f, 0.42f);
            var path = new List<Vector3> { pos, pos + Vector3.up * h };
            KitPrims.Tube(m, path, new List<float> { 0.012f, 0.01f }, 4, Palette.Hex("#4e6b2c"), 0.8f, 0.9f, 0.4f);
            for (int i = 0; i < 6; i++)
            {
                float t = 0.45f + i * 0.1f;
                Vector3 p = pos + Vector3.up * h * t;
                KitPrims.Sphere(m, Matrix4x4.TRS(p, Quaternion.identity, Vector3.one * (0.045f - i * 0.004f)), 0, blue, 0.85f, 1f, 0f, 0, 0f, 0.5f);
            }
        }

        static void AddGlowFlower(MeshData glow, Vector3 pos, Vector3 n, ref DetRandom rng)
        {
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2f + rng.Value();
                Vector3 d = new Vector3(Mathf.Cos(a), 0.3f, Mathf.Sin(a)).normalized;
                KitPrims.Flake(glow, pos + n * 0.02f, (n + Vector3.up).normalized, d, n, 0.07f, 0.05f, Palette.Hex("#ffd27a"), 1f, 0f, rng.Value());
            }
        }

        static void AddBloom(MeshData m, Vector3 pos, Vector3 n, ref DetRandom rng)
        {
            Color red = Palette.Jitter(Palette.Hex("#c8322a"), 0.08f, ref rng);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                Vector3 d = new Vector3(Mathf.Cos(a), 0.5f, Mathf.Sin(a)).normalized;
                KitPrims.Flake(m, pos, (n + Vector3.up * 0.5f).normalized, d, n, 0.08f, 0.07f, red, 1f, 0.2f, rng.Value());
            }
        }

        // ------------------------------------------------------------------ autumn tree

        public static KitMesh Tree(int seed, Color leaf, float height = 5.2f, float canopy = 2.2f)
        {
            var k = new KitMesh();
            var rng = new DetRandom((ulong)(seed * 15485863 + 11));
            // Trunk with a lean and a couple of limbs.
            var trunk = new List<Vector3>();
            var tr = new List<float>();
            Vector3 lean = new Vector3(rng.Signed() * 0.25f, 0, rng.Signed() * 0.25f);
            float trunkTop = height - canopy * 0.9f;
            for (int i = 0; i <= 6; i++)
            {
                float t = i / 6f;
                trunk.Add(new Vector3(0, t * trunkTop, 0) + lean * t * t + new Vector3(Mathf.Sin(t * 4f + seed) * 0.06f, 0, Mathf.Cos(t * 3f + seed) * 0.06f));
                tr.Add(Mathf.Lerp(0.24f, 0.11f, t) * (i == 0 ? 1.35f : 1f));
            }
            KitPrims.Tube(k.Lit, trunk, tr, 7, Bark, 0.35f, 0.8f);
            Vector3 top = trunk[trunk.Count - 1];
            int limbs = 3;
            for (int l = 0; l < limbs; l++)
            {
                float a = l / (float)limbs * Mathf.PI * 2f + rng.Value();
                Vector3 dir = new Vector3(Mathf.Cos(a), 0.9f, Mathf.Sin(a)).normalized;
                var limb = new List<Vector3> { top - Vector3.up * 0.4f, top + dir * canopy * 0.45f, top + dir * canopy * 0.75f + Vector3.up * 0.3f };
                KitPrims.Tube(k.Lit, limb, new List<float> { 0.09f, 0.06f, 0.035f }, 5, Bark, 0.5f, 0.85f);
            }
            // Canopy lobes.
            Vector3 centre = top + Vector3.up * canopy * 0.55f;
            int lobes = 6 + rng.Range(0, 4);
            var lobePos = new List<Vector3>();
            var lobeR = new List<float>();
            lobePos.Add(centre);
            lobeR.Add(canopy * 0.72f);
            for (int i = 1; i < lobes; i++)
            {
                Vector3 d = rng.OnSphere();
                d.y = Mathf.Abs(d.y) * 0.6f - 0.1f;
                d.Normalize();
                lobePos.Add(centre + d * canopy * rng.Range(0.45f, 0.7f));
                lobeR.Add(canopy * rng.Range(0.42f, 0.58f));
            }
            var m = k.Foliage;
            for (int i = 0; i < lobes; i++)
            {
                KitPrims.Sphere(m, Matrix4x4.TRS(lobePos[i], Quaternion.identity, Vector3.one * lobeR[i] * 0.86f), 2, Palette.Mul(leaf, 0.62f), 0.35f, 0.7f, 0.1f, seed + i, 0f, 0.3f);
            }
            float golden = Mathf.PI * (3f - Mathf.Sqrt(5f));
            for (int li = 0; li < lobes; li++)
            {
                float R = lobeR[li];
                int count = Mathf.CeilToInt(R * R * 95f);
                for (int i = 0; i < count; i++)
                {
                    float y = 1f - (i + 0.5f) / count * 2f;
                    float r = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
                    float a = i * golden + li;
                    Vector3 n = new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                    Vector3 pos = lobePos[li] + n * R * (1f + (rng.Value() - 0.5f) * 0.08f);
                    // Skip flakes buried inside a neighbouring lobe.
                    bool buried = false;
                    for (int o = 0; o < lobes && !buried; o++)
                    {
                        if (o == li) continue;
                        if ((pos - lobePos[o]).magnitude < lobeR[o] * 0.92f) buried = true;
                    }
                    if (buried) continue;
                    Vector3 cloudN = ((pos - centre).normalized * 0.6f + n * 0.4f).normalized;
                    float depth = Mathf.Clamp01((pos - centre).magnitude / (canopy * 1.2f));
                    float ao = Mathf.Lerp(0.5f, 1f, depth) * Mathf.Lerp(0.62f, 1f, Mathf.Clamp01(cloudN.y + 0.55f));
                    Vector3 tip = (Vector3.down * 0.75f + new Vector3(rng.Signed(), 0, rng.Signed()) * 0.5f).normalized;
                    Color c = Palette.Jitter(leaf, 0.05f, ref rng);
                    if (rng.Chance(0.08f)) c = Color.Lerp(c, Rust, 0.35f);
                    float sc = 0.85f + 0.3f * rng.Value();
                    KitPrims.Flake(m, pos, n, tip, cloudN, 0.34f * sc, 0.24f * sc, c, ao, 0.5f, rng.Value());
                }
            }
            return k;
        }

        // ------------------------------------------------------------------ small growth

        public static KitMesh GrassTuft(int seed, float size = 0.35f, bool dry = false)
        {
            var k = new KitMesh();
            var rng = new DetRandom((ulong)(seed * 7 + 1));
            int blades = 9 + rng.Range(0, 6);
            Color baseC = dry ? Palette.Hex("#a99a58") : Palette.Hex("#788736");
            for (int i = 0; i < blades; i++)
            {
                float a = rng.Value() * Mathf.PI * 2f;
                Vector3 d = new Vector3(Mathf.Cos(a) * 0.35f, 1f, Mathf.Sin(a) * 0.35f).normalized;
                Vector3 p = new Vector3(rng.Signed() * 0.05f, 0, rng.Signed() * 0.05f);
                Color c = Palette.Jitter(baseC, 0.15f, ref rng);
                float len = size * rng.Range(0.7f, 1.2f);
                // Blade: thin flake standing up.
                Vector3 side = Vector3.Cross(d, Vector3.right).normalized;
                Vector3 outward = Vector3.Cross(side, d).normalized;
                KitPrims.Flake(k.Foliage, p, outward, d, (outward + Vector3.up * 0.6f).normalized, len, len * 0.2f, c, 0.92f, 0.9f, rng.Value());
            }
            return k;
        }

        public static KitMesh WildMass(int seed, float radius = 1.1f)
        {
            var k = new KitMesh();
            var rng = new DetRandom((ulong)(seed * 977 + 31));
            int lobes = 3 + rng.Range(0, 3);
            for (int i = 0; i < lobes; i++)
            {
                Vector3 c = new Vector3(rng.Signed() * radius * 0.6f, 0f, rng.Signed() * radius * 0.6f);
                float r = radius * rng.Range(0.45f, 0.75f);
                var bush = DomeBush(seed * 13 + i, r, 0);
                Color tint = Color.Lerp(new Color(0.75f, 0.85f, 0.7f), new Color(1.1f, 0.95f, 0.75f), rng.Value());
                k.Foliage.Append(bush.Foliage, Matrix4x4.TRS(c, Quaternion.Euler(0, rng.Value() * 360f, 0), new Vector3(1f, rng.Range(0.8f, 1.3f), 1f)), tint, 0.05f);
            }
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = new Vector3(rng.Signed() * radius, radius * rng.Range(0.6f, 1.2f), rng.Signed() * radius);
                AddSprout(k.Foliage, p, rng.Range(0.3f, 0.7f), ref rng);
            }
            return k;
        }

        /// <summary>Pale cyan lantern lily: a luminous accent used sparingly near hazards and cups.</summary>
        public static KitMesh LanternLily(int seed)
        {
            var k = new KitMesh();
            var rng = new DetRandom((ulong)(seed * 211 + 17));
            int stems = 2 + rng.Range(0, 3);
            for (int i = 0; i < stems; i++)
            {
                Vector3 b = new Vector3(rng.Signed() * 0.12f, 0, rng.Signed() * 0.12f);
                float h = rng.Range(0.35f, 0.6f);
                Vector3 lean = new Vector3(rng.Signed() * 0.15f, 1f, rng.Signed() * 0.15f).normalized;
                Vector3 top = b + lean * h;
                KitPrims.Tube(k.Foliage, new List<Vector3> { b, b + lean * h * 0.5f + Vector3.right * 0.03f, top }, new List<float> { 0.014f, 0.011f, 0.008f }, 4, Palette.Hex("#3d5a3a"), 0.7f, 0.9f, 0.6f);
                // Drooping bell.
                var prof = new List<Vector2> { new Vector2(0.005f, 0f), new Vector2(0.05f, -0.03f), new Vector2(0.06f, -0.08f), new Vector2(0.07f, -0.11f) };
                KitPrims.Lathe(k.Glow, Matrix4x4.TRS(top, Quaternion.Euler(rng.Signed() * 15f, 0, rng.Signed() * 15f), Vector3.one), prof, 6, Color.white, 1f, 1f, false);
                // Two leaves at the base.
                Vector3 ld = new Vector3(rng.Signed(), 0.4f, rng.Signed()).normalized;
                KitPrims.Flake(k.Foliage, b, (ld + Vector3.up).normalized, ld, Vector3.up, 0.22f, 0.08f, Palette.Hex("#4c6a3c"), 0.8f, 0.3f, rng.Value());
            }
            return k;
        }
    }
}
