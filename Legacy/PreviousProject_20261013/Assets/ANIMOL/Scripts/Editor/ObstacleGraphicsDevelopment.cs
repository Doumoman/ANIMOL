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
    public static class ObstacleGraphicsDevelopment
    {
        public const string MapPath="Assets/ANIMOL/Data/Development/ObstacleGraphicsV1/DEV-OBSTACLE-V6-JOIN.asset";
        public static void CreateMap()
        {
            if(AssetDatabase.LoadAssetAtPath<StageMapDefinition>(MapPath)!=null)throw new InvalidOperationException("Development map already exists; preserve authored changes.");
            Directory.CreateDirectory(Path.GetDirectoryName(MapPath));AssetDatabase.Refresh();
            var types=AssetDatabase.LoadAssetAtPath<StageMapObjectTypeRegistry>("Assets/ANIMOL/Data/Development/M9Objects/MapObjectTypeRegistry.asset");
            var map=ScriptableObject.CreateInstance<StageMapDefinition>();map.EditorInitializeIdentity("DEV-OBSTACLE-V6-JOIN","T01");map.EditorInitializeBoundsFromAuthoredContent(1);
            map.EditorTrySetCellBounds(new RectInt(-20,-4,44,18),false,out _);
            for(int x=-19;x<23;x++)map.FreeShapeTerrain.cells.Add(new FreeShapeCell{x=x,y=-1,styleId="T01_A"});
            for(int i=0;i<4;i++)for(int kind=0;kind<2;kind++)
            {
                int x=-16+i*9,y=kind*4+1;var style="T01_"+"ABCD"[i];
                map.FreeShapeTerrain.cells.Add(new FreeShapeCell{x=x-1,y=y,styleId=style});
                var type=types.Find(kind==0?StageMapObjectKind.DropPlatform:StageMapObjectKind.MoonLanternStep);
                var settings=type.DefaultSettings.Clone();settings.EditorSetObstacleStyle(style);
                map.EditorPlaceObject("OVG-"+i+"-"+kind,type.Kind,x,y,type.StableTypeId,type.Prefab,settings);
            }
            map.EditorPlaceObject("START",StageMapObjectKind.PlayerStart,-18,1);
            map.EditorPlaceObject("EXIT",StageMapObjectKind.Exit,20,1);
            map.EditorNotifyAuthoredPropertiesChanged();
            ObstacleSnapshotAdapter.Validate(map.FreeShapeTerrain,ObstacleSnapshotAdapter.Preview(map));
            AssetDatabase.CreateAsset(map,MapPath);EditorUtility.SetDirty(map);AssetDatabase.SaveAssetIfDirty(map);
        }
        public static void AuditInstalledTypes()
        {
            var registry=AssetDatabase.LoadAssetAtPath<StageMapObjectTypeRegistry>("Assets/ANIMOL/Data/Development/M9Objects/MapObjectTypeRegistry.asset");
            var lines=registry.Types.Select(t=>string.Join("\t",t.StableTypeId,t.Kind,t.ThemeId,t.ImplementationLevel,t.FootprintCells,
                string.Join(",",t.Prefab.GetComponents<MonoBehaviour>().Where(c=>c!=null).Select(c=>c.GetType().Name)),ObstacleSnapshotAdapter.Availability(t)));
            File.WriteAllLines("Docs/Validation/ObstacleGraphicsV1/operational-types.tsv",new[]{"id\tkind\ttheme\tlevel\tfootprint\tcomponents\tgraphics"}.Concat(lines));
        }
    }
}
