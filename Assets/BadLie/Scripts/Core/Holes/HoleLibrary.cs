using System.Collections.Generic;
using BadLie.Course;

namespace BadLie.Holes
{
    /// <summary>The authored holes of the garden biome, in run order.</summary>
    public static class HoleLibrary
    {
        public const int Count = 5;

        static readonly System.Func<HoleDef>[] builders =
        {
            Hole1LanternGate.Build,
            Hole2SunkenParterre.Build,
            Hole3SluiceWalk.Build,
            Hole4FloodedCloister.Build,
            Hole5Orrery.Build,
        };

        public static int Available { get { return builders.Length; } }

        public static HoleDef Get(int index)
        {
            if (index < 0) index = 0;
            if (index >= builders.Length) index = builders.Length - 1;
            return builders[index]();
        }
    }
}
