using System;
using System.Collections;
using System.IO;
using System.Linq;
using ANIMOL.PortraitArtV1;
using ANIMOL.Typography;
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
    public sealed class MissingUiStoreFlowTests
    {
        private UiNavigationService nav;
        private UiModalStack modals;
        private MissingStoreView store;
        private bool previousBackground;
        [UnitySetUp] public IEnumerator Load()
        {
            previousBackground=Application.runInBackground;Application.runInBackground=true;
            SceneManager.LoadScene("Lobby");
            for(int i=0;i<6;i++)yield return null;
            nav=Object.FindFirstObjectByType<UiNavigationService>();modals=nav.GetComponent<UiModalStack>();
            store=nav.GetComponentInChildren<MissingStoreView>(true);Assert.NotNull(store);
            nav.Navigate("SC01_Lobby",false);
            Object.FindFirstObjectByType<PortraitEntryController>().ModeScreen.Shop.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(nav.CurrentScreenId,Is.EqualTo("SC11_Store"));Assert.IsTrue(store.isActiveAndEnabled);
        }
        [TearDown] public void RestoreBackground()=>Application.runInBackground=previousBackground;
        [UnityTest] public IEnumerator StoreAndModals1080x1920()=>Survey(1920);
        [UnityTest] public IEnumerator StoreAndModals1080x2400()=>Survey(2400);
        private IEnumerator Survey(int height)
        {
#if UNITY_EDITOR
            var setup=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ANIMOL.Editor.PortraitGameViewSetup")).First(t=>t!=null);
            setup.GetMethod("SelectFixedResolution").Invoke(null,new object[]{1080,height,"MissingUI Task1 QA"});
            var window=UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));window.Show();window.Focus();
            for(int i=0;i<6;i++)yield return null;
            Assert.That(Screen.width,Is.EqualTo(1080));Assert.That(Screen.height,Is.EqualTo(height));
            var safe=nav.GetComponentInChildren<SafeAreaLayout>();
            foreach(bool inset in new[]{false,true})
            {
                safe.enabled=true;safe.Apply();
                if(inset)
                {
                    safe.enabled=false;var r=(RectTransform)safe.transform;
                    r.anchorMin=new Vector2(48f/1080,96f/height);r.anchorMax=new Vector2(1-48f/1080,1-120f/height);
                }
                var prefix=height+(inset?"_safe":"_full");
                yield return Settle();store.Body.verticalNormalizedPosition=1;yield return Settle();
                yield return Capture(prefix+"_store_top");
                Assert.That(store.Body.content.rect.height,Is.GreaterThan(0));
                if(height==1920)Assert.That(store.Body.content.rect.height,Is.GreaterThan(store.Body.viewport.rect.height));
                store.Body.verticalNormalizedPosition=0;yield return Settle();yield return Capture(prefix+"_store_bottom");
                AssertInside((RectTransform)store.Back.transform,(RectTransform)safe.transform);
                Assert.IsTrue(store.GrowthDetails.interactable);Assert.IsTrue(store.EmoteDetails.interactable);
                store.GrowthDetails.onClick.Invoke();yield return Settle();
                Assert.That(modals.Count,Is.EqualTo(1));
                var detail=nav.transform.Find("SafeArea/ModalHost/ProductDetailModal");
                var purchase=MissingStoreView.Find<Button>(detail,"ProductPurchaseButton");Assert.IsFalse(purchase.interactable);
                var desc=MissingStoreView.Find<Text>(detail,"ProductDescription");
                Assert.That(desc.text,Does.Contain("전체 비용: --"));Assert.That(desc.text,Does.Contain("동물 해금, 동물 레벨, 액티브·패시브 성장은 포함되지 않습니다."));
                var scroll=detail.GetComponentInChildren<ScrollRect>(true);scroll.verticalNormalizedPosition=1;
                yield return Settle();yield return Capture(prefix+"_growth_top");
                scroll.verticalNormalizedPosition=0;yield return Settle();yield return Capture(prefix+"_growth_bottom");
                AssertInside((RectTransform)purchase.transform,(RectTransform)safe.transform);
                var screen=nav.CurrentScreenId;store.Back.onClick.Invoke();store.GrowthDetails.onClick.Invoke();Assert.That(nav.CurrentScreenId,Is.EqualTo(screen));Assert.That(modals.Count,Is.EqualTo(1));
                MissingStoreView.Find<Button>(detail,"ProductCancelButton").onClick.Invoke();Assert.That(modals.Count,Is.Zero);
                store.EmoteDetails.onClick.Invoke();yield return Settle();yield return Capture(prefix+"_emote");
                Assert.That(desc.text,Does.Contain("번들 전체 구성: 설정 대기"));Assert.IsFalse(purchase.interactable);modals.Pop();
                store.RestoreDetails.onClick.Invoke();yield return Settle();yield return Capture(prefix+"_restore");
                Assert.That(desc.text,Does.Contain("복원 결과: --"));Assert.IsFalse(purchase.interactable);modals.Pop();
            }
            safe.enabled=true;safe.Apply();
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            store.Back.onClick.Invoke();yield return null;Assert.That(nav.CurrentScreenId,Is.EqualTo("SC01_Lobby"));
#else
            Assert.Ignore("Requires actual Editor Game View.");yield break;
#endif
        }
        [UnityTest] public IEnumerator CommonModalLongTextScrollAndExistingCancelListenersSurviveRestyling()
        {
            foreach(var name in new[]{"ProductDetailModal","ServiceErrorModal","RewardedAdModal","ConfirmExitModal","MultiplayerPauseModal"})
            {
                var modal=nav.transform.Find("SafeArea/ModalHost/"+name);Assert.NotNull(modal.GetComponent<MissingCommonModalSkin>(),name);
                Assert.IsTrue(modals.Push(name));yield return Settle();
                var scroll=modal.GetComponentInChildren<ScrollRect>();Assert.NotNull(scroll);
                var text=scroll.content.GetComponentsInChildren<Text>().Last();var old=text.text;
                text.text="[QA synthetic long text, not live product data]\n"+string.Join("\n",Enumerable.Repeat("긴 설명과 실제 실패 사유를 모두 읽기 위한 레이아웃 검사 / Long product description and failure reason.",22));
                nav.GetComponent<UiScenePolish>().Apply();yield return Settle();
                Assert.That(scroll.content.rect.height,Is.GreaterThan(scroll.viewport.rect.height));
                scroll.verticalNormalizedPosition=0;yield return Settle();yield return Capture("fixture_"+name);
                var b=modal.GetComponentInChildren<Button>();Assert.That(b.image.color,Is.EqualTo(Color.white));
                text.text=old;modals.Pop();Assert.That(modals.Count,Is.Zero);
            }
            store.GrowthDetails.onClick.Invoke();yield return null;
            MissingStoreView.Find<Button>(nav.transform,"ProductCancelButton").onClick.Invoke();Assert.That(modals.Count,Is.Zero);
        }
        [UnityTest] public IEnumerator ReentryInstallsOnceAndLeavesPersistentFilesUntouched()
        {
            var before=Directory.GetFiles(Application.persistentDataPath,"*",SearchOption.AllDirectories).ToDictionary(p=>p,File.ReadAllBytes);
            for(int i=0;i<3;i++)
            {
                MissingUiProjectEntry.Install(SceneManager.GetActiveScene(),LoadSceneMode.Single);
                store.GrowthDetails.onClick.Invoke();yield return null;Assert.That(modals.Count,Is.EqualTo(1));
                MissingStoreView.Find<Button>(nav.transform,"ProductCancelButton").onClick.Invoke();
                store.Back.onClick.Invoke();Object.FindFirstObjectByType<PortraitEntryController>().ModeScreen.Shop.onClick.Invoke();yield return null;
            }
            Assert.That(nav.GetComponents<MissingUiProjectEntry>().Length,Is.EqualTo(1));
            Assert.That(nav.GetComponentsInChildren<MissingStoreView>(true).Length,Is.EqualTo(1));
            Assert.That(modals.Count,Is.Zero);
            CollectionAssert.AreEquivalent(before.Keys,Directory.GetFiles(Application.persistentDataPath,"*",SearchOption.AllDirectories));
            foreach(var file in before)CollectionAssert.AreEqual(file.Value,File.ReadAllBytes(file.Key),file.Key);
        }
        private static IEnumerator Settle(){for(int i=0;i<8;i++)yield return null;Canvas.ForceUpdateCanvases();}
        [UnityTest] public IEnumerator RaycastModalBlocksUnderlyingBackAndWheelScrollsBody()
        {
#if UNITY_EDITOR
            var setup=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ANIMOL.Editor.PortraitGameViewSetup")).First(t=>t!=null);
            setup.GetMethod("Use1080x1920").Invoke(null,null);yield return Settle();
#endif
            store.Body.verticalNormalizedPosition=1;yield return Settle();
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,store.Body.viewport.position),scrollDelta=new Vector2(0,-12)};
            ExecuteEvents.Execute(store.Body.gameObject,pointer,ExecuteEvents.scrollHandler);yield return Settle();
            Assert.That(store.Body.verticalNormalizedPosition,Is.LessThan(1));
            var hits=new System.Collections.Generic.List<RaycastResult>();
            pointer.position=RectTransformUtility.WorldToScreenPoint(null,store.Back.transform.position);
            EventSystem.current.RaycastAll(pointer,hits);Assert.IsNotEmpty(hits);
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.SameAs(store.Back));
            store.GrowthDetails.onClick.Invoke();yield return Settle();
            hits.Clear();EventSystem.current.RaycastAll(pointer,hits);Assert.IsNotEmpty(hits);
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.Not.SameAs(store.Back));
            var cancel=MissingStoreView.Find<Button>(nav.transform,"ProductCancelButton");
            ExecuteEvents.Execute(cancel.gameObject,pointer,ExecuteEvents.pointerClickHandler);Assert.That(modals.Count,Is.Zero);
            ExecuteEvents.Execute(store.Back.gameObject,pointer,ExecuteEvents.pointerClickHandler);Assert.That(nav.CurrentScreenId,Is.EqualTo("SC01_Lobby"));
        }
        private static void AssertInside(RectTransform child,RectTransform parent)
        {
            var a=new Vector3[4];var b=new Vector3[4];child.GetWorldCorners(a);parent.GetWorldCorners(b);
            Assert.That(a[0].x,Is.GreaterThanOrEqualTo(b[0].x-1));Assert.That(a[0].y,Is.GreaterThanOrEqualTo(b[0].y-1));
            Assert.That(a[2].x,Is.LessThanOrEqualTo(b[2].x+1));Assert.That(a[2].y,Is.LessThanOrEqualTo(b[2].y+1));
        }
        private static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();
            var directory="Docs/MissingUiV1Task1/Captures";Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory,name+".png"),texture.EncodeToPNG());Object.Destroy(texture);
        }
    }
}
