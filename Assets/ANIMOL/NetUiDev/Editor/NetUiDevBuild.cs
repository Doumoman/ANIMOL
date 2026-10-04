using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Animol.NetUiDev.Editor
{
    public static class NetUiDevBuild
    {
        private const string Define = "ANIMOL_NET_UI_DEV";
        [MenuItem("ANIMOL/NET02 Existing UI DEV/0 Enable DEV Define")]
        public static void Enable()
        {
            SetDefine(NamedBuildTarget.Standalone, true);
            SetDefine(NamedBuildTarget.Android, true);
            Debug.Log("NET02 enabled. Wait for script recompilation, connect the existing adapters/SC06, then build with existing Bootstrap/Lobby scenes.");
        }
        [MenuItem("ANIMOL/NET02 Existing UI DEV/9 Disable DEV Define")]
        public static void Disable()
        { SetDefine(NamedBuildTarget.Standalone, false); SetDefine(NamedBuildTarget.Android, false); }

        [MenuItem("ANIMOL/NET02 Existing UI DEV/1 Build Windows Development")]
        public static void BuildWindows()
        {
            string[] scenes = Preflight(NamedBuildTarget.Standalone);
            string output = "Builds/ANIMOLNet02UiDev/Windows/ANIMOLNet02Dev.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            string oldName = PlayerSettings.productName;
            bool oldSingle = PlayerSettings.forceSingleInstance;
            InsecureHttpOption oldHttp = PlayerSettings.insecureHttpOption;
            try
            {
                PlayerSettings.productName = "ANIMOLNet02Dev";
                PlayerSettings.forceSingleInstance = false;
                PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
                CheckReport(BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes, locationPathName = output,
                    target = BuildTarget.StandaloneWindows64,
                    subtarget = (int)StandaloneBuildSubtarget.Player,
                    options = BuildOptions.Development
                }));
            }
            finally { PlayerSettings.productName = oldName; PlayerSettings.forceSingleInstance = oldSingle; PlayerSettings.insecureHttpOption = oldHttp; }
        }

        [MenuItem("ANIMOL/NET02 Existing UI DEV/2 Build Android Development APK")]
        public static void BuildAndroid()
        {
            string[] scenes = Preflight(NamedBuildTarget.Android);
            string output = "Builds/ANIMOLNet02UiDev/Android/ANIMOLNet02Dev.apk";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            string oldName = PlayerSettings.productName;
            string oldIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            UIOrientation oldOrientation = PlayerSettings.defaultInterfaceOrientation;
            bool oldKeystore = PlayerSettings.Android.useCustomKeystore;
            bool oldInternet = PlayerSettings.Android.forceInternetPermission;
            bool oldBundle = EditorUserBuildSettings.buildAppBundle;
            AndroidArchitecture oldArchitectures = PlayerSettings.Android.targetArchitectures;
            ScriptingImplementation oldBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android);
            InsecureHttpOption oldHttp = PlayerSettings.insecureHttpOption;
            try
            {
                PlayerSettings.productName = "ANIMOLNet02Dev";
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.animol.net02dev");
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
                PlayerSettings.Android.useCustomKeystore = false;
                PlayerSettings.Android.forceInternetPermission = true;
                EditorUserBuildSettings.buildAppBundle = false;
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
                CheckReport(BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes, locationPathName = output, target = BuildTarget.Android,
                    options = BuildOptions.Development
                }));
            }
            finally
            {
                PlayerSettings.productName = oldName;
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, oldIdentifier);
                PlayerSettings.defaultInterfaceOrientation = oldOrientation;
                PlayerSettings.Android.useCustomKeystore = oldKeystore;
                PlayerSettings.Android.forceInternetPermission = oldInternet;
                EditorUserBuildSettings.buildAppBundle = oldBundle;
                PlayerSettings.Android.targetArchitectures = oldArchitectures;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, oldBackend);
                PlayerSettings.insecureHttpOption = oldHttp;
            }
        }

        private static string[] Preflight(NamedBuildTarget target)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) throw new InvalidOperationException("Wait for Unity compilation/import before building NET02.");
            if (!HasDefine(target)) throw new InvalidOperationException("Enable ANIMOL_NET_UI_DEV and wait for domain reload first.");
            if (Type.GetType("Animol.NetUiDev.NetUiDevCoordinator, ANIMOL.NetUiDev") == null)
                throw new InvalidOperationException("NET02 runtime assembly is not compiled. Enable the active platform define and wait for recompilation.");
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0 || scenes.Any(scene => !File.Exists(scene))) throw new InvalidOperationException("Use the existing enabled production Bootstrap/Lobby build scenes.");
            bool firstIsBootstrap = Path.GetFileNameWithoutExtension(scenes[0]).IndexOf("Bootstrap", StringComparison.OrdinalIgnoreCase) >= 0;
            bool hasLobby = scenes.Any(scene => Path.GetFileNameWithoutExtension(scene).IndexOf("Lobby", StringComparison.OrdinalIgnoreCase) >= 0);
            if (!firstIsBootstrap || !hasLobby)
                throw new InvalidOperationException("Existing first enabled scene must be Bootstrap and enabled scenes must contain Lobby. If project scene names differ, inspect and adapt this guard to the actual startup owner; do not generate an alternate scene.");
            return scenes;
        }
        private static bool HasDefine(NamedBuildTarget target)
        { return PlayerSettings.GetScriptingDefineSymbols(target).Split(';').Contains(Define); }
        private static void SetDefine(NamedBuildTarget target, bool enabled)
        {
            var values = PlayerSettings.GetScriptingDefineSymbols(target).Split(';').Where(value => !string.IsNullOrWhiteSpace(value) && value != Define).ToList();
            if (enabled) values.Add(Define);
            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", values));
        }
        private static void CheckReport(BuildReport report)
        { if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("NET02 build failed: " + report.summary.result); Debug.Log("NET02 development build: " + report.summary.outputPath); }
    }
}
