using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BadLie.Sim;
using UnityEngine;

namespace BadLie.Game
{
    /// <summary>
    /// Scripted capture mode for real gameplay screenshots from a player build:
    ///   BADLIE.x86_64 -capture hole1 -captureOut /path -screen-width 1080 -screen-height 2340
    /// It drives the game through injected pointer input with a fixed time step, so frames
    /// are reproducible, then quits. Nothing here alters gameplay rules.
    /// </summary>
    public sealed class CaptureDirector : MonoBehaviour
    {
        public static bool Requested { get { return Arg("-capture") != null; } }

        string outDir;
        readonly List<string> log = new List<string>();

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }

        IEnumerator Start()
        {
            outDir = Arg("-captureOut") ?? Path.Combine(Application.persistentDataPath, "captures");
            Directory.CreateDirectory(outDir);
            Time.captureDeltaTime = 1f / 30f;
            PointerInput.Scripted = true;
            Cam.enabled = false;
            string scenario = Arg("-capture");
            Log("scenario " + scenario + " screen " + Screen.width + "x" + Screen.height);
            yield return null;
            switch (scenario)
            {
                case "look":
                    yield return Look();
                    break;
                case "flow":
                    yield return Flow();
                    break;
                default:
                    yield return Hole1();
                    break;
            }
            File.WriteAllLines(Path.Combine(outDir, "capture_log.txt"), log.ToArray());
            Application.Quit();
        }

        void Log(string s)
        {
            log.Add(string.Format("[{0:F2}] {1}", Time.time, s));
            Debug.Log("[Capture] " + s);
        }

        Camera Cam { get { return GameRoot.Instance.Cam; } }

        /// <summary>
        /// Software rendering is slow, so the camera only renders frames that become
        /// screenshots; game time still advances at a fixed step in between.
        /// </summary>
        IEnumerator Shot(string name)
        {
            float t0 = Time.realtimeSinceStartup;
            Cam.enabled = true;
            yield return null;
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
            Destroy(tex);
            Cam.enabled = false;
            Log(string.Format("shot {0} ({1:F1}s)", name, Time.realtimeSinceStartup - t0));
        }

        IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++) yield return null;
        }

        /// <summary>Drag from a to b over the given frames; release if requested.</summary>
        IEnumerator Drag(Vector2 a, Vector2 b, int frames, bool release)
        {
            PointerInput.ScriptedFrame = new PointerFrame { PrimaryDown = true, PrimaryHeld = true, PrimaryPos = a, TouchCount = 1 };
            yield return null;
            for (int i = 1; i <= frames; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)frames);
                PointerInput.ScriptedFrame = new PointerFrame { PrimaryHeld = true, PrimaryPos = p, TouchCount = 1 };
                yield return null;
            }
            if (release)
            {
                PointerInput.ScriptedFrame = new PointerFrame { PrimaryUp = true, PrimaryHeld = false, PrimaryPos = b, TouchCount = 0 };
                yield return null;
                PointerInput.ScriptedFrame = new PointerFrame();
            }
        }

        IEnumerator WaitForShot(HoleSession s, float maxSeconds)
        {
            float t = 0f;
            while (s.Current == HoleSession.Phase.Flight && t < maxSeconds)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// Full loop with the real UI: title, hole card, HUD, aiming and cancelling, a penalty,
        /// a bot-played hole out, the upgrade choice, a resumed run, and a failed run's verdict.
        /// </summary>
        IEnumerator Flow()
        {
            var root = GameRoot.Instance;
            var run = root.Run;
            SaveSystem.ClearRun();
            run.ShowTitle();
            yield return Frames(60);
            yield return Shot("F01_title");

            run.NewRun(20261005);
            yield return Frames(70);
            yield return Shot("F02_hole_intro");

            run.ToolBeginPlay();
            yield return Frames(45);
            yield return Shot("F03_hud");

            float w = Screen.width, h = Screen.height;
            Vector2 start = new Vector2(w * 0.55f, h * 0.24f);
            yield return Drag(start, start + new Vector2(-w * 0.05f, -h * 0.12f), 10, false);
            yield return Frames(3);
            yield return Shot("F04_aiming");
            yield return Drag(start + new Vector2(-w * 0.05f, -h * 0.12f), start + new Vector2(2f, 3f), 6, false);
            yield return Frames(3);
            yield return Shot("F05_cancel_zone");
            PointerInput.ScriptedFrame = new PointerFrame { PrimaryUp = true, PrimaryPos = start };
            yield return null;
            PointerInput.ScriptedFrame = new PointerFrame();
            yield return Frames(10);
            Log("cancelled: strokes still " + run.State.Strokes);

            // Play the hole with the bot until it is holed (same shot path as the aim gesture).
            var s = run.Session;
            var planner = new BadLie.Bot.ShotPlanner(s.Course) { Mods = s.Mods };
            int guard = 0;
            while (run.State.Phase == BadLie.Run.RunPhase.Playing && guard++ < 10)
            {
                float score;
                var plan = planner.Plan(s.Lie, s.LieHeight, out score);
                // Show the real aim visuals for this planned shot before releasing it.
                Log(string.Format("bot shot {0}: dir {1} power {2:F2} score {3:F1}", guard, plan.Direction, plan.Power, score));
                s.Shoot(plan);
                yield return Frames(14);
                if (guard == 1) yield return Shot("F06_ball_rolling");
                yield return WaitForShot(s, 25f);
                yield return Frames(30);
            }
            yield return Frames(40);
            yield return Shot("F07_hole_complete");
            run.ToolShowChoice();
            yield return Frames(20);
            yield return Shot("F08_upgrade_choice");
            run.ToolChoose(run.State.Offer[0]);
            yield return Frames(70);
            yield return Shot("F09_next_hole_intro");

            // Resume an interrupted run from the save file.
            run.ToolContinue();
            yield return Frames(50);
            yield return Shot("F10_resumed");

            // Force a final, missed stroke to show the failure verdict.
            run.ToolBeginPlay();
            yield return Frames(20);
            run.State.Strokes = 1;
            run.Session.Shoot(new BadLie.Sim.ShotInput(new Vector2(-1f, 0.2f), 1f));
            yield return WaitForShot(run.Session, 25f);
            yield return Frames(80);
            yield return Shot("F11_run_lost");
            Log("end reason: " + run.State.EndReason);
        }

        /// <summary>Quick look-dev pass: tee view, approach view, whole-hole view.</summary>
        IEnumerator Look()
        {
            var root = GameRoot.Instance;
            var s = root.LoadHoleForTools(0, -1);
            s.SetReady();
            yield return Frames(40);
            yield return Shot("L1_tee");
            s.PlaceBall(new Vector2(4.2f, 16.8f), 0f);
            root.Rig.SnapTo(s.Ball.ContactPosition, root.Rig.PlayWidth);
            s.SetReady();
            yield return Frames(30);
            yield return Shot("L2_approach");
            var rig = root.Rig;
            float fit = rig.WidthToFit(s.Course.Def.PlayBounds);
            Vector2 c = s.Course.Def.PlayBounds.center;
            rig.ScriptTo(new Vector3(c.x, 0f, c.y), fit, 0.01f);
            yield return Frames(30);
            yield return Shot("L3_overview");
        }

        IEnumerator Hole1()
        {
            var root = GameRoot.Instance;
            var s = root.LoadHoleForTools(0, -1);
            s.SetReady();
            yield return Frames(45);
            yield return Shot("01_tee");

            // Whole-hole view.
            var rig = root.Rig;
            float fit = rig.WidthToFit(s.Course.Def.PlayBounds);
            Vector2 c = s.Course.Def.PlayBounds.center;
            rig.ScriptTo(new Vector3(c.x, 0f, c.y), fit, 0.01f);
            yield return Frames(30);
            yield return Shot("02_overview");
            s.SetReady();
            yield return Frames(40);

            // Aim: pull back and slightly right to send the ball up the fairway.
            float w = Screen.width, h = Screen.height;
            Vector2 start = new Vector2(w * 0.5f, h * 0.30f);
            Vector2 pull = new Vector2(w * 0.06f, -h * 0.13f);
            yield return Drag(start, start + pull, 12, false);
            yield return Frames(4);
            yield return Shot("03_aiming");
            Log(string.Format("aim dir {0} power {1:F2}", s.Aim.Direction, s.Aim.Power));
            PointerInput.ScriptedFrame = new PointerFrame { PrimaryUp = true, PrimaryPos = start + pull };
            yield return null;
            PointerInput.ScriptedFrame = new PointerFrame();
            yield return Frames(18);
            yield return Shot("04_rolling");
            yield return WaitForShot(s, 20f);
            yield return Frames(40);
            yield return Shot("05_rest");
            Log("ball at " + s.Ball.ContactPosition + " outcome " + (s.LastShot != null ? s.LastShot.Outcome.ToString() : "none"));
        }
    }
}
