using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ANIMOL.Core;
using ANIMOL.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ANIMOL.ProductionV1.Tests
{
    public sealed class ProductionFlowTests
    {
        private ProductionController p;
        private Button B(string name)=>p.GetComponentsInChildren<Button>(true).Single(b=>b.name=="PV1_"+name);
        [UnitySetUp] public IEnumerator OpenLobby()
        {
            SceneManager.LoadScene("Lobby");yield return null;yield return null;yield return null;
            p=Object.FindFirstObjectByType<ProductionController>();Assert.NotNull(p);p.Initialize();
        }
        [UnityTest] public IEnumerator DetailGrowthReturnRetainsSelectionAndScrollWithoutCreatingImages()
        {
            int before=p.GetComponentsInChildren<Image>(true).Length;
            p.Navigation.Navigate(ProductionController.Screens[0]);B("Theme_0").onClick.Invoke();
            p.StageScroll.verticalNormalizedPosition=.35f;float position=p.StageScroll.verticalNormalizedPosition;
            B("Stage_0").onClick.Invoke();var stage=p.SelectedStage;
            B("DetailUpgrade").onClick.Invoke();B("Account").onClick.Invoke();p.Back();p.Back();
            Assert.AreEqual(ProductionController.Screens[2],p.Navigation.CurrentScreenId);Assert.AreSame(stage,p.SelectedStage);
            p.Back();Assert.AreEqual(position,p.StageScroll.verticalNormalizedPosition,.01f);
            Assert.AreEqual(before,p.GetComponentsInChildren<Image>(true).Length);yield return null;
        }
        [UnityTest] public IEnumerator ModalRaycastBlocksUnderlyingButtonsAndBackOnlyClosesModal()
        {
            B("LobbyUpgrade").onClick.Invoke();var screen=p.Navigation.CurrentScreenId;
            p.ShowState(ProductionState.Unconfigured,"긴 한글 안내: 운영 서비스와 확정된 효과가 연결되지 않아 강화 구매를 진행할 수 없습니다.");yield return null;
            Canvas.ForceUpdateCanvases();var results=new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=new Vector2(Screen.width*.5f,Screen.height*.5f)},results);
            Assert.IsTrue(results.Count>0);Assert.IsTrue(results[0].gameObject.transform.IsChildOf(p.transform.Find("SafeArea/ModalHost/ProductionV1Modal")));
            p.Back();Assert.AreEqual(screen,p.Navigation.CurrentScreenId);Assert.AreEqual(0,p.GetComponent<UiModalStack>().Count);
        }
        [UnityTest] public IEnumerator ActualUnapprovedStageFailsLoadingOnceAndCanReturn()
        {
            p.SelectTheme(0);p.SelectStage(0);Assert.IsFalse(B("StageStart").interactable);
            p.BeginLoading();p.BeginLoading();Assert.AreEqual(1,p.LoadRequests);
            yield return null;yield return new WaitForEndOfFrame();yield return null;
            Assert.IsFalse(p.Loading);Assert.IsNull(CampaignLaunchContext.Pending);Assert.AreEqual(1,p.GetComponent<UiModalStack>().Count);
            p.Back();B("LoadingReturn").onClick.Invoke();Assert.AreEqual(ProductionController.Screens[2],p.Navigation.CurrentScreenId);
        }
        [UnityTest] public IEnumerator UnqueriedAndUnconfiguredGrowthNeverDisplaysInventedLevelsOrOffers()
        {
            B("LobbyUpgrade").onClick.Invoke();B("Account").onClick.Invoke();
            Assert.IsTrue(Enumerable.Range(0,3).All(i=>!B("Buy_"+i).interactable));
            var root=p.transform.Find("SafeArea/ScreenHost/SC10A_AccountUpgrade/ProductionV1");
            Assert.IsTrue(root.GetComponentsInChildren<TextMeshProUGUI>(true).Where(t=>t.name=="Level").All(t=>t.text.Contains("Lv.--")));
            p.Back();B("Character").onClick.Invoke();Assert.IsFalse(B("AnimalPurchase_0").interactable);Assert.IsFalse(B("AnimalPurchase_1").interactable);
            Assert.IsFalse(p.transform.Find("SafeArea/ScreenHost/SC10B_CharacterUpgrade/ProductionV1").GetComponentsInChildren<TextMeshProUGUI>().Any(t=>t.text.Contains("DEV_")));yield return null;
        }
        [UnityTest] public IEnumerator AllModalStatesReuseOneSerializedPanel()
        {
            B("LobbyUpgrade").onClick.Invoke();int before=p.GetComponentsInChildren<Image>(true).Length;
            foreach(ProductionState state in System.Enum.GetValues(typeof(ProductionState)))
            {
                p.ShowState(state,"검증용 상태 · 2,147,483,647\n실제 운영 구매 결과가 아닙니다.");yield return null;
                Assert.AreEqual(1,p.GetComponent<UiModalStack>().Count);p.Back();
            }
            Assert.AreEqual(before,p.GetComponentsInChildren<Image>(true).Length);
        }
        [UnityTest] public IEnumerator ApprovedResponseUpdatesOnlyAfterRequestAndDoubleConfirmIsIgnored()
        {
            var catalog=Object.Instantiate(p.Catalog);catalog.Account=Object.Instantiate(p.Catalog.Account);
            typeof(AccountUpgradeCatalog).GetField("prototypeValues",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(catalog.Account,false);
            typeof(ProductionController).GetField("catalog",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(p,catalog);
            var approved=new ApprovedGrowthSnapshot{Coins=10000};foreach(var track in catalog.Account.Tracks)approved.Levels[track.TrackId]=0;
            var gateway=new ApprovedTestGateway(approved);p.BindAccountGateway(gateway,()=>approved);
            B("LobbyUpgrade").onClick.Invoke();B("Account").onClick.Invoke();B("Buy_0").onClick.Invoke();
            Assert.AreEqual(0,gateway.Calls);B("ModalConfirm").onClick.Invoke();B("ModalConfirm").onClick.Invoke();
            Assert.AreEqual(0,gateway.Calls);Assert.AreEqual(10000,approved.Coins);
            yield return null;yield return null;
            Assert.AreEqual(1,gateway.Calls);Assert.AreEqual(1,approved.Levels[catalog.Account.Tracks[0].TrackId]);Assert.IsFalse(p.Purchase.Pending);
            Object.Destroy(catalog.Account);Object.Destroy(catalog);
        }
        private sealed class ApprovedTestGateway:IAccountGateway
        {
            private readonly ApprovedGrowthSnapshot state;public int Calls;public bool IsConnected=>true;
            public ApprovedTestGateway(ApprovedGrowthSnapshot state){this.state=state;}
            public AccountRequestResult RequestUpgrade(string id,string track,int expected,int version)
            {Calls++;state.Levels[track]=expected+1;state.Coins=9820;return new AccountRequestResult(AccountRequestStatus.Approved,"isolated test response");}
        }
        [UnityTest] public IEnumerator CatalogLevelsMaxInsufficientAndLargeBalancesAreDistinct()
        {
            var catalog=Object.Instantiate(p.Catalog);catalog.Account=Object.Instantiate(p.Catalog.Account);
            typeof(AccountUpgradeCatalog).GetField("prototypeValues",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(catalog.Account,false);
            typeof(ProductionController).GetField("catalog",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(p,catalog);
            var state=new ApprovedGrowthSnapshot{Coins=int.MaxValue};
            foreach(var t in catalog.Account.Tracks)state.Levels[t.TrackId]=0;
            p.BindAccountGateway(new ApprovedTestGateway(state),()=>state);B("LobbyUpgrade").onClick.Invoke();B("Account").onClick.Invoke();
            foreach(int level in new[]{0,5,catalog.Account.Tracks[0].MaxLevel})
            {
                state.Levels[catalog.Account.Tracks[0].TrackId]=level;p.RefreshAccount();
                Assert.AreEqual(level<catalog.Account.Tracks[0].MaxLevel,B("Buy_0").interactable);
                if(level==catalog.Account.Tracks[0].MaxLevel)Assert.AreEqual("MAX",B("Buy_0").GetComponentInChildren<TextMeshProUGUI>().text);
            }
            state.Levels[catalog.Account.Tracks[0].TrackId]=0;state.Coins=0;p.RefreshAccount();
            Assert.AreEqual("코인 부족",B("Buy_0").GetComponentInChildren<TextMeshProUGUI>().text);B("Buy_0").onClick.Invoke();
            Assert.AreEqual(1,p.GetComponent<UiModalStack>().Count);Assert.AreEqual(0,state.Coins);
            yield return null;Object.Destroy(catalog.Account);Object.Destroy(catalog);
        }
    }
}
