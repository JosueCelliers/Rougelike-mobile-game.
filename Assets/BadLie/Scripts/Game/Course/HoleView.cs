using System.Collections.Generic;
using BadLie.Core;
using BadLie.Course;
using BadLie.Geometry;
using UnityEngine;

namespace BadLie.Game
{
    /// <summary>
    /// Builds every visible part of a hole from its CourseModel: ground slabs, water, the cup,
    /// walls and set dressing. Geometry comes from the same model the ball simulation uses.
    /// </summary>
    public sealed class HoleView : MonoBehaviour
    {
        public CourseModel Course { get; private set; }
        public Transform Flag { get; private set; }
        public Transform FlagCloth { get; private set; }
        public readonly List<Renderer> FoliageRenderers = new List<Renderer>();
        public int Triangles { get; private set; }

        GameConfig cfg;
        static readonly Dictionary<string, Mesh[]> kitCache = new Dictionary<string, Mesh[]>();

        public static HoleView Create(CourseModel course, GameConfig config, Transform parent)
        {
            var go = new GameObject("Hole " + course.Def.Name);
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<HoleView>();
            view.Build(course, config);
            return view;
        }

        void Build(CourseModel course, GameConfig config)
        {
            Course = course;
            cfg = config;
            Atmosphere.SetStripe(course.Def.StripeAngle);

            // Decor placements first: their footprints darken the ground.
            var placements = new List<Placement>();
            CollectDecor(course.Def, placements);
            var casters = new List<AOCaster>();
            foreach (var p in placements)
            {
                if (p.Info.AOStrength <= 0f) continue;
                Vector2 c = new Vector2(p.Position.x, p.Position.z);
                casters.Add(new AOCaster { A = c, B = c, Radius = p.Info.Radius * p.Scale * 0.6f, Strength = p.Info.AOStrength, Falloff = 0.3f + 0.25f * p.Info.Radius * p.Scale });
            }

            var mesher = new TerrainMesher(course, casters);
            var ground = new GameObject("Ground").transform;
            ground.SetParent(transform, false);
            foreach (var pad in course.Pads)
            {
                if (pad.Water)
                {
                    AddMesh(ground, "Water " + pad.Def.Id, mesher.BuildWater(pad), cfg.Water, false, true);
                    continue;
                }
                var top = mesher.BuildPadTop(pad, true);
                var sides = mesher.BuildPadSides(pad);
                AddMesh(ground, "Pad " + pad.Def.Id, top, cfg.Terrain, true, true);
                AddMesh(ground, "Pad " + pad.Def.Id + " faces", sides, cfg.Terrain, true, true);
            }
            AddMesh(ground, "Lower Estate Water", mesher.BuildLowerWater(70f, 6f), cfg.LowerWater, false, true);
            AddMesh(ground, "Cup", mesher.BuildCupLiner(), cfg.Lit, false, true);

            var obstacles = new GameObject("Obstacles").transform;
            obstacles.SetParent(transform, false);
            AddKit(obstacles, "Walls", CourseDressing.BuildObstacles(course), Matrix4x4.identity);

            var decor = new GameObject("Decor").transform;
            decor.SetParent(transform, false);
            foreach (var p in placements) AddPlacement(decor, p);

            BuildFlag();
            StaticBatchingUtility.Combine(ground.gameObject);
            StaticBatchingUtility.Combine(obstacles.gameObject);
        }

        // ------------------------------------------------------------------ helpers

        struct Placement
        {
            public KitInfo Info;
            public Vector3 Position;
            public float Yaw;
            public float Scale;
            public int Variant;
            public Color Tint;
        }

        void CollectDecor(HoleDef def, List<Placement> list)
        {
            foreach (var d in def.Decor)
            {
                KitInfo info;
                if (!KitRegistry.TryGet(d.Kit, out info))
                {
                    Debug.LogWarning("Unknown kit piece " + d.Kit);
                    continue;
                }
                Vector3 pos = d.Position;
                if (d.Ground) pos.y = GroundOrLower(new Vector2(pos.x, pos.z)) + d.Position.y;
                list.Add(new Placement { Info = info, Position = pos, Yaw = d.Yaw, Scale = d.Scale, Variant = KitRegistry.VariantFor(info, d.Seed), Tint = d.Tint });
            }
            foreach (var s in def.Scatter) Scatter(s, list);
        }

        float GroundOrLower(Vector2 p)
        {
            var g = Course.Ground(p);
            return g.Height;
        }

        void Scatter(ScatterDef s, List<Placement> list)
        {
            var rng = new DetRandom((ulong)(s.Seed * 7919 + 1));
            Rect b = s.Area.Bounds;
            float sp = s.Spacing;
            int nx = Mathf.CeilToInt(b.width / sp), nz = Mathf.CeilToInt(b.height / sp);
            for (int j = 0; j <= nz; j++)
            {
                for (int i = 0; i <= nx; i++)
                {
                    Vector2 p = new Vector2(b.xMin + (i + 0.5f + rng.Signed() * 0.42f) * sp, b.yMin + (j + 0.5f + rng.Signed() * 0.42f) * sp);
                    if (s.Area.Distance(p) > 0f) continue;
                    if (s.Clearance > 0f && PlayableDistance(p) < s.Clearance) continue;
                    string kit = s.Kits[rng.Range(0, s.Kits.Length)];
                    KitInfo info;
                    if (!KitRegistry.TryGet(kit, out info)) continue;
                    float y = s.Ground ? GroundOrLower(p) : s.FixedY;
                    list.Add(new Placement
                    {
                        Info = info,
                        Position = new Vector3(p.x, y, p.y),
                        Yaw = rng.Value() * 360f,
                        Scale = rng.Range(s.ScaleRange.x, s.ScaleRange.y),
                        Variant = rng.Range(0, info.Variants),
                        Tint = Color.white,
                    });
                }
            }
        }

        float PlayableDistance(Vector2 p)
        {
            float best = float.MaxValue;
            foreach (var pad in Course.Pads)
            {
                if (pad.Def.OutOfBounds) continue;
                best = Mathf.Min(best, pad.Shape.Distance(p));
            }
            return best;
        }

        void AddPlacement(Transform parent, Placement p)
        {
            Mesh[] meshes = KitMeshes(p.Info, p.Variant);
            var go = new GameObject(p.Info.Name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(p.Position, Quaternion.Euler(0f, p.Yaw, 0f));
            go.transform.localScale = Vector3.one * p.Scale;
            Material[] mats = { cfg.Lit, cfg.Foliage, cfg.Glow, cfg.GlowAmber };
            for (int i = 0; i < meshes.Length; i++)
            {
                if (meshes[i] == null) continue;
                var part = new GameObject(i == 1 ? "foliage" : (i == 0 ? "lit" : "glow"));
                part.transform.SetParent(go.transform, false);
                part.AddComponent<MeshFilter>().sharedMesh = meshes[i];
                var r = part.AddComponent<MeshRenderer>();
                r.sharedMaterial = mats[i];
                r.shadowCastingMode = i >= 2 ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
                Triangles += (int)(meshes[i].GetIndexCount(0) / 3);
                if (i == 1) FoliageRenderers.Add(r);
            }
        }

        public static Mesh[] KitMeshes(KitInfo info, int variant)
        {
            string key = info.Name + "#" + variant;
            Mesh[] meshes;
            if (kitCache.TryGetValue(key, out meshes)) return meshes;
            var baked = Resources.Load<KitMeshSet>("Kit/" + info.Name + "_" + variant);
            if (baked != null) meshes = baked.Meshes;
            else
            {
                KitMesh k = info.Build(variant * 101 + 7);
                meshes = new Mesh[4];
                if (!k.Lit.IsEmpty) meshes[0] = k.Lit.ToMesh(key + " lit");
                if (!k.Foliage.IsEmpty) meshes[1] = k.Foliage.ToMesh(key + " foliage");
                if (!k.Glow.IsEmpty) meshes[2] = k.Glow.ToMesh(key + " glow");
                if (!k.GlowAmber.IsEmpty) meshes[3] = k.GlowAmber.ToMesh(key + " amber");
            }
            kitCache[key] = meshes;
            return meshes;
        }

        void AddKit(Transform parent, string name, KitMesh k, Matrix4x4 tr)
        {
            var go = new GameObject(name).transform;
            go.SetParent(parent, false);
            AddMesh(go, name + " stone", k.Lit, cfg.Lit, true, true);
            var f = AddMesh(go, name + " foliage", k.Foliage, cfg.Foliage, true, true);
            if (f != null) FoliageRenderers.Add(f);
            AddMesh(go, name + " glow", k.Glow, cfg.Glow, false, false);
            AddMesh(go, name + " amber", k.GlowAmber, cfg.GlowAmber, false, false);
        }

        MeshRenderer AddMesh(Transform parent, string name, MeshData data, Material mat, bool castShadows, bool receive)
        {
            if (data == null || data.IsEmpty) return null;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = data.ToMesh(name);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = receive;
            Triangles += data.Indices.Count / 3;
            return r;
        }

        void BuildFlag()
        {
            Vector2 cup = Course.Cup;
            var flag = new GameObject("Flag");
            flag.transform.SetParent(transform, false);
            flag.transform.position = new Vector3(cup.x, Course.CupHeight, cup.y);
            var pole = new MeshData();
            var prof = new List<Vector2> { new Vector2(0.022f, -0.3f), new Vector2(0.02f, 1.55f), new Vector2(0.03f, 1.58f), new Vector2(0.0f, 1.62f) };
            KitPrims.Lathe(pole, Matrix4x4.identity, prof, 8, Palette.Hex("#e9e2d4"), 0.7f, 1f, false);
            var poleGo = new GameObject("Pole");
            poleGo.transform.SetParent(flag.transform, false);
            poleGo.AddComponent<MeshFilter>().sharedMesh = pole.ToMesh("Flag pole");
            poleGo.AddComponent<MeshRenderer>().sharedMaterial = cfg.Lit;

            // Pennant: a small vermilion triangle, sway grows toward the free end.
            var cloth = new MeshData();
            Color red = Palette.Hex("#e0442f");
            const int cols = 6;
            var top = new int[cols + 1];
            var bot = new int[cols + 1];
            for (int i = 0; i <= cols; i++)
            {
                float u = i / (float)cols;
                float x = u * 0.5f;
                float hh = Mathf.Lerp(0.32f, 0.02f, u);
                top[i] = cloth.Add(new Vector3(x, 1.52f, 0), Vector3.back, red, new Vector4(1f, u * 3.5f, 0.3f, 0));
                bot[i] = cloth.Add(new Vector3(x, 1.52f - hh, 0), Vector3.back, red, new Vector4(0.9f, u * 3.5f, 0.3f, 0));
            }
            for (int i = 0; i < cols; i++) cloth.Quad(bot[i], top[i], top[i + 1], bot[i + 1]);
            var clothGo = new GameObject("Pennant");
            clothGo.transform.SetParent(flag.transform, false);
            clothGo.AddComponent<MeshFilter>().sharedMesh = cloth.ToMesh("Pennant");
            var cr = clothGo.AddComponent<MeshRenderer>();
            cr.sharedMaterial = cfg.Foliage;
            Flag = flag.transform;
            FlagCloth = clothGo.transform;
        }
    }
}
