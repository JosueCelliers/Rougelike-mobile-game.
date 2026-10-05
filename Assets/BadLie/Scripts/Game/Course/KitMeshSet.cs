using UnityEngine;

namespace BadLie.Game
{
    /// <summary>Baked meshes of one kit variant: 0 lit, 1 foliage, 2 glow (cyan), 3 glow (amber).</summary>
    public sealed class KitMeshSet : ScriptableObject
    {
        public Mesh[] Meshes = new Mesh[4];
    }
}
