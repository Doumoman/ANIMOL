using System.Linq;
using ANIMOL.ProductionV1.Editor;
using ANIMOL.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.ProductionV1.Tests
{
    public sealed class ProductionAssetTests
    {
        [Test] public void AllSixtyUiSourcesKeepPointFullRectAndNativeMargins()
        {
            var c=Resources.Load<ProductionCatalog>("ANIMOLProductionV1/Catalog");Assert.NotNull(c);Assert.AreEqual(60,c.Art.Length);
            foreach(var s in c.Art)
            {
                Assert.NotNull(s);var t=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(s));
                Assert.AreEqual(FilterMode.Point,t.filterMode,s.name);Assert.IsFalse(t.mipmapEnabled);Assert.AreEqual(TextureImporterCompression.Uncompressed,t.textureCompression);
                var settings=new TextureImporterSettings();t.ReadTextureSettings(settings);Assert.AreEqual(SpriteMeshType.FullRect,settings.spriteMeshType);
                Assert.AreEqual(new Vector2(.5f,.5f),settings.spritePivot);
            }
            Assert.AreEqual(new Rect(0,0,128,160),c.Find("UI_Character_Rabbit_Portrait").rect);
            Assert.AreEqual(8,c.LoadingFrames.Length);Assert.IsTrue(c.LoadingFrames.All(s=>s.rect.size==new Vector2(32,32)));
            Assert.AreEqual(new Vector4(16,18,16,18),c.Find("UI_Campaign_StageNode_Frame").border);
        }
        [Test] public void SevenScreensAndLobbyEntryAreSerializedAndRegenerationIsIdempotent()
        {
            var scene=EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/Lobby.unity");
            var c=Object.FindFirstObjectByType<ProductionController>();Assert.NotNull(c);
            var host=c.transform.Find("SafeArea/ScreenHost");
            foreach(var id in ProductionController.Screens)Assert.NotNull(host.Find(id+"/ProductionV1"),id);
            int count=c.GetComponentsInChildren<Transform>(true).Length;
            ProductionBuilder.Apply(scene);ProductionBuilder.Apply(scene);
            Assert.AreEqual(count,c.GetComponentsInChildren<Transform>(true).Length);
            Assert.AreEqual(1,c.GetComponentsInChildren<Button>(true).Count(b=>b.name=="PV1_LobbyUpgrade"));
            Assert.AreEqual(0,c.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));
            var scaler=c.GetComponent<CanvasScaler>();Assert.AreEqual(new Vector2(1080,1920),scaler.referenceResolution);
        }
        [Test] public void PurchaseGateRejectsDuplicateAndUnknownOutcomes()
        {
            var gate=new PurchasePresentation();Assert.IsTrue(gate.Begin());Assert.IsFalse(gate.Begin());
            gate.Complete(ANIMOL.Core.AccountRequestStatus.Unavailable);Assert.IsFalse(gate.Pending);Assert.IsTrue(gate.ResultUnknown);Assert.IsFalse(gate.Begin());
        }
        [Test] public void GamePlanesHaveNoCollidersAndAlwaysSortBehindTerrain()
        {
            var scene=EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/Campaign/ThemeRuntime_T01.unity");
            var bg=Object.FindFirstObjectByType<ProductionGameBackdrop>();Assert.NotNull(bg);Assert.AreEqual(3,bg.Layers.Length);
            Assert.AreEqual(0,bg.GetComponentsInChildren<Collider2D>(true).Length);
            foreach(var r in bg.Layers){Assert.Less(r.sortingOrder,0);Assert.AreEqual("Default",r.sortingLayerName);Assert.AreEqual(16,r.sprite.pixelsPerUnit);Assert.AreEqual(704,r.sprite.rect.width);}
            EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/Lobby.unity");
        }
    }
}
