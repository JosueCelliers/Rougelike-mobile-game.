using System.IO;
using BadLie.Course;
using BadLie.Game;
using BadLie.Geometry;
using BadLie.Holes;
using UnityEditor;
using UnityEngine;

namespace BadLie.EditorTools
{
    /// <summary>
    /// Level-design helpers: one prefab per environment-kit piece and variant (each a
    /// <see cref="KitPiece"/> that rebuilds from code), and a command that builds any hole
    /// into the open scene for inspection in the Scene view.
    /// </summary>
    public static class KitTools
    {
        public const string PrefabDir = ProjectSetup.Root + "/Kit";

        [MenuItem("BAD LIE/Create Kit Prefabs")]
        public static void CreatePrefabs()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(ProjectSetup.ConfigPath);
            Directory.CreateDirectory(PrefabDir);
            int n = 0;
            foreach (var info in KitRegistry.All)
            {
                for (int v = 0; v < info.Variants; v++)
                {
                    var go = new GameObject(info.Name + "_" + v);
                    var piece = go.AddComponent<KitPiece>();
                    piece.Kit = info.Name;
                    piece.Variant = v;
                    piece.Config = cfg;
                    PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/" + info.Name + "_" + v + ".prefab");
                    Object.DestroyImmediate(go);
                    n++;
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[BadLie] Kit prefabs written: " + n + " in " + PrefabDir);
        }

        [MenuItem("BAD LIE/Preview Hole/1 The Lantern Gate")] static void P1() { Preview(0); }
        [MenuItem("BAD LIE/Preview Hole/2 The Sunken Parterre")] static void P2() { Preview(1); }
        [MenuItem("BAD LIE/Preview Hole/3 The Sluice Walk")] static void P3() { Preview(2); }
        [MenuItem("BAD LIE/Preview Hole/4 The Flooded Cloister")] static void P4() { Preview(3); }
        [MenuItem("BAD LIE/Preview Hole/5 The Orrery")] static void P5() { Preview(4); }

        /// <summary>Builds a hole (terrain, walls, dressing, cup) under "Hole Preview" in the open scene.</summary>
        public static void Preview(int index)
        {
            var old = GameObject.Find("Hole Preview");
            if (old != null) Object.DestroyImmediate(old);
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(ProjectSetup.ConfigPath);
            var root = new GameObject("Hole Preview");
            var course = new CourseModel(HoleLibrary.Get(index));
            HoleView.Create(course, cfg, root.transform);
            Selection.activeGameObject = root;
            Debug.Log("[BadLie] Previewing " + course.Def.Name + ". Delete \"Hole Preview\" before saving the scene.");
        }
    }
}
