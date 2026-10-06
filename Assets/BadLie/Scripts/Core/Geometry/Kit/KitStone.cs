using System;
using System.Collections.Generic;
using BadLie.Core;
using UnityEngine;

namespace BadLie.Geometry
{
    /// <summary>Stonework: coursed walls, balustrades, curbs, columns, arches and urns.</summary>
    public static class KitStone
    {
        public static readonly Color Sandstone = Palette.Hex("#d7b88a");
        public static readonly Color SandstoneDark = Palette.Hex("#b8956a");
        public static readonly Color Crimson = Palette.Hex("#9c3f48");
        public static readonly Color Terracotta = Palette.Hex("#b4624a");
        public static readonly Color TerracottaCap = Palette.Hex("#d08a6a");
        public static readonly Color Coping = Palette.Hex("#cdb593");
        public static readonly Color Moss = Palette.Hex("#6b7330");

        /// <summary>Point and tangent at arc length s along a polyline.</summary>
        public static void Along(IList<Vector2> path, float s, out Vector2 pos, out Vector2 tangent)
        {
            float acc = 0f;
            for (int i = 0; i < path.Count - 1; i++)
            {
                float seg = (path[i + 1] - path[i]).magnitude;
                if (acc + seg >= s || i == path.Count - 2)
                {
                    float t = seg > 1e-6f ? Mathf.Clamp01((s - acc) / seg) : 0f;
                    pos = Vector2.Lerp(path[i], path[i + 1], t);
                    tangent = seg > 1e-6f ? (path[i + 1] - path[i]) / seg : Vector2.right;
                    return;
                }
                acc += seg;
            }
            pos = path[path.Count - 1];
            tangent = Vector2.right;
        }

        static Quaternion Facing(Vector2 tangent)
        {
            // Local +X runs along the wall.
            float yaw = -Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
            return Quaternion.Euler(0f, yaw, 0f);
        }

        // ------------------------------------------------------------------ walls

        public static KitMesh SandstoneWall(IList<Vector2> path, float height, float thickness, Func<Vector2, float> ground, int seed, bool ruined = false)
        {
            var k = new KitMesh();
            var m = k.Lit;
            var rng = new DetRandom((ulong)(seed * 4421 + 19));
            float len = Geo2D.PathLength(path);
            const float capH = 0.12f;
            float course = 0.27f;
            int rows = Mathf.Max(1, Mathf.RoundToInt((height - capH) / course));
            course = (height - capH) / rows;
            for (int r = 0; r < rows; r++)
            {
                float s = (r % 2 == 1) ? -0.3f : 0f;
                while (s < len)
                {
                    float bl = rng.Range(0.42f, 0.74f);
                    float s0 = Mathf.Max(0f, s), s1 = Mathf.Min(len, s + bl);
                    s += bl;
                    if (s1 - s0 < 0.12f) continue;
                    if (ruined && r >= rows - 2 && rng.Chance(0.35f + 0.25f * (r - rows + 2))) continue;
                    Vector2 p, t;
                    Along(path, (s0 + s1) * 0.5f, out p, out t);
                    float baseY = ground(p) - 0.04f;
                    Color c = Palette.Jitter(Color.Lerp(SandstoneDark, Sandstone, rng.Range(0.3f, 1f)), 0.07f, ref rng);
                    if (rng.Chance(0.15f)) c = Color.Lerp(c, Palette.Hex("#c9a35a"), 0.4f);
                    float inset = rng.Range(0f, 0.025f);
                    var tr = Matrix4x4.TRS(new Vector3(p.x, baseY + r * course + 0.01f, p.y), Facing(t), Vector3.one);
                    KitPrims.BevelBox(m, tr, new Vector3(s1 - s0 - 0.025f, course - 0.022f, thickness - inset * 2f), 0.035f, c, Mathf.Lerp(0.6f, 0.95f, (r + 1f) / rows), Mathf.Lerp(0.45f, 0.85f, r / (float)rows));
                }
            }
            if (!ruined)
            {
                float s = 0f;
                while (s < len)
                {
                    float bl = rng.Range(0.75f, 1.0f);
                    float s0 = s, s1 = Mathf.Min(len, s + bl);
                    s += bl;
                    if (s1 - s0 < 0.15f) continue;
                    Vector2 p, t;
                    Along(path, (s0 + s1) * 0.5f, out p, out t);
                    float baseY = ground(p) - 0.04f;
                    Color c = Palette.Jitter(Coping, 0.05f, ref rng);
                    var tr = Matrix4x4.TRS(new Vector3(p.x, baseY + rows * course + 0.01f, p.y), Facing(t), Vector3.one);
                    KitPrims.BevelBox(m, tr, new Vector3(s1 - s0 - 0.02f, capH, thickness + 0.08f), 0.03f, c, 1f, 0.85f);
                    // Moss creeping on the cap.
                    if (rng.Chance(0.3f))
                    {
                        var moss = Matrix4x4.TRS(new Vector3(p.x, baseY + rows * course + capH + 0.005f, p.y), Facing(t), Vector3.one);
                        KitPrims.BevelBox(k.Foliage, moss, new Vector3((s1 - s0) * rng.Range(0.3f, 0.8f), 0.03f, thickness * 0.8f), 0.012f, Palette.Jitter(Moss, 0.1f, ref rng), 1f, 0.9f);
                    }
                }
            }
            return k;
        }

        public static KitMesh Balustrade(IList<Vector2> path, float height, Func<Vector2, float> ground, int seed)
        {
            var k = new KitMesh();
            var m = k.Lit;
            var rng = new DetRandom((ulong)(seed * 9973 + 23));
            // Post positions: path vertices plus subdivisions.
            var posts = new List<Vector2>();
            for (int i = 0; i < path.Count; i++)
            {
                posts.Add(path[i]);
                if (i == path.Count - 1) break;
                float seg = (path[i + 1] - path[i]).magnitude;
                int sub = Mathf.CeilToInt(seg / 2.3f);
                for (int j = 1; j < sub; j++) posts.Add(Vector2.Lerp(path[i], path[i + 1], j / (float)sub));
            }
            const float pier = 0.34f;
            for (int i = 0; i < posts.Count; i++)
            {
                Vector2 p = posts[i];
                Vector2 t = i < posts.Count - 1 ? (posts[i + 1] - p).normalized : (p - posts[i - 1]).normalized;
                float by = ground(p) - 0.05f;
                var q = Facing(t);
                Color c = Palette.Jitter(Crimson, 0.06f, ref rng);
                KitPrims.BevelBox(m, Matrix4x4.TRS(new Vector3(p.x, by, p.y), q, Vector3.one), new Vector3(pier, height - 0.06f, pier), 0.03f, c, 0.95f, 0.5f);
                KitPrims.BevelBox(m, Matrix4x4.TRS(new Vector3(p.x, by + height - 0.08f, p.y), q, Vector3.one), new Vector3(pier + 0.08f, 0.1f, pier + 0.08f), 0.025f, Lichen(c, ref rng), 1f, 0.9f);
                if (i == 0 || i == posts.Count - 1 || i % 3 == 0) Urn(m, new Vector3(p.x, by + height + 0.02f, p.y), 0.55f, Palette.Jitter(Palette.Hex("#b0574e"), 0.05f, ref rng));
            }
            for (int i = 0; i < posts.Count - 1; i++)
            {
                Vector2 a = posts[i], b = posts[i + 1];
                Vector2 d = b - a;
                float L = d.magnitude;
                if (L < pier + 0.1f) continue;
                Vector2 t = d / L;
                Vector2 mid = (a + b) * 0.5f;
                float by = ground(mid) - 0.05f;
                var q = Facing(t);
                float span = L - pier;
                Color c = Palette.Jitter(Crimson, 0.05f, ref rng);
                KitPrims.BevelBox(m, Matrix4x4.TRS(new Vector3(mid.x, by, mid.y), q, Vector3.one), new Vector3(span, 0.16f, 0.28f), 0.025f, c, 0.85f, 0.5f);
                KitPrims.BevelBox(m, Matrix4x4.TRS(new Vector3(mid.x, by + height - 0.2f, mid.y), q, Vector3.one), new Vector3(span, 0.11f, 0.3f), 0.025f, Lichen(c, ref rng), 1f, 0.85f);
                int n = Mathf.Max(1, Mathf.FloorToInt(span / 0.31f));
                float bh = height - 0.2f - 0.16f;
                for (int j = 0; j < n; j++)
                {
                    float f = (j + 0.5f) / n;
                    Vector2 bp = Vector2.Lerp(a + t * pier * 0.5f, b - t * pier * 0.5f, f);
                    if (rng.Chance(0.04f)) continue; // a missing baluster here and there
                    Baluster(m, new Vector3(bp.x, by + 0.16f, bp.y), bh, Palette.Jitter(Crimson, 0.06f, ref rng));
                }
            }
            return k;
        }

        static Color Lichen(Color c, ref DetRandom rng)
        {
            // Ochre lichen marks vertex alpha; the Lit shader turns it into blotches.
            Color l = c;
            l.a = rng.Range(0.45f, 0.72f);
            return l;
        }

        static readonly Vector2[] BalusterProfile =
        {
            new Vector2(0.060f, 0.00f), new Vector2(0.060f, 0.05f), new Vector2(0.045f, 0.08f), new Vector2(0.060f, 0.16f),
            new Vector2(0.085f, 0.32f), new Vector2(0.070f, 0.48f), new Vector2(0.040f, 0.62f), new Vector2(0.048f, 0.70f),
            new Vector2(0.040f, 0.80f), new Vector2(0.060f, 0.92f), new Vector2(0.060f, 1.00f),
        };

        static void Baluster(MeshData m, Vector3 basePos, float h, Color c)
        {
            var prof = new List<Vector2>();
            foreach (var p in BalusterProfile) prof.Add(new Vector2(p.x, p.y * h));
            KitPrims.Lathe(m, Matrix4x4.Translate(basePos), prof, 8, c, 0.45f, 0.85f, false);
        }

        public static void Urn(MeshData m, Vector3 basePos, float h, Color c)
        {
            var prof = new List<Vector2>
            {
                new Vector2(0.11f, 0f), new Vector2(0.11f, 0.06f), new Vector2(0.06f, 0.1f), new Vector2(0.07f, 0.16f),
                new Vector2(0.16f, 0.32f), new Vector2(0.17f, 0.44f), new Vector2(0.12f, 0.58f), new Vector2(0.09f, 0.66f),
                new Vector2(0.13f, 0.72f), new Vector2(0.13f, 0.76f), new Vector2(0.05f, 0.82f), new Vector2(0.03f, 0.95f), new Vector2(0.0f, 1f),
            };
            for (int i = 0; i < prof.Count; i++) prof[i] = new Vector2(prof[i].x * h * 1.15f, prof[i].y * h);
            KitPrims.Lathe(m, Matrix4x4.Translate(basePos), prof, 10, c, 0.5f, 1f, false);
        }

        public static KitMesh Curb(IList<Vector2> path, float height, float thickness, Func<Vector2, float> ground, int seed)
        {
            var k = new KitMesh();
            var rng = new DetRandom((ulong)(seed * 3571 + 29));
            float len = Geo2D.PathLength(path);
            float s = 0f;
            while (s < len)
            {
                float bl = rng.Range(0.24f, 0.3f);
                float s0 = s, s1 = Mathf.Min(len, s + bl);
                s += bl;
                if (s1 - s0 < 0.06f) continue;
                Vector2 p, t;
                Along(path, (s0 + s1) * 0.5f, out p, out t);
                float by = ground(p) - 0.03f;
                Color c = Palette.Jitter(Color.Lerp(Terracotta, TerracottaCap, rng.Value() * 0.4f), 0.08f, ref rng);
                KitPrims.BevelBox(k.Lit, Matrix4x4.TRS(new Vector3(p.x, by, p.y), Facing(t), Vector3.one), new Vector3(s1 - s0 - 0.015f, height + 0.03f, thickness), 0.022f, c, 1f, 0.55f);
            }
            return k;
        }

        public static KitMesh CopingWall(IList<Vector2> path, float height, float thickness, Func<Vector2, float> ground, int seed)
        {
            var k = new KitMesh();
            var rng = new DetRandom((ulong)(seed * 2203 + 37));
            float len = Geo2D.PathLength(path);
            float s = 0f;
            while (s < len)
            {
                float bl = rng.Range(0.6f, 0.85f);
                float s0 = s, s1 = Mathf.Min(len, s + bl);
                s += bl;
                if (s1 - s0 < 0.1f) continue;
                Vector2 p, t;
                Along(path, (s0 + s1) * 0.5f, out p, out t);
                float by = ground(p) - 0.04f;
                Color c = Palette.Jitter(Coping, 0.06f, ref rng);
                KitPrims.BevelBox(k.Lit, Matrix4x4.TRS(new Vector3(p.x, by, p.y), Facing(t), Vector3.one), new Vector3(s1 - s0 - 0.02f, height + 0.04f, thickness), 0.035f, c, 1f, 0.6f);
            }
            return k;
        }

        // ------------------------------------------------------------------ pieces

        public static KitMesh Column(int seed, float height = 2.8f, bool broken = false)
        {
            var k = new KitMesh();
            var rng = new DetRandom((ulong)(seed * 613 + 41));
            Color c = Palette.Jitter(Sandstone, 0.06f, ref rng);
            KitPrims.BevelBox(k.Lit, Matrix4x4.identity, new Vector3(0.7f, 0.22f, 0.7f), 0.04f, Palette.Mul(c, 0.92f), 0.85f, 0.45f);
            KitPrims.BevelBox(k.Lit, Matrix4x4.Translate(Vector3.up * 0.22f), new Vector3(0.58f, 0.1f, 0.58f), 0.03f, c, 0.9f, 0.6f);
            float shaftH = broken ? height * rng.Range(0.35f, 0.6f) : height - 0.55f;
            var prof = new List<Vector2>();
            for (int i = 0; i <= 6; i++)
            {
                float t = i / 6f;
                prof.Add(new Vector2(Mathf.Lerp(0.23f, 0.19f, t) * (1f + 0.04f * Mathf.Sin(t * Mathf.PI)), 0.32f + t * shaftH));
            }
            KitPrims.Lathe(k.Lit, Matrix4x4.identity, prof, 12, c, 0.6f, 1f, true);
            if (!broken)
            {
                float ty = 0.32f + shaftH;
                var cap = new List<Vector2> { new Vector2(0.2f, ty), new Vector2(0.27f, ty + 0.08f), new Vector2(0.3f, ty + 0.12f) };
                KitPrims.Lathe(k.Lit, Matrix4x4.identity, cap, 12, c, 0.9f, 1f, false);
                KitPrims.BevelBox(k.Lit, Matrix4x4.Translate(Vector3.up * (ty + 0.12f)), new Vector3(0.68f, 0.12f, 0.68f), 0.03f, Palette.Mul(c, 1.05f), 1f, 0.85f);
            }
            else
            {
                // Fallen drum beside the stump.
                var drum = new List<Vector2> { new Vector2(0.2f, 0f), new Vector2(0.2f, 0.5f) };
                KitPrims.Lathe(k.Lit, Matrix4x4.TRS(new Vector3(0.6f, 0.2f, 0.2f), Quaternion.Euler(0, rng.Value() * 180f, 90f), Vector3.one), drum, 12, Palette.Mul(c, 0.95f), 0.6f, 0.9f, true);
            }
            return k;
        }

        public static KitMesh UrnOnPlinth(int seed)
        {
            var k = new KitMesh();
            var rng = new DetRandom((ulong)(seed * 17 + 3));
            KitPrims.BevelBox(k.Lit, Matrix4x4.identity, new Vector3(0.6f, 0.5f, 0.6f), 0.04f, Palette.Jitter(Sandstone, 0.05f, ref rng), 0.95f, 0.45f);
            Urn(k.Lit, Vector3.up * 0.5f, 0.85f, Palette.Jitter(Palette.Hex("#b5634e"), 0.06f, ref rng));
            var bush = KitFoliage.DomeBush(seed + 5, 0.28f, 3);
            k.Foliage.Append(bush.Foliage, Matrix4x4.Translate(Vector3.up * 1.12f));
            k.GlowAmber.Append(bush.GlowAmber, Matrix4x4.Translate(Vector3.up * 1.12f));
            return k;
        }

        /// <summary>Weathered arch: two piers and a ring of voussoirs, some fallen.</summary>
        public static KitMesh Arch(int seed, float span = 2.6f, float pierH = 2.2f, float depth = 0.7f, float decay = 0.25f)
        {
            var k = new KitMesh();
            var rng = new DetRandom((ulong)(seed * 7121 + 43));
            float pierW = 0.62f;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (span * 0.5f + pierW * 0.5f);
                int courses = 7;
                float ch = pierH / courses;
                for (int c = 0; c < courses; c++)
                {
                    Color col = Palette.Jitter(Color.Lerp(SandstoneDark, Sandstone, rng.Value()), 0.07f, ref rng);
                    KitPrims.BevelBox(k.Lit, Matrix4x4.Translate(new Vector3(x + rng.Signed() * 0.015f, c * ch, 0)), new Vector3(pierW, ch - 0.02f, depth), 0.035f, col, Mathf.Lerp(0.6f, 1f, c / (float)courses), Mathf.Lerp(0.4f, 0.9f, c / (float)courses));
                }
            }
            float R = span * 0.5f + pierW * 0.5f;
            int stones = 11;
            for (int i = 0; i < stones; i++)
            {
                float a0 = Mathf.PI * i / stones, a1 = Mathf.PI * (i + 1) / stones;
                float am = (a0 + a1) * 0.5f;
                bool fallen = (i > stones / 2) && rng.Chance(decay * (i - stones / 2) / 3f);
                if (fallen) continue;
                Vector3 p = new Vector3(Mathf.Cos(am) * R, pierH + Mathf.Sin(am) * R - pierW * 0.5f, 0);
                float arcLen = R * (a1 - a0);
                Color col = Palette.Jitter(Color.Lerp(SandstoneDark, Sandstone, rng.Value()), 0.07f, ref rng);
                var tr = Matrix4x4.TRS(p, Quaternion.Euler(0, 0, am * Mathf.Rad2Deg - 90f), Vector3.one);
                KitPrims.BevelBox(k.Lit, tr * Matrix4x4.Translate(new Vector3(0, -pierW * 0.5f, 0)), new Vector3(arcLen - 0.03f, pierW, depth), 0.035f, col, 0.95f, 0.75f);
            }
            // Fallen voussoirs at the foot, moss and ivy.
            for (int i = 0; i < 2; i++)
            {
                Vector3 p = new Vector3(rng.Range(-span * 0.4f, span * 0.6f), 0.02f, rng.Signed() * 0.6f);
                KitPrims.BevelBox(k.Lit, Matrix4x4.TRS(p, Quaternion.Euler(rng.Signed() * 20f, rng.Value() * 180f, rng.Signed() * 15f), Vector3.one), new Vector3(0.55f, 0.32f, 0.5f), 0.04f, Palette.Jitter(SandstoneDark, 0.06f, ref rng), 0.9f, 0.4f);
            }
            var ivy = KitFoliage.WildMass(seed + 3, 0.55f);
            k.Foliage.Append(ivy.Foliage, Matrix4x4.TRS(new Vector3(-(span * 0.5f + pierW * 0.5f), pierH + R * 0.6f, 0), Quaternion.identity, new Vector3(0.9f, 0.6f, 0.9f)));
            var foot = KitFoliage.WildMass(seed + 9, 0.7f);
            k.Foliage.Append(foot.Foliage, Matrix4x4.TRS(new Vector3(span * 0.5f + pierW * 0.7f, 0, 0.2f), Quaternion.identity, Vector3.one));
            return k;
        }

        /// <summary>
        /// One bay of a cloister arcade springing from two column capitals: a ring of
        /// voussoirs and a weathered beam above. Span is centre to centre along local X; the
        /// columns themselves are separate posts. Decorative only (it stands above the ball).
        /// </summary>
        public static KitMesh Arcade(int seed, float span, float spring = 2.62f, float depth = 0.46f)
        {
            var k = new KitMesh();
            var rng = new DetRandom((ulong)(seed * 4099 + 77));
            float R = span * 0.5f - 0.26f;
            const float ring = 0.3f;
            int stones = 9;
            for (int i = 0; i < stones; i++)
            {
                float a0 = Mathf.PI * i / stones, a1 = Mathf.PI * (i + 1) / stones;
                float am = (a0 + a1) * 0.5f;
                Vector3 p = new Vector3(Mathf.Cos(am) * (R + ring * 0.5f), spring + Mathf.Sin(am) * (R + ring * 0.5f), 0);
                float arcLen = (R + ring * 0.5f) * (a1 - a0);
                Color col = Palette.Jitter(Color.Lerp(SandstoneDark, Sandstone, rng.Value()), 0.06f, ref rng);
                var tr = Matrix4x4.TRS(p, Quaternion.Euler(0, 0, am * Mathf.Rad2Deg - 90f), Vector3.one);
                KitPrims.BevelBox(k.Lit, tr * Matrix4x4.Translate(new Vector3(0, -ring * 0.5f, 0)), new Vector3(arcLen - 0.025f, ring, depth), 0.03f, col, 0.95f, 0.7f);
            }
            // Spandrel blocks and the beam: a little broken along its length.
            float beamY = spring + R + ring + 0.02f;
            for (int side = -1; side <= 1; side += 2)
            {
                KitPrims.BevelBox(k.Lit, Matrix4x4.Translate(new Vector3(side * (span * 0.5f - 0.12f), spring, 0)), new Vector3(0.5f, beamY - spring, depth - 0.04f), 0.03f, Palette.Jitter(Sandstone, 0.05f, ref rng), 0.95f, 0.75f);
            }
            int pieces = 3;
            float pl = span / pieces;
            for (int i = 0; i < pieces; i++)
            {
                if (i == 2 && rng.Chance(0.4f)) continue;
                float x = -span * 0.5f + pl * (i + 0.5f);
                KitPrims.BevelBox(k.Lit, Matrix4x4.TRS(new Vector3(x, beamY + rng.Signed() * 0.015f, 0), Quaternion.Euler(0, 0, rng.Signed() * 1.2f), Vector3.one), new Vector3(pl - 0.03f, 0.24f, depth + 0.06f), 0.035f, Palette.Jitter(Coping, 0.05f, ref rng), 1f, 0.8f);
            }
            // Ivy hanging off one end.
            if (rng.Chance(0.7f))
            {
                var ivy = KitFoliage.WildMass(seed + 13, 0.45f);
                float sx = rng.Chance(0.5f) ? -1f : 1f;
                k.Foliage.Append(ivy.Foliage, Matrix4x4.TRS(new Vector3(sx * span * 0.32f, beamY + 0.1f, 0), Quaternion.identity, new Vector3(1.0f, 0.7f, 0.8f)));
            }
            return k;
        }

        public static KitMesh Steps(int seed, int count, float width, float rise, float run)
        {
            var k = new KitMesh();
            var rng = new DetRandom((ulong)(seed * 19 + 1));
            for (int i = 0; i < count; i++)
            {
                Color c = Palette.Jitter(Coping, 0.06f, ref rng);
                KitPrims.BevelBox(k.Lit, Matrix4x4.Translate(new Vector3(0, i * rise - 0.02f, -i * run)), new Vector3(width, rise + 0.02f, run * (count - i)), 0.03f, c, 0.95f, 0.6f);
            }
            return k;
        }
    }
}
