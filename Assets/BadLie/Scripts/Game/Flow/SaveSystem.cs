using System;
using System.IO;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using BadLie.Run;
using UnityEngine;

namespace BadLie.Game
{
    /// <summary>Run persistence (JSON file, written atomically) and player settings.</summary>
    public static class SaveSystem
    {
        static string Dir { get { return Application.persistentDataPath; } }
        static string RunPath { get { return Path.Combine(Dir, "run.json"); } }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void BadLie_SyncFS();
        /// <summary>In the browser, flush the file system to IndexedDB so the run survives a reload.</summary>
        static void Flush() { BadLie_SyncFS(); }
#else
        static void Flush() { }
#endif

        public static void SaveRun(RunState s)
        {
            try
            {
                string tmp = RunPath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(s));
                if (File.Exists(RunPath)) File.Delete(RunPath);
                File.Move(tmp, RunPath);
                Flush();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BadLie] Could not save run: " + e.Message);
            }
        }

        public static RunState LoadRun()
        {
            try
            {
                if (!File.Exists(RunPath)) return null;
                var s = JsonUtility.FromJson<RunState>(File.ReadAllText(RunPath));
                if (s == null || s.Version != 1 || s.HoleOrder == null || s.HoleOrder.Length == 0) return null;
                return s;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BadLie] Could not load run: " + e.Message);
                return null;
            }
        }

        public static bool HasResumableRun()
        {
            var s = LoadRun();
            return s != null && !s.Finished;
        }

        public static void ClearRun()
        {
            try
            {
                if (File.Exists(RunPath)) File.Delete(RunPath);
                Flush();
            }
            catch (Exception) { }
        }
    }

    /// <summary>Player settings kept in PlayerPrefs.</summary>
    public static class Settings
    {
        public static event Action Changed;

        public static float SoundVolume
        {
            get { return PlayerPrefs.GetFloat("bl.sound", 0.9f); }
            set { PlayerPrefs.SetFloat("bl.sound", Mathf.Clamp01(value)); PlayerPrefs.Save(); if (Changed != null) Changed(); }
        }

        public static float MusicVolume
        {
            get { return PlayerPrefs.GetFloat("bl.music", 0.6f); }
            set { PlayerPrefs.SetFloat("bl.music", Mathf.Clamp01(value)); PlayerPrefs.Save(); if (Changed != null) Changed(); }
        }

        public static bool Haptics
        {
            get { return PlayerPrefs.GetInt("bl.haptics", 1) == 1; }
            set { PlayerPrefs.SetInt("bl.haptics", value ? 1 : 0); PlayerPrefs.Save(); if (Changed != null) Changed(); }
        }

        public static bool LeftHanded
        {
            get { return PlayerPrefs.GetInt("bl.left", 0) == 1; }
            set { PlayerPrefs.SetInt("bl.left", value ? 1 : 0); PlayerPrefs.Save(); if (Changed != null) Changed(); }
        }
    }
}
