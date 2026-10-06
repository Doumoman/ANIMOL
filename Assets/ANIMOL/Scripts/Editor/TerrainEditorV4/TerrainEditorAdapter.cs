using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Development;
using ANIMOL.Gameplay;
using Animol.TerrainStructure;
using Animol.TerrainStructure.Editor;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Editor
{
    public sealed partial class TerrainEditorAdapter : ITerrainEditorAdapter
    {
        private readonly string guid;
        private StageMapDefinition cachedMap;
        public StageMapDefinition Map
        {
            get
            {
                var map = cachedMap != null ? cachedMap : (cachedMap = AssetDatabase.LoadAssetAtPath<StageMapDefinition>(AssetDatabase.GUIDToAssetPath(guid)));
                if (map == null || !EditorUtility.IsPersistent(map)) throw new InvalidOperationException("정본 맵 에셋 연결이 끊겼습니다.");
                return map;
            }
        }
        public StageTerrainStructureRegistry Terrain => StageTerrainStructureRegistry.Load();
        private StageMapObjectTypeRegistry objects;
        public StageMapObjectTypeRegistry Objects => objects != null ? objects :
            (objects = AssetDatabase.LoadAssetAtPath<StageMapObjectTypeRegistry>("Assets/ANIMOL/Data/Development/M9Objects/MapObjectTypeRegistry.asset"));
        public Font Font { get; }
        public Sprite ButtonSprite { get; }
        public event Action Changed;
        public IReadOnlyList<TerrainEditorPart> Markers { get; }

        public TerrainEditorAdapter(string assetGuid)
        {
            guid = assetGuid;
            Markers=StageMapScenePalette.MarkerKinds.Select(kind=>new TerrainEditorPart{id="marker:"+StageMapScenePalette.MarkerLabel(kind),name=StageMapScenePalette.MarkerLabel(kind),marker=kind,theme="COMMON",style="",size=Vector2.one,category=kind is StageMapObjectKind.Hole or StageMapObjectKind.Spike?"Hazard":"Object"}).ToArray();
            Font = AssetDatabase.LoadAssetAtPath<Font>("Assets/ANIMOL/Fonts/pixelroborobo.otf");
            // The Scene editor uses flat controls; no project-wide sprite search at startup.
            Read(); Bind();
        }
        public AnimolTerrainSavedMap Read() => Map.ReadTerrain(Terrain);
        public void Validate(AnimolTerrainSavedMap candidate) => StageTerrainStructureIntegration.Validate(Map,candidate);
        private void Bind()
        {
            AnimolTerrainMapEditorBridge.Bind(Map,Read,c=>StageTerrainStructureIntegration.Write(Map,c),
                Notify,Map.StageId,Validate);
        }
        private void Notify() { StageTerrainStructureIntegration.Refresh(Map); Changed?.Invoke(); }
        public void Commit(AnimolTerrainSavedMap candidate,string label)
        { RequireEdit(); Bind(); AnimolTerrainMapEditorBridge.Commit(candidate,label); }
        private void RequireEdit()
        { if(TerrainEditorScreen.Instance?.IsTesting==true)throw new InvalidOperationException("시험 플레이 중에는 맵 변경을 할 수 없습니다."); }
        public void Save()
        { Read(); AssetDatabase.SaveAssetIfDirty(Map); }
        // Keyboard and buttons share native Undo; there is no runtime Input polling handler.
        public void Undo() { RequireEdit(); Bind(); UnityEditor.Undo.PerformUndo(); }
        public void Redo() { RequireEdit(); Bind(); UnityEditor.Undo.PerformRedo(); }
        public void Exit() => TerrainEditorSession.Close();
        public void Dispose() { var map=AssetDatabase.LoadAssetAtPath<StageMapDefinition>(AssetDatabase.GUIDToAssetPath(guid));if(map!=null)AnimolTerrainMapEditorBridge.Unbind(map); DisposeThumbnails(); Changed=null; }
        public void EditMapConfiguration(string operation)
        {
            RequireEdit();var map=Map;if(operation=="Units" && Mathf.Approximately(map.WorldUnitsPerCell,1))return;var before=EditorJsonUtility.ToJson(map);
            UnityEditor.Undo.IncrementCurrentGroup();int group=UnityEditor.Undo.GetCurrentGroup();
            CampaignMapBackupStore.CreateBackup(map,"autosave-v4-bounds-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
            UnityEditor.Undo.RecordObject(map,"Configure ANIMOL map");
            try
            {
                if(operation=="Units")
                {
                    if(Mathf.Approximately(map.WorldUnitsPerCell,1))return;
                    var serialized=new SerializedObject(map);serialized.FindProperty("worldUnitsPerCell").floatValue=1;serialized.ApplyModifiedPropertiesWithoutUndo();
                    map.EditorNotifyAuthoredPropertiesChanged();
                }
                else if(!StageMapAuthoringOperations.AddChunk(map,(StageMapChunkEdge)Enum.Parse(typeof(StageMapChunkEdge),operation)))throw new InvalidOperationException("청크 확장 실패");
                map.EditorInvalidateTerrainDerivedCache();EditorUtility.SetDirty(map);AssetDatabase.SaveAssetIfDirty(map);
                UnityEditor.Undo.FlushUndoRecordObjects();Notify();UnityEditor.Undo.CollapseUndoOperations(group);
            }
            catch
            {UnityEditor.Undo.FlushUndoRecordObjects();AnimolTerrainMapEditorBridge.RevertUndoGroup(group);EditorJsonUtility.FromJsonOverwrite(before,map);EditorUtility.SetDirty(map);AssetDatabase.SaveAssetIfDirty(map);try{Notify();}catch{}throw;}
        }
    }
}
