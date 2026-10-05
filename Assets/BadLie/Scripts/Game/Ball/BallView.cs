using System;
using System.Collections.Generic;
using BadLie.Course;
using BadLie.Geometry;
using BadLie.Sim;
using UnityEngine;

namespace BadLie.Game
{
    /// <summary>
    /// The visible ball. Plays back a simulated shot sample by sample (so what you see is
    /// exactly what was simulated), rolls the mesh, keeps a contact shadow on the ground and
    /// runs the cup-drop and sink animations.
    /// </summary>
    public sealed class BallView : MonoBehaviour
    {
        public float Radius = 0.14f;
        public event Action<SimEvent> OnEvent;
        public event Action OnPlaybackFinished;

        public bool Playing { get; private set; }
        public Vector3 ContactPosition { get; private set; }
        public float Speed { get; private set; }
        public Vector3 Velocity { get; private set; }

        Transform mesh;
        Transform shadow;
        Renderer shadowRenderer;
        TrailRenderer trail;
        CourseModel course;
        SimResult shot;
        float t;
        int sampleIndex;
        int eventIndex;
        Quaternion spin = Quaternion.identity;
        enum Ending { None, CupDrop, Sink, Fall }
        Ending ending;
        float endingT;
        Vector3 endingFrom;

        public static BallView Create(GameConfig cfg, Transform parent)
        {
            var go = new GameObject("Ball");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<BallView>();
            view.Build(cfg);
            return view;
        }

        void Build(GameConfig cfg)
        {
            var md = new MeshData();
            KitPrims.Sphere(md, Matrix4x4.Scale(Vector3.one * Radius), 3, Color.white, 1f, 1f);
            var m = new GameObject("Mesh");
            m.transform.SetParent(transform, false);
            m.AddComponent<MeshFilter>().sharedMesh = md.ToMesh("Ball");
            var r = m.AddComponent<MeshRenderer>();
            r.sharedMaterial = cfg.Ball;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mesh = m.transform;

            // Contact shadow: keeps the ball anchored and readable from any angle.
            var s = new GameObject("Contact Shadow");
            s.transform.SetParent(transform, false);
            var sm = new MeshData();
            int a = sm.Add(new Vector3(-1, 0, -1), Vector3.up, new Color(0.12f, 0.08f, 0.14f, 0.62f), new Vector4(0, 0, 0, 0));
            int b = sm.Add(new Vector3(-1, 0, 1), Vector3.up, new Color(0.12f, 0.08f, 0.14f, 0.62f), new Vector4(0, 1, 0, 0));
            int c = sm.Add(new Vector3(1, 0, 1), Vector3.up, new Color(0.12f, 0.08f, 0.14f, 0.62f), new Vector4(1, 1, 0, 0));
            int d = sm.Add(new Vector3(1, 0, -1), Vector3.up, new Color(0.12f, 0.08f, 0.14f, 0.62f), new Vector4(1, 0, 0, 0));
            sm.Quad(a, b, c, d);
            s.AddComponent<MeshFilter>().sharedMesh = sm.ToMesh("Contact shadow");
            shadowRenderer = s.AddComponent<MeshRenderer>();
            shadowRenderer.sharedMaterial = cfg.Shadow;
            shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shadow = s.transform;

            trail = m.AddComponent<TrailRenderer>();
            trail.time = 0.22f;
            trail.minVertexDistance = 0.05f;
            trail.widthMultiplier = 0.2f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0, 1f), new Keyframe(1, 0f));
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.95f, 0.82f), 0f), new GradientColorKey(new Color(1f, 0.85f, 0.6f), 1f) },
                new[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = grad;
            trail.sharedMaterial = cfg.FxAdditive;
            trail.emitting = false;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public void SetCourse(CourseModel c) { course = c; }

        /// <summary>Places the ball at rest at a ground contact point.</summary>
        public void Place(Vector2 p, float height)
        {
            Playing = false;
            ending = Ending.None;
            mesh.gameObject.SetActive(true);
            shadow.gameObject.SetActive(true);
            mesh.localScale = Vector3.one;
            ContactPosition = new Vector3(p.x, height, p.y);
            Speed = 0f;
            Velocity = Vector3.zero;
            trail.emitting = false;
            trail.Clear();
            Apply(ContactPosition);
        }

        public void Play(SimResult result)
        {
            shot = result;
            t = 0f;
            sampleIndex = 0;
            eventIndex = 0;
            Playing = true;
            ending = Ending.None;
        }

        /// <summary>Skip to the end of the current playback (used when resuming).</summary>
        public void FinishImmediately()
        {
            if (!Playing || shot == null) return;
            t = shot.Time + 10f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (ending != Ending.None)
            {
                UpdateEnding(dt);
                return;
            }
            if (!Playing || shot == null) return;
            t += dt;
            var path = shot.Path;
            while (sampleIndex < path.Count - 2 && path[sampleIndex + 1].Time <= t) sampleIndex++;
            PathSample a = path[sampleIndex];
            PathSample b = path[Mathf.Min(sampleIndex + 1, path.Count - 1)];
            float span = Mathf.Max(b.Time - a.Time, 1e-5f);
            float u = Mathf.Clamp01((t - a.Time) / span);
            Vector3 prev = ContactPosition;
            ContactPosition = Vector3.Lerp(a.Position, b.Position, u);
            Speed = Mathf.Lerp(a.Speed, b.Speed, u);
            Velocity = dt > 0f ? (ContactPosition - prev) / dt : Vector3.zero;
            Roll(ContactPosition - prev);
            Apply(ContactPosition);
            trail.emitting = Speed > 5.2f;

            while (eventIndex < shot.Events.Count && shot.Events[eventIndex].Time <= t)
            {
                var e = shot.Events[eventIndex++];
                if (OnEvent != null) OnEvent(e);
            }

            if (t >= shot.Time)
            {
                Playing = false;
                trail.emitting = false;
                ContactPosition = path[path.Count - 1].Position;
                Apply(ContactPosition);
                switch (shot.Outcome)
                {
                    case ShotOutcome.Holed: BeginEnding(Ending.CupDrop); break;
                    case ShotOutcome.Water: BeginEnding(Ending.Sink); break;
                    case ShotOutcome.OutOfBounds: BeginEnding(Ending.Fall); break;
                    default:
                        if (OnPlaybackFinished != null) OnPlaybackFinished();
                        break;
                }
            }
        }

        void BeginEnding(Ending e)
        {
            ending = e;
            endingT = 0f;
            endingFrom = ContactPosition;
        }

        void UpdateEnding(float dt)
        {
            endingT += dt;
            switch (ending)
            {
                case Ending.CupDrop:
                {
                    Vector3 cup = new Vector3(course.Cup.x, course.CupHeight, course.Cup.y);
                    float k = Mathf.Clamp01(endingT / 0.16f);
                    Vector3 p = Vector3.Lerp(endingFrom, cup, k);
                    float drop = Mathf.Clamp01((endingT - 0.08f) / 0.22f);
                    p.y -= drop * drop * 0.32f;
                    ContactPosition = p;
                    Apply(p);
                    shadow.gameObject.SetActive(endingT < 0.1f);
                    if (endingT > 0.42f) FinishEnding(false);
                    break;
                }
                case Ending.Sink:
                {
                    float k = Mathf.Clamp01(endingT / 0.6f);
                    Vector3 p = endingFrom + Vector3.down * (k * 0.35f);
                    ContactPosition = p;
                    Apply(p);
                    mesh.localScale = Vector3.one * (1f - 0.5f * k);
                    shadow.gameObject.SetActive(false);
                    if (endingT > 0.7f) FinishEnding(true);
                    break;
                }
                case Ending.Fall:
                {
                    Vector3 p = endingFrom + Vector3.down * (endingT * endingT * 4.9f);
                    ContactPosition = p;
                    Apply(p);
                    shadow.gameObject.SetActive(false);
                    if (endingT > 0.55f) FinishEnding(true);
                    break;
                }
            }
        }

        void FinishEnding(bool hide)
        {
            ending = Ending.None;
            if (hide) mesh.gameObject.SetActive(false);
            else mesh.gameObject.SetActive(false);
            if (OnPlaybackFinished != null) OnPlaybackFinished();
        }

        void Roll(Vector3 delta)
        {
            Vector3 flat = new Vector3(delta.x, 0f, delta.z);
            float dist = flat.magnitude;
            if (dist < 1e-6f) return;
            Vector3 axis = Vector3.Cross(Vector3.up, flat / dist);
            spin = Quaternion.AngleAxis(dist / Radius * Mathf.Rad2Deg, axis) * spin;
            mesh.rotation = spin;
        }

        void Apply(Vector3 contact)
        {
            transform.position = contact;
            mesh.position = contact + Vector3.up * Radius;
            float groundY = contact.y;
            if (course != null)
            {
                var g = course.Ground(new Vector2(contact.x, contact.z));
                if (!g.IsVoid) groundY = g.Height;
            }
            float height = Mathf.Max(0f, contact.y - groundY);
            float s = Radius * (1.35f + height * 0.8f);
            shadow.position = new Vector3(contact.x, groundY + 0.012f, contact.z);
            shadow.localScale = new Vector3(s, 1f, s);
            shadowRenderer.enabled = height < 2.5f;
        }
    }
}
