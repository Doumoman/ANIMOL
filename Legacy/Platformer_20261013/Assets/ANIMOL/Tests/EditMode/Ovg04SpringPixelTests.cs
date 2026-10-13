using System.Collections.Generic;
using System.IO;
using ANIMOL.Editor;
using ANIMOL.Gameplay;
using ANIMOL.Gameplay.ObstacleGraphics;
using Animol.TerrainStructure;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ANIMOL.Tests
{
    public sealed class Ovg04SpringPixelTests
    {
        [Test] public void SideAndTopNativeFramesMatchInputPngWithoutTerrainBody()
        {
            var scene=EditorSceneManager.NewPreviewScene();var previous=RenderTexture.active;
            var cameraRoot=new GameObject("OVG04 pixels");SceneManager.MoveGameObjectToScene(cameraRoot,scene);
            var camera=cameraRoot.AddComponent<Camera>();camera.scene=scene;camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);
            camera.orthographic=true;camera.orthographicSize=.5f;camera.aspect=1;camera.transform.position=new Vector3(.5f,.5f,-10);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.allowHDR=false;camera.allowMSAA=false;
            int samples=0;
            try
            {
                for(int theme=1;theme<=5;theme++)foreach(var kind in new[]{"C02","C03"})
                foreach(var facing in kind=="C02"?new[]{"LEFT","RIGHT"}:new[]{"UP"})
                for(int f=-1;f<7;f++)
                {
                    var themeId="T"+theme.ToString("D2");var pose=f<0?"idle":"active";int frame=Mathf.Max(0,f);
                    var cell=new GraphicCell{Kind=kind,ThemeId=themeId,StyleId=themeId+"_A",ArtVersion=6,Facing=facing,Pose=pose,Frame=frame,SurfaceEnabled=true};
                    var root=new GameObject("spring");SceneManager.MoveGameObjectToScene(root,scene);
                    root.AddComponent<FreeShapeTerrainRenderer>().Rebuild(new FreeShapeLayer(),1,new Dictionary<Vector2Int,GraphicCell>{{Vector2Int.zero,cell}});
                    var expected=new Texture2D(2,2);expected.LoadImage(File.ReadAllBytes(ObstacleArtV1Builder.Source+"/Art/Mechanisms/"+themeId+"_"+kind+"_"+facing+"_"+pose+"_f"+frame+".png"));
                    try
                    {
                        foreach(int scale in new[]{1,8})
                        {
                            var target=new RenderTexture(32*scale,32*scale,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                            var actual=new Texture2D(32*scale,32*scale,TextureFormat.RGBA32,false);target.Create();
                            try
                            {
                                camera.targetTexture=target;camera.Render();RenderTexture.active=target;actual.ReadPixels(new Rect(0,0,target.width,target.height),0,0);actual.Apply();
                                var wanted=expected.GetPixels32();var pixels=actual.GetPixels32();int differences=0;
                                for(int i=0;i<pixels.Length;i++){var a=pixels[i];var b=wanted[(i/(32*scale)/scale)*32+(i%(32*scale))/scale];if(a.a!=b.a||a.a!=0&&(a.r!=b.r||a.g!=b.g||a.b!=b.b))differences++;}
                                Assert.That(differences,Is.Zero,themeId+"/"+kind+"/"+facing+"/"+pose+"/"+frame+" x"+scale);samples++;
                                if(theme==1&&f<0)File.WriteAllBytes(Ovg04ValidationSetup.Evidence+"/"+kind+"-"+facing+"-native-"+scale+"x.png",actual.EncodeToPNG());
                            }
                            finally{camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(actual);}
                        }
                    }
                    finally{Object.DestroyImmediate(root);Object.DestroyImmediate(expected);}
                }
                File.WriteAllText(Ovg04ValidationSetup.Evidence+"/spring-pixel-parity.json","{\"samples\":"+samples+",\"differences\":0,\"visualVersion\":3,\"sourceTerrainArtVersion\":4,\"runtimeTerrainArtVersion\":6}");
            }
            finally{RenderTexture.active=previous;EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
