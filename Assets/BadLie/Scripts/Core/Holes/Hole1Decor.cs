using BadLie.Core;
using BadLie.Course;
using UnityEngine;

namespace BadLie.Holes
{
    /// <summary>
    /// Set dressing for hole 1, kept apart from the playable layout. Order in the middle,
    /// wilderness pressing in from the frame: clipped forms near play, overgrowth beyond,
    /// ruins in the flooded estate and towers in the haze.
    /// </summary>
    public static class Hole1Decor
    {
        static Vector2 V(float x, float z) { return new Vector2(x, z); }

        public static void Add(HoleDef h)
        {
            // Tee: fallen amber leaves under the tee tree.
            h.Prop("leaves", -3.2f, -1.6f, 0f, 1.4f, 3);
            h.Prop("leaves", -1.2f, -0.9f, 70f, 0.8f, 5);

            // Lower terrace below the tee.
            h.Prop("tree_red", 5.6f, -8.4f, 30f, 1.05f, 2);
            h.Prop("tree_saffron", -5.4f, -9.6f, 200f, 0.95f, 1);
            h.Prop("urn", -5.9f, -4.6f, 0f, 1f, 1);
            h.Prop("urn", 6.9f, -4.6f, 0f, 1f, 2);
            h.Prop("topiary_small", -1.6f, -5.2f, 10f, 1f, 1);
            h.Prop("topiary_small", 2.8f, -5.3f, 50f, 1f, 2);
            h.Prop("bush_flowers", 0.8f, -6.4f, 0f, 1.1f, 3);
            h.Prop("leaves_red", 5.0f, -6.9f, 0f, 1.5f, 1);

            // Raised bed east of the fairway: topiary row and flowering domes.
            h.Prop("topiary_cone", 7.4f, -1.4f, 0f, 1f, 1);
            h.Prop("topiary_cone", 7.6f, 2.0f, 40f, 1.05f, 2);
            h.Prop("topiary_cone", 7.9f, 5.4f, 80f, 0.95f, 3);
            h.Prop("bush_flowers", 9.6f, -0.6f, 0f, 1.1f, 1);
            h.Prop("bush_flowers", 10.2f, 3.6f, 90f, 1f, 2);
            h.Prop("bush_dome", 9.9f, 6.9f, 0f, 1.2f, 3);
            h.Prop("tree_saffron", 12.1f, 1.6f, 120f, 1.1f, 2);
            h.Prop("wild_mass", 12.9f, 8.6f, 0f, 1f, 1);

            // East garden beyond the balustrade: overgrowth and trees.
            h.Prop("tree_red", 17.4f, 6.5f, 60f, 1.15f, 1);
            h.Prop("tree_amber", 18.6f, 17.2f, 160f, 1.2f, 2);
            h.Prop("tree_red", 17.0f, 27.4f, 10f, 1.05f, 3);
            h.Prop("wild_mass", 15.6f, 12.8f, 0f, 1.2f, 2);
            h.Prop("wild_mass", 15.4f, 21.6f, 40f, 1.1f, 3);
            h.Prop("hedge_wild", 15.4f, 32.4f, 90f, 1f, 2);
            h.Prop("arch_ruin", 20.4f, 23.0f, 75f, 1.1f, 1);

            // Behind the green: hedges overtaken by roots, weathered arches, trees.
            h.Prop("arch_ruin", 1.2f, 39.4f, 4f, 1.15f, 2);
            h.Prop("arch_ruin", 9.6f, 38.6f, -8f, 1.05f, 1);
            h.Prop("hedge_wild", -3.4f, 37.2f, 2f, 1f, 1);
            h.Prop("hedge_wild", 5.4f, 37.6f, -3f, 1f, 3);
            h.Prop("tree_amber", -4.6f, 42.4f, 30f, 1.25f, 1);
            h.Prop("tree_red", 5.8f, 43.6f, 140f, 1.2f, 2);
            h.Prop("tree_saffron", 13.2f, 41.6f, 80f, 1.1f, 3);
            h.Prop("topiary_cone", -1.6f, 36.6f, 0f, 1.1f, 2);
            h.Prop("topiary_cone", 12.6f, 35.6f, 0f, 1.1f, 1);

            // West bank by the sluice.
            h.Prop("tree_amber", -8.6f, 31.4f, 0f, 1.15f, 3);
            h.Prop("wild_mass", -4.4f, 28.6f, 0f, 1f, 4);
            h.Prop("reeds", -6.3f, 26.0f, 0f, 1f, 1);
            h.Prop("column_broken", -10.6f, 27.6f, 30f, 1f, 1);
            h.PropAt("sluice_gate", new Vector3(-7.95f, -1.15f, 23.0f), -90f, 1.15f);

            // The flooded lower estate: ruins standing in still water.
            h.PropAt("ruin_wall", new Vector3(-9.6f, -2.3f, 3.6f), 70f, 1f);
            h.PropAt("ruin_wall", new Vector3(-10.4f, -2.3f, 15.2f), 100f, 0.9f);
            h.PropAt("column_broken", new Vector3(-8.3f, -2.1f, -1.4f), 0f, 1.1f);
            h.PropAt("column", new Vector3(-9.0f, -2.1f, 9.6f), 0f, 1f);
            h.PropAt("sunken_slabs", new Vector3(-8.0f, -1.9f, 6.2f), 20f, 1f);
            h.PropAt("lily_pads", new Vector3(-7.6f, -1.9f, 1.4f), 0f, 1f);
            h.PropAt("lily_pads", new Vector3(-8.4f, -1.9f, 12.2f), 60f, 1.2f);
            h.PropAt("lily_pads", new Vector3(-11.2f, -1.9f, 19.0f), 120f, 1f);
            h.PropAt("reeds", new Vector3(-7.0f, -1.9f, 17.4f), 0f, 1f);
            h.PropAt("reeds", new Vector3(-7.2f, -1.9f, -2.8f), 0f, 1f);
            h.PropAt("lily_pads", new Vector3(-2.0f, -1.9f, -13.0f), 0f, 1.1f);

            // The island in the flood.
            h.Prop("tree_red", -17.2f, 4.4f, 20f, 1.2f, 1);
            h.Prop("tree_amber", -16.0f, 15.6f, 90f, 1.15f, 2);
            h.Prop("arch_ruin", -14.4f, 9.6f, 92f, 1f, 2);
            h.Prop("wild_mass", -14.2f, 1.8f, 0f, 1.2f, 1);
            h.Prop("wild_mass", -14.6f, 19.4f, 30f, 1.1f, 2);

            // Canal: lilies and luminous lantern lilies at the skip line.
            h.PropAt("lily_pads", new Vector3(-4.6f, -0.14f, 23.6f), 0f, 0.8f);
            h.Prop("lily_lantern", 0.7f, 19.85f, 0f, 1f, 1);
            h.Prop("lily_lantern", 9.6f, 19.7f, 0f, 1f, 2);
            h.Prop("lily_lantern", -1.6f, 28.4f, 0f, 1f, 3);
            h.Prop("lily_lantern", 8.9f, 31.4f, 0f, 1f, 4);

            // Distant structures softened by the haze.
            h.PropAt("tower_far", new Vector3(-26f, -1.9f, 54f), 0f, 1.2f);
            h.PropAt("tower_far", new Vector3(21f, -1.9f, 62f), 40f, 1.4f);
            h.PropAt("tower_far", new Vector3(-6f, -1.9f, 78f), 80f, 1.6f);
            h.PropAt("aqueduct_far", new Vector3(-40f, -1.9f, 36f), 70f, 1.3f);
            h.PropAt("aqueduct_far", new Vector3(30f, -1.9f, 44f), -60f, 1.2f);

            // Grass tufts only in the out-of-bounds beds and banks (never on the playing line).
            h.Scatter_(new BoxShape(V(11.0f, 0.5f), V(5f, 12f), 0f, 0.2f), 1.1f, 13, 0.6f, "tuft_dry", "tuft");
            h.Scatter_(new BoxShape(V(18f, 16f), V(8f, 38f), 0f, 0.2f), 1.3f, 14, 0.6f, "tuft", "tuft_dry", "tuft");
            h.Scatter_(new BoxShape(V(4f, 41f), V(18f, 9f), 0f, 0.2f), 1.2f, 15, 0.6f, "tuft", "tuft_dry");
            h.Scatter_(new BoxShape(V(-7f, 30f), V(10f, 9f), 0f, 0.2f), 1.2f, 16, 0.6f, "tuft", "tuft_dry");
        }
    }
}
