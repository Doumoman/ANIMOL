using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.PortraitArtV1;
using ANIMOL.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace ANIMOL.MissingUiV1.Project.Tests
{
    public sealed class MissingUtilityFlowTests
    {
        private UiNavigationService nav;
        private UiModalStack modals;
        private Dictionary<string,(bool present,float value)> floats;
        private Dictionary<string,(bool present,int value)> ints;
        private bool oldBackground;
        private const string Prefix="ANIMOL.M6.";
        private MissingUtilityView View(MissingUtilityScreen screen)=>nav.GetComponentsInChildren<MissingUtilityView>(true).Single(v=>v.Screen==screen);
        [UnitySetUp] public IEnumerator Load()
        {
            floats=new[]{"ControlSize","ControlOffset","ControlOpacity"}.ToDictionary(k=>k,k=>(PlayerPrefs.HasKey(Prefix+k),PlayerPrefs.GetFloat(Prefix+k)));
            ints=new[]{"LargeText","Vibration","Language","Handedness","DedicatedDrop"}.ToDictionary(k=>k,k=>(PlayerPrefs.HasKey(Prefix+k),PlayerPrefs.GetInt(Prefix+k)));
            oldBackground=Application.runInBackground;Application.runInBackground=true;
            SceneManager.LoadScene("Lobby");yield return Settle();
            nav=Object.FindFirstObjectByType<UiNavigationService>();modals=nav.GetComponent<UiModalStack>();
            nav.Navigate("SC01_Lobby",false);Object.FindFirstObjectByType<PortraitEntryController>().ModeScreen.Settings.onClick.Invoke();yield return Settle();
            Assert.That(nav.CurrentScreenId,Is.EqualTo("SC13_Settings"));Assert.IsTrue(View(MissingUtilityScreen.Settings).isActiveAndEnabled);
        }
        [UnityTearDown] public IEnumerator Restore()
        {
            foreach(var kv in floats){if(kv.Value.present)PlayerPrefs.SetFloat(Prefix+kv.Key,kv.Value.value);else PlayerPrefs.DeleteKey(Prefix+kv.Key);}
            foreach(var kv in ints){if(kv.Value.present)PlayerPrefs.SetInt(Prefix+kv.Key,kv.Value.value);else PlayerPrefs.DeleteKey(Prefix+kv.Key);}
            PlayerPrefs.Save();MissingUtilityView.ApplyPreferences();Application.runInBackground=oldBackground;yield return null;
        }
        [UnityTest] public IEnumerator RealSettingsRoutesAndUnavailableServices()
        {
            var settings=View(MissingUtilityScreen.Settings);
            Assert.IsFalse(settings.Bgm.interactable);Assert.IsFalse(settings.Sfx.interactable);Assert.IsFalse(settings.Mute.interactable);Assert.IsFalse(settings.ResetAll.interactable);
            Assert.IsFalse(settings.Bgm.handleRect.gameObject.activeSelf);Assert.IsFalse(settings.Mute.transform.Find("Track").gameObject.activeSelf);
            settings.Controls.onClick.Invoke();yield return Settle();Assert.That(nav.CurrentScreenId,Is.EqualTo("SC19_ControlSettings"));
            View(MissingUtilityScreen.Controls).Back.onClick.Invoke();Assert.That(nav.CurrentScreenId,Is.EqualTo("SC13_Settings"));
            settings.Accessibility.onClick.Invoke();yield return Settle();Assert.That(View(MissingUtilityScreen.Controls).Body.verticalNormalizedPosition,Is.LessThan(.05f));
            View(MissingUtilityScreen.Controls).Back.onClick.Invoke();settings.Account.onClick.Invoke();yield return Settle();
            var account=View(MissingUtilityScreen.Account);
            Assert.IsFalse(account.Google.interactable);Assert.IsFalse(account.ChooseLocal.interactable);Assert.IsFalse(account.ChooseCloud.interactable);
            foreach(var name in new[]{"LocalRecord","CloudRecord","AccountData"})
                Assert.That(MissingStoreView.Find<Text>(account.transform,name).text,Does.Contain("--"));
            Assert.That(account.Identity.Korean,Does.Contain("게스트"));account.Back.onClick.Invoke();
            settings.Help.onClick.Invoke();Assert.That(nav.CurrentScreenId,Is.EqualTo("SC14_Help"));nav.Back();
            settings.Records.onClick.Invoke();Assert.That(nav.CurrentScreenId,Is.EqualTo("SC17_ProfileRecords"));nav.Back();
            settings.Back.onClick.Invoke();Assert.That(nav.CurrentScreenId,Is.EqualTo("SC01_Lobby"));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator PreferenceWidgetsPersistAcrossSceneReloadAndPreviewUsesActualHudRules()
        {
            View(MissingUtilityScreen.Settings).Controls.onClick.Invoke();yield return Settle();var c=View(MissingUtilityScreen.Controls);
            c.Size.value=1.31f;c.Position.value=-.07f;c.Opacity.value=.41f;c.LeftHand.isOn=true;c.Drop.isOn=true;c.LargeText.isOn=true;c.Vibration.isOn=false;
            MobileControlPreferences.Language=UiLanguage.Korean;MissingUtilityView.ApplyPreferences();c.Language.onClick.Invoke();yield return Settle();
            Assert.That(PlayerPrefs.GetFloat(Prefix+"ControlSize"),Is.EqualTo(1.31f).Within(.001));Assert.That(MobileControlPreferences.Language,Is.EqualTo(UiLanguage.English));
            AssertPreview(c);
            Assert.That(c.SizeValue.GetComponent<Text>().fontSize,Is.EqualTo(48));
            SceneManager.LoadScene("Lobby");yield return Settle();nav=Object.FindFirstObjectByType<UiNavigationService>();modals=nav.GetComponent<UiModalStack>();
            nav.Navigate("SC13_Settings");View(MissingUtilityScreen.Settings).Controls.onClick.Invoke();yield return Settle();c=View(MissingUtilityScreen.Controls);
            Assert.That(c.Size.value,Is.EqualTo(1.31f).Within(.001));Assert.That(c.Position.value,Is.EqualTo(-.07f).Within(.001));Assert.That(c.Opacity.value,Is.EqualTo(.41f).Within(.001));
            Assert.IsTrue(c.LeftHand.isOn);Assert.IsTrue(c.Drop.isOn);Assert.IsTrue(c.LargeText.isOn);Assert.IsFalse(c.Vibration.isOn);
            Assert.That(c.LanguageValue.English,Is.EqualTo("Selected: English"));AssertPreview(c);
        }
        [UnityTest] public IEnumerator ResetCancelIsReadOnlyAndConfirmUsesExistingEightKeyReset()
        {
            var files=GameSaveFiles().ToDictionary(p=>p,File.ReadAllBytes);
            View(MissingUtilityScreen.Settings).Controls.onClick.Invoke();yield return Settle();var c=View(MissingUtilityScreen.Controls);
            c.Size.value=1.3f;c.Position.value=.07f;c.Opacity.value=.4f;c.LeftHand.isOn=true;c.Drop.isOn=true;c.LargeText.isOn=true;c.Vibration.isOn=false;
            var snapshot=Values();c.ResetControls.onClick.Invoke();yield return Settle();Assert.That(modals.Count,Is.EqualTo(1));
            c.ResetControls.onClick.Invoke();c.Back.onClick.Invoke();c.Size.value=.8f;Assert.That(modals.Count,Is.EqualTo(1));Assert.That(nav.CurrentScreenId,Is.EqualTo("SC19_ControlSettings"));
            var modal=nav.GetComponentInChildren<MissingControlReset>(true);modal.Cancel.onClick.Invoke();CollectionAssert.AreEqual(snapshot,Values());
            c.ResetControls.onClick.Invoke();yield return Settle();modal.Confirm.onClick.Invoke();modal.Confirm.onClick.Invoke();yield return Settle();
            Assert.That(modals.Count,Is.Zero);Assert.That(MobileControlPreferences.SizeScale,Is.EqualTo(1));Assert.That(MobileControlPreferences.HorizontalOffset,Is.Zero);
            Assert.That(MobileControlPreferences.Opacity,Is.EqualTo(.82f));Assert.IsFalse(MobileControlPreferences.LargeText);Assert.IsTrue(MobileControlPreferences.Vibration);
            Assert.That(MobileControlPreferences.Language,Is.EqualTo(UiLanguage.Korean));Assert.That(MobileControlPreferences.Handedness,Is.EqualTo(ControlHandedness.Right));Assert.IsFalse(MobileControlPreferences.UseDedicatedDropButton);
            CollectionAssert.AreEquivalent(files.Keys,GameSaveFiles());
            foreach(var kv in files)CollectionAssert.AreEqual(kv.Value,File.ReadAllBytes(kv.Key),kv.Key);
        }
        [UnityTest] public IEnumerator PointerSliderToggleAndModalRaycastUseExistingInputOwner()
        {
            View(MissingUtilityScreen.Settings).Controls.onClick.Invoke();yield return Settle();var c=View(MissingUtilityScreen.Controls);
            c.Body.verticalNormalizedPosition=.7f;yield return Settle();
            var r=(RectTransform)c.Size.handleRect.parent;
            var e=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(new Vector3(r.rect.xMax,r.rect.center.y)))};
            ExecuteEvents.Execute(c.Size.gameObject,e,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(c.Size.gameObject,e,ExecuteEvents.dragHandler);ExecuteEvents.Execute(c.Size.gameObject,e,ExecuteEvents.pointerUpHandler);
            Assert.That(MobileControlPreferences.SizeScale,Is.EqualTo(1.35f).Within(.001));
            var before=c.LeftHand.isOn;ExecuteEvents.Execute(c.LeftHand.gameObject,e,ExecuteEvents.pointerClickHandler);Assert.That(c.LeftHand.isOn,Is.Not.EqualTo(before));
            c.ResetControls.onClick.Invoke();yield return Settle();
            e.position=RectTransformUtility.WorldToScreenPoint(null,c.Back.transform.position);var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);
            Assert.IsNotEmpty(hits);Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.Not.SameAs(c.Back));
            var old=c.Body.verticalNormalizedPosition;
            modals.Pop();e.scrollDelta=new Vector2(0,-20);ExecuteEvents.Execute(c.Body.gameObject,e,ExecuteEvents.scrollHandler);yield return Settle();
            Assert.That(c.Body.verticalNormalizedPosition,Is.LessThan(old));
        }
        [UnityTest] public IEnumerator ReentryDoesNotDuplicatePreferenceListenersOrInstallations()
        {
            for(var i=0;i<3;i++)
            {
                MissingUtilityEntry.Install(SceneManager.GetActiveScene(),LoadSceneMode.Single);
                View(MissingUtilityScreen.Settings).Controls.onClick.Invoke();yield return Settle();var c=View(MissingUtilityScreen.Controls);
                MobileControlPreferences.Language=UiLanguage.Korean;c.Language.onClick.Invoke();Assert.That(MobileControlPreferences.Language,Is.EqualTo(UiLanguage.English));
                c.Back.onClick.Invoke();
            }
            Assert.That(nav.GetComponents<MissingUtilityEntry>().Length,Is.EqualTo(1));Assert.That(nav.GetComponentsInChildren<MissingUtilityView>(true).Length,Is.EqualTo(3));
            Assert.That(nav.GetComponentsInChildren<MissingControlReset>(true).Length,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator GameView1920()=>Survey(1920);
        [UnityTest] public IEnumerator GameView2400()=>Survey(2400);
        private IEnumerator Survey(int height)
        {
#if UNITY_EDITOR
            var setup=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ANIMOL.Editor.PortraitGameViewSetup")).First(t=>t!=null);
            setup.GetMethod("SelectFixedResolution").Invoke(null,new object[]{1080,height,"MissingUI Task2 QA"});
            var window=UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));window.Show();window.Focus();yield return Settle();
            Assert.That(Screen.width,Is.EqualTo(1080));Assert.That(Screen.height,Is.EqualTo(height));
            var safe=nav.GetComponentInChildren<SafeAreaLayout>();
            // Editor focus callbacks can explicitly call SafeAreaLayout.Apply even when it is disabled.
            // Keep the simulated device inset stable for the rendered frame, then verify it at capture.
            var probe=safe.gameObject.AddComponent<MissingUtilitySafeAreaProbe>();probe.enabled=false;
            foreach(bool inset in new[]{false,true})
            {
                probe.enabled=false;safe.enabled=true;safe.Apply();
                if(inset){safe.enabled=false;probe.Minimum=new Vector2(48f/1080,96f/height);probe.Maximum=new Vector2(1-48f/1080,1-120f/height);probe.enabled=true;}
                MobileControlPreferences.LargeText=inset;MobileControlPreferences.Language=inset?UiLanguage.English:UiLanguage.Korean;
                MobileControlPreferences.SizeScale=inset?1.35f:.75f;MobileControlPreferences.Handedness=inset?ControlHandedness.Left:ControlHandedness.Right;
                MobileControlPreferences.HorizontalOffset=inset?.08f:-.08f;MobileControlPreferences.Opacity=inset?.3f:1f;MobileControlPreferences.UseDedicatedDropButton=inset;
                MissingUtilityView.ApplyPreferences();
                foreach(var pair in new[]{("SC13_Settings",MissingUtilityScreen.Settings),("SC19_ControlSettings",MissingUtilityScreen.Controls),("SC20_AccountAndSave",MissingUtilityScreen.Account)})
                {
                    nav.Navigate(pair.Item1,false);yield return Settle();var view=View(pair.Item2);view.Body.verticalNormalizedPosition=1;yield return Settle();
                    yield return Capture(height+"_"+(inset?"safe_english_large_":"full_korean_")+pair.Item2+"_top");
                    view.Body.verticalNormalizedPosition=0;yield return Settle();yield return Capture(height+"_"+(inset?"safe_english_large_":"full_korean_")+pair.Item2+"_bottom");
                    AssertInside((RectTransform)view.Back.transform,(RectTransform)safe.transform);
                    foreach(var label in view.GetComponentsInChildren<MissingUtilityText>())Assert.That(label.GetComponent<Text>().text,Is.Not.Null.And.Not.Empty);
                }
                nav.Navigate("SC19_ControlSettings",false);View(MissingUtilityScreen.Controls).ResetControls.onClick.Invoke();yield return Settle();
                var modal=nav.GetComponentInChildren<MissingControlReset>();AssertInside((RectTransform)modal.Confirm.transform,(RectTransform)safe.transform);
                modal.Body.verticalNormalizedPosition=0;yield return Settle();yield return Capture(height+"_"+(inset?"safe_english_large":"full_korean")+"_reset");modals.Pop();
            }
            Object.Destroy(probe);safe.enabled=true;safe.Apply();
#else
            Assert.Ignore("Requires Editor Game View");yield break;
#endif
        }
        private static void AssertPreview(MissingUtilityView view)
        {
            var p=view.Preview;Assert.IsEmpty(p.GetComponentsInChildren<Button>(true));
            foreach(var name in new[]{"SwipeMoveRegion","JumpActionButton","SpecialActionButton","AbilityActionButton","AnimalChangeButton","DropActionButton"})
            {
                var rect=(RectTransform)p.ReferenceRoot.Find(name);
                var expected=MobileControlLayoutApplier.ReferenceRect(name,MobileControlPreferences.Handedness,MobileControlPreferences.HorizontalOffset,MobileControlPreferences.SizeScale);
                var actual=new Rect(Vector2.Scale(rect.anchorMin,MobileControlLayoutApplier.ReferenceSize)-rect.sizeDelta*rect.localScale.x*.5f,rect.sizeDelta*rect.localScale.x);
                Assert.That(actual.x,Is.EqualTo(expected.x).Within(.1f));Assert.That(actual.y,Is.EqualTo(expected.y).Within(.1f));Assert.That(actual.size,Is.EqualTo(expected.size));
                Assert.That(rect.GetComponent<Image>().color.a,Is.EqualTo(MobileControlPreferences.Opacity).Within(.001));
                Assert.That(rect.gameObject.activeSelf,Is.EqualTo(name!="DropActionButton"||MobileControlPreferences.UseDedicatedDropButton));
            }
        }
        private static float[] Values()=>new[]{MobileControlPreferences.SizeScale,MobileControlPreferences.HorizontalOffset,MobileControlPreferences.Opacity,MobileControlPreferences.LargeText?1:0,MobileControlPreferences.Vibration?1:0,(float)MobileControlPreferences.Language,(float)MobileControlPreferences.Handedness,MobileControlPreferences.UseDedicatedDropButton?1:0};
        private static IEnumerable<string> GameSaveFiles()=>Directory.GetFiles(Application.persistentDataPath,"*",SearchOption.AllDirectories).Where(p=>
        {
            var relative=Path.GetRelativePath(Application.persistentDataPath,p).Replace('\\','/');
            // Unity rotates its own telemetry independently of the UI or game save contract.
            return !(relative.StartsWith("Unity/",StringComparison.Ordinal)&&(relative.Contains("/Editor/Analytics/")||relative.Contains("/Insights/")));
        });
        private static IEnumerator Settle(){for(int i=0;i<10;i++)yield return null;Canvas.ForceUpdateCanvases();}
        private static void AssertInside(RectTransform child,RectTransform parent)
        {
            var a=new Vector3[4];var b=new Vector3[4];child.GetWorldCorners(a);parent.GetWorldCorners(b);
            Assert.That(a[0].x,Is.GreaterThanOrEqualTo(b[0].x-1));Assert.That(a[0].y,Is.GreaterThanOrEqualTo(b[0].y-1));Assert.That(a[2].x,Is.LessThanOrEqualTo(b[2].x+1));Assert.That(a[2].y,Is.LessThanOrEqualTo(b[2].y+1));
        }
        private static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            if(name.Contains("_safe_"))
            {
                var probe=Object.FindFirstObjectByType<MissingUtilitySafeAreaProbe>();Assert.IsTrue(probe.enabled);
                var r=(RectTransform)probe.transform;Assert.That(r.anchorMin,Is.EqualTo(probe.Minimum));Assert.That(r.anchorMax,Is.EqualTo(probe.Maximum));
            }
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            Directory.CreateDirectory("Docs/MissingUiV1Task2/Captures");File.WriteAllBytes("Docs/MissingUiV1Task2/Captures/"+name+".png",texture.EncodeToPNG());Object.Destroy(texture);
        }
    }

    [DefaultExecutionOrder(31000)]
    public sealed class MissingUtilitySafeAreaProbe:MonoBehaviour
    {
        public Vector2 Minimum,Maximum;
        private void LateUpdate()
        {
            var r=(RectTransform)transform;
            r.anchorMin=Minimum;r.anchorMax=Maximum;r.offsetMin=r.offsetMax=Vector2.zero;
        }
    }
}
