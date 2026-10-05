using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BadLie.Geometry
{
    /// <summary>
    /// Engine-light mesh accumulator used by every procedural builder. Vertex colours carry
    /// albedo (linear) and TEXCOORD0..2 carry shader data (AO, sway, coverage, edge info).
    /// </summary>
    public sealed class MeshData
    {
        public readonly List<Vector3> Positions = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<Color> Colors = new List<Color>();
        public readonly List<Vector4> UV0 = new List<Vector4>();
        public readonly List<Vector4> UV1 = new List<Vector4>();
        public readonly List<Vector4> UV2 = new List<Vector4>();
        public readonly List<int> Indices = new List<int>();
        public bool UseUV1, UseUV2;

        public int Count { get { return Positions.Count; } }
        public bool IsEmpty { get { return Indices.Count == 0; } }

        public int Add(Vector3 p, Vector3 n, Color c, Vector4 uv0)
        {
            Positions.Add(p);
            Normals.Add(n);
            Colors.Add(c);
            UV0.Add(uv0);
            if (UseUV1) UV1.Add(Vector4.zero);
            if (UseUV2) UV2.Add(Vector4.zero);
            return Positions.Count - 1;
        }

        public int Add(Vector3 p, Vector3 n, Color c, Vector4 uv0, Vector4 uv1, Vector4 uv2)
        {
            Positions.Add(p);
            Normals.Add(n);
            Colors.Add(c);
            UV0.Add(uv0);
            UseUV1 = true;
            UseUV2 = true;
            while (UV1.Count < Positions.Count - 1) UV1.Add(Vector4.zero);
            while (UV2.Count < Positions.Count - 1) UV2.Add(Vector4.zero);
            UV1.Add(uv1);
            UV2.Add(uv2);
            return Positions.Count - 1;
        }

        /// <summary>Triangle with Unity's clockwise front-face winding.</summary>
        public void Tri(int a, int b, int c)
        {
            Indices.Add(a);
            Indices.Add(b);
            Indices.Add(c);
        }

        /// <summary>Quad a-b-c-d given clockwise as seen from the front.</summary>
        public void Quad(int a, int b, int c, int d)
        {
            Tri(a, b, c);
            Tri(a, c, d);
        }

        public void Append(MeshData m, Matrix4x4 tr)
        {
            Append(m, tr, Color.white, 0f);
        }

        /// <summary>Appends a transformed copy; tint multiplies colours, aoBias lowers AO.</summary>
        public void Append(MeshData m, Matrix4x4 tr, Color tint, float aoBias)
        {
            int baseIndex = Positions.Count;
            Matrix4x4 nm = tr.inverse.transpose;
            for (int i = 0; i < m.Positions.Count; i++)
            {
                Positions.Add(tr.MultiplyPoint3x4(m.Positions[i]));
                Normals.Add(nm.MultiplyVector(m.Normals[i]).normalized);
                Color c = m.Colors[i];
                Colors.Add(new Color(c.r * tint.r, c.g * tint.g, c.b * tint.b, c.a * tint.a));
                Vector4 uv = m.UV0[i];
                uv.x = Mathf.Clamp01(uv.x - aoBias);
                UV0.Add(uv);
            }
            if (UseUV1 || m.UseUV1)
            {
                UseUV1 = true;
                while (UV1.Count < baseIndex) UV1.Add(Vector4.zero);
                for (int i = 0; i < m.Positions.Count; i++) UV1.Add(m.UseUV1 ? m.UV1[i] : Vector4.zero);
            }
            if (UseUV2 || m.UseUV2)
            {
                UseUV2 = true;
                while (UV2.Count < baseIndex) UV2.Add(Vector4.zero);
                for (int i = 0; i < m.Positions.Count; i++) UV2.Add(m.UseUV2 ? m.UV2[i] : Vector4.zero);
            }
            bool flip = tr.determinant < 0f;
            for (int i = 0; i < m.Indices.Count; i += 3)
            {
                if (flip)
                {
                    Indices.Add(baseIndex + m.Indices[i]);
                    Indices.Add(baseIndex + m.Indices[i + 2]);
                    Indices.Add(baseIndex + m.Indices[i + 1]);
                }
                else
                {
                    Indices.Add(baseIndex + m.Indices[i]);
                    Indices.Add(baseIndex + m.Indices[i + 1]);
                    Indices.Add(baseIndex + m.Indices[i + 2]);
                }
            }
        }

        /// <summary>Area-weighted smooth normals from the triangles.</summary>
        public void RecalculateNormals()
        {
            var acc = new Vector3[Positions.Count];
            for (int i = 0; i < Indices.Count; i += 3)
            {
                int a = Indices[i], b = Indices[i + 1], c = Indices[i + 2];
                Vector3 n = Vector3.Cross(Positions[b] - Positions[a], Positions[c] - Positions[a]);
                acc[a] += n;
                acc[b] += n;
                acc[c] += n;
            }
            for (int i = 0; i < acc.Length; i++)
            {
                Normals[i] = acc[i].sqrMagnitude > 1e-12f ? acc[i].normalized : Vector3.up;
            }
        }

        public Bounds ComputeBounds()
        {
            if (Positions.Count == 0) return new Bounds();
            var b = new Bounds(Positions[0], Vector3.zero);
            for (int i = 1; i < Positions.Count; i++) b.Encapsulate(Positions[i]);
            return b;
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (Positions.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(Positions);
            mesh.SetNormals(Normals);
            mesh.SetColors(Colors);
            mesh.SetUVs(0, UV0);
            if (UseUV1)
            {
                while (UV1.Count < Positions.Count) UV1.Add(Vector4.zero);
                mesh.SetUVs(1, UV1);
            }
            if (UseUV2)
            {
                while (UV2.Count < Positions.Count) UV2.Add(Vector4.zero);
                mesh.SetUVs(2, UV2);
            }
            mesh.SetTriangles(Indices, 0, true);
            return mesh;
        }

        public void Clear()
        {
            Positions.Clear();
            Normals.Clear();
            Colors.Clear();
            UV0.Clear();
            UV1.Clear();
            UV2.Clear();
            Indices.Clear();
            UseUV1 = UseUV2 = false;
        }
    }

    public static class Palette
    {
        /// <summary>sRGB hex to linear colour (vertex colours are not converted by the engine).</summary>
        public static Color Hex(string hex, float a = 1f)
        {
            Color c;
            ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out c);
            c.a = a;
            return c.linear;
        }

        public static Color Jitter(Color c, float amount, ref Core.DetRandom rng)
        {
            float k = 1f + rng.Signed() * amount;
            float hueShift = rng.Signed() * amount * 0.3f;
            return new Color(Mathf.Clamp01(c.r * k * (1f + hueShift)), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k * (1f - hueShift)), c.a);
        }

        public static Color Mul(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, c.a); }
    }
}
