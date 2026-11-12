#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace William.NotebookUI.Editor
{
    public sealed class NotebookAndroidCampaignBuild : IPreprocessBuildWithReport
    {
        public const string MainScene = "Assets/ANIMOL/Scenes/Main/NotebookUI.unity";
        public const string TheaterScene = "Assets/ANIMOL/Scenes/Gameplay/ANIMOL_Echo_Theater.unity";
        public const string TheaterMaps = "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/EchoTheaterMapSet.asset";
        public int callbackOrder => 10;

        [MenuItem("ANIMOL/Build/Prepare Android 40 Stage Preview")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before preparing the build.");
            UnityEditor.Build.Profile.BuildProfile.SetActiveBuildProfile(null);
            StarwayStageCatalogSync.Sync();
            var preview = AssetDatabase.LoadAssetAtPath<NotebookCampaignPreview>(NotebookCampaignPreview.AssetPath);
            if (!preview)
            {
                preview = ScriptableObject.CreateInstance<NotebookCampaignPreview>();
                AssetDatabase.CreateAsset(preview, NotebookCampaignPreview.AssetPath);
            }
            preview.enabled = true;
            EditorUtility.SetDirty(preview);
            AssetDatabase.SaveAssetIfDirty(preview);
            NotebookStartupAssetSync.Sync();
            ValidateMaps();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScene, true), new EditorBuildSettingsScene(TheaterScene, true) };
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;
            PlayerSettings.Android.defaultWindowWidth = 1080;
            PlayerSettings.Android.defaultWindowHeight = 1920;
            PlayerSettings.Android.startInFullscreen = true;
            PlayerSettings.Android.fullscreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.Android.renderOutsideSafeArea = true;
            PlayerSettings.Android.resizeableActivity = true;
            // 9:16 is the design frame. Let tall phones fill their native display.
            PlayerSettings.Android.minAspectRatio = 1f;
            PlayerSettings.Android.maxAspectRatio = 3f;
            PlayerSettings.Android.splashScreenScale = AndroidSplashScreenScale.ScaleToFill;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            SetAndroidCompression();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                throw new BuildFailedException("Android build support is required.");
            Debug.Log("Android preview ready: Starway 20 + Echo Theater 20, portrait 1080x1920, native full-screen background.");
        }

        // Unity 6 keeps the Build Profiles compression-method API internal.
        const BindingFlags CompressionApi = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        public static string AndroidCompression => typeof(EditorUserBuildSettings).GetMethod("GetCompressionType", CompressionApi)
            .Invoke(null, new object[] { BuildTargetGroup.Android }).ToString();
        static void SetAndroidCompression()
        {
            var setter = typeof(EditorUserBuildSettings).GetMethod("SetCompressionType", CompressionApi);
            if (setter == null) throw new InvalidOperationException("Android Compression Method API is unavailable.");
            setter.Invoke(null, new[] { (object)BuildTargetGroup.Android,
                Enum.Parse(setter.GetParameters()[1].ParameterType, "Lz4HC") });
        }

        public static void ValidateMaps()
        {
            var starway = StarwayStageCatalog.Current;
            var preview = NotebookCampaignPreview.Current;
            if (!starway || !preview || !preview.enabled || !preview.theaterMaps)
                throw new BuildFailedException("Campaign preview catalogs are missing or disabled.");
            var maps = starway.maps.Select(entry => entry.map).ToArray();
            for (int i = 1; i <= NotebookCampaignStages.PerTheme; i++)
            {
                string sw = "SW" + i.ToString("00"), et = "ET-" + i.ToString("00");
                ValidateMap(NotebookCampaignStages.FindStarwayMap(maps, sw), sw);
                if (!NotebookCampaignPreview.TryTheaterIndex(et, out int index))
                    throw new BuildFailedException("Missing or misordered theater stage: " + et);
                ValidateMap(preview.theaterMaps.maps[index].map, et);
            }
        }
        static void ValidateMap(PuzzleMap map, string id)
        {
            if (!map || !map.scenario || !map.theme) throw new BuildFailedException(id + " needs map, scenario and theme.");
            var errors = map.ValidateMap();
            if (errors.Count != 0) throw new BuildFailedException(id + ": " + string.Join("; ", errors));
        }
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android || !NotebookCampaignPreview.IsEnabled) return;
            ValidateMaps();
            string[] enabled = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (!enabled.SequenceEqual(new[] { MainScene, TheaterScene }) || PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
                throw new BuildFailedException("Run ANIMOL > Build > Prepare Android 40 Stage Preview before building.");
        }

        [MenuItem("ANIMOL/Build/Build Android 40 Stage Preview APK")]
        public static void BuildApk()
        {
            Prepare();
            Directory.CreateDirectory("Builds/Android");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { MainScene, TheaterScene }, target = BuildTarget.Android,
                locationPathName = "Builds/Android/ANIMOL_40Stage_Portrait.apk", options = BuildOptions.CompressWithLz4HC
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Android preview build failed: " + report.summary.result);
        }
    }
}
#endif
