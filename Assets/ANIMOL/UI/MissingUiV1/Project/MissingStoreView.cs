using System.Linq;
using ANIMOL.UI;
using UnityEngine;
using UnityEngine.UI;
using static ANIMOL.MissingUiV1.Project.MissingUiLayout;

namespace ANIMOL.MissingUiV1.Project
{
    public sealed class MissingStoreView : MonoBehaviour
    {
        public MissingUiArt Art;
        public Button GrowthDetails, EmoteDetails, RestoreDetails, Back;
        public ScrollRect Body;
        private UiNavigationService navigation;
        private UiModalStack modals;

        public static MissingStoreView Build(Transform parent, MissingUiArt art)
        {
            var root = Node("MissingStore", parent); Fill(root);
            root.gameObject.SetActive(false);
            var view = root.gameObject.AddComponent<MissingStoreView>(); view.Art = art;
            var title = Label("StoreHeading", root, "상점", 56, art);
            Rect(title.rectTransform, Vector2.up, Vector2.one, new Vector2(44,-126), new Vector2(-44,-28));
            view.Body = Scroll(root, art);
            Rect((RectTransform)view.Body.transform, Vector2.zero, Vector2.one, new Vector2(36,174), new Vector2(-36,-148));
            var balance = Node("AccountSummary", view.Body.content); Picture(balance, art.Row, true); Vertical(balance,32,16);
            Label("Balance",balance,"보유 코인  --",36,art,true).color=Ink;
            Label("Availability",balance,"계정·상품 정보 연결 대기\n가격과 보유 여부는 조회 후 표시됩니다.",32,art,true).color=Ink;
            view.GrowthDetails = Card(view.Body.content, "Growth", "성장 완료 팩", "계정 공통 성장 3트랙\n최대 스태미나 · 재생 · 소모 감소", art.Growth,art);
            view.EmoteDetails = Card(view.Body.content, "Emote", "이모티콘 번들", "번들 구성과 보유 정보\n상품 조회 연결 대기", art.Emotes,art);
            view.RestoreDetails = Button("MissingRestoreInfo",view.Body.content,"구매 복원 안내",art);
            Label("StoreNotice",view.Body.content,"구매·복원은 서비스 연결 후 이용할 수 있습니다.",32,art,true);
            var footer = Node("FooterFixed",root);
            Rect(footer,Vector2.zero,Vector2.right,Vector2.zero,new Vector2(0,160));
            var background=Picture(footer,null,true); background.color=Ink;
            // Keep the active SC11 back-control name used by the existing entry/raycast contract.
            // The original inactive children remain first so MetaUiPresenter retains its legacy bindings.
            view.Back = Button("StoreBackButton",footer,"뒤로",art);
            Fill((RectTransform)view.Back.transform,24);
            return view;
        }
        private static Button Card(Transform parent, string key, string title, string description, Sprite sprite, MissingUiArt art)
        {
            var card=Node(key+"ProductCard",parent); Picture(card,art.Product,true); Vertical(card,32,20);
            var picture=Node(key+"FullArt",card); Height(picture,256); Picture(picture,sprite);
            Label(key+"Heading",card,title,48,art,true);
            Label(key+"Description",card,description,32,art,true);
            var price=Node(key+"PricePlate",card); Picture(price,art.Price,true); Vertical(price,32,8);
            Label(key+"Price",price,"가격  --  ·  연결 대기",32,art,true);
            Label(key+"Ownership",card,"보유 여부  --",32,art,true);
            foreach(var text in card.GetComponentsInChildren<Text>(true))text.color=Ink;
            return Button("Missing"+key+"Details",card,"구성 자세히 보기",art);
        }
        private void OnEnable()
        {
            navigation=GetComponentInParent<UiNavigationService>(); modals=navigation == null ? null : navigation.GetComponent<UiModalStack>();
            GrowthDetails.onClick.AddListener(OpenGrowth); EmoteDetails.onClick.AddListener(OpenEmotes);
            RestoreDetails.onClick.AddListener(OpenRestore); Back.onClick.AddListener(Return);
        }
        private void OnDisable()
        {
            if(GrowthDetails!=null)GrowthDetails.onClick.RemoveListener(OpenGrowth);
            if(EmoteDetails!=null)EmoteDetails.onClick.RemoveListener(OpenEmotes);
            if(RestoreDetails!=null)RestoreDetails.onClick.RemoveListener(OpenRestore);
            if(Back!=null)Back.onClick.RemoveListener(Return);
        }
        private void Return() { if(modals!=null && modals.Count==0)navigation.Back(); }
        private void OpenGrowth() => Show("성장 완료 팩",
            "계정 공통 성장만 대상입니다.\n\n최대 스태미나\n현재 -- / 적용 후 --\n\n스태미나 재생\n현재 -- / 적용 후 --\n\n스태미나 소모 감소\n현재 -- / 적용 후 --\n\n동물 해금, 동물 레벨, 액티브·패시브 성장은 포함되지 않습니다.\n\n전체 비용: --\n플랫폼 표시 가격: --\n보유 여부: --\n\n이미 성장한 계정의 보상 정책과 판매 조건은 설정 대기입니다. 상품·가격·보유 조회 및 결제 서비스가 연결되지 않아 구매할 수 없습니다.","구매 연결 대기");
        private void OpenEmotes() => Show("이모티콘 번들",
            "번들 전체 구성: 설정 대기\n상품 조회 후 포함된 이모티콘을 표시합니다.\n\n전체 비용: --\n플랫폼 표시 가격: --\n보유 여부: --\n\n동물 성장이나 해금을 포함하지 않습니다. 상품·가격·보유 조회 및 결제 서비스가 연결되지 않아 구매할 수 없습니다.","구매 연결 대기");
        private void OpenRestore() => Show("구매 복원",
            "플랫폼 구매 복원 서비스 연결 대기\n\n복원할 상품과 보유 여부: --\n복원 결과: --\n\n현재 구매 내역을 조회하거나 복원 요청을 보낼 수 없습니다. 이 안내를 열거나 닫아도 구매 내역, 재화, 성장, 보유 상태는 바뀌지 않습니다.","복원 연결 대기");
        private void Show(string title,string body,string action)
        {
            if(modals==null || modals.Count!=0)return;
            var modal=navigation.transform.Find("SafeArea/ModalHost/ProductDetailModal"); if(modal==null)return;
            Find<Text>(modal,"ProductTitle").text=title; Find<Text>(modal,"ProductDescription").text=body;
            var purchase=Find<Button>(modal,"ProductPurchaseButton"); purchase.interactable=false;
            purchase.GetComponentInChildren<Text>(true).text=action;
            modals.Push("ProductDetailModal");
            var scroll=modal.GetComponentInChildren<ScrollRect>(true); if(scroll!=null) { scroll.StopMovement(); scroll.verticalNormalizedPosition=1; }
        }
        public static T Find<T>(Transform root,string name) where T:Component => root.GetComponentsInChildren<T>(true).FirstOrDefault(x=>x.name==name);
    }
}
