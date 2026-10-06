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

        /// <summary>A bunker: sand paint plus a soft bowl in the pad it sits on. The outline is
        /// roughened so it reads as dug, not drawn; physics and visuals share the same shape.</summary>
        public static void Bunker(HoleDef h, string padId, Shape2D shape, float depth = 0.15f)
        {
            Vector2 c = shape.Bounds.center;
            int seed = Mathf.RoundToInt(c.x * 37f + c.y * 113f) & 0xffff;
            var organic = new WobbleShape(shape, 0.17f, 1.15f, seed);
            h.Paint(SurfaceType.Sand, organic, padId);
            FindPad(h, padId).Features.Add(FeatureDef.Bowl(organic, depth, 0.65f));
        }
    }
}
