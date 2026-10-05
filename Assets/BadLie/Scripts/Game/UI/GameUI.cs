using System;
using System.Collections.Generic;
using BadLie.Run;
using UnityEngine;
using UnityEngine.UIElements;

namespace BadLie.Game
{
    /// <summary>
    /// The whole interface, built in code on UI Toolkit: compact HUD in the top safe area,
    /// thumb-zone buttons at the bottom, cards for holes, upgrades and run results.
    /// </summary>
    public sealed class GameUI : MonoBehaviour
    {
        GameConfig cfg;
        UIDocument doc;
        VisualElement root, safe, hud, overlay, toastLayer, hintLayer;
        // HUD parts
        Label hudHole, hudName, hudStrokes, hudSub;
        HexPips pips;
        VisualElement upgradeRow;
        Button surveyBtn, undoBtn, settingsBtn;
        Label toastHead, toastDetail;
        VisualElement toast;
        float toastUntil;
        CancelRing cancelRing;
        Label hintLabel;
        Rect lastSafe;

        public bool ModalOpen { get; private set; }
        public event Action SurveyToggled;
        public event Action UndoPressed;
        public event Action SettingsPressed;

        public static GameUI Create(GameConfig cfg, Transform parent)
        {
            var go = new GameObject("UI");
            go.transform.SetParent(parent, false);
            var ui = go.AddComponent<GameUI>();
            ui.cfg = cfg;
            ui.doc = go.AddComponent<UIDocument>();
            ui.doc.panelSettings = cfg.Panel;
            ui.Build();
            return ui;
        }

        // ------------------------------------------------------------------ helpers

        static Label L(string text, params string[] classes)
        {
            var l = new Label(text);
            foreach (var c in classes) l.AddToClassList(c);
            l.pickingMode = PickingMode.Ignore;
            return l;
        }

        Label T(string text, params string[] classes)
        {
            var l = L(text, classes);
            if (cfg.TitleFont != null) l.style.unityFontDefinition = FontDefinition.FromFont(cfg.TitleFont);
            return l;
        }

        Label B(string text, params string[] classes)
        {
            var l = L(text, classes);
            if (cfg.BodyFont != null) l.style.unityFontDefinition = FontDefinition.FromFont(cfg.BodyFont);
            return l;
        }

        Button Btn(string text, Action onClick, bool primary = false)
        {
            var b = new Button(() => { AudioDirector.UiTap(); if (onClick != null) onClick(); }) { text = text };
            b.AddToClassList("btn");
            if (primary) b.AddToClassList("btn-primary");
            if (cfg.TitleFont != null) b.style.unityFontDefinition = FontDefinition.FromFont(cfg.TitleFont);
            return b;
        }

        Button IconBtn(IconKind kind, Action onClick)
        {
            var b = new Button(() => { AudioDirector.UiTap(); if (onClick != null) onClick(); });
            b.AddToClassList("btn");
            b.AddToClassList("btn-round");
            b.Add(new Icon(kind, 72f));
            return b;
        }

        // ------------------------------------------------------------------ build

        void Build()
        {
            root = doc.rootVisualElement;
            if (cfg.Style != null) root.styleSheets.Add(cfg.Style);
            if (cfg.BodyFont != null) root.style.unityFontDefinition = FontDefinition.FromFont(cfg.BodyFont);
            root.pickingMode = PickingMode.Ignore;

            safe = new VisualElement();
            safe.AddToClassList("safe");
            safe.pickingMode = PickingMode.Ignore;
            root.Add(safe);

            hud = Layer("hud");
            hintLayer = Layer("hints");
            toastLayer = Layer("toasts");
            overlay = Layer("overlay");

            BuildHud();
            BuildToast();
            BuildHint();
            ApplySafeArea();
            SetHudVisible(false);
        }

        VisualElement Layer(string name)
        {
            var v = new VisualElement { name = name };
            v.AddToClassList("layer");
            v.pickingMode = PickingMode.Ignore;
            safe.Add(v);
            return v;
        }

        void BuildHud()
        {
            var top = new VisualElement();
            top.AddToClassList("hud-top");
            top.pickingMode = PickingMode.Ignore;
            hud.Add(top);

            var cluster = new VisualElement();
            cluster.AddToClassList("hud-cluster");
            cluster.pickingMode = PickingMode.Ignore;
            top.Add(cluster);
            hudHole = T("HOLE 1 / 5 · PAR 4", "hud-hole");
            hudName = T("THE LANTERN GATE", "hud-name");
            cluster.Add(hudHole);
            cluster.Add(hudName);
            var row = new VisualElement();
            row.AddToClassList("hud-strokes-row");
            row.pickingMode = PickingMode.Ignore;
            pips = new HexPips { Size = 34f };
            row.Add(pips);
            hudStrokes = B("10", "hud-strokes-num");
            row.Add(hudStrokes);
            row.Add(B("STROKES\nLEFT", "hud-strokes-label"));
            cluster.Add(row);
            hudSub = B("This hole: 0", "hud-sub");
            cluster.Add(hudSub);
            upgradeRow = new VisualElement();
            upgradeRow.AddToClassList("hud-upgrades");
            upgradeRow.pickingMode = PickingMode.Ignore;
            cluster.Add(upgradeRow);

            settingsBtn = IconBtn(IconKind.Gear, () => { if (SettingsPressed != null) SettingsPressed(); });
            settingsBtn.style.width = 120;
            settingsBtn.style.height = 120;
            settingsBtn.style.minWidth = 120;
            settingsBtn.style.minHeight = 120;
            top.Add(settingsBtn);

            var bottom = new VisualElement();
            bottom.AddToClassList("hud-bottom");
            bottom.pickingMode = PickingMode.Ignore;
            hud.Add(bottom);
            surveyBtn = IconBtn(IconKind.Eye, () => { if (SurveyToggled != null) SurveyToggled(); });
            undoBtn = Btn("", () => { if (UndoPressed != null) UndoPressed(); });
            undoBtn.Add(new Icon(IconKind.Feather, 64f) { Tint = UiColors.Cyan });
            undoBtn.Add(B("UNDO SHOT", "kicker"));
            undoBtn.style.flexDirection = FlexDirection.Row;
            undoBtn.style.alignItems = Align.Center;
            bottom.Add(surveyBtn);
            bottom.Add(undoBtn);
            undoBtn.style.display = DisplayStyle.None;
        }

        void BuildToast()
        {
            toast = new VisualElement();
            toast.AddToClassList("toast");
            toast.pickingMode = PickingMode.Ignore;
            var plate = new VisualElement();
            plate.AddToClassList("plate");
            plate.pickingMode = PickingMode.Ignore;
            toastHead = T("WATER", "title");
            toastHead.style.fontSize = 52;
            toastDetail = B("+1 penalty stroke", "body");
            toastDetail.style.fontSize = 34;
            plate.Add(toastHead);
            plate.Add(toastDetail);
            toast.Add(plate);
            toastLayer.Add(toast);
        }

        void BuildHint()
        {
            cancelRing = new CancelRing();
            cancelRing.style.display = DisplayStyle.None;
            hintLayer.Add(cancelRing);
            hintLabel = B("Release inside the ring to cancel", "hint");
            hintLabel.style.display = DisplayStyle.None;
            hintLayer.Add(hintLabel);
        }

        void Update()
        {
            if (Screen.safeArea != lastSafe) ApplySafeArea();
            if (toast.ClassListContains("show") && Time.unscaledTime > toastUntil) toast.RemoveFromClassList("show");
        }

        /// <summary>Keeps everything inside the device safe area (notches, rounded corners, gesture bar).</summary>
        void ApplySafeArea()
        {
            lastSafe = Screen.safeArea;
            Rect s = SafeAreaOverride.Get();
            float scale = PanelScale();
            safe.style.left = s.xMin / scale;
            safe.style.right = (Screen.width - s.xMax) / scale;
            safe.style.top = (Screen.height - s.yMax) / scale;
            safe.style.bottom = s.yMin / scale;
        }

        float PanelScale()
        {
            // Panel is scaled to match a 1080 px wide reference.
            return Mathf.Max(0.01f, Screen.width / 1080f);
        }

        // ------------------------------------------------------------------ input

        public bool IsOverUI(Vector2 screenPos)
        {
            if (doc == null || root.panel == null) return false;
            if (ModalOpen) return true;
            Vector2 p = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(screenPos.x, Screen.height - screenPos.y));
            var picked = root.panel.Pick(p);
            return picked != null && picked.pickingMode == PickingMode.Position && picked != root && !(picked is TextElement && picked.parent == null);
        }

        public void SetAimHint(bool show, Vector2 screenStart, float radiusPx, bool cancelActive)
        {
            if (!show)
            {
                cancelRing.style.display = DisplayStyle.None;
                hintLabel.style.display = DisplayStyle.None;
                return;
            }
            float scale = PanelScale();
            Rect s = SafeAreaOverride.Get();
            float x = (screenStart.x - s.xMin) / scale;
            float y = (Screen.height - screenStart.y - (Screen.height - s.yMax)) / scale;
            float r = radiusPx / scale;
            cancelRing.style.display = DisplayStyle.Flex;
            cancelRing.style.left = x - r;
            cancelRing.style.top = y - r;
            cancelRing.style.width = r * 2f;
            cancelRing.style.height = r * 2f;
            if (cancelRing.Active != cancelActive)
            {
                cancelRing.Active = cancelActive;
                cancelRing.MarkDirtyRepaint();
            }
            hintLabel.style.display = cancelActive ? DisplayStyle.Flex : DisplayStyle.None;
            hintLabel.style.left = x;
            hintLabel.style.top = y + r + 18f;
        }

        // ------------------------------------------------------------------ HUD

        public void SetHudVisible(bool on)
        {
            hud.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void UpdateHud(RunState s, string holeName, int par, bool canUndo, bool surveying)
        {
            hudHole.text = string.Format("HOLE {0} / {1}  ·  PAR {2}", s.HoleIndex + 1, RunRules.HolesPerRun, par);
            hudName.text = holeName.ToUpperInvariant();
            int shown = Mathf.Max(0, s.Strokes);
            hudStrokes.text = shown.ToString();
            hudStrokes.style.color = shown <= 2 ? UiColors.Vermilion : UiColors.Ivory;
            pips.Set(shown, Mathf.Max(12, shown));
            hudSub.text = string.Format("This hole: {0}{1}", s.HoleStrokes, s.HolePenalties > 0 ? string.Format("  (incl. {0} penalty)", s.HolePenalties) : "");
            upgradeRow.Clear();
            foreach (var u in s.Upgrades)
            {
                var id = (UpgradeId)u;
                var slot = new DiamondSlot(84f) { Art = UpgradeArtIndex.Icon(cfg, id), Spent = id == UpgradeId.SecondChance && s.SecondChanceUsed };
                var art = new VisualElement();
                art.style.position = Position.Absolute;
                art.style.left = 18;
                art.style.top = 18;
                art.style.width = 48;
                art.style.height = 48;
                art.pickingMode = PickingMode.Ignore;
                var tex = UpgradeArtIndex.Icon(cfg, id);
                if (tex != null) art.style.backgroundImage = new StyleBackground(tex);
                if (slot.Spent) art.style.opacity = 0.3f;
                slot.Add(art);
                slot.style.marginRight = 6;
                slot.pickingMode = PickingMode.Ignore;
                upgradeRow.Add(slot);
            }
            undoBtn.style.display = canUndo ? DisplayStyle.Flex : DisplayStyle.None;
            surveyBtn.style.borderTopColor = surveying ? UiColors.Cyan : UiColors.Gold;
            surveyBtn.style.borderBottomColor = surveying ? UiColors.Cyan : UiColors.Gold;
            surveyBtn.style.borderLeftColor = surveying ? UiColors.Cyan : UiColors.Gold;
            surveyBtn.style.borderRightColor = surveying ? UiColors.Cyan : UiColors.Gold;
        }

        public void Toast(string head, string detail, Color headColor, float seconds = 2.6f)
        {
            toastHead.text = head;
            toastHead.style.color = headColor;
            toastDetail.text = detail;
            toast.AddToClassList("show");
            toastUntil = Time.unscaledTime + seconds;
        }

        // ------------------------------------------------------------------ modal screens

        public void CloseModal()
        {
            overlay.Clear();
            ModalOpen = false;
        }

        VisualElement Modal(bool dim)
        {
            overlay.Clear();
            ModalOpen = true;
            var c = new VisualElement();
            c.AddToClassList("center");
            if (dim) c.AddToClassList("dim-bg");
            overlay.Add(c);
            return c;
        }

        public void ShowTitle(bool canContinue, string continueInfo, Action onContinue, Action onNew, Action onSettings)
        {
            SetHudVisible(false);
            var c = Modal(false);
            c.style.justifyContent = Justify.SpaceBetween;
            c.style.paddingTop = 240;
            c.style.paddingBottom = 140;
            var head = new VisualElement { pickingMode = PickingMode.Ignore };
            head.style.alignItems = Align.Center;
            head.Add(T("BAD LIE", "logo"));
            var rule = new VisualElement();
            rule.AddToClassList("rule");
            rule.style.width = 520;
            head.Add(rule);
            head.Add(T("A GOLF ROGUELIKE OF THE FLOODED ESTATE", "kicker"));
            c.Add(head);
            var buttons = new VisualElement();
            buttons.style.alignItems = Align.Center;
            buttons.style.width = 760;
            if (canContinue)
            {
                var cont = Btn("CONTINUE", () => { CloseModal(); onContinue(); }, true);
                cont.style.width = 700;
                buttons.Add(cont);
                var info = B(continueInfo, "body", "dim");
                info.style.fontSize = 32;
                buttons.Add(info);
            }
            var nb = Btn("NEW RUN", () => { CloseModal(); onNew(); }, !canContinue);
            nb.style.width = 700;
            buttons.Add(nb);
            var sb = Btn("SETTINGS", onSettings);
            sb.style.width = 700;
            buttons.Add(sb);
            c.Add(buttons);
        }

        public void ShowHoleIntro(int index, string name, string subtitle, int par, int restore, IList<string> features, Action onStart)
        {
            var c = Modal(false);
            c.style.justifyContent = Justify.FlexEnd;
            c.style.paddingBottom = 260;
            var card = new VisualElement();
            card.AddToClassList("plate");
            card.AddToClassList("card");
            card.Add(T(string.Format("HOLE {0} OF {1}", index + 1, RunRules.HolesPerRun), "kicker"));
            card.Add(T(name.ToUpperInvariant(), "title"));
            var rule = new VisualElement();
            rule.AddToClassList("rule");
            card.Add(rule);
            if (!string.IsNullOrEmpty(subtitle)) card.Add(B(subtitle, "body"));
            var meta = B(string.Format("Par {0}  ·  Holing out restores +{1} strokes", par, restore), "body", "dim");
            meta.style.fontSize = 34;
            meta.style.marginTop = 10;
            card.Add(meta);
            if (features != null && features.Count > 0)
            {
                var f = B(string.Join("   ·   ", features), "body", "accent");
                f.style.fontSize = 32;
                f.style.marginTop = 10;
                card.Add(f);
            }
            var go = Btn("TEE OFF", () => { CloseModal(); onStart(); }, true);
            go.style.marginTop = 26;
            card.Add(go);
            c.Add(card);
        }

        public void ShowHoleComplete(ShotReport rep, RunState s, Action onContinue)
        {
            var c = Modal(true);
            var card = new VisualElement();
            card.AddToClassList("plate");
            card.AddToClassList("card");
            card.Add(T(rep.Headline, "title"));
            var rule = new VisualElement();
            rule.AddToClassList("rule");
            card.Add(rule);
            card.Add(B(rep.Detail, "body"));
            var left = B(string.Format("Strokes left: {0}", s.Strokes), "body", "accent");
            left.style.marginTop = 12;
            card.Add(left);
            var go = Btn(s.Phase == RunPhase.Won ? "SEE THE CARD" : "CHOOSE EQUIPMENT", () => { CloseModal(); onContinue(); }, true);
            go.style.marginTop = 26;
            card.Add(go);
            c.Add(card);
        }

        public void ShowUpgradeChoice(RunState s, Action<int> onChoose)
        {
            var c = Modal(true);
            c.style.justifyContent = Justify.Center;
            var col = new VisualElement();
            col.style.width = 960;
            col.Add(T("CHOOSE ONE", "title"));
            col.Add(B("Equipment found on the next tee. It lasts for the rest of the run.", "body", "dim"));
            int selected = -1;
            var cards = new List<VisualElement>();
            Button confirm = null;
            var owned = s.OwnedUpgrades();
            foreach (int idInt in s.Offer)
            {
                var id = (UpgradeId)idInt;
                var def = RunUpgrades.Get(id);
                var card = new VisualElement();
                card.AddToClassList("upgrade-card");
                var art = new VisualElement();
                art.AddToClassList("upgrade-art");
                art.pickingMode = PickingMode.Ignore;
                var tex = UpgradeArtIndex.Get(cfg, id);
                if (tex != null) art.style.backgroundImage = new StyleBackground(tex);
                card.Add(art);
                var text = new VisualElement();
                text.AddToClassList("upgrade-text");
                text.pickingMode = PickingMode.Ignore;
                text.Add(T(def.Name.ToUpperInvariant(), "upgrade-name"));
                text.Add(B(def.Effect, "upgrade-effect"));
                if (!string.IsNullOrEmpty(def.Tradeoff)) text.Add(B("Cost: " + def.Tradeoff, "upgrade-trade"));
                text.Add(B(def.Use, "upgrade-use"));
                foreach (var combo in RunUpgrades.Combos)
                {
                    bool pairs = (combo.A == id && RunUpgrades.Has(owned, combo.B)) || (combo.B == id && RunUpgrades.Has(owned, combo.A));
                    if (pairs) text.Add(B("Combo · " + combo.Name + ": " + combo.Effect, "combo"));
                }
                card.Add(text);
                int capture = idInt;
                card.RegisterCallback<ClickEvent>(e =>
                {
                    AudioDirector.UiTap();
                    selected = capture;
                    foreach (var other in cards) other.RemoveFromClassList("selected");
                    card.AddToClassList("selected");
                    confirm.text = "TAKE " + RunUpgrades.Get((UpgradeId)capture).Name.ToUpperInvariant();
                    confirm.SetEnabled(true);
                    confirm.style.opacity = 1f;
                });
                cards.Add(card);
                col.Add(card);
            }
            confirm = Btn("TAP A CARD", () =>
            {
                if (selected < 0) return;
                AudioDirector.UiSelect();
                CloseModal();
                onChoose(selected);
            }, true);
            confirm.style.marginTop = 20;
            confirm.SetEnabled(false);
            confirm.style.opacity = 0.5f;
            col.Add(confirm);
            c.Add(col);
        }

        public void ShowLastChance(string reason, Action onUndo, Action onEnd)
        {
            var c = Modal(true);
            var card = new VisualElement();
            card.AddToClassList("plate");
            card.AddToClassList("card");
            card.Add(T("OUT OF STROKES", "title"));
            var rule = new VisualElement();
            rule.AddToClassList("rule");
            card.Add(rule);
            card.Add(B(reason, "body"));
            card.Add(B("Second Chance can undo that shot once.", "body", "accent"));
            var undo = Btn("USE SECOND CHANCE", () => { CloseModal(); onUndo(); }, true);
            undo.style.marginTop = 22;
            card.Add(undo);
            card.Add(Btn("END THE RUN", () => { CloseModal(); onEnd(); }));
            c.Add(card);
        }

        public void ShowRunEnd(RunState s, bool won, Action onNew)
        {
            SetHudVisible(false);
            var c = Modal(true);
            var card = new VisualElement();
            card.AddToClassList("plate");
            card.AddToClassList("card");
            card.Add(T(won ? "THE ESTATE IS CLEARED" : "THE RUN ENDS HERE", "title"));
            var rule = new VisualElement();
            rule.AddToClassList("rule");
            card.Add(rule);
            card.Add(B(s.EndReason, won ? "body" : "body"));
            var table = new VisualElement();
            table.style.marginTop = 18;
            table.Add(ScoreRow("HOLE", "PAR", "STROKES", "RESTORED", true));
            foreach (var h in s.History)
            {
                table.Add(ScoreRow(h.Name, h.Par.ToString(), h.Strokes + (h.Penalties > 0 ? " (" + h.Penalties + "p)" : ""), h.Restored > 0 ? "+" + h.Restored : "—", false));
            }
            card.Add(table);
            var totals = B(string.Format("Total strokes {0}  ·  Strokes left {1}", s.TotalStrokes, Mathf.Max(0, s.Strokes)), "body", "dim");
            totals.style.marginTop = 14;
            totals.style.fontSize = 32;
            card.Add(totals);
            if (s.Upgrades.Count > 0)
            {
                var names = new List<string>();
                foreach (var u in s.Upgrades) names.Add(RunUpgrades.Get((UpgradeId)u).Name);
                var eq = B("Equipment: " + string.Join(", ", names), "body", "accent");
                eq.style.fontSize = 32;
                card.Add(eq);
                foreach (var combo in RunUpgrades.ActiveCombos(s.OwnedUpgrades()))
                {
                    var cb = B("Combo: " + combo.Name, "combo");
                    cb.style.unityTextAlign = TextAnchor.MiddleCenter;
                    card.Add(cb);
                }
            }
            var go = Btn("NEW RUN", () => { CloseModal(); onNew(); }, true);
            go.style.marginTop = 26;
            card.Add(go);
            c.Add(card);
        }

        VisualElement ScoreRow(string a, string b, string c, string d, bool header)
        {
            var row = new VisualElement();
            row.AddToClassList("score-row");
            row.pickingMode = PickingMode.Ignore;
            var cells = new[] { a, b, c, d };
            float[] widths = { 400, 110, 170, 170 };
            for (int i = 0; i < 4; i++)
            {
                var l = header ? T(cells[i], "score-cell", "dim") : B(cells[i], "score-cell");
                l.style.width = widths[i];
                l.style.unityTextAlign = i == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
                if (header) l.style.fontSize = 26;
                row.Add(l);
            }
            return row;
        }

        public void ShowSettings(bool inRun, Action onAbandon, Action onClose)
        {
            var c = Modal(true);
            var card = new VisualElement();
            card.AddToClassList("plate");
            card.AddToClassList("card");
            card.Add(T("SETTINGS", "title"));
            var rule = new VisualElement();
            rule.AddToClassList("rule");
            card.Add(rule);
            card.Add(SliderRow("SOUND", Settings.SoundVolume, v => Settings.SoundVolume = v));
            card.Add(SliderRow("MUSIC", Settings.MusicVolume, v => Settings.MusicVolume = v));
            Button left = null;
            left = Btn(Settings.LeftHanded ? "BUTTONS: LEFT-HANDED" : "BUTTONS: RIGHT-HANDED", () =>
            {
                Settings.LeftHanded = !Settings.LeftHanded;
                left.text = Settings.LeftHanded ? "BUTTONS: LEFT-HANDED" : "BUTTONS: RIGHT-HANDED";
                ApplyHandedness();
            });
            card.Add(left);
            if (inRun) card.Add(Btn("ABANDON RUN", () => { CloseModal(); onAbandon(); }));
            card.Add(Btn("CLOSE", () => { CloseModal(); onClose(); }, true));
            c.Add(card);
        }

        VisualElement SliderRow(string label, float value, Action<float> set)
        {
            var row = new VisualElement();
            row.AddToClassList("setting-row");
            row.Add(T(label, "setting-label"));
            var s = new SimpleSlider(value);
            s.Changed += v => set(v);
            row.Add(s);
            return row;
        }

        public void ApplyHandedness()
        {
            var bottom = surveyBtn.parent;
            bottom.style.flexDirection = Settings.LeftHanded ? FlexDirection.RowReverse : FlexDirection.Row;
        }
    }

    /// <summary>Safe area, with a command-line override (-safearea top,bottom px) for phone-notch tests.</summary>
    public static class SafeAreaOverride
    {
        static bool parsed;
        static Vector2 insets;

        public static Rect Get()
        {
            if (!parsed)
            {
                parsed = true;
                var args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (args[i] != "-safearea") continue;
                    var parts = args[i + 1].Split(',');
                    float t, b;
                    if (parts.Length == 2 && float.TryParse(parts[0], out t) && float.TryParse(parts[1], out b)) insets = new Vector2(t, b);
                }
            }
            Rect s = Screen.safeArea;
            if (insets != Vector2.zero) s = Rect.MinMaxRect(0f, insets.y, Screen.width, Screen.height - insets.x);
            return s;
        }
    }
}
