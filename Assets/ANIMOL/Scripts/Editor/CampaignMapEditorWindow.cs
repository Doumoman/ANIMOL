using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Core;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Editor
{
    public enum StageMapTool { Select, Brush, Eraser, Line, Rectangle, Fill, Stamp, Object, CampaignMarker, EditPath }

    public sealed class CampaignMapEditorWindow : EditorWindow
    {
        private StageMapDefinition selectedMap;
        private CampaignCatalog catalog;
        private StageMapTool tool;
        private StageMapLayer layer = StageMapLayer.Terrain;
        private StageMapObjectKind objectKind;
        private StageMapObjectTypeRegistry objectRegistry;
        private int objectTypeIndex;
        private string objectTypeSearch = string.Empty;
        private string objectThemeFilter = "COMMON";
        private HalfBlockPlacement halfPlacement;
        private string objectStableId = string.Empty;
        private string selectedObjectStableId = string.Empty;
        private Vector2Int objectPaletteCell;
        private string objectPaletteMessage = string.Empty;
        private string tileId = "DEV_TERRAIN_PLACEHOLDER";
        private string variantId = string.Empty;
        private string search = string.Empty;
        private ContentAvailability? availabilityFilter;
        private Vector2 scroll;
        private Vector2 authoringScroll;
        private Vector2Int dragStart;
        private bool dragging;
        private readonly bool[] layerVisibility = { true, true, true, true };
        private List<StageMapPatternCell> clipboard = new List<StageMapPatternCell>();
        private StageMapPatternAsset selectedPattern;
        private Vector2Int inspectedChunk;
        private string manualSessionBackupPath = string.Empty;
        private string selectedMarkerStableId = string.Empty;
        private string manualMessage = string.Empty;
        private StageMapPaletteCategory paletteCategory = StageMapPaletteCategory.Select;
        private TerrainTileTopology terrainTopology = TerrainTileTopology.Center;
        private string terrainDesignId = "BASE";
        private bool scenePaletteVisible = true;
        private bool hasSelectedCell;
        private Vector2Int selectedCell;
        private bool pathEditing;
        private int selectedPathNode = -1;
        private Vector2 sceneLogicScroll;

        [MenuItem("ANIMOL/M7/Campaign Map Editor")]
        public static void Open() => GetWindow<CampaignMapEditorWindow>("ANIMOL Map Editor");

        [MenuItem("ANIMOL/M8/Variable Chunk Map Editor")]
        public static void OpenM8() => Open();

        [MenuItem("ANIMOL/Map Editor/Scene Palette %#m")]
        public static void OpenScenePalette()
        {
            var window = GetWindow<CampaignMapEditorWindow>("ANIMOL Map Editor");
            if (Selection.activeObject is StageMapDefinition map) window.SelectMap(map, false);
            window.scenePaletteVisible = true;
            window.FocusSceneAuthoring();
        }

        public static CampaignMapEditorWindow OpenForManualSession(StageMapDefinition map, string backupPath)
        {
            var window = GetWindow<CampaignMapEditorWindow>("ANIMOL Map Editor");
            window.minSize = new Vector2(980f, 720f);
            window.selectedMap = map;
            window.manualSessionBackupPath = backupPath ?? string.Empty;
            window.search = "T01-S01";
            window.objectThemeFilter = "T01";
            window.tileId = "M9_MOON_SOLID_16PX_PLACEHOLDER";
            window.paletteCategory = StageMapPaletteCategory.Select;
            window.tool = StageMapTool.Select;
            window.Focus();
            if (map != null) Selection.activeObject = map;
            window.RepaintAll();
            if (map != null) EditorApplication.delayCall += () => StageMapAuthoringWorkspace.Open(map, true);
            return window;
        }

        public static CampaignMapEditorWindow OpenForWorkspace(StageMapDefinition map)
        {
            var window = GetWindow<CampaignMapEditorWindow>("ANIMOL Map Editor");
            window.SelectMap(map, false);
            window.scenePaletteVisible = true;
            window.RepaintAll();
            return window;
        }

        public static void SelectWorkspaceProxy(StageMapAuthoringProxy proxy)
        {
            if (proxy == null || proxy.Map == null) return;
            var window = OpenForWorkspace(proxy.Map);
            window.paletteCategory = StageMapPaletteCategory.Select;
            window.tool = StageMapTool.Select;
            window.pathEditing = false;
            if (proxy.Role == StageMapAuthoringProxyRole.Cell)
            {
                window.layer = proxy.Layer;
                window.selectedCell = proxy.AuthoredCell;
                window.hasSelectedCell = true;
                window.selectedObjectStableId = string.Empty;
                window.selectedMarkerStableId = string.Empty;
            }
            else
            {
                window.selectedObjectStableId = proxy.StableId;
                var placement = proxy.Map.Objects.FirstOrDefault(item => item.StableId == proxy.StableId);
                window.selectedMarkerStableId = placement != null && T01S01ManualMapWorkflow.IsCampaignMarker(placement.Kind)
                    ? proxy.StableId : string.Empty;
                window.selectedPathNode = proxy.PathNodeIndex;
                window.hasSelectedCell = false;
            }
            window.RepaintAll();
        }

        private void OnEnable()
        {
            catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignCatalogGenerator.CatalogPath);
            objectRegistry = AssetDatabase.LoadAssetAtPath<StageMapObjectTypeRegistry>("Assets/ANIMOL/Data/Development/M9Objects/MapObjectTypeRegistry.asset");
            SceneView.duringSceneGui += OnSceneGui;
            Undo.undoRedoPerformed += RepaintAll;
            EditorApplication.update += SyncSelectedMapAsset;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGui;
            Undo.undoRedoPerformed -= RepaintAll;
            EditorApplication.update -= SyncSelectedMapAsset;
        }

        private void RepaintAll()
        {
            Repaint();
            StageMapAuthoringWorkspace.ScheduleRefresh(selectedMap);
            SceneView.RepaintAll();
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is StageMapDefinition map)
            {
                SelectMap(map, true);
            }
        }

        private void SyncSelectedMapAsset()
        {
            if (Selection.activeObject is StageMapDefinition map && map != selectedMap)
                SelectMap(map, true);
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawCatalogPanel();
                authoringScroll = EditorGUILayout.BeginScrollView(authoringScroll);
                DrawAuthoringPanel();
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawCatalogPanel()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(285f)))
            {
                EditorGUILayout.LabelField("5 themes × 20 stable stage IDs", EditorStyles.boldLabel);
                search = EditorGUILayout.TextField("Search", search);
                availabilityFilter = (ContentAvailability?)EditorGUILayout.EnumPopup("Ready filter", availabilityFilter ?? ContentAvailability.Unassigned);
                if (GUILayout.Button("Clear filter")) availabilityFilter = null;
                scroll = EditorGUILayout.BeginScrollView(scroll);
                if (catalog != null)
                {
                    foreach (var theme in catalog.Themes)
                    {
                        EditorGUILayout.LabelField(theme.ThemeId, EditorStyles.boldLabel);
                        foreach (var stage in theme.Stages)
                        {
                            if (!string.IsNullOrWhiteSpace(search) && stage.StageId.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                            var availability = ContentAvailabilityResolver.Resolve(stage);
                            if (availabilityFilter.HasValue && availability != availabilityFilter.Value) continue;
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField($"{stage.StageId} [{availability}]", GUILayout.Width(185f));
                                var mapPath = $"Assets/ANIMOL/Data/Campaign/Maps/{stage.StageId}.asset";
                                var hasMapAsset = AssetDatabase.GetMainAssetTypeAtPath(mapPath) == typeof(StageMapDefinition);
                                if (GUILayout.Button(hasMapAsset ? "Open" : "Create", GUILayout.Width(70f))) SelectOrCreate(stage, false);
                            }
                        }
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawAuthoringPanel()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUI.BeginChangeCheck();
                var requestedMap = (StageMapDefinition)EditorGUILayout.ObjectField("Map", selectedMap, typeof(StageMapDefinition), false);
                if (EditorGUI.EndChangeCheck()) SelectMap(requestedMap, true);
                if (selectedMap == null)
                {
                    EditorGUILayout.HelpBox("Select a stable campaign slot. Missing map assets are created blank; existing assets are never regenerated.", MessageType.Info);
                    return;
                }
                EditorGUILayout.LabelField($"{selectedMap.StageId} · source cell {StageMapDefinition.SourceCellPixels}×{StageMapDefinition.SourceCellPixels}px", EditorStyles.boldLabel);
                if (selectedMap.WorldUnitsPerCell <= 0f)
                    EditorGUILayout.HelpBox("운영 worldUnitsPerCell은 아직 미확정입니다. 편집기는 1 unit/cell 프리뷰를 사용하므로 별도 설정 없이 제작할 수 있습니다. 이 값은 운영 Ready 전에 확정하면 됩니다.", MessageType.Info);

                if (selectedMap.StageId == "T01-S01") DrawT01S01ManualSession();

                DrawChunkAuthoring();

                DrawWindowPalette();
                for (var i = 0; i < layerVisibility.Length; i++)
                    layerVisibility[i] = EditorGUILayout.ToggleLeft($"Show {(StageMapLayer)i}", layerVisibility[i]);

                DrawObjectPalette();
                DrawCampaignMarkers();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Region / pattern library", EditorStyles.boldLabel);
                if (GUILayout.Button("Copy authored bounds on current layer")) CopyAuthoredBounds();
                if (GUILayout.Button("Paste clipboard at (0,0)")) StageMapAuthoringOperations.Stamp(selectedMap, Vector2Int.zero, clipboard);
                selectedPattern = (StageMapPatternAsset)EditorGUILayout.ObjectField("Pattern", selectedPattern, typeof(StageMapPatternAsset), false);
                if (GUILayout.Button("Save clipboard as pattern asset")) SaveClipboardPattern();

                EditorGUILayout.Space();
                if (GUILayout.Button("Create versioned map backup")) StageMapBackupService.CreateBackup(selectedMap);
                if (GUILayout.Button("Restore latest backup"))
                {
                    var latest = StageMapBackupService.FindLatest(selectedMap);
                    if (!string.IsNullOrWhiteSpace(latest)) StageMapBackupService.Restore(selectedMap, latest);
                }
                if (GUILayout.Button("Batch static validation")) UiBuildPipeline.ValidateM7C();

                EditorGUILayout.Space();
                var serialized = new SerializedObject(selectedMap);
                serialized.Update();
                EditorGUI.BeginChangeCheck();
                foreach (var propertyName in new[] { "mapVersion", "worldUnitsPerCell", "fixedAnimalIds", "initialAnimalId", "mainTimeLimitSeconds", "escapeTimeLimitSeconds", "fastClearThresholdSeconds", "optionalObjectiveIds", "exitRevealCutsceneSeconds", "timersAdvanceDuringExitReveal" })
                    EditorGUILayout.PropertyField(serialized.FindProperty(propertyName), true);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(selectedMap, "Edit ANIMOL Map Properties");
                    serialized.ApplyModifiedProperties();
                    selectedMap.EditorNotifyAuthoredPropertiesChanged();
                    selectedMap.EditorMarkCollisionDataSynchronized();
                    EditorUtility.SetDirty(selectedMap);
                    AssetDatabase.SaveAssetIfDirty(selectedMap);
                }
                else serialized.ApplyModifiedProperties();

                var report = StageMapValidator.ValidateForOperation(selectedMap);
                EditorGUILayout.HelpBox(report.IsValid ? "Operational static gate passed." :
                    "운영 Ready 검증(편집 차단 아님):\n" + string.Join("\n", report.Errors),
                    report.IsValid ? MessageType.Info : MessageType.Warning);
            }
        }

        private void DrawWindowPalette()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene View direct authoring", EditorStyles.boldLabel);
            var next = (StageMapPaletteCategory)GUILayout.Toolbar((int)paletteCategory,
                new[] { "선택/편집", "일반 블록", "특수 블록", "로직 장치" });
            if (next != paletteCategory) ActivateCategory(next);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(StageMapAuthoringWorkspace.IsOpenFor(selectedMap) ? "맵 전체 다시 포커스" : "전용 편집 씬 열기")) FocusSceneAuthoring();
                if (GUILayout.Button("프리뷰 새로고침")) StageMapAuthoringWorkspace.RebuildPreview(selectedMap);
                scenePaletteVisible = GUILayout.Toggle(scenePaletteVisible, "Scene 팔레트 표시", "Button");
            }
            EditorGUILayout.HelpBox(StageMapAuthoringWorkspace.IsOpenFor(selectedMap)
                ? $"FOCUSED WORKSPACE · {selectedMap.StageId} · Hierarchy 오브젝트를 직접 선택할 수 있습니다."
                : "전용 편집 씬을 열면 다른 게임 씬과 분리된 Hierarchy 프리뷰에서 이 맵만 볼 수 있습니다.",
                StageMapAuthoringWorkspace.IsOpenFor(selectedMap) ? MessageType.Info : MessageType.None);

            switch (paletteCategory)
            {
                case StageMapPaletteCategory.Terrain:
                    layer = (StageMapLayer)EditorGUILayout.EnumPopup("Layer", layer);
                    tileId = EditorGUILayout.TextField("Stable block family ID", tileId);
                    terrainDesignId = EditorGUILayout.TextField("Design variant", terrainDesignId);
                    DrawTerrainFamilyButtons();
                    DrawNineSliceGrid();
                    tool = GUILayout.Toolbar(TerrainToolIndex(tool),
                        new[] { "Paint", "Erase", "Line", "Rect", "Fill" }) switch
                    {
                        0 => StageMapTool.Brush, 1 => StageMapTool.Eraser, 2 => StageMapTool.Line,
                        3 => StageMapTool.Rectangle, _ => StageMapTool.Fill
                    };
                    variantId = StageMapScenePalette.ComposeVariantId(terrainDesignId, terrainTopology);
                    EditorGUILayout.LabelField("Saved variant ID", variantId);
                    break;
                case StageMapPaletteCategory.Markers:
                    DrawMarkerButtons();
                    EditorGUILayout.HelpBox("START/BUBBLE/CHECK/EXIT/HOLE/SPIKE를 고르고 Scene 셀을 클릭합니다.", MessageType.None);
                    break;
                case StageMapPaletteCategory.LogicObjects:
                    DrawCompactLogicButtons(140f);
                    break;
                default:
                    EditorGUILayout.HelpBox("Scene에서 오브젝트 또는 셀을 클릭해 선택합니다. 이동 핸들로 셀 스냅 이동, Delete로 삭제, F로 프레임합니다.", MessageType.None);
                    DrawSelectedSceneItemSummary();
                    break;
            }
        }

        private void DrawScenePaletteOverlay(Rect area)
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(area, GUI.skin.window);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(StageMapAuthoringWorkspace.IsOpenFor(selectedMap) ? "ANIMOL MAP · FOCUSED" : "ANIMOL MAP", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(scenePaletteVisible ? "−" : "+", GUILayout.Width(28f)))
                    scenePaletteVisible = !scenePaletteVisible;
            }
            if (scenePaletteVisible)
            {
                GUILayout.Label($"{selectedMap.StageId} · 16px cell / 16×16 chunk · {selectedMap.Cells.Count} cells / {selectedMap.Objects.Count} objects", EditorStyles.miniLabel);
                var next = (StageMapPaletteCategory)GUILayout.Toolbar((int)paletteCategory,
                    new[] { "선택", "블록", "특수", "장치" });
                if (next != paletteCategory) ActivateCategory(next);
                switch (paletteCategory)
                {
                    case StageMapPaletteCategory.Terrain:
                        GUILayout.Label($"{tileId} / {terrainDesignId}", EditorStyles.miniBoldLabel);
                        DrawTerrainFamilyButtons();
                        DrawNineSliceGrid();
                        var toolIndex = GUILayout.Toolbar(TerrainToolIndex(tool), new[] { "칠", "삭제", "선", "면", "채움" });
                        tool = toolIndex switch
                        {
                            0 => StageMapTool.Brush, 1 => StageMapTool.Eraser, 2 => StageMapTool.Line,
                            3 => StageMapTool.Rectangle, _ => StageMapTool.Fill
                        };
                        break;
                    case StageMapPaletteCategory.Markers:
                        DrawMarkerButtons();
                        break;
                    case StageMapPaletteCategory.LogicObjects:
                        DrawCompactLogicButtons(210f);
                        break;
                    default:
                        DrawSelectedSceneItemSummary();
                        break;
                }
                GUILayout.FlexibleSpace();
                GUILayout.Label("S 선택 · B 블록 · M 특수 · L 장치 · E 삭제도구", EditorStyles.miniLabel);
                GUILayout.Label("LMB 배치/선택 · Alt/RMB 화면이동 · Delete 삭제 · F 프레임", EditorStyles.miniLabel);
            }
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private void DrawNineSliceGrid()
        {
            GUILayout.Label("9방향 블록 모양", EditorStyles.miniBoldLabel);
            for (var row = 0; row < 3; row++)
            {
                using (new GUILayout.HorizontalScope())
                {
                    for (var column = 0; column < 3; column++)
                    {
                        var topology = StageMapScenePalette.NineSliceOrder[row * 3 + column];
                        var selected = terrainTopology == topology;
                        if (GUILayout.Toggle(selected, StageMapScenePalette.TopologyGlyph(topology), "Button", GUILayout.Height(24f)))
                            terrainTopology = topology;
                    }
                }
            }
            variantId = StageMapScenePalette.ComposeVariantId(terrainDesignId, terrainTopology);
        }

        private void DrawTerrainFamilyButtons()
        {
            var families = selectedMap.Cells.Where(item => item.Layer == layer && !string.IsNullOrWhiteSpace(item.TileId))
                .Select(item => item.TileId).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).Take(8).ToArray();
            if (families.Length > 0)
            {
                GUILayout.Label("현재 맵 블록 패밀리", EditorStyles.miniBoldLabel);
                for (var start = 0; start < families.Length; start += 2)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        for (var index = start; index < Mathf.Min(start + 2, families.Length); index++)
                        {
                            var family = families[index];
                            var label = family.Length > 21 ? family.Substring(0, 19) + "…" : family;
                            if (GUILayout.Toggle(string.Equals(tileId, family, StringComparison.Ordinal), label, "Button", GUILayout.Height(24f)))
                                tileId = family;
                        }
                    }
                }
            }

            var styles = selectedMap.Cells.Where(item => item.Layer == layer && item.TileId == tileId)
                .Select(item => ParseDesignId(item.VariantId)).Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).Take(6).ToArray();
            if (styles.Length == 0) return;
            GUILayout.Label("디자인", EditorStyles.miniBoldLabel);
            using (new GUILayout.HorizontalScope())
            {
                foreach (var style in styles)
                    if (GUILayout.Toggle(string.Equals(terrainDesignId, style, StringComparison.Ordinal), style, "Button"))
                        terrainDesignId = style;
            }
        }

        private static string ParseDesignId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var separator = value.IndexOf(StageMapScenePalette.VariantSeparator, StringComparison.Ordinal);
            return separator <= 0 ? value : value.Substring(0, separator);
        }

        private void DrawMarkerButtons()
        {
            for (var row = 0; row < 2; row++)
            {
                using (new GUILayout.HorizontalScope())
                {
                    for (var column = 0; column < 3; column++)
                    {
                        var kind = StageMapScenePalette.MarkerKinds[row * 3 + column];
                        if (GUILayout.Toggle(tool == StageMapTool.CampaignMarker && objectKind == kind,
                                StageMapScenePalette.MarkerLabel(kind), "Button"))
                        {
                            objectKind = kind;
                            tool = StageMapTool.CampaignMarker;
                        }
                    }
                }
            }
        }

        private void DrawCompactLogicButtons(float height)
        {
            var types = FilteredObjectTypes();
            if (types.Length == 0) { GUILayout.Label("등록된 장치가 없습니다."); return; }
            objectTypeIndex = Mathf.Clamp(objectTypeIndex, 0, types.Length - 1);
            using (var scrollView = new GUILayout.ScrollViewScope(sceneLogicScroll, GUILayout.Height(height)))
            {
                sceneLogicScroll = scrollView.scrollPosition;
                foreach (var type in types)
                {
                    var typeIndex = Array.IndexOf(types, type);
                    var label = $"{(type.ImplementationLevel == MapObjectImplementationLevel.PlaceablePrototype ? "⚠ " : string.Empty)}{type.DisplayName}\n{type.StableTypeId}";
                    if (GUILayout.Toggle(tool == StageMapTool.Object && objectTypeIndex == typeIndex, label, "Button", GUILayout.Height(38f)))
                    {
                        objectTypeIndex = typeIndex;
                        tool = StageMapTool.Object;
                    }
                }
            }
            GUILayout.Label("경고 표시는 DEV 프로토타입이며 Ready를 차단합니다.", EditorStyles.miniLabel);
        }

        private void DrawSelectedSceneItemSummary()
        {
            var placement = SelectedPlacement();
            if (placement != null)
            {
                GUILayout.Label($"선택: {placement.Kind}", EditorStyles.boldLabel);
                GUILayout.Label(placement.StableId, EditorStyles.miniLabel);
                GUILayout.Label($"셀 ({placement.X}, {placement.Y}) · path {placement.Settings.PathCells.Count}", EditorStyles.miniLabel);
                using (new GUILayout.HorizontalScope())
                {
                    var wantsPath = GUILayout.Toggle(pathEditing, "경로 편집", "Button");
                    if (wantsPath != pathEditing)
                    {
                        pathEditing = wantsPath;
                        tool = pathEditing ? StageMapTool.EditPath : StageMapTool.Select;
                    }
                    if (GUILayout.Button("마지막 점 삭제") && placement.Settings.PathCells.Count > 0)
                    {
                        var path = placement.Settings.PathCells.Take(placement.Settings.PathCells.Count - 1).ToArray();
                        if (!StageMapObjectAuthoringOperations.SetPath(selectedMap, placement.StableId, path, out var validation))
                            objectPaletteMessage = string.Join(" | ", validation.Errors);
                    }
                    if (GUILayout.Button("삭제")) DeleteSelection();
                }
            }
            else if (hasSelectedCell)
            {
                var cell = selectedMap.FindCell(selectedCell.x, selectedCell.y, layer);
                GUILayout.Label(cell == null ? $"빈 셀 ({selectedCell.x}, {selectedCell.y})" :
                    $"셀 ({cell.X}, {cell.Y}) · {cell.TileId}\n{cell.VariantId}");
                if (cell != null && GUILayout.Button("선택 셀 삭제")) DeleteSelection();
            }
            else GUILayout.Label("Scene의 블록/오브젝트를 클릭하세요.", EditorStyles.wordWrappedMiniLabel);
            if (!string.IsNullOrWhiteSpace(objectPaletteMessage))
                GUILayout.Label(objectPaletteMessage, EditorStyles.wordWrappedMiniLabel);
        }

        private void ActivateCategory(StageMapPaletteCategory category)
        {
            paletteCategory = category;
            pathEditing = false;
            tool = category switch
            {
                StageMapPaletteCategory.Terrain => StageMapTool.Brush,
                StageMapPaletteCategory.Markers => StageMapTool.CampaignMarker,
                StageMapPaletteCategory.LogicObjects => StageMapTool.Object,
                _ => StageMapTool.Select
            };
            RepaintAll();
        }

        private static int TerrainToolIndex(StageMapTool value) => value switch
        {
            StageMapTool.Eraser => 1, StageMapTool.Line => 2, StageMapTool.Rectangle => 3,
            StageMapTool.Fill => 4, _ => 0
        };

        private void OnSceneGui(SceneView sceneView)
        {
            if (selectedMap == null) return;
            var focusedWorkspace = StageMapAuthoringWorkspace.IsOpenFor(selectedMap);
            var units = selectedMap.GetEditorPreviewUnitsPerCell();
            var bounds = selectedMap.EditorPreviewCellBounds;
            for (var x = bounds.xMin; x <= bounds.xMax; x++)
            {
                var alpha = x % StageMapDefinition.TilesPerChunk == 0 ? .8f : .15f;
                Handles.color = new Color(1f, 1f, 1f, alpha);
                Handles.DrawLine(new Vector3(x * units, bounds.yMin * units), new Vector3(x * units, bounds.yMax * units));
            }
            for (var y = bounds.yMin; y <= bounds.yMax; y++)
            {
                var alpha = y % StageMapDefinition.TilesPerChunk == 0 ? .8f : .15f;
                Handles.color = new Color(1f, 1f, 1f, alpha);
                Handles.DrawLine(new Vector3(bounds.xMin * units, y * units), new Vector3(bounds.xMax * units, y * units));
            }
            DrawAuthoredCells(units, focusedWorkspace);
            if (!focusedWorkspace) DrawSelectionAndPathHandles(units);

            var current = Event.current;
            var overlayRect = scenePaletteVisible ? new Rect(12f, 12f, 322f, Mathf.Min(590f, sceneView.position.height - 36f)) : new Rect(12f, 12f, 52f, 30f);
            DrawScenePaletteOverlay(overlayRect);
            if (current.alt || overlayRect.Contains(current.mousePosition)) return;

            if (current.type == EventType.Layout && !(focusedWorkspace && tool == StageMapTool.Select))
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if (HandleSceneKeyboard(current)) return;

            var ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
            if (Mathf.Abs(ray.direction.z) < .0001f) return;
            var world = ray.origin + ray.direction * (-ray.origin.z / ray.direction.z);
            var cell = new Vector2Int(Mathf.FloorToInt(world.x / units), Mathf.FloorToInt(world.y / units));
            DrawCellCursor(cell, units);
            if (tool == StageMapTool.Object) DrawObjectGhost(cell, units);
            if (tool == StageMapTool.CampaignMarker) DrawCampaignMarkerGhost(cell, units);
            if (current.button != 0) return;
            if (current.type != EventType.MouseDown && current.type != EventType.MouseDrag && current.type != EventType.MouseUp) return;
            if (!selectedMap.CanAuthorCell(cell.x, cell.y)) return;
            if (focusedWorkspace && tool == StageMapTool.Select) return;

            if (current.type == EventType.MouseDown)
            {
                if (tool == StageMapTool.Select) SelectAtCell(cell);
                else if (tool == StageMapTool.EditPath) AppendPathNode(cell);
                else { dragStart = cell; dragging = true; ApplyImmediate(cell); }
                current.Use();
            }
            else if (current.type == EventType.MouseDrag && dragging && (tool == StageMapTool.Brush || tool == StageMapTool.Eraser))
            {
                ApplyImmediate(cell);
                current.Use();
            }
            else if (current.type == EventType.MouseUp && dragging)
            {
                ApplyDrag(dragStart, cell);
                dragging = false;
                current.Use();
            }
        }

        private void ApplyImmediate(Vector2Int cell)
        {
            if (tool == StageMapTool.Brush)
            {
                variantId = StageMapScenePalette.ComposeVariantId(terrainDesignId, terrainTopology);
                StageMapAuthoringOperations.Paint(selectedMap, cell, layer, tileId, variantId);
            }
            else if (tool == StageMapTool.Eraser) StageMapAuthoringOperations.Erase(selectedMap, cell, layer);
            else if (tool == StageMapTool.Fill)
            {
                variantId = StageMapScenePalette.ComposeVariantId(terrainDesignId, terrainTopology);
                StageMapAuthoringOperations.Fill(selectedMap, cell, layer, tileId, variantId);
            }
            else if (tool == StageMapTool.Stamp) StageMapAuthoringOperations.Stamp(selectedMap, cell, selectedPattern != null ? selectedPattern.Cells : clipboard);
            else if (tool == StageMapTool.Object)
            {
                var type = SelectedObjectType();
                if (type == null) StageMapAuthoringOperations.PlaceObject(selectedMap, cell, objectKind, $"{objectKind}-{Guid.NewGuid():N}");
                else
                {
                    var stableId = string.IsNullOrWhiteSpace(objectStableId) ? StageMapObjectAuthoringOperations.GenerateStableId(type.Kind) : objectStableId.Trim();
                    var settings = CreatePlacementSettings(type, cell);
                    if (StageMapObjectAuthoringOperations.Place(selectedMap, type, cell, stableId, settings, out var validation))
                    {
                        selectedObjectStableId = stableId;
                        objectStableId = string.Empty;
                        objectPaletteMessage = string.Join(" | ", validation.Warnings);
                    }
                    else objectPaletteMessage = string.Join(" | ", validation.Errors);
                }
            }
            else if (tool == StageMapTool.CampaignMarker)
            {
                if (T01S01ManualMapWorkflow.TryPlaceCampaignMarker(selectedMap, objectKind, cell, out var stableId, out manualMessage))
                    selectedMarkerStableId = stableId;
            }
        }

        private void ApplyDrag(Vector2Int from, Vector2Int to)
        {
            variantId = StageMapScenePalette.ComposeVariantId(terrainDesignId, terrainTopology);
            if (tool == StageMapTool.Line) StageMapAuthoringOperations.Line(selectedMap, from, to, layer, tileId, variantId);
            if (tool == StageMapTool.Rectangle) StageMapAuthoringOperations.Rectangle(selectedMap, from, to, layer, tileId, variantId);
        }

        private void DrawAuthoredCells(float units, bool focusedWorkspace)
        {
            if (!focusedWorkspace)
            {
                foreach (var cell in selectedMap.Cells)
                {
                    if (!layerVisibility[(int)cell.Layer]) continue;
                    Handles.color = cell.Layer switch
                    {
                        StageMapLayer.Background => new Color(.25f, .45f, .8f, .25f),
                        StageMapLayer.Terrain => new Color(.35f, .8f, .4f, .5f),
                        StageMapLayer.Decoration => new Color(.9f, .7f, .2f, .4f),
                        _ => new Color(.8f, .3f, .8f, .5f)
                    };
                    Handles.DrawSolidRectangleWithOutline(new Rect(cell.X * units, cell.Y * units, units, units), Handles.color, Color.black);
                }
            }
            foreach (var item in selectedMap.Objects)
            {
                var color = StageMapScenePalette.ObjectColor(item.Kind);
                var footprint = item.Settings.FootprintCells;
                var yOffset = item.Kind == StageMapObjectKind.HalfBlock && item.Settings.HalfPlacement == HalfBlockPlacement.Upper ? .5f : 0f;
                var rect = new Rect(item.X * units, (item.Y + yOffset) * units,
                    Mathf.Max(.5f, footprint.x) * units, Mathf.Max(.5f, footprint.y) * units);
                if (!focusedWorkspace)
                    Handles.DrawSolidRectangleWithOutline(rect, new Color(color.r, color.g, color.b, .2f), color);
                Handles.Label(new Vector3(rect.center.x, rect.yMax),
                    $"{StageMapScenePalette.MarkerLabel(item.Kind)}\n{item.StableId}");
            }
        }

        private void DrawCellCursor(Vector2Int cell, float units)
        {
            if (!selectedMap.CanAuthorCell(cell.x, cell.y)) return;
            var rect = new Rect(cell.x * units, cell.y * units, units, units);
            Handles.DrawSolidRectangleWithOutline(rect, new Color(1f, 1f, 1f, .04f), new Color(1f, 1f, 1f, .8f));
            Handles.Label(new Vector3(rect.xMax, rect.yMax), $"({cell.x},{cell.y})");
        }

        private void DrawSelectionAndPathHandles(float units)
        {
            var placement = SelectedPlacement();
            if (placement == null)
            {
                if (hasSelectedCell)
                {
                    Handles.DrawSolidRectangleWithOutline(new Rect(selectedCell.x * units, selectedCell.y * units, units, units),
                        new Color(1f, 1f, 1f, .08f), Color.white);
                }
                return;
            }

            var center = new Vector3((placement.X + .5f) * units, (placement.Y + .5f) * units, 0f);
            Handles.color = Color.white;
            Handles.DrawWireDisc(center, Vector3.forward, units * .65f);
            if (!pathEditing && tool == StageMapTool.Select)
            {
                EditorGUI.BeginChangeCheck();
                var moved = Handles.PositionHandle(center, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    var destination = new Vector2Int(Mathf.FloorToInt(moved.x / units), Mathf.FloorToInt(moved.y / units));
                    if (!StageMapObjectAuthoringOperations.Move(selectedMap, placement.StableId, destination, out var validation))
                        objectPaletteMessage = string.Join(" | ", validation.Errors.Concat(validation.Warnings));
                    else objectPaletteMessage = string.Join(" | ", validation.Warnings);
                }
            }

            var nodes = placement.Settings.PathCells.ToArray();
            if (nodes.Length == 0) return;
            var points = nodes.Select(node => new Vector3((node.x + .5f) * units, (node.y + .5f) * units, 0f)).ToArray();
            Handles.color = new Color(1f, .25f, .85f, 1f);
            if (points.Length > 1) Handles.DrawAAPolyLine(4f, points);
            for (var index = 0; index < points.Length; index++)
            {
                Handles.Label(points[index] + Vector3.up * units * .25f, index.ToString());
                if (!pathEditing) continue;
                EditorGUI.BeginChangeCheck();
                var moved = Handles.PositionHandle(points[index], Quaternion.identity);
                if (!EditorGUI.EndChangeCheck()) continue;
                var cell = new Vector2Int(Mathf.FloorToInt(moved.x / units), Mathf.FloorToInt(moved.y / units));
                var edited = nodes.ToArray();
                edited[index] = cell;
                selectedPathNode = index;
                if (!StageMapObjectAuthoringOperations.SetPath(selectedMap, placement.StableId, edited, out var validation))
                    objectPaletteMessage = string.Join(" | ", validation.Errors.Concat(validation.Warnings));
                else objectPaletteMessage = string.Join(" | ", validation.Warnings);
            }
        }

        private bool HandleSceneKeyboard(Event current)
        {
            if (current.type != EventType.KeyDown || EditorGUIUtility.editingTextField) return false;
            if (current.keyCode is KeyCode.Delete or KeyCode.Backspace)
            {
                DeleteSelection();
                current.Use();
                return true;
            }
            if (current.keyCode == KeyCode.Escape)
            {
                dragging = false;
                pathEditing = false;
                tool = StageMapTool.Select;
                paletteCategory = StageMapPaletteCategory.Select;
                current.Use();
                return true;
            }
            if (current.keyCode == KeyCode.F) { FrameSelection(); current.Use(); return true; }
            if (current.keyCode == KeyCode.S) ActivateCategory(StageMapPaletteCategory.Select);
            else if (current.keyCode == KeyCode.B) ActivateCategory(StageMapPaletteCategory.Terrain);
            else if (current.keyCode == KeyCode.M) ActivateCategory(StageMapPaletteCategory.Markers);
            else if (current.keyCode == KeyCode.L) ActivateCategory(StageMapPaletteCategory.LogicObjects);
            else if (current.keyCode == KeyCode.E)
            {
                paletteCategory = StageMapPaletteCategory.Terrain;
                tool = StageMapTool.Eraser;
            }
            else return false;
            current.Use();
            return true;
        }

        private void SelectAtCell(Vector2Int cell)
        {
            var placement = StageMapScenePalette.FindObjectAtCell(selectedMap, cell);
            if (placement != null)
            {
                selectedObjectStableId = placement.StableId;
                selectedMarkerStableId = T01S01ManualMapWorkflow.IsCampaignMarker(placement.Kind) ? placement.StableId : string.Empty;
                hasSelectedCell = false;
                selectedPathNode = -1;
            }
            else
            {
                selectedObjectStableId = string.Empty;
                selectedMarkerStableId = string.Empty;
                selectedCell = cell;
                hasSelectedCell = true;
            }
            pathEditing = false;
            RepaintAll();
        }

        private void AppendPathNode(Vector2Int cell)
        {
            var placement = SelectedPlacement();
            if (placement == null) return;
            var nodes = placement.Settings.PathCells.ToList();
            if (nodes.Count == 0) nodes.Add(new Vector2Int(placement.X, placement.Y));
            if (nodes.Contains(cell)) { objectPaletteMessage = "경로에는 같은 셀을 두 번 넣을 수 없습니다."; return; }
            nodes.Add(cell);
            if (!StageMapObjectAuthoringOperations.SetPath(selectedMap, placement.StableId, nodes, out var validation))
                objectPaletteMessage = string.Join(" | ", validation.Errors.Concat(validation.Warnings));
            else objectPaletteMessage = string.Join(" | ", validation.Warnings);
        }

        private void DeleteSelection()
        {
            var placement = SelectedPlacement();
            if (placement != null)
            {
                StageMapObjectAuthoringOperations.Remove(selectedMap, placement.StableId);
                selectedObjectStableId = string.Empty;
                selectedMarkerStableId = string.Empty;
                pathEditing = false;
            }
            else if (hasSelectedCell)
            {
                StageMapAuthoringOperations.Erase(selectedMap, selectedCell, layer);
                hasSelectedCell = false;
            }
            RepaintAll();
        }

        private void FrameSelection()
        {
            if (SceneView.lastActiveSceneView == null) return;
            var units = selectedMap.GetEditorPreviewUnitsPerCell();
            var placement = SelectedPlacement();
            var cell = placement == null ? selectedCell : new Vector2Int(placement.X, placement.Y);
            SceneView.lastActiveSceneView.LookAt(new Vector3((cell.x + .5f) * units, (cell.y + .5f) * units, 0f),
                Quaternion.identity, units * 6f);
        }

        private StageMapObjectPlacement SelectedPlacement()
        {
            var id = !string.IsNullOrWhiteSpace(selectedObjectStableId) ? selectedObjectStableId : selectedMarkerStableId;
            return string.IsNullOrWhiteSpace(id) ? null : selectedMap?.Objects.FirstOrDefault(item => item.StableId == id);
        }

        private void DrawObjectPalette()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("M9 object palette · DEV tuning", EditorStyles.boldLabel);
            objectRegistry = (StageMapObjectTypeRegistry)EditorGUILayout.ObjectField("Type registry", objectRegistry, typeof(StageMapObjectTypeRegistry), false);
            objectTypeSearch = EditorGUILayout.TextField("Type search", objectTypeSearch);
            var themeOptions = new[] { "ALL", "COMMON", "T01", "T02", "T03", "T04", "T05" };
            var themeIndex = Mathf.Max(0, Array.IndexOf(themeOptions, objectThemeFilter));
            objectThemeFilter = themeOptions[EditorGUILayout.Popup("Theme filter", themeIndex, themeOptions)];
            var types = objectRegistry == null ? Array.Empty<StageMapObjectTypeDefinition>() : objectRegistry.Types
                .Where(item => item != null)
                .Where(item => objectThemeFilter == "ALL" || (objectThemeFilter == "COMMON" ? string.IsNullOrEmpty(item.ThemeId) : item.ThemeId == objectThemeFilter))
                .Where(item => string.IsNullOrWhiteSpace(objectTypeSearch) || item.StableTypeId.IndexOf(objectTypeSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                               item.DisplayName.IndexOf(objectTypeSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            if (types.Length == 0)
            {
                EditorGUILayout.HelpBox("No registered type matches this M9B theme/search filter.", MessageType.Info);
                return;
            }
            objectTypeIndex = Mathf.Clamp(objectTypeIndex, 0, types.Length - 1);
            objectTypeIndex = EditorGUILayout.Popup("Registered type", objectTypeIndex,
                types.Select(item => $"{item.StableTypeId} · {item.DisplayName}").ToArray());
            var selectedType = types[objectTypeIndex];
            var previewSprite = StageMapScenePalette.ResolvePreviewSprite(selectedType);
            if (previewSprite != null)
            {
                var previewTexture = AssetPreview.GetAssetPreview(previewSprite) ?? AssetPreview.GetMiniThumbnail(previewSprite);
                if (previewTexture != null) GUILayout.Label(previewTexture, GUILayout.Width(96f), GUILayout.Height(96f));
            }
            EditorGUILayout.HelpBox($"{selectedType.StableTypeId} · {selectedType.ImplementationLevel}\n{selectedType.Description}",
                selectedType.ImplementationLevel == MapObjectImplementationLevel.PlaceablePrototype ? MessageType.Warning : MessageType.Info);
            tool = GUILayout.Toggle(tool == StageMapTool.Object, "Scene click placement + ghost", "Button") ? StageMapTool.Object : tool;
            objectStableId = EditorGUILayout.TextField("Stable ID (blank=generate)", objectStableId);
            objectPaletteCell = EditorGUILayout.Vector2IntField("Helper cell", objectPaletteCell);
            if (selectedType.Kind == StageMapObjectKind.HalfBlock)
                halfPlacement = (HalfBlockPlacement)EditorGUILayout.EnumPopup("Half position", halfPlacement);
            EditorGUILayout.LabelField("Footprint / snap", $"{selectedType.FootprintCells.x:0.#}×{selectedType.FootprintCells.y:0.#} / y {selectedType.VerticalSnapCells:0.#}");
            if (GUILayout.Button("Place registered object at helper cell"))
            {
                var stableId = string.IsNullOrWhiteSpace(objectStableId) ? StageMapObjectAuthoringOperations.GenerateStableId(selectedType.StableTypeId) : objectStableId.Trim();
                if (StageMapObjectAuthoringOperations.Place(selectedMap, selectedType, objectPaletteCell, stableId,
                    CreatePlacementSettings(selectedType, objectPaletteCell), out var validation))
                {
                    selectedObjectStableId = stableId;
                    objectStableId = string.Empty;
                    objectPaletteMessage = string.Join(" | ", validation.Warnings);
                }
                else objectPaletteMessage = string.Join(" | ", validation.Errors);
            }
            if (GUILayout.Button("Place pounder + 2-tile rice helper set"))
                StageMapObjectAuthoringOperations.PlacePounderRiceSet(selectedMap, objectRegistry, objectPaletteCell,
                    string.IsNullOrWhiteSpace(objectStableId) ? null : objectStableId, out objectPaletteMessage);
            if (selectedType.RequiresLinkedPair && GUILayout.Button("Place linked pair (A/B)"))
                StageMapObjectAuthoringOperations.PlaceLinkedPair(selectedMap, selectedType, objectPaletteCell,
                    objectPaletteCell + new Vector2Int(3, 0), out objectPaletteMessage);
            if (!string.IsNullOrWhiteSpace(objectPaletteMessage)) EditorGUILayout.HelpBox(objectPaletteMessage, MessageType.Info);

            var registeredIds = new HashSet<string>(objectRegistry.Types.Where(item => item != null).Select(item => item.StableTypeId), StringComparer.Ordinal);
            var placements = selectedMap.Objects.Where(item => registeredIds.Contains(item.DataKey)).ToArray();
            if (placements.Length == 0) return;
            var selectedIndex = Mathf.Max(0, Array.FindIndex(placements, item => item.StableId == selectedObjectStableId));
            selectedIndex = EditorGUILayout.Popup("Edit placed object", selectedIndex, placements.Select(item => $"{item.StableId} · {item.Kind}").ToArray());
            selectedObjectStableId = placements[selectedIndex].StableId;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Flip Left/Right")) StageMapObjectAuthoringOperations.Flip(selectedMap, selectedObjectStableId);
                if (GUILayout.Button("Duplicate +2 X"))
                {
                    if (StageMapObjectAuthoringOperations.Duplicate(selectedMap, selectedObjectStableId, new Vector2Int(2, 0), out var duplicateId, out var duplicateValidation))
                    { selectedObjectStableId = duplicateId; objectPaletteMessage = string.Join(" | ", duplicateValidation.Warnings); }
                    else objectPaletteMessage = string.Join(" | ", duplicateValidation.Errors);
                }
                if (GUILayout.Button("Remove")) StageMapObjectAuthoringOperations.Remove(selectedMap, selectedObjectStableId);
            }
            DrawSelectedObjectProperties(selectedObjectStableId);
        }

        private void DrawT01S01ManualSession()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("T01-S01 manual authoring session", EditorStyles.boldLabel);
            if (!string.IsNullOrWhiteSpace(manualSessionBackupPath))
                EditorGUILayout.HelpBox($"Opening backup created: {manualSessionBackupPath}", MessageType.Info);
            EditorGUILayout.HelpBox("No layout or object is generated here. Author terrain, markers and Moon devices yourself; every edit invalidates the human completion review.", MessageType.None);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save map"))
                {
                    AssetDatabase.SaveAssetIfDirty(selectedMap);
                    manualMessage = "Saved the current T01-S01 map asset.";
                }
                if (GUILayout.Button("Save + Local Play")) T01S01ManualMapWorkflow.SaveAndPlay(selectedMap);
                if (GUILayout.Button("Play MoonObjectLab")) T01S01ManualMapWorkflow.OpenMoonObjectLabAndPlay();
            }

            var report = T01S01ManualMapWorkflow.Validate(selectedMap, objectRegistry);
            var readiness = report.ReadyBlocked ? "READY BLOCKED" : "Ready gate eligible (human approval still required)";
            EditorGUILayout.HelpBox($"{readiness} · Rabbit · 60s collect -> 30s escape\n" +
                                    string.Join("\n", report.Information), report.ReadyBlocked ? MessageType.Warning : MessageType.Info);
            if (report.Errors.Count > 0)
                EditorGUILayout.HelpBox("Local-play blockers:\n" + string.Join("\n", report.Errors), MessageType.Error);
            if (report.Warnings.Count > 0)
                EditorGUILayout.HelpBox("Collision / path / play-check warnings:\n" + string.Join("\n", report.Warnings), MessageType.Warning);
            if (!string.IsNullOrWhiteSpace(manualMessage)) EditorGUILayout.HelpBox(manualMessage, MessageType.Info);
        }

        private void DrawCampaignMarkers()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Campaign markers · manual placement", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Use Scene click or the helper cell for start/checkpoint/bubble/exit/hazard markers. Existing markers are never moved automatically.", MessageType.None);
            var markerKinds = new[]
            {
                StageMapObjectKind.PlayerStart, StageMapObjectKind.Checkpoint, StageMapObjectKind.BubbleCandidate,
                StageMapObjectKind.Exit, StageMapObjectKind.Hole, StageMapObjectKind.Spike
            };
            var current = Mathf.Max(0, Array.IndexOf(markerKinds, objectKind));
            objectKind = markerKinds[EditorGUILayout.Popup("Marker kind", current, markerKinds.Select(value => value.ToString()).ToArray())];
            tool = GUILayout.Toggle(tool == StageMapTool.CampaignMarker, "Scene click marker placement", "Button")
                ? StageMapTool.CampaignMarker : tool;
            objectPaletteCell = EditorGUILayout.Vector2IntField("Marker helper cell", objectPaletteCell);
            if (GUILayout.Button("Place marker at helper cell"))
            {
                if (T01S01ManualMapWorkflow.TryPlaceCampaignMarker(selectedMap, objectKind, objectPaletteCell,
                        out var stableId, out manualMessage))
                    selectedMarkerStableId = stableId;
            }

            var markers = selectedMap.Objects.Where(item => T01S01ManualMapWorkflow.IsCampaignMarker(item.Kind)).ToArray();
            if (markers.Length == 0) return;
            var markerIndex = Mathf.Max(0, Array.FindIndex(markers, item => item.StableId == selectedMarkerStableId));
            markerIndex = EditorGUILayout.Popup("Selected marker", markerIndex,
                markers.Select(item => $"{item.StableId} · {item.Kind} ({item.X},{item.Y})").ToArray());
            selectedMarkerStableId = markers[markerIndex].StableId;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Frame marker"))
                {
                    var marker = markers[markerIndex];
                    var units = selectedMap.GetEditorPreviewUnitsPerCell();
                    SceneView.lastActiveSceneView?.LookAt(new Vector3(marker.X * units, marker.Y * units, 0f), Quaternion.identity, units * 7f);
                }
                if (GUILayout.Button("Remove marker"))
                {
                    StageMapObjectAuthoringOperations.Remove(selectedMap, selectedMarkerStableId);
                    selectedMarkerStableId = string.Empty;
                }
            }
        }

        private StageMapObjectTypeDefinition SelectedObjectType()
        {
            var types = FilteredObjectTypes();
            return types.Length == 0 ? null : types[Mathf.Clamp(objectTypeIndex, 0, types.Length - 1)];
        }

        private StageMapObjectTypeDefinition[] FilteredObjectTypes()
        {
            return objectRegistry == null ? Array.Empty<StageMapObjectTypeDefinition>() : objectRegistry.Types
                .Where(item => item != null)
                .Where(item => objectThemeFilter == "ALL" || (objectThemeFilter == "COMMON" ? string.IsNullOrEmpty(item.ThemeId) : item.ThemeId == objectThemeFilter))
                .Where(item => string.IsNullOrWhiteSpace(objectTypeSearch) || item.StableTypeId.IndexOf(objectTypeSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                               item.DisplayName.IndexOf(objectTypeSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
        }

        private StageMapObjectSettings CreatePlacementSettings(StageMapObjectTypeDefinition type, Vector2Int cell)
        {
            var settings = type.DefaultSettings.Clone();
            var path = settings.PathCells.ToArray();
            if (type.Kind == StageMapObjectKind.Pounder) path = new[] { cell, cell + new Vector2Int(0, -3) };
            if (type.Kind == StageMapObjectKind.RailPlatform) path = new[] { cell, cell + new Vector2Int(4, 0) };
            if (type.Kind == StageMapObjectKind.MoonJadeBalance) path = new[] { cell, cell + Vector2Int.down };
            if (type.Kind == StageMapObjectKind.CloudBalloonTether) path = new[] { cell, cell + new Vector2Int(0, -3) };
            if (type.Kind == StageMapObjectKind.LibIndexDrawer) path = new[] { cell, cell + new Vector2Int((int)settings.Direction * 2, 0) };
            if (type.Kind == StageMapObjectKind.GreenSandRetrace) path = new[] { cell, cell + new Vector2Int(3, 0) };
            if (type.Kind == StageMapObjectKind.MineMagnetPair) path = new[] { cell, cell + new Vector2Int((int)settings.Direction, 0) };
            if (type.Kind == StageMapObjectKind.MoonJadePendulum) path = new[] { cell + new Vector2Int(-2, 0), cell + new Vector2Int(2, 0) };
            if (type.Kind == StageMapObjectKind.MoonSlidingEave) path = new[] { cell, cell + new Vector2Int((int)settings.Direction, 0) };
            settings.EditorConfigure(settings.Version, settings.Direction, type.FootprintCells,
                type.Kind == StageMapObjectKind.HalfBlock ? halfPlacement : settings.HalfPlacement,
                settings.SpringContactPolicy, settings.HorizontalImpulse, settings.VerticalImpulse, settings.MovementSpeed,
                settings.UpperPauseSeconds, settings.LowerPauseSeconds, settings.ActivationRangeCells,
                settings.GroundSpeedMultiplier, settings.EndStopSeconds, settings.ReturnWhenEmpty, path);
            settings.EditorConfigureAuthoring(type.ImplementationLevel, settings.LinkedInstanceIds, settings.PhaseSeed,
                settings.ResetPolicy, settings.RouteRole, type.RequiresLinkedPair ? 1 : settings.MinimumLinkCount,
                settings.PrototypeNotice);
            return settings;
        }

        private void DrawSelectedObjectProperties(string stableId)
        {
            var serialized = new SerializedObject(selectedMap);
            serialized.Update();
            var objects = serialized.FindProperty("objects");
            SerializedProperty target = null;
            for (var i = 0; i < objects.arraySize; i++)
            {
                var element = objects.GetArrayElementAtIndex(i);
                if (element.FindPropertyRelative("stableId").stringValue == stableId) { target = element; break; }
            }
            if (target == null) return;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(target.FindPropertyRelative("dataKey"));
            EditorGUILayout.PropertyField(target.FindPropertyRelative("prefab"));
            EditorGUILayout.PropertyField(target.FindPropertyRelative("settings"), true);
            if (!EditorGUI.EndChangeCheck()) { serialized.ApplyModifiedProperties(); return; }
            StageMapBackupService.CreateBackup(selectedMap, $"autosave-before-properties-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}");
            Undo.RecordObject(selectedMap, "Edit ANIMOL Map Object Properties");
            serialized.ApplyModifiedProperties();
            selectedMap.EditorNotifyAuthoredPropertiesChanged();
            selectedMap.EditorMarkCollisionDataSynchronized();
            EditorUtility.SetDirty(selectedMap);
            AssetDatabase.SaveAssetIfDirty(selectedMap);
        }

        private void DrawObjectGhost(Vector2Int cell, float units)
        {
            var type = SelectedObjectType();
            if (type == null) return;
            var yOffset = type.Kind == StageMapObjectKind.HalfBlock && halfPlacement == HalfBlockPlacement.Upper ? .5f : 0f;
            var rect = new Rect(cell.x * units, (cell.y + yOffset) * units,
                type.FootprintCells.x * units, type.FootprintCells.y * units);
            var candidate = new StageMapObjectPlacement("GHOST", type.Kind, cell.x, cell.y, type.StableTypeId, type.Prefab, CreatePlacementSettings(type, cell));
            var validation = StageMapObjectAuthoringOperations.ValidatePlacement(selectedMap, candidate);
            var fill = validation.IsValid ? new Color(.2f, .9f, .75f, .25f) : new Color(1f, .2f, .2f, .3f);
            DrawSpriteInScene(StageMapScenePalette.ResolvePreviewSprite(type), rect, validation.IsValid ? .8f : .35f);
            Handles.DrawSolidRectangleWithOutline(rect, fill, validation.IsValid ? Color.cyan : Color.red);
            Handles.Label(new Vector3(rect.center.x, rect.yMax), $"{type.StableTypeId}\n{type.FootprintCells.x:0.#}×{type.FootprintCells.y:0.#}");
            SceneView.RepaintAll();
        }

        private static void DrawSpriteInScene(Sprite sprite, Rect worldRect, float alpha)
        {
            if (sprite == null || sprite.texture == null) return;
            var topLeft = HandleUtility.WorldToGUIPoint(new Vector3(worldRect.xMin, worldRect.yMax));
            var bottomRight = HandleUtility.WorldToGUIPoint(new Vector3(worldRect.xMax, worldRect.yMin));
            var screenRect = Rect.MinMaxRect(Mathf.Min(topLeft.x, bottomRight.x), Mathf.Min(topLeft.y, bottomRight.y),
                Mathf.Max(topLeft.x, bottomRight.x), Mathf.Max(topLeft.y, bottomRight.y));
            var textureRect = sprite.textureRect;
            var uv = new Rect(textureRect.x / sprite.texture.width, textureRect.y / sprite.texture.height,
                textureRect.width / sprite.texture.width, textureRect.height / sprite.texture.height);
            Handles.BeginGUI();
            var previous = GUI.color; GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTextureWithTexCoords(screenRect, sprite.texture, uv, true);
            GUI.color = previous; Handles.EndGUI();
        }

        private void DrawCampaignMarkerGhost(Vector2Int cell, float units)
        {
            var valid = selectedMap.CanAuthorCell(cell.x, cell.y) && T01S01ManualMapWorkflow.IsCampaignMarker(objectKind) &&
                        ((objectKind != StageMapObjectKind.PlayerStart && objectKind != StageMapObjectKind.Exit) ||
                         selectedMap.Objects.All(item => item.Kind != objectKind));
            var rect = new Rect(cell.x * units, cell.y * units, units, units);
            Handles.DrawSolidRectangleWithOutline(rect, valid ? new Color(.2f, .75f, 1f, .25f) : new Color(1f, .2f, .2f, .3f),
                valid ? Color.cyan : Color.red);
            Handles.Label(new Vector3(rect.center.x, rect.yMax), objectKind.ToString());
            SceneView.RepaintAll();
        }

        private void DrawChunkAuthoring()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Variable chunks · 16×16 tile cells each", EditorStyles.boldLabel);
            var bounds = selectedMap.EditorPreviewChunkBounds;
            EditorGUILayout.LabelField("Chunk bounds", $"({bounds.xMin},{bounds.yMin}) · {bounds.width}×{bounds.height}" +
                (selectedMap.HasValidChunkBounds ? string.Empty : " · virtual preview"));
            var cells = selectedMap.EditorPreviewCellBounds;
            EditorGUILayout.LabelField("Cell bounds", $"x {cells.xMin}..{cells.xMax - 1}, y {cells.yMin}..{cells.yMax - 1}");
            if (!selectedMap.HasValidChunkBounds)
                EditorGUILayout.HelpBox("빈 맵은 (0,0) 1×1 청크를 가상으로 표시합니다. 첫 블록/마커/장치를 배치하면 해당 청크가 실제 맵 데이터로 자동 생성됩니다.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ Left")) StageMapAuthoringOperations.AddChunk(selectedMap, StageMapChunkEdge.Left);
                if (GUILayout.Button("+ Right")) StageMapAuthoringOperations.AddChunk(selectedMap, StageMapChunkEdge.Right);
                if (GUILayout.Button("+ Bottom")) StageMapAuthoringOperations.AddChunk(selectedMap, StageMapChunkEdge.Bottom);
                if (GUILayout.Button("+ Top")) StageMapAuthoringOperations.AddChunk(selectedMap, StageMapChunkEdge.Top);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("− Left")) RequestRemoveChunk(StageMapChunkEdge.Left);
                if (GUILayout.Button("− Right")) RequestRemoveChunk(StageMapChunkEdge.Right);
                if (GUILayout.Button("− Bottom")) RequestRemoveChunk(StageMapChunkEdge.Bottom);
                if (GUILayout.Button("− Top")) RequestRemoveChunk(StageMapChunkEdge.Top);
            }
            inspectedChunk = EditorGUILayout.Vector2IntField("Inspect chunk", inspectedChunk);
            if (!selectedMap.HasValidChunkBounds || !selectedMap.ContainsChunk(inspectedChunk))
                EditorGUILayout.HelpBox("The inspected chunk is outside this map.", MessageType.Info);
            else
            {
                var occupancy = selectedMap.GetChunkOccupancy(inspectedChunk);
                EditorGUILayout.HelpBox($"Chunk {inspectedChunk}: {occupancy.CellCount} cells, {occupancy.ObjectCount} objects" +
                    (occupancy.ObjectCount == 0 ? string.Empty : $"\n{string.Join(", ", occupancy.ObjectIds)}"), MessageType.None);
                if (GUILayout.Button("Frame inspected chunk in Scene View"))
                {
                    var units = selectedMap.GetEditorPreviewUnitsPerCell();
                    var center = new Vector3((inspectedChunk.x + .5f) * StageMapDefinition.TilesPerChunk * units,
                        (inspectedChunk.y + .5f) * StageMapDefinition.TilesPerChunk * units, 0f);
                    SceneView.lastActiveSceneView?.LookAt(center, Quaternion.identity, StageMapDefinition.TilesPerChunk * units * .7f);
                }
            }
        }

        private void RequestRemoveChunk(StageMapChunkEdge edge)
        {
            var impact = StageMapAuthoringOperations.PreviewRemoveChunk(selectedMap, edge);
            if (impact == null)
            {
                EditorUtility.DisplayDialog("Cannot shrink map", "A map must retain at least one chunk on each axis.", "OK");
                return;
            }
            if (!impact.HasOccupiedData)
            {
                StageMapAuthoringOperations.RemoveChunk(selectedMap, edge, false, out _);
                return;
            }
            var preview = string.Join("\n", impact.RemovedObjects.Concat(impact.RemovedCells).Take(12));
            if (impact.RemovedObjects.Count + impact.RemovedCells.Count > 12) preview += "\n…";
            var confirmed = EditorUtility.DisplayDialog("Delete occupied chunk data?",
                $"Shrinking {edge} removes {impact.RemovedCells.Count} cells and {impact.RemovedObjects.Count} objects.\n\n{preview}\n\nThis requires explicit deletion and is Undoable.",
                "Delete data and shrink", "Cancel");
            if (confirmed) StageMapAuthoringOperations.RemoveChunk(selectedMap, edge, true, out _);
        }

        private void SelectOrCreate(CampaignStageDefinition stage, bool forceCreate)
        {
            var path = $"Assets/ANIMOL/Data/Campaign/Maps/{stage.StageId}.asset";
            var map = AssetDatabase.LoadAssetAtPath<StageMapDefinition>(path);
            if (map == null && (forceCreate || EditorUtility.DisplayDialog("Create blank map", $"Create a blank map asset for {stage.StageId}?", "Create", "Cancel")))
            {
                EnsureFolder("Assets/ANIMOL/Data/Campaign/Maps");
                map = CreateInstance<StageMapDefinition>();
                map.EditorInitializeIdentity(stage.StageId, stage.ThemeId);
                AssetDatabase.CreateAsset(map, path);
                AssetDatabase.SaveAssets();
            }
            if (map != null)
            {
                SelectMap(map, true);
                Selection.activeObject = map;
            }
        }

        private void CopyAuthoredBounds()
        {
            var cells = selectedMap.Cells.Where(c => c.Layer == layer).ToArray();
            if (cells.Length == 0) { clipboard.Clear(); return; }
            var minX = cells.Min(c => c.X); var minY = cells.Min(c => c.Y);
            var maxX = cells.Max(c => c.X); var maxY = cells.Max(c => c.Y);
            clipboard = StageMapAuthoringOperations.CopyRegion(selectedMap, new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1), layer);
        }

        private void SaveClipboardPattern()
        {
            if (clipboard.Count == 0) return;
            EnsureFolder("Assets/ANIMOL/Data/Campaign/MapPatterns");
            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/ANIMOL/Data/Campaign/MapPatterns/Pattern.asset");
            var pattern = CreateInstance<StageMapPatternAsset>();
            pattern.EditorSet(clipboard);
            AssetDatabase.CreateAsset(pattern, path);
            AssetDatabase.SaveAssets();
            selectedPattern = pattern;
        }

        private void FocusSceneAuthoring()
        {
            if (selectedMap == null) return;
            if (StageMapAuthoringWorkspace.IsOpenFor(selectedMap)) StageMapAuthoringWorkspace.FocusMap(selectedMap);
            else StageMapAuthoringWorkspace.Open(selectedMap, true);
        }

        private void SelectMap(StageMapDefinition map, bool openWorkspace)
        {
            if (selectedMap != map)
            {
                selectedMap = map;
                selectedObjectStableId = string.Empty;
                selectedMarkerStableId = string.Empty;
                selectedPathNode = -1;
                hasSelectedCell = false;
                pathEditing = false;
                dragging = false;
                objectPaletteMessage = string.Empty;
                if (map != null && new[] { "T01", "T02", "T03", "T04", "T05" }.Contains(map.ThemeId))
                    objectThemeFilter = map.ThemeId;
            }

            RepaintAll();
            if (!openWorkspace || map == null) return;
            EditorApplication.delayCall += () =>
            {
                if (selectedMap == map) StageMapAuthoringWorkspace.Open(map, true);
            };
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var slash = path.LastIndexOf('/');
            var parent = slash > 0 ? path.Substring(0, slash) : "Assets";
            var name = slash > 0 ? path.Substring(slash + 1) : path;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
