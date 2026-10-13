using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using ANIMOL.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.Development
{
    public sealed partial class TerrainEditorScreen
    {
        private RectTransform settingsContent,settingsViewport;
        private Button applySettings;
        private StageMapObjectSettings settingsDraft;
        private string settingsInstance;
        private readonly Dictionary<FieldInfo,InputField> settingInputs=new Dictionary<FieldInfo,InputField>();
        private static readonly Dictionary<string,string> SettingNames=new Dictionary<string,string>
        {
            {"direction","진행 방향"},{"footprintCells","가로,세로 범위"},{"halfPlacement","반블록 위치"},{"movementSpeed","이동 속도"},
            {"horizontalImpulse","가로 힘"},{"verticalImpulse","세로 힘"},{"warningSeconds","경고 시간"},{"activeSeconds","활성 시간"},{"recoverSeconds","복구 시간"},
            {"pathCells","경로 X,Y; X,Y"},{"linkedInstanceIds","연결 instance ID; ID"},{"minimumLinkCount","최소 연결 수"},{"phaseSeed","위상 시드"},
            {"resetPolicy","초기화 방식"},{"routeRole","경로 역할"},{"passengerPolicy","탑승 정책"},{"triggerMode","동작 조건"},
            {"upperPauseSeconds","상단 대기"},{"lowerPauseSeconds","하단 대기"},{"groundSpeedMultiplier","지상 속도 배율"},{"endStopSeconds","끝 대기"},
            {"activationRangeCells","활성 범위"},{"returnWhenEmpty","비었을 때 복귀"},{"arcHeightCells","곡선 높이"},{"rotationDegrees","동작 회전각"},
            {"effectStrength","동작 세기"},{"deferWhileOccupied","탑승 중 변경 연기"},{"springContactPolicy","스프링 접촉"},{"prototypeNotice","개발 참고"}
        };
        private void InitializeSettings()
        {
            settingsViewport=PanelRect("Object settings scroll",infoPanel,Ink,new Vector2(.03f,.1f),new Vector2(.97f,.44f));
            settingsViewport.gameObject.AddComponent<RectMask2D>();
            var scroll=settingsViewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.scrollSensitivity=24;
            settingsContent=new GameObject("Object settings",typeof(RectTransform)).GetComponent<RectTransform>();settingsContent.SetParent(settingsViewport,false);
            settingsContent.anchorMin=new Vector2(0,1);settingsContent.anchorMax=Vector2.one;settingsContent.pivot=new Vector2(.5f,1);
            scroll.viewport=settingsViewport;scroll.content=settingsContent;
            applySettings=Button("Apply settings",infoPanel,"객체 설정 적용",.04f,.96f,()=>Run(SaveSettings));
            Stretch(applySettings.GetComponent<RectTransform>(),new Vector2(.04f,.01f),new Vector2(.96f,.09f));
        }
        private void RefreshSettings()
        {
            if(settingsContent==null)return;
            var obj=Adapter.Map.Objects.FirstOrDefault(o=>o.StableId==State.instanceId);
            settingsViewport.gameObject.SetActive(obj!=null);applySettings.gameObject.SetActive(obj!=null);
            if(obj==null){settingsInstance=null;return;}
            var signature=obj.StableId+":"+Adapter.Map.AuthoringRevision;
            if(signature==settingsInstance)return;
            settingsInstance=signature;settingsDraft=obj.Settings.Clone();settingInputs.Clear();
            for(int i=settingsContent.childCount-1;i>=0;i--){var child=settingsContent.GetChild(i).gameObject;child.SetActive(false);Destroy(child);}
            var fields=typeof(StageMapObjectSettings).GetFields(BindingFlags.Instance|BindingFlags.NonPublic).Where(f=>f.IsDefined(typeof(SerializeField),false)).ToArray();
            float y=0;
            foreach(var field in fields)
            {
                if(field.Name=="version" || field.Name=="implementationLevel")continue;
                var row=PanelRect(field.Name,settingsContent,Panel,new Vector2(0,1),Vector2.one);row.pivot=new Vector2(.5f,1);row.sizeDelta=new Vector2(0,54);row.anchoredPosition=new Vector2(0,-y);y+=58;
                var label=Label("Name",row,SettingNames.TryGetValue(field.Name,out var name)?name:field.Name,13);Stretch(label.rectTransform,new Vector2(.02f,.56f),new Vector2(.98f,1));
                var input=Input("Value",row,"",new Vector2(.02f,.03f),new Vector2(.98f,.55f));input.text=FormatSetting(field.GetValue(settingsDraft));
                input.interactable=obj.Settings.Version<=3;settingInputs.Add(field,input);
            }
            settingsContent.sizeDelta=new Vector2(0,y);applySettings.interactable=obj.Settings.Version<=3;
        }
        private static string FormatSetting(object value)
        {
            if(value is List<Vector2Int> points)return string.Join("; ",points.Select(v=>v.x+","+v.y));
            if(value is List<string> ids)return string.Join("; ",ids);
            if(value is Vector2 v)return v.x.ToString(CultureInfo.InvariantCulture)+","+v.y.ToString(CultureInfo.InvariantCulture);
            return Convert.ToString(value,CultureInfo.InvariantCulture);
        }
        private void SaveSettings()
        {
            if(IsTesting || settingsDraft==null)return;
            var candidate=settingsDraft.Clone();
            foreach(var pair in settingInputs)
            {
                var field=pair.Key;var text=pair.Value.text;var type=field.FieldType;object value;
                if(type==typeof(float)){var f=float.Parse(text,CultureInfo.InvariantCulture);if(float.IsNaN(f)||float.IsInfinity(f))throw new InvalidOperationException("유한한 숫자를 입력하세요.");value=f;}
                else if(type==typeof(int))value=int.Parse(text,CultureInfo.InvariantCulture);
                else if(type==typeof(bool))value=bool.Parse(text);
                else if(type.IsEnum){value=Enum.Parse(type,text,true);if(!Enum.IsDefined(type,value))throw new InvalidOperationException("유효한 선택값을 입력하세요: "+field.Name);}
                else if(type==typeof(Vector2)){var xy=text.Split(',');value=new Vector2(float.Parse(xy[0],CultureInfo.InvariantCulture),float.Parse(xy[1],CultureInfo.InvariantCulture));}
                else if(type==typeof(List<Vector2Int>))value=text.Split(';').Where(s=>!string.IsNullOrWhiteSpace(s)).Select(s=>{var xy=s.Split(',');return new Vector2Int(int.Parse(xy[0]),int.Parse(xy[1]));}).ToList();
                else if(type==typeof(List<string>))value=text.Split(';').Where(s=>!string.IsNullOrWhiteSpace(s)).Select(s=>s.Trim()).ToList();
                else value=text;
                field.SetValue(candidate,value);
            }
            Adapter.CommitObject(new TerrainEditorObjectEdit{operation="Settings",instanceId=State.instanceId,settings=candidate});
            SetStatus("객체 설정 저장 완료");
        }
    }
}
