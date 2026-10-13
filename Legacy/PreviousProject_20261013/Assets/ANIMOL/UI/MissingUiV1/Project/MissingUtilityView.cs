using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static ANIMOL.MissingUiV1.Project.MissingUiLayout;
using W=ANIMOL.MissingUiV1.Project.MissingUtilityWidgets;

namespace ANIMOL.MissingUiV1.Project
{
    public enum MissingUtilityScreen { Settings,Controls,Account }

    public sealed class MissingUtilityView:MonoBehaviour
    {
        public MissingUtilityScreen Screen;
        public ScrollRect Body;
        public Button Back,Controls,Accessibility,Account,Help,Records,ResetControls,ResetAll,Language;
        public Button Google,ChooseLocal,ChooseCloud;
        public Slider Size,Position,Opacity,Bgm,Sfx;
        public Toggle LeftHand,Drop,LargeText,Vibration,Mute;
        public MissingUtilityText SizeValue,PositionValue,OpacityValue,LanguageValue,Identity;
        public MissingControlPreview Preview;
        private UiNavigationService navigation;
        private UiModalStack modals;
        private readonly List<Action> removeListeners=new List<Action>();
        public bool ModalOpen=>modals!=null&&modals.Count>0;

        public static MissingUtilityView Build(Transform parent,MissingUtilityArt art,MissingUtilityScreen screen)
        {
            var root=Node("Missing"+screen,parent);Fill(root);root.gameObject.SetActive(false);
            var view=root.gameObject.AddComponent<MissingUtilityView>();view.Screen=screen;
            var title=W.Words(root,"UtilityHeading",screen==MissingUtilityScreen.Settings?"설정":screen==MissingUtilityScreen.Controls?"조작·접근성":"계정·저장",
                screen==MissingUtilityScreen.Settings?"Settings":screen==MissingUtilityScreen.Controls?"Controls & accessibility":"Account & saves",art,48,false);
            title.TextColor=White;Rect((RectTransform)title.transform,Vector2.up,Vector2.one,new Vector2(40,-128),new Vector2(-40,-24));
            view.Body=Scroll(root,art.Common);Rect((RectTransform)view.Body.transform,Vector2.zero,Vector2.one,new Vector2(36,174),new Vector2(-36,-150));
            if(screen==MissingUtilityScreen.Settings)view.BuildSettings(art);
            if(screen==MissingUtilityScreen.Controls)view.BuildControls(art);
            if(screen==MissingUtilityScreen.Account)view.BuildAccount(art);
            var footer=Node("FooterFixed",root);Rect(footer,Vector2.zero,Vector2.right,Vector2.zero,new Vector2(0,160));Picture(footer,null,true).color=Ink;
            var backName=screen==MissingUtilityScreen.Settings?"SettingsBackButton":screen==MissingUtilityScreen.Controls?"ControlSettingsBackButton":"AccountSaveBackButton";
            view.Back=W.Action(footer,backName,"뒤로","Back",art);Fill((RectTransform)view.Back.transform,24);
            return view;
        }
        private void BuildSettings(MissingUtilityArt art)
        {
            var row=W.Row(Body.content,"Music","배경음","Music",art.Music,art);
            W.Words(row,"MusicState","음량 -- · 오디오 설정 서비스 연결 대기","Volume -- · Audio settings service unavailable",art);
            Bgm=W.Slider(row,"MissingMusicSlider",art,0,1,false);
            row=W.Row(Body.content,"Sound","효과음","Sound effects",art.Sound,art);
            W.Words(row,"SoundState","음량 -- · 오디오 설정 서비스 연결 대기","Volume -- · Audio settings service unavailable",art);
            Sfx=W.Slider(row,"MissingSoundSlider",art,0,1,false);
            row=W.Row(Body.content,"Mute","음소거","Mute",art.Mute,art);
            W.Words(row,"MuteState","상태 -- · 실제 음소거 연결 대기","State -- · Mute service unavailable",art);
            Mute=W.Toggle(row,"MissingMuteToggle",art,false);
            row=W.Row(Body.content,"Vibration","중요 사건의 진동","Vibration for important events",art.Vibration,art);
            W.Words(row,"VibrationScope","출구 개방·복귀·승리 등 기존 정책에 적용됩니다. 일반 버튼에는 진동을 추가하지 않습니다.","Uses the existing gameplay feedback policy. Ordinary buttons do not vibrate.",art);
            Vibration=W.Toggle(row,"MissingVibrationToggle",art);
            Controls=W.Action(Body.content,"MissingOpenControls","조작 설정","Controls",art);
            Accessibility=W.Action(Body.content,"MissingOpenAccessibility","접근성·언어","Accessibility & language",art);
            Account=W.Action(Body.content,"MissingOpenAccount","계정·저장","Account & saves",art);
            Help=W.Action(Body.content,"MissingOpenHelp","도움말","Help",art);
            Records=W.Action(Body.content,"MissingOpenRecords","기록","Records",art);
            row=W.Row(Body.content,"ResetScope","설정 초기화","Reset settings",art.Controls,art);
            W.Words(row,"ResetScopeText","전체 설정 초기화는 연결되지 않았습니다. 조작·접근성 초기화는 해당 화면에서 확인 후 실행할 수 있습니다.","Reset all settings is unavailable. Controls and accessibility can be reset after confirmation on their screen.",art);
            ResetAll=W.Action(row,"MissingResetAllUnavailable","전체 초기화 · 미지원","Reset all · unavailable",art);ResetAll.interactable=false;
        }
        private void BuildControls(MissingUtilityArt art)
        {
            var row=W.Row(Body.content,"LayoutPreview","현재 조작 배치","Current control layout",art.Controls,art);
            W.Words(row,"LayoutGuide","현재 HUD와 같은 배치 규칙의 미리보기입니다. 아래 위젯으로 조정합니다.","Preview uses the current HUD layout rules. Adjust it with the controls below.",art);
            Preview=MissingControlPreview.Build(row,art);
            row=W.Row(Body.content,"Size","버튼 크기","Control size",art.Controls,art);
            SizeValue=W.Words(row,"SizeValue","","",art);Size=W.Slider(row,"MissingSizeSlider",art,.75f,1.35f);
            row=W.Row(Body.content,"Position","가로 위치","Horizontal position",art.HandRight,art);
            PositionValue=W.Words(row,"PositionValue","","",art);Position=W.Slider(row,"MissingPositionSlider",art,-.08f,.08f);
            W.Words(row,"PositionNote","화면 밖으로 나가지 않도록 기존 HUD 규칙이 위치를 제한합니다.","The existing HUD rules keep controls inside the screen.",art);
            row=W.Row(Body.content,"Opacity","투명도","Opacity",art.Opacity,art);
            OpacityValue=W.Words(row,"OpacityValue","","",art);Opacity=W.Slider(row,"MissingOpacitySlider",art,.3f,1f);
            row=W.Row(Body.content,"Handedness","왼손 배치","Left-handed layout",art.HandLeft,art);LeftHand=W.Toggle(row,"MissingLeftHandToggle",art);
            row=W.Row(Body.content,"Drop","전용 하향 버튼","Dedicated drop button",art.Controls,art);Drop=W.Toggle(row,"MissingDropToggle",art);
            row=W.Row(Body.content,"Accessibility","큰 글씨","Large text",art.TextSize,art);LargeText=W.Toggle(row,"MissingLargeTextToggle",art);
            row=W.Row(Body.content,"Vibration","중요 사건의 진동","Vibration for important events",art.Vibration,art);Vibration=W.Toggle(row,"MissingVibrationToggle",art);
            row=W.Row(Body.content,"Language","언어 선택","Language preference",art.Language,art);
            LanguageValue=W.Words(row,"LanguageValue","","",art);
            W.Words(row,"LanguageScope","언어 선택은 저장됩니다. 기존 콘텐츠 전체의 번역이 완료된 것은 아닙니다.","Your language preference is saved. Existing game content is not fully translated.",art);
            Language=W.Action(row,"MissingLanguageChoice","한국어 / English","English / 한국어",art);
            ResetControls=W.Action(Body.content,"MissingResetControls","조작·접근성 초기화","Reset controls & accessibility",art);
        }
        private void BuildAccount(MissingUtilityArt art)
        {
            var row=W.Row(Body.content,"Identity","계정 상태","Account state",art.Account,art);
            Identity=W.Words(row,"IdentityValue","-- · 계정 정보 조회 대기","-- · Account information unavailable",art);
            W.Words(row,"AccountData","계정 이름 --\n검증된 코인 --\n계정 서비스 연결 대기","Account name --\nVerified coins --\nAccount service unavailable",art);
            Google=W.Action(row,"MissingGoogleUnavailable","계정 연결 · 준비 중","Link account · unavailable",art);Google.interactable=false;
            W.Words(row,"GoogleNotice","Google 로그인 서비스가 연결되지 않았습니다.","Google sign-in service is not connected.",art);
            row=W.Row(Body.content,"LocalSave","로컬 기록","Local record",art.LocalSave,art);
            W.Words(row,"LocalRecord","진행도 --\n갱신 시각 --\n저장 요약 조회 계약 연결 대기","Progress --\nUpdated --\nSave-summary query unavailable",art);
            ChooseLocal=W.Action(row,"MissingLocalSaveUnavailable","로컬 저장 선택 · 미지원","Choose local · unavailable",art);ChooseLocal.interactable=false;
            row=W.Row(Body.content,"CloudSave","클라우드 기록","Cloud record",art.Account,art);
            W.Words(row,"CloudRecord","진행도 --\n갱신 시각 --\n클라우드 조회 연결 대기","Progress --\nUpdated --\nCloud query unavailable",art);
            ChooseCloud=W.Action(row,"MissingCloudSaveUnavailable","클라우드 저장 선택 · 미지원","Choose cloud · unavailable",art);ChooseCloud.interactable=false;
            row=W.Row(Body.content,"SaveScope","저장 데이터 비교","Compare saved data",art.LocalSave,art);
            W.Words(row,"SaveScopeText","실제 기록 조회와 충돌 해결 서비스가 연결되기 전에는 저장 선택·동기화·덮어쓰기를 실행할 수 없습니다. 미조회 값은 0이나 기록 없음으로 표시하지 않습니다.","Selecting, synchronizing or replacing a save is unavailable until real record queries and conflict resolution are connected. Unknown values are not zero or an empty save.",art);
        }
        private void OnEnable()
        {
            navigation=GetComponentInParent<UiNavigationService>();modals=navigation==null?null:navigation.GetComponent<UiModalStack>();
            Bind(Back,()=>{if(!ModalOpen)navigation.Back();});
            Bind(Controls,()=>Navigate("SC19_ControlSettings"));
            Bind(Accessibility,()=>{if(ModalOpen)return;Navigate("SC19_ControlSettings");var view=navigation.GetComponentsInChildren<MissingUtilityView>(true).First(v=>v.Screen==MissingUtilityScreen.Controls);view.Body.verticalNormalizedPosition=0;});
            Bind(Account,()=>Navigate("SC20_AccountAndSave"));Bind(Help,()=>Navigate("SC14_Help"));Bind(Records,()=>Navigate("SC17_ProfileRecords"));
            Bind(ResetControls,()=>{if(!ModalOpen)modals.Push(MissingControlReset.DialogName);});
            Bind(Language,()=>Change(()=>MobileControlPreferences.Language=MobileControlPreferences.Language==UiLanguage.Korean?UiLanguage.English:UiLanguage.Korean));
            Bind(Size,v=>Change(()=>MobileControlPreferences.SizeScale=v));Bind(Position,v=>Change(()=>MobileControlPreferences.HorizontalOffset=v));Bind(Opacity,v=>Change(()=>MobileControlPreferences.Opacity=v));
            Bind(LeftHand,v=>Change(()=>MobileControlPreferences.Handedness=v?ControlHandedness.Left:ControlHandedness.Right));Bind(Drop,v=>Change(()=>MobileControlPreferences.UseDedicatedDropButton=v));
            Bind(LargeText,v=>Change(()=>MobileControlPreferences.LargeText=v));Bind(Vibration,v=>Change(()=>MobileControlPreferences.Vibration=v));
            Refresh();
        }
        private void OnDisable(){foreach(var remove in removeListeners)remove();removeListeners.Clear();}
        private void Navigate(string id){if(!ModalOpen)navigation.Navigate(id);}
        private void Bind(Button b,UnityAction a){if(b==null)return;b.onClick.AddListener(a);removeListeners.Add(()=>b.onClick.RemoveListener(a));}
        private void Bind(Slider s,UnityAction<float> a){if(s==null)return;s.onValueChanged.AddListener(a);removeListeners.Add(()=>s.onValueChanged.RemoveListener(a));}
        private void Bind(Toggle t,UnityAction<bool> a){if(t==null)return;t.onValueChanged.AddListener(a);removeListeners.Add(()=>t.onValueChanged.RemoveListener(a));}
        private void Change(Action change)
        {
            if(ModalOpen){Refresh();return;}
            change();PlayerPrefs.Save();ApplyPreferences();
        }
        public static void ApplyPreferences()
        {
            foreach(var layout in FindObjectsByType<MobileControlLayoutApplier>(FindObjectsInactive.Include,FindObjectsSortMode.None))layout.Apply();
            foreach(var layout in FindObjectsByType<PortraitAccessibilityLayout>(FindObjectsInactive.Include,FindObjectsSortMode.None))layout.Apply();
            foreach(var view in FindObjectsByType<MissingUtilityView>(FindObjectsInactive.Include,FindObjectsSortMode.None))view.Refresh();
        }
        public void Refresh()
        {
            if(Size!=null){Size.SetValueWithoutNotify(MobileControlPreferences.SizeScale);Set(SizeValue,$"크기 {MobileControlPreferences.SizeScale:0.00}",$"Scale {MobileControlPreferences.SizeScale:0.00}");}
            if(Position!=null){Position.SetValueWithoutNotify(MobileControlPreferences.HorizontalOffset);Set(PositionValue,$"위치 {MobileControlPreferences.HorizontalOffset:0.00}",$"Offset {MobileControlPreferences.HorizontalOffset:0.00}");}
            if(Opacity!=null){Opacity.SetValueWithoutNotify(MobileControlPreferences.Opacity);Set(OpacityValue,$"불투명도 {MobileControlPreferences.Opacity:0%}",$"Opacity {MobileControlPreferences.Opacity:0%}");}
            if(LeftHand!=null)LeftHand.SetIsOnWithoutNotify(MobileControlPreferences.Handedness==ControlHandedness.Left);
            if(Drop!=null)Drop.SetIsOnWithoutNotify(MobileControlPreferences.UseDedicatedDropButton);
            if(LargeText!=null)LargeText.SetIsOnWithoutNotify(MobileControlPreferences.LargeText);
            if(Vibration!=null)Vibration.SetIsOnWithoutNotify(MobileControlPreferences.Vibration);
            if(LanguageValue!=null)Set(LanguageValue,MobileControlPreferences.Language==UiLanguage.Korean?"현재 선택: 한국어":"현재 선택: English",MobileControlPreferences.Language==UiLanguage.Korean?"Selected: Korean":"Selected: English");
            if(Identity!=null)
            {
                var owner=GetComponentInParent<GrowthEconomyUiPresenter>();
                if(owner!=null&&owner.IdentityState==AccountIdentityState.Guest)Set(Identity,"게스트 · 계정 서비스 미연결","Guest · Account service unavailable");
                else Set(Identity,"-- · 계정 정보 조회 대기","-- · Account information unavailable");
            }
            if(Preview!=null)Preview.Apply();
        }
        private static void Set(MissingUtilityText text,string ko,string en){text.Korean=ko;text.English=en;text.Apply();}
    }
}
