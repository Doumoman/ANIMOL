using System;
using System.IO;
using System.Reflection;
using ANIMOL.Core;
using ANIMOL.Editor;
using ANIMOL.Gameplay;
using Animol.TerrainStructure.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Tests
{
    [Category("CellBoundsRegression")]
    public sealed class CampaignMapAccessTests
    {
        private string folder, id;
        private CampaignCatalog catalog;
        private CampaignStageDefinition stage;
        private StageMapDefinition map;
        [SetUp] public void Setup()
        {
            id="CAMPAIGN-ACCESS-"+Guid.NewGuid().ToString("N");
            folder="Assets/"+id; AssetDatabase.CreateFolder("Assets",id);
            map=ScriptableObject.CreateInstance<StageMapDefinition>();map.EditorInitializeIdentity(id,"T01");
            map.EditorTrySetCellBounds(new RectInt(0,0,32,32),false,out _);
            var data=new SerializedObject(map);data.FindProperty("worldUnitsPerCell").floatValue=1;data.ApplyModifiedPropertiesWithoutUndo();
            map.FreeShapeTerrain.artVersion=6;
            AssetDatabase.CreateAsset(map,folder+"/not-the-stage-name.asset");
            stage=ScriptableObject.CreateInstance<CampaignStageDefinition>();
            data=new SerializedObject(stage);data.FindProperty("stageId").stringValue=id;data.FindProperty("themeId").stringValue="T01";data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(stage,folder+"/Stage.asset");
            var theme=ScriptableObject.CreateInstance<ThemeDefinition>();data=new SerializedObject(theme);
            data.FindProperty("themeId").stringValue="T01";var stages=data.FindProperty("stages");stages.arraySize=1;stages.GetArrayElementAtIndex(0).objectReferenceValue=stage;data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(theme,folder+"/Theme.asset");
            catalog=ScriptableObject.CreateInstance<CampaignCatalog>();data=new SerializedObject(catalog);
            var themes=data.FindProperty("themes");themes.arraySize=1;themes.GetArrayElementAtIndex(0).objectReferenceValue=theme;data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(catalog,folder+"/Catalog.asset");
        }
        [TearDown] public void Cleanup()
        {
            TerrainEditorSceneEditor.Close();
            if (map!=null) AnimolTerrainMapEditorBridge.Unbind(map);
            foreach(var window in Resources.FindObjectsOfTypeAll<CampaignMapEditorWindow>())window.Close();
            AssetDatabase.DeleteAsset(folder);
            var backup=Path.GetFullPath(CampaignMapBackupStore.BackupRoot+"/"+id);
            if(Directory.Exists(backup))Directory.Delete(backup,true);
        }
        [Test] public void LinksExactAssetOutsideConventionalPathAndReloadsWithoutChangingReadiness()
        {
            var version=stage.ContentVersion;var policy=stage.RuntimePolicy;
            CampaignMapAccess.Link(catalog,stage,map);
            AssetDatabase.ImportAsset(folder+"/Stage.asset",ImportAssetOptions.ForceUpdate);
            Assert.That(stage.MapDefinition,Is.EqualTo(map));
            Assert.That(stage.ContentVersion,Is.EqualTo(version));Assert.That(stage.RuntimePolicy,Is.EqualTo(policy));
            Assert.That(CampaignMapAccess.CreateAndLink(catalog,stage),Is.SameAs(map));
        }
        [Test] public void WrongIdentityIsRejectedWithoutChangingStageFile()
        {
            var before=File.ReadAllText(folder+"/Stage.asset");
            var data=new SerializedObject(map);data.FindProperty("themeId").stringValue="T02";data.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(()=>CampaignMapAccess.Link(catalog,stage,map));
            Assert.That(stage.MapDefinition,Is.Null);Assert.That(File.ReadAllText(folder+"/Stage.asset"),Is.EqualTo(before));
        }
        [Test] public void BrowsingCampaignNeverTakesOwnershipFromLiveAdapter()
        {
            using(var adapter=new TerrainEditorAdapter(AssetDatabase.AssetPathToGUID(folder+"/not-the-stage-name.asset")))
            {
                var read=AnimolTerrainMapEditorBridge.Read;
                var window=CampaignMapEditorWindow.OpenForWorkspace(map);
                Assert.That(AnimolTerrainMapEditorBridge.Read,Is.SameAs(read));
                window.Close();Assert.That(AnimolTerrainMapEditorBridge.Read,Is.SameAs(read));
            }
            Assert.That(AnimolTerrainMapEditorBridge.IsBound,Is.False);
        }
        [Test] public void ExistingUnlinkedMapCannotBeDuplicatedByCreate()
        {
            Assert.Throws<InvalidOperationException>(()=>CampaignMapAccess.CreateAndLink(catalog,stage));
            Assert.That(stage.MapDefinition,Is.Null);
            Assert.That(AssetDatabase.LoadMainAssetAtPath(CampaignMapAccess.MapFolder+"/"+id+".asset"),Is.Null);
        }
        [Test] public void NewStageCreatesOneLinkedV6MapWithUsableBounds()
        {
            AssetDatabase.DeleteAsset(folder+"/not-the-stage-name.asset");map=null;
            var path=CampaignMapAccess.MapFolder+"/"+id+".asset";
            try
            {
                map=CampaignMapAccess.CreateAndLink(catalog,stage);
                Assert.That(stage.MapDefinition,Is.SameAs(map));
                Assert.That(map.FreeShapeTerrain.artVersion,Is.EqualTo(6));
                Assert.That(map.WorldUnitsPerCell,Is.EqualTo(1));Assert.That(map.ContainsCell(0,0),Is.True);
                Assert.That(CampaignMapAccess.FindConventionalMap(stage),Is.SameAs(map));
                Assert.That(CampaignMapAccess.CreateAndLink(catalog,stage),Is.SameAs(map));
            }
            finally { AssetDatabase.DeleteAsset(path);map=null; }
        }
        [Test] public void SceneOpensReadOnlyWithFourMarkersAndRejectsHugeStrokeWithoutWriting()
        {
            var before=File.ReadAllText(folder+"/not-the-stage-name.asset");
            TerrainEditorSceneEditor.Open(map);
            Assert.That(TerrainEditorSceneEditor.Active,Is.True,TerrainEditorSceneEditor.Status);
            Assert.That(TerrainEditorSceneEditor.PartCount,Is.EqualTo(4));
            Assert.That(TerrainEditorSceneEditor.State.freeShape,Is.True);
            TerrainEditorSceneEditor.State.freeTool="Rect";
            TerrainEditorSceneEditor.BeginFreeStroke(Vector2Int.one);
            TerrainEditorSceneEditor.UpdateFreeStroke(new Vector2Int(1000000,1000000));
            Assert.That(TerrainEditorSceneEditor.FreeCandidateValid,Is.False);
            TerrainEditorSceneEditor.Cancel(false);TerrainEditorSceneEditor.Close();
            Assert.That(File.ReadAllText(folder+"/not-the-stage-name.asset"),Is.EqualTo(before));
        }
        [Test] public void BackupIsLocalAndMismatchedRestoreLeavesMapUntouched()
        {
            var before=EditorJsonUtility.ToJson(map,true);
            var path=CampaignMapBackupStore.CreateBackup(map,"test");
            Assert.That(File.ReadAllText(path),Is.EqualTo(before));
            Assert.That(path.Replace('\\','/'),Does.StartWith("UserSettings/ANIMOL/MapBackups/"));
            File.WriteAllText(path,before.Replace(id,"OTHER-STAGE"));
            Assert.Throws<InvalidOperationException>(()=>CampaignMapBackupStore.Restore(map,path));
            Assert.That(EditorJsonUtility.ToJson(map,true),Is.EqualTo(before));
        }
        [Test] public void StartingAnotherStrokeDiscardsThePreviousCandidate()
        {
            TerrainEditorSceneEditor.Open(map);
            TerrainEditorSceneEditor.State.freeTool="Paint";
            TerrainEditorSceneEditor.BeginFreeStroke(new Vector2Int(2,2));
            TerrainEditorSceneEditor.BeginFreeStroke(new Vector2Int(4,4));
            Assert.That(TerrainEditorSceneEditor.FreeCandidateValid,Is.True);
            TerrainEditorSceneEditor.CommitFreeStroke();
            Assert.That(map.FreeShapeTerrain.cells.Count,Is.EqualTo(1));
            Assert.That(map.FreeShapeTerrain.cells[0].Position,Is.EqualTo(new Vector2Int(4,4)));
        }
        [Test] public void TerrainCatalogIsCachedAndReplacedWhenSourceChanges()
        {
            var registry=ScriptableObject.CreateInstance<StageTerrainStructureRegistry>();
            var first=new TextAsset("{\"entries\":[]}");var second=new TextAsset("{\"entries\":[],\"version\":3}");
            try
            {
                registry.catalogJson=first;var parsed=registry.Catalog;Assert.That(registry.Catalog,Is.SameAs(parsed));
                registry.catalogJson=second;Assert.That(registry.Catalog,Is.Not.SameAs(parsed));
            }
            finally { UnityEngine.Object.DestroyImmediate(registry);UnityEngine.Object.DestroyImmediate(first);UnityEngine.Object.DestroyImmediate(second); }
        }
        [Test] public void InstalledEditorSupportsRemovingRetiredMenus()
        {
            Assert.That(typeof(Menu).GetMethod("RemoveMenuItem",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static,null,new[]{typeof(string)},null),Is.Not.Null);
            Assert.DoesNotThrow(CampaignEditorMenus.RemoveRetiredMenus);
        }
    }
}
