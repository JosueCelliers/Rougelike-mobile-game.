using System.Collections.Generic;
using BadLie.Course;
using BadLie.Sim;
using UnityEngine;

namespace BadLie.Game
{
    /// <summary>
    /// Restrained impact effects (grass bits, stone dust, sand, splashes), expanding rings for
    /// skips / bank shots / the magnet, the cup sparkle, and ambient drifting leaves and motes.
    /// </summary>
    public sealed class FxDirector : MonoBehaviour
    {
        static FxDirector inst;
        GameConfig cfg;
        ParticleSystem puffs, sparks, leaves, motes;
        readonly List<Ring> rings = new List<Ring>();
        Mesh ringMesh;

        sealed class Ring
        {
            public Transform T;
            public Material M;
            public float Age, Life, From, To;
            public Color C;
        }

        public static void Init(GameConfig cfg, Transform parent)
        {
            var go = new GameObject("FX");
            go.transform.SetParent(parent, false);
            inst = go.AddComponent<FxDirector>();
            inst.cfg = cfg;
            inst.puffs = inst.MakeSystem("Puffs", cfg.FxAlpha, false);
            inst.sparks = inst.MakeSystem("Sparks", cfg.FxAdditive, false);
            inst.ringMesh = MakeRingMesh();
        }

        ParticleSystem MakeSystem(string name, Material mat, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = loop;
            main.playOnAwake = false;
            main.maxParticles = 600;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 1f;
            var em = ps.emission;
            em.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.6f), new Keyframe(0.3f, 1f), new Keyframe(1, 0.8f)));
            ps.Play();
            return ps;
        }

        static Mesh MakeRingMesh()
        {
            var m = new Mesh { name = "FX Ring" };
            const int n = 40;
            var v = new Vector3[n * 2];
            var uv = new Vector2[n * 2];
            var t = new int[n * 6];
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v[i * 2] = d * 0.86f;
                v[i * 2 + 1] = d;
                uv[i * 2] = new Vector2(0.5f, 0.5f);
                uv[i * 2 + 1] = new Vector2(0.5f, 0.5f);
                int j = (i + 1) % n;
                t[i * 6] = i * 2; t[i * 6 + 1] = j * 2; t[i * 6 + 2] = j * 2 + 1;
                t[i * 6 + 3] = i * 2; t[i * 6 + 4] = j * 2 + 1; t[i * 6 + 5] = i * 2 + 1;
            }
            m.vertices = v;
            m.uv = uv;
            m.triangles = t;
            return m;
        }

        /// <summary>Ambient drifting leaves and sunlit motes across the current hole.</summary>
        public static void SetupAmbient(Rect area, float groundY)
        {
            if (inst == null) return;
            if (inst.leaves == null)
            {
                inst.leaves = inst.MakeSystem("Leaves", inst.cfg.FxAlpha, true);
                inst.motes = inst.MakeSystem("Motes", inst.cfg.FxAdditive, true);
            }
            Configure(inst.leaves, area, groundY + 3.5f, 2.2f, new Color(0.95f, 0.62f, 0.25f, 0.9f), new Color(0.78f, 0.32f, 0.18f, 0.9f), 0.13f, 7f, 0.06f);
            Configure(inst.motes, area, groundY + 1.2f, 2.5f, new Color(1f, 0.86f, 0.55f, 0.55f), new Color(0.65f, 1f, 0.95f, 0.45f), 0.05f, 6f, -0.01f);
        }

        static void Configure(ParticleSystem ps, Rect area, float y, float height, Color a, Color b, float size, float rate, float gravity)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.transform.position = new Vector3(area.center.x, y, area.center.y);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.7f, size * 1.3f);
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
            main.gravityModifier = gravity;
            var em = ps.emission;
            em.enabled = true;
            em.rateOverTime = rate;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(area.width, height, area.height);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            vel.z = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.4f;
            ps.Simulate(8f, true, true);
            ps.Play();
        }

        static void Burst(ParticleSystem ps, Vector3 pos, int count, Color a, Color b, float size, float speed, float up, float gravity, float life)
        {
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                Vector3 dir = Random.insideUnitSphere;
                dir.y = Mathf.Abs(dir.y) * up + 0.15f;
                ep.position = pos + Vector3.up * 0.05f;
                ep.velocity = dir.normalized * speed * Random.Range(0.5f, 1.2f);
                ep.startColor = Color.Lerp(a, b, Random.value);
                ep.startSize = size * Random.Range(0.6f, 1.3f);
                ep.startLifetime = life * Random.Range(0.7f, 1.2f);
                ps.Emit(ep, 1);
            }
            var main = ps.main;
            main.gravityModifier = gravity;
        }

        public static void RingAt(Vector3 pos, Color c, float from, float to, float life)
        {
            if (inst == null) return;
            var go = new GameObject("Ring");
            go.transform.SetParent(inst.transform, false);
            go.transform.position = pos + Vector3.up * 0.04f;
            go.AddComponent<MeshFilter>().sharedMesh = inst.ringMesh;
            var mr = go.AddComponent<MeshRenderer>();
            var m = new Material(inst.cfg.FxAdditive);
            mr.sharedMaterial = m;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            inst.rings.Add(new Ring { T = go.transform, M = m, Life = life, From = from, To = to, C = c });
        }

        void Update()
        {
            for (int i = rings.Count - 1; i >= 0; i--)
            {
                var r = rings[i];
                r.Age += Time.deltaTime;
                float k = Mathf.Clamp01(r.Age / r.Life);
                float s = Mathf.Lerp(r.From, r.To, 1f - (1f - k) * (1f - k));
                r.T.localScale = new Vector3(s, 1f, s);
                r.M.SetColor("_Color", new Color(r.C.r, r.C.g, r.C.b, r.C.a * (1f - k)));
                if (k >= 1f)
                {
                    Destroy(r.T.gameObject);
                    Destroy(r.M);
                    rings.RemoveAt(i);
                }
            }
        }

        public static void BallEvent(SimEvent e)
        {
            if (inst == null) return;
            Vector3 p = e.Position;
            float s = Mathf.Clamp01(e.Strength / 8f);
            switch (e.Type)
            {
                case SimEventType.Wall:
                    if (e.Material == WallMaterial.Hedge) Burst(inst.puffs, p, 4 + (int)(6 * s), new Color(0.38f, 0.45f, 0.18f), new Color(0.55f, 0.6f, 0.25f), 0.07f, 1.4f, 0.8f, 0.6f, 0.7f);
                    else Burst(inst.puffs, p + Vector3.up * 0.1f, 3 + (int)(8 * s), new Color(0.9f, 0.82f, 0.68f, 0.7f), new Color(0.75f, 0.66f, 0.55f, 0.6f), 0.12f, 0.9f, 0.5f, 0.05f, 0.6f);
                    break;
                case SimEventType.BankWall:
                    Burst(inst.sparks, p + Vector3.up * 0.12f, 14, new Color(1f, 0.85f, 0.45f), new Color(1f, 0.65f, 0.3f), 0.07f, 2.2f, 0.6f, 0.4f, 0.45f);
                    RingAt(p, new Color(1f, 0.78f, 0.4f, 0.9f), 0.2f, 1.1f, 0.45f);
                    break;
                case SimEventType.Land:
                case SimEventType.Bounce:
                    if (e.Surface == SurfaceType.Sand) Burst(inst.puffs, p, 10 + (int)(10 * s), new Color(0.92f, 0.8f, 0.6f, 0.9f), new Color(0.8f, 0.66f, 0.46f, 0.8f), 0.08f, 1.6f, 1f, 1.2f, 0.6f);
                    else if (s > 0.25f) Burst(inst.puffs, p, 4 + (int)(6 * s), new Color(0.45f, 0.62f, 0.25f), new Color(0.35f, 0.5f, 0.2f), 0.06f, 1.2f, 1f, 1f, 0.5f);
                    break;
                case SimEventType.Skip:
                    Burst(inst.puffs, p, 16, new Color(0.85f, 0.97f, 0.95f, 0.9f), new Color(0.6f, 0.85f, 0.85f, 0.8f), 0.07f, 2.4f, 1.5f, 1.3f, 0.6f);
                    RingAt(p, new Color(0.6f, 1f, 0.95f, 0.85f), 0.15f, 1.3f, 0.8f);
                    RingAt(p, new Color(0.6f, 1f, 0.95f, 0.5f), 0.1f, 0.8f, 1.1f);
                    break;
                case SimEventType.Splash:
                case SimEventType.Fell:
                    Burst(inst.puffs, p, 22, new Color(0.88f, 0.96f, 0.95f, 0.9f), new Color(0.55f, 0.75f, 0.75f, 0.8f), 0.09f, 2.6f, 2f, 1.4f, 0.8f);
                    RingAt(p, new Color(0.85f, 0.95f, 0.95f, 0.7f), 0.15f, 1.0f, 1.0f);
                    break;
                case SimEventType.Holed:
                    Burst(inst.sparks, p + Vector3.up * 0.1f, 26, new Color(1f, 0.9f, 0.6f), new Color(0.65f, 1f, 0.95f), 0.06f, 1.4f, 2.2f, -0.15f, 1.3f);
                    RingAt(p, new Color(1f, 0.92f, 0.7f, 0.9f), 0.3f, 1.6f, 0.9f);
                    break;
                case SimEventType.MagnetPull:
                    RingAt(p, new Color(0.6f, 1f, 0.95f, 0.6f), 1.15f, 0.35f, 0.7f);
                    break;
                case SimEventType.LipOut:
                    Burst(inst.sparks, p + Vector3.up * 0.1f, 6, new Color(1f, 1f, 1f), new Color(1f, 0.9f, 0.7f), 0.04f, 1.2f, 1f, 0.5f, 0.3f);
                    break;
            }
        }
    }
}
