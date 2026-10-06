using BadLie.Core;
using BadLie.Course;
using UnityEngine;
using static BadLie.Holes.HoleKit;

namespace BadLie.Holes
{
    /// <summary>
    /// Hole 2 — The Sunken Parterre. Tee off a balustraded terrace and drop into a sunken
    /// formal garden. Fairway lanes zig-zag round clipped hedge boxes and bunkers; the straight
    /// line runs through two overgrown beds (slow rough, unless you carry Rough Rider) to a
    /// raised, undulating green.
    /// </summary>
    public static class Hole2SunkenParterre
    {
        public static HoleDef Build()
        {
            var h = new HoleDef
            {
                Id = "sunken-parterre",
                Name = "The Sunken Parterre",
                Subtitle = "Drop into the old parterre. The overgrown beds are the short way.",
                // Playtests: every bot, even with no upgrades, needs three. The drop off the terrace
                // carries a long way, so this is the run's par 3.
                Par = 3,
                Tee = V(0f, -2.2f),
                Cup = V(1.4f, 37.3f),
                CameraYaw = 0f,
                StripeAngle = 0f,
                PlayBounds = Rect.MinMaxRect(-9.5f, -6f, 9.5f, 42f),
                LowerWaterLevel = -1.9f,
            };
            h.AltCups.Add(V(-2.4f, 36.5f));
            h.AltCups.Add(V(2.6f, 35.6f));
            h.Features.Add("Overgrown beds: Rough Rider's straight line");
            h.Features.Add("A drop from the terrace");

            // ---------------------------------------------------------------- ground
            h.Pad("tee-terrace", new BoxShape(V(0f, -1.6f), V(9f, 7.2f), 0f, 0.3f), 6, HeightDef.Flat(1.2f), SurfaceType.Stone, EdgeStyle.Stone);
            h.Pad("parterre", new BoxShape(V(0f, 17.4f), V(20f, 45.2f), 0f, 0.4f), 2, HeightDef.Flat(0f), SurfaceType.Rough, EdgeStyle.Stone);
            var green = h.Pad("green", new PolygonShape(new[] { V(-5.6f, 33.4f), V(5.6f, 33.4f), V(6.6f, 36.2f), V(5.8f, 39.6f), V(-5.8f, 39.6f), V(-6.6f, 36.2f) }, 0.8f), 7, HeightDef.Flat(0.38f), SurfaceType.Fairway, EdgeStyle.Stone);
            green.Features.Add(FeatureDef.Bump(V(-2.6f, 38.0f), 2.6f, 0.07f));
            green.Features.Add(FeatureDef.Bump(V(3.4f, 37.6f), 2.0f, -0.05f));
            h.Pad("apron", new BoxShape(V(0f, 32.55f), V(11.6f, 1.9f), 0f, 0.2f), 6, HeightDef.Ramp(V(0f, 31.6f), 0f, V(0f, 33.25f), 0.38f), SurfaceType.Fairway, EdgeStyle.Stone);

            // ---------------------------------------------------------------- surfaces
            h.Paint(SurfaceType.Fairway, new BoxShape(V(0f, 6.6f), V(8.4f, 6.4f), 0f, 0.8f));
            h.Paint(SurfaceType.Fairway, new BoxShape(V(-3.6f, 18f), V(3.0f, 19f), 0f, 0.6f));
            h.Paint(SurfaceType.Fairway, new BoxShape(V(3.6f, 18f), V(3.0f, 19f), 0f, 0.6f));
            h.Paint(SurfaceType.Fairway, new BoxShape(V(0f, 29.6f), V(10.4f, 3.8f), 0f, 0.8f));
            h.Paint(SurfaceType.Green, new PolygonShape(new[] { V(-4.6f, 34.3f), V(4.6f, 34.3f), V(5.6f, 36.3f), V(4.9f, 38.8f), V(-4.9f, 38.8f), V(-5.6f, 36.3f) }, 0.9f), "green");
            Bunker(h, "parterre", new EllipseShape(V(7.5f, 12.2f), V(1.5f, 2.6f)));
            Bunker(h, "parterre", new EllipseShape(V(-7.5f, 23.4f), V(1.45f, 2.5f)));
            Bunker(h, "parterre", new EllipseShape(V(-7.0f, 31.8f), V(1.6f, 1.2f), 20f));

            // ---------------------------------------------------------------- obstacles
            // Terrace balustrade, open in the middle of the front so the ball can drop through.
            h.Wall(WallStyle.Balustrade, 0.95f, 0.34f, V(-2.25f, 1.7f), V(-4.2f, 1.7f), V(-4.2f, -4.9f), V(4.2f, -4.9f), V(4.2f, 1.7f), V(2.25f, 1.7f));
            // Clipped hedge boxes in the outer beds.
            h.Block(V(-7.6f, 12.0f), V(3.4f, 5.6f), 0f, 1.0f, BlockStyle.Hedge, 0.3f);
            h.Block(V(7.6f, 23.2f), V(3.4f, 5.6f), 0f, 1.0f, BlockStyle.Hedge, 0.3f);
            // Low box hedges framing the overgrown middle beds (gaps let the straight line through).
            h.Block(V(-2.15f, 14.4f), V(0.5f, 7.6f), 0f, 0.55f, BlockStyle.Hedge, 0.2f);
            h.Block(V(2.15f, 14.4f), V(0.5f, 7.6f), 0f, 0.55f, BlockStyle.Hedge, 0.2f);
            h.Block(V(-2.15f, 23.0f), V(0.5f, 5.8f), 0f, 0.55f, BlockStyle.Hedge, 0.2f);
            h.Block(V(2.15f, 23.0f), V(0.5f, 5.8f), 0f, 0.55f, BlockStyle.Hedge, 0.2f);
            // Topiary at the bed corners.
            h.Post(V(-5.9f, 18.0f), 0.45f, 1.9f, PostStyle.Topiary);
            h.Post(V(5.9f, 18.0f), 0.45f, 1.9f, PostStyle.Topiary);
            h.Post(V(-5.9f, 28.0f), 0.45f, 1.9f, PostStyle.Topiary);
            h.Post(V(5.9f, 28.0f), 0.45f, 1.9f, PostStyle.Topiary);
            // The green's back wall and the urns flanking the apron.
            h.Wall(WallStyle.Sandstone, 0.7f, 0.4f, V(-6.4f, 39.7f), V(6.4f, 39.7f));
            h.Post(V(-6.2f, 32.6f), 0.32f, 1.4f, PostStyle.Urn);
            h.Post(V(6.2f, 32.6f), 0.32f, 1.4f, PostStyle.Urn);

            // ---------------------------------------------------------------- surroundings
            OutOfBounds(h, "west-garden", new BoxShape(V(-14.5f, 19f), V(10f, 56f), 0f, 0.6f), 1.2f, SurfaceType.Rough, EdgeStyle.Stone);
            OutOfBounds(h, "east-garden", new BoxShape(V(14.5f, 19f), V(10f, 56f), 0f, 0.6f), 1.2f, SurfaceType.Rough, EdgeStyle.Stone);
            OutOfBounds(h, "north-terrace", new BoxShape(V(0f, 46f), V(20f, 13f), 0f, 0.5f), 1.2f, SurfaceType.Rough, EdgeStyle.Stone);
            OutOfBounds(h, "south-court", new BoxShape(V(0f, -9.5f), V(20f, 9f), 0f, 0.5f), 0.6f, SurfaceType.Stone, EdgeStyle.Stone);

            Hole2Decor.Add(h);
            return h;
        }
    }

    public static class Hole2Decor
    {
        public static void Add(HoleDef h)
        {
            // Balustrades crown the retaining walls of the sunken garden.
            h.Wall(WallStyle.Balustrade, 0.95f, 0.34f, V(-10.2f, 2.4f), V(-10.2f, 40.4f));
            h.Wall(WallStyle.Balustrade, 0.95f, 0.34f, V(10.2f, 2.4f), V(10.2f, 40.4f));
            // Urns on the tee terrace.
            h.Prop("urn", -3.7f, -4.2f, 0f, 0.9f, 1);
            h.Prop("urn", 3.7f, -4.2f, 0f, 0.9f, 2);
            h.Prop("leaves", 1.6f, -0.4f, 0f, 1f, 2);
            // Overgrowth in the middle beds.
            h.Prop("tuft", -0.8f, 12.4f, 0f, 1.2f, 1);
            h.Prop("tuft_dry", 0.9f, 15.6f, 60f, 1.3f, 2);
            h.Prop("tuft", -0.4f, 21.8f, 20f, 1.2f, 3);
            h.Prop("tuft_dry", 0.7f, 24.9f, 90f, 1.1f, 4);
            h.Prop("leaves_red", 0.2f, 17.5f, 0f, 0.9f, 1);
            // West garden.
            h.Prop("tree_red", -13.2f, 4.2f, 30f, 1.15f, 1);
            h.Prop("tree_amber", -14.0f, 17.6f, 0f, 1.2f, 2);
            h.Prop("tree_saffron", -13.0f, 30.8f, 120f, 1.1f, 3);
            h.Prop("topiary_cone", -11.4f, 10.4f, 0f, 1f, 1);
            h.Prop("topiary_cone", -11.4f, 24.4f, 0f, 1f, 2);
            h.Prop("bush_flowers", -11.6f, 37.2f, 0f, 1.1f, 1);
            h.Prop("wild_mass", -17.0f, 24f, 0f, 1.4f, 2);
            // East garden.
            h.Prop("tree_amber", 13.4f, 7.6f, 70f, 1.2f, 3);
            h.Prop("tree_red", 14.2f, 22.4f, 10f, 1.1f, 2);
            h.Prop("tree_amber", 13.2f, 35.4f, 150f, 1.15f, 1);
            h.Prop("topiary_cone", 11.4f, 15.6f, 0f, 1f, 3);
            h.Prop("topiary_cone", 11.4f, 29.4f, 0f, 1f, 1);
            h.Prop("bush_flowers", 11.8f, 2.4f, 0f, 1.1f, 2);
            h.Prop("arch_ruin", 16.6f, 16f, 90f, 1.1f, 1);
            // North terrace beyond the green: arches, root-bound hedges, trees.
            h.Prop("hedge_wild", -4.6f, 41.6f, 0f, 1f, 1);
            h.Prop("hedge_wild", 4.4f, 41.8f, 0f, 1f, 2);
            h.Prop("arch_ruin", 0f, 45.4f, 0f, 1.2f, 2);
            h.Prop("tree_saffron", -7.6f, 47.8f, 30f, 1.2f, 1);
            h.Prop("tree_red", 7.8f, 48.6f, 160f, 1.25f, 3);
            h.Prop("lily_lantern", -5.2f, 35.4f, 0f, 1f, 1);
            h.Prop("lily_lantern", 5.6f, 38.6f, 0f, 1f, 2);
            // South court below the tee.
            h.Prop("tree_saffron", -6.6f, -9.2f, 0f, 1.1f, 2);
            h.Prop("bush_flowers", 5.6f, -8.6f, 0f, 1.2f, 3);
            h.Prop("topiary_small", 2.2f, -10.4f, 0f, 1f, 1);
            // Distant structures.
            h.PropAt("tower_far", new Vector3(-28f, -1.9f, 58f), 0f, 1.3f);
            h.PropAt("tower_far", new Vector3(24f, -1.9f, 66f), 30f, 1.5f);
            h.PropAt("aqueduct_far", new Vector3(-36f, -1.9f, 30f), 80f, 1.3f);
            h.Scatter_(new BoxShape(V(-14.5f, 19f), V(9f, 50f)), 1.4f, 21, 0.5f, "tuft", "tuft_dry");
            h.Scatter_(new BoxShape(V(14.5f, 19f), V(9f, 50f)), 1.4f, 22, 0.5f, "tuft", "tuft_dry");
        }
    }
}
