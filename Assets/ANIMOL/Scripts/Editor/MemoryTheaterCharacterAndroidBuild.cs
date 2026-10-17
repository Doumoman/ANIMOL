using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Animol.Art.MemoryTheater160
{
    public static class MemoryTheaterCharacterAndroidBuild
    {
        // Validation build leaves the project's default Build Settings scene list untouched.
        public static void BuildBatch()
        {
            var args = Environment.GetCommandLineArgs();
            string output = null;
            for (var i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-memoryTheaterBuildOutput") output = args[i + 1];
            if (string.IsNullOrEmpty(output)) throw new ArgumentException("Missing -memoryTheaterBuildOutput");
            var previousBundle = EditorUserBuildSettings.buildAppBundle;
            try
            {
                EditorUserBuildSettings.buildAppBundle = false;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] {
                        "Assets/ANIMOL/Scenes/QA/ANIMOL_MemoryTheater_Puzzle_QA.unity",
                        "Assets/ANIMOL/Scenes/Prototype/ANIMOL_memory_theater_Prototype.unity",
                        "Assets/ANIMOL/Scenes/QA/ANIMOL_MemoryTheater_Character_QA.unity" },
                    locationPathName = output,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Memory Theater Android build: " + report.summary.result +
                        ", errors=" + report.summary.totalErrors);
                UnityEngine.Debug.Log("Memory Theater Android validation build succeeded: " + output +
                    ", bytes=" + report.summary.totalSize);
            }
            finally { EditorUserBuildSettings.buildAppBundle = previousBundle; }
        }
    }
}
