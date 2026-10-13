using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.PortraitArtV1;
using ANIMOL.ProductionV1;
using ANIMOL.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace ANIMOL.FiveThemeMenu.Tests
{
    public sealed class CorrectionSafeAreaProbe:MonoBehaviour
    {
        public Vector2 Minimum,Maximum;
        private void LateUpdate(){var r=(RectTransform)transform;r.anchorMin=Minimum;r.anchorMax=Maximum;r.offsetMin=r.offsetMax=Vector2.zero;}
    }
    public sealed class OriginalDesignCorrectionTests
    {
        private const string Output="Docs/Validation/OriginalCorrectionV2/";
        [UnityTest] public IEnumerator Actual1920GameViewAndCampaign()=>Survey(1920);
        [UnityTest] public IEnumerator Actual2400GameViewAndCampaign()=>Survey(2400);
        private IEnumerator Survey(int height)
        {
            Directory.CreateDirectory(Output);
            SceneManager.LoadScene("Lobby");yield return Settle();
            var setup=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ANIMOL.Editor.PortraitGameViewSetup")).First(t=>t!=null);
            setup.GetMethod("SelectFixedResolution").Invoke(null,new object[]{1080,height,"Original Design Correction QA"});
            var window=UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));window.Show();window.Focus();
            yield return Settle();Assert.That(Screen.width,Is.EqualTo(1080));Assert.That(Screen.height,Is.EqualTo(height));
            var entry=Object.FindFirstObjectByType<PortraitEntryController>();var cycle=Object.FindFirstObjectByType<MenuThemeCycle>();
            var production=Object.FindFirstObjectByType<ProductionController>();production.Initialize();
            cycle.SendMessage("OnApplicationFocus",true);var evidence=new List<string>();
            Assert.That(cycle.Catalog.themes.SelectMany(t=>new[]{t.far,t.mid,t.platform,t.near}).All(t=>UnityEditor.AssetDatabase.GetAssetPath(t).StartsWith("Assets/ANIMOL/UI/OriginalDesignCorrectionV2/")),Is.True);
            Assert.That(cycle.Output.raycastTarget,Is.False);
            // Actual running time: do not advance InspectionTime to stand in for a cycle.
            var seen=new HashSet<int>();var directions=new HashSet<int>();var frames=new HashSet<int>();double start=cycle.Elapsed;float real=Time.realtimeSinceStartup;
            while(cycle.Elapsed-start<25.2)
            {
                // Keep the automated Editor survey in the foreground state. Real focus-loss
                // pause behavior remains covered by MenuThemeFlowTests and is not changed.
                if(!(bool)typeof(MenuThemeCycle).GetField("focused",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(cycle))cycle.SendMessage("OnApplicationFocus",true);
                Assert.That(Time.realtimeSinceStartup-real,Is.LessThan(40),"Uninterrupted background clock");
                seen.Add(cycle.ThemeIndex);directions.Add(cycle.RabbitDirection);frames.Add(cycle.FrameIndex);
                yield return null;
            }
            Assert.That(seen.Count,Is.EqualTo(5));Assert.That(directions.Count,Is.EqualTo(2));Assert.That(frames.Count,Is.EqualTo(8));
            evidence.Add("PASS live 25.2+ seconds, all five themes, both rabbit directions, eight frames");
            foreach(int theme in Enumerable.Range(0,5))foreach(int cam in new[]{-1,1})foreach(int rabbit in new[]{-1,1})
            {
                cycle.InspectionCameraDirection=cam;cycle.InspectionRabbitDirection=rabbit;cycle.InspectionFrame=0;cycle.InspectionTime=theme*5+2.5;
                yield return Settle();
                SaveNative(cycle,$"Native_T0{theme+1}_c{cam}_r{rabbit}");
                if(cam==1 && rabbit==1)yield return Capture(height+"_Lobby_T0"+(theme+1));
            }
            cycle.InspectionCameraDirection=null;cycle.InspectionRabbitDirection=null;cycle.InspectionFrame=null;cycle.InspectionTime=null;
            var safe=production.GetComponentInChildren<SafeAreaLayout>();var probe=safe.gameObject.AddComponent<CorrectionSafeAreaProbe>();probe.enabled=false;
            try
            {
                foreach(bool inset in new[]{false,true})
                {
                    safe.enabled=true;safe.Apply();probe.enabled=false;
                    if(inset){safe.enabled=false;probe.Minimum=new Vector2(48f/1080,96f/height);probe.Maximum=new Vector2(1-48f/1080,1-120f/height);probe.enabled=true;}
                    Click(entry.ModeScreen.Campaign);yield return Settle();
                    Assert.That(production.Navigation.CurrentScreenId,Is.EqualTo(ProductionController.Screens[0]));Assert.That(cycle.IsVisible,Is.False);
                    var root=production.transform.Find("SafeArea/ScreenHost/SC02_ThemeSelect/ProductionV1");
                    var papers=root.GetComponentsInChildren<Image>().Where(i=>i.name=="ThemePaper").ToArray();Assert.That(papers.Length,Is.EqualTo(5));
                    foreach(var p in papers)Assert.That((Color32)p.color,Is.EqualTo(new Color32(244,244,244,255)));
                    foreach(var label in root.GetComponentsInChildren<TextMeshProUGUI>())Assert.That((Color32)label.color,Is.EqualTo(new Color32(26,28,44,255)));
                    var scroll=root.GetComponentInChildren<ScrollRect>();var progression=(CampaignProgressionService)typeof(ProductionController).GetField("progression",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(production);
                    for(int i=0;i<5;i++)
                    {
                        var theme=production.Catalog.Campaign.Themes[i];var text=root.GetComponentsInChildren<TextMeshProUGUI>().Single(t=>t.name=="ThemeData_"+i);
                        Assert.That(text.text,Does.Contain(theme.DisplayName));Assert.That(text.text,Does.Contain(theme.Stages.Count(s=>s!=null&&progression.Progress.IsCompleted(s.StageId))+"/"+theme.Stages.Count));
                        var thumb=root.GetComponentsInChildren<Image>().Single(im=>im.name=="Thumb_Theme_"+theme.ThemeId);Assert.That(thumb.sprite,Is.SameAs(production.Catalog.Find("Thumb_Theme_"+theme.ThemeId)));
                        scroll.verticalNormalizedPosition=1-i/4f;yield return Settle();
                        if(i==0 || i==4)yield return Capture(height+"_SC02_"+(inset?"safe_":"full_")+i);
                        var button=root.GetComponentsInChildren<Button>().Single(b=>b.name=="PV1_Theme_"+i);Click(button);yield return Settle();
                        Assert.That(production.SelectedTheme,Is.EqualTo(i));Assert.That(production.Navigation.CurrentScreenId,Is.EqualTo(ProductionController.Screens[1]));
                        Click(production.GetComponentsInChildren<Button>().Single(b=>b.name=="PV1_SC03_StageSelectBack"));yield return Settle();
                        Assert.That(production.Navigation.CurrentScreenId,Is.EqualTo(ProductionController.Screens[0]));
                    }
                    Click(root.GetComponentsInChildren<Button>().Single(b=>b.name=="PV1_SC02_ThemeSelectBack"));yield return Settle();
                    Assert.That(production.Navigation.CurrentScreenId,Is.EqualTo(PortraitEntryController.ModeId));
                    evidence.Add("PASS "+(inset?"simulated Safe Area":"full viewport")+": five theme data/art IDs, scroll, pointer raycasts, SC03/back/lobby");
                    Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
                }
            }
            finally{probe.enabled=false;Object.Destroy(probe);safe.enabled=true;safe.Apply();cycle.InspectionTime=null;}
            File.WriteAllLines(Output+height+"-checks.txt",evidence);
        }
        private static IEnumerator Settle(){yield return new WaitForSecondsRealtime(.45f);Canvas.ForceUpdateCanvases();}
        private static void Click(Button button)
        {
            Assert.That(button.IsInteractable(),Is.True,button.name);Canvas.ForceUpdateCanvases();
            var rect=(RectTransform)button.transform;var position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var data=new PointerEventData(EventSystem.current){position=position,button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Assert.That(hits.Count,Is.GreaterThan(0),button.name);Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.SameAs(button),button.name+" raycast");
            ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
        }
        private static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();
            try{File.WriteAllBytes(Output+name+".png",texture.EncodeToPNG());}finally{Object.Destroy(texture);}
        }
        private static void SaveNative(MenuThemeCycle cycle,string name)
        {
            var previous=RenderTexture.active;var texture=new Texture2D(352,704,TextureFormat.RGBA32,false);
            try{RenderTexture.active=cycle.NativeFrame;texture.ReadPixels(new Rect(0,0,352,704),0,0);texture.Apply();File.WriteAllBytes(Output+name+".png",texture.EncodeToPNG());Assert.That(texture.GetPixels32().All(c=>c.a==255),Is.True);}
            finally{RenderTexture.active=previous;Object.Destroy(texture);}
        }
    }
}
