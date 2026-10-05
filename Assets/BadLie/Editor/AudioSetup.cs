using BadLie.Game;
using UnityEditor;
using UnityEngine;

namespace BadLie.EditorTools
{
    /// <summary>Imports the synthesised clips (Tools/audio/make_audio.py) with mobile-friendly settings.</summary>
    public static class AudioSetup
    {
        const string Dir = ProjectSetup.Root + "/Audio/";

        public static void Configure(GameConfig cfg)
        {
            cfg.Strike = new[] { Clip("strike_soft"), Clip("strike_mid"), Clip("strike_hard") };
            cfg.WallStone = new[] { Clip("wall_stone_a"), Clip("wall_stone_b") };
            cfg.WallHedge = Clip("wall_hedge");
            cfg.CupDrop = Clip("cup_drop");
            cfg.Splash = Clip("splash");
            cfg.Skip = Clip("skip");
            cfg.LandGrass = Clip("land_grass");
            cfg.LandSand = Clip("land_sand");
            cfg.LipOut = Clip("lip_out");
            cfg.UiTap = Clip("ui_tap");
            cfg.UiSelect = Clip("ui_select");
            cfg.Penalty = Clip("penalty");
            cfg.HoleComplete = Clip("hole_complete");
            cfg.RunWon = Clip("run_won");
            cfg.RunLost = Clip("run_lost");
            cfg.Magnet = Clip("magnet");
            cfg.Ambience = Clip("ambience_garden", true);
            cfg.Music = Clip("music_estate", true);
        }

        static AudioClip Clip(string name, bool stream = false)
        {
            string path = Dir + name + ".wav";
            var imp = AssetImporter.GetAtPath(path) as AudioImporter;
            if (imp == null)
            {
                Debug.LogWarning("[BadLie] Missing audio " + path + " (run Tools/audio/make_audio.py)");
                return null;
            }
            imp.forceToMono = true;
            var s = imp.defaultSampleSettings;
            s.loadType = stream ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = stream ? 0.55f : 0.75f;
            s.preloadAudioData = !stream;
            imp.defaultSampleSettings = s;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
