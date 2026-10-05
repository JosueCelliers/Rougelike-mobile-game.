using BadLie.Course;

namespace BadLie.Sim
{
    /// <summary>
    /// Every physics constant of the ball simulation, kept in one place so feel can be tuned
    /// without touching the integrator. Units: metres, seconds.
    /// </summary>
    public sealed class SimTuning
    {
        public float Gravity = 9.81f;
        public float BallRadius = 0.14f;
        public float Dt = 1f / 240f;
        public float MaxShotTime = 24f;

        /// <summary>Full-power launch speed before upgrades.</summary>
        public float MaxLaunchSpeed = 9.6f;
        public float MinLaunchSpeed = 0.45f;

        /// <summary>Rolling-ball factor applied to the slope component of gravity (5/7 for a solid sphere).</summary>
        public float SlopeFactor = 5f / 7f;
        public float StopSpeed = 0.06f;
        /// <summary>A resting ball moves again only if external acceleration exceeds RollDecel * this.</summary>
        public float StaticFactor = 1.0f;

        /// <summary>Height differences smaller than this are rolled over; larger ones are ledges.</summary>
        public float StepUp = 0.035f;
        public float StepDown = 0.035f;
        public float BounceThreshold = 1.1f;

        public float VoidLevel = -1.6f;

        // Cup
        public float CupRadius = 0.32f;
        public float CaptureRadius = 0.30f;
        /// <summary>Max speed captured when passing dead centre.</summary>
        public float CaptureSpeedCentre = 2.35f;
        /// <summary>Extra allowance so slow balls at the rim always drop.</summary>
        public float CaptureSpeedFloor = 0.35f;
        public float LipOutSpeedKeep = 0.82f;
        public float LipOutMaxDeflectDeg = 24f;

        // Water skipping (Skip Stone)
        public float SkipMinSpeed = 3.0f;
        public float SkipVerticalFactor = 0.42f;
        public float SkipMaxVertical = 3.4f;
        public float SkipHorizontalKeep = 0.86f;

        // Walls
        public WallProps StoneWall = new WallProps(0.56f, 0.90f);
        public WallProps HedgeWall = new WallProps(0.26f, 0.80f);
        public WallProps MetalWall = new WallProps(0.62f, 0.92f);
        public WallProps WoodWall = new WallProps(0.45f, 0.88f);
        public WallProps LedgeWall = new WallProps(0.42f, 0.86f);
        /// <summary>Bank Shot: the first rebound of each shot uses these instead.</summary>
        public WallProps BankShotWall = new WallProps(0.92f, 0.985f);

        // Zones
        public float FlowCoupling = 2.6f;

        // Cup Magnet
        public float MagnetCaptureRadius = 0.43f;
        public float MagnetMaxSpeed = 1.5f;
        public float MagnetPullRadius = 1.15f;
        public float MagnetPull = 0.9f;

        readonly SurfaceProps[] surfaces = new SurfaceProps[7];

        public SimTuning()
        {
            // RollDecel, LinearDrag, LandingKeep, LandingBounce
            surfaces[(int)SurfaceType.Rough] = new SurfaceProps(5.4f, 0.48f, 0.62f, 0.14f);
            surfaces[(int)SurfaceType.Fairway] = new SurfaceProps(2.15f, 0.12f, 0.90f, 0.28f);
            surfaces[(int)SurfaceType.Green] = new SurfaceProps(1.30f, 0.08f, 0.93f, 0.22f);
            surfaces[(int)SurfaceType.Runnel] = new SurfaceProps(1.20f, 0.10f, 0.80f, 0.10f);
            surfaces[(int)SurfaceType.Stone] = new SurfaceProps(1.60f, 0.10f, 0.95f, 0.42f);
            surfaces[(int)SurfaceType.Sand] = new SurfaceProps(9.5f, 1.45f, 0.30f, 0.0f);
            surfaces[(int)SurfaceType.Water] = new SurfaceProps(9.5f, 2.0f, 0.0f, 0.0f);
        }

        public SurfaceProps Surface(SurfaceType s) { return surfaces[(int)s]; }

        public void SetSurface(SurfaceType s, SurfaceProps p) { surfaces[(int)s] = p; }

        public WallProps Wall(WallMaterial m)
        {
            switch (m)
            {
                case WallMaterial.Hedge: return HedgeWall;
                case WallMaterial.Metal: return MetalWall;
                case WallMaterial.Wood: return WoodWall;
                case WallMaterial.Ledge: return LedgeWall;
                default: return StoneWall;
            }
        }

        public static readonly SimTuning Default = new SimTuning();
    }
}
