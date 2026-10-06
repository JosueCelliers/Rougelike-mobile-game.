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
                case "holes":
                    yield return Holes(Arg("-captureHoles") ?? "1,2,3,4,5");
                    break;
                case "lookall":
                    yield return LookAll(Arg("-captureHoles") ?? "1,2,3,4,5");
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

        /// <summary>
        /// Per-hole pass with the real game camera: the tee, the whole hole, then the bot plays
        /// the hole and the frame is taken wherever each shot comes to rest.
        /// </summary>
        IEnumerator Holes(string list)
        {
            var root = GameRoot.Instance;
            foreach (var part in list.Split(','))
            {
                int n;
                if (!int.TryParse(part.Trim(), out n)) continue;
                var s = root.LoadHoleForTools(n - 1, -1);
                s.SetReady();
                yield return Frames(45);
                yield return Shot("H" + n + "_1_tee");
                var rig = root.Rig;
                float fit = rig.WidthToFit(s.Course.Def.PlayBounds);
                Vector2 c = s.Course.Def.PlayBounds.center;
                rig.ScriptTo(new Vector3(c.x, 0f, c.y), fit, 0.01f);
                yield return Frames(30);
                yield return Shot("H" + n + "_2_overview");
                rig.SnapTo(s.Ball.ContactPosition, rig.PlayWidth);
                s.SetReady();
                yield return Frames(20);
                var planner = new BadLie.Bot.ShotPlanner(s.Course) { Mods = s.Mods };
                for (int k = 1; k <= 6; k++)
                {
                    float score;
                    var plan = planner.Plan(s.Lie, s.LieHeight, out score);
                    Log(string.Format("H{0} shot {1}: dir {2} power {3:F2} score {4:F1}", n, k, plan.Direction, plan.Power, score));
                    s.Shoot(plan);
                    yield return Frames(12);
                    if (k == 1) yield return Shot("H" + n + "_3_flight");
                    yield return WaitForShot(s, 25f);
                    yield return Frames(40);
                    if (s.LastShot != null && s.LastShot.Outcome == ShotOutcome.Holed)
                    {
                        yield return Shot("H" + n + "_9_holed");
                        break;
                    }
                    // No run controller in tool mode: move the lie here (hazards replay from the old lie).
                    if (s.LastShot != null && !s.LastShot.IsHazard) s.PlaceBall(s.LastShot.FinalPosition, s.LastShot.FinalHeight);
                    else s.PlaceBall(s.Lie, s.LieHeight);
                    s.SetReady();
                    yield return Frames(10);
                    yield return Shot("H" + n + "_" + (3 + k) + "_lie");
                }
            }
        }

        /// <summary>Look-dev pass over several holes: the tee, then an approach about nine metres
        /// (walking distance) short of the cup, both with the real game camera.</summary>
        IEnumerator LookAll(string list)
        {
            var root = GameRoot.Instance;
            foreach (var part in list.Split(','))
            {
                int n;
                if (!int.TryParse(part.Trim(), out n)) continue;
                var s = root.LoadHoleForTools(n - 1, -1);
                s.SetReady();
                yield return Frames(40);
                yield return Shot("A" + n + "_tee");
                Vector2 approach;
                float h;
                if (FindApproach(s.Course, 9f, out approach, out h))
                {
                    s.PlaceBall(approach, h);
                    root.Rig.SnapTo(s.Ball.ContactPosition, root.Rig.PlayWidth);
                    s.SetReady();
                    yield return Frames(30);
                    yield return Shot("A" + n + "_approach");
                }
            }
        }

        static bool FindApproach(BadLie.Course.CourseModel course, float walk, out Vector2 best, out float height)
        {
            var field = new BadLie.Bot.DistanceField(course);
            best = course.Tee;
            height = course.TeeHeight;
            float bestErr = float.MaxValue;
            Rect b = course.Def.PlayBounds;
            for (float z = b.yMin; z <= b.yMax; z += 0.5f)
            {
                for (float x = b.xMin; x <= b.xMax; x += 0.5f)
                {
                    var p = new Vector2(x, z);
                    var g = course.Ground(p);
                    if (g.IsVoid || g.Water || g.OutOfBounds) continue;
                    if (g.Surface != BadLie.Course.SurfaceType.Fairway && g.Surface != BadLie.Course.SurfaceType.Stone) continue;
                    float d = field.Distance(p);
                    if (d == float.MaxValue) continue;
                    // Prefer points roughly in line between tee and cup.
                    float err = Mathf.Abs(d - walk) + 0.15f * Mathf.Abs(Geo2DCross(course.Tee, course.Cup, p));
                    if (err < bestErr)
                    {
                        bestErr = err;
                        best = p;
                        height = g.Height;
                    }
                }
            }
            return bestErr < float.MaxValue;
        }

        static float Geo2DCross(Vector2 a, Vector2 b, Vector2 p)
        {
            Vector2 ab = (b - a).normalized;
            Vector2 ap = p - a;
            return ab.x * ap.y - ab.y * ap.x;
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
