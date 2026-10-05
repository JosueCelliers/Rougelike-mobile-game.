using System;
using System.Collections.Generic;

namespace BadLie.Run
{
    public enum RunPhase
    {
        HoleIntro = 0,
        Playing = 1,
        ChoosingUpgrade = 2,
        Won = 3,
        Lost = 4,
        /// <summary>Out of strokes, but Second Chance can still undo the last shot.</summary>
        LastChance = 5,
    }

    [Serializable]
    public sealed class HoleRecord
    {
        public string Name;
        public int Par;
        public int Strokes;
        public int Penalties;
        public int Restored;
    }

    /// <summary>
    /// Everything needed to resume a run exactly, serialised with JsonUtility. A shot that was
    /// in flight when the app closed is stored as its input and re-simulated on resume, which
    /// reproduces the same outcome because the simulation is deterministic.
    /// </summary>
    [Serializable]
    public sealed class RunState
    {
        public int Version = 1;
        public long Seed;
        public int HoleIndex;
        public int[] HoleOrder = new int[0];
        public int[] CupVariants = new int[0];
        public int Strokes;
        public int HoleStrokes;
        public int HolePenalties;
        public int TotalStrokes;
        public List<int> Upgrades = new List<int>();
        public bool SecondChanceUsed;
        public RunPhase Phase;

        public bool HasLie;
        public float LieX, LieZ, LieY;

        public bool UndoAvailable;
        public float UndoX, UndoZ, UndoY;
        public int UndoStrokes, UndoHoleStrokes, UndoPenalties, UndoTotal;

        public bool PendingShot;
        public float PendDirX, PendDirZ, PendPower;

        public List<int> Offer = new List<int>();
        public List<HoleRecord> History = new List<HoleRecord>();
        public string EndReason = "";
        public string LastEvent = "";

        public bool Finished { get { return Phase == RunPhase.Won || Phase == RunPhase.Lost; } }

        public List<UpgradeId> OwnedUpgrades()
        {
            var l = new List<UpgradeId>();
            foreach (var u in Upgrades) l.Add((UpgradeId)u);
            return l;
        }

        public bool Owns(UpgradeId id) { return Upgrades.Contains((int)id); }
    }
}
