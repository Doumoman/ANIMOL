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
    [TestFixture(2),TestFixture(4)]
    public sealed class FreeShapeArtV2ParityTests
    {
        private readonly int artVersion;
        public FreeShapeArtV2ParityTests(int artVersion){this.artVersion=artVersion;}
        private string Output=>artVersion==2?"Docs/Validation/CleanArtV2":"Docs/Validation/JointFinishV4";
        private string Source=>artVersion==2?FreeShapeArtV2Builder.Source:FreeShapeArtV4Builder.Source;
        [Serializable] private class Comparison {public string style,fixture,path;public int rgbDifferences,alphaDifferences,pixels,solidCells,emptyCells;}
        [Serializable] private class Results {public List<Comparison> comparisons=new List<Comparison>();}

        [Test] public void TwentyStylesSixteenFixturesMatchPackageInSceneAndRuntimeWithExactPhysics()
        {
            Directory.CreateDirectory(Output+"/Renders");
            var results=new Results();var registry=FreeShapeArtRegistry.Load(FreeShapeLayer.Contract,artVersion);
            var scene=EditorSceneManager.NewPreviewScene();var old=RenderTexture.active;
            var cameraObject=new GameObject("Clean V2 pixel comparison camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.allowHDR=false;camera.allowMSAA=false;
            var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                foreach(var style in registry.styles)
                {
                    var map=ScriptableObject.CreateInstance<StageMapDefinition>();var path="Assets/TEST-CLEAN-V2-"+Guid.NewGuid().ToString("N")+".asset";
                    TerrainEditorAdapter adapter=null;
                    try
                    {
                        map.EditorInitializeIdentity("CLEAN-V2-PARITY",style.themeId);map.EditorInitializeVariableChunksFromAuthoredContent(1);map.EditorTrySetChunkBounds(new RectInt(-3,-3,8,8),false,out _);
                        AssetDatabase.CreateAsset(map,path);adapter=new TerrainEditorAdapter(AssetDatabase.AssetPathToGUID(path));
                        foreach(var fixture in registry.fixtures)
                        {
                            var points=FreeShapeTopology.FromRows(fixture.rows,fixture.origin).ToHashSet();
                            var dto=adapter.Read();dto.freeShape.artVersion=artVersion;dto.freeShape.seed=0;dto.freeShape.cells=points.Select(p=>new FreeShapeCell{x=p.x,y=p.y,styleId=style.styleId}).ToList();dto.revision++;
                            map.EditorApplyTerrainCandidate(adapter.Terrain,dto);
                            int width=fixture.rows[0].Length,height=fixture.rows.Length;
                            source.LoadImage(File.ReadAllBytes(Source+"/Samples/"+style.styleId+"/"+fixture.id+".png"));
                            camera.orthographicSize=height*.5f;camera.aspect=(float)width/height;camera.transform.position=new Vector3(fixture.origin.x+width*.5f,fixture.origin.y+height*.5f,-10);
                            foreach(bool runtime in new[]{false,true})
                            {
                                var root=new GameObject(runtime?"Runtime loader":"Scene authoring artwork");SceneManager.MoveGameObjectToScene(root,scene);
                                var rt=new RenderTexture(width*32,height*32,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();
                                var read=new Texture2D(width*32,height*32,TextureFormat.RGBA32,false);
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
                                    var a=read.GetPixels32();var b=source.GetPixels32();var comparison=new Comparison{style=style.styleId,fixture=fixture.id,path=runtime?"Runtime":"Scene",pixels=a.Length,solidCells=points.Count,emptyCells=width*height-points.Count};
                                    for(int i=0;i<a.Length;i++){if(a[i].a!=b[i].a)comparison.alphaDifferences++;if(a[i].a!=0&&b[i].a!=0&&(a[i].r!=b[i].r||a[i].g!=b[i].g||a[i].b!=b[i].b))comparison.rgbDifferences++;}
                                    results.comparisons.Add(comparison);
                                    // Retain every Scene render; Runtime equality is measured independently above.
                                    if(!runtime || artVersion==4 || comparison.rgbDifferences+comparison.alphaDifferences!=0)
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
            Assert.That(results.comparisons.Count,Is.EqualTo(640));
            Assert.That(results.comparisons.Sum(c=>c.rgbDifferences+c.alphaDifferences),Is.Zero,"See ArtParity.json and the retained renders.");
        }
    }
}
