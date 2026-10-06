using System;
using System.Linq;
using UnityEngine;

namespace ANIMOL.Core
{
    // Serialized legacy enum values stay reserved. Only these ten devices and campaign markers are active.
    public static class CommonObstacleCatalog
    {
        public const string RegistryResource = "ANIMOL_CommonObstaclesV6";
        public const string RegistryPath = "Assets/ANIMOL/Resources/ANIMOL_CommonObstaclesV6.asset";
        public static bool IsCurrent(StageMapObjectKind kind) => (int)kind >= 1001 && (int)kind <= 1010;
        public static bool IsMarker(StageMapObjectKind kind) => kind is StageMapObjectKind.PlayerStart or StageMapObjectKind.Checkpoint or StageMapObjectKind.BubbleCandidate or StageMapObjectKind.Exit;
        public static bool IsRetired(StageMapObjectKind kind) => !IsCurrent(kind) && !IsMarker(kind);
        public static string Id(StageMapObjectKind kind) => IsCurrent(kind) ? "C" + ((int)kind - 1000).ToString("00") : null;
        public static StageMapObjectTypeRegistry Load()
        {
            var registry=Resources.Load<StageMapObjectTypeRegistry>(RegistryResource);
            if(registry==null || registry.Version!=2 || registry.Types.Count!=10 || registry.Types.Any(t=>t==null || !IsCurrent(t.Kind) || t.Prefab==null || t.StableTypeId!=Id(t.Kind)) || registry.Types.Select(t=>t.Kind).Distinct().Count()!=10)
                throw new InvalidOperationException("V6 common obstacle registry is missing or invalid.");
            return registry;
        }
        public static void ValidateSettings(StageMapObjectPlacement p)
        {
            var s=p.Settings;
            if(!IsCurrent(p.Kind) || s.FootprintCells!=Vector2.one || s.PathCells.Count!=0 || s.LinkedInstanceIds.Count!=0)
                throw new InvalidOperationException("공통 장애물은 경로/연결 없는 고정 1셀입니다.");
            Facing(p);
            foreach(float value in new[]{s.WarningSeconds,s.ActiveSeconds,s.RecoverSeconds,s.VerticalImpulse,s.MovementSpeed,s.ActivationRangeCells,s.EffectStrength})
                if(float.IsNaN(value)||float.IsInfinity(value)||value<0)throw new InvalidOperationException("장애물 설정은 유효한 양수 또는 0이어야 합니다.");
            if(p.Kind is StageMapObjectKind.CommonC04 or StageMapObjectKind.CommonC05 or StageMapObjectKind.CommonC09 && s.WarningSeconds<=0)
                throw new InvalidOperationException("상태 변경에는 예고 시간이 필요합니다.");
            if(p.Kind is StageMapObjectKind.CommonC03 or StageMapObjectKind.CommonC06 && s.VerticalImpulse<=0)
                throw new InvalidOperationException("반동 속도는 양수여야 합니다.");
        }
        public static string Facing(StageMapObjectPlacement p)
        {
            if(p.Kind is StageMapObjectKind.CommonC02 or StageMapObjectKind.CommonC08)
            {
                if(p.Settings.ObstacleFacing == ObstacleFacing.Up) throw new InvalidOperationException("C02/C08 requires LEFT or RIGHT.");
            }
            else if(p.Kind != StageMapObjectKind.CommonC07 && p.Settings.ObstacleFacing != ObstacleFacing.Up)
                throw new InvalidOperationException("This obstacle requires UP.");
            return p.Settings.ObstacleFacing.ToString().ToUpperInvariant();
        }
    }
}
