using System;
using BadLie.Sim;
using UnityEngine;

namespace BadLie.Game
{
    /// <summary>
    /// Drag-back aiming. Touch anywhere (except UI), pull away from where the shot should go,
    /// release to shoot. Releasing back inside the start circle, a second finger, right-click
    /// or Escape cancels. Direction is measured on the course plane (so it matches what you
    /// see); power is measured in screen distance (so it feels the same in every direction).
    /// </summary>
    public sealed class AimController
    {
        public enum State { Idle, Pressed, Aiming }

        public State Current { get; private set; }
        public Vector2 StartScreen { get; private set; }
        public Vector2 CurrentScreen { get; private set; }
        public Vector2 Direction { get; private set; }
        public float Power { get; private set; }
        public bool InCancelZone { get; private set; }

        public event Action<ShotInput> OnShoot;
        public event Action OnCancel;
        public event Action OnBegin;

        public float DeadZonePx
        {
            get
            {
                float shortSide = Mathf.Min(Screen.width, Screen.height);
                return Mathf.Max(PointerInput.Millimetres(4f), shortSide * 0.04f);
            }
        }

        public float MaxDragPx
        {
            get
            {
                float shortSide = Mathf.Min(Screen.width, Screen.height);
                return Mathf.Clamp(shortSide * 0.38f, PointerInput.Millimetres(20f), PointerInput.Millimetres(40f)) + DeadZonePx;
            }
        }

        public bool IsActive { get { return Current == State.Aiming; } }

        public void Reset()
        {
            Current = State.Idle;
            Power = 0f;
            InCancelZone = false;
        }

        /// <param name="overUI">Returns true when a screen point is over interactive UI.</param>
        public void Tick(PointerFrame f, CameraRig rig, Vector3 ball, bool allowed, Func<Vector2, bool> overUI)
        {
            if (Current == State.Idle)
            {
                if (!allowed) return;
                if (f.PrimaryDown && (overUI == null || !overUI(f.PrimaryPos)))
                {
                    Current = State.Pressed;
                    StartScreen = CurrentScreen = f.PrimaryPos;
                    Power = 0f;
                    InCancelZone = true;
                }
                return;
            }

            if (!allowed || f.Cancel)
            {
                bool wasAiming = Current == State.Aiming;
                Reset();
                if (wasAiming && OnCancel != null) OnCancel();
                return;
            }

            CurrentScreen = f.PrimaryPos;
            Vector2 drag = CurrentScreen - StartScreen;
            float len = drag.magnitude;
            float dead = DeadZonePx;
            if (Current == State.Pressed && len > dead)
            {
                Current = State.Aiming;
                if (OnBegin != null) OnBegin();
            }
            if (Current == State.Aiming)
            {
                InCancelZone = len <= dead;
                Power = Mathf.Clamp01((len - dead) / (MaxDragPx - dead));
                Vector3 s, c;
                if (len > 1f && rig.ScreenToGround(StartScreen, ball.y, out s) && rig.ScreenToGround(CurrentScreen, ball.y, out c))
                {
                    Vector2 pull = new Vector2(c.x - s.x, c.z - s.z);
                    if (pull.sqrMagnitude > 1e-8f) Direction = -pull.normalized;
                }
            }

            if (f.PrimaryUp || !f.PrimaryHeld)
            {
                bool shoot = Current == State.Aiming && !InCancelZone && Power > 0.001f;
                bool wasAiming = Current == State.Aiming;
                var input = new ShotInput(Direction, Power);
                Reset();
                if (shoot)
                {
                    if (OnShoot != null) OnShoot(input);
                }
                else if (wasAiming && OnCancel != null) OnCancel();
            }
        }
    }
}
