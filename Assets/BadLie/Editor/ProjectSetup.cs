using System.IO;
using BadLie.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BadLie.EditorTools
{
    /// <summary>
    /// Reproducible project setup: player settings, render pipeline settings, generated
    /// textures, materials, post-processing, the game config asset and the main scene.
    /// Menu: BAD LIE > Setup Project. Batch: -executeMethod BadLie.EditorTools.ProjectSetup.Run
    /// </summary>
    public static class ProjectSetup
    {
        public const string Root = "Assets/BadLie";
        public const string MatDir = Root + "/Materials";
        public const string ConfigPath = Root + "/Config/GameConfig.asset";
        public const string ScenePath = Root + "/Scenes/Main.unity";

        [MenuItem("BAD LIE/Setup Project")]
        public static void Run()
        {
            Directory.CreateDirectory(MatDir);
            Directory.CreateDirectory(Root + "/Config");
            Directory.CreateDirectory(Root + "/Scenes");
            ConfigurePlayer();
            ConfigureRendering();
            TextureGen.GenerateAll();
            var cfg = BuildConfig();
            UiSetup.Configure(cfg);
            AudioSetup.Configure(cfg);
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
            BuildScene(cfg);
            AssetDatabase.SaveAssets();
            Debug.Log("[BadLie] Project setup complete");
        }

        // ------------------------------------------------------------------ player

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Bad Lie Studio";
            PlayerSettings.productName = "BAD LIE";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.badliestudio.badlie");
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, "com.badliestudio.badlie");
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.Android.renderOutsideSafeArea = true;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.statusBarHidden = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 1170;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneLinux64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneLinux64, new[] { GraphicsDeviceType.OpenGLCore, GraphicsDeviceType.Vulkan });
        }

        // ------------------------------------------------------------------ rendering

        static void ConfigureRendering()
        {
            string[] assets = { "Assets/Settings/Mobile_RPAsset.asset", "Assets/Settings/PC_RPAsset.asset" };
            foreach (var path in assets)
            {
                var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (rp == null) continue;
                var so = new SerializedObject(rp);
                Set(so, "m_MSAA", 4);
                Set(so, "m_RenderScale", 1f);
                Set(so, "m_SupportsHDR", true);
                Set(so, "m_MainLightShadowsSupported", true);
                Set(so, "m_MainLightShadowmapResolution", 2048);
                Set(so, "m_AdditionalLightsRenderingMode", 0);
                Set(so, "m_ShadowDistance", 130f);
                Set(so, "m_ShadowCascadeCount", 1);
                Set(so, "m_SoftShadowsSupported", true);
                Set(so, "m_SoftShadowQuality", 1);
                Set(so, "m_RequireDepthTexture", false);
                Set(so, "m_RequireOpaqueTexture", false);
                Set(so, "m_ShadowDepthBias", 1.2f);
                Set(so, "m_ShadowNormalBias", 0.8f);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(rp);
            }
            var mobile = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(assets[0]);
            GraphicsSettings.defaultRenderPipeline = mobile;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = mobile;
            }
            AssetDatabase.SaveAssets();
        }

        static void Set(SerializedObject so, string prop, object value)
        {
            var p = so.FindProperty(prop);
            if (p == null)
            {
                Debug.LogWarning("[BadLie] URP property not found: " + prop);
                return;
            }
            switch (p.propertyType)
            {
                case SerializedPropertyType.Boolean: p.boolValue = (bool)value; break;
                case SerializedPropertyType.Float: p.floatValue = System.Convert.ToSingle(value); break;
                case SerializedPropertyType.Integer: p.intValue = System.Convert.ToInt32(value); break;
                case SerializedPropertyType.Enum: p.intValue = System.Convert.ToInt32(value); break;
                default: Debug.LogWarning("[BadLie] Unhandled property type for " + prop + ": " + p.propertyType); break;
            }
        }

        // ------------------------------------------------------------------ materials

        static Material Mat(string name, string shader)
        {
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var sh = Shader.Find(shader);
            if (sh == null) throw new System.Exception("Shader not found: " + shader);
            if (m == null)
            {
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = sh;
            return m;
        }

        static Color C(string hex)
        {
            Color c;
            ColorUtility.TryParseHtmlString(hex, out c);
            return c;
        }

        static GameConfig BuildConfig()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<GameConfig>();
                AssetDatabase.CreateAsset(cfg, ConfigPath);
            }
            var noise = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureGen.Dir + "/T_Noise.png");
            var setts = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureGen.Dir + "/T_Setts.png");
            var dot = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureGen.Dir + "/T_SoftDot.png");
            var ring = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureGen.Dir + "/T_Ring.png");
            cfg.Noise = noise;
            cfg.Setts = setts;
            cfg.SoftDot = dot;
            cfg.Ring = ring;

            var terrain = Mat("M_Terrain", "BadLie/Terrain");
            terrain.SetTexture("_NoiseTex", noise);
            terrain.SetTexture("_SettsTex", setts);
            terrain.SetFloat("_SettsScale", 2.2f);
            terrain.SetColor("_RoughA", C("#5b7038"));
            terrain.SetColor("_RoughB", C("#465a2c"));
            terrain.SetColor("_FairA", C("#649c4e"));
            terrain.SetColor("_FairB", C("#568c45"));
            terrain.SetColor("_GreenA", C("#7cb75c"));
            terrain.SetColor("_GreenB", C("#6eaa53"));
            terrain.SetColor("_Collar", C("#5c924b"));
            terrain.SetColor("_Sand", C("#e2c79b"));
            terrain.SetColor("_SandDark", C("#c7a77a"));
            terrain.SetColor("_Stone0", C("#cfb68f"));
            terrain.SetColor("_Stone1", C("#c9a28f"));
            terrain.SetColor("_Stone2", C("#c8aa76"));
            terrain.SetColor("_Stone3", C("#a9a98e"));
            terrain.SetColor("_Stone4", C("#b5ab9f"));
            terrain.SetColor("_Grout", C("#8c7c66"));
            terrain.SetColor("_Runnel", C("#2f6766"));
            terrain.SetColor("_Earth", C("#6b4a3a"));
            terrain.SetColor("_EarthDark", C("#3c2a26"));
            terrain.SetColor("_WallStone", C("#cfb084"));
            terrain.SetColor("_Brick", C("#b4624a"));
            terrain.SetColor("_Metal", C("#2b2628"));
            cfg.Terrain = terrain;

            var water = Mat("M_Water", "BadLie/Water");
            water.SetTexture("_NoiseTex", noise);
            water.SetColor("_Deep", C("#0d2a2d"));
            water.SetColor("_Shallow", C("#24504d"));
            water.SetColor("_Reflect", C("#8c8a80"));
            water.SetColor("_Foam", C("#efe3c8"));
            water.SetFloat("_ReflectStrength", 0.32f);
            water.SetFloat("_Glint", 1.6f);
            cfg.Water = water;

            var lower = Mat("M_LowerWater", "BadLie/Water");
            lower.SetTexture("_NoiseTex", noise);
            lower.SetColor("_Deep", C("#0f272a"));
            lower.SetColor("_Shallow", C("#20403f"));
            lower.SetColor("_Reflect", C("#7a8079"));
            lower.SetColor("_Foam", C("#d9cbb4"));
            lower.SetFloat("_RippleScale", 0.12f);
            lower.SetFloat("_RippleStrength", 0.12f);
            lower.SetFloat("_FoamWidth", 0.35f);
            lower.SetFloat("_ReflectStrength", 0.6f);
            cfg.LowerWater = lower;

            var lit = Mat("M_Lit", "BadLie/Lit");
            lit.SetTexture("_NoiseTex", noise);
            lit.SetColor("_GlowColor", new Color(1.2f, 2.4f, 2.2f));
            cfg.Lit = lit;

            var foliage = Mat("M_Foliage", "BadLie/Foliage");
            foliage.SetFloat("_Jitter", 0.04f);
            foliage.SetFloat("_Translucency", 0.65f);
            foliage.SetFloat("_Wrap", 0.55f);
            foliage.SetFloat("_RimLight", 0.28f);
            cfg.Foliage = foliage;

            var glow = Mat("M_Glow", "BadLie/Glow");
            glow.SetColor("_Color", new Color(1.1f, 2.6f, 2.4f));
            cfg.Glow = glow;
            var amber = Mat("M_GlowAmber", "BadLie/Glow");
            amber.SetColor("_Color", new Color(2.8f, 1.7f, 0.55f));
            cfg.GlowAmber = amber;

            cfg.Ball = Mat("M_Ball", "BadLie/Ball");

            var fxa = Mat("M_FxAlpha", "BadLie/FX");
            fxa.SetTexture("_MainTex", dot);
            fxa.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            fxa.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            cfg.FxAlpha = fxa;
            var fxadd = Mat("M_FxAdditive", "BadLie/FX");
            fxadd.SetTexture("_MainTex", dot);
            fxadd.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            fxadd.SetFloat("_DstBlend", (float)BlendMode.One);
            cfg.FxAdditive = fxadd;
            var aim = Mat("M_Aim", "BadLie/FX");
            aim.SetTexture("_MainTex", dot);
            aim.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            aim.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            aim.SetFloat("_ZTest", (float)CompareFunction.Always);
            aim.SetFloat("_Fog", 0f);
            aim.renderQueue = 3100;
            cfg.AimLine = aim;
            var shadow = Mat("M_ContactShadow", "BadLie/FX");
            shadow.SetTexture("_MainTex", dot);
            shadow.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            shadow.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            shadow.renderQueue = 2900;
            cfg.Shadow = shadow;

            foreach (var m in new[] { terrain, water, lower, lit, foliage, glow, amber, cfg.Ball, fxa, fxadd, aim, shadow }) EditorUtility.SetDirty(m);
            return cfg;
        }

        // ------------------------------------------------------------------ scene

        static VolumeProfile BuildVolume()
        {
            string path = Root + "/Config/PostProfile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            foreach (var c in profile.components.ToArray()) { profile.Remove(c.GetType()); Object.DestroyImmediate(c, true); }
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.15f);
            bloom.intensity.Override(0.32f);
            bloom.scatter.Override(0.55f);
            bloom.highQualityFiltering.Override(false);
            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.15f);
            color.contrast.Override(10f);
            color.saturation.Override(6f);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.2f);
            vig.smoothness.Override(0.45f);
            vig.color.Override(new Color(0.16f, 0.08f, 0.14f));
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        static void BuildScene(GameConfig cfg)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 1f;
            sun.shadowBias = 0.05f;
            sun.shadowNormalBias = 0.4f;

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.50f, 0.46f);
            cam.allowMSAA = true;
            cam.allowHDR = true;
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.None;
            camData.renderShadows = true;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CameraRig>().Cam = cam;

            var volGo = new GameObject("Post Volume");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = BuildVolume();

            var rootGo = new GameObject("Game");
            var atmos = rootGo.AddComponent<Atmosphere>();
            atmos.Sun = sun;
            var root = rootGo.AddComponent<GameRoot>();
            root.Config = cfg;
            root.Cam = cam;
            root.Atmos = atmos;
            atmos.Apply();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
