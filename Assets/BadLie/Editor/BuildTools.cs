using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BadLie.EditorTools
{
    /// <summary>Command-line builds. Example:
    /// Unity -batchmode -quit -projectPath . -executeMethod BadLie.EditorTools.BuildTools.BuildLinux</summary>
    public static class BuildTools
    {
        static string[] Scenes { get { return new[] { ProjectSetup.ScenePath }; } }

        [MenuItem("BAD LIE/Build/Linux (capture)")]
        public static void BuildLinux()
        {
            Build(BuildTarget.StandaloneLinux64, "Builds/Linux/BADLIE.x86_64", BuildOptions.Development);
        }

        [MenuItem("BAD LIE/Build/Android APK")]
        public static void BuildAndroid()
        {
            EditorUserBuildSettings.buildAppBundle = false;
            Build(BuildTarget.Android, "Builds/Android/BADLIE.apk", BuildOptions.None);
        }

        static void Build(BuildTarget target, string path, BuildOptions options)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = path,
                target = target,
                options = options,
            });
            var s = report.summary;
            Debug.Log(string.Format("[BadLie] Build {0}: {1} errors={2} size={3:F1}MB time={4}", target, s.result, s.totalErrors, s.totalSize / (1024f * 1024f), s.totalTime));
            if (s.result != BuildResult.Succeeded && Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
