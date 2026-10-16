using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Animol.Art.LetterObservatory160
{
    public static class LetterObservatoryCharacterAndroidBuild
    {
        // Validation build leaves the project's default Build Settings scene list untouched.
        public static void BuildBatch()
        {
            var args = Environment.GetCommandLineArgs();
            string output = null;
            for (var i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-letterObservatoryBuildOutput") output = args[i + 1];
            if (string.IsNullOrEmpty(output)) throw new ArgumentException("Missing -letterObservatoryBuildOutput");
            var previousBundle = EditorUserBuildSettings.buildAppBundle;
            try
            {
                EditorUserBuildSettings.buildAppBundle = false;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] {
                        "Assets/ANIMOL/Scenes/ANIMOL_letter_observatory_Prototype.unity",
                        "Assets/ANIMOL/Scenes/ANIMOL_LetterObservatory_Art_QA.unity" },
                    locationPathName = output,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Letter Observatory Android build: " +
                        report.summary.result + ", errors=" + report.summary.totalErrors);
                UnityEngine.Debug.Log("Letter Observatory Android validation build succeeded: " + output +
                    ", bytes=" + report.summary.totalSize);
            }
            finally { EditorUserBuildSettings.buildAppBundle = previousBundle; }
        }
    }
}
