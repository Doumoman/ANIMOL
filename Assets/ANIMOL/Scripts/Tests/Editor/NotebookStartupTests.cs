using System.Linq;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using William.NotebookUI;
using William.NotebookUI.Editor;

namespace ANIMOL.Presentation.Tests
{
    public sealed class NotebookStartupTests
    {
        [Test] public void LobbyAndProgressIndexDoNotReferencePuzzleOrStoryContent()
        {
            foreach (string root in new[] { NotebookAndroidCampaignBuild.MainScene, NotebookStageIndex.AssetPath, NotebookCampaignPreview.AssetPath })
            {
                var dependencies = AssetDatabase.GetDependencies(root, true);
                Assert.That(dependencies.Any(p => p.Contains("/MapData/") && p.EndsWith(".asset")), Is.False, root);
                Assert.That(dependencies, Does.Not.Contain(NotebookStartupAssetSync.StoryPrefab), root);
                Assert.That(dependencies, Does.Not.Contain(StarwayStageCatalog.AssetPath), root);
                Assert.That(dependencies.Any(p => p.Contains("/Art/Maps/")), Is.False, root);
            }
        }

        [Test] public void LightweightIndexKeepsEverySourceIdAndRejectsDuplicateStageNames()
        {
            var index = NotebookStageIndex.Current;
            Assert.That(index.stages.Length, Is.EqualTo(20));
            foreach (var source in StarwayStageCatalog.Current.maps)
            {
                Assert.That(NotebookCampaignStages.TryStarwayStageId(source.map, out string id), Is.True);
                Assert.That(index.Find(id)?.sourceId, Is.EqualTo(source.sourceId), id);
                Assert.That(index.Find(id)?.assetName, Is.EqualTo(source.assetName), id);
            }
            var duplicate = ScriptableObject.CreateInstance<NotebookStageIndex>();
            try
            {
                duplicate.stages = new[] { new NotebookStageIndex.Entry { stageId = "SW01" }, new NotebookStageIndex.Entry { stageId = "SW01" } };
                Assert.That(duplicate.Find("SW01"), Is.Null);
            }
            finally { Object.DestroyImmediate(duplicate); }
        }

        [Test] public void ReadingLobbyProgressAndAvailabilityDoesNotLoadDeferredAssets()
        {
            foreach (var asset in Resources.FindObjectsOfTypeAll<NotebookDeferredAssets>()) Resources.UnloadAsset(asset);
            var gateway = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/UI/Main/NotebookCanvas.prefab")
                .GetComponentInChildren<NotebookProjectGateway>(true);
            gateway.ReadVerifiedProgress(snapshot => Assert.That(snapshot.verified, Is.True));
            for (int i = 1; i <= 20; i++)
            {
                Assert.That(gateway.CanOpenStage("SW" + i.ToString("00")), Is.True);
                Assert.That(gateway.CanOpenStage("ET-" + i.ToString("00")), Is.True);
            }
            Assert.That(Resources.FindObjectsOfTypeAll<NotebookDeferredAssets>(), Is.Empty);
        }

        [Test] public void AndroidPatternUsesCompressedGpuStorageAtItsOriginalResolution()
        {
            const string path = "Assets/ANIMOL/Art/UI/Main/Pattern/Textures/CharacterFacesStars_All_2048.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), "Desktop/source import settings stay intact.");
            var android = importer.GetPlatformTextureSettings("Android");
            Assert.That(android.overridden, Is.True);
            Assert.That(android.format, Is.EqualTo(TextureImporterFormat.ASTC_4x4));
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.That(texture.width, Is.EqualTo(2048));
            Assert.That(texture.height, Is.EqualTo(2048));
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                Assert.That(texture.format, Is.EqualTo(TextureFormat.ASTC_4x4));
        }
    }
}
