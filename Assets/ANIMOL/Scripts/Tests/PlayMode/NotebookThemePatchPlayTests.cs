using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using William.NotebookUI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace ANIMOL.Presentation.Tests
{
    public sealed class NotebookThemePatchPlayTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        NotebookRoot root;
        string wallet,preferences; bool hadWallet,hadPreferences;
        [SetUp] public void PreserveSaves()
        {
            hadWallet=PlayerPrefs.HasKey("WilliamNotebookUI.Wallet"); wallet=PlayerPrefs.GetString("WilliamNotebookUI.Wallet");
            hadPreferences=PlayerPrefs.HasKey("WilliamNotebookUI.Preferences"); preferences=PlayerPrefs.GetString("WilliamNotebookUI.Preferences");
        }
        [UnityTearDown] public IEnumerator RestoreSaves()
        {
            if(root) UnityEngine.Object.Destroy(root.GetComponentInParent<Canvas>().rootCanvas.gameObject);
            yield return null;
            if(hadWallet) PlayerPrefs.SetString("WilliamNotebookUI.Wallet",wallet);else PlayerPrefs.DeleteKey("WilliamNotebookUI.Wallet");
            if(hadPreferences) PlayerPrefs.SetString("WilliamNotebookUI.Preferences",preferences);else PlayerPrefs.DeleteKey("WilliamNotebookUI.Preferences");
            PlayerPrefs.Save();
        }
        IEnumerator Load()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/Main/NotebookUI.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            root=UnityEngine.Object.FindFirstObjectByType<NotebookRoot>(); Assert.That(root,Is.Not.Null);
            Send(root,"OnApplicationFocus",true);
#else
            Assert.Ignore("Actual Editor scene required");yield break;
#endif
        }
        static void Send(object target,string method,object value)=>target.GetType().GetMethod(method,Private).Invoke(target,new[]{value});
        NotebookBmModel Model=>(NotebookBmModel)typeof(NotebookRoot).GetField("model",Private).GetValue(root);
        static bool Running(NotebookMapThemeNavigation nav,string field)=>typeof(NotebookMapThemeNavigation).GetField(field,Private).GetValue(nav)!=null;
        static void NoHiddenInput(NotebookMapThemeNavigation nav)
        {
            Assert.That(nav.rail.gameObject.activeSelf,Is.False);
            var group=nav.rail.GetComponent<CanvasGroup>(); Assert.That(group.interactable||group.blocksRaycasts,Is.False);
            Assert.That(nav.buttons.All(b=>!b.interactable),Is.True);
        }
        [UnityTest] public IEnumerator EntrySamplesOncePerPlaybackAndChangesAcrossActualCampaignClicks()
        {
            yield return Load(); var transition=root.campaignSkyTransition; string previous="";
            for(int i=0;i<6;i++)
            {
                root.OpenMain(); var random=JsonUtility.ToJson(UnityEngine.Random.state);
                root.campaignButton.onClick.Invoke(); Assert.That(transition.IsPlaying,Is.True);
                var signature=transition.LinkSignature; Assert.That(signature.Split(',').Length,Is.EqualTo(8));
                Assert.That(signature,Is.Not.EqualTo(previous));previous=signature;
                Assert.That(JsonUtility.ToJson(UnityEngine.Random.state),Is.EqualTo(random));
                var material=(Material)typeof(NotebookCampaignSkyTransition).GetField("ownedMaterial",Private).GetValue(transition);
                var links=Enumerable.Range(0,8).Select(n=>material.GetVector("_Link"+n)).ToArray();
                yield return new WaitForSecondsRealtime(.15f);
                Assert.That(transition.LinkSignature,Is.EqualTo(signature));
                CollectionAssert.AreEqual(links,Enumerable.Range(0,8).Select(n=>material.GetVector("_Link"+n)).ToArray());
                transition.CompleteImmediately(); Assert.That(root.stageScreen.activeSelf,Is.True);
                Assert.That(root.mapThemeNavigation.RailVisible,Is.True); Assert.That(root.stageSheet.gameObject.activeSelf,Is.False);
            }
        }
        [UnityTest] public IEnumerator ThemeBrowsingRetargetsCancelsManualInputAndPreservesProgress()
        {
            yield return Load(); root.OpenStages(); yield return null;var nav=root.mapThemeNavigation;
            var state=Model.State; var entries=state.entryIds.ToArray();var rewards=state.rewardIds.ToArray();
            var clears=state.clearedStageIds.ToArray();var owned=state.ownedThemes.ToArray();int balance=state.balance;
            string selected=root.SelectedStageId;var catalog=Model.Catalog;
            for(int i=0;i<4;i++)
            {
                nav.buttons[i].onClick.Invoke(); yield return new WaitForSecondsRealtime(.48f);
                float max=Mathf.Max(0,nav.scroll.content.rect.height-nav.scroll.viewport.rect.height);
                Assert.That(nav.scroll.content.anchoredPosition.y,Is.EqualTo(Mathf.Clamp(catalog.themes[i].startY,0,max)).Within(1));
                Assert.That(nav.ActiveTheme,Is.EqualTo(i));Assert.That(nav.buttons[i].image.sprite,Is.EqualTo(nav.selectedSprite));
            }
            nav.NavigateToTheme(0);yield return new WaitForSecondsRealtime(.1f);
            float current=nav.scroll.content.anchoredPosition.y;nav.NavigateToTheme(2);
            Assert.That(nav.scroll.content.anchoredPosition.y,Is.EqualTo(current).Within(1));
            yield return new WaitForSecondsRealtime(.48f);Assert.That(nav.ActiveTheme,Is.EqualTo(2));
            nav.NavigateToTheme(0);yield return new WaitForSecondsRealtime(.08f);
            ExecuteEvents.Execute<IBeginDragHandler>(nav.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.beginDragHandler);
            Assert.That(Running(nav,"scrollRoutine"),Is.False);Assert.That(nav.scroll.inertia,Is.True);
            ExecuteEvents.Execute<IEndDragHandler>(nav.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.endDragHandler);
            nav.NavigateToTheme(3);yield return null;
            ExecuteEvents.Execute<IScrollHandler>(nav.gameObject,new PointerEventData(EventSystem.current){scrollDelta=new Vector2(0,1)},ExecuteEvents.scrollHandler);
            Assert.That(Running(nav,"scrollRoutine"),Is.False);
            nav.scroll.content.anchoredPosition=new Vector2(0,catalog.themes[1].startY+100);yield return null;
            Assert.That(nav.ActiveTheme,Is.EqualTo(1));
            nav.NavigateToTheme(3);root.SetReducedMotion(true);
            Assert.That(nav.scroll.content.anchoredPosition.y,Is.EqualTo(catalog.themes[3].startY).Within(1));
            Assert.That(Running(nav,"scrollRoutine"),Is.False);
            nav.NavigateToTheme(0);Assert.That(nav.scroll.content.anchoredPosition.y,Is.Zero);
            Assert.That(root.SelectedStageId,Is.EqualTo(selected));Assert.That(root.MapSelectionVisible,Is.False);
            CollectionAssert.AreEqual(entries,state.entryIds);CollectionAssert.AreEqual(rewards,state.rewardIds);
            CollectionAssert.AreEqual(clears,state.clearedStageIds);CollectionAssert.AreEqual(owned,state.ownedThemes);
            Assert.That(state.balance,Is.GreaterThanOrEqualTo(balance));
        }
        [UnityTest] public IEnumerator SelectionWaitsForTabsAndInterruptionsLeaveNoCallbacksOrHiddenRaycasts()
        {
            yield return Load();root.OpenStages();yield return null;var nav=root.mapThemeNavigation;
            root.nodes[0].button.onClick.Invoke();Assert.That(nav.buttons.All(b=>!b.interactable),Is.True);
            Assert.That(root.stageSheet.gameObject.activeSelf,Is.False);
            yield return new WaitForSecondsRealtime(.08f);
            Assert.That(nav.rail.anchoredPosition.y,Is.GreaterThan(24));Assert.That(root.stageSheet.gameObject.activeSelf,Is.False);
            root.nodes[1].button.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.23f);
            NoHiddenInput(nav);Assert.That(root.stageSheet.gameObject.activeSelf,Is.True);
            yield return new WaitForSecondsRealtime(.24f);Assert.That(root.stageSheet.anchoredPosition.y,Is.EqualTo(24));
            root.nodes[2].button.onClick.Invoke();Assert.That(root.SelectedStageId,Is.EqualTo(root.nodes[2].stageId));
            Assert.That(root.stageSheet.anchoredPosition.y,Is.EqualTo(24));Assert.That(Running(nav,"railRoutine"),Is.False);
            root.HideStageSheet();Assert.That(root.stageSheet.gameObject.activeSelf,Is.False);
            yield return new WaitForSecondsRealtime(.22f);Assert.That(nav.RailVisible,Is.True);
            foreach(string interruption in new[]{"focus","pause","exit","disable","reduce","mapDisable"})
            {
                root.OpenStages();root.nodes[0].button.onClick.Invoke();
                if(interruption=="focus") {Send(root,"OnApplicationFocus",false);Send(nav,"OnApplicationFocus",false);Send(root,"OnApplicationFocus",true);}
                if(interruption=="pause") {Send(root,"OnApplicationPause",true);Send(nav,"OnApplicationPause",true);Send(root,"OnApplicationPause",false);}
                if(interruption=="exit") root.OpenMain();
                if(interruption=="disable") {root.enabled=false;root.enabled=true;}
                if(interruption=="reduce") root.SetReducedMotion(true);
                if(interruption=="mapDisable") root.stageScreen.SetActive(false);
                yield return new WaitForSecondsRealtime(.55f);
                Assert.That(Running(nav,"railRoutine")||Running(nav,"scrollRoutine"),Is.False,interruption);
                if(root.stageSheet.gameObject.activeInHierarchy) NoHiddenInput(nav);
                else if(!nav.RailVisible) NoHiddenInput(nav);
                root.SetReducedMotion(false);root.OpenStages();yield return null;
                Assert.That(root.stageSheet.gameObject.activeSelf,Is.False,interruption);Assert.That(nav.RailVisible,Is.True,interruption);
            }
        }
        [UnityTest] public IEnumerator ActualGameViewButtonsAndSheetFitAtFourResolutionsAndSimulatedNotch()
        {
#if UNITY_EDITOR
            yield return Load();root.SetReducedMotion(true);root.OpenStages();
            string folder=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Temp/RandomLinksThemeTabsQA");Directory.CreateDirectory(folder);
            var setup=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ANIMOL.Presentation.Editor.PortraitGameView")).First(t=>t!=null);
            foreach(var size in new[]{new Vector2Int(360,640),new Vector2Int(390,844),new Vector2Int(445,773),new Vector2Int(1440,900)})
            {
                if(size.x<size.y) setup.GetMethod("SelectFixedResolution",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,new object[]{size.x,size.y,"Theme tabs QA"});
                else typeof(NotebookUiPlayTests).GetMethod("SelectWidePcResolution",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{size.x,size.y});
                yield return null;yield return null;Canvas.ForceUpdateCanvases();
                Assert.That(Screen.width,Is.EqualTo(size.x));Assert.That(Screen.height,Is.EqualTo(size.y));
                root.HideStageSheet();
                foreach(var button in root.mapThemeNavigation.buttons) Inside((RectTransform)button.transform,new Rect(0,0,size.x,size.y));
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"Tabs_"+size.x+"x"+size.y+".png"));yield return null;
                root.nodes[0].button.onClick.Invoke();Inside(root.stageSheet,new Rect(0,0,size.x,size.y));NoHiddenInput(root.mapThemeNavigation);
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"Sheet_"+size.x+"x"+size.y+".png"));yield return null;
            }
            // Exercise the same existing safe-area geometry with a bottom home indicator and top notch.
            setup.GetMethod("SelectFixedResolution",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,new object[]{390,844,"Theme tabs notch QA"});yield return null;yield return null;
            var layout=root.GetComponentInParent<NotebookCanvasLayout>();var safe=new Rect(0,34,390,763);
            float scale=Mathf.Min(safe.width/1080f,safe.height/1500f);layout.scaler.scaleFactor=scale;
            layout.design.sizeDelta=new Vector2(1080,safe.height/scale);layout.design.anchoredPosition=(safe.center-new Vector2(195,422))/scale;
            layout.mapScreen.sizeDelta=new Vector2(safe.width/scale,safe.height/scale);Canvas.ForceUpdateCanvases();
            root.HideStageSheet();foreach(var button in root.mapThemeNavigation.buttons) Inside((RectTransform)button.transform,safe);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"Tabs_NotchSimulated_390x844.png"));yield return null;
            root.nodes[0].button.onClick.Invoke();Inside(root.stageSheet,safe);NoHiddenInput(root.mapThemeNavigation);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"Sheet_NotchSimulated_390x844.png"));yield return null;
            layout.RefreshLayout();
#else
            Assert.Ignore("Editor GameView required");yield break;
#endif
        }
        static void Inside(RectTransform rect,Rect bounds)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            foreach(var corner in corners)
            {
                var point=RectTransformUtility.WorldToScreenPoint(null,corner);
                Assert.That(point.x,Is.InRange(bounds.xMin-1,bounds.xMax+1),rect.name);
                Assert.That(point.y,Is.InRange(bounds.yMin-1,bounds.yMax+1),rect.name);
            }
        }
    }
}
