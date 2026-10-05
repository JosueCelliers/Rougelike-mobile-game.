using BadLie.Core;
using BadLie.Course;
using UnityEngine;

namespace BadLie.Holes
{
    /// <summary>Small authoring helpers shared by the hole definitions.</summary>
    public static class HoleKit
    {
        public static Vector2 V(float x, float z) { return new Vector2(x, z); }

        /// <summary>Out-of-bounds ground at priority 0 so it tucks under playable pads without seams.</summary>
        public static PadDef OutOfBounds(HoleDef h, string id, Shape2D shape, float height, SurfaceType surface, EdgeStyle edge)
        {
            var p = h.Pad(id, shape, 0, HeightDef.Flat(height), surface, edge);
            p.OutOfBounds = true;
            return p;
        }

        public static PadDef FindPad(HoleDef h, string id)
        {
            foreach (var p in h.Pads) if (p.Id == id) return p;
            return null;
        }

        /// <summary>A bunker: sand paint plus a soft bowl in the pad it sits on.</summary>
        public static void Bunker(HoleDef h, string padId, Shape2D shape, float depth = 0.15f)
        {
            h.Paint(SurfaceType.Sand, shape, padId);
            FindPad(h, padId).Features.Add(FeatureDef.Bowl(shape, depth, 0.65f));
        }
    }
}
