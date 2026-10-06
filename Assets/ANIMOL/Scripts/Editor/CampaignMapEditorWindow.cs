using System;
using System.Linq;
using ANIMOL.Core;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace ANIMOL.Editor
{
    // Navigation only. The Scene editor exclusively owns the editing session.
    public sealed class CampaignMapEditorWindow : EditorWindow
    {
        [SerializeField] private CampaignCatalog catalog;
        [SerializeField] private CampaignStageDefinition selectedStage;
        [SerializeField] private StageMapDefinition selectedMap;
        private string search = "", error = "";
        private Vector2 scroll;

        [MenuItem("ANIMOL/Campaign Maps %#m", priority = 0)]
        public static void Open() => GetWindow<CampaignMapEditorWindow>("Campaign Maps");
        public static void OpenM8() => Open();
        public static void OpenScenePalette() => TerrainEditorSceneEditor.OpenSelected();
        public static CampaignMapEditorWindow OpenForWorkspace(StageMapDefinition map)
        {
            var window = GetWindow<CampaignMapEditorWindow>("Campaign Maps");
            window.selectedMap = map;
            window.selectedStage = window.catalog?.EnumerateStages().FirstOrDefault(s => s.MapDefinition == map);
            window.Repaint();
            return window;
        }
        public static CampaignMapEditorWindow OpenForManualSession(StageMapDefinition map, string backupPath) => OpenForWorkspace(map);
        public static void SelectWorkspaceProxy(StageMapAuthoringProxy proxy)
        { if (proxy != null) OpenForWorkspace(proxy.Map); }

        [OnOpenAsset]
        private static bool OpenAsset(int id, int line)
        {
            var asset = EditorUtility.InstanceIDToObject(id);
            if (asset is CampaignStageDefinition stage)
            {
                var window = GetWindow<CampaignMapEditorWindow>("Campaign Maps");
                window.selectedStage = stage; window.selectedMap = stage.MapDefinition;
                if (stage.MapDefinition != null) window.Edit(stage.MapDefinition);
                return true;
            }
            if (asset is StageMapDefinition map) { OpenForWorkspace(map).Edit(map); return true; }
            return false;
        }
        private void OnEnable()
        {
            minSize = new Vector2(620, 420);
            if (catalog == null) catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignCatalogGenerator.CatalogPath);
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("캠페인 맵 · 자유형 지형", EditorStyles.boldLabel);
            search = EditorGUILayout.TextField("스테이지 / 테마 검색", search);
            if (catalog == null) { EditorGUILayout.HelpBox("캠페인 카탈로그가 없습니다.", MessageType.Error); return; }
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                scroll = EditorGUILayout.BeginScrollView(scroll);
                foreach (var theme in catalog.Themes.Where(t => t != null))
                {
                    EditorGUILayout.LabelField(theme.ThemeId, EditorStyles.boldLabel);
                    foreach (var stage in theme.Stages.Where(s => s != null))
                    {
                        if (!string.IsNullOrWhiteSpace(search) && (stage.StageId + " " + stage.DisplayName).IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Toggle(selectedStage == stage, stage.StageId + "  " + stage.DisplayName, "Button", GUILayout.MinWidth(180)))
                            { if (selectedStage != stage) { selectedStage = stage; selectedMap = stage.MapDefinition ?? CampaignMapAccess.FindConventionalMap(stage); error = ""; } }
                            GUILayout.Label(stage.MapDefinition == null ? "맵 미연결" : stage.MapDefinition.name, GUILayout.MinWidth(120));
                            using (new EditorGUI.DisabledScope(stage.MapDefinition == null))
                                if (GUILayout.Button("편집", GUILayout.Width(65))) { selectedStage = stage; selectedMap = stage.MapDefinition; Edit(selectedMap); }
                        }
                    }
                }
                EditorGUILayout.EndScrollView();
                EditorGUILayout.Space();
                if (selectedStage != null)
                {
                    EditorGUILayout.LabelField(selectedStage.StageId + " · 실제 캠페인 연결", EditorStyles.boldLabel);
                    selectedMap = (StageMapDefinition)EditorGUILayout.ObjectField("맵 원본", selectedMap, typeof(StageMapDefinition), false);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUI.DisabledScope(selectedMap == null || selectedStage.MapDefinition == selectedMap))
                            if (GUILayout.Button("선택한 맵 연결")) Run(() => CampaignMapAccess.Link(catalog, selectedStage, selectedMap));
                        if (selectedStage.MapDefinition == null && GUILayout.Button("빈 V4 맵 생성 및 연결"))
                            Run(() => { selectedMap = CampaignMapAccess.CreateAndLink(catalog, selectedStage); Edit(selectedMap); });
                        using (new EditorGUI.DisabledScope(selectedStage.MapDefinition == null))
                            if (GUILayout.Button("연결된 맵 편집")) Edit(selectedStage.MapDefinition);
                    }
                }
                if (selectedMap != null)
                {
                    EditorGUILayout.SelectableLabel(AssetDatabase.GetAssetPath(selectedMap), GUILayout.Height(20));
                    if (selectedStage == null && GUILayout.Button("선택한 맵 편집")) Edit(selectedMap);
                }
            }
            if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
            EditorGUILayout.HelpBox("연결된 맵 원본에 저장됩니다. 맵 연결만으로 Ready 상태나 플레이 검증이 완료되지는 않습니다.", MessageType.None);
        }
        private void Edit(StageMapDefinition map) => Run(() => TerrainEditorSceneEditor.Open(map));
        private void Run(Action action) { try { action(); error = ""; } catch (Exception ex) { error = ex.Message; } }
    }
}
