using UnityEngine;
using UnityEngine.UIElements;

namespace BadLie.Game
{
    /// <summary>
    /// Every asset the runtime needs, wired up by the editor setup tool
    /// (BAD LIE > Setup Project) so scenes and materials are reproducible.
    /// </summary>
    [CreateAssetMenu(menuName = "BAD LIE/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Materials")]
        public Material Terrain;
        public Material Water;
        public Material LowerWater;
        public Material Lit;
        public Material Foliage;
        public Material Glow;
        public Material GlowAmber;
        public Material Ball;
        public Material FxAlpha;
        public Material FxAdditive;
        public Material AimLine;
        public Material Shadow;

        [Header("Textures")]
        public Texture2D Noise;
        public Texture2D Setts;
        public Texture2D SoftDot;
        public Texture2D Ring;
        public Texture2D Leaf;

        [Header("UI")]
        public PanelSettings Panel;
        public StyleSheet Style;
        public Font TitleFont;
        public Font BodyFont;
        public Texture2D[] UpgradeArt;
        public Texture2D[] UpgradeIcons;

        [Header("Audio")]
        public AudioClip[] Strike;
        public AudioClip[] WallStone;
        public AudioClip WallHedge;
        public AudioClip CupDrop;
        public AudioClip Splash;
        public AudioClip Skip;
        public AudioClip LandGrass;
        public AudioClip LandSand;
        public AudioClip LipOut;
        public AudioClip UiTap;
        public AudioClip UiSelect;
        public AudioClip Penalty;
        public AudioClip HoleComplete;
        public AudioClip RunWon;
        public AudioClip RunLost;
        public AudioClip Magnet;
        public AudioClip Ambience;
        public AudioClip Music;
    }
}
