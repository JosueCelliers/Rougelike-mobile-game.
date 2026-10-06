using BadLie.Core;
using BadLie.Course;
using UnityEngine;
using static BadLie.Holes.HoleKit;

namespace BadLie.Holes
{
    /// <summary>
    /// Hole 4 — The Flooded Cloister. A colonnaded walkway around a flooded court with an
    /// island green. Walk the long way (bank round the corners, cross the bridge), or skip two
    /// short gaps via a stepping stone. Skip Stone and Bank Shot together make it a trick hole.
    /// </summary>
    public static class Hole4FloodedCloister
    {
        public static HoleDef Build()
        {
            var h = new HoleDef
            {
                Id = "flooded-cloister",
                Name = "The Flooded Cloister",
                Subtitle = "Round the cloister and over the bridge, or straight across the water.",
                Par = 4,
                Tee = V(0f, 1.9f),
                Cup = V(0.5f, 12.3f),
                CameraYaw = 0f,
                StripeAngle = 0f,
                PlayBounds = Rect.MinMaxRect(-10f, -3f, 10f, 26f),
                LowerWaterLevel = -1.9f,
            };
            h.AltCups.Add(V(-1.1f, 11.4f));
            h.AltCups.Add(V(1.2f, 13.6f));
            h.Features.Add("Two short water gaps");
            h.Features.Add("Corner banks");

            // ---------------------------------------------------------------- ground
            var outer = new BoxShape(V(0f, 11f), V(20f, 22f), 0f, 0.4f);
            var inner = new BoxShape(V(0f, 11f), V(12.4f, 13.2f), 0f, 0.6f);
            h.Pad("walkway", new SubtractShape(outer, inner), 5, HeightDef.Flat(0.35f), SurfaceType.Stone, EdgeStyle.Stone);
            h.WaterPad("court", new BoxShape(V(0f, 11f), V(13.2f, 14.0f), 0f, 0.3f), 1, 0.02f, 0.55f);
            h.Pad("step", new BoxShape(V(0f, 6.9f), V(3.2f, 1.7f), 0f, 0.45f), 6, HeightDef.Flat(0.3f), SurfaceType.Fairway, EdgeStyle.Coping);
            var island = h.Pad("island", new CircleShape(V(0f, 11.95f), 3.0f), 7, HeightDef.Flat(0.42f), SurfaceType.Fairway, EdgeStyle.Coping);
            island.Features.Add(FeatureDef.Bump(V(-1.2f, 13.2f), 1.3f, 0.05f));
            h.Pad("bridge", new BoxShape(V(0f, 16.3f), V(1.8f, 3.0f), 0f, 0.1f), 6, HeightDef.Ramp(V(0f, 14.95f), 0.42f, V(0f, 17.6f), 0.35f), SurfaceType.Stone, EdgeStyle.Coping);

            // ---------------------------------------------------------------- surfaces
            h.Paint(SurfaceType.Green, new CircleShape(V(0f, 11.95f), 2.45f), "island");

            // ---------------------------------------------------------------- walls
            h.Wall(WallStyle.Sandstone, 1.1f, 0.44f, V(-9.75f, 0.25f), V(9.75f, 0.25f), V(9.75f, 21.75f), V(-9.75f, 21.75f), V(-9.75f, 0.25f));
            h.Wall(WallStyle.Curb, 0.15f, 0.16f, V(-0.95f, 15.3f), V(-0.95f, 17.5f));
            h.Wall(WallStyle.Curb, 0.15f, 0.16f, V(0.95f, 15.3f), V(0.95f, 17.5f));
            // The arcade: columns along the inner edge of the walkway.
            float[] side = { 6.2f, 9.0f, 11.8f, 14.6f };
            foreach (var z in side)
            {
                h.Post(V(-6.75f, z + 0.2f), 0.28f, 2.6f, z > 12f ? PostStyle.BrokenColumn : PostStyle.Column);
                h.Post(V(6.75f, z - 0.2f), 0.28f, 2.6f, z < 10f ? PostStyle.BrokenColumn : PostStyle.Column);
            }
            float[] across = { -5.2f, -2.8f, 2.8f, 5.2f };
            foreach (var x in across)
            {
                h.Post(V(x, 3.95f), 0.28f, 2.6f, PostStyle.Column);
                h.Post(V(x, 18.05f), 0.28f, 2.6f, Mathf.Abs(x) < 3f ? PostStyle.BrokenColumn : PostStyle.Column);
            }

            // ---------------------------------------------------------------- surroundings
            OutOfBounds(h, "north-garden", new BoxShape(V(0f, 27.5f), V(30f, 11f), 0f, 0.6f), 0.9f, SurfaceType.Rough, EdgeStyle.Stone);
            OutOfBounds(h, "south-steps", new BoxShape(V(0f, -3.35f), V(14f, 7.5f), 0f, 0.4f), -0.3f, SurfaceType.Stone, EdgeStyle.Stone);

            Hole4Decor.Add(h);
            return h;
        }
    }

    public static class Hole4Decor
    {
        public static void Add(HoleDef h)
        {
            // What is left of the arcade: bays still standing between intact columns.
            h.PropAt("arcade_28", new Vector3(-6.75f, 0.35f, 7.8f), 90f, 1f);
            h.PropAt("arcade_28", new Vector3(-6.75f, 0.35f, 10.6f), 90f, 1f);
            h.PropAt("arcade_28", new Vector3(6.75f, 0.35f, 13.0f), 90f, 1f);
            h.PropAt("lily_pads", new Vector3(-3.8f, 0.02f, 7.6f), 0f, 0.8f);
            h.PropAt("lily_pads", new Vector3(4.2f, 0.02f, 15.6f), 90f, 0.9f);
            h.PropAt("lily_pads", new Vector3(-4.4f, 0.02f, 15.2f), 30f, 0.7f);
            h.PropAt("reeds", new Vector3(5.0f, 0.02f, 6.2f), 0f, 0.9f);
            h.Prop("lily_lantern", -2.2f, 13.9f, 0f, 1f, 1);
            h.Prop("lily_lantern", 2.4f, 10.6f, 0f, 1f, 2);
            h.Prop("lily_lantern", 1.4f, 7.4f, 0f, 0.8f, 3);
            // North garden behind the cloister.
            h.Prop("tree_amber", -8.4f, 27.6f, 0f, 1.2f, 1);
            h.Prop("tree_red", 0.6f, 29.4f, 60f, 1.25f, 2);
            h.Prop("tree_saffron", 9.2f, 27.0f, 0f, 1.15f, 3);
            h.Prop("arch_ruin", -3.6f, 24.6f, 0f, 1.1f, 1);
            h.Prop("arch_ruin", 4.8f, 24.4f, 0f, 1.05f, 2);
            h.Prop("hedge_wild", 12.6f, 24.2f, 90f, 1f, 1);
            h.Prop("hedge_wild", -12.4f, 24.8f, 90f, 1f, 3);
            // Ruins of the cloister's lost wings in the flood.
            h.PropAt("ruin_wall", new Vector3(-13.4f, -2.3f, 6.0f), 90f, 1.0f);
            h.PropAt("ruin_wall", new Vector3(13.6f, -2.3f, 13.0f), 90f, 1.05f);
            h.PropAt("column_broken", new Vector3(-12.6f, -2.1f, 14.4f), 0f, 1.1f);
            h.PropAt("column", new Vector3(12.8f, -2.1f, 3.6f), 0f, 1.1f);
            h.PropAt("column_broken", new Vector3(14.2f, -2.1f, 19.6f), 0f, 1f);
            h.PropAt("sunken_slabs", new Vector3(-14.6f, -1.9f, 18.6f), 30f, 1f);
            h.PropAt("lily_pads", new Vector3(-12.2f, -1.9f, 1.4f), 0f, 1.2f);
            h.PropAt("lily_pads", new Vector3(12.4f, -1.9f, 8.6f), 0f, 1.2f);
            h.PropAt("reeds", new Vector3(-11.0f, -1.9f, 10.4f), 0f, 1f);
            // South steps down to the flood.
            h.Prop("urn", -5.6f, -3.2f, 0f, 1f, 1);
            h.Prop("urn", 5.6f, -3.2f, 0f, 1f, 2);
            h.Prop("tree_red", -9.6f, -4.6f, 0f, 1.05f, 3);
            h.PropAt("tower_far", new Vector3(-26f, -1.9f, 44f), 0f, 1.5f);
            h.PropAt("tower_far", new Vector3(22f, -1.9f, 52f), 60f, 1.3f);
            h.PropAt("aqueduct_far", new Vector3(30f, -1.9f, 20f), -80f, 1.3f);
            h.Scatter_(new BoxShape(V(0f, 28f), V(28f, 9f)), 1.4f, 41, 0.6f, "tuft", "tuft_dry");
        }
    }
}
