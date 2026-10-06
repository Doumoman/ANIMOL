using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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
    public sealed class ObstacleConnectionPixelTests
    {
        [TestCase(1),TestCase(2),TestCase(3),Timeout(600000)]
        public void TwentyStylesWallStepAndSeparatedConnectionsMatchSource(int geometry)
        {
            var scene=EditorSceneManager.NewPreviewScene();var cameraRoot=new GameObject("Connection camera");SceneManager.MoveGameObjectToScene(cameraRoot,scene);
            var camera=cameraRoot.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.orthographic=true;camera.orthographicSize=4;camera.aspect=2.5f;camera.transform.position=new Vector3(-7,1,-10);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.allowHDR=false;camera.allowMSAA=false;
            var previous=RenderTexture.active;int comparisons=0;long differences=0;
            try
            {
                foreach(var style in FreeShapeArtRegistry.Load().styles)
                {
                    var root=new GameObject(style.styleId);SceneManager.MoveGameObjectToScene(root,scene);var expected=new Texture2D(2,2,TextureFormat.RGBA32,false);
                    try
                    {
                        var layer=new FreeShapeLayer();var devices=new Dictionary<Vector2Int,GraphicCell>();var numbers=new[]{1,2,3,4,5,6,7,8,10};
                        for(int i=0;i<numbers.Length;i++)
                        {
                            int x=-17+i%5*4,y=-2+i/5*4;layer.cells.Add(new FreeShapeCell{x=x,y=y,styleId=style.styleId});
                            if(geometry<3){layer.cells.Add(new FreeShapeCell{x=x,y=y-1,styleId=style.styleId});layer.cells.Add(new FreeShapeCell{x=x+1,y=y-1,styleId=style.styleId});}
                            if(geometry==1)layer.cells.Add(new FreeShapeCell{x=x,y=y+1,styleId=style.styleId});
                            var kind="C"+numbers[i].ToString("D2");devices.Add(new Vector2Int(x+(geometry==3?2:1),y),new GraphicCell{Kind=kind,StyleId=style.styleId,ThemeId=style.themeId,SurfaceEnabled=true,Facing=numbers[i] is 2 or 8?"RIGHT":"UP",Pose=numbers[i]==4?"active":"idle"});
                        }
                        root.AddComponent<FreeShapeTerrainRenderer>().Rebuild(layer,1,devices);Assert.That(root.GetComponentsInChildren<Collider2D>(),Is.Empty);
                        expected.LoadImage(File.ReadAllBytes("Docs/Validation/OVG02/Expected/"+style.styleId+"-connection"+geometry+".png"));var source=expected.GetPixels32();
                        foreach(int scale in new[]{1,8})
                        {
                            var rt=new RenderTexture(640*scale,256*scale,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();var pixels=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
                            try
                            {
                                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);pixels.Apply();var actual=pixels.GetPixels32();comparisons++;
                                for(int i=0;i<actual.Length;i++){var a=actual[i];var b=source[(i/rt.width/scale)*640+i%rt.width/scale];if(a.a!=b.a||a.a!=0&&(a.r!=b.r||a.g!=b.g||a.b!=b.b))differences++;}
                                File.WriteAllBytes("Docs/Validation/OVG02/Renders/"+style.styleId+"-connection"+geometry+"-"+scale+"x.png",pixels.EncodeToPNG());
                            }finally{camera.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(pixels);rt.Release();Object.DestroyImmediate(rt);}
                        }
                    }finally{Object.DestroyImmediate(expected);Object.DestroyImmediate(root);}
                }
                File.WriteAllText("Docs/Validation/OVG02/connection"+geometry+"-parity.json","{\"comparisons\":"+comparisons+",\"differences\":"+differences+"}");
                Assert.That(comparisons,Is.EqualTo(40));Assert.That(differences,Is.Zero);
            }finally{Object.DestroyImmediate(cameraRoot);EditorSceneManager.ClosePreviewScene(scene);RenderTexture.active=previous;}
        }
    }
}
