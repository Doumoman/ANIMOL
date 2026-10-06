using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Gameplay;
using ANIMOL.Gameplay.ObstacleGraphics;
using Animol.TerrainStructure;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace ANIMOL.Tests
{
    [Category("ObstacleGraphics")]
    public sealed class ObstacleGraphicsPixelTests
    {
        [Serializable] private class Sample {public string style;public int scale,pixels,differences,renderers;public double milliseconds;}
        [Serializable] private class Report {public List<Sample> samples=new();}
        [Test,Timeout(600000)] public void AllTwentyStylesTenGraphicKindsMatchSourceAt1xAnd8x()
        {
            const string output="Docs/Validation/ObstacleGraphicsV1";Directory.CreateDirectory(output+"/Renders");
            var report=new Report();var scene=EditorSceneManager.NewPreviewScene();var previous=RenderTexture.active;
            var cameraRoot=new GameObject("OVG pixel camera");SceneManager.MoveGameObjectToScene(cameraRoot,scene);
            var camera=cameraRoot.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.orthographic=true;camera.orthographicSize=3;camera.aspect=2.5f;
            camera.transform.position=new Vector3(-9.5f,1,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.allowHDR=false;camera.allowMSAA=false;
            try
            {
                foreach(var style in FreeShapeArtRegistry.Load().styles)
                {
                    var root=new GameObject(style.styleId);SceneManager.MoveGameObjectToScene(root,scene);var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
                    try
                    {
                        var layer=new FreeShapeLayer();var devices=new Dictionary<Vector2Int,GraphicCell>();
                        for(int i=0;i<10;i++)
                        {
                            int x=-17+(i%5)*3,y=-2+(i/5)*3;
                            layer.cells.Add(new FreeShapeCell{x=x,y=y,styleId=style.styleId});var kind="C"+(i+1).ToString("D2");
                            devices.Add(new Vector2Int(x+1,y),new GraphicCell{Kind=kind,StyleId=style.styleId,ThemeId=style.themeId,SurfaceEnabled=true,Facing=kind is "C02" or "C08"?"RIGHT":"UP",Pose=kind=="C09"?"active":"idle"});
                        }
                        var renderer=root.AddComponent<FreeShapeTerrainRenderer>();renderer.Rebuild(layer,1,devices);
                        source.LoadImage(File.ReadAllBytes(output+"/Expected/"+style.styleId+".png"));var expected=source.GetPixels32();
                        foreach(int scale in new[]{1,8})
                        {
                            var rt=new RenderTexture(480*scale,192*scale,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();var read=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
                            try
                            {
                                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);read.Apply();var pixels=read.GetPixels32();
                                var sample=new Sample{style=style.styleId,scale=scale,pixels=pixels.Length,renderers=root.GetComponentsInChildren<Renderer>().Length,milliseconds=renderer.LastMilliseconds};
                                for(int i=0;i<pixels.Length;i++){var b=expected[(i/rt.width/scale)*480+(i%rt.width)/scale];var a=pixels[i];if(a.a!=b.a||a.a!=0&&(a.r!=b.r||a.g!=b.g||a.b!=b.b))sample.differences++;}
                                report.samples.Add(sample);File.WriteAllBytes(output+"/Renders/"+style.styleId+"-"+scale+"x.png",read.EncodeToPNG());
                            }
                            finally{camera.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(read);rt.Release();Object.DestroyImmediate(rt);}
                        }
                    }
                    finally{Object.DestroyImmediate(root);Object.DestroyImmediate(source);}
                }
            }
            finally{File.WriteAllText(output+"/pixel-parity.json",JsonUtility.ToJson(report,true));Object.DestroyImmediate(cameraRoot);EditorSceneManager.ClosePreviewScene(scene);RenderTexture.active=previous;}
            Assert.That(report.samples.Count,Is.EqualTo(40));Assert.That(report.samples.Sum(s=>s.differences),Is.Zero);
        }
    }
}
