using System;

namespace BadLie.Course
{
    /// <summary>
    /// Playing surfaces. The numeric order is also the paint priority: when painted regions
    /// overlap, the higher value wins, both in physics and in the terrain shader.
    /// </summary>
    public enum SurfaceType : byte
    {
        Rough = 0,
        Fairway = 1,
        Green = 2,
        Runnel = 3,
        Stone = 4,
        Sand = 5,
        Water = 6,
    }

    public enum WallMaterial : byte
    {
        Stone = 0,
        Hedge = 1,
        Metal = 2,
        Wood = 3,
        Ledge = 4,
    }

    /// <summary>How a surface treats a rolling or landing ball.</summary>
    [Serializable]
    public struct SurfaceProps
    {
        /// <summary>Constant rolling deceleration (m/s^2).</summary>
        public float RollDecel;
        /// <summary>Speed-proportional deceleration (1/s).</summary>
        public float LinearDrag;
        /// <summary>Fraction of horizontal speed kept on a hard landing.</summary>
        public float LandingKeep;
        /// <summary>Vertical restitution on landing.</summary>
        public float LandingBounce;

        public SurfaceProps(float rollDecel, float linearDrag, float landingKeep, float landingBounce)
        {
            RollDecel = rollDecel;
            LinearDrag = linearDrag;
            LandingKeep = landingKeep;
            LandingBounce = landingBounce;
        }
    }

    public struct WallProps
    {
        public float Restitution;
        public float TangentKeep;

        public WallProps(float restitution, float tangentKeep)
        {
            Restitution = restitution;
            TangentKeep = tangentKeep;
        }
    }

    public static class SurfaceNames
    {
        public static string Display(SurfaceType s)
        {
            switch (s)
            {
                case SurfaceType.Rough: return "Rough";
                case SurfaceType.Fairway: return "Fairway";
                case SurfaceType.Green: return "Green";
                case SurfaceType.Runnel: return "Runnel";
                case SurfaceType.Stone: return "Stone";
                case SurfaceType.Sand: return "Sand";
                case SurfaceType.Water: return "Water";
            }
            return s.ToString();
        }
    }
}
