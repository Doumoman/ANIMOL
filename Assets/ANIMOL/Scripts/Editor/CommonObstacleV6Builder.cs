using System;
using System.IO;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using ANIMOL.Gameplay.ObstacleGraphics;
using Animol.TerrainStructure;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Editor
{
    public static class CommonObstacleV6Builder
    {
        public const string Root="Assets/ANIMOL/ObstacleGraphicsV1/CommonDevices";
        public const string DevMapPath="Assets/ANIMOL/Data/Development/ObstacleGraphicsV1/DEV-COMMON-10-V6.asset";
        private static readonly string[] Names={"하향 발판","좌우 반사","상향 스프링","점멸 위험칸","이탈 소멸판","착지 반동","방향 바람칸","방향 벨트","접근 생성판","저마찰 바닥"};
        private static readonly string[] Descriptions={"위에서 착지, 아래 입력으로 통과", "충돌 전 수평 속도의 부호만 반사", "수평 속도를 유지하고 위로 발사", "공용 시간의 예고 이후 해당 칸 위험", "마지막 탑승자 이탈 후 소멸, 공간이 비면 복구", "착지 시 낮은 반동", "공중 칸의 지정 방향으로 힘 적용", "몸체는 고정, 표면이 좌우로 운반", "접근 예고 후 생성, 이탈 예고 후 소멸", "입력이 없을 때 자연 감속만 완화"};
        public static void Install()
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            var types=new StageMapObjectTypeDefinition[10];
            for(int i=0;i<10;i++)
            {
                string id="C"+(i+1).ToString("00");var kind=(StageMapObjectKind)(1001+i);
                string prefabPath=Root+"/"+id+".prefab",typePath=Root+"/"+id+".asset";
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if(prefab==null)
                {
                    var go=new GameObject(id);go.AddComponent<CommonObstacleObject>();
                    prefab=PrefabUtility.SaveAsPrefabAsset(go,prefabPath);UnityEngine.Object.DestroyImmediate(go);
                }
                var settings=new StageMapObjectSettings();
                settings.EditorConfigure(3,StageMapObjectDirection.Right,Vector2.one,HalfBlockPlacement.Lower,SideSpringContactPolicy.FacingSideOnly,
                    0,i==2?11:i==5?5:0,2,0,0,2,.55f,0,true,Array.Empty<Vector2Int>());
                settings.EditorSetObstacleFacing(i==1||i==7?ObstacleFacing.Right:ObstacleFacing.Up);
                settings.EditorConfigureAuthoring(MapObjectImplementationLevel.DevPlayable,Array.Empty<string>(),0,MapObjectResetPolicy.MapReload,MapObjectRouteRole.Required,0,"");
                settings.EditorConfigureDesignBehavior(.45f,1.5f,1,MapObjectPassengerPolicy.DeferStateChangeWhileOccupied,MapObjectTriggerMode.OnOccupancy,0,0,i==6?32:i==9?.8f:1,true);
                var type=AssetDatabase.LoadAssetAtPath<StageMapObjectTypeDefinition>(typePath);
                if(type==null){type=ScriptableObject.CreateInstance<StageMapObjectTypeDefinition>();AssetDatabase.CreateAsset(type,typePath);}
                type.EditorConfigure(id,id+" · "+Names[i],kind,Vector2.one,1,prefab,settings);
                string facing=i==1||i==7?"RIGHT":"UP",pose=i==8?"inactive":"idle";
                var icon=ObstacleArtRegistry.Load().Sprite("T01/"+id+"/"+facing+"/"+pose);
                type.EditorConfigureCatalog(id,"COMMON",Descriptions[i],StageMapLayer.Object,icon,icon,MapObjectImplementationLevel.DevPlayable,new[]{nameof(CommonObstacleObject)},3,false);
                type.EditorConfigureDesignContract(Descriptions[i],"모든 플레이어의 실제 접촉 상태",Descriptions[i],"맵 재시작 시 초기화",new[]{"obstacleFacing","verticalImpulse","movementSpeed","warningSeconds","activeSeconds","recoverSeconds","activationRangeCells","effectStrength"},new[]{"1 cell","V6 only"});
                EditorUtility.SetDirty(type);AssetDatabase.SaveAssetIfDirty(type);types[i]=type;
            }
            var registry=AssetDatabase.LoadAssetAtPath<StageMapObjectTypeRegistry>(CommonObstacleCatalog.RegistryPath);
            if(registry==null){registry=ScriptableObject.CreateInstance<StageMapObjectTypeRegistry>();AssetDatabase.CreateAsset(registry,CommonObstacleCatalog.RegistryPath);}
            registry.EditorConfigure(2,types);EditorUtility.SetDirty(registry);AssetDatabase.SaveAssetIfDirty(registry);
            if(registry.Types.Count!=10 || registry.Types.Any(t=>!CommonObstacleCatalog.IsCurrent(t.Kind)))throw new InvalidOperationException("Invalid current catalog.");
        }
        public static void CreateDevelopmentMap()
        {
            if(AssetDatabase.LoadMainAssetAtPath(DevMapPath)!=null)throw new InvalidOperationException("Development map already exists.");
            var map=ScriptableObject.CreateInstance<StageMapDefinition>();
            map.EditorInitializeIdentity("DEV-COMMON-10-V6","T01");map.EditorInitializeBoundsFromAuthoredContent(1);
            map.EditorTrySetCellBounds(new RectInt(-20,-4,64,18),false,out _);
            var layer=new FreeShapeLayer();
            for(int x=-20;x<44;x++)layer.cells.Add(new FreeShapeCell{x=x,y=-1,styleId="T01_A"});
            map.FreeShapeTerrain.cells.AddRange(layer.cells);
            var registry=CommonObstacleCatalog.Load();
            for(int i=0;i<10;i++)
            {
                var type=registry.Types[i];var settings=type.DefaultSettings.Clone();settings.EditorSetObstacleStyle("T01_A");
                map.EditorPlaceObject("COMMON-"+(i+1),type.Kind,-14+i*5,0,type.StableTypeId,type.Prefab,settings);
            }
            map.EditorPlaceObject("START",StageMapObjectKind.PlayerStart,-18,1);
            map.EditorPlaceObject("EXIT",StageMapObjectKind.Exit,40,1);
            AssetDatabase.CreateAsset(map,DevMapPath);EditorUtility.SetDirty(map);AssetDatabase.SaveAssetIfDirty(map);
        }
    }
}
