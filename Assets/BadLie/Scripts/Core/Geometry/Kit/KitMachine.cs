using System.Collections.Generic;
using BadLie.Core;
using UnityEngine;

namespace BadLie.Geometry
{
    /// <summary>
    /// The estate's old machinery (COCOON language): heavy dark housings set into the stone,
    /// bevelled rims, radial seams and a few pale lights. Plus lamp posts and far structures.
    /// </summary>
    public static class KitMachine
    {
        public static readonly Color Iron = Palette.Hex("#1f1c1e");
        public static readonly Color Bronze = Palette.Hex("#3a2e26");
        public static readonly Color Rim = Palette.Hex("#4a3d34");

        public static KitMesh LampPost(int seed, float height = 1.6f)
        {
            var k = new KitMesh();
            var rng = new DetRandom((ulong)(seed * 3 + 7));
            KitPrims.BevelBox(k.Lit, Matrix4x4.identity, new Vector3(0.42f, 0.32f, 0.42f), 0.04f, Palette.Jitter(KitStone.Coping, 0.05f, ref rng), 0.95f, 0.45f);
            var pole = new List<Vector2> { new Vector2(0.07f, 0.32f), new Vector2(0.05f, 0.42f), new Vector2(0.04f, height - 0.42f), new Vector2(0.06f, height - 0.38f) };
            KitPrims.Lathe(k.Lit, Matrix4x4.identity, pole, 8, Iron, 0.5f, 0.9f, false);
            float ly = height - 0.38f;
            // Cage posts.
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                Vector3 off = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.1f;
                KitPrims.BevelBox(k.Lit, Matrix4x4.Translate(new Vector3(off.x, ly, off.z)), new Vector3(0.02f, 0.3f, 0.02f), 0.004f, Iron, 0.9f, 0.8f);
            }
            var roof = new List<Vector2> { new Vector2(0.15f, ly + 0.3f), new Vector2(0.13f, ly + 0.34f), new Vector2(0.03f, ly + 0.44f), new Vector2(0.0f, ly + 0.5f) };
            KitPrims.Lathe(k.Lit, Matrix4x4.identity, roof, 8, Iron, 0.95f, 1f, false);
            var glass = new List<Vector2> { new Vector2(0.06f, ly + 0.02f), new Vector2(0.085f, ly + 0.12f), new Vector2(0.085f, ly + 0.2f), new Vector2(0.05f, ly + 0.29f) };
            KitPrims.Lathe(k.Glow, Matrix4x4.identity, glass, 8, Color.white, 1f, 1f, true);
            return k;
        }

        /// <summary>Sluice gate housing sunk into a stone bank, with slot lights.</summary>
        public static KitMesh SluiceGate(int seed, float width = 2.2f)
        {
            var k = new KitMesh();
            var rng = new DetRandom((ulong)(seed * 101 + 1));
            // Stone surround.
            KitPrims.BevelBox(k.Lit, Matrix4x4.Translate(new Vector3(0, 0, 0.35f)), new Vector3(width + 0.8f, 1.5f, 0.6f), 0.06f, Palette.Jitter(KitStone.Sandstone, 0.05f, ref rng), 0.95f, 0.45f);
            // Rounded dark housing with a chunky rim.
            KitPrims.BevelBox(k.Lit, Matrix4x4.Translate(new Vector3(0, 0.2f, 0.05f)), new Vector3(width, 1.15f, 0.5f), 0.16f, Iron, 0.9f, 0.4f);
            KitPrims.BevelBox(k.Lit, Matrix4x4.Translate(new Vector3(0, 1.3f, 0.05f)), new Vector3(width + 0.2f, 0.18f, 0.62f), 0.07f, Rim, 1f, 0.8f);
            // Vertical seams and slot lights.
            int slots = 5;
            for (int i = 0; i < slots; i++)
            {
                float x = Mathf.Lerp(-width * 0.38f, width * 0.38f, i / (float)(slots - 1));
                KitPrims.BevelBox(k.Lit, Matrix4x4.Translate(new Vector3(x, 0.3f, -0.21f)), new Vector3(0.05f, 0.9f, 0.04f), 0.01f, Rim, 0.8f, 0.6f);
                KitPrims.BevelBox(k.Glow, Matrix4x4.Translate(new Vector3(x + 0.12f, 0.95f, -0.22f)), new Vector3(0.1f, 0.05f, 0.03f), 0.01f, Color.white, 1f, 1f);
            }
            // A half-sunk gear wheel on one side.
            Gear(k.Lit, Matrix4x4.TRS(new Vector3(width * 0.5f + 0.5f, 0.15f, 0.0f), Quaternion.Euler(0, 0, 0), Vector3.one), 0.7f, 12, Bronze);
            return k;
        }

        public static void Gear(MeshData m, Matrix4x4 tr, float radius, int teeth, Color c)
        {
            // Disc body plus teeth, standing upright in the XY plane.
            var body = new List<Vector2> { new Vector2(0f, -0.08f), new Vector2(radius * 0.85f, -0.08f), new Vector2(radius * 0.85f, 0.08f), new Vector2(0f, 0.08f) };
            KitPrims.Lathe(m, tr * Matrix4x4.Rotate(Quaternion.Euler(90, 0, 0)), new List<Vector2> { new Vector2(radius * 0.85f, -0.08f), new Vector2(radius * 0.85f, 0.08f) }, 24, c, 0.6f, 0.9f, true);
            KitPrims.Lathe(m, tr * Matrix4x4.Rotate(Quaternion.Euler(-90, 0, 0)), new List<Vector2> { new Vector2(radius * 0.85f, -0.08f), new Vector2(radius * 0.85f, 0.07f) }, 24, c, 0.6f, 0.9f, true);
            for (int i = 0; i < teeth; i++)
            {
                float a = i * 360f / teeth;
                var t = tr * Matrix4x4.Rotate(Quaternion.Euler(0, 0, a)) * Matrix4x4.Translate(new Vector3(0, radius * 0.82f, -0.07f));
                KitPrims.BevelBox(m, t * Matrix4x4.Rotate(Quaternion.Euler(-90, 0, 0)), new Vector3(radius * 0.2f, 0.14f, radius * 0.22f), 0.02f, c, 0.8f, 0.8f);
            }
        }

        /// <summary>Round machine set flush into the ground: dark disc, bevelled rim, radial seams, pale lights.</summary>
        public static KitMesh GroundMachine(int seed, float radius = 1.4f)
        {
            var k = new KitMesh();
            var rim = new List<Vector2> { new Vector2(radius, -0.1f), new Vector2(radius, 0.03f), new Vector2(radius - 0.08f, 0.07f), new Vector2(radius - 0.22f, 0.07f), new Vector2(radius - 0.26f, 0.02f) };
            KitPrims.Lathe(k.Lit, Matrix4x4.identity, rim, 32, Rim, 0.7f, 1f, false);
            var disc = new List<Vector2> { new Vector2(radius - 0.26f, 0.02f), new Vector2(0.3f, 0.0f), new Vector2(0.0f, 0.0f) };
            KitPrims.Lathe(k.Lit, Matrix4x4.identity, disc, 32, Iron, 0.8f, 0.8f, false);
            int seams = 8;
            for (int i = 0; i < seams; i++)
            {
                float a = i * 360f / seams;
                KitPrims.BevelBox(k.Lit, Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, a, 0), Vector3.one) * Matrix4x4.Translate(new Vector3(radius * 0.55f, 0f, 0f)), new Vector3(radius * 0.7f, 0.03f, 0.035f), 0.008f, Rim, 0.9f, 0.9f);
                KitPrims.BevelBox(k.Glow, Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, a + 22.5f, 0), Vector3.one) * Matrix4x4.Translate(new Vector3(radius - 0.17f, 0.065f, 0f)), new Vector3(0.12f, 0.02f, 0.05f), 0.006f, Color.white, 1f, 1f);
            }
            return k;
        }

        /// <summary>
        /// Concentric orrery rings set flush into a stone dais: segmented dark-metal bands with
        /// bevelled lips and a few pale lights. Purely visual (the dais carries the physics).
        /// </summary>
        public static KitMesh OrreryRings(int seed, float radius = 7.4f)
        {
            var k = new KitMesh();
            var rng = new DetRandom((ulong)(seed * 71 + 3));
            float[] radii = { radius, radius + 1.05f };
            for (int r = 0; r < radii.Length; r++)
            {
                float R = radii[r];
                int segs = Mathf.RoundToInt(R * 5.2f);
                for (int i = 0; i < segs; i++)
                {
                    float a0 = i * 360f / segs, a1 = (i + 1) * 360f / segs;
                    float am = (a0 + a1) * 0.5f;
                    float len = 2f * Mathf.PI * R / segs - 0.06f;
                    var tr = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, -am, 0), Vector3.one) * Matrix4x4.Translate(new Vector3(R, 0f, 0f));
                    KitPrims.BevelBox(k.Lit, tr * Matrix4x4.Rotate(Quaternion.Euler(0, 90, 0)), new Vector3(len, 0.035f, r == 0 ? 0.34f : 0.22f), 0.012f, r == 0 ? Bronze : Iron, 0.9f, 0.9f);
                    if (i % 4 == 0)
                    {
                        KitPrims.BevelBox(k.Glow, tr * Matrix4x4.Translate(new Vector3(0, 0.03f, 0)), new Vector3(0.08f, 0.02f, 0.08f), 0.01f, Color.white, 1, 1);
                    }
                }
            }
            // Radial spokes between the rings.
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f;
                var tr = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, -a, 0), Vector3.one) * Matrix4x4.Translate(new Vector3(radius + 0.52f, 0f, 0f));
                KitPrims.BevelBox(k.Lit, tr, new Vector3(0.9f, 0.03f, 0.08f), 0.01f, Rim, 0.9f, 0.9f);
            }
            return k;
        }

        // ------------------------------------------------------------------ far structures

        /// <summary>A ruined tower softened by distance; detail is coarse on purpose.</summary>
        public static KitMesh Tower(int seed, float height = 12f, float radius = 2.2f)
        {
            var k = new KitMesh();
            var rng = new DetRandom((ulong)(seed * 59 + 2));
            var prof = new List<Vector2>();
            for (int i = 0; i <= 6; i++)
            {
                float t = i / 6f;
                prof.Add(new Vector2(radius * (1.06f - 0.12f * t), t * height));
            }
            KitPrims.Lathe(k.Lit, Matrix4x4.identity, prof, 14, Palette.Jitter(KitStone.Sandstone, 0.06f, ref rng), 0.4f, 1f, true);
            // Broken crown of merlons.
            int merlons = 10;
            for (int i = 0; i < merlons; i++)
            {
                if (rng.Chance(0.3f)) continue;
                float a = i * 360f / merlons;
                var tr = Matrix4x4.TRS(Vector3.up * height, Quaternion.Euler(0, a, 0), Vector3.one) * Matrix4x4.Translate(new Vector3(radius * 0.88f, 0, 0));
                KitPrims.BevelBox(k.Lit, tr, new Vector3(0.5f, rng.Range(0.4f, 0.9f), 0.7f), 0.05f, Palette.Jitter(KitStone.Sandstone, 0.06f, ref rng), 1f, 0.9f);
            }
            // Dark window slots.
            for (int i = 0; i < 4; i++)
            {
                float a = rng.Value() * 360f;
                float y = rng.Range(height * 0.35f, height * 0.85f);
                var tr = Matrix4x4.TRS(Vector3.up * y, Quaternion.Euler(0, a, 0), Vector3.one) * Matrix4x4.Translate(new Vector3(radius * 0.97f, 0, 0));
                KitPrims.BevelBox(k.Lit, tr, new Vector3(0.12f, 1.0f, 0.38f), 0.02f, Palette.Hex("#2a1f22"), 0.3f, 0.3f);
            }
            var ivy = KitFoliage.WildMass(seed, 1.6f);
            k.Foliage.Append(ivy.Foliage, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1.4f, 1.6f, 1.4f)));
            return k;
        }

        public static KitMesh Aqueduct(int seed, int arches = 4, float span = 3f, float height = 5f)
        {
            var k = new KitMesh();
            for (int i = 0; i < arches; i++)
            {
                var a = KitStone.Arch(seed + i, span, height, 1.1f, 0.6f);
                k.Append(a, Matrix4x4.Translate(new Vector3(i * (span + 0.62f), 0, 0)));
            }
            return k;
        }
    }
}
