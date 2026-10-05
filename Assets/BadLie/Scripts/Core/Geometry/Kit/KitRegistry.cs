using System;
using System.Collections.Generic;
using UnityEngine;

namespace BadLie.Geometry
{
    /// <summary>Metadata for one placeable kit piece.</summary>
    public sealed class KitInfo
    {
        public string Name;
        /// <summary>Ground footprint used for contact shading.</summary>
        public float Radius;
        public float AOStrength = 0.5f;
        public int Variants = 1;
        public Func<int, KitMesh> Build;
    }

    /// <summary>
    /// Name-to-generator table for the garden environment kit. The editor bakes these into
    /// mesh assets; the runtime falls back to generating them (deterministic by seed).
    /// </summary>
    public static class KitRegistry
    {
        static readonly Dictionary<string, KitInfo> table = new Dictionary<string, KitInfo>();

        static KitRegistry()
        {
            Add("tree_amber", 2.2f, 0.45f, 3, s => KitFoliage.Tree(s, KitFoliage.Amber, 5.6f, 2.3f));
            Add("tree_red", 2.0f, 0.45f, 3, s => KitFoliage.Tree(s + 100, KitFoliage.Rust, 5.0f, 2.1f));
            Add("tree_saffron", 2.0f, 0.45f, 3, s => KitFoliage.Tree(s + 200, KitFoliage.Saffron, 4.8f, 2.0f));
            Add("topiary_cone", 0.75f, 0.6f, 3, s => KitFoliage.TopiaryCone(s, 2.4f, 0.75f));
            Add("topiary_small", 0.5f, 0.55f, 3, s => KitFoliage.TopiaryCone(s + 50, 1.5f, 0.5f));
            Add("hedge_3", 1.6f, 0.6f, 3, s => KitFoliage.HedgeBlock(s, new Vector3(3f, 1.05f, 0.95f)));
            Add("hedge_2", 1.1f, 0.6f, 3, s => KitFoliage.HedgeBlock(s + 10, new Vector3(2f, 1.05f, 0.95f)));
            Add("hedge_wild", 1.6f, 0.6f, 3, s => KitFoliage.HedgeBlock(s + 20, new Vector3(3f, 1.1f, 1.0f), 0.8f, true));
            Add("bush_dome", 0.7f, 0.55f, 4, s => KitFoliage.DomeBush(s, 0.62f, 0));
            Add("bush_flowers", 0.7f, 0.55f, 4, s => KitFoliage.DomeBush(s + 30, 0.6f, 7));
            Add("wild_mass", 1.3f, 0.55f, 4, s => KitFoliage.WildMass(s, 1.1f));
            Add("tuft", 0.15f, 0.15f, 4, s => KitFoliage.GrassTuft(s, 0.42f));
            Add("tuft_dry", 0.15f, 0.15f, 4, s => KitFoliage.GrassTuft(s + 7, 0.48f, true));
            Add("lily_lantern", 0.2f, 0.2f, 4, s => KitFoliage.LanternLily(s));
            Add("column", 0.38f, 0.55f, 2, s => KitStone.Column(s, 2.9f));
            Add("column_broken", 0.38f, 0.5f, 3, s => KitStone.Column(s + 5, 2.9f, true));
            Add("urn", 0.35f, 0.5f, 3, s => KitStone.UrnOnPlinth(s));
            Add("arch_ruin", 1.9f, 0.5f, 2, s => KitStone.Arch(s, 2.6f, 2.3f, 0.7f, 0.35f));
            Add("lamp_post", 0.22f, 0.45f, 1, s => KitMachine.LampPost(s));
            Add("sluice_gate", 1.5f, 0.5f, 1, s => KitMachine.SluiceGate(s));
            Add("ground_machine", 1.4f, 0.0f, 1, s => KitMachine.GroundMachine(s));
            Add("tower_far", 2.2f, 0.0f, 3, s => KitMachine.Tower(s, 13f, 2.3f));
            Add("aqueduct_far", 3f, 0.0f, 2, s => KitMachine.Aqueduct(s, 4, 3.2f, 5.2f));
            Add("steps", 1f, 0.3f, 1, s => KitStone.Steps(s, 4, 2.2f, 0.16f, 0.36f));
            Add("lily_pads", 1.2f, 0.0f, 4, s => KitWater.LilyPads(s, 1.2f));
            Add("orrery_rings", 8.5f, 0.0f, 1, s => KitMachine.OrreryRings(s, 7.1f));
            Add("vent_small", 1.0f, 0.0f, 1, s => KitMachine.GroundMachine(s, 1.0f));
            Add("reeds", 0.4f, 0.25f, 4, s => KitWater.Reeds(s, 0.9f));
            Add("ruin_wall", 1.8f, 0.4f, 3, s => KitWater.RuinWall(s, 3.4f, 2.3f));
            Add("leaves", 1.4f, 0.0f, 3, s => KitWater.LeafLitter(s, 1.4f));
            Add("leaves_red", 1.4f, 0.0f, 3, s => KitWater.LeafLitter(s + 3, 1.4f, true));
            Add("sunken_slabs", 2f, 0.0f, 2, s => KitWater.SunkenSlabs(s));
        }

        static void Add(string name, float radius, float ao, int variants, Func<int, KitMesh> build)
        {
            table[name] = new KitInfo { Name = name, Radius = radius, AOStrength = ao, Variants = variants, Build = build };
        }

        public static bool TryGet(string name, out KitInfo info) { return table.TryGetValue(name, out info); }

        public static IEnumerable<KitInfo> All { get { return table.Values; } }

        /// <summary>Variant index used for a given placement seed.</summary>
        public static int VariantFor(KitInfo info, int seed)
        {
            return ((seed % info.Variants) + info.Variants) % info.Variants;
        }
    }
}
