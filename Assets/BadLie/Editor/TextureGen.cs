using System.IO;
using BadLie.Core;
using UnityEditor;
using UnityEngine;

namespace BadLie.EditorTools
{
    /// <summary>Generates the small tileable data textures the shaders sample.</summary>
    public static class TextureGen
    {
        public const string Dir = "Assets/BadLie/Textures";

        public static void GenerateAll()
        {
            Directory.CreateDirectory(Dir);
            Write("T_Noise.png", NoiseTexture(256), false, true);
            Write("T_Setts.png", SettsTexture(512, 9), false, true);
            Write("T_SoftDot.png", SoftDot(64), true, false);
            Write("T_Ring.png", Ring(128), true, false);
            AssetDatabase.Refresh();
        }

        static void Write(string name, Texture2D tex, bool srgb, bool repeat)
        {
            string path = Dir + "/" + name;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.sRGBTexture = srgb;
            imp.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            imp.mipmapEnabled = true;
            imp.alphaIsTransparency = srgb;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.filterMode = FilterMode.Trilinear;
            imp.anisoLevel = repeat ? 4 : 1;
            imp.SaveAndReimport();
        }

        static float WrapNoise(float x, float y, int period, int seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            float ux = fx * fx * (3 - 2 * fx), uy = fy * fy * (3 - 2 * fy);
            int x0 = ((xi % period) + period) % period, x1 = (x0 + 1) % period;
            int y0 = ((yi % period) + period) % period, y1 = (y0 + 1) % period;
            float a = Hash.Float01(x0, y0, seed), b = Hash.Float01(x1, y0, seed);
            float c = Hash.Float01(x0, y1, seed), d = Hash.Float01(x1, y1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, ux), Mathf.Lerp(c, d, ux), uy);
        }

        static float WrapFbm(float u, float v, int basePeriod, int octaves, int seed)
        {
            float sum = 0, amp = 0.5f, norm = 0;
            int period = basePeriod;
            for (int i = 0; i < octaves; i++)
            {
                sum += WrapNoise(u * period, v * period, period, seed + i * 17) * amp;
                norm += amp;
                amp *= 0.5f;
                period *= 2;
            }
            return sum / norm;
        }

        static float Worley(float u, float v, int cells, int seed, out float f2, out int id)
        {
            float x = u * cells, y = v * cells;
            int cx = Mathf.FloorToInt(x), cy = Mathf.FloorToInt(y);
            float f1 = 9f;
            f2 = 9f;
            id = 0;
            for (int j = -1; j <= 1; j++)
            {
                for (int i = -1; i <= 1; i++)
                {
                    int gx = cx + i, gy = cy + j;
                    int wx = ((gx % cells) + cells) % cells, wy = ((gy % cells) + cells) % cells;
                    float px = gx + 0.12f + 0.76f * Hash.Float01(wx, wy, seed);
                    float py = gy + 0.12f + 0.76f * Hash.Float01(wx, wy, seed + 1);
                    float d = Mathf.Sqrt((px - x) * (px - x) + (py - y) * (py - y));
                    if (d < f1)
                    {
                        f2 = f1;
                        f1 = d;
                        id = wy * cells + wx;
                    }
                    else if (d < f2) f2 = d;
                }
            }
            return f1;
        }

        static Texture2D NoiseTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size, v = y / (float)size;
                    float r = WrapFbm(u, v, 4, 4, 11);
                    float g = WrapFbm(u, v, 6, 4, 23);
                    float f2;
                    int id;
                    float f1 = Worley(u, v, 14, 5, out f2, out id);
                    float b = Mathf.Clamp01(1f - f1 * 1.35f) * 0.75f + Hash.Float01(id, 3, 9) * 0.25f;
                    float a = WrapFbm(u, v, 32, 3, 41);
                    px[y * size + x] = new Color32((byte)(r * 255), (byte)(g * 255), (byte)(b * 255), (byte)(a * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        /// <summary>Stone setts: R,G per-stone randoms, B bevel height, A grout mask.</summary>
        static Texture2D SettsTexture(int size, int cells)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size, v = y / (float)size;
                    float f2;
                    int id;
                    float f1 = Worley(u, v, cells, 77, out f2, out id);
                    float edge = f2 - f1; // 0 on the boundary
                    float grout = 1f - Mathf.Clamp01((edge - 0.045f) / 0.035f);
                    float height = Mathf.Pow(Mathf.Clamp01(edge / 0.32f), 0.6f);
                    // Chipped corners: a little noise on the stone surface.
                    height *= 0.85f + 0.15f * WrapFbm(u, v, 24, 2, 3);
                    float r = Hash.Float01(id, 0, 99);
                    float g = Hash.Float01(id, 1, 99);
                    px[y * size + x] = new Color32((byte)(r * 255), (byte)(g * 255), (byte)(height * 255), (byte)(grout * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        static Texture2D SoftDot(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(a * 1.6f));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        static Texture2D Ring(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.8f) / 0.12f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }
    }
}
