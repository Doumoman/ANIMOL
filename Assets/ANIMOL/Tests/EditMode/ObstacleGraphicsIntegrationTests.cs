using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Development;
using ANIMOL.Editor;
using ANIMOL.Gameplay;
using ANIMOL.Gameplay.ObstacleGraphics;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace ANIMOL.Tests
{
    [Category("ObstacleGraphics")]
    public sealed class ObstacleGraphicsIntegrationTests
    {
        [Test] public void ActualSceneAndRuntimePathsMatchForAuthoredDevicesAt1xAnd8x()
        {
            var map=AssetDatabase.LoadAssetAtPath<StageMapDefinition>(CommonObstacleV6Builder.DevMapPath);Assert.That(map,Is.Not.Null);
            var before=File.ReadAllBytes(CommonObstacleV6Builder.DevMapPath);
            var scene=EditorSceneManager.NewPreviewScene();var cameraRoot=new GameObject("OVG integration camera");SceneManager.MoveGameObjectToScene(cameraRoot,scene);
            var camera=cameraRoot.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.orthographic=true;camera.orthographicSize=.5f;camera.aspect=2;
            camera.allowHDR=false;camera.allowMSAA=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
            var adapter=new TerrainEditorAdapter(AssetDatabase.AssetPathToGUID(CommonObstacleV6Builder.DevMapPath));
            var expected=new Dictionary<string,Color32[]>();var previous=RenderTexture.active;int comparisons=0,differences=0;
            try
            {
                foreach(bool runtime in new[]{false,true})
                {
                    var root=new GameObject(runtime?"Runtime":"Scene");SceneManager.MoveGameObjectToScene(root,scene);
                    try
                    {
                        if(runtime)
                        {
                            var loader=root.AddComponent<StageMapRuntimeLoader>();loader.enabled=false;loader.ConfigureDefinition(map,adapter.Objects,true);loader.ConfigureStandaloneDevelopmentOwner();loader.Load(map);
                            Assert.That(root.GetComponent<ObstacleRuntimeGraphics>(),Is.Not.Null);
                            Assert.That(root.GetComponentsInChildren<StageMapRuntimeObject>().Length,Is.EqualTo(10));
                        }
                        else TerrainEditorArt.BuildMap(adapter,root.transform);
                        Assert.That(root.GetComponentInChildren<FreeShapeTerrainRenderer>().GetComponentsInChildren<Collider2D>(),Is.Empty);
                        foreach(var device in map.Objects.Where(o=>ObstacleSnapshotAdapter.Kind(o.Kind)!=null))foreach(int scale in new[]{1,8})
                        {
                            camera.transform.position=new Vector3(device.X,device.Y+.5f,-10);
                            var rt=new RenderTexture(64*scale,32*scale,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();var pixels=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
                            try
                            {
                                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);pixels.Apply();var values=pixels.GetPixels32();var key=device.StableId+"-"+scale;
                                File.WriteAllBytes("Docs/Validation/CommonObstaclesV6/Renders/"+key+(runtime?"-Runtime":"-Scene")+".png",pixels.EncodeToPNG());
                                if(!runtime)expected.Add(key,values);
                                else{comparisons++;for(int i=0;i<values.Length;i++){var a=values[i];var b=expected[key][i];if(a.a!=b.a||a.a!=0&&(a.r!=b.r||a.g!=b.g||a.b!=b.b))differences++;}}
                            }
                            finally{camera.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(pixels);rt.Release();Object.DestroyImmediate(rt);}
                        }
                    }
                    finally{Object.DestroyImmediate(root);}
                }
                File.WriteAllText("Docs/Validation/CommonObstaclesV6/scene-runtime-parity.json","{\"comparisons\":"+comparisons+",\"differences\":"+differences+"}");
                Assert.That(comparisons,Is.EqualTo(20));Assert.That(differences,Is.Zero);Assert.That(File.ReadAllBytes(CommonObstacleV6Builder.DevMapPath),Is.EqualTo(before));
            }
            finally{adapter.Dispose();Object.DestroyImmediate(cameraRoot);EditorSceneManager.ClosePreviewScene(scene);RenderTexture.active=previous;}
        }
    }
}
