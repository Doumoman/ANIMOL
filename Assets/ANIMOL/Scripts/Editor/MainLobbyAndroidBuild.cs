using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace ANIMOL.Menu.Editor
{
    public static class MainLobbyAndroidBuild
    {
        public static void BuildBatch()
        {
            var args = Environment.GetCommandLineArgs();
            string output = null;
            for (var i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-mainLobbyBuildOutput") output = args[i + 1];
            if (string.IsNullOrEmpty(output)) throw new ArgumentException("Missing -mainLobbyBuildOutput");
            var previousBundle = EditorUserBuildSettings.buildAppBundle;
            try
            {
                EditorUserBuildSettings.buildAppBundle = false;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/WilliamNotebookUI/Generated/NotebookUI.unity" },
                    locationPathName = output,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Notebook UI Android build: " + report.summary.result +
                        ", errors=" + report.summary.totalErrors);
                UnityEngine.Debug.Log("Notebook UI Android validation build succeeded: " + output +
                    ", bytes=" + report.summary.totalSize);
            }
            finally { EditorUserBuildSettings.buildAppBundle = previousBundle; }
        }
    }
}
