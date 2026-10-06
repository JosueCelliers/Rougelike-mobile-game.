using BadLie.Geometry;
using UnityEngine;

namespace BadLie.Game
{
    /// <summary>
    /// A reusable environment-kit prefab. It rebuilds its meshes from the procedural kit, with
    /// the same generator and seed the holes use, so the prefab file stays tiny and always
    /// matches the code. Drop one into any scene and pick a kit piece and variant.
    /// The generated child renderers are never saved.
    /// </summary>
    [ExecuteAlways]
    public sealed class KitPiece : MonoBehaviour
    {
        public string Kit = "tree_amber";
        public int Variant;
        public GameConfig Config;

        bool dirty = true;

        void OnEnable() { dirty = true; }

        void OnValidate() { dirty = true; }

        void Update()
        {
            if (!dirty) return;
            dirty = false;
            Rebuild();
        }

        public void Rebuild()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i).gameObject;
                if ((c.hideFlags & HideFlags.DontSave) == 0) continue;
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
            }
            KitInfo info;
            if (Config == null || !KitRegistry.TryGet(Kit, out info)) return;
            var meshes = HoleView.KitMeshes(info, Mathf.Clamp(Variant, 0, info.Variants - 1));
            Material[] mats = { Config.Lit, Config.Foliage, Config.Glow, Config.GlowAmber };
            string[] names = { "lit", "foliage", "glow", "amber" };
            for (int i = 0; i < meshes.Length; i++)
            {
                if (meshes[i] == null) continue;
                var part = new GameObject(names[i]) { hideFlags = HideFlags.DontSave | HideFlags.NotEditable };
                part.transform.SetParent(transform, false);
                part.AddComponent<MeshFilter>().sharedMesh = meshes[i];
                var r = part.AddComponent<MeshRenderer>();
                r.sharedMaterial = mats[i];
                if (i >= 2) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }
}
