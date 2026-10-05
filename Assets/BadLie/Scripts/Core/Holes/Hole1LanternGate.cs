using BadLie.Core;
using BadLie.Course;
using UnityEngine;

namespace BadLie.Holes
{
    /// <summary>
    /// Hole 1 — The Lantern Gate. The representative hole from the brief: a narrow dogleg
    /// fairway from a tee under an amber tree, a curved sandstone bank wall at the bend, a dark
    /// canal guarding a raised green, and a longer curbed stone route around the water.
    /// </summary>
    public static class Hole1LanternGate
    {
        static Vector2 V(float x, float z) { return new Vector2(x, z); }

        public static HoleDef Build()
        {
            var h = new HoleDef
            {
                Id = "lantern-gate",
                Name = "The Lantern Gate",
                Subtitle = "Bank off the old wall, or take the long stone walk.",
                Par = 4,
                Tee = V(0f, -0.3f),
                TeeYaw = 0f,
                Cup = V(3.3f, 30.3f),
                CameraYaw = 10f,
                StripeAngle = 90f,
                PlayBounds = Rect.MinMaxRect(-8f, -5f, 16f, 36f),
                LowerWaterLevel = -1.9f,
            };
            h.AltCups.Add(V(5.4f, 29.2f));
            h.AltCups.Add(V(1.9f, 29.3f));
            h.Features.Add("Curved bank wall");
            h.Features.Add("Canal: skip it to reach the green directly");

            // ---------------------------------------------------------------- ground
            var lawn = new PolygonShape(new[]
            {
                V(-5.6f, -3.6f), V(5.6f, -3.6f), V(6.2f, 8.2f), V(14.2f, 11.4f),
                V(14.2f, 26.4f), V(9.4f, 26.4f), V(9.4f, 20.5f), V(-6.1f, 20.5f), V(-6.4f, 8f),
            }, 0.5f);
            h.Pad("lawn", lawn, 1, HeightDef.Flat(0f), SurfaceType.Rough, EdgeStyle.Stone);

            h.Pad("tee", new BoxShape(V(0f, -0.4f), V(2.6f, 2.2f), 0f, 0.35f), 6, HeightDef.Flat(0f), SurfaceType.Stone, EdgeStyle.Stone);

            // Canal: dark water just below the lawn, ending at an old sluice on the left.
            var canal = new BoxShape(V(1.225f, 23.0f), V(16.75f, 5.8f), 0f, 0f);
            h.WaterPad("canal", canal, 0, -0.14f, 0.5f);

            // Raised plateau (rough fringe) carrying the green.
            var plateau = new PolygonShape(new[]
            {
                V(-1.8f, 25.7f), V(8.7f, 25.7f), V(9.9f, 27.4f), V(9.6f, 33.2f), V(5.8f, 34.6f), V(-0.8f, 34.5f), V(-2.6f, 31.4f),
            }, 0.7f);
            var plat = h.Pad("plateau", plateau, 8, HeightDef.Flat(0.62f), SurfaceType.Rough, EdgeStyle.Stone);
            plat.Features.Add(FeatureDef.Bump(V(5.6f, 31.8f), 2.6f, 0.07f));

            // Apron ramp from the canal's far bank up to the plateau: the skip landing.
            var apron = new BoxShape(V(3.6f, 24.6f), V(6.4f, 2.8f), 0f, 0.25f);
            h.Pad("apron", apron, 7, HeightDef.Ramp(V(3.6f, 23.4f), 0f, V(3.6f, 25.6f), 0.62f), SurfaceType.Fairway, EdgeStyle.Stone);
            h.Pad("apron-foot", new BoxShape(V(3.6f, 23.15f), V(6.4f, 0.5f), 0f, 0.15f), 7, HeightDef.Flat(0f), SurfaceType.Fairway, EdgeStyle.Coping);

            // Long stone walk: flat, ramp, flat, onto the plateau from the right.
            // Adjoining walk pads share one height function so their seams never form ledges.
            var walkHeight = HeightDef.Ramp(V(12.1f, 19.0f), 0f, V(12.1f, 25.3f), 0.62f);
            var walkLow = new CapsuleChainShape(new[] { V(9.0f, 15.9f), V(11.7f, 16.9f), V(12.1f, 18.6f) }, 1.2f);
            h.Pad("walk-low", walkLow, 5, walkHeight, SurfaceType.Stone, EdgeStyle.Brick);
            h.Pad("walk-ramp", new BoxShape(V(12.1f, 22.5f), V(2.4f, 7.0f), 0f, 0f), 5, walkHeight, SurfaceType.Stone, EdgeStyle.Brick);
            var walkTop = new CapsuleChainShape(new[] { V(12.1f, 25.6f), V(11.7f, 27.0f), V(9.4f, 28.2f) }, 1.2f);
            h.Pad("walk-top", walkTop, 9, walkHeight, SurfaceType.Stone, EdgeStyle.Brick);

            // ---------------------------------------------------------------- surfaces
            var fairwayLine = new CapsuleChainShape(new[] { V(0f, 0.9f), V(-0.3f, 6.5f), V(0.9f, 11.6f), V(4.3f, 15.2f), V(9.2f, 16.1f) }, 2.15f);
            h.Paint(SurfaceType.Fairway, fairwayLine);
            h.Paint(SurfaceType.Fairway, new BoxShape(V(5.0f, 18.9f), V(7.4f, 3.4f), 6f, 1.2f));
            h.Paint(SurfaceType.Green, new PolygonShape(new[]
            {
                V(0.4f, 27.8f), V(6.6f, 27.4f), V(7.6f, 29.8f), V(6.8f, 32.4f), V(2.6f, 33.0f), V(0.2f, 31.6f),
            }, 0.9f), "plateau");

            // Bunker on the inside of the dogleg punishes cutting the corner.
            var bunker = new EllipseShape(V(5.6f, 11.6f), V(1.9f, 1.25f), 25f);
            h.Paint(SurfaceType.Sand, bunker, "lawn");
            FindPad(h, "lawn").Features.Add(FeatureDef.Bowl(bunker, 0.16f, 0.7f));

            // ---------------------------------------------------------------- walls
            // The curved sandstone bank wall around the outside of the bend.
            var arc = new ArcShape(V(3.0f, 12.5f), 7.5f, 104f, 176f, 0.22f);
            h.Wall(WallStyle.Sandstone, 0.85f, 0.44f, arc.SamplePoints(20));

            // Crimson balustrade along the open left edge over the flooded estate.
            h.Wall(WallStyle.Balustrade, 0.95f, 0.34f, V(-5.3f, -3.2f), V(-5.75f, 8f), V(-5.6f, 13.6f));
            h.Wall(WallStyle.Balustrade, 0.95f, 0.34f, V(-5.3f, -3.25f), V(5.2f, -3.25f));
            // Root-bound hedge on the right of the fairway.
            h.Wall(WallStyle.Hedge, 1.05f, 0.9f, V(5.0f, -3.0f), V(5.5f, 3.8f), V(5.8f, 7.6f));
            h.Wall(WallStyle.Hedge, 1.05f, 0.9f, V(6.9f, 9.0f), V(13.6f, 11.8f));
            // Right boundary along the stone walk.
            h.Wall(WallStyle.Balustrade, 0.95f, 0.34f, V(13.9f, 12.2f), V(13.9f, 26.0f));
            // A low ruined wall keeps putts from rolling off the plateau's west side.
            h.Wall(WallStyle.Ruin, 0.5f, 0.42f, V(-1.5f, 26.1f), V(-2.25f, 31.2f), V(-0.55f, 34.1f));
            // Back hedge behind the green.
            h.Wall(WallStyle.Hedge, 1.15f, 0.9f, V(-2.2f, 33.8f), V(1.2f, 35.4f), V(6.0f, 35.5f), V(9.4f, 33.6f));

            // Terracotta curbs keep the stone walk honest.
            h.Wall(WallStyle.Curb, 0.15f, 0.18f, V(9.2f, 14.75f), V(11.9f, 15.7f), V(13.35f, 17.4f), V(13.35f, 25.4f));
            h.Wall(WallStyle.Curb, 0.15f, 0.18f, V(10.85f, 18.7f), V(10.85f, 25.6f));
            h.Wall(WallStyle.Curb, 0.15f, 0.18f, V(13.3f, 25.6f), V(12.9f, 27.6f), V(10.5f, 29.4f));

            // Low copings along the apron's sides keep a skipped ball on the landing ramp.
            h.Wall(WallStyle.Coping, 0.22f, 0.24f, V(0.55f, 23.0f), V(0.55f, 25.75f));
            h.Wall(WallStyle.Coping, 0.22f, 0.24f, V(6.65f, 23.0f), V(6.65f, 25.75f));

            // Stone coping where the canal meets the fairway to the left of the bank wall,
            // and the sluice wall that closes the canal.
            h.Wall(WallStyle.Coping, 0.32f, 0.3f, V(-6.0f, 20.35f), V(-1.4f, 20.35f));
            h.Wall(WallStyle.Coping, 0.55f, 0.5f, V(-7.25f, 20.1f), V(-7.25f, 25.8f));

            // The tee tree (its trunk is in play) and topiary flanking the stone walk.
            h.Post(V(-3.5f, -2.0f), 0.26f, 3.0f, PostStyle.Trunk);
            h.Post(V(8.6f, 13.6f), 0.55f, 2.3f, PostStyle.Topiary);
            h.Post(V(13.1f, 14.2f), 0.55f, 2.3f, PostStyle.Topiary);

            // Lamp posts at the canal bank flank the skip line.
            h.Post(V(1.4f, 20.1f), 0.22f, 1.6f, PostStyle.Bollard);
            h.Post(V(8.9f, 20.1f), 0.22f, 1.6f, PostStyle.Bollard);

            // ---------------------------------------------------------------- surroundings
            // Out-of-bounds ground: terraces and planting beds that frame the course. They sit at
            // priority 0 and tuck under the playable pads so no seams open between them.
            OutOfBounds(h, "terrace-low", new BoxShape(V(0.4f, -7.2f), V(16.4f, 8.6f), 0f, 0.4f), -0.85f, SurfaceType.Stone, EdgeStyle.Stone);
            OutOfBounds(h, "bed-right", new PolygonShape(new[] { V(4.9f, -3.3f), V(13.9f, -3.3f), V(13.9f, 10.6f), V(5.6f, 7.9f) }, 0.4f), 0.42f, SurfaceType.Rough, EdgeStyle.Brick);
            OutOfBounds(h, "garden-east", new PolygonShape(new[] { V(13.4f, -3.4f), V(23f, -3.4f), V(23f, 36f), V(13.4f, 36f) }, 0.6f), 0.25f, SurfaceType.Rough, EdgeStyle.Earth);
            OutOfBounds(h, "terrace-back", new PolygonShape(new[] { V(-6f, 32.8f), V(13.6f, 32.0f), V(14.4f, 46f), V(-6f, 46f) }, 0.5f), 1.05f, SurfaceType.Rough, EdgeStyle.Stone);
            OutOfBounds(h, "west-bank", new PolygonShape(new[] { V(-12.5f, 25.6f), V(-0.4f, 25.6f), V(-0.6f, 35.4f), V(-12.5f, 36f) }, 0.4f), 0.2f, SurfaceType.Rough, EdgeStyle.Earth);
            OutOfBounds(h, "isle", new PolygonShape(new[] { V(-21f, -1f), V(-13.5f, 0.5f), V(-12.2f, 10f), V(-13.4f, 21f), V(-21f, 23f) }, 1.0f), -1.45f, SurfaceType.Rough, EdgeStyle.Earth);

            Hole1Decor.Add(h);
            return h;
        }

        static void OutOfBounds(HoleDef h, string id, Shape2D shape, float height, SurfaceType surface, EdgeStyle edge)
        {
            var p = h.Pad(id, shape, 0, HeightDef.Flat(height), surface, edge);
            p.OutOfBounds = true;
        }

        static PadDef FindPad(HoleDef h, string id)
        {
            foreach (var p in h.Pads) if (p.Id == id) return p;
            return null;
        }
    }
}
