using System.Collections.Generic;
using BadLie.Core;
using BadLie.Course;
using UnityEngine;

namespace BadLie.Geometry
{
    /// <summary>Builds the meshes of a hole's authored walls, posts and blocks.</summary>
    public static class CourseDressing
    {
        public static KitMesh BuildObstacles(CourseModel course)
        {
            var all = new KitMesh();
            System.Func<Vector2, float> groundFn = p => course.HeightAt(p);
            var def = course.Def;
            for (int i = 0; i < def.Walls.Count; i++)
            {
                WallDef w = def.Walls[i];
                if (w.Path.Count < 2) continue;
                var path = w.Closed ? Closed(w.Path) : w.Path;
                KitMesh k = null;
                float fixedBase = w.BaseY;
                System.Func<Vector2, float> ground = float.IsNaN(fixedBase) ? groundFn : (p => fixedBase);
                switch (w.Style)
                {
                    case WallStyle.Sandstone: k = KitStone.SandstoneWall(path, w.Height, w.Thickness, ground, w.Seed); break;
                    case WallStyle.Ruin: k = KitStone.SandstoneWall(path, w.Height, w.Thickness, ground, w.Seed, true); break;
                    case WallStyle.Balustrade: k = KitStone.Balustrade(path, w.Height, ground, w.Seed); break;
                    case WallStyle.Curb: k = KitStone.Curb(path, w.Height, w.Thickness, ground, w.Seed); break;
                    case WallStyle.Coping: k = KitStone.CopingWall(path, w.Height, w.Thickness, ground, w.Seed); break;
                    case WallStyle.Hedge: k = HedgeAlong(path, w.Height, w.Thickness, ground, w.Seed); break;
                    case WallStyle.Machine: k = MachineWall(path, w.Height, w.Thickness, ground, w.Seed); break;
                }
                if (k != null) all.Append(k, Matrix4x4.identity);
            }
            for (int i = 0; i < def.Posts.Count; i++)
            {
                PostDef p = def.Posts[i];
                float y = float.IsNaN(p.BaseY) ? course.HeightAt(p.Center) : p.BaseY;
                var tr = Matrix4x4.Translate(new Vector3(p.Center.x, y, p.Center.y));
                KitMesh k;
                switch (p.Style)
                {
                    case PostStyle.Bollard: k = KitMachine.LampPost(p.Seed, p.Height); break;
                    case PostStyle.BrokenColumn: k = KitStone.Column(p.Seed, p.Height, true); break;
                    case PostStyle.Trunk: k = KitFoliage.Tree(p.Seed, KitFoliage.Amber, 5.2f, 2.1f); break;
                    case PostStyle.Urn: k = KitStone.UrnOnPlinth(p.Seed); break;
                    case PostStyle.Topiary: k = KitFoliage.TopiaryCone(p.Seed, p.Height, p.Radius * 1.25f); break;
                    default: k = KitStone.Column(p.Seed, p.Height); break;
                }
                all.Append(k, tr);
            }
            for (int i = 0; i < def.Blocks.Count; i++)
            {
                BlockDef b = def.Blocks[i];
                float y = float.IsNaN(b.BaseY) ? course.HeightAt(b.Box.Center) : b.BaseY;
                Vector2 size = b.Box.HalfSize * 2f;
                var tr = Matrix4x4.TRS(new Vector3(b.Box.Center.x, y, b.Box.Center.y), Quaternion.Euler(0, -b.Box.AngleDeg, 0), Vector3.one);
                var k = new KitMesh();
                switch (b.Style)
                {
                    case BlockStyle.Hedge:
                        KitFoliage.AddHedgeBlock(k.Foliage, Matrix4x4.identity, new Vector3(size.x, b.Height, size.y), b.Seed, 0.25f);
                        break;
                    case BlockStyle.Machine:
                        KitPrims.BevelBox(k.Lit, Matrix4x4.identity, new Vector3(size.x, b.Height, size.y), 0.14f, KitMachine.Iron, 0.9f, 0.4f);
                        KitPrims.BevelBox(k.Glow, Matrix4x4.Translate(new Vector3(0, b.Height * 0.7f, -size.y * 0.5f - 0.005f)), new Vector3(size.x * 0.5f, 0.04f, 0.02f), 0.01f, Color.white, 1, 1);
                        break;
                    default:
                        KitPrims.BevelBox(k.Lit, Matrix4x4.identity, new Vector3(size.x, b.Height, size.y), 0.05f, KitStone.Sandstone, 0.95f, 0.45f);
                        break;
                }
                all.Append(k, tr);
            }
            return all;
        }

        static List<Vector2> Closed(List<Vector2> path)
        {
            var l = new List<Vector2>(path);
            l.Add(path[0]);
            return l;
        }

        /// <summary>Clipped hedge running along a path, split into blocks with small gaps.</summary>
        public static KitMesh HedgeAlong(IList<Vector2> path, float height, float thickness, System.Func<Vector2, float> ground, int seed)
        {
            var k = new KitMesh();
            var rng = new DetRandom((ulong)(seed * 41 + 3));
            float len = Geo2D.PathLength(path);
            float s = 0f;
            int idx = 0;
            while (s < len - 0.2f)
            {
                float bl = Mathf.Min(rng.Range(1.8f, 2.8f), len - s);
                Vector2 p, t;
                KitStone.Along(path, s + bl * 0.5f, out p, out t);
                float yaw = -Mathf.Atan2(t.y, t.x) * Mathf.Rad2Deg;
                var tr = Matrix4x4.TRS(new Vector3(p.x, ground(p) - 0.03f, p.y), Quaternion.Euler(0, yaw, 0), Vector3.one);
                bool wild = rng.Chance(0.35f);
                KitFoliage.AddHedgeBlock(k.Foliage, tr, new Vector3(bl - 0.06f, height + 0.03f, thickness), seed * 13 + idx, wild ? 0.7f : 0.15f);
                s += bl;
                idx++;
            }
            return k;
        }

        static KitMesh MachineWall(IList<Vector2> path, float height, float thickness, System.Func<Vector2, float> ground, int seed)
        {
            var k = new KitMesh();
            float len = Geo2D.PathLength(path);
            float s = 0f;
            while (s < len - 0.05f)
            {
                float bl = Mathf.Min(1.6f, len - s);
                Vector2 p, t;
                KitStone.Along(path, s + bl * 0.5f, out p, out t);
                float yaw = -Mathf.Atan2(t.y, t.x) * Mathf.Rad2Deg;
                var tr = Matrix4x4.TRS(new Vector3(p.x, ground(p) - 0.03f, p.y), Quaternion.Euler(0, yaw, 0), Vector3.one);
                KitPrims.BevelBox(k.Lit, tr, new Vector3(bl - 0.04f, height, thickness), 0.08f, KitMachine.Iron, 0.9f, 0.45f);
                KitPrims.BevelBox(k.Glow, tr * Matrix4x4.Translate(new Vector3(0, height * 0.75f, 0)), new Vector3(bl * 0.4f, 0.035f, thickness + 0.01f), 0.01f, Color.white, 1, 1);
                s += bl;
            }
            return k;
        }
    }
}
