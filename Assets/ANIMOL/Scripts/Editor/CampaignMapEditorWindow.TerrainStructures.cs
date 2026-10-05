using System;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using Animol.TerrainStructure;
using Animol.TerrainStructure.Editor;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Editor
{
    public sealed partial class CampaignMapEditorWindow
    {
        private StageMapDefinition boundTerrainMap;
        private int structureThemeFilter;
        private Vector2 structureScroll;
        private void BindTerrainStructures()
        {
            if (boundTerrainMap != null && boundTerrainMap != selectedMap) AnimolTerrainMapEditorBridge.Unbind(boundTerrainMap);
            boundTerrainMap = selectedMap;
            if (selectedMap == null) return;
            var map = selectedMap;
            AnimolTerrainMapEditorBridge.Bind(map, () => map.ReadTerrain(StageTerrainStructureRegistry.Load()),
                candidate => StageTerrainStructureIntegration.Write(map, candidate),
                () => { StageTerrainStructureIntegration.Refresh(map); Repaint(); }, map.StageId,
                candidate => map.ValidateTerrainCandidate(StageTerrainStructureRegistry.Load(), candidate));
        }
        private void UnbindTerrainStructures()
        { if (boundTerrainMap != null) AnimolTerrainMapEditorBridge.Unbind(boundTerrainMap); boundTerrainMap = null; }

        private void DrawTerrainStructurePanel()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("원본 디자인 · 완성 부품 (v3)", EditorStyles.boldLabel);
            var registry = Resources.Load<StageTerrainStructureRegistry>("ANIMOL_TerrainStructureRegistry");
            if (registry == null)
            {
                if (GUILayout.Button("원본 5장 / 60부품 등록")) StageTerrainStructureIntegration.Initialize();
                return;
            }
            BindTerrainStructures();
            try { selectedMap.ReadTerrain(registry); }
            catch (Exception ex) { EditorGUILayout.HelpBox(ex.Message, MessageType.Error); return; }
            structureThemeFilter = EditorGUILayout.Popup("Theme filter", structureThemeFilter, new[] { "현재 맵", "T01", "T02", "T03", "T04", "T05" });
            var theme = structureThemeFilter == 0 ? selectedMap.ThemeId : "T" + structureThemeFilter.ToString("00");
            structureScroll = EditorGUILayout.BeginScrollView(structureScroll, GUILayout.Height(230));
            foreach (var entry in registry.Catalog.entries.Where(e => e.themeId == theme))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var frame = registry.Frame(entry.frameId);
                    var rect = GUILayoutUtility.GetRect(100, 60, GUILayout.Width(100));
                    if (frame.Sprite != null)
                    {
                        var sr = frame.Sprite.rect; var texture = frame.Sprite.texture;
                        var uv = new Rect(sr.x / texture.width, sr.y / texture.height, sr.width / texture.width, sr.height / texture.height);
                        var scale = Mathf.Min(rect.width / sr.width, rect.height / sr.height);
                        var target = new Rect(rect.x, rect.y, sr.width * scale, sr.height * scale);
                        GUI.DrawTextureWithTexCoords(target, texture, uv);
                    }
                    var solid = entry.solidRows.Sum(row => row.Count(c => c == '#'));
                    var support = entry.supportRows.Sum(row => row.Count(c => c == '#'));
                    EditorGUILayout.LabelField(entry.id + "\n" + entry.width + "×" + entry.height +
                        (entry.kind == "Structure" ? $" · 고체 {solid} · 빈 셀 {entry.width * entry.height - solid}" : $" · 추가 충돌 0 · 받침 {support}"), GUILayout.Height(50));
                    using (new EditorGUI.DisabledScope(theme != selectedMap.ThemeId))
                        if (GUILayout.Button("배치/편집", GUILayout.Width(80))) AnimolTerrainMapEditorWindow.OpenForCatalogEntry(entry.id);
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.HelpBox("부품 창에서 좌표 배치, 셀 마스크 ghost, instance 전체 이동/삭제를 사용합니다. 저장 대상은 현재 StageMapDefinition입니다.", MessageType.None);
        }
    }
}
