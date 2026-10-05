using System;
using System.Collections;
using System.Collections.Generic;
using BadLie.Course;
using BadLie.Holes;
using BadLie.Run;
using BadLie.Sim;
using UnityEngine;

namespace BadLie.Game
{
    /// <summary>
    /// Runs the loop: title → hole intro → play → (hole complete → choose upgrade)* → won/lost →
    /// new run. Owns the RunState, saves after every change and resumes interrupted runs,
    /// including a shot that was still rolling when the app closed.
    /// </summary>
    public sealed class RunController : MonoBehaviour
    {
        GameRoot root;
        GameConfig cfg;
        GameUI ui;
        public RunState State { get; private set; }
        public HoleSession Session { get; private set; }
        HoleDef def;
        bool surveying;
        bool introCamera;

        public GameUI UI { get { return ui; } }

        public void Init(GameRoot r, GameConfig c, GameUI u)
        {
            root = r;
            cfg = c;
            ui = u;
            ui.SurveyToggled += ToggleSurvey;
            ui.UndoPressed += UseSecondChance;
            ui.SettingsPressed += OpenSettings;
        }

        // ------------------------------------------------------------------ title

        public void ShowTitle()
        {
            var saved = SaveSystem.LoadRun();
            bool canContinue = saved != null && !saved.Finished;
            string info = canContinue ? string.Format("Hole {0} of {1} · {2} strokes left", saved.HoleIndex + 1, RunRules.HolesPerRun, Mathf.Max(0, saved.Strokes)) : "";
            // The opening hole drifts behind the title.
            LoadHole(0, -1, false);
            Session.SetIdle();
            var fit = root.Rig.WidthToFit(Session.Course.Def.PlayBounds) * 0.85f;
            Vector2 c = Session.Course.Def.PlayBounds.center;
            root.Rig.SnapTo(new Vector3(c.x, 0f, c.y - 6f), fit);
            root.Rig.ScriptTo(new Vector3(c.x, 0f, c.y + 4f), fit * 0.9f, 6f);
            AudioDirector.MusicLevel(1f);
            ui.ShowTitle(canContinue, info, () => Continue(saved), () => NewRun(NewSeed()), () => OpenSettingsFromTitle());
        }

        static long NewSeed()
        {
            return DateTime.UtcNow.Ticks ^ 0x5DEECE66DL;
        }

        void OpenSettingsFromTitle()
        {
            ui.ShowSettings(false, null, ShowTitle);
        }

        // ------------------------------------------------------------------ run lifecycle

        public void NewRun(long seed)
        {
            var alts = new List<int>();
            for (int i = 0; i < RunRules.HolesPerRun; i++) alts.Add(HoleLibrary.Get(i).AltCups.Count);
            State = RunRules.NewRun(seed, alts);
            Save();
            StartHoleIntro();
        }

        void Continue(RunState saved)
        {
            State = saved;
            switch (State.Phase)
            {
                case RunPhase.HoleIntro:
                    StartHoleIntro();
                    break;
                case RunPhase.ChoosingUpgrade:
                    LoadCurrentHole(false);
                    Session.SetIdle();
                    ShowUpgradeChoice();
                    break;
                case RunPhase.Won:
                case RunPhase.Lost:
                    ui.ShowRunEnd(State, State.Phase == RunPhase.Won, () => NewRun(NewSeed()));
                    break;
                default:
                    LoadCurrentHole(true);
                    if (State.PendingShot) ResolveInterruptedShot();
                    else if (State.Phase == RunPhase.LastChance) ui.ShowLastChance(State.EndReason, UseSecondChance, EndRunNow);
                    else ReadyToPlay();
                    break;
            }
        }

        /// <summary>
        /// The app closed mid-shot: the stroke was already spent and saved, so replay the same
        /// input through the deterministic simulation and apply its result.
        /// </summary>
        void ResolveInterruptedShot()
        {
            var input = RunRules.PendingInput(State);
            var result = Session.Simulate(input, SimOptions.Full);
            ui.Toast("SHOT RESUMED", "Your last shot was replayed exactly where it ended.", UiColors.Cyan, 2.4f);
            ApplyResult(result, true);
        }

        void StartHoleIntro()
        {
            State.Phase = RunPhase.HoleIntro;
            Save();
            LoadCurrentHole(false);
            Session.SetIdle();
            ui.SetHudVisible(false);
            var rig = root.Rig;
            float fit = rig.WidthToFit(def.PlayBounds);
            Vector2 c = def.PlayBounds.center;
            rig.SnapTo(new Vector3(c.x, 0f, c.y), fit);
            rig.ScriptTo(new Vector3(c.x, 0f, c.y), fit * 0.92f, 3f);
            introCamera = true;
            AudioDirector.MusicLevel(1f);
            ui.ShowHoleIntro(State.HoleIndex, def.Name, def.Subtitle, def.Par, RunRules.RestorePerHole, def.Features, BeginPlay);
        }

        void BeginPlay()
        {
            introCamera = false;
            RunRules.BeginHole(State, Session.Course.Tee, Session.Course.TeeHeight);
            Save();
            Session.PlaceBall(Session.Course.Tee, Session.Course.TeeHeight);
            ReadyToPlay();
            root.Rig.FrameBall(Session.Ball.ContactPosition, 0.9f);
        }

        void ReadyToPlay()
        {
            ui.SetHudVisible(true);
            Session.Mods = RunUpgrades.ModifiersFor(State.OwnedUpgrades());
            Session.PlaceBall(new Vector2(State.LieX, State.LieZ), State.LieY);
            Session.SetReady();
            RefreshHud();
        }

        void LoadCurrentHole(bool placeAtLie)
        {
            int libIndex = State.HoleOrder[State.HoleIndex];
            int cupVariant = State.CupVariants[State.HoleIndex];
            LoadHole(libIndex, cupVariant, true);
            Session.Mods = RunUpgrades.ModifiersFor(State.OwnedUpgrades());
            if (placeAtLie && State.HasLie) Session.PlaceBall(new Vector2(State.LieX, State.LieZ), State.LieY);
        }

        void LoadHole(int libIndex, int cupVariant, bool wire)
        {
            if (Session != null) Destroy(Session.gameObject);
            def = HoleLibrary.Get(libIndex);
            var course = new CourseModel(def, SimTuning.Default, cupVariant);
            Session = HoleSession.Create(root.transform, cfg, root.Rig, course);
            Session.PlaceBall(course.Tee, course.TeeHeight);
            root.Rig.SnapTo(Session.Ball.ContactPosition, root.Rig.PlayWidth);
            FxDirector.SetupAmbient(def.PlayBounds, 0f);
            if (!wire) return;
            Session.OverUI = p => ui.IsOverUI(p);
            Session.ShotAllowed = () => State != null && RunRules.CanShoot(State) && !ui.ModalOpen;
            Session.ShotReleased += OnShotReleased;
            Session.ShotFinished += r => ApplyResult(r, false);
            Session.BallEvent += e =>
            {
                AudioDirector.BallEvent(e);
                FxDirector.BallEvent(e);
            };
            Session.AimBegan += () => AudioDirector.MusicLevel(0.75f);
            Session.AimCancelled += () => { AudioDirector.MusicLevel(1f); AudioDirector.UiTap(); };
        }

        // ------------------------------------------------------------------ shots

        void OnShotReleased(ShotInput input, SimResult result)
        {
            RunRules.ReleaseShot(State, input);
            Save();
            AudioDirector.Strike(input.Power);
            AudioDirector.MusicLevel(1f);
            RefreshHud();
        }

        void ApplyResult(SimResult r, bool resumed)
        {
            var rep = RunRules.ResolveShot(State, r, def.Par, def.Name, Session.Course.Cup);
            Save();
            RefreshHud();
            if (rep.Holed)
            {
                if (resumed) FxDirector.BallEvent(new SimEvent { Type = SimEventType.Holed, Position = new Vector3(Session.Course.Cup.x, Session.Course.CupHeight, Session.Course.Cup.y) });
                StartCoroutine(AfterHoled(rep));
                return;
            }
            if (rep.Hazard)
            {
                AudioDirector.Penalty();
                ui.Toast(rep.Headline, rep.Detail, UiColors.Vermilion);
            }
            Session.PlaceBall(new Vector2(State.LieX, State.LieZ), State.LieY);
            if (rep.LastChance)
            {
                ui.ShowLastChance(State.EndReason, UseSecondChance, EndRunNow);
                return;
            }
            if (rep.RunLost)
            {
                StartCoroutine(AfterLost());
                return;
            }
            Session.SetReady();
            RefreshHud();
        }

        IEnumerator AfterHoled(ShotReport rep)
        {
            AudioDirector.HoleComplete();
            Vector3 cup = new Vector3(Session.Course.Cup.x, Session.Course.CupHeight, Session.Course.Cup.y);
            root.Rig.ScriptTo(cup - root.Rig.ForwardFlat * 2.5f, root.Rig.PlayWidth * 0.8f, 0.8f);
            yield return new WaitForSeconds(0.9f);
            AudioDirector.MusicLevel(0.6f);
            ui.ShowHoleComplete(rep, State, () =>
            {
                if (State.Phase == RunPhase.Won)
                {
                    AudioDirector.RunWon();
                    SaveSystem.SaveRun(State);
                    ui.ShowRunEnd(State, true, () => NewRun(NewSeed()));
                }
                else ShowUpgradeChoice();
            });
        }

        IEnumerator AfterLost()
        {
            yield return new WaitForSeconds(1.2f);
            AudioDirector.RunLost();
            ui.ShowRunEnd(State, false, () => NewRun(NewSeed()));
        }

        void ShowUpgradeChoice()
        {
            ui.SetHudVisible(false);
            ui.ShowUpgradeChoice(State, id =>
            {
                RunRules.Choose(State, id);
                Save();
                StartHoleIntro();
            });
        }

        void UseSecondChance()
        {
            if (!RunRules.CanUndo(State)) return;
            RunRules.Undo(State);
            Save();
            ui.Toast("SECOND CHANCE", "The last shot never happened. Stroke refunded.", UiColors.Cyan);
            ui.CloseModal();
            ReadyToPlay();
        }

        void EndRunNow()
        {
            RunRules.Lose(State, def.Name, def.Par);
            Save();
            AudioDirector.RunLost();
            ui.ShowRunEnd(State, false, () => NewRun(NewSeed()));
        }

        void ToggleSurvey()
        {
            if (Session == null || Session.Current != HoleSession.Phase.Ready) return;
            surveying = !surveying;
            Session.SetSurvey(surveying);
            RefreshHud();
        }

        void OpenSettings()
        {
            ui.ShowSettings(true, AbandonRun, () => { });
        }

        void AbandonRun()
        {
            SaveSystem.ClearRun();
            State = null;
            ShowTitle();
        }

        void RefreshHud()
        {
            if (State == null || def == null) return;
            ui.UpdateHud(State, def.Name, def.Par, RunRules.CanUndo(State) && Session != null && Session.Current == HoleSession.Phase.Ready, surveying);
        }

        void Save()
        {
            if (State != null) SaveSystem.SaveRun(State);
        }

        // ------------------------------------------------------------------ tool hooks
        // Used by capture scenarios and automated playthroughs; they call the same paths as UI.

        public void ToolBeginPlay()
        {
            ui.CloseModal();
            BeginPlay();
        }

        public void ToolChoose(int upgradeId)
        {
            ui.CloseModal();
            RunRules.Choose(State, upgradeId);
            Save();
            StartHoleIntro();
        }

        public void ToolShowChoice()
        {
            ui.CloseModal();
            ShowUpgradeChoice();
        }

        public void ToolContinue()
        {
            ui.CloseModal();
            Continue(SaveSystem.LoadRun());
        }

        void Update()
        {
            if (Session == null) return;
            if (introCamera) return;
            var aim = Session.Aim;
            if (aim.Current == AimController.State.Aiming)
                ui.SetAimHint(true, aim.StartScreen, aim.DeadZonePx, aim.InCancelZone);
            else
                ui.SetAimHint(false, Vector2.zero, 0f, false);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        void OnApplicationQuit()
        {
            Save();
        }
    }
}
