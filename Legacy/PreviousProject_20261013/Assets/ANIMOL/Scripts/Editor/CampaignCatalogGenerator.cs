using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Core;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Editor
{
    public static class CampaignCatalogGenerator
    {
        public const string CatalogPath = "Assets/ANIMOL/Data/Campaign/CampaignCatalog.asset";
        private const string ThemeFolder = "Assets/ANIMOL/Data/Campaign/Themes";
        private const string StageFolder = "Assets/ANIMOL/Data/Campaign/Stages";

        public static CampaignCatalog BuildOrUpdate()
        {
            EnsureFolder("Assets/ANIMOL");
            EnsureFolder("Assets/ANIMOL/Data");
            EnsureFolder("Assets/ANIMOL/Data/Campaign");
            EnsureFolder(ThemeFolder);
            EnsureFolder(StageFolder);

            var themes = new List<ThemeDefinition>(5);
            CampaignStageDefinition previous = null;
            for (var themeNumber = 1; themeNumber <= 5; themeNumber++)
            {
                var themeId = $"T{themeNumber:00}";
                var themePath = $"{ThemeFolder}/{themeId}.asset";
                var theme = AssetDatabase.LoadAssetAtPath<ThemeDefinition>(themePath);
                var isNewTheme = theme == null;
                if (isNewTheme)
                {
                    theme = ScriptableObject.CreateInstance<ThemeDefinition>();
                    AssetDatabase.CreateAsset(theme, themePath);
                    Set(theme, "themeId", themeId);
                    Set(theme, "displayOrder", themeNumber);
                    Set(theme, "displayName", $"테마 {themeNumber:00}");
                    Set(theme, "description", "실제 테마 콘텐츠 연결 전 임시 슬롯");
                    Set(theme, "lockedDescription", "이전 테마를 완료하면 열립니다.");
                    Set(theme, "completedDescription", "테마 완료");
                }

                var stages = new CampaignStageDefinition[20];
                for (var stageNumber = 1; stageNumber <= 20; stageNumber++)
                {
                    var stageId = $"{themeId}-S{stageNumber:00}";
                    var stagePath = $"{StageFolder}/{stageId}.asset";
                    var stage = AssetDatabase.LoadAssetAtPath<CampaignStageDefinition>(stagePath);
                    if (stage == null)
                    {
                        stage = ScriptableObject.CreateInstance<CampaignStageDefinition>();
                        AssetDatabase.CreateAsset(stage, stagePath);
                        Set(stage, "stageId", stageId);
                        Set(stage, "themeId", themeId);
                        Set(stage, "stageOrder", stageNumber);
                        Set(stage, "displayName", $"스테이지 {stageNumber:00}");
                        Set(stage, "prerequisiteStageId", previous == null ? string.Empty : previous.StageId);
                        Set(stage, "targetBubbleCount", 3);
                        Set(stage, "contentVersion", 1);
                    }
                    stages[stageNumber - 1] = stage;
                    previous = stage;
                }

                SetObjectArray(theme, "stages", stages);
                themes.Add(theme);
            }

            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CampaignCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            SetObjectArray(catalog, "themes", themes.ToArray());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return catalog;
        }

        private static void Set(UnityEngine.Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void Set(UnityEngine.Object target, string propertyName, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetObjectArray<T>(UnityEngine.Object target, string propertyName, T[] values) where T : UnityEngine.Object
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrWhiteSpace(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent ?? "Assets", name);
        }
    }
}
