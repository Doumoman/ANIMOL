using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Animol.Art.Starway160
{
    public static class StarwayCharacterAndroidBuild
    {
        // Validation build keeps the project's default Build Settings scene list untouched.
        public static void BuildBatch()
        {
            var args = Environment.GetCommandLineArgs();
            string output = null;
            for (var i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-starwayBuildOutput") output = args[i + 1];
            if (string.IsNullOrEmpty(output)) throw new ArgumentException("Missing -starwayBuildOutput");
            var previousBundle = EditorUserBuildSettings.buildAppBundle;
            try
            {
                EditorUserBuildSettings.buildAppBundle = false;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] {
                        "Assets/ANIMOL/Scenes/Runtime/ANIMOL_Puzzle.unity",
                        "Assets/ANIMOL/Scenes/Prototype/ANIMOL_starway_Prototype.unity",
                        "Assets/ANIMOL/Scenes/QA/ANIMOL_Starway_Character_QA.unity" },
                    locationPathName = output,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Starway Android build: " + report.summary.result +
                        ", errors=" + report.summary.totalErrors);
                UnityEngine.Debug.Log("Starway Android validation build succeeded: " + output +
                    ", bytes=" + report.summary.totalSize);
            }
            finally { EditorUserBuildSettings.buildAppBundle = previousBundle; }
        }
    }
}
