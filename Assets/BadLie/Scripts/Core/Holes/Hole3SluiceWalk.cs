using BadLie.Core;
using BadLie.Course;
using UnityEngine;
using static BadLie.Holes.HoleKit;

namespace BadLie.Holes
{
    /// <summary>
    /// Hole 3 — The Sluice Walk. A stone causeway along the open edge of the flooded estate.
    /// Three runnels fed by the old sluice machinery cross it and push the ball towards the
    /// drop. A long sandstone wall on the right rewards bank shots; Heavy Core holds a line.
    /// The green sits on a round machine dais with a vent.
    /// </summary>
    public static class Hole3SluiceWalk
    {
        public static HoleDef Build()
        {
            var h = new HoleDef
            {
                Id = "sluice-walk",
                Name = "The Sluice Walk",
                Subtitle = "The runnels still run. Hug the wall, or carry weight.",
                Par = 4,
                Tee = V(0f, -1.4f),
                Cup = V(0.9f, 38.4f),
                CameraYaw = 6f,
                StripeAngle = 90f,
                PlayBounds = Rect.MinMaxRect(-7f, -5f, 9f, 44f),
                LowerWaterLevel = -1.9f,
            };
            h.AltCups.Add(V(-1.0f, 39.2f));
            h.AltCups.Add(V(1.9f, 37.0f));
            h.Features.Add("Runnels push toward the drop");
            h.Features.Add("Long bank wall");

            // ---------------------------------------------------------------- ground
            var causewayPath = new[] { V(0f, -2.4f), V(1.2f, 8f), V(-0.6f, 17f), V(1.0f, 26f), V(0.2f, 33.4f) };
            h.Pad("tee", new BoxShape(V(0f, -1.6f), V(5.2f, 4.0f), 0f, 0.4f), 6, HeightDef.Flat(0.4f), SurfaceType.Stone, EdgeStyle.Stone);
            h.Pad("causeway", new CapsuleChainShape(causewayPath, 2.6f), 5, HeightDef.Flat(0.4f), SurfaceType.Stone, EdgeStyle.Stone);
            h.Pad("ramp", new BoxShape(V(0.3f, 32.9f), V(4.2f, 3.2f), 0f, 0.1f), 7, HeightDef.Ramp(V(0.3f, 31.6f), 0.4f, V(0.3f, 33.8f), 0.75f), SurfaceType.Stone, EdgeStyle.Machine);
            var dais = h.Pad("dais", new CircleShape(V(0.5f, 38.6f), 4.7f), 8, HeightDef.Flat(0.75f), SurfaceType.Fairway, EdgeStyle.Machine);
            dais.Features.Add(FeatureDef.Bump(V(-1.4f, 40.2f), 2.2f, 0.06f));

            // ---------------------------------------------------------------- surfaces
            h.Paint(SurfaceType.Green, new CircleShape(V(0.5f, 38.6f), 3.7f), "dais");
            float[] runnelZ = { 8.1f, 18.0f, 27.4f };
            foreach (var z in runnelZ)
            {
                var band = new BoxShape(V(0.2f, z), V(8.6f, 1.8f), 0f, 0.3f);
                h.Paint(SurfaceType.Runnel, band, "causeway");
                h.Flow(band, 1.5f, "runnel", V(4.4f, z), V(-4.4f, z));
            }
            // A vent on the dais blows across the green toward the west edge.
            h.Push(new CircleShape(V(-1.6f, 37.4f), 1.2f), V(-1f, 0.15f), 2.0f, "vent");

            // ---------------------------------------------------------------- walls
            // Long sandstone bank wall on the east side, broken only by the runnel outlets.
            h.Wall(WallStyle.Sandstone, 0.85f, 0.42f, V(2.55f, -3.4f), V(3.3f, 6.9f));
            h.Wall(WallStyle.Sandstone, 0.85f, 0.42f, V(3.45f, 9.3f), V(2.15f, 16.8f));
            h.Wall(WallStyle.Sandstone, 0.85f, 0.42f, V(2.05f, 19.2f), V(3.35f, 26.2f));
            h.Wall(WallStyle.Sandstone, 0.85f, 0.42f, V(3.35f, 28.6f), V(2.95f, 33.0f));
            // Machine housings close the outlets on the wall side.
            h.Block(V(3.9f, 8.1f), V(1.1f, 2.6f), 0f, 1.1f, BlockStyle.Machine, 0.18f);
            h.Block(V(3.4f, 18.0f), V(1.1f, 2.6f), 0f, 1.1f, BlockStyle.Machine, 0.18f);
            h.Block(V(4.0f, 27.4f), V(1.1f, 2.6f), 0f, 1.1f, BlockStyle.Machine, 0.18f);
            // Broken balustrade on the open west edge, with gaps where the runnels spill.
            h.Wall(WallStyle.Balustrade, 0.95f, 0.34f, V(-2.3f, -3.4f), V(-1.55f, 6.7f));
            h.Wall(WallStyle.Balustrade, 0.95f, 0.34f, V(-2.05f, 9.6f), V(-3.25f, 16.4f));
            h.Wall(WallStyle.Balustrade, 0.95f, 0.34f, V(-3.2f, 19.7f), V(-1.75f, 25.8f));
            h.Wall(WallStyle.Balustrade, 0.95f, 0.34f, V(-1.75f, 29.2f), V(-2.4f, 33.2f));
            // Tee platform back rail.
            h.Wall(WallStyle.Balustrade, 0.95f, 0.34f, V(-2.4f, -3.5f), V(2.4f, -3.5f));
            // A low machine rim round the back of the dais.
            var rim = new ArcShape(V(0.5f, 38.6f), 4.5f, 300f, 600f, 0.15f);
            h.Wall(WallStyle.Machine, 0.3f, 0.3f, rim.SamplePoints(28)).BaseY = 0.75f;

            // ---------------------------------------------------------------- surroundings
            OutOfBounds(h, "machine-yard", new PolygonShape(new[] { V(1.8f, -5f), V(15f, -5f), V(15f, 44f), V(3.4f, 44f), V(2.4f, 33f), V(2.4f, 26f), V(1.2f, 17f), V(2.6f, 8f) }, 0.4f), 1.05f, SurfaceType.Stone, EdgeStyle.Stone);
            OutOfBounds(h, "north-bank", new BoxShape(V(1f, 47f), V(16f, 8f), 0f, 0.5f), 1.0f, SurfaceType.Rough, EdgeStyle.Earth);
            OutOfBounds(h, "ruin-isle", new PolygonShape(new[] { V(-16f, 6f), V(-9.5f, 7f), V(-8.6f, 15f), V(-10f, 24f), V(-16f, 25f) }, 1.0f), -1.4f, SurfaceType.Rough, EdgeStyle.Earth);

            Hole3Decor.Add(h);
            return h;
        }
    }

    public static class Hole3Decor
    {
        public static void Add(HoleDef h)
        {
            // The sluice house that feeds the runnels.
            h.PropAt("sluice_gate", new Vector3(7.6f, 1.05f, 30.4f), 180f, 1.4f);
            h.PropAt("sluice_gate", new Vector3(6.8f, 1.05f, 14.0f), 180f, 1.0f);
            h.Prop("ground_machine", 9.6f, 22f, 0f, 1.2f, 1);
            h.Prop("lamp_post", 4.0f, 3.2f, 0f, 1f, 1);
            h.Prop("lamp_post", 3.8f, 22.6f, 0f, 1f, 2);
            h.Prop("tree_red", 11.6f, 6.4f, 40f, 1.15f, 2);
            h.Prop("tree_amber", 12.4f, 37.6f, 150f, 1.2f, 1);
            h.Prop("wild_mass", 8.6f, 9.6f, 0f, 1.2f, 3);
            h.Prop("wild_mass", 10.4f, 28.4f, 0f, 1.1f, 2);
            h.Prop("topiary_cone", 6.0f, 36.0f, 0f, 1f, 2);
            h.Prop("column_broken", 6.4f, 4.6f, 0f, 1f, 3);
            // North bank behind the dais.
            h.Prop("arch_ruin", -1f, 46.6f, 0f, 1.2f, 1);
            h.Prop("tree_saffron", -6.6f, 46.4f, 0f, 1.2f, 2);
            h.Prop("tree_red", 6.8f, 47.6f, 0f, 1.15f, 3);
            h.Prop("hedge_wild", 3.0f, 44.4f, 0f, 1f, 2);
            // Ruins in the flood to the west.
            h.Prop("tree_amber", -12.6f, 10.6f, 0f, 1.1f, 3);
            h.Prop("arch_ruin", -12.0f, 19.4f, 85f, 1f, 2);
            h.PropAt("column_broken", new Vector3(-5.6f, -2.1f, 2.2f), 0f, 1f);
            h.PropAt("ruin_wall", new Vector3(-6.2f, -2.3f, 13.2f), 80f, 0.9f);
            h.PropAt("column", new Vector3(-5.0f, -2.1f, 24.6f), 0f, 1f);
            h.PropAt("lily_pads", new Vector3(-4.6f, -1.9f, 8.6f), 0f, 1.1f);
            h.PropAt("lily_pads", new Vector3(-5.4f, -1.9f, 18.6f), 50f, 1f);
            h.PropAt("reeds", new Vector3(-4.4f, -1.9f, 30.6f), 0f, 1f);
            h.PropAt("lily_pads", new Vector3(-5.0f, -1.9f, 36.0f), 100f, 1.2f);
            h.Prop("lily_lantern", -3.6f, 38.4f, 0f, 1f, 1);
            h.PropAt("tower_far", new Vector3(-30f, -1.9f, 50f), 0f, 1.4f);
            h.PropAt("aqueduct_far", new Vector3(-34f, -1.9f, 20f), 70f, 1.3f);
            h.PropAt("tower_far", new Vector3(26f, -1.9f, 64f), 0f, 1.3f);
            h.Scatter_(new BoxShape(V(9f, 20f), V(10f, 46f)), 1.5f, 31, 0.6f, "tuft_dry", "tuft");
        }
    }
}
