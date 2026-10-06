using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Development;
using ANIMOL.Editor;
using ANIMOL.Gameplay;
using Animol.TerrainStructure;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object=UnityEngine.Object;

namespace ANIMOL.Tests
{

    public sealed class ThemeFinishV6ParityTests
    {
        private const int artVersion=6;
        private const string Output="Docs/Validation/ThemeFinishV6";
        private const string Source=Output+"/Expected";
        [Serializable] private class Fixture {public string id;public string[] rows;public Vector2Int origin;public int seed;public bool scale8;}
        [Serializable] private class Fixtures {public Fixture[] fixtures;}
        [Serializable] private class Comparison {public string style,fixture,path;public int rgbDifferences,alphaDifferences,pixels,solidCells,emptyCells;}
        [Serializable] private class Results {public List<Comparison> comparisons=new List<Comparison>();}

        [Test] public void AllThemesMatchDeliveredAtlasesInSceneRuntimeAndInteger8xWithEmptyHolePhysics()
        {
            Directory.CreateDirectory(Output+"/Renders");
            var results=new Results();var registry=FreeShapeArtRegistry.Load(FreeShapeLayer.Contract,artVersion);
            var scene=EditorSceneManager.NewPreviewScene();var old=RenderTexture.active;
            var cameraObject=new GameObject("V6 pixel comparison camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.allowHDR=false;camera.allowMSAA=false;
            var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                foreach(var style in registry.styles)
                {
                    var map=ScriptableObject.CreateInstance<StageMapDefinition>();var path="Assets/TEST-THEME-V6-"+Guid.NewGuid().ToString("N")+".asset";
                    TerrainEditorAdapter adapter=null;
                    try
                    {
                        map.EditorInitializeIdentity("THEME-V6-PARITY",style.themeId);map.EditorInitializeVariableChunksFromAuthoredContent(1);map.EditorTrySetChunkBounds(new RectInt(-3,-3,8,8),false,out _);
                        AssetDatabase.CreateAsset(map,path);adapter=new TerrainEditorAdapter(AssetDatabase.AssetPathToGUID(path));
                        foreach(var fixture in JsonUtility.FromJson<Fixtures>(File.ReadAllText(Source+"/Fixtures.json")).fixtures)
                        {
                            var points=FreeShapeTopology.FromRows(fixture.rows,fixture.origin).ToHashSet();
                            var dto=adapter.Read();dto.freeShape.artVersion=artVersion;dto.freeShape.seed=fixture.seed;dto.freeShape.cells=points.Select(p=>new FreeShapeCell{x=p.x,y=p.y,styleId=style.styleId}).ToList();dto.revision++;
                            map.EditorApplyTerrainCandidate(adapter.Terrain,dto);
                            int width=fixture.rows[0].Length,height=fixture.rows.Length;
                            source.LoadImage(File.ReadAllBytes(Source+"/"+style.styleId+"-"+fixture.id+".png"));
                            camera.orthographicSize=height*.5f;camera.aspect=(float)width/height;camera.transform.position=new Vector3(fixture.origin.x+width*.5f,fixture.origin.y+height*.5f,-10);
                            foreach(bool runtime in new[]{false,true})
                            foreach(int scale in (fixture.scale8?new[]{1,8}:new[]{1}))
                            {
                                var root=new GameObject(runtime?"Runtime loader":"Scene authoring artwork");SceneManager.MoveGameObjectToScene(root,scene);
                                var rt=new RenderTexture(width*32*scale,height*32*scale,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();
                                var read=new Texture2D(width*32*scale,height*32*scale,TextureFormat.RGBA32,false);
                                try
                                {
                                    if(runtime)
                                    {
                                        var loader=root.AddComponent<StageMapRuntimeLoader>();loader.enabled=false;loader.ConfigureStandaloneDevelopmentOwner();loader.Load(map);
                                        var collider=loader.ResolveLayer(StageMapLayer.Terrain).GetComponent<TilemapCollider2D>();collider.ProcessTilemapChanges();Physics2D.SyncTransforms();
                                        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
                                        {var p=fixture.origin+new Vector2Int(x,y);Assert.That(collider.OverlapPoint((Vector2)p+Vector2.one*.5f),Is.EqualTo(points.Contains(p)),style.styleId+"/"+fixture.id+" physics "+p);}
                                        Assert.That(root.GetComponent<StageTerrainStructureRuntime>().LogicalGrid.solids.Count,Is.EqualTo(points.Count));
                                    }
                                    else TerrainEditorArt.BuildMap(adapter,root.transform);
                                    Assert.That(root.GetComponentInChildren<FreeShapeTerrainRenderer>().GetComponentsInChildren<Collider2D>(),Is.Empty);
                                    camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);read.Apply();
                                    var a=read.GetPixels32();var b=source.GetPixels32();var comparison=new Comparison{style=style.styleId,fixture=fixture.id,path=(runtime?"Runtime":"Scene")+"-"+scale+"x",pixels=a.Length,solidCells=points.Count,emptyCells=width*height-points.Count};
                                    for(int i=0;i<a.Length;i++){int j=(i/(width*32*scale)/scale)*(width*32)+(i%(width*32*scale))/scale;if(a[i].a!=b[j].a)comparison.alphaDifferences++;if(a[i].a!=0&&b[j].a!=0&&(a[i].r!=b[j].r||a[i].g!=b[j].g||a[i].b!=b[j].b))comparison.rgbDifferences++;}
                                    results.comparisons.Add(comparison);
                                    // Retain both independently rendered paths at their measured resolution.
                                    File.WriteAllBytes(Output+"/Renders/"+style.styleId+"-"+fixture.id+"-"+comparison.path+".png",read.EncodeToPNG());
                                }
                                finally{camera.targetTexture=null;RenderTexture.active=old;Object.DestroyImmediate(read);rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(root);}
                            }
                        }
                    }
                    finally{adapter?.Dispose();Undo.ClearUndo(map);AssetDatabase.DeleteAsset(path);}
                }
            }
            finally
            {
                File.WriteAllText(Output+"/ArtParity.json",JsonUtility.ToJson(results,true));RenderTexture.active=old;Object.DestroyImmediate(source);Object.DestroyImmediate(cameraObject);EditorSceneManager.ClosePreviewScene(scene);
            }
            Assert.That(results.comparisons.Count,Is.EqualTo(960));
            Assert.That(results.comparisons.Sum(c=>c.rgbDifferences+c.alphaDifferences),Is.Zero,"See ArtParity.json and the retained renders.");
        }
    }
}
