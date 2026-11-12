using System.Linq;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using William.NotebookUI;
using William.NotebookUI.Editor;

namespace ANIMOL.Presentation.Tests
{
    public sealed class AndroidCampaignBuildTests
    {
        [Test] public void FortyStagesHaveValidRuntimeReferencesAndStopAtTwenty()
        {
            NotebookAndroidCampaignBuild.ValidateMaps();
            var gateway = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/UI/Main/NotebookCanvas.prefab")
                .GetComponentInChildren<NotebookProjectGateway>(true);
            var deps = AssetDatabase.GetDependencies(NotebookCampaignPreview.AssetPath, true);
            for (int i = 1; i <= 20; i++)
            {
                string sw = "SW" + i.ToString("00"), et = "ET-" + i.ToString("00");
                Assert.That(gateway.CanOpenStage(sw), Is.True, sw);
                Assert.That(gateway.CanOpenStage(et), Is.True, et);
                Assert.That(NotebookCampaignPreview.TryTheaterIndex(et, out int index), Is.True);
                Assert.That(deps, Does.Contain(AssetDatabase.GetAssetPath(NotebookCampaignPreview.Current.theaterMaps.maps[index].map)));
            }
            Assert.That(gateway.CanOpenStage("ET-21"), Is.False);
            Assert.That(gateway.CanOpenStage("SW21"), Is.False);
            Assert.That(gateway.CanOpenStage("observatory-01"), Is.False);
            Assert.That(NotebookCampaignStages.TryNext("SW20", out _), Is.False);
            Assert.That(NotebookCampaignStages.TryNext("ET-20", out _), Is.False);
        }

        [Test] public void PreviewAccessDoesNotPersistAPurchaseOrSkipStageProgress()
        {
            var catalog = NotebookCatalog.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/ANIMOL/Data/UI/Notebook/catalog.json"));
            var state = new NotebookWalletState();
            var preview = new NotebookBmModel(catalog, state, 1000, true);
            Assert.That(preview.CanEnter("ET-01"), Is.True);
            Assert.That(preview.CanEnter("ET-02"), Is.False);
            Assert.That(preview.Owns("observatory"), Is.False);
            Assert.That(preview.Owns("sea"), Is.False);
            Assert.That(preview.AcceptVerifiedClear("ET-01", true), Is.True);
            Assert.That(preview.CanEnter("ET-02"), Is.True);
            Assert.That(state.ownedThemes, Is.EqualTo(new[] { "starway" }));
            Assert.That(new NotebookBmModel(catalog, state, 1000).Owns("theater"), Is.False);
        }

        [Test] public void AndroidUsesPortraitNativeFullscreenAndOnlyCampaignScenes()
        {
            Assert.That(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path),
                Is.EqualTo(new[] { NotebookAndroidCampaignBuild.MainScene, NotebookAndroidCampaignBuild.TheaterScene }));
            Assert.That(PlayerSettings.defaultInterfaceOrientation, Is.EqualTo(UIOrientation.Portrait));
            Assert.That(PlayerSettings.defaultScreenWidth, Is.EqualTo(1080));
            Assert.That(PlayerSettings.defaultScreenHeight, Is.EqualTo(1920));
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeLeft || PlayerSettings.allowedAutorotateToLandscapeRight, Is.False);
            Assert.That(PlayerSettings.Android.startInFullscreen, Is.True);
            Assert.That(PlayerSettings.Android.renderOutsideSafeArea, Is.True);
            Assert.That(PlayerSettings.Android.maxAspectRatio, Is.GreaterThanOrEqualTo(3f));
            Assert.That(PlayerSettings.Android.targetArchitectures, Is.EqualTo(AndroidArchitecture.ARM64));
            Assert.That(NotebookAndroidCampaignBuild.AndroidCompression, Is.EqualTo("Lz4HC"));
        }
    }
}
