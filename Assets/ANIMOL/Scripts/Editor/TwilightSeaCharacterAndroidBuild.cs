using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Animol.Art.TwilightSea160
{
    public static class TwilightSeaCharacterAndroidBuild
    {
        // Validation build leaves the project's default Build Settings scene list untouched.
        public static void BuildBatch()
        {
            var args = Environment.GetCommandLineArgs();
            string output = null;
            for (var i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-twilightSeaBuildOutput") output = args[i + 1];
            if (string.IsNullOrEmpty(output)) throw new ArgumentException("Missing -twilightSeaBuildOutput");
            var previousBundle = EditorUserBuildSettings.buildAppBundle;
            try
            {
                EditorUserBuildSettings.buildAppBundle = false;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] {
                        "Assets/ANIMOL/Scenes/Prototype/ANIMOL_twilight_sea_Prototype.unity",
                        "Assets/ANIMOL/Scenes/QA/ANIMOL_TwilightCoast_Puzzle_QA.unity",
                        "Assets/ANIMOL/Scenes/QA/ANIMOL_TwilightSea_Art_QA.unity" },
                    locationPathName = output,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Twilight Sea Android build: " + report.summary.result +
                        ", errors=" + report.summary.totalErrors);
                UnityEngine.Debug.Log("Twilight Sea Android validation build succeeded: " + output +
                    ", bytes=" + report.summary.totalSize);
            }
            finally { EditorUserBuildSettings.buildAppBundle = previousBundle; }
        }
    }
}
