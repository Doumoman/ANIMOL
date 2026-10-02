using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace ANIMOL.PortraitArtV1.Tests
{
    public sealed class PortraitEntryFlowTests
    {
        private RenderTexture target;
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var old=SceneManager.GetActiveScene();var next=SceneManager.CreateScene("PortraitArtTestCleanup");SceneManager.SetActiveScene(next);
            Time.timeScale=1;yield return SceneManager.UnloadSceneAsync(old);
            if(target!=null) { target.Release();Object.Destroy(target);target=null; }
        }
        private IEnumerator Load(string scene)
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/"+scene+".unity",new LoadSceneParameters(LoadSceneMode.Single));
#endif
            yield return new WaitForSecondsRealtime(.6f);
        }
        private IEnumerator Portrait()
        {
            var c=Object.FindFirstObjectByType<PortraitEntryController>();var canvas=c.Navigation.GetComponent<Canvas>();
            canvas.GetComponent<PortraitDisplayController>().enabled=false;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;scaler.scaleFactor=1;
            if(target==null) { target=new RenderTexture(1080,1920,24);target.Create(); }
            Camera.main.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=Camera.main;canvas.planeDistance=10;
            yield return null;Canvas.ForceUpdateCanvases();
        }
        private static void Click(Button button)
        {
            Assert.That(button.IsActive() && button.IsInteractable(),Is.True,button.name);
            var rect=(RectTransform)button.transform;
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(Camera.main,rect.TransformPoint(rect.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Assert.That(hits.Count,Is.GreaterThan(0));Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject),Is.EqualTo(button.gameObject));
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        [UnityTest] public IEnumerator BootstrapSinglePlayOpensModeAndExistingScreenReturnRoutes()
        {
            yield return Load("Bootstrap");yield return Portrait();
            var c=Object.FindFirstObjectByType<PortraitEntryController>();
            Assert.That(c.Navigation.CurrentScreenId,Is.EqualTo(PortraitEntryController.StartId));
            Assert.That(Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Count(b=>b.IsActive()),Is.EqualTo(1));
            Click(c.StartScreen.Play);yield return new WaitForSecondsRealtime(.7f);yield return Portrait();
            c=Object.FindFirstObjectByType<PortraitEntryController>();
            Assert.That(c.Navigation.CurrentScreenId,Is.EqualTo(PortraitEntryController.ModeId));
            Assert.That(Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            foreach(var route in new[]{(c.ModeScreen.Campaign,"SC02_ThemeSelect","PV1_SC02_ThemeSelectBack"),(c.ModeScreen.Shop,"SC11_Store","StoreBackButton"),(c.ModeScreen.Settings,"SC13_Settings","SettingsBackButton")}) {
                Click(route.Item1);yield return new WaitForSecondsRealtime(.25f);Assert.That(c.Navigation.CurrentScreenId,Is.EqualTo(route.Item2));
                Click(c.Navigation.GetComponentsInChildren<Button>().First(b=>b.name==route.Item3));yield return new WaitForSecondsRealtime(.25f);
                Assert.That(c.Navigation.CurrentScreenId,Is.EqualTo(PortraitEntryController.ModeId));
            }
            Click(c.ModeScreen.Back);yield return new WaitForSecondsRealtime(.25f);Assert.That(c.Navigation.CurrentScreenId,Is.EqualTo(PortraitEntryController.StartId));
        }
        [UnityTest] public IEnumerator UnavailableServicesStayVisibleDisabledAndNeverUseDevCoinBalance()
        {
            yield return Load("Lobby");var c=Object.FindFirstObjectByType<PortraitEntryController>();
            foreach(var button in new[]{c.ModeScreen.Ad,c.ModeScreen.Competition,c.ModeScreen.Cooperation}) {
                Assert.That(button.gameObject.activeInHierarchy,Is.True);Assert.That(button.interactable,Is.False);
                button.onClick.Invoke();Assert.That(c.Navigation.CurrentScreenId,Is.EqualTo(PortraitEntryController.ModeId));
            }
            Assert.That(c.ModeScreen.Currency.text,Is.EqualTo("--"));Assert.That(c.Ledger.VerifiedCoinBalance,Is.Null);
            Assert.That(c.BindVerifiedLedger(new DevRewardLedgerAdapter(12500,3,"2030-01-02")),Is.False);
            Assert.That(c.ModeScreen.Currency.text,Is.EqualTo("--"));
            var ledger=new ReadOnlyVerifiedFixture();Assert.That(c.BindVerifiedLedger(ledger),Is.True);
            Assert.That(c.ModeScreen.Currency.text,Is.EqualTo("12,345"));ledger.Balance=56789;c.RefreshState();
            Assert.That(c.ModeScreen.Currency.text,Is.EqualTo("56,789"));Assert.That(ledger.GrantCalls,Is.Zero);
            Assert.That(c.ModeScreen.Ad.interactable,Is.False);
        }
        [UnityTest] public IEnumerator ModuleDoesNotInstallInProtectedMoonScene()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/GraphicsQA/MoonGraphicsQA.unity",new LoadSceneParameters(LoadSceneMode.Single));
#endif
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(Object.FindObjectsByType<PortraitEntryController>(FindObjectsSortMode.None),Is.Empty);
        }
        private sealed class ReadOnlyVerifiedFixture : IRewardLedgerAdapter
        {
            public int Balance=12345,GrantCalls;
            public bool IsDevelopmentOnly=>false;public bool IsAvailable=>true;public int? VerifiedCoinBalance=>Balance;
            public DailyAdQuotaSnapshot Quota=>DailyAdQuotaSnapshot.Unavailable(3);
            public RewardGrantStatus ConfirmApprovedGrant(RewardedAdGrantRequest request) { GrantCalls++;return RewardGrantStatus.Unavailable; }
        }
    }
}
