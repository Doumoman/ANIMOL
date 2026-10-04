using ANIMOL.UI;
using UnityEngine;
using UnityEngine.UI;
using static ANIMOL.MissingUiV1.Project.MissingUiLayout;
using W=ANIMOL.MissingUiV1.Project.MissingUtilityWidgets;

namespace ANIMOL.MissingUiV1.Project
{
    public sealed class MissingControlReset:MonoBehaviour
    {
        public const string DialogName="MissingControlResetModal";
        public Button Cancel,Confirm;
        public ScrollRect Body;
        private UiModalStack modals;
        public static MissingControlReset Build(Transform parent,MissingUtilityArt art)
        {
            var root=Node(DialogName,parent);Fill(root);root.gameObject.SetActive(false);
            var overlay=Picture(root,null,true);overlay.color=new Color(0,0,0,.72f);overlay.raycastTarget=true;
            var dialog=root.gameObject.AddComponent<MissingControlReset>();
            var card=Node("Card",root);Rect(card,new Vector2(.04f,.15f),new Vector2(.96f,.85f),Vector2.zero,Vector2.zero);Picture(card,art.Common.Panel,true);
            dialog.Body=Scroll(card,art.Common);Rect((RectTransform)dialog.Body.transform,Vector2.zero,Vector2.one,new Vector2(38,190),new Vector2(-38,-46));
            W.Words(dialog.Body.content,"ResetHeading","조작·접근성을 초기화할까요?","Reset controls and accessibility?",art,48);
            W.Words(dialog.Body.content,"ResetDescription",
                "초기화 대상\n버튼 크기 · 가로 위치 · 투명도\n좌우손 배치 · 전용 하향 버튼\n큰 글씨 · 진동 · 언어 선택\n\n기존 조작 설정의 기본값으로 복원합니다.\n\n계정 진행도, 동물 성장, 재화, 구매 내역, 로컬·클라우드 게임 저장은 변경하지 않습니다.\n\nBGM/SFX/음소거는 미연결이며 이번 초기화 대상이 아닙니다.",
                "Settings to reset\nControl size, horizontal position and opacity\nHandedness and dedicated drop button\nLarge text, vibration and language preference\n\nRestores the existing control preference defaults.\n\nAccount progress, animal growth, currencies, purchases and local/cloud game saves are not changed.\n\nMusic, sound and mute are unavailable and are outside this reset.",art);
            dialog.Cancel=W.Action(card,"MissingResetCancel","취소","Cancel",art);
            dialog.Confirm=W.Action(card,"MissingResetConfirm","초기화","Reset",art,true);
            Rect((RectTransform)dialog.Cancel.transform,Vector2.zero,new Vector2(.5f,0),new Vector2(28,38),new Vector2(-28,150));
            Rect((RectTransform)dialog.Confirm.transform,new Vector2(.5f,0),Vector2.right,new Vector2(28,38),new Vector2(-28,150));
            return dialog;
        }
        private void OnEnable()
        {
            modals=GetComponentInParent<UiModalStack>();Cancel.onClick.AddListener(Close);Confirm.onClick.AddListener(ResetPreferences);
            Body.StopMovement();Body.verticalNormalizedPosition=1;
        }
        private void OnDisable(){Cancel.onClick.RemoveListener(Close);Confirm.onClick.RemoveListener(ResetPreferences);}
        private void Close(){if(isActiveAndEnabled)modals.Pop();}
        private void ResetPreferences()
        {
            if(!isActiveAndEnabled||modals==null)return;
            MobileControlPreferences.Reset(); // The existing owner defines the exact eight-key reset scope.
            MissingUtilityView.ApplyPreferences();modals.Pop();
        }
    }
}
