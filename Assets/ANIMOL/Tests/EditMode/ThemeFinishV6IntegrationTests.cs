using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ANIMOL.Core;
using ANIMOL.Editor;
using ANIMOL.Gameplay;
using Animol.TerrainStructure;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object=UnityEngine.Object;
using Engine=Animol.TerrainStructure.AnimolTerrainPlacementEngine;

namespace ANIMOL.Tests
{
    [Category("CellBoundsRegression")]
    public sealed class ThemeFinishV6IntegrationTests
    {
        [Serializable] private class Catalog {public Style[] styles;}
        [Serializable] private class Style {public string styleId,themeId,themeName,displayName;public Variant[] variants;public Panels panels;}
        [Serializable] private class Variant {public string atlas;}
        [Serializable] private class Panels {public string motif;}
        [Test] public void InstalledReferencesAreExactPackageAtlasesAndMotifsOnly()
        {
            var art=FreeShapeArtRegistry.Load(FreeShapeLayer.Contract,6);art.ValidateComplete();
            var catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText(FreeShapeArtV6Builder.Source+"/Data/sprite_lookup.json"));
            var paths=new HashSet<string>();
            foreach(var s in catalog.styles)
            {
                var installed=art.Style(s.styleId);
                Assert.That(installed.themeId,Is.EqualTo(s.themeId));Assert.That(installed.themeName,Is.EqualTo(s.themeName));Assert.That(installed.displayName,Is.EqualTo(s.displayName));
                for(int v=0;v<4;v++)
                {
                    var path=AssetDatabase.GetAssetPath(installed.cells[v*47]);paths.Add(path);
                    Assert.That(path,Does.StartWith(FreeShapeArtV6Builder.Root+"/Art/Atlases/"));
                    Assert.That(FreeShapeArtV6Builder.HashFile(path),Is.EqualTo(FreeShapeArtV6Builder.HashFile(FreeShapeArtV6Builder.Source+"/"+s.variants[v].atlas)));
                    Assert.That(installed.cells.Skip(v*47).Take(47).All(c=>AssetDatabase.GetAssetPath(c)==path),Is.True);
                    CheckImport(path,SpriteImportMode.Multiple,new Vector2(.5f,.5f));
                }
                var motif=AssetDatabase.GetAssetPath(installed.motif);paths.Add(motif);
                Assert.That(motif,Does.StartWith(FreeShapeArtV6Builder.Root+"/Art/Motifs/"));
                Assert.That(FreeShapeArtV6Builder.HashFile(motif),Is.EqualTo(FreeShapeArtV6Builder.HashFile(FreeShapeArtV6Builder.Source+"/"+s.panels.motif)));
                CheckImport(motif,SpriteImportMode.Single,Vector2.zero);
            }
            Assert.That(paths.Count,Is.EqualTo(100));
            Assert.That(Directory.GetFiles(FreeShapeArtV6Builder.Root,"*.png",SearchOption.AllDirectories).Length,Is.EqualTo(100));
        }
        private static void CheckImport(string path,SpriteImportMode mode,Vector2 pivot)
        {
            var t=(TextureImporter)AssetImporter.GetAtPath(path);var settings=new TextureImporterSettings();t.ReadTextureSettings(settings);
            Assert.That(t.spriteImportMode,Is.EqualTo(mode));Assert.That(t.spritePixelsPerUnit,Is.EqualTo(32));Assert.That(settings.spriteMeshType,Is.EqualTo(SpriteMeshType.FullRect));
            Assert.That(settings.spritePivot,Is.EqualTo(pivot));Assert.That(t.filterMode,Is.EqualTo(FilterMode.Point));Assert.That(t.textureCompression,Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(t.mipmapEnabled,Is.False);Assert.That(t.sRGBTexture,Is.True);Assert.That(t.npotScale,Is.EqualTo(TextureImporterNPOTScale.None));
        }
        [Test] public void UnknownOrUninstalledContractsNeverFallback()
        {
            Assert.Throws<InvalidOperationException>(()=>FreeShapeArtRegistry.Load("OTHER",6));
            Assert.Throws<InvalidOperationException>(()=>FreeShapeArtRegistry.Load(FreeShapeLayer.Contract,99));
            Assert.Throws<InvalidOperationException>(()=>FreeShapeArtRegistry.Load(new FreeShapeLayer{artVersion=6,schemaVersion=2}));
            foreach(int v in new[]{1,2,3,4,5})
                Assert.Throws<InvalidOperationException>(()=>FreeShapeArtRegistry.Load(FreeShapeLayer.Contract,v));
        }
        [Test] public void SceneBrushAndUndoRemainFixedToV6()
        {
            var map=ScriptableObject.CreateInstance<StageMapDefinition>();string path="Assets/TEST-V6-SCENE-"+Guid.NewGuid().ToString("N")+".asset";
            try
            {
                map.EditorInitializeIdentity("TEST-V6-SCENE","T03");map.EditorInitializeBoundsFromAuthoredContent(1);map.EditorTrySetCellBounds(new RectInt(-16,-16,48,48),false,out _);map.FreeShapeTerrain.artVersion=6;
                AssetDatabase.CreateAsset(map,path);var before=File.ReadAllBytes(path);
                TerrainEditorSceneEditor.Open(map);Assert.That(File.ReadAllBytes(path),Is.EqualTo(before));
                Assert.That(TerrainEditorSceneEditor.SelectedFreeShapeArt.version,Is.EqualTo(6));
                TerrainEditorSceneEditor.State.freeTool="Paint";TerrainEditorSceneEditor.State.freeBrush=1;
                TerrainEditorSceneEditor.BeginFreeStroke(new Vector2Int(-1,-1));TerrainEditorSceneEditor.CommitFreeStroke();
                Assert.That(map.FreeShapeTerrain.artVersion,Is.EqualTo(6));Assert.That(map.FreeShapeTerrain.cells.Single().styleId,Is.EqualTo("T03_A"));
                TerrainEditorSceneEditor.Adapter.Undo();Assert.That(map.FreeShapeTerrain.cells,Is.Empty);
                Assert.That(TerrainEditorSceneEditor.SelectedFreeShapeArt.version,Is.EqualTo(6));
                TerrainEditorSceneEditor.Adapter.Redo();Assert.That(map.FreeShapeTerrain.cells.Count,Is.EqualTo(1));
                Assert.That(TerrainEditorSceneEditor.SelectedFreeShapeArt.version,Is.EqualTo(6));
                TerrainEditorSceneEditor.Close();AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);TerrainEditorSceneEditor.Open(map);
                Assert.That(TerrainEditorSceneEditor.SelectedFreeShapeArt.version,Is.EqualTo(6));
            }
            finally{TerrainEditorSceneEditor.Close();Undo.ClearUndo(map);AssetDatabase.DeleteAsset(path);}
        }
        [Test, Timeout(600000)] public void AllTwentyStylesRepeatedHoleEditsRefreshEveryNeighborAndPreserveEmptyCells()
        {
            var map=ScriptableObject.CreateInstance<StageMapDefinition>();string path="Assets/TEST-V6-HOLES-"+Guid.NewGuid().ToString("N")+".asset";
            var go=new GameObject("V6 incremental holes");var renderer=go.AddComponent<FreeShapeTerrainRenderer>();TerrainEditorAdapter adapter=null;
            try
            {
                map.EditorInitializeIdentity("TEST-V6-HOLES","T01");map.EditorInitializeBoundsFromAuthoredContent(1);map.EditorTrySetCellBounds(new RectInt(-32,-32,64,64),false,out _);map.FreeShapeTerrain.artVersion=6;
                AssetDatabase.CreateAsset(map,path);adapter=new TerrainEditorAdapter(AssetDatabase.AssetPathToGUID(path));
                adapter.Changed+=()=>renderer.Rebuild(map.FreeShapeTerrain,1);
                var art=FreeShapeArtRegistry.Load(FreeShapeLayer.Contract,6);
                foreach(var style in art.styles)
                {
                    map.FreeShapeTerrain.cells.Clear();map.EditorInitializeIdentity("TEST-V6-HOLES",style.themeId);
                    var dto=adapter.Read();dto.freeShape.cells=Enumerable.Range(-17,8).SelectMany(x=>Enumerable.Range(-17,8).Select(y=>new FreeShapeCell{x=x,y=y,styleId=style.styleId})).ToList();dto.revision++;adapter.Commit(dto,"V6 hole fixture");
                    for(int repeat=0;repeat<2;repeat++)foreach(var step in new[]{0,1,2})
                    {
                        var points=step==0?new[]{new Vector2Int(-15,-15)}:Enumerable.Range(-15,2).SelectMany(x=>Enumerable.Range(-15,2).Select(y=>new Vector2Int(x,y))).ToArray();
                        Assert.That(Engine.TryEditFreeShape(adapter.Terrain.Catalog,adapter.Read(),points,step!=2,style.styleId,out var candidate,out var error),Is.True,error);adapter.Commit(candidate,"V6 hole edit");
                        var index=FreeShapeTopology.Index(map.FreeShapeTerrain);var tilemaps=go.GetComponentsInChildren<Tilemap>();
                        for(int y=-17;y<-9;y++)for(int x=-17;x<-9;x++)
                        {
                            var p=new Vector2Int(x,y);var sprites=tilemaps.Select(t=>t.GetSprite(new Vector3Int(x,y,0))).Where(s=>s!=null).ToArray();
                            if(!index.ContainsKey(p))Assert.That(sprites,Is.Empty);else Assert.That(sprites,Is.EqualTo(new[]{art.Cell(style.styleId,FreeShapeTopology.Raw(index,p),FreeShapeTopology.Variant(x,y,0))}));
                        }
                        Assert.That(go.GetComponentsInChildren<Collider2D>(),Is.Empty);
                    }
                }
            }
            finally{adapter?.Dispose();Object.DestroyImmediate(go);Undo.ClearUndo(map);AssetDatabase.DeleteAsset(path);}
        }
    }
}
