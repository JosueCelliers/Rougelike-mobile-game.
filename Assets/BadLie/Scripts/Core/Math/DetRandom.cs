using UnityEngine;

namespace BadLie.Core
{
    /// <summary>
    /// Small deterministic PRNG (PCG32). Used for course selection, upgrade offers and
    /// procedural decoration. Never used by the shot simulation, which has no randomness.
    /// </summary>
    public struct DetRandom
    {
        ulong state;
        ulong inc;

        public DetRandom(ulong seed, ulong stream = 54u)
        {
            state = 0u;
            inc = (stream << 1) | 1u;
            NextUInt();
            state += seed;
            NextUInt();
        }

        public static DetRandom FromString(string s, ulong salt = 0)
        {
            return new DetRandom(Hash.Fnv1a64(s) ^ salt);
        }

        public uint NextUInt()
        {
            ulong old = state;
            state = old * 6364136223846793005UL + inc;
            uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);
            int rot = (int)(old >> 59);
            return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
        }

        /// <summary>[0, 1)</summary>
        public float Value() { return (NextUInt() >> 8) * (1f / 16777216f); }

        public float Range(float min, float max) { return min + (max - min) * Value(); }

        /// <summary>[min, max)</summary>
        public int Range(int min, int max)
        {
            if (max <= min) return min;
            return min + (int)(NextUInt() % (uint)(max - min));
        }

        public float Signed() { return Value() * 2f - 1f; }

        public Vector2 InCircle()
        {
            float a = Value() * Mathf.PI * 2f;
            float r = Mathf.Sqrt(Value());
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }

        public Vector3 OnSphere()
        {
            float z = Signed();
            float a = Value() * Mathf.PI * 2f;
            float r = Mathf.Sqrt(Mathf.Max(0f, 1f - z * z));
            return new Vector3(r * Mathf.Cos(a), r * Mathf.Sin(a), z);
        }

        public bool Chance(float p) { return Value() < p; }
    }

    public static class Hash
    {
        public static ulong Fnv1a64(string s)
        {
            ulong h = 14695981039346656037UL;
            for (int i = 0; i < s.Length; i++)
            {
                h ^= s[i];
                h *= 1099511628211UL;
            }
            return h;
        }

        public static uint U32(int x, int y, int seed = 0)
        {
            unchecked
            {
                uint h = (uint)x * 0x8da6b343u ^ (uint)y * 0xd8163841u ^ (uint)seed * 0xcb1ab31fu;
                h ^= h >> 13;
                h *= 0x5bd1e995u;
                h ^= h >> 15;
                return h;
            }
        }

        public static float Float01(int x, int y, int seed = 0) { return (U32(x, y, seed) >> 8) * (1f / 16777216f); }
    }

    /// <summary>Deterministic value noise for procedural generation (not the engine's Perlin).</summary>
    public static class Noise
    {
        public static float Value2(float x, float y, int seed = 0)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            float ux = fx * fx * (3f - 2f * fx), uy = fy * fy * (3f - 2f * fy);
            float a = Hash.Float01(xi, yi, seed), b = Hash.Float01(xi + 1, yi, seed);
            float c = Hash.Float01(xi, yi + 1, seed), d = Hash.Float01(xi + 1, yi + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, ux), Mathf.Lerp(c, d, ux), uy);
        }

        /// <summary>Fractal value noise in [0,1].</summary>
        public static float Fbm2(float x, float y, int octaves, int seed = 0)
        {
            float sum = 0f, amp = 0.5f, norm = 0f, f = 1f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Value2(x * f, y * f, seed + i * 31) * amp;
                norm += amp;
                amp *= 0.5f;
                f *= 2.03f;
            }
            return sum / norm;
        }

        public static float Value3(Vector3 p, int seed = 0)
        {
            int xi = Mathf.FloorToInt(p.x), yi = Mathf.FloorToInt(p.y), zi = Mathf.FloorToInt(p.z);
            float fx = p.x - xi, fy = p.y - yi, fz = p.z - zi;
            float ux = fx * fx * (3f - 2f * fx), uy = fy * fy * (3f - 2f * fy), uz = fz * fz * (3f - 2f * fz);
            float c000 = Hash.Float01(xi, yi * 7919 + zi, seed), c100 = Hash.Float01(xi + 1, yi * 7919 + zi, seed);
            float c010 = Hash.Float01(xi, (yi + 1) * 7919 + zi, seed), c110 = Hash.Float01(xi + 1, (yi + 1) * 7919 + zi, seed);
            float c001 = Hash.Float01(xi, yi * 7919 + zi + 1, seed), c101 = Hash.Float01(xi + 1, yi * 7919 + zi + 1, seed);
            float c011 = Hash.Float01(xi, (yi + 1) * 7919 + zi + 1, seed), c111 = Hash.Float01(xi + 1, (yi + 1) * 7919 + zi + 1, seed);
            float x00 = Mathf.Lerp(c000, c100, ux), x10 = Mathf.Lerp(c010, c110, ux);
            float x01 = Mathf.Lerp(c001, c101, ux), x11 = Mathf.Lerp(c011, c111, ux);
            return Mathf.Lerp(Mathf.Lerp(x00, x10, uy), Mathf.Lerp(x01, x11, uy), uz);
        }

        public static float Fbm3(Vector3 p, int octaves, int seed = 0)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Value3(p, seed + i * 17) * amp;
                norm += amp;
                amp *= 0.5f;
                p *= 2.03f;
            }
            return sum / norm;
        }
    }
}
