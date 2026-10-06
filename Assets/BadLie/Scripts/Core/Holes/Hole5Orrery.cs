using BadLie.Core;
using BadLie.Course;
using UnityEngine;
using static BadLie.Holes.HoleKit;

namespace BadLie.Holes
{
    /// <summary>
    /// Hole 5 — The Orrery. The estate's great machine: a stone dais ringed by vents that bend
    /// the ball clockwise, a moat round a raised, domed island green and two narrow bridges.
    /// Below it, a meadow shortcut, bunkers and a runnel that spills into a basin.
    /// </summary>
    public static class Hole5Orrery
    {
        public static HoleDef Build()
        {
            var h = new HoleDef
            {
                Id = "orrery",
                Name = "The Orrery",
                Subtitle = "The old machine still turns the air. Cross the moat however you can.",
                Par = 5,
                Tee = V(0f, -1.5f),
                Cup = V(0.5f, 34.0f),
                CameraYaw = 4f,
                StripeAngle = 90f,
                PlayBounds = Rect.MinMaxRect(-10f, -5f, 10f, 44f),
                LowerWaterLevel = -1.9f,
            };
            h.AltCups.Add(V(-1.2f, 33.0f));
            h.AltCups.Add(V(1.3f, 32.6f));
            h.Features.Add("Vents bend the ball");
            h.Features.Add("Moat: take a bridge, or skip the gap");
            h.Features.Add("Meadow shortcut");

            Vector2 c = V(0f, 33.5f);
            // ---------------------------------------------------------------- ground
            h.Pad("tee", new BoxShape(V(0f, -1.3f), V(4.6f, 3.6f), 0f, 0.4f), 6, HeightDef.Flat(0.3f), SurfaceType.Stone, EdgeStyle.Stone);
            h.Pad("meadow", new PolygonShape(new[] { V(-9f, -3.4f), V(9f, -3.4f), V(10f, 26f), V(-10f, 26f) }, 0.6f), 2, HeightDef.Flat(0f), SurfaceType.Rough, EdgeStyle.Earth);
            h.WaterPad("basin", new BoxShape(V(8.9f, 18.6f), V(3.0f, 5.2f), 0f, 0.6f), 3, -0.25f, 0.5f);
            h.Pad("footbridge", new BoxShape(V(1.0f, 18.6f), V(2.4f, 2.8f), 0f, 0.2f), 4, HeightDef.Flat(0f), SurfaceType.Stone, EdgeStyle.Coping);
            h.Pad("ramp", new BoxShape(V(0.6f, 23.6f), V(4.4f, 2.8f), 0f, 0.15f), 4, HeightDef.Ramp(V(0.6f, 22.6f), 0f, V(0.6f, 24.4f), 0.5f), SurfaceType.Stone, EdgeStyle.Machine);
            h.Pad("dais", new CircleShape(c, 9.0f), 5, HeightDef.Flat(0.5f), SurfaceType.Stone, EdgeStyle.Machine);
            h.WaterPad("moat", new SubtractShape(new CircleShape(c, 6.0f), new CircleShape(c, 4.3f)), 6, 0.15f, 0.4f);
            var island = h.Pad("island", new CircleShape(c, 4.4f), 7, HeightDef.Flat(0.85f), SurfaceType.Fairway, EdgeStyle.Coping);
            island.Features.Add(FeatureDef.Bump(V(0.3f, 33.8f), 4.2f, 0.14f));
            // Two narrow bridges ramp from the dais up onto the island.
            Bridge(h, "bridge-sw", c, 225f);
            Bridge(h, "bridge-ne", c, 45f);

            // ---------------------------------------------------------------- surfaces
            h.Paint(SurfaceType.Fairway, new CapsuleChainShape(new[] { V(0f, 0.6f), V(3.4f, 9f), V(3.2f, 15.4f), V(1.0f, 21.6f) }, 2.3f));
            h.Paint(SurfaceType.Green, new CircleShape(c, 3.6f), "island");
            Bunker(h, "meadow", new EllipseShape(V(-2.3f, 11.4f), V(1.6f, 1.1f), -20f));
            Bunker(h, "meadow", new EllipseShape(V(-0.4f, 14.5f), V(1.3f, 1.0f), 15f));
            Bunker(h, "meadow", new EllipseShape(V(6.5f, 12.0f), V(1.2f, 1.9f)));
            // The runnel crosses the meadow and spills into the basin; the footbridge is dry.
            var band = new BoxShape(V(0f, 18.6f), V(20f, 1.8f), 0f, 0.2f);
            h.Paint(SurfaceType.Runnel, band, "meadow");
            h.Flow(new SubtractShape(band, new BoxShape(V(1.0f, 18.6f), V(2.4f, 2.8f), 0f, 0.2f)), 1.6f, "runnel", V(-10f, 18.6f), V(10f, 18.6f));
            // Vents on the dais bend shots clockwise round the moat.
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f;
                Vector2 dir = Geo2D.FromAngle(a);
                Vector2 tangent = new Vector2(dir.y, -dir.x); // clockwise
                h.Push(new CircleShape(c + dir * 7.5f, 1.35f), tangent, 1.9f, "vent");
            }

            // ---------------------------------------------------------------- walls
            var rim = new ArcShape(c, 8.75f, 288f, 612f, 0.15f);
            h.Wall(WallStyle.Machine, 0.32f, 0.3f, rim.SamplePoints(48)).BaseY = 0.5f;
            // A low coping round the moat, open at both bridges and at the skip gap facing the ramp.
            foreach (var arc in new[] { new Vector2(57f, 213f), new Vector2(237f, 259f), new Vector2(281f, 393f) })
            {
                var coping = new ArcShape(c, 6.13f, arc.x, arc.y, 0.08f);
                h.Wall(WallStyle.Coping, 0.16f, 0.16f, coping.SamplePoints(Mathf.CeilToInt((arc.y - arc.x) / 6f))).BaseY = 0.5f;
            }
            h.Wall(WallStyle.Curb, 0.15f, 0.16f, V(-0.25f, 17.3f), V(-0.25f, 19.9f));
            h.Wall(WallStyle.Curb, 0.15f, 0.16f, V(2.25f, 17.3f), V(2.25f, 19.9f));
            h.Wall(WallStyle.Balustrade, 0.95f, 0.34f, V(-2.2f, -3.2f), V(2.2f, -3.2f));
            h.Wall(WallStyle.Hedge, 1.0f, 0.9f, V(-8.6f, -2.8f), V(-9.6f, 21.6f));
            h.Wall(WallStyle.Hedge, 1.0f, 0.9f, V(8.6f, -2.8f), V(9.4f, 15.6f));
            h.Post(V(-4.6f, 6.2f), 0.5f, 2.2f, PostStyle.Topiary);
            h.Post(V(5.6f, 4.0f), 0.5f, 2.2f, PostStyle.Topiary);

            // ---------------------------------------------------------------- surroundings
            OutOfBounds(h, "west-meadow", new BoxShape(V(-15f, 9f), V(11f, 28f), 0f, 0.6f), 0.6f, SurfaceType.Rough, EdgeStyle.Earth);
            OutOfBounds(h, "east-meadow", new BoxShape(V(15f, 9f), V(11f, 28f), 0f, 0.6f), 0.6f, SurfaceType.Rough, EdgeStyle.Earth);
            OutOfBounds(h, "south-court", new BoxShape(V(0f, -6.6f), V(16f, 7f), 0f, 0.4f), -0.35f, SurfaceType.Stone, EdgeStyle.Stone);

            Hole5Decor.Add(h, c);
            return h;
        }

        static void Bridge(HoleDef h, string id, Vector2 centre, float angle)
        {
            Vector2 dir = Geo2D.FromAngle(angle);
            Vector2 inner = centre + dir * 4.5f, outer = centre + dir * 6.4f;
            Vector2 mid = centre + dir * 5.45f;
            h.Pad(id, new BoxShape(mid, V(2.8f, 2.3f), angle, 0.15f), 8, HeightDef.Ramp(outer, 0.5f, inner, 0.85f), SurfaceType.Stone, EdgeStyle.Coping);
        }
    }

    public static class Hole5Decor
    {
        public static void Add(HoleDef h, Vector2 c)
        {
            h.PropAt("orrery_rings", new Vector3(c.x, 0.505f, c.y), 0f, 1f);
            for (int i = 0; i < 4; i++)
            {
                Vector2 p = c + Geo2D.FromAngle(i * 90f) * 7.5f;
                h.PropAt("vent_small", new Vector3(p.x, 0.5f, p.y), i * 90f, 1f);
            }
            h.Prop("lily_lantern", -3.2f, 37.8f, 0f, 1f, 1);
            h.Prop("lily_lantern", 3.4f, 29.6f, 0f, 1f, 2);
            h.PropAt("lily_pads", new Vector3(-4.9f, 0.15f, 36.4f), 0f, 0.5f);
            h.PropAt("lily_pads", new Vector3(5.0f, 0.15f, 31.0f), 0f, 0.5f);
            // Meadow: wild grasses on the direct line and around.
            h.Prop("tuft_dry", -3.8f, 9.6f, 0f, 1.3f, 1);
            h.Prop("tuft", -5.6f, 14.6f, 0f, 1.2f, 2);
            h.Prop("tuft_dry", -6.4f, 4.2f, 0f, 1.2f, 3);
            h.Prop("tuft", -3.0f, 18.0f, 0f, 1.1f, 4);
            h.Prop("leaves", 1.8f, 3.4f, 0f, 1.2f, 1);
            h.Prop("reeds", 9.9f, 16.4f, 0f, 1f, 2);
            // Beyond: banks, a sluice feeding the runnel, the tower that watches over the hole.
            h.PropAt("sluice_gate", new Vector3(-10.6f, 0.6f, 18.6f), -90f, 1.0f);
            h.Prop("tree_red", -13.6f, 2.4f, 0f, 1.2f, 1);
            h.Prop("tree_amber", -14.2f, 14.6f, 0f, 1.25f, 2);
            h.Prop("tree_saffron", 13.6f, 4.6f, 0f, 1.15f, 3);
            h.Prop("tree_red", 14.4f, 17.2f, 0f, 1.2f, 2);
            h.Prop("wild_mass", -12.2f, 21.0f, 0f, 1.3f, 1);
            h.Prop("wild_mass", 12.4f, 21.8f, 0f, 1.2f, 2);
            h.PropAt("tower_far", new Vector3(-24f, -1.9f, 86f), 0f, 1.5f);
            h.PropAt("tower_far", new Vector3(-26f, -1.9f, 50f), 40f, 1.3f);
            h.PropAt("tower_far", new Vector3(24f, -1.9f, 58f), 80f, 1.4f);
            h.PropAt("ruin_wall", new Vector3(-11.6f, -2.3f, 31.0f), 60f, 1.1f);
            h.PropAt("ruin_wall", new Vector3(11.8f, -2.3f, 35.6f), 120f, 1.05f);
            h.PropAt("column_broken", new Vector3(-10.6f, -2.1f, 40.6f), 0f, 1.2f);
            h.PropAt("column", new Vector3(10.4f, -2.1f, 27.6f), 0f, 1.2f);
            h.PropAt("lily_pads", new Vector3(-10.0f, -1.9f, 25.6f), 0f, 1.3f);
            h.PropAt("lily_pads", new Vector3(9.6f, -1.9f, 41.4f), 40f, 1.2f);
            // Beyond the dais: the drowned end of the machine hall.
            h.PropAt("sunken_slabs", new Vector3(2.6f, -1.9f, 49.5f), 20f, 1.2f);
            h.PropAt("ruin_wall", new Vector3(-6.4f, -2.3f, 52.0f), 10f, 1.1f);
            h.PropAt("column_broken", new Vector3(6.8f, -2.1f, 47.6f), 0f, 1.2f);
            h.PropAt("column", new Vector3(-1.8f, -2.1f, 56.4f), 0f, 1.25f);
            h.PropAt("lily_pads", new Vector3(-3.4f, -1.9f, 46.8f), 70f, 1.3f);
            h.PropAt("lily_pads", new Vector3(8.4f, -1.9f, 54.6f), 10f, 1.1f);
            h.PropAt("reeds", new Vector3(-8.8f, -1.9f, 45.2f), 0f, 1.1f);
            h.PropAt("aqueduct_far", new Vector3(0f, -1.9f, 66f), 0f, 1.2f);
            h.Prop("bush_flowers", -5.4f, -5.6f, 0f, 1.1f, 1);
            h.Prop("bush_flowers", 5.6f, -5.4f, 0f, 1.1f, 2);
            h.Prop("urn", -2.6f, -7.4f, 0f, 1f, 1);
            h.Prop("urn", 2.6f, -7.4f, 0f, 1f, 2);
            h.Scatter_(new BoxShape(V(-15f, 9f), V(10f, 26f)), 1.4f, 51, 0.6f, "tuft", "tuft_dry");
            h.Scatter_(new BoxShape(V(15f, 9f), V(10f, 26f)), 1.4f, 52, 0.6f, "tuft_dry", "tuft");
        }
    }
}
