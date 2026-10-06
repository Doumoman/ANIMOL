using System;
using System.Linq;
using ANIMOL.Core;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Editor
{
    public static class CampaignMapAccess
    {
        public const string MapFolder = "Assets/ANIMOL/Data/Campaign/Maps";

        public static StageMapDefinition FindConventionalMap(CampaignStageDefinition stage) => stage == null ? null :
            AssetDatabase.LoadAssetAtPath<StageMapDefinition>(MapFolder + "/" + stage.StageId + ".asset");

        public static void ValidateLink(CampaignCatalog catalog, CampaignStageDefinition stage, StageMapDefinition map)
        {
            if (catalog == null || stage == null || map == null || !catalog.EnumerateStages().Contains(stage))
                throw new InvalidOperationException("카탈로그의 스테이지와 저장된 맵을 선택하세요.");
            if (!EditorUtility.IsPersistent(stage) || !EditorUtility.IsPersistent(map))
                throw new InvalidOperationException("저장된 에셋만 연결할 수 있습니다.");
            if (stage.StageId != map.StageId || stage.ThemeId != map.ThemeId)
                throw new InvalidOperationException("스테이지 ID와 테마가 일치하는 맵만 연결할 수 있습니다.");
            if (catalog.EnumerateStages().Any(s => s != stage && (s.StageId == stage.StageId || s.MapDefinition == map)))
                throw new InvalidOperationException("중복 스테이지 ID 또는 다른 스테이지의 맵 연결이 있습니다.");
        }

        public static void Link(CampaignCatalog catalog, CampaignStageDefinition stage, StageMapDefinition map)
        {
            ValidateLink(catalog, stage, map);
            if (stage.MapDefinition == map) return;
            Undo.RecordObject(stage, "Link campaign map");
            var serialized = new SerializedObject(stage);
            serialized.FindProperty("mapDefinition").objectReferenceValue = map;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stage);
            AssetDatabase.SaveAssetIfDirty(stage);
        }

        public static StageMapDefinition CreateAndLink(CampaignCatalog catalog, CampaignStageDefinition stage)
        {
            if (catalog == null || stage == null || !catalog.EnumerateStages().Contains(stage) || !EditorUtility.IsPersistent(stage))
                throw new InvalidOperationException("카탈로그의 저장된 스테이지를 선택하세요.");
            if (stage.MapDefinition != null) return stage.MapDefinition;
            if (string.IsNullOrWhiteSpace(stage.StageId) || stage.StageId.Any(c => !char.IsLetterOrDigit(c) && c != '-'))
                throw new InvalidOperationException("유효하지 않은 스테이지 ID입니다.");
            var path = MapFolder + "/" + stage.StageId + ".asset";
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                throw new InvalidOperationException("같은 경로에 맵이 이미 있습니다. 맵 원본 필드에서 확인하고 연결하세요: " + path);
            var existing = AssetDatabase.FindAssets("t:StageMapDefinition").Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<StageMapDefinition>).FirstOrDefault(m => m != null && m.StageId == stage.StageId);
            if (existing != null)
                throw new InvalidOperationException("이 스테이지의 맵이 이미 있습니다. 새 복사본 대신 기존 맵을 연결하세요: " + AssetDatabase.GetAssetPath(existing));
            EnsureFolder(MapFolder);
            var map = ScriptableObject.CreateInstance<StageMapDefinition>();
            try
            {
                map.EditorInitializeIdentity(stage.StageId, stage.ThemeId);
                map.EditorTrySetChunkBounds(new RectInt(0, 0, 1, 1), false, out _);
                var serialized = new SerializedObject(map);
                serialized.FindProperty("worldUnitsPerCell").floatValue = 1;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                map.FreeShapeTerrain.artVersion = Animol.TerrainStructure.FreeShapeLayer.ArtVersion;
                AssetDatabase.CreateAsset(map, path);
                Link(catalog, stage, map);
                AssetDatabase.SaveAssetIfDirty(map);
                return map;
            }
            catch
            {
                if (AssetDatabase.Contains(map)) AssetDatabase.DeleteAsset(path);
                else UnityEngine.Object.DestroyImmediate(map);
                throw;
            }
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
