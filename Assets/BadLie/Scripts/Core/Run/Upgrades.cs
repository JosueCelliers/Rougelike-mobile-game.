using System.Collections.Generic;
using BadLie.Sim;

namespace BadLie.Run
{
    public enum UpgradeId
    {
        SkipStone = 0,
        BankShot = 1,
        RoughRider = 2,
        HeavyCore = 3,
        CupMagnet = 4,
        SecondChance = 5,
    }

    public sealed class UpgradeDef
    {
        public UpgradeId Id;
        public string Name;
        /// <summary>One line of flavour describing the equipment.</summary>
        public string Item;
        /// <summary>Exactly what changes, with numbers.</summary>
        public string Effect;
        /// <summary>The cost or limit, stated plainly ("" if none).</summary>
        public string Tradeoff;
        /// <summary>Which kind of route or shot this opens up.</summary>
        public string Use;
    }

    public sealed class ComboDef
    {
        public string Id;
        public string Name;
        public UpgradeId A, B;
        public string Effect;
        public string Playstyle;
    }

    /// <summary>The upgrade catalogue and how upgrades translate into simulation modifiers.</summary>
    public static class RunUpgrades
    {
        // Tuning values for upgrade effects (kept next to their descriptions on purpose).
        public const float HeavyCoreSpeed = 0.88f;
        public const float HeavyCoreSpeedCombo = 0.94f;
        public const float HeavyCoreCoupling = 0.25f;
        public const float RoughRiderScale = 0.42f;
        public const float RoughRiderLandingKeep = 0.85f;
        public const float GroundbreakerSand = 0.6f;

        public static readonly UpgradeDef[] All =
        {
            new UpgradeDef
            {
                Id = UpgradeId.SkipStone, Name = "Skip Stone",
                Item = "A flat river pebble set into the club face.",
                Effect = "Once per shot, a ball hitting water at 3 m/s or more skips instead of sinking. The skip keeps 86% of its speed and hops about 2–4 m.",
                Tradeoff = "Only one skip per shot. A second water contact sinks as usual.",
                Use = "Cut straight across short water gaps.",
            },
            new UpgradeDef
            {
                Id = UpgradeId.BankShot, Name = "Bank Shot",
                Item = "Brass corner plates for a sharper rebound.",
                Effect = "The first wall rebound of every shot keeps 92% of its speed into the wall (normally 56%).",
                Tradeoff = "Only the first rebound per shot. Hedges still absorb later hits.",
                Use = "Play around corners off walls and ledges.",
            },
            new UpgradeDef
            {
                Id = UpgradeId.RoughRider, Name = "Rough Rider",
                Item = "Scythe-ground sole that slices through long grass.",
                Effect = "Rough slows the ball 58% less, and landings in rough keep 85% of their speed (normally 62%).",
                Tradeoff = "No effect on sand, water or stone.",
                Use = "Take direct lines through overgrown ground.",
            },
            new UpgradeDef
            {
                Id = UpgradeId.HeavyCore, Name = "Heavy Core",
                Item = "A lead-weighted core from the sluice workshop.",
                Effect = "Flowing water and machinery vents push the ball 75% less.",
                Tradeoff = "Maximum shot speed −12% (about 22% less roll distance).",
                Use = "Cross runnels and vents on a straight line.",
            },
            new UpgradeDef
            {
                Id = UpgradeId.CupMagnet, Name = "Cup Magnet",
                Item = "A lodestone ring hung inside the flagstick.",
                Effect = "On the green, below 1.5 m/s the cup captures from 0.43 m (normally 0.30 m) and draws the ball gently inward within 1.15 m.",
                Tradeoff = "No help on fast putts. Ignored off the green.",
                Use = "Finish holes from awkward lies.",
            },
            new UpgradeDef
            {
                Id = UpgradeId.SecondChance, Name = "Second Chance",
                Item = "A pressed feather from the groundskeeper's journal.",
                Effect = "Once per run, undo your last shot: the ball returns to its lie and the stroke (and any penalty) is refunded.",
                Tradeoff = "One use per run. Must be used before your next shot.",
                Use = "Insurance for a risky line.",
            },
        };

        public static readonly ComboDef[] Combos =
        {
            new ComboDef
            {
                Id = "ricochet", Name = "Ricochet", A = UpgradeId.SkipStone, B = UpgradeId.BankShot,
                Effect = "A water skip re-arms Bank Shot, so the next wall after a skip also keeps 92% of its speed.",
                Playstyle = "Trick shots: skip a channel, then bank off the far wall.",
            },
            new ComboDef
            {
                Id = "groundbreaker", Name = "Groundbreaker", A = UpgradeId.HeavyCore, B = UpgradeId.RoughRider,
                Effect = "Sand slows the ball 40% less, and Heavy Core's speed loss is halved (−6% instead of −12%).",
                Playstyle = "Straight lines: ignore currents, rough and bunkers.",
            },
        };

        public static UpgradeDef Get(UpgradeId id) { return All[(int)id]; }

        public static bool Has(IList<UpgradeId> owned, UpgradeId id)
        {
            for (int i = 0; i < owned.Count; i++) if (owned[i] == id) return true;
            return false;
        }

        public static List<ComboDef> ActiveCombos(IList<UpgradeId> owned)
        {
            var list = new List<ComboDef>();
            foreach (var c in Combos) if (Has(owned, c.A) && Has(owned, c.B)) list.Add(c);
            return list;
        }

        public static ShotModifiers ModifiersFor(IList<UpgradeId> owned)
        {
            var m = ShotModifiers.None;
            bool heavy = Has(owned, UpgradeId.HeavyCore);
            bool rough = Has(owned, UpgradeId.RoughRider);
            if (heavy)
            {
                m.LaunchSpeedScale = rough ? HeavyCoreSpeedCombo : HeavyCoreSpeed;
                m.ForceCoupling = HeavyCoreCoupling;
            }
            if (rough)
            {
                m.RoughScale = RoughRiderScale;
                m.RoughLandingKeep = RoughRiderLandingKeep;
            }
            if (heavy && rough) m.SandScale = GroundbreakerSand;
            m.SkipStone = Has(owned, UpgradeId.SkipStone);
            m.BankShot = Has(owned, UpgradeId.BankShot);
            m.Ricochet = m.SkipStone && m.BankShot;
            m.CupMagnet = Has(owned, UpgradeId.CupMagnet);
            return m;
        }
    }
}
