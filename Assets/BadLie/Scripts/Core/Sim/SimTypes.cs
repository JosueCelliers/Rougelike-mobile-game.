using System.Collections.Generic;
using BadLie.Course;
using UnityEngine;

namespace BadLie.Sim
{
    /// <summary>Upgrade effects as seen by the simulation. Default is "no upgrades".</summary>
    public struct ShotModifiers
    {
        public float LaunchSpeedScale;
        public float ForceCoupling;
        public float RoughScale;
        public float RoughLandingKeep;
        public float SandScale;
        public bool SkipStone;
        public bool BankShot;
        public bool Ricochet;
        public bool CupMagnet;

        public static ShotModifiers None
        {
            get
            {
                return new ShotModifiers
                {
                    LaunchSpeedScale = 1f,
                    ForceCoupling = 1f,
                    RoughScale = 1f,
                    RoughLandingKeep = -1f,
                    SandScale = 1f,
                };
            }
        }
    }

    public struct BallStart
    {
        public Vector2 Position;
        public float Height;

        public BallStart(Vector2 p, float h)
        {
            Position = p;
            Height = h;
        }
    }

    /// <summary>The player's input: a direction on the course plane and a power in [0,1].</summary>
    [System.Serializable]
    public struct ShotInput
    {
        public Vector2 Direction;
        public float Power;

        public ShotInput(Vector2 dir, float power)
        {
            Direction = dir.sqrMagnitude > 1e-12f ? dir.normalized : Vector2.up;
            Power = Mathf.Clamp01(power);
        }
    }

    public enum ShotOutcome : byte
    {
        Stopped,
        Holed,
        Water,
        OutOfBounds,
        Timeout,
        PreviewEnd,
    }

    public enum SimEventType : byte
    {
        Launch,
        Wall,
        BankWall,
        Airborne,
        Land,
        Bounce,
        Skip,
        Splash,
        Fell,
        SurfaceChange,
        LipOut,
        Holed,
        MagnetPull,
        Stop,
        EnterFlow,
    }

    public struct SimEvent
    {
        public SimEventType Type;
        public float Time;
        public Vector3 Position;
        public float Strength;
        public SurfaceType Surface;
        public WallMaterial Material;
        public Vector2 Normal;

        public override string ToString()
        {
            return string.Format("{0:F2}s {1} {2} str={3:F2}", Time, Type, Position, Strength);
        }
    }

    public struct PathSample
    {
        public float Time;
        public Vector3 Position;   // x, contact height, z
        public float Speed;
        public bool Grounded;
    }

    public struct SimOptions
    {
        public bool RecordPath;
        public int RecordEvery;
        /// <summary>Preview: stop after this much travel (0 = unlimited).</summary>
        public float MaxDistance;
        /// <summary>Preview: stop this far after the first wall contact (0 = unlimited).</summary>
        public float AfterFirstHit;
        public float MaxTime;

        public static SimOptions Full
        {
            get { return new SimOptions { RecordPath = true, RecordEvery = 2 }; }
        }

        public static SimOptions Preview(float distance, float afterHit)
        {
            return new SimOptions { RecordPath = true, RecordEvery = 2, MaxDistance = distance, AfterFirstHit = afterHit };
        }

        public static SimOptions Fast
        {
            get { return new SimOptions { RecordPath = false, RecordEvery = 0 }; }
        }
    }

    public sealed class SimResult
    {
        public ShotOutcome Outcome;
        public Vector2 FinalPosition;
        public float FinalHeight;
        public SurfaceType FinalSurface;
        public float Time;
        public float Distance;
        public float LaunchSpeed;
        public int WallHits;
        public bool Skipped;
        public bool BankUsed;
        public Vector2 HazardPoint;
        public readonly List<PathSample> Path = new List<PathSample>(512);
        public readonly List<SimEvent> Events = new List<SimEvent>(16);

        public bool IsHazard { get { return Outcome == ShotOutcome.Water || Outcome == ShotOutcome.OutOfBounds; } }

        public void Clear()
        {
            Outcome = ShotOutcome.Stopped;
            FinalPosition = Vector2.zero;
            FinalHeight = 0f;
            FinalSurface = SurfaceType.Rough;
            Time = Distance = LaunchSpeed = 0f;
            WallHits = 0;
            Skipped = BankUsed = false;
            HazardPoint = Vector2.zero;
            Path.Clear();
            Events.Clear();
        }
    }
}
