using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace BadLie.Game
{
    /// <summary>One frame of pointer input, unified across touch and mouse.</summary>
    public struct PointerFrame
    {
        public bool PrimaryDown;      // primary pointer went down this frame
        public bool PrimaryHeld;
        public bool PrimaryUp;        // released this frame
        public Vector2 PrimaryPos;
        public int TouchCount;        // simultaneous touches (1 for mouse)
        public Vector2 SecondaryPos;
        public bool Cancel;           // right mouse / escape / second finger
        public float Scroll;          // mouse wheel steps
        public bool IsMouse;
        public bool MiddleHeld;
        public Vector2 MouseDelta;
    }

    /// <summary>
    /// Reads touch (Enhanced Touch) and mouse into a PointerFrame. Capture scripts can inject
    /// frames, which then replace device input entirely (used for scripted screenshots/tests).
    /// </summary>
    public static class PointerInput
    {
        static bool enabled;
        static int primaryTouchId = -1;
        static bool mouseWasDown;
        static Vector2 lastMouse;

        public static bool Scripted;
        public static PointerFrame ScriptedFrame;

        public static void Enable()
        {
            if (enabled) return;
            enabled = true;
            EnhancedTouchSupport.Enable();
        }

        public static PointerFrame Read()
        {
            if (Scripted)
            {
                var f = ScriptedFrame;
                // Edges are one-frame events.
                ScriptedFrame.PrimaryDown = false;
                ScriptedFrame.PrimaryUp = false;
                ScriptedFrame.Cancel = false;
                ScriptedFrame.Scroll = 0f;
                return f;
            }
            Enable();
            var frame = new PointerFrame();
            var touches = ETouch.activeTouches;
            if (touches.Count > 0 || primaryTouchId >= 0)
            {
                frame.TouchCount = touches.Count;
                bool found = false;
                for (int i = 0; i < touches.Count; i++)
                {
                    var t = touches[i];
                    if (primaryTouchId < 0 && t.phase == UnityEngine.InputSystem.TouchPhase.Began)
                    {
                        primaryTouchId = t.touchId;
                        frame.PrimaryDown = true;
                    }
                    if (t.touchId == primaryTouchId)
                    {
                        found = true;
                        frame.PrimaryPos = t.screenPosition;
                        bool ended = t.phase == UnityEngine.InputSystem.TouchPhase.Ended || t.phase == UnityEngine.InputSystem.TouchPhase.Canceled;
                        frame.PrimaryHeld = !ended;
                        if (ended)
                        {
                            frame.PrimaryUp = true;
                            primaryTouchId = -1;
                        }
                    }
                    else
                    {
                        frame.SecondaryPos = t.screenPosition;
                    }
                }
                if (!found && primaryTouchId >= 0)
                {
                    frame.PrimaryUp = true;
                    primaryTouchId = -1;
                }
                if (touches.Count >= 2) frame.Cancel = true;
                return frame;
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                frame.IsMouse = true;
                Vector2 pos = mouse.position.ReadValue();
                bool down = mouse.leftButton.isPressed;
                frame.PrimaryPos = pos;
                frame.PrimaryDown = down && !mouseWasDown;
                frame.PrimaryUp = !down && mouseWasDown;
                frame.PrimaryHeld = down;
                frame.TouchCount = down ? 1 : 0;
                frame.Cancel = mouse.rightButton.wasPressedThisFrame;
                frame.Scroll = mouse.scroll.ReadValue().y;
                frame.MiddleHeld = mouse.middleButton.isPressed;
                frame.MouseDelta = pos - lastMouse;
                lastMouse = pos;
                mouseWasDown = down;
            }
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) frame.Cancel = true;
            return frame;
        }

        /// <summary>Screen size in pixels of a physical length, with a sensible fallback.</summary>
        public static float Millimetres(float mm)
        {
            float dpi = Screen.dpi > 30f ? Screen.dpi : 160f * Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height) / 400f);
            return mm / 25.4f * dpi;
        }
    }
}
