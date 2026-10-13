using System;
using System.Collections.Generic;
using ANIMOL.AnimalMultiplayerPhase3;
using ANIMOL.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static ANIMOL.MissingUiV1.Project.MissingUiLayout;
using W=ANIMOL.MissingUiV1.Project.MissingUtilityWidgets;

namespace ANIMOL.MissingUiV1.Project.Multiplayer
{
    public enum MissingMultiplayerScreen { Hub,Custom,Create,Join,Room }
    public enum MissingMultiplayerContextAction { Normal,Ranked,Create,Lookup,Join,LeaveSaved }
    public enum MissingMultiplayerRoomAction { Ready,Start,Leave,Copy }

    public sealed class MissingMultiplayerView:MonoBehaviour
    {
        public MissingMultiplayerScreen Screen;
        public ScrollRect Body;
        public Button Back,NormalBrowse,RankedBrowse,Custom,Create,Join,Browse,Entry,Lookup,Paste,Invite,Ready,StartMatch,Leave,Copy;
        public MissingRoomCodeInput Code;
        public MissingUtilityText CodeNotice;
        public RectTransform[] ParticipantSlots;
        private UiNavigationService navigation;
        private UiModalStack modals;
        private readonly List<Action> removeListeners=new List<Action>();
        // Optional DEV context reader. This port cannot submit an admission or grant online access.
        public Action<MissingMultiplayerContextAction> DevContextRequested;
        public Action<MissingMultiplayerRoomAction> DevRoomRequested;
        public bool DevLeaveSavedEntry;
        public AnimalMultiplayerPhase3Host Host=>navigation==null?null:navigation.GetComponent<AnimalMultiplayerPhase3Entry>()?.Host;
        public bool CanNavigate=>isActiveAndEnabled&&navigation!=null&&(modals==null||modals.Count==0)&&Host!=null&&!Host.Presenter.IsCommitting&&!Host.Presenter.IsMultiplayerModalOpen;

        public static string ScreenId(MissingMultiplayerScreen kind)=>kind==MissingMultiplayerScreen.Hub?"SC05_CompetitiveHub":kind==MissingMultiplayerScreen.Room?"SC06_MatchRoom":"MissingUiV1_"+kind;
        public static MissingMultiplayerView Build(Transform parent,MissingMultiplayerArt art,MissingMultiplayerScreen kind)
        {
            var root=Node("MissingMultiplayer"+kind,parent);Fill(root);root.gameObject.SetActive(false);
            var v=root.gameObject.AddComponent<MissingMultiplayerView>();v.Screen=kind;
            Picture(root,null,true).color=new Color32(11,21,29,255);
            string[] ko={"경쟁전","커스텀 방","방 만들기","코드로 참가","대기방"};
            string[] en={"Competition","Custom rooms","Create a room","Join by code","Waiting room"};
            var title=W.Words(root,"MultiplayerHeading",ko[(int)kind],en[(int)kind],art.Utility,48,false);title.TextColor=White;
            Rect((RectTransform)title.transform,Vector2.up,Vector2.one,new Vector2(40,-128),new Vector2(-40,-24));
            v.Body=Scroll(root,art.Utility.Common);Rect((RectTransform)v.Body.transform,Vector2.zero,Vector2.one,new Vector2(36,174),new Vector2(-36,-150));
            switch(kind){case MissingMultiplayerScreen.Hub:v.BuildHub(art);break;case MissingMultiplayerScreen.Custom:v.BuildCustom(art);break;case MissingMultiplayerScreen.Create:v.BuildCreate(art);break;case MissingMultiplayerScreen.Join:v.BuildJoin(art);break;case MissingMultiplayerScreen.Room:v.BuildRoom(art);break;}
            var footer=Node("FixedFooter",root);Rect(footer,Vector2.zero,Vector2.right,Vector2.zero,new Vector2(0,160));Picture(footer,null,true).color=Ink;
            v.Back=W.Action(footer,"MissingMultiplayerBack","뒤로","Back",art.Utility);Fill((RectTransform)v.Back.transform,24);
            return v;
        }
        private RectTransform Card(MissingMultiplayerArt a,string id,string ko,string en,Sprite icon,bool mode=false)
        {
            var row=W.Row(Body.content,id,ko,en,icon,a.Utility);row.GetComponent<Image>().sprite=mode?a.ModeFrame:a.Utility.Common.Row;return row;
        }
        private void Words(Transform p,string id,string ko,string en,MissingMultiplayerArt a)=>W.Words(p,id,ko,en,a.Utility);
        private Button Disabled(Transform p,string id,string ko,string en,MissingMultiplayerArt a)
        {var b=W.Action(p,id,ko,en,a.Utility);b.interactable=false;return b;}
        private void Policy(MissingMultiplayerArt a)
        {
            var row=Card(a,"CompetitivePolicy","경쟁전 안내","Competition rules",a.Casual);
            Words(row,"FourPlayerPolicy","경쟁 최대 4인 · 지상·특수·공중 각 1종\n다른 참가자와 같은 동물을 사용할 수 있어요.","Up to 4 players · One ground, special and air animal each. Players may use the same animals.",a);
            Words(row,"PolicyUnavailable","현재 입장 정책 -- · 서비스 연결 대기\n방에 참가하거나 준비 상태를 변경할 수 없습니다.","Entry policy -- · Service unavailable. Room entry and ready actions are unavailable.",a);
        }
        private void BuildHub(MissingMultiplayerArt a)
        {
            Policy(a);
            var row=Card(a,"CasualCard","일반 경쟁","Casual competition",a.Casual,true);
            Words(row,"CasualRules","일반 경기 규칙 · 상세 규칙 조회 대기\n지금은 동물 그림을 열람할 수 있어요.","Casual match rules · Details unavailable. Animal art can be browsed now.",a);
            NormalBrowse=W.Action(row,"MissingCasualBrowse","동물 열람","Browse animals",a.Utility);
            row=Card(a,"RankedCard","랭크 경쟁","Ranked competition",a.Ranked,true);
            Words(row,"RankedRules","랭크 경기 규칙 · 상세 규칙 조회 대기\n현재 랭킹·보상·입장 처리는 연결되지 않았습니다.","Ranked match rules · Details unavailable. Ranking, rewards and entry services are not connected.",a);
            RankedBrowse=W.Action(row,"MissingRankedBrowse","동물 열람","Browse animals",a.Utility);
            row=Card(a,"CustomCard","커스텀 방","Custom rooms",a.Private,true);
            Words(row,"CustomKind","방 만들기 또는 코드로 참가하는 입장 방식입니다. 경기 규칙은 실제 방 정책을 확인한 후 표시합니다.","Create a room or join by code. Match rules will come from the room policy.",a);
            Custom=W.Action(row,"MissingOpenCustom","커스텀 방 보기","Custom rooms",a.Utility);
        }
        private void BuildCustom(MissingMultiplayerArt a)
        {
            Policy(a);
            var row=Card(a,"CreateCard","방 만들기","Create a room",a.Private,true);
            Words(row,"CreateNotice","방 옵션과 현재 연결 상태를 확인합니다. 이 버튼은 방을 생성하지 않습니다.","Review room options and availability. Opening this screen does not create a room.",a);
            Create=W.Action(row,"MissingOpenCreate","방 옵션 보기","Room options",a.Utility);
            row=Card(a,"JoinCard","코드로 참가","Join by code",a.Enter,true);
            Words(row,"JoinNotice","초대받은 코드를 입력합니다. 현재 방 조회·참가 서비스는 연결 대기입니다.","Enter an invitation code. Room lookup and entry services are unavailable.",a);
            Join=W.Action(row,"MissingOpenJoin","코드 입력","Enter code",a.Utility);
        }
        private void BuildCreate(MissingMultiplayerArt a)
        {
            Policy(a);
            var row=Card(a,"RoomOptions","방 옵션","Room options",a.Private);
            Words(row,"ActualRoomOptions","경기 유형 --\n허용 옵션 --\n정책 버전 --\n실제 모드·방 정책 조회 연결 대기","Match rules --\nAllowed options --\nPolicy revision --\nMode and room policy queries unavailable",a);
            row=Card(a,"OriginalLoadout","동물 편성","Animal loadout",a.Casual);
            Words(row,"LoadoutUnknown","지상 -- · 특수 -- · 공중 --\n계정 편성과 사용 권한 조회 연결 대기","Ground -- · Special -- · Air --\nAccount loadout and permissions unavailable",a);
            Browse=W.Action(row,"MissingCreateBrowse","동물 열람","Browse animals",a.Utility);
            Entry=Disabled(Body.content,"MissingCreateUnavailable","생성·입장 연결 대기","Create / entry unavailable",a);
        }
        private void BuildJoin(MissingMultiplayerArt a)
        {
            var row=Card(a,"CodeCard","방 코드","Room code",a.Enter);
            Words(row,"CodeGuide","초대받은 코드를 입력하거나 붙여넣으세요. 조회 서비스가 연결되기 전에는 코드 유효성을 확인할 수 없습니다.","Type or paste the invitation code. Its validity cannot be checked until the lookup service is connected.",a);
            Code=MissingRoomCodeInput.Build(row,a);
            Paste=W.Action(row,"MissingPasteCode","코드 붙여넣기","Paste code",a.Utility);
            CodeNotice=W.Words(row,"CodeNotice","방 조회 서비스 연결 대기","Room lookup service unavailable",a.Utility);
            Lookup=Disabled(row,"MissingLookupUnavailable","방 조회 · 미지원","Look up · unavailable",a);
            row=Card(a,"RoomQuerySummary","조회한 방","Room details",a.Private);
            Words(row,"UnknownRoom","방장 --\n방 코드 --\n현재 인원 -- / 4\n경기 유형 --\n참가 조건 --\n정책 버전 --","Host --\nRoom code --\nPlayers -- / 4\nMatch rules --\nEntry conditions --\nPolicy revision --",a);
            Words(row,"CodeNotReceipt","입력한 코드는 조회 결과나 입장 승인이 아닙니다.","An entered code is not a room result or entry approval.",a);
            Entry=Disabled(Body.content,"MissingJoinUnavailable","참가 · 연결 대기","Join · unavailable",a);
        }
        private void BuildRoom(MissingMultiplayerArt a)
        {
            var row=Card(a,"RoomIdentity","입장 정보 조회 대기","Room information unavailable",a.Private);
            Words(row,"RoomIdentityText","방 코드 -- · 방장 --\n현재 인원 -- / 4 · 정책 --\n승인된 입장과 참가자 정보를 받은 상태가 아닙니다.","Code -- · Host --\nPlayers -- / 4 · Policy --\nNo approved entry or participant information has been received.",a);
            Copy=Disabled(row,"MissingCopyUnavailable","코드 복사 · 미지원","Copy code · unavailable",a);
            ParticipantSlots=new RectTransform[4];
            for(int i=0;i<4;i++)
            {
                row=Card(a,"ParticipantSlot"+(i+1),"참가자 정보 --","Participant --",a.Avatar);row.GetComponent<Image>().sprite=a.PlayerFrame;ParticipantSlots[i]=row;
                Words(row,"ParticipantState","본인 -- · 방장 --\n연결 -- · 준비 --\n지상 -- · 특수 -- · 공중 --","Self -- · Host --\nConnection -- · Ready --\nGround -- · Special -- · Air --",a);
            }
            row=Card(a,"RoomActionAvailability","대기방 안내","Room actions",a.Invite);
            Words(row,"RoomUnavailable","실제 방 상태와 사용 권한을 조회한 후 준비·시작·초대·나가기를 사용할 수 있습니다. 다른 참가자의 동물 선택은 중복을 제한하지 않습니다.","Ready, start, invite and leave require actual room state and permissions. Animal choices by other players do not reserve or lock animals.",a);
            Ready=Disabled(Body.content,"MissingReadyUnavailable","준비 · 미지원","Ready · unavailable",a);
            StartMatch=Disabled(Body.content,"MissingStartUnavailable","시작 · 미지원","Start · unavailable",a);
            Invite=Disabled(Body.content,"MissingInviteUnavailable","초대 · 미지원","Invite · unavailable",a);
            Leave=Disabled(Body.content,"MissingLeaveUnavailable","방 나가기 · 미지원","Leave room · unavailable",a);
        }
        private void OnEnable()
        {
            navigation=GetComponentInParent<UiNavigationService>();modals=navigation==null?null:navigation.GetComponent<UiModalStack>();
            foreach(var button in GetComponentsInChildren<Button>(true))
                if(button.GetComponent<UiButtonFeedback>()==null)button.gameObject.AddComponent<UiButtonFeedback>();
            Bind(Back,()=>{if(CanNavigate)navigation.Back();});
            Bind(Custom,()=>Navigate(MissingMultiplayerScreen.Custom));Bind(Create,()=>Navigate(MissingMultiplayerScreen.Create));Bind(Join,()=>Navigate(MissingMultiplayerScreen.Join));
            Bind(NormalBrowse,()=>BrowseOnly(MissingMultiplayerContextAction.Normal));
            Bind(RankedBrowse,()=>BrowseOnly(MissingMultiplayerContextAction.Ranked));
            Bind(Browse,()=>BrowseOnly(MissingMultiplayerContextAction.Create));Bind(Paste,PasteCode);
            Bind(Lookup,()=>RequestDevContext(MissingMultiplayerContextAction.Lookup));
            Bind(Entry,()=>RequestDevContext(DevLeaveSavedEntry?MissingMultiplayerContextAction.LeaveSaved:Screen==MissingMultiplayerScreen.Join?MissingMultiplayerContextAction.Join:MissingMultiplayerContextAction.Create));
            Bind(Ready,()=>RoomAction(MissingMultiplayerRoomAction.Ready));
            Bind(StartMatch,()=>RoomAction(MissingMultiplayerRoomAction.Start));
            Bind(Leave,()=>RoomAction(MissingMultiplayerRoomAction.Leave));
            Bind(Copy,()=>RoomAction(MissingMultiplayerRoomAction.Copy));
            // All DEV actions default to disabled until authoritative state is supplied.
        }
        private void OnDisable(){foreach(var remove in removeListeners)remove();removeListeners.Clear();}
        private void Bind(Button b,UnityAction a){if(b==null)return;b.onClick.AddListener(a);removeListeners.Add(()=>b.onClick.RemoveListener(a));}
        private void Navigate(MissingMultiplayerScreen kind){if(CanNavigate)navigation.Navigate(ScreenId(kind));}
        private void BrowseOnly(MissingMultiplayerContextAction action)
        {if(!CanNavigate)return;if(DevContextRequested!=null)DevContextRequested(action);else Host.OpenUnconfigured();}
        private void RequestDevContext(MissingMultiplayerContextAction action)
        {if(CanNavigate)DevContextRequested?.Invoke(action);}
        private void RoomAction(MissingMultiplayerRoomAction action)
        {if(CanNavigate)DevRoomRequested?.Invoke(action);}
        private void PasteCode()
        {
            if(!CanNavigate)return;
            string text=GUIUtility.systemCopyBuffer;
            if(string.IsNullOrEmpty(text)){CodeNotice.Korean="붙여넣을 코드가 없습니다.";CodeNotice.English="No code to paste.";}
            else
            {
                Code.Field.text=text;Code.Field.ActivateInputField();
                CodeNotice.Korean="코드를 입력했습니다. 방 조회 서비스 연결 대기";CodeNotice.English="Code entered. Room lookup service unavailable.";
            }
            CodeNotice.Apply();
        }
    }
}
