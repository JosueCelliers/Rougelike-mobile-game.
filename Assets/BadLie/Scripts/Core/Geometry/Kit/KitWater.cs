using System.Collections.Generic;
using BadLie.Core;
using UnityEngine;

namespace BadLie.Geometry
{
    /// <summary>Waterside and ground-litter pieces: lily pads, reeds, ruins in the flood, leaf litter.</summary>
    public static class KitWater
    {
        public static KitMesh LilyPads(int seed, float radius = 1.2f)
        {
            var k = new KitMesh();
            var rng = new DetRandom((ulong)(seed * 131 + 5));
            int n = 5 + rng.Range(0, 5);
            for (int i = 0; i < n; i++)
            {
                Vector2 c = rng.InCircle() * radius;
                float r = rng.Range(0.16f, 0.32f);
                float notch = rng.Value() * 360f;
                Color col = Palette.Jitter(Palette.Hex("#4d6b33"), 0.12f, ref rng);
                Pad(k.Foliage, new Vector3(c.x, 0.012f, c.y), r, notch, col);
                if (rng.Chance(0.18f))
                {
                    // A pale lily bloom.
                    for (int p = 0; p < 6; p++)
                    {
                        float a = p / 6f * Mathf.PI * 2f;
                        Vector3 d = new Vector3(Mathf.Cos(a), 0.6f, Mathf.Sin(a)).normalized;
                        KitPrims.Flake(k.Foliage, new Vector3(c.x, 0.03f, c.y), (d + Vector3.up).normalized, d, Vector3.up, 0.09f, 0.06f, Palette.Hex("#f1e6dc"), 1f, 0.1f, rng.Value());
                    }
                }
            }
            return k;
        }

        static void Pad(MeshData m, Vector3 c, float r, float notchDeg, Color col)
        {
            const int segs = 12;
            int centre = m.Add(c + Vector3.up * 0.01f, Vector3.up, col, new Vector4(0.95f, 0.05f, 0.5f, 0));
            var ring = new int[segs + 1];
            for (int i = 0; i <= segs; i++)
            {
                float a = notchDeg * Mathf.Deg2Rad + 0.35f + i / (float)segs * (Mathf.PI * 2f - 0.7f);
                ring[i] = m.Add(c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r, Vector3.up, Palette.Mul(col, 0.92f), new Vector4(0.9f, 0.05f, 0.5f, 0));
            }
            for (int i = 0; i < segs; i++) m.Tri(centre, ring[i + 1], ring[i]);
        }

        public static KitMesh Reeds(int seed, float height = 0.9f)
        {
            var k = new KitMesh();
            var rng = new DetRandom((ulong)(seed * 17 + 2));
            int n = 9 + rng.Range(0, 7);
            for (int i = 0; i < n; i++)
            {
                Vector3 b = new Vector3(rng.Signed() * 0.35f, 0, rng.Signed() * 0.35f);
                Vector3 d = new Vector3(rng.Signed() * 0.25f, 1f, rng.Signed() * 0.25f).normalized;
                float h = height * rng.Range(0.6f, 1.15f);
                Vector3 side = Vector3.Cross(d, Vector3.right).normalized;
                Vector3 outward = Vector3.Cross(side, d).normalized;
                Color c = Palette.Jitter(Color.Lerp(Palette.Hex("#6f7a3c"), Palette.Hex("#a59a58"), rng.Value() * 0.6f), 0.1f, ref rng);
                KitPrims.Flake(k.Foliage, b, outward, d, (outward + Vector3.up * 0.6f).normalized, h, 0.05f, c, 0.7f, 1.2f, rng.Value());
                if (rng.Chance(0.3f))
                {
                    // Seed head.
                    var top = b + d * h * 0.85f;
                    KitPrims.Tube(k.Foliage, new List<Vector3> { top, top + d * 0.14f }, new List<float> { 0.025f, 0.018f }, 5, Palette.Hex("#5a3b2a"), 0.9f, 1f, 1.2f);
                }
            }
            return k;
        }

        /// <summary>A broken length of wall standing in the flood.</summary>
        public static KitMesh RuinWall(int seed, float length = 3.4f, float height = 2.2f)
        {
            var k = new KitMesh();
            var rng = new DetRandom((ulong)(seed * 977 + 13));
            var path = new List<Vector2> { new Vector2(-length * 0.5f, 0), new Vector2(length * 0.5f, 0) };
            float course = 0.32f;
            int rows = Mathf.RoundToInt(height / course);
            for (int r = 0; r < rows; r++)
            {
                float s = (r % 2) * -0.3f;
                // Ragged top: rows shorten toward a broken edge.
                float limit = length * Mathf.Lerp(1f, 0.35f, Mathf.Clamp01((r - rows * 0.4f) / (rows * 0.6f))) + rng.Signed() * 0.3f;
                while (s < limit)
                {
                    float bl = rng.Range(0.45f, 0.75f);
                    float s0 = Mathf.Max(0f, s), s1 = Mathf.Min(limit, s + bl);
                    s += bl;
                    if (s1 - s0 < 0.15f) continue;
                    Vector3 p = new Vector3(-length * 0.5f + (s0 + s1) * 0.5f, r * course, 0);
                    Color c = Palette.Jitter(Color.Lerp(KitStone.SandstoneDark, KitStone.Sandstone, rng.Value()), 0.08f, ref rng);
                    if (r < 2) c = Color.Lerp(c, Palette.Hex("#556b4a"), 0.35f); // waterline algae
                    KitPrims.BevelBox(k.Lit, Matrix4x4.Translate(p + new Vector3(0, 0, rng.Signed() * 0.02f)), new Vector3(s1 - s0 - 0.02f, course - 0.02f, 0.55f), 0.04f, c, Mathf.Lerp(0.45f, 1f, r / (float)rows), Mathf.Lerp(0.35f, 0.9f, r / (float)rows));
                }
            }
            var ivy = KitFoliage.WildMass(seed + 1, 0.6f);
            k.Foliage.Append(ivy.Foliage, Matrix4x4.TRS(new Vector3(-length * 0.25f, height * 0.75f, 0), Quaternion.identity, new Vector3(1f, 0.7f, 0.8f)));
            return k;
        }

        /// <summary>Fallen autumn leaves scattered flat on the ground.</summary>
        public static KitMesh LeafLitter(int seed, float radius = 1.4f, bool red = false)
        {
            var k = new KitMesh();
            var rng = new DetRandom((ulong)(seed * 29 + 9));
            int n = Mathf.RoundToInt(radius * radius * 18f);
            for (int i = 0; i < n; i++)
            {
                Vector2 c = rng.InCircle() * radius;
                if (rng.Value() > 1.1f - c.magnitude / radius) continue;
                Color col = Palette.Jitter(red ? KitFoliage.Rust : Color.Lerp(KitFoliage.Amber, KitFoliage.Saffron, rng.Value()), 0.15f, ref rng);
                Vector3 dir = new Vector3(rng.Signed(), 0, rng.Signed());
                KitPrims.Flake(k.Foliage, new Vector3(c.x, 0.015f + rng.Value() * 0.01f, c.y), Vector3.up, dir, Vector3.up, 0.12f, 0.08f, col, 0.95f, 0f, rng.Value());
            }
            return k;
        }

        /// <summary>Square plinth/slab partly sunk in water: stepping stones of the flooded estate.</summary>
        public static KitMesh SunkenSlabs(int seed)
        {
            var k = new KitMesh();
            var rng = new DetRandom((ulong)(seed * 41 + 7));
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = new Vector3(i * 1.1f + rng.Signed() * 0.2f, -0.25f + rng.Value() * 0.18f, rng.Signed() * 0.4f);
                KitPrims.BevelBox(k.Lit, Matrix4x4.TRS(p, Quaternion.Euler(rng.Signed() * 6f, rng.Value() * 40f, rng.Signed() * 6f), Vector3.one), new Vector3(0.8f, 0.5f, 0.8f), 0.05f, Palette.Jitter(KitStone.Coping, 0.08f, ref rng), 0.95f, 0.4f);
            }
            return k;
        }
    }
}
