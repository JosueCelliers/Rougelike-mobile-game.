using System.Collections.Generic;
using BadLie.Course;
using BadLie.Geometry;
using BadLie.Holes;
using NUnit.Framework;
using UnityEngine;

namespace BadLie.Tests
{
    public class GeometryTests
    {
        /// <summary>Fraction of triangles whose winding disagrees with their vertex normals.</summary>
        static float BadWinding(MeshData m, out int tris)
        {
            int bad = 0;
            tris = 0;
            for (int i = 0; i < m.Indices.Count; i += 3)
            {
                int a = m.Indices[i], b = m.Indices[i + 1], c = m.Indices[i + 2];
                Vector3 gn = Vector3.Cross(m.Positions[b] - m.Positions[a], m.Positions[c] - m.Positions[a]);
                if (gn.sqrMagnitude < 1e-12f) continue;
                tris++;
                Vector3 vn = m.Normals[a] + m.Normals[b] + m.Normals[c];
                if (Vector3.Dot(gn, vn) < 0f) bad++;
            }
            return tris == 0 ? 0f : bad / (float)tris;
        }

        static void AssertWinding(string label, MeshData m, float tolerance)
        {
            if (m.IsEmpty) return;
            int tris;
            float bad = BadWinding(m, out tris);
            Assert.Less(bad, tolerance, label + ": " + (bad * 100f).ToString("F1") + "% of " + tris + " triangles face inwards");
            foreach (var p in m.Positions) Assert.IsFalse(float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z), label + " has NaN vertices");
        }

        [Test]
        public void KitPiecesFaceOutwards()
        {
            foreach (var info in KitRegistry.All)
            {
                for (int v = 0; v < info.Variants; v++)
                {
                    var k = info.Build(v * 101 + 7);
                    AssertWinding(info.Name + " lit", k.Lit, 0.03f);
                    AssertWinding(info.Name + " foliage", k.Foliage, 0.06f);
                    AssertWinding(info.Name + " glow", k.Glow, 0.03f);
                }
            }
        }

        [Test]
        public void HoleTerrainFacesOutwards()
        {
            for (int h = 0; h < HoleLibrary.Available; h++)
            {
                var course = new CourseModel(HoleLibrary.Get(h));
                var mesher = new TerrainMesher(course, null);
                foreach (var pad in course.Pads)
                {
                    if (pad.Water)
                    {
                        AssertWinding("water " + pad.Def.Id, mesher.BuildWater(pad), 0.001f);
                        continue;
                    }
                    var top = mesher.BuildPadTop(pad, true);
                    Assert.IsFalse(top.IsEmpty, "pad " + pad.Def.Id + " produced no surface");
                    AssertWinding("top " + pad.Def.Id, top, 0.001f);
                    AssertWinding("sides " + pad.Def.Id, mesher.BuildPadSides(pad), 0.02f);
                }
                AssertWinding("cup", mesher.BuildCupLiner(), 0.01f);
                var walls = CourseDressing.BuildObstacles(course);
                AssertWinding("walls lit", walls.Lit, 0.03f);
                AssertWinding("walls foliage", walls.Foliage, 0.06f);
            }
        }

        [Test]
        public void TerrainTopMatchesPhysicsHeights()
        {
            var course = new CourseModel(HoleLibrary.Get(0));
            var mesher = new TerrainMesher(course, null);
            foreach (var pad in course.Pads)
            {
                if (pad.Water) continue;
                var top = mesher.BuildPadTop(pad, false);
                foreach (var p in top.Positions)
                {
                    float h = pad.HeightAt(new Vector2(p.x, p.z));
                    Assert.AreEqual(h, p.y, 1e-4f, "vertex height mismatch on " + pad.Def.Id);
                }
            }
        }

        [Test]
        public void CupSitsOnAGreen()
        {
            for (int h = 0; h < HoleLibrary.Available; h++)
            {
                var def = HoleLibrary.Get(h);
                var cups = new List<Vector2> { def.Cup };
                cups.AddRange(def.AltCups);
                for (int i = 0; i < cups.Count; i++)
                {
                    var course = new CourseModel(def, null, i - 1);
                    var g = course.Ground(course.Cup);
                    Assert.AreEqual(SurfaceType.Green, g.Surface, def.Name + " cup " + i + " is not on the green");
                    Assert.Less(g.Gradient.magnitude, 0.08f, def.Name + " cup " + i + " sits on too steep a slope");
                }
            }
        }

        [Test]
        public void TeeIsOnSolidGround()
        {
            for (int h = 0; h < HoleLibrary.Available; h++)
            {
                var course = new CourseModel(HoleLibrary.Get(h));
                var g = course.Ground(course.Tee);
                Assert.IsFalse(g.IsVoid || g.Water || g.OutOfBounds, course.Def.Name + " tee is not on playable ground");
                Assert.Greater(course.Clearance(course.Tee, course.TeeHeight, 0.14f, 1f), 0.05f, course.Def.Name + " tee overlaps an obstacle");
            }
        }
    }
}
