using System.IO;
using BadLie.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BadLie.EditorTools
{
    /// <summary>Panel settings, theme, stylesheet, fonts and upgrade art for the UI Toolkit interface.</summary>
    public static class UiSetup
    {
        const string UiDir = ProjectSetup.Root + "/UI";
        static readonly string[] ArtNames = { "skip_stone", "bank_shot", "rough_rider", "heavy_core", "cup_magnet", "second_chance" };

        public static void Configure(GameConfig cfg)
        {
            AssetDatabase.ImportAsset(UiDir + "/BadLieTheme.tss", ImportAssetOptions.ForceUpdate);
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(UiDir + "/BadLieTheme.tss");
            string panelPath = UiDir + "/PanelSettings.asset";
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, panelPath);
            }
            panel.themeStyleSheet = theme;
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1080, 2340);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0f;
            panel.sortingOrder = 10;
            EditorUtility.SetDirty(panel);
            cfg.Panel = panel;
            cfg.Style = AssetDatabase.LoadAssetAtPath<StyleSheet>(UiDir + "/BadLie.uss");
            cfg.TitleFont = AssetDatabase.LoadAssetAtPath<Font>(ProjectSetup.Root + "/Fonts/Cinzel.ttf");
            cfg.BodyFont = AssetDatabase.LoadAssetAtPath<Font>(ProjectSetup.Root + "/Fonts/BarlowSemiCondensed-Medium.ttf");

            cfg.UpgradeArt = new Texture2D[ArtNames.Length];
            cfg.UpgradeIcons = new Texture2D[ArtNames.Length];
            for (int i = 0; i < ArtNames.Length; i++)
            {
                cfg.UpgradeArt[i] = ImportUiTexture(UiDir + "/Upgrades/" + ArtNames[i] + ".png");
                cfg.UpgradeIcons[i] = ImportUiTexture(UiDir + "/Upgrades/" + ArtNames[i] + "_icon.png");
            }
        }

        static Texture2D ImportUiTexture(string path)
        {
            if (!File.Exists(path)) return null;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Default;
            imp.sRGBTexture = true;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
