#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Animol.Starway;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace WilliamParadox.Story.Tests.PlayMode
{
    public sealed class StoryCrayonPlayTests
    {
        const string AssetsRoot="Assets/ANIMOL/Art/UI/Story/Crayon/";
        const string Output="Docs/Validation/StoryCrayon";
        static string SaveKey=>StorySaveStore.Key("william-paradox.ranch.dialogue-preview");
        bool hadSave;string save;StoryRunner runner;
        [SetUp] public void Preserve(){hadSave=PlayerPrefs.HasKey(SaveKey);save=PlayerPrefs.GetString(SaveKey);}
        [UnityTearDown] public IEnumerator Restore()
        {
            if(runner){runner.persistProgress=false;Object.Destroy(runner.gameObject);}yield return null;
            if(hadSave)PlayerPrefs.SetString(SaveKey,save);else PlayerPrefs.DeleteKey(SaveKey);PlayerPrefs.Save();
        }
        static Image Outline(StoryCrayonFrame frame)=>frame.transform.Find("CrayonOutline").GetComponent<Image>();
        void ArmHostInput()
        {
            // Test dispatch independently of which desktop app currently has OS focus.
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(StoryRunner).GetField("hasFocus",flags).SetValue(runner,true);
            typeof(StoryRunner).GetField("inputReadyAt",flags).SetValue(runner,0f);
            typeof(StoryRunner).GetField("lastInputFrame",flags).SetValue(runner,-1);
            typeof(StoryRunner).GetField("lastInputTime",flags).SetValue(runner,-100f);
            runner.view.SetPaused(runner.IsPaused);
        }
        [UnityTest] public IEnumerator ActualHostPreservesPointerTargetsAndConnectsExternalPopupReducedMotion()
        {
            var setup=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ANIMOL.Presentation.Editor.PortraitGameView")).First(t=>t!=null);
            setup.GetMethod("SelectFixedResolution",BindingFlags.Static|BindingFlags.Public).Invoke(null,new object[]{1080,1920,"Story crayon QA"});
            EditorSceneManager.LoadSceneInPlayMode("Assets/WilliamParadoxStoryV14/Scenes/RanchStoryPreview.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;yield return null;
            runner=Object.FindFirstObjectByType<StoryRunner>();runner.persistProgress=false;runner.SetReducedMotion(true);runner.StartStory(false);
            ArmHostInput();
            var view=runner.view.dialogueBridge.dialogue;var frames=runner.GetComponentsInChildren<StoryCrayonFrame>(true);
            Assert.That(frames.Length,Is.EqualTo(9));
            foreach(var frame in frames)
            {
                var image=Outline(frame);Assert.That(image.raycastTarget,Is.False);Assert.That(image.type,Is.EqualTo(Image.Type.Sliced));
                Assert.That(image.material.name,Does.EndWith("_Still"));
                Assert.That(frame.transform.Cast<Transform>().Count(t=>t.name=="CrayonOutline"),Is.EqualTo(1));
            }
            Assert.That(view.transform.Find("SafeArea/DialoguePanel").GetComponent<RectTransform>().anchoredPosition.y,Is.EqualTo(20));
            Assert.That(view.SkipButton.GetComponentInParent<StoryCrayonFrame>(),Is.Not.Null);
            Assert.That(view.GetComponentsInChildren<StoryNeonPanel>(true).All(p=>!p.enabled),Is.True);
            var skip=view.SkipButton;var originalTarget=skip.targetGraphic;var rect=(RectTransform)skip.transform;
            var position=rect.anchoredPosition;var size=rect.sizeDelta;Canvas.ForceUpdateCanvases();
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(Vector3.zero))};
            var hits=new List<RaycastResult>();skip.GetComponentInParent<Canvas>().GetComponent<GraphicRaycaster>().Raycast(pointer,hits);
            Assert.That(hits.Any(h=>h.gameObject==originalTarget.gameObject),Is.True);
            Assert.That(hits.Any(h=>h.gameObject.name=="CrayonOutline"||h.gameObject.name=="CrayonQuietFill"),Is.False);
            var border=Outline(skip.GetComponent<StoryCrayonFrame>());
            ExecuteEvents.Execute(skip.gameObject,pointer,ExecuteEvents.pointerDownHandler);Assert.That(border.sprite.name,Is.EqualTo("Utility_Pressed_A"));
            ExecuteEvents.Execute(skip.gameObject,pointer,ExecuteEvents.pointerUpHandler);
            skip.interactable=false;yield return null;Assert.That(border.sprite.name,Is.EqualTo("Utility_Disabled_A"));skip.interactable=true;
            Assert.That(skip.targetGraphic,Is.SameAs(originalTarget));Assert.That(rect.anchoredPosition,Is.EqualTo(position));Assert.That(rect.sizeDelta,Is.EqualTo(size));
            yield return Capture(skip.GetComponentInParent<Canvas>().rootCanvas,"R01_1080x1920");
            var lore=runner.GetComponentInChildren<RanchOptionalLore>(true);lore.Open("LORE_CLOCK");yield return null;
            Assert.That(lore.panel.transform.Find("Backdrop").GetComponent<Image>().color.a,Is.Zero,"Old popup fill must not cover the crayon frame");
            Assert.That(lore.panel.activeSelf,Is.True);yield return Capture(skip.GetComponentInParent<Canvas>().rootCanvas,"HostEnvironmentPopup");lore.Close();
            view.SetReducedMotion(false);foreach(var frame in frames)Assert.That(Outline(frame).material.name,Does.EndWith("_Animated"));
            view.SetReducedMotion(true);foreach(var frame in frames)Assert.That(Outline(frame).material.name,Does.EndWith("_Still"));
            ArmHostInput();int clicks=0;skip.onClick.AddListener(()=>clicks++);ExecuteEvents.Execute(skip.gameObject,pointer,ExecuteEvents.pointerClickHandler);Assert.That(clicks,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator NineSliceStoryShaderClipsThroughBothMaskTypesAndCanvasGroup()
        {
            foreach(bool stencil in new[]{false,true})
            {
                var root=new GameObject("Story mask QA",typeof(Canvas));var cameraObject=new GameObject("Story mask QA camera",typeof(Camera));
                var camera=cameraObject.GetComponent<Camera>();var target=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);
                camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<29;camera.transform.position=new Vector3(0,0,-10);
                var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                var mask=new GameObject("Mask",typeof(RectTransform)).GetComponent<RectTransform>();mask.SetParent(root.transform,false);mask.sizeDelta=new Vector2(128,128);
                if(stencil){mask.gameObject.AddComponent<Image>();mask.gameObject.AddComponent<Mask>().showMaskGraphic=false;}else mask.gameObject.AddComponent<RectMask2D>();
                var image=new GameObject("Nine-slice outline",typeof(RectTransform),typeof(Image)).GetComponent<Image>();image.transform.SetParent(mask,false);image.rectTransform.sizeDelta=new Vector2(256,100);
                image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(AssetsRoot+"Sprites/PanelFrame_A.png");image.material=AssetDatabase.LoadAssetAtPath<Material>(AssetsRoot+"Materials/StoryCrayon_PanelFrame_Still.mat");image.type=Image.Type.Sliced;image.raycastTarget=false;
                var group=image.gameObject.AddComponent<CanvasGroup>();foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=29;
                try
                {
                    yield return null;Canvas.ForceUpdateCanvases();camera.Render();var pixels=Read(target);int visible=0,outside=0;
                    for(int y=0;y<256;y++)for(int x=0;x<256;x++)if(pixels[y*256+x].a>2){visible++;if(x<63||x>192||y<63||y>192)outside++;}
                    Assert.That(visible,Is.GreaterThan(10));Assert.That(outside,Is.Zero,stencil?"Mask":"RectMask2D");
                    group.alpha=0;Canvas.ForceUpdateCanvases();camera.Render();Assert.That(Read(target).Count(c=>c.a>2),Is.Zero);
                }
                finally{camera.targetTexture=null;Object.Destroy(root);Object.Destroy(cameraObject);target.Release();Object.Destroy(target);}
                yield return null;
            }
        }
        static Color32[] Read(RenderTexture target)
        {
            var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
            try{image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();return image.GetPixels32();}
            finally{RenderTexture.active=previous;Object.Destroy(image);}
        }
        static IEnumerator Capture(Canvas canvas,string name)
        {
            var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldPlane=canvas.planeDistance;
            var cameraObject=new GameObject("Story screenshot camera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.transform.position=new Vector3(0,0,-10);
            var target=new RenderTexture(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;
            var previous=RenderTexture.active;Texture2D image=null;
            try
            {
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;yield return null;Canvas.ForceUpdateCanvases();yield return null;camera.Render();RenderTexture.active=target;
                image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
                Directory.CreateDirectory(Output);File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());
            }
            finally{canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;RenderTexture.active=previous;if(image)Object.Destroy(image);Object.Destroy(target);Object.Destroy(cameraObject);}
        }
    }
}
#endif
