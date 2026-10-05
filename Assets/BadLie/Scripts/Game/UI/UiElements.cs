using System;
using BadLie.Run;
using UnityEngine;
using UnityEngine.UIElements;

namespace BadLie.Game
{
    /// <summary>Shared colours of the interface (sRGB).</summary>
    public static class UiColors
    {
        public static readonly Color Ink = new Color(0.10f, 0.08f, 0.10f, 0.86f);
        public static readonly Color InkSolid = new Color(0.10f, 0.08f, 0.10f, 1f);
        public static readonly Color Gold = new Color(0.86f, 0.71f, 0.45f, 1f);
        public static readonly Color GoldDim = new Color(0.86f, 0.71f, 0.45f, 0.35f);
        public static readonly Color Ivory = new Color(0.96f, 0.91f, 0.82f, 1f);
        public static readonly Color IvoryDim = new Color(0.96f, 0.91f, 0.82f, 0.62f);
        public static readonly Color Cyan = new Color(0.61f, 0.95f, 0.92f, 1f);
        public static readonly Color Amber = new Color(1f, 0.77f, 0.42f, 1f);
        public static readonly Color Vermilion = new Color(0.88f, 0.27f, 0.18f, 1f);
        public static readonly Color Empty = new Color(0.96f, 0.91f, 0.82f, 0.18f);
    }

    /// <summary>Stroke budget as hexagonal pips (Death's Door-like), filled for remaining strokes.</summary>
    public sealed class HexPips : VisualElement
    {
        int value, capacity = 12;
        public float Size = 30f;
        public Color Fill = UiColors.Ivory;
        public Color Warn = UiColors.Vermilion;

        public HexPips()
        {
            generateVisualContent += Draw;
            pickingMode = PickingMode.Ignore;
        }

        public void Set(int strokes, int cap)
        {
            value = strokes;
            capacity = cap;
            style.width = Mathf.Min(cap, 14) * Size * 0.92f + 4f;
            style.height = Size * 1.05f;
            MarkDirtyRepaint();
        }

        void Draw(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;
            int n = Mathf.Min(capacity, 14);
            float r = Size * 0.46f;
            bool low = value <= 2;
            for (int i = 0; i < n; i++)
            {
                Vector2 c = new Vector2(2f + r + i * Size * 0.92f, Size * 0.52f);
                bool filled = i < value;
                p.BeginPath();
                for (int k = 0; k < 6; k++)
                {
                    float a = Mathf.PI / 6f + k * Mathf.PI / 3f;
                    Vector2 q = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                    if (k == 0) p.MoveTo(q); else p.LineTo(q);
                }
                p.ClosePath();
                if (filled)
                {
                    p.fillColor = low ? Warn : Fill;
                    p.Fill();
                }
                else
                {
                    p.strokeColor = UiColors.Empty;
                    p.lineWidth = 2.5f;
                    p.Stroke();
                }
            }
        }
    }

    public enum IconKind { Eye, Feather, Gear, Close, Flag, Ball, Back }

    /// <summary>Line-art icon drawn with Painter2D so it stays crisp at any size.</summary>
    public sealed class Icon : VisualElement
    {
        public IconKind Kind;
        public Color Tint = UiColors.Ivory;
        public float Line = 4f;

        public Icon(IconKind kind, float size)
        {
            Kind = kind;
            style.width = size;
            style.height = size;
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;
            Rect r = contentRect;
            float s = Mathf.Min(r.width, r.height);
            Vector2 c = r.center;
            p.strokeColor = Tint;
            p.fillColor = Tint;
            p.lineWidth = Line;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;
            switch (Kind)
            {
                case IconKind.Eye:
                    p.BeginPath();
                    p.MoveTo(c + new Vector2(-s * 0.42f, 0));
                    p.BezierCurveTo(c + new Vector2(-s * 0.18f, -s * 0.3f), c + new Vector2(s * 0.18f, -s * 0.3f), c + new Vector2(s * 0.42f, 0));
                    p.BezierCurveTo(c + new Vector2(s * 0.18f, s * 0.3f), c + new Vector2(-s * 0.18f, s * 0.3f), c + new Vector2(-s * 0.42f, 0));
                    p.Stroke();
                    p.BeginPath();
                    p.Arc(c, s * 0.11f, Angle.Degrees(0f), Angle.Degrees(360f));
                    p.Fill();
                    break;
                case IconKind.Feather:
                    p.BeginPath();
                    p.MoveTo(c + new Vector2(-s * 0.32f, s * 0.36f));
                    p.LineTo(c + new Vector2(s * 0.3f, -s * 0.34f));
                    p.Stroke();
                    p.BeginPath();
                    p.MoveTo(c + new Vector2(-s * 0.2f, s * 0.22f));
                    p.BezierCurveTo(c + new Vector2(-s * 0.28f, -s * 0.12f), c + new Vector2(s * 0.02f, -s * 0.4f), c + new Vector2(s * 0.3f, -s * 0.34f));
                    p.BezierCurveTo(c + new Vector2(s * 0.24f, -s * 0.02f), c + new Vector2(s * 0.06f, s * 0.22f), c + new Vector2(-s * 0.2f, s * 0.22f));
                    p.Stroke();
                    break;
                case IconKind.Gear:
                    p.BeginPath();
                    p.Arc(c, s * 0.16f, Angle.Degrees(0f), Angle.Degrees(360f));
                    p.Stroke();
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.PI / 4f;
                        Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        p.BeginPath();
                        p.MoveTo(c + d * s * 0.27f);
                        p.LineTo(c + d * s * 0.4f);
                        p.Stroke();
                    }
                    p.BeginPath();
                    p.Arc(c, s * 0.28f, Angle.Degrees(0f), Angle.Degrees(360f));
                    p.Stroke();
                    break;
                case IconKind.Close:
                    p.BeginPath();
                    p.MoveTo(c + new Vector2(-s * 0.28f, -s * 0.28f));
                    p.LineTo(c + new Vector2(s * 0.28f, s * 0.28f));
                    p.MoveTo(c + new Vector2(s * 0.28f, -s * 0.28f));
                    p.LineTo(c + new Vector2(-s * 0.28f, s * 0.28f));
                    p.Stroke();
                    break;
                case IconKind.Flag:
                    p.BeginPath();
                    p.MoveTo(c + new Vector2(-s * 0.2f, s * 0.4f));
                    p.LineTo(c + new Vector2(-s * 0.2f, -s * 0.4f));
                    p.Stroke();
                    p.fillColor = UiColors.Vermilion;
                    p.BeginPath();
                    p.MoveTo(c + new Vector2(-s * 0.2f, -s * 0.4f));
                    p.LineTo(c + new Vector2(s * 0.32f, -s * 0.24f));
                    p.LineTo(c + new Vector2(-s * 0.2f, -s * 0.08f));
                    p.ClosePath();
                    p.Fill();
                    break;
                case IconKind.Ball:
                    p.BeginPath();
                    p.Arc(c, s * 0.3f, Angle.Degrees(0f), Angle.Degrees(360f));
                    p.Fill();
                    break;
                case IconKind.Back:
                    p.BeginPath();
                    p.MoveTo(c + new Vector2(s * 0.14f, -s * 0.3f));
                    p.LineTo(c + new Vector2(-s * 0.18f, 0f));
                    p.LineTo(c + new Vector2(s * 0.14f, s * 0.3f));
                    p.Stroke();
                    break;
            }
        }
    }

    /// <summary>Diamond frame holding an owned upgrade (or an empty slot).</summary>
    public sealed class DiamondSlot : VisualElement
    {
        public Texture2D Art;
        public bool Spent;
        public bool Highlight;

        public DiamondSlot(float size)
        {
            style.width = size;
            style.height = size;
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;
            Rect r = contentRect;
            Vector2 c = r.center;
            float h = Mathf.Min(r.width, r.height) * 0.48f;
            p.BeginPath();
            p.MoveTo(c + new Vector2(0, -h));
            p.LineTo(c + new Vector2(h, 0));
            p.LineTo(c + new Vector2(0, h));
            p.LineTo(c + new Vector2(-h, 0));
            p.ClosePath();
            p.fillColor = UiColors.Ink;
            p.Fill();
            p.strokeColor = Highlight ? UiColors.Cyan : (Art != null && !Spent ? UiColors.Gold : UiColors.GoldDim);
            p.lineWidth = 3f;
            p.Stroke();
        }
    }

    /// <summary>Small circle shown where the aim drag started: release inside it to cancel.</summary>
    public sealed class CancelRing : VisualElement
    {
        public bool Active;

        public CancelRing()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;
            Rect r = contentRect;
            p.BeginPath();
            p.Arc(r.center, r.width * 0.46f, Angle.Degrees(0f), Angle.Degrees(360f));
            p.strokeColor = Active ? UiColors.Vermilion : new Color(1f, 1f, 1f, 0.55f);
            p.lineWidth = Active ? 5f : 3f;
            p.Stroke();
            if (Active)
            {
                float s = r.width * 0.16f;
                p.BeginPath();
                p.MoveTo(r.center + new Vector2(-s, -s));
                p.LineTo(r.center + new Vector2(s, s));
                p.MoveTo(r.center + new Vector2(s, -s));
                p.LineTo(r.center + new Vector2(-s, s));
                p.Stroke();
            }
        }
    }

    /// <summary>A minimal slider drawn with Painter2D (track, fill, knob) using pointer capture.</summary>
    public sealed class SimpleSlider : VisualElement
    {
        float value;
        public event Action<float> Changed;

        public SimpleSlider(float initial)
        {
            value = Mathf.Clamp01(initial);
            style.height = 96f;
            style.flexGrow = 1f;
            generateVisualContent += Draw;
            RegisterCallback<PointerDownEvent>(e => { this.CapturePointer(e.pointerId); SetFromX(e.localPosition.x); });
            RegisterCallback<PointerMoveEvent>(e => { if (this.HasPointerCapture(e.pointerId)) SetFromX(e.localPosition.x); });
            RegisterCallback<PointerUpEvent>(e => { if (this.HasPointerCapture(e.pointerId)) this.ReleasePointer(e.pointerId); });
        }

        void SetFromX(float x)
        {
            float pad = 30f;
            value = Mathf.Clamp01((x - pad) / Mathf.Max(1f, contentRect.width - 2f * pad));
            MarkDirtyRepaint();
            if (Changed != null) Changed(value);
        }

        void Draw(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;
            Rect r = contentRect;
            float pad = 30f, y = r.height * 0.5f;
            float x0 = pad, x1 = r.width - pad, xv = Mathf.Lerp(x0, x1, value);
            p.lineCap = LineCap.Round;
            p.lineWidth = 8f;
            p.strokeColor = UiColors.Empty;
            p.BeginPath();
            p.MoveTo(new Vector2(x0, y));
            p.LineTo(new Vector2(x1, y));
            p.Stroke();
            p.strokeColor = UiColors.Gold;
            p.BeginPath();
            p.MoveTo(new Vector2(x0, y));
            p.LineTo(new Vector2(Mathf.Max(x0 + 0.1f, xv), y));
            p.Stroke();
            p.fillColor = UiColors.Ivory;
            p.BeginPath();
            p.Arc(new Vector2(xv, y), 22f, Angle.Degrees(0f), Angle.Degrees(360f));
            p.Fill();
        }
    }

    public static class UpgradeArtIndex
    {
        public static Texture2D Get(GameConfig cfg, UpgradeId id)
        {
            if (cfg == null || cfg.UpgradeArt == null) return null;
            int i = (int)id;
            return i < cfg.UpgradeArt.Length ? cfg.UpgradeArt[i] : null;
        }

        public static Texture2D Icon(GameConfig cfg, UpgradeId id)
        {
            if (cfg == null || cfg.UpgradeIcons == null) return null;
            int i = (int)id;
            return i < cfg.UpgradeIcons.Length ? cfg.UpgradeIcons[i] : null;
        }
    }
}
