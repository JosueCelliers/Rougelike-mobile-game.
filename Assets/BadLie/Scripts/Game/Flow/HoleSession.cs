using System;
using BadLie.Course;
using BadLie.Sim;
using UnityEngine;

namespace BadLie.Game
{
    /// <summary>
    /// Plays one hole: aiming, the preview, simulating a released shot and playing it back.
    /// Rules (strokes, penalties, upgrades) live in the run controller, which listens here.
    /// </summary>
    public sealed class HoleSession : MonoBehaviour
    {
        public CourseModel Course { get; private set; }
        public HoleView View { get; private set; }
        public BallView Ball { get; private set; }
        public CameraRig Rig { get; private set; }
        public AimVisuals AimVis { get; private set; }
        public readonly AimController Aim = new AimController();

        public Vector2 Lie { get; private set; }
        public float LieHeight { get; private set; }
        public ShotModifiers Mods = ShotModifiers.None;
        public SimResult LastShot { get; private set; }
        public ShotInput LastInput { get; private set; }
        public bool Surveying { get; private set; }

        public Func<bool> ShotAllowed;
        public Func<Vector2, bool> OverUI;

        public event Action<ShotInput, SimResult> ShotReleased;
        public event Action<SimResult> ShotFinished;
        public event Action<SimEvent> BallEvent;
        public event Action AimBegan;
        public event Action AimCancelled;

        public enum Phase { Idle, Ready, Flight }
        public Phase Current { get; private set; }

        readonly BallSimulator sim = new BallSimulator();
        readonly SimResult preview = new SimResult();
        Vector2 previewDir;
        float previewPower = -1f;
        Vector2 surveyLast;
        bool surveyDragging;
        float pinchLast = -1f;

        public static HoleSession Create(Transform parent, GameConfig cfg, CameraRig rig, CourseModel course)
        {
            var go = new GameObject("Hole Session");
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<HoleSession>();
            s.Rig = rig;
            s.Course = course;
            s.View = HoleView.Create(course, cfg, go.transform);
            s.Ball = BallView.Create(cfg, go.transform);
            s.Ball.SetCourse(course);
            s.Ball.OnEvent += s.HandleBallEvent;
            s.Ball.OnPlaybackFinished += s.HandlePlaybackFinished;
            s.AimVis = AimVisuals.Create(cfg, go.transform);
            s.Aim.OnShoot += s.Shoot;
            s.Aim.OnCancel += () => { s.AimVis.Hide(); s.Rig.FrameBall(s.Ball.ContactPosition); if (s.AimCancelled != null) s.AimCancelled(); };
            s.Aim.OnBegin += () => { s.Rig.HoldForAim(); if (s.AimBegan != null) s.AimBegan(); };
            rig.SetupForHole(course);
            return s;
        }

        public void PlaceBall(Vector2 p, float h)
        {
            Lie = p;
            LieHeight = h;
            Ball.Place(p, h);
        }

        public void SetReady()
        {
            Current = Phase.Ready;
            Aim.Reset();
            AimVis.Hide();
            Rig.FrameBall(Ball.ContactPosition);
        }

        public void SetIdle()
        {
            Current = Phase.Idle;
            Aim.Reset();
            AimVis.Hide();
        }

        public void SetSurvey(bool on)
        {
            Surveying = on;
            surveyDragging = false;
            if (on)
            {
                Aim.Reset();
                AimVis.Hide();
                Rig.BeginSurvey();
            }
            else Rig.FrameBall(Ball.ContactPosition, 0.6f);
        }

        public SimResult Simulate(ShotInput input, SimOptions opts, SimResult reuse = null)
        {
            return sim.Simulate(Course, new BallStart(Lie, LieHeight), input, Mods, opts, reuse);
        }

        public void Shoot(ShotInput input)
        {
            if (Current != Phase.Ready) return;
            AimVis.Hide();
            LastInput = input;
            LastShot = Simulate(input, SimOptions.Full);
            Current = Phase.Flight;
            if (ShotReleased != null) ShotReleased(input, LastShot);
            Ball.Play(LastShot);
        }

        void HandleBallEvent(SimEvent e)
        {
            if (BallEvent != null) BallEvent(e);
        }

        void HandlePlaybackFinished()
        {
            if (Current != Phase.Flight) return;
            Current = Phase.Idle;
            if (ShotFinished != null) ShotFinished(LastShot);
        }

        void Update()
        {
            var f = PointerInput.Read();
            if (Current == Phase.Flight)
            {
                Rig.Follow(Ball.ContactPosition);
                return;
            }
            if (Current != Phase.Ready) return;

            if (Surveying)
            {
                UpdateSurvey(f);
                return;
            }

            if (f.IsMouse && f.Scroll != 0f && Aim.Current == AimController.State.Idle) Rig.Zoom(f.Scroll > 0f ? 0.9f : 1.1f);

            bool allowed = ShotAllowed == null || ShotAllowed();
            Aim.Tick(f, Rig, Ball.ContactPosition, allowed, OverUI);
            if (Aim.IsActive)
            {
                RefreshPreview();
                AimVis.Draw(Course, Ball.ContactPosition, Aim.Direction, Aim.Power, Aim.InCancelZone, Aim.InCancelZone ? null : preview);
                Atmosphere.SetReveal(Rig.Cam, Ball.ContactPosition + Vector3.up * 0.14f, !Aim.InCancelZone, AimVis.LastPreviewEnd, 0.11f);
            }
            else
            {
                Atmosphere.SetReveal(Rig.Cam, Ball.ContactPosition + Vector3.up * 0.14f, false, Vector3.zero, 0.09f);
            }
        }

        void LateUpdate()
        {
            if (Current == Phase.Flight) Atmosphere.SetReveal(Rig.Cam, Ball.ContactPosition + Vector3.up * 0.14f, false, Vector3.zero, 0.09f);
        }

        void UpdateSurvey(PointerFrame f)
        {
            // Two fingers: pinch to zoom (spreading the fingers zooms in).
            if (f.TouchCount >= 2)
            {
                float d = (f.PrimaryPos - f.SecondaryPos).magnitude;
                if (pinchLast > 0f && d > 1f) Rig.Zoom(pinchLast / d);
                pinchLast = d;
                surveyDragging = false;
                return;
            }
            if (pinchLast > 0f)
            {
                // Back to one finger: continue panning from where it is now, without a jump.
                pinchLast = -1f;
                surveyDragging = f.PrimaryHeld;
                surveyLast = f.PrimaryPos;
            }
            if (f.PrimaryDown && (OverUI == null || !OverUI(f.PrimaryPos)))
            {
                surveyDragging = true;
                surveyLast = f.PrimaryPos;
            }
            if (surveyDragging && f.PrimaryHeld)
            {
                Rig.Pan(f.PrimaryPos - surveyLast);
                surveyLast = f.PrimaryPos;
            }
            if (f.PrimaryUp) surveyDragging = false;
            if (f.Scroll != 0f) Rig.Zoom(f.Scroll > 0f ? 0.9f : 1.1f);
        }

        void RefreshPreview()
        {
            if (Mathf.Abs(Aim.Power - previewPower) < 0.002f && (Aim.Direction - previewDir).sqrMagnitude < 1e-6f) return;
            previewPower = Aim.Power;
            previewDir = Aim.Direction;
            float distance = PreviewLength(Aim.Power);
            Simulate(new ShotInput(Aim.Direction, Aim.Power), SimOptions.Preview(distance, 1.3f), preview);
        }

        /// <summary>The preview shows only the first part of the shot.</summary>
        public static float PreviewLength(float power) { return 2.4f + 5.2f * power; }
    }
}
