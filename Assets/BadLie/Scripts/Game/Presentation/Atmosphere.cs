using UnityEngine;

namespace BadLie.Game
{
    /// <summary>
    /// The art-direction knobs for light, shadow tint, ambient, fog and wind, pushed to every
    /// BAD LIE shader as globals. Colours are authored in sRGB and converted to linear here.
    /// </summary>
    [ExecuteAlways]
    public sealed class Atmosphere : MonoBehaviour
    {
        public Light Sun;
        [Header("Sun")]
        public Color SunColor = new Color(1.0f, 0.84f, 0.62f);
        public float SunIntensity = 1.75f;
        public Vector2 SunAngles = new Vector2(36f, 146f);
        [Header("Shade")]
        public Color ShadowColor = new Color(0.54f, 0.43f, 0.67f);
        public float ShadowIntensity = 0.62f;
        public Color AmbientSky = new Color(0.44f, 0.41f, 0.50f);
        public Color AmbientGround = new Color(0.20f, 0.31f, 0.33f);
        public float AmbientIntensity = 0.36f;
        public Color AOColor = new Color(0.46f, 0.36f, 0.50f);
        [Header("Fog")]
        public Color FogColor = new Color(0.50f, 0.43f, 0.46f);
        public Vector3 FogHeight = new Vector3(-0.9f, -3.8f, 0.5f);
        /// <summary>Distance haze, relative to the camera-to-focus distance: (start offset, end offset, max).</summary>
        public Vector3 FogDistance = new Vector3(12f, 100f, 0.55f);
        public static float CameraDistance = 50f;
        [Header("Wind")]
        public Vector2 WindDir = new Vector2(0.8f, 0.35f);
        public float WindStrength = 0.06f;

        static readonly int SunId = Shader.PropertyToID("_BL_SunColor");
        static readonly int ShadowId = Shader.PropertyToID("_BL_ShadowColor");
        static readonly int SkyId = Shader.PropertyToID("_BL_AmbientSky");
        static readonly int GroundId = Shader.PropertyToID("_BL_AmbientGround");
        static readonly int AOId = Shader.PropertyToID("_BL_AOColor");
        static readonly int FogId = Shader.PropertyToID("_BL_FogColor");
        static readonly int FogHId = Shader.PropertyToID("_BL_FogHeight");
        static readonly int FogDId = Shader.PropertyToID("_BL_FogDistance");
        static readonly int WindId = Shader.PropertyToID("_BL_Wind");
        static readonly int TimeId = Shader.PropertyToID("_BL_Time");
        static readonly int StripeId = Shader.PropertyToID("_BL_Stripe");
        static readonly int RevealId = Shader.PropertyToID("_BL_Reveal");
        static readonly int Reveal2Id = Shader.PropertyToID("_BL_Reveal2");

        public static Vector4 Linear(Color c, float k)
        {
            Color l = c.linear;
            return new Vector4(l.r * k, l.g * k, l.b * k, 1f);
        }

        void OnEnable() { Apply(); }

        void OnValidate() { Apply(); }

        void Update()
        {
            if (Application.isPlaying) Shader.SetGlobalVector(FogDId, new Vector4(CameraDistance + FogDistance.x, CameraDistance + FogDistance.y, FogDistance.z, 0));
            Shader.SetGlobalVector(TimeId, new Vector4(Application.isPlaying ? Time.time : (float)(System.DateTime.Now.TimeOfDay.TotalSeconds % 1000.0), 0, 0, 0));
#if UNITY_EDITOR
            if (!Application.isPlaying) Apply();
#endif
        }

        public void Apply()
        {
            Shader.SetGlobalVector(SunId, Linear(SunColor, SunIntensity));
            Shader.SetGlobalVector(ShadowId, Linear(ShadowColor, ShadowIntensity));
            Shader.SetGlobalVector(SkyId, Linear(AmbientSky, AmbientIntensity));
            Shader.SetGlobalVector(GroundId, Linear(AmbientGround, AmbientIntensity));
            Shader.SetGlobalVector(AOId, Linear(AOColor, 1f));
            Shader.SetGlobalVector(FogId, Linear(FogColor, 1f));
            Shader.SetGlobalVector(FogHId, new Vector4(FogHeight.x, FogHeight.y, FogHeight.z, 0));
            Shader.SetGlobalVector(FogDId, new Vector4(CameraDistance + FogDistance.x, CameraDistance + FogDistance.y, FogDistance.z, 0));
            Vector2 w = WindDir.normalized;
            Shader.SetGlobalVector(WindId, new Vector4(w.x, w.y, WindStrength, 0));
            if (Sun != null)
            {
                Sun.transform.rotation = Quaternion.Euler(SunAngles.x, SunAngles.y, 0f);
                Sun.color = SunColor;
                Sun.intensity = SunIntensity;
            }
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = AmbientSky * AmbientIntensity;
            RenderSettings.ambientEquatorColor = Color.Lerp(AmbientSky, AmbientGround, 0.5f) * AmbientIntensity;
            RenderSettings.ambientGroundColor = AmbientGround * AmbientIntensity;
        }

        public static void SetStripe(float angleDeg)
        {
            float a = angleDeg * Mathf.Deg2Rad;
            Shader.SetGlobalVector(StripeId, new Vector4(Mathf.Cos(a), Mathf.Sin(a), 0, 0));
        }

        /// <summary>Dither away geometry in front of the ball (and optionally the aim line).</summary>
        public static void SetReveal(Camera cam, Vector3 ballWorld, bool aimOn, Vector3 aimEndWorld, float radius)
        {
            if (cam == null)
            {
                Shader.SetGlobalVector(RevealId, Vector4.zero);
                return;
            }
            Vector3 vp = cam.WorldToViewportPoint(ballWorld);
            Vector3 ve = cam.WorldToViewportPoint(aimEndWorld);
            Shader.SetGlobalVector(RevealId, new Vector4(vp.x, vp.y, radius, vp.z));
            Shader.SetGlobalVector(Reveal2Id, new Vector4(ve.x, ve.y, aimOn ? 1f : 0f, cam.aspect));
        }
    }
}
