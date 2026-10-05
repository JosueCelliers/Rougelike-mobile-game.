using System.Collections.Generic;
using BadLie.Core;
using UnityEngine;

namespace BadLie.Course
{
    public enum HeightKind { Flat, Ramp, Tilt }

    /// <summary>Base height of a pad: flat, a ramp between two points, or a tilted plane.</summary>
    public sealed class HeightDef
    {
        public HeightKind Kind;
        public float H;
        public Vector2 A, B;
        public float HA, HB;
        public Vector2 Slope;

        public static HeightDef Flat(float h) { return new HeightDef { Kind = HeightKind.Flat, H = h }; }

        /// <summary>Height goes linearly from ha at a to hb at b, clamped beyond the ends.</summary>
        public static HeightDef Ramp(Vector2 a, float ha, Vector2 b, float hb)
        {
            return new HeightDef { Kind = HeightKind.Ramp, A = a, B = b, HA = ha, HB = hb };
        }

        /// <summary>Plane through origin with height h and gradient slope (rise per metre in x and z).</summary>
        public static HeightDef Tilt(Vector2 origin, float h, Vector2 slope)
        {
            return new HeightDef { Kind = HeightKind.Tilt, A = origin, H = h, Slope = slope };
        }

        public float Eval(Vector2 p, out Vector2 grad)
        {
            switch (Kind)
            {
                case HeightKind.Ramp:
                {
                    Vector2 ab = B - A;
                    float len2 = Mathf.Max(ab.sqrMagnitude, 1e-8f);
                    float t = Vector2.Dot(p - A, ab) / len2;
                    if (t <= 0f) { grad = Vector2.zero; return HA; }
                    if (t >= 1f) { grad = Vector2.zero; return HB; }
                    grad = ab * ((HB - HA) / len2);
                    return HA + (HB - HA) * t;
                }
                case HeightKind.Tilt:
                    grad = Slope;
                    return H + Vector2.Dot(p - A, Slope);
                default:
                    grad = Vector2.zero;
                    return H;
            }
        }

        public float Max
        {
            get { return Kind == HeightKind.Ramp ? Mathf.Max(HA, HB) : H; }
        }

        public float Min
        {
            get { return Kind == HeightKind.Ramp ? Mathf.Min(HA, HB) : H; }
        }
    }

    public enum FeatureKind { Bowl, Bump }

    /// <summary>Smooth height change inside a pad (bunker bowls, mounds, green undulations).</summary>
    public sealed class FeatureDef
    {
        public FeatureKind Kind;
        public Shape2D Shape;
        public Vector2 Center;
        public float Radius;
        /// <summary>Bowl: depth (positive digs down). Bump: height (positive raises).</summary>
        public float Amount;
        public float Rim;

        public static FeatureDef Bowl(Shape2D shape, float depth, float rim)
        {
            return new FeatureDef { Kind = FeatureKind.Bowl, Shape = shape, Amount = depth, Rim = rim };
        }

        public static FeatureDef Bump(Vector2 center, float radius, float height)
        {
            return new FeatureDef { Kind = FeatureKind.Bump, Center = center, Radius = radius, Amount = height, Shape = new CircleShape(center, radius) };
        }

        public float Eval(Vector2 p, out Vector2 grad)
        {
            grad = Vector2.zero;
            if (Kind == FeatureKind.Bowl)
            {
                float sd = Shape.Distance(p);
                if (sd >= 0f) return 0f;
                float t = -sd / Rim;
                if (t >= 1f) return -Amount;
                float s = Ease.Smooth01(t);
                Vector2 g = Shape.Gradient(p);
                // h = -A * S(t), t = -sd/rim  =>  dh/dp = A * S'(t) * grad(sd) / rim
                grad = g * (Amount * Ease.SmoothDeriv01(t) / Rim);
                return -Amount * s;
            }
            else
            {
                Vector2 d = p - Center;
                float r = d.magnitude;
                if (r >= Radius) return 0f;
                float u = r / Radius;
                float c = Mathf.Cos(Mathf.PI * u) * 0.5f + 0.5f;
                float h = Amount * c * c;
                if (r > 1e-5f)
                {
                    float dc = -Mathf.Sin(Mathf.PI * u) * 0.5f * Mathf.PI / Radius;
                    grad = d / r * (Amount * 2f * c * dc);
                }
                return h;
            }
        }
    }

    public enum EdgeStyle : byte
    {
        Earth,      // soil bank with roots
        Stone,      // coursed sandstone retaining wall
        Brick,      // terracotta brick face
        Machine,    // dark metal skirt
        Coping,     // stone kerb edging used for water channels
    }

    /// <summary>A walkable (or water) slab of ground at a height. Pads can sit on each other.</summary>
    public sealed class PadDef
    {
        public string Id;
        public Shape2D Shape;
        public int Priority;
        public HeightDef Height = HeightDef.Flat(0f);
        public SurfaceType Base = SurfaceType.Rough;
        public bool Water;
        public bool OutOfBounds;
        public EdgeStyle Edge = EdgeStyle.Earth;
        /// <summary>Visual depth of water above its bed.</summary>
        public float WaterDepth = 0.45f;
        public readonly List<FeatureDef> Features = new List<FeatureDef>();
    }

    public sealed class PaintDef
    {
        public Shape2D Shape;
        public SurfaceType Surface;
        /// <summary>Optional: restricts the paint to one pad.</summary>
        public string PadId;
    }

    public enum WallStyle : byte
    {
        Sandstone,
        Balustrade,
        Hedge,
        Curb,
        Ruin,
        Machine,
        Coping,
        Invisible,
    }

    /// <summary>A wall along a path. Height is measured from the ground at its base.</summary>
    public sealed class WallDef
    {
        public List<Vector2> Path = new List<Vector2>();
        public float Thickness = 0.4f;
        public float Height = 0.8f;
        public WallStyle Style = WallStyle.Sandstone;
        public bool Closed;
        /// <summary>Absolute base height; NaN means sample the ground at the wall.</summary>
        public float BaseY = float.NaN;
        public int Seed;

        public WallMaterial Material
        {
            get
            {
                switch (Style)
                {
                    case WallStyle.Hedge: return WallMaterial.Hedge;
                    case WallStyle.Machine: return WallMaterial.Metal;
                    default: return WallMaterial.Stone;
                }
            }
        }
    }

    public enum PostStyle : byte { Column, BrokenColumn, Trunk, Urn, Bollard, Machine, Topiary }

    public sealed class PostDef
    {
        public Vector2 Center;
        public float Radius = 0.25f;
        public float Height = 1.5f;
        public PostStyle Style = PostStyle.Column;
        public float BaseY = float.NaN;
        public int Seed;
    }

    public enum BlockStyle : byte { Hedge, Plinth, Machine, Planter }

    /// <summary>Solid rectangular obstacle such as a clipped hedge block or a plinth.</summary>
    public sealed class BlockDef
    {
        public BoxShape Box;
        public float Height = 1f;
        public BlockStyle Style = BlockStyle.Hedge;
        public float BaseY = float.NaN;
        public int Seed;

        public WallMaterial Material
        {
            get
            {
                switch (Style)
                {
                    case BlockStyle.Hedge: return WallMaterial.Hedge;
                    case BlockStyle.Machine: return WallMaterial.Metal;
                    default: return WallMaterial.Stone;
                }
            }
        }
    }

    public enum ZoneKind : byte { Flow, Push }

    /// <summary>Moving environmental force: flowing water (runnels) or machinery vents.</summary>
    public sealed class ZoneDef
    {
        public ZoneKind Kind;
        public Shape2D Shape;
        /// <summary>Flow: direction follows this centreline.</summary>
        public List<Vector2> FlowPath;
        /// <summary>Flow: water speed (m/s). Push: acceleration (m/s^2).</summary>
        public float Strength;
        public Vector2 PushDir;
        public string Label;
    }

    public sealed class DecorDef
    {
        public string Kit;
        public Vector3 Position;
        public float Yaw;
        public float Scale = 1f;
        /// <summary>Use the ground height under the prop instead of Position.y.</summary>
        public bool Ground = true;
        public int Seed;
        public Color Tint = Color.white;
    }

    /// <summary>Procedural placement of decor kit pieces inside an area.</summary>
    public sealed class ScatterDef
    {
        public string[] Kits;
        public Shape2D Area;
        public float Spacing = 1.5f;
        public Vector2 ScaleRange = new Vector2(0.85f, 1.15f);
        public int Seed;
        /// <summary>Keep this far from any playable surface.</summary>
        public float Clearance;
        public bool Ground = true;
        public float FixedY;
    }

    public sealed class HoleDef
    {
        public string Id;
        public string Name;
        public string Subtitle;
        public int Par = 3;
        public Vector2 Tee;
        public float TeeYaw;
        public Vector2 Cup;
        public float CameraYaw;
        public float StripeAngle;
        /// <summary>Region the camera may survey.</summary>
        public Rect PlayBounds;
        public float LowerWaterLevel = -1.9f;

        public readonly List<PadDef> Pads = new List<PadDef>();
        public readonly List<PaintDef> Paints = new List<PaintDef>();
        public readonly List<WallDef> Walls = new List<WallDef>();
        public readonly List<PostDef> Posts = new List<PostDef>();
        public readonly List<BlockDef> Blocks = new List<BlockDef>();
        public readonly List<ZoneDef> Zones = new List<ZoneDef>();
        public readonly List<DecorDef> Decor = new List<DecorDef>();
        public readonly List<ScatterDef> Scatter = new List<ScatterDef>();

        /// <summary>Alternative pin positions used by controlled run variation.</summary>
        public readonly List<Vector2> AltCups = new List<Vector2>();
        /// <summary>Upgrade opportunities shown on the hole card.</summary>
        public readonly List<string> Features = new List<string>();

        // Fluent helpers used by the hole library.
        public PadDef Pad(string id, Shape2D shape, int priority, HeightDef height, SurfaceType baseSurface = SurfaceType.Rough, EdgeStyle edge = EdgeStyle.Earth)
        {
            var p = new PadDef { Id = id, Shape = shape, Priority = priority, Height = height, Base = baseSurface, Edge = edge };
            Pads.Add(p);
            return p;
        }

        public PadDef WaterPad(string id, Shape2D shape, int priority, float level, float depth = 0.45f)
        {
            var p = new PadDef { Id = id, Shape = shape, Priority = priority, Height = HeightDef.Flat(level), Base = SurfaceType.Water, Water = true, WaterDepth = depth, Edge = EdgeStyle.Coping };
            Pads.Add(p);
            return p;
        }

        public PaintDef Paint(SurfaceType s, Shape2D shape, string padId = null)
        {
            var p = new PaintDef { Surface = s, Shape = shape, PadId = padId };
            Paints.Add(p);
            return p;
        }

        public WallDef Wall(WallStyle style, float height, float thickness, params Vector2[] path)
        {
            var w = new WallDef { Style = style, Height = height, Thickness = thickness, Seed = Walls.Count * 7 + 3 };
            w.Path.AddRange(path);
            Walls.Add(w);
            return w;
        }

        public WallDef Wall(WallStyle style, float height, float thickness, List<Vector2> path)
        {
            var w = new WallDef { Style = style, Height = height, Thickness = thickness, Path = path, Seed = Walls.Count * 7 + 3 };
            Walls.Add(w);
            return w;
        }

        public PostDef Post(Vector2 c, float radius, float height, PostStyle style)
        {
            var p = new PostDef { Center = c, Radius = radius, Height = height, Style = style, Seed = Posts.Count * 13 + 5 };
            Posts.Add(p);
            return p;
        }

        public BlockDef Block(Vector2 center, Vector2 size, float angle, float height, BlockStyle style, float corner = 0.12f)
        {
            var b = new BlockDef { Box = new BoxShape(center, size, angle, corner), Height = height, Style = style, Seed = Blocks.Count * 11 + 1 };
            Blocks.Add(b);
            return b;
        }

        public ZoneDef Flow(Shape2D area, float speed, string label, params Vector2[] path)
        {
            var z = new ZoneDef { Kind = ZoneKind.Flow, Shape = area, Strength = speed, FlowPath = new List<Vector2>(path), Label = label };
            Zones.Add(z);
            return z;
        }

        public ZoneDef Push(Shape2D area, Vector2 dir, float accel, string label)
        {
            var z = new ZoneDef { Kind = ZoneKind.Push, Shape = area, PushDir = dir.normalized, Strength = accel, Label = label };
            Zones.Add(z);
            return z;
        }

        public DecorDef Prop(string kit, float x, float z, float yaw = 0f, float scale = 1f, int seed = 0)
        {
            var d = new DecorDef { Kit = kit, Position = new Vector3(x, 0f, z), Yaw = yaw, Scale = scale, Seed = seed == 0 ? Decor.Count * 17 + 9 : seed };
            Decor.Add(d);
            return d;
        }

        public DecorDef PropAt(string kit, Vector3 pos, float yaw = 0f, float scale = 1f)
        {
            var d = new DecorDef { Kit = kit, Position = pos, Yaw = yaw, Scale = scale, Ground = false, Seed = Decor.Count * 17 + 9 };
            Decor.Add(d);
            return d;
        }

        public ScatterDef Scatter_(Shape2D area, float spacing, int seed, float clearance, params string[] kits)
        {
            var s = new ScatterDef { Area = area, Spacing = spacing, Seed = seed, Clearance = clearance, Kits = kits };
            Scatter.Add(s);
            return s;
        }
    }
}
