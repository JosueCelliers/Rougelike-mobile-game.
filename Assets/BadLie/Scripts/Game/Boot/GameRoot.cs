using BadLie.Course;
using BadLie.Holes;
using BadLie.Sim;
using UnityEngine;

namespace BadLie.Game
{
    /// <summary>Composition root: creates services (UI, audio, effects) and the run controller.</summary>
    public sealed class GameRoot : MonoBehaviour
    {
        public GameConfig Config;
        public Camera Cam;
        public Atmosphere Atmos;

        public CameraRig Rig { get; private set; }
        public RunController Run { get; private set; }
        public GameUI UI { get; private set; }
        public static GameRoot Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            PointerInput.Enable();
            Rig = Cam.GetComponent<CameraRig>();
            if (Rig == null) Rig = Cam.gameObject.AddComponent<CameraRig>();
            Rig.Cam = Cam;
            AudioDirector.Init(Config, transform);
            FxDirector.Init(Config, transform);
            UI = GameUI.Create(Config, transform);
            UI.ApplyHandedness();
            Run = gameObject.AddComponent<RunController>();
            Run.Init(this, Config, UI);
        }

        void Start()
        {
            if (CaptureDirector.Requested)
            {
                gameObject.AddComponent<CaptureDirector>();
                return;
            }
            Run.ShowTitle();
        }

        /// <summary>Loads a hole outside of a run (used by capture scenarios and tools).</summary>
        public HoleSession LoadHoleForTools(int index, int cupVariant)
        {
            var existing = FindObjectsByType<HoleSession>(FindObjectsSortMode.None);
            foreach (var e in existing) Destroy(e.gameObject);
            var def = HoleLibrary.Get(index);
            var course = new CourseModel(def, SimTuning.Default, cupVariant);
            var s = HoleSession.Create(transform, Config, Rig, course);
            s.PlaceBall(course.Tee, course.TeeHeight);
            Rig.SnapTo(s.Ball.ContactPosition, Rig.PlayWidth);
            FxDirector.SetupAmbient(def.PlayBounds, 0f);
            return s;
        }
    }
}
