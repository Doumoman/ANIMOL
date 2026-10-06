using System.Linq;
using ANIMOL.Development;
using ANIMOL.Gameplay.ObstacleGraphics;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Editor
{
    public static partial class TerrainEditorSceneEditor
    {
        private static Vector2 obstacleScroll;
        public static void SetObstacleMode()
        {
            SetFreeShapeMode(false); state.category="Obstacles"; state.partId="";
            state.paletteHeight=Mathf.Max(.32f,state.paletteHeight);
            status="장애물 · 현재 테마와 공통 장치 · 공통 10종"; view?.Repaint();
        }
        private static void DrawCommonSettings(SerializedObject serialized,ANIMOL.Core.StageMapObjectKind kind)
        {
            string[] fields=kind switch {
                ANIMOL.Core.StageMapObjectKind.CommonC02=>new[]{"obstacleFacing"},
                ANIMOL.Core.StageMapObjectKind.CommonC03 or ANIMOL.Core.StageMapObjectKind.CommonC06=>new[]{"verticalImpulse"},
                ANIMOL.Core.StageMapObjectKind.CommonC04=>new[]{"warningSeconds","activeSeconds","recoverSeconds","phaseSeed"},
                ANIMOL.Core.StageMapObjectKind.CommonC05=>new[]{"warningSeconds","activeSeconds"},
                ANIMOL.Core.StageMapObjectKind.CommonC07=>new[]{"obstacleFacing","effectStrength"},
                ANIMOL.Core.StageMapObjectKind.CommonC08=>new[]{"obstacleFacing","movementSpeed"},
                ANIMOL.Core.StageMapObjectKind.CommonC09=>new[]{"activationRangeCells","warningSeconds"},
                ANIMOL.Core.StageMapObjectKind.CommonC10=>new[]{"effectStrength"},
                _=>System.Array.Empty<string>()};
            foreach(var name in fields)
            {
                string label=name switch {"obstacleFacing"=>"방향","verticalImpulse"=>"반동 속도","warningSeconds"=>"예고 시간","activeSeconds"=>kind==ANIMOL.Core.StageMapObjectKind.CommonC05?"소멸 시간":"위험 시간","recoverSeconds"=>"안전 시간","phaseSeed"=>"공용 위상 오프셋","effectStrength"=>kind==ANIMOL.Core.StageMapObjectKind.CommonC10?"자연 감속":"바람 가속도","movementSpeed"=>"운반 속도","activationRangeCells"=>"접근 거리 (셀)",_=>name};
                EditorGUILayout.PropertyField(serialized.FindProperty("value").FindPropertyRelative(name),new GUIContent(label));
            }
        }
        private static void DrawObstaclePalette()
        {
            EditorGUI.DrawRect(paletteRect,TerrainEditorScreen.Panel);
            GUILayout.BeginArea(new Rect(8,paletteRect.y+5,paletteRect.width-16,52));GUILayout.BeginHorizontal();
            if(GUILayout.Button("자유형 지형 V6",buttonStyle,GUILayout.Width(125)))SetFreeShapeMode(true);
            if(GUILayout.Button("캠페인 마커",buttonStyle,GUILayout.Width(110))){state.category="Markers";state.partId="";}
            GUILayout.Label("장애물 재료",smallStyle,GUILayout.Width(80));
            var styles=SelectedFreeShapeArt.styles.Where(s=>s.themeId==adapter.Map.ThemeId).ToArray();
            int index=System.Array.FindIndex(styles,s=>s.styleId==state.freeStyle);
            var selected=EditorGUILayout.Popup(Mathf.Max(0,index),styles.Select(s=>s.styleId+" · "+s.displayName).ToArray(),GUILayout.Width(210));
            if(state.freeStyle!=styles[selected].styleId){Cancel(false);state.freeStyle=styles[selected].styleId;}
            var retired=adapter.Map.Objects.Count(o=>ANIMOL.Core.CommonObstacleCatalog.IsRetired(o.Kind));
            if(retired>0 && GUILayout.Button("폐기 기록 "+retired+"개 정리",buttonStyle))Run(()=>adapter.RemoveRetiredObstacles());
            GUILayout.EndHorizontal();
            GUILayout.Label("C01~C10 · 5테마 공통 기능 · 선택한 V6 재료로 접합합니다.",smallStyle);
            GUILayout.EndArea();
            var items=parts.Where(p=>p.obj!=null && (p.theme=="COMMON"||p.theme==adapter.Map.ThemeId) &&
                ANIMOL.Core.CommonObstacleCatalog.IsCurrent(p.obj.Kind)).ToArray();
            float width=190,height=100;int columns=Mathf.Max(1,Mathf.FloorToInt((paletteRect.width-24)/(width+6)));
            obstacleScroll=GUI.BeginScrollView(new Rect(5,paletteRect.y+62,paletteRect.width-10,paletteRect.height-68),obstacleScroll,
                new Rect(0,0,columns*(width+6),Mathf.CeilToInt(items.Length/(float)columns)*(height+6)));
            for(int i=0;i<items.Length;i++)
            {
                var p=items[i];var rect=new Rect(i%columns*(width+6),i/columns*(height+6),width,height);
                EditorGUI.DrawRect(rect,p.id==state.partId?TerrainEditorScreen.Hex("5d275d"):TerrainEditorScreen.Ink);
                var kind=ObstacleSnapshotAdapter.Kind(p.obj.Kind);
                if(kind!=null)
                {
                    var body=SelectedFreeShapeArt.Cell(state.freeStyle,0,0);var br=body.rect;
                    if(kind is "C02" or "C03" or "C08" or "C10")
                        GUI.DrawTextureWithTexCoords(new Rect(rect.x+6,rect.y+5,48,48),body.texture,new Rect(br.x/body.texture.width,br.y/body.texture.height,32f/body.texture.width,32f/body.texture.height));
                    else if(kind is "C01" or "C05" or "C06")
                        GUI.DrawTextureWithTexCoords(new Rect(rect.x+6,rect.y+5,48,12),body.texture,new Rect(br.x/body.texture.width,(br.y+24)/body.texture.height,32f/body.texture.width,8f/body.texture.height));
                    var sprite=ObstacleArtRegistry.Load().Sprite(adapter.Map.ThemeId+"/"+kind+"/"+(kind is "C02" or "C08"?"RIGHT":"UP")+"/"+(kind=="C09"?"inactive":"idle"));var r=sprite.rect;
                    GUI.DrawTextureWithTexCoords(new Rect(rect.x+6,rect.y+5,48,48),sprite.texture,new Rect(r.x/sprite.texture.width,r.y/sprite.texture.height,r.width/sprite.texture.width,r.height/sprite.texture.height));
                }
                else if(p.obj.Icon!=null){var sprite=p.obj.Icon;var r=sprite.rect;GUI.DrawTextureWithTexCoords(new Rect(rect.x+6,rect.y+5,48,48),sprite.texture,new Rect(r.x/sprite.texture.width,r.y/sprite.texture.height,r.width/sprite.texture.width,r.height/sprite.texture.height));}
                GUI.Label(new Rect(rect.x+60,rect.y+5,width-64,44),p.name,smallStyle);
                GUI.Label(new Rect(rect.x+6,rect.y+55,width-12,40),ObstacleSnapshotAdapter.Availability(p.obj)+"\n"+p.size.x+"×"+p.size.y+"셀",smallStyle);
                if(GUI.Button(rect,new GUIContent("",p.obj.EffectDescription+"\n"+p.obj.Description),GUIStyle.none))SelectPart(p.id);
            }
            GUI.EndScrollView();
        }
    }
}
