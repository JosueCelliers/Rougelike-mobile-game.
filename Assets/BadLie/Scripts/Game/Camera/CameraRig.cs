using BadLie.Course;
using UnityEngine;

namespace BadLie.Game
{
    /// <summary>
    /// Angled overhead camera with a restrained perspective. Holds still while aiming, follows
    /// the ball while it rolls, allows clamped survey panning, and plays the hole intro.
    /// Framing is expressed as the visible ground width so every phone shape frames alike.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public Camera Cam;
        public float Pitch = 52f;
        public float Fov = 30f;
        public float PlayWidth = 12.5f;
        public float SurveyWidth = 19f;
        public float MinWidth = 8f;
        public float MaxWidth = 26f;
        /// <summary>Where the ball sits vertically on screen when idle (0 bottom .. 1 top).</summary>
        public float BallScreenY = 0.40f;

        public enum Mode { Idle, Aim, Follow, Survey, Scripted }
        public Mode Current { get; private set; }

        float yaw;
        Vector3 focus, focusVel;
        float width, widthVel;
        Vector3 targetFocus;
        float targetWidth;
        Rect bounds;
        float smoothTime = 0.45f;

        public float Yaw { get { return yaw; } }
        public float Width { get { return width; } }
        public Vector3 Focus { get { return focus; } }

        public void SetupForHole(CourseModel course)
        {
            yaw = course.Def.CameraYaw;
            bounds = course.Def.PlayBounds;
            if (bounds.width <= 0f) bounds = course.Bounds;
        }

        public Vector3 ForwardFlat
        {
            get
            {
                float a = yaw * Mathf.Deg2Rad;
                return new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            }
        }

        public float DistanceFor(float groundWidth)
        {
            float aspect = Cam != null && Cam.aspect > 0.01f ? Cam.aspect : 0.46f;
            float halfH = Mathf.Tan(Fov * 0.5f * Mathf.Deg2Rad);
            return groundWidth * 0.5f / (halfH * aspect);
        }

        /// <summary>Ground length covered by the full screen height at a given width.</summary>
        public float GroundDepthFor(float groundWidth)
        {
            float d = DistanceFor(groundWidth);
            return 2f * d * Mathf.Tan(Fov * 0.5f * Mathf.Deg2Rad) / Mathf.Sin(Pitch * Mathf.Deg2Rad);
        }

        Vector3 FocusForBall(Vector3 ball, float groundWidth)
        {
            float depth = GroundDepthFor(groundWidth);
            return ball + ForwardFlat * ((0.5f - BallScreenY) * depth);
        }

        public void SnapTo(Vector3 ball, float groundWidth)
        {
            width = targetWidth = groundWidth;
            focus = targetFocus = ClampFocus(FocusForBall(ball, groundWidth));
            focusVel = Vector3.zero;
            widthVel = 0f;
            Current = Mode.Idle;
            ApplyTransform();
        }

        public void FrameBall(Vector3 ball, float smooth = 0.5f)
        {
            Current = Mode.Idle;
            smoothTime = smooth;
            targetWidth = PlayWidth;
            targetFocus = ClampFocus(FocusForBall(ball, PlayWidth));
        }

        public void HoldForAim() { Current = Mode.Aim; }

        public void Follow(Vector3 ball)
        {
            Current = Mode.Follow;
            smoothTime = 0.38f;
            targetWidth = Mathf.Max(width, PlayWidth);
            // Keep the ball inside the middle of the frame without chasing every wobble.
            Vector3 desired = FocusForBall(ball, targetWidth);
            Vector3 delta = desired - targetFocus;
            float slack = GroundDepthFor(targetWidth) * 0.12f;
            if (delta.magnitude > slack) targetFocus = ClampFocus(desired - delta.normalized * slack);
        }

        public void BeginSurvey()
        {
            Current = Mode.Survey;
            smoothTime = 0.35f;
            targetWidth = SurveyWidth;
        }

        public void Pan(Vector2 screenDelta)
        {
            if (Cam == null) return;
            // Convert a screen drag into a ground-plane move (dragging moves the world).
            float depth = GroundDepthFor(width);
            Vector3 right = Quaternion.Euler(0, yaw, 0) * Vector3.right;
            Vector3 fwd = ForwardFlat;
            Vector3 move = -right * (screenDelta.x / Screen.width * width) - fwd * (screenDelta.y / Screen.height * depth);
            targetFocus = ClampFocus(targetFocus + move);
            focus = ClampFocus(focus + move);
        }

        public void Zoom(float factor)
        {
            targetWidth = Mathf.Clamp(targetWidth * factor, MinWidth, MaxWidth);
        }

        public void ScriptTo(Vector3 focusPoint, float groundWidth, float smooth)
        {
            Current = Mode.Scripted;
            smoothTime = smooth;
            targetFocus = focusPoint;
            targetWidth = groundWidth;
        }

        /// <summary>Width that fits the whole hole on screen (for the intro and survey).</summary>
        public float WidthToFit(Rect r)
        {
            // Rotate rect corners into camera space to measure extents.
            Quaternion inv = Quaternion.Euler(0, -yaw, 0);
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            Vector2[] corners = { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax) };
            foreach (var c in corners)
            {
                Vector3 l = inv * new Vector3(c.x, 0, c.y);
                minX = Mathf.Min(minX, l.x);
                maxX = Mathf.Max(maxX, l.x);
                minZ = Mathf.Min(minZ, l.z);
                maxZ = Mathf.Max(maxZ, l.z);
            }
            float w = maxX - minX;
            float depthNeeded = maxZ - minZ;
            float ratio = GroundDepthFor(1f);
            return Mathf.Clamp(Mathf.Max(w, depthNeeded / ratio), MinWidth, MaxWidth * 1.4f);
        }

        Vector3 ClampFocus(Vector3 f)
        {
            f.x = Mathf.Clamp(f.x, bounds.xMin, bounds.xMax);
            f.z = Mathf.Clamp(f.z, bounds.yMin, bounds.yMax);
            return f;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (Current != Mode.Aim)
            {
                focus = Vector3.SmoothDamp(focus, targetFocus, ref focusVel, smoothTime, Mathf.Infinity, dt);
                width = Mathf.SmoothDamp(width, targetWidth, ref widthVel, smoothTime, Mathf.Infinity, dt);
            }
            ApplyTransform();
        }

        public void ApplyTransform()
        {
            if (Cam == null) return;
            float d = DistanceFor(width);
            Quaternion rot = Quaternion.Euler(Pitch, yaw, 0f);
            Vector3 pos = focus - rot * Vector3.forward * d;
            Cam.transform.SetPositionAndRotation(pos, rot);
            Atmosphere.CameraDistance = d;
            Cam.fieldOfView = Fov;
            Cam.nearClipPlane = Mathf.Max(1f, d - 45f);
            Cam.farClipPlane = d + 160f;
        }

        /// <summary>Ray from a screen point onto the horizontal plane at height y.</summary>
        public bool ScreenToGround(Vector2 screen, float y, out Vector3 world)
        {
            world = Vector3.zero;
            if (Cam == null) return false;
            Ray r = Cam.ScreenPointToRay(screen);
            if (Mathf.Abs(r.direction.y) < 1e-5f) return false;
            float t = (y - r.origin.y) / r.direction.y;
            if (t < 0f) return false;
            world = r.origin + r.direction * t;
            return true;
        }
    }
}
