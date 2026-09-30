using System;
using System.Linq;
using ANIMOL.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ANIMOL.Editor
{
    public enum StageMapAuthoringProxyRole { Cell, Object, PathNode }

    public sealed class StageMapAuthoringProxy : MonoBehaviour
    {
        [SerializeField] private StageMapDefinition map;
        [SerializeField] private StageMapAuthoringProxyRole role;
        [SerializeField] private StageMapLayer layer;
        [SerializeField] private string stableId = string.Empty;
        [SerializeField] private int pathNodeIndex = -1;
        [SerializeField] private Vector2Int authoredCell;
        [SerializeField] private Vector2 authoredSize = Vector2.one;

        public StageMapDefinition Map => map;
        public StageMapAuthoringProxyRole Role => role;
        public StageMapLayer Layer => layer;
        public string StableId => stableId;
        public int PathNodeIndex => pathNodeIndex;
        public Vector2Int AuthoredCell => authoredCell;
        public Vector2 AuthoredSize => authoredSize;

        public void EditorConfigure(StageMapDefinition sourceMap, StageMapAuthoringProxyRole proxyRole,
            StageMapLayer sourceLayer, string objectStableId, int nodeIndex, Vector2Int cell)
        {
            map = sourceMap;
            role = proxyRole;
            layer = sourceLayer;
            stableId = objectStableId ?? string.Empty;
            pathNodeIndex = nodeIndex;
            authoredCell = cell;
            authoredSize = Vector2.one;
        }

        public void EditorSetAuthoredSize(Vector2 size) => authoredSize = size;
    }

    [InitializeOnLoad]
    public static class StageMapAuthoringWorkspace
    {
        public const string ScenePath = "Assets/ANIMOL/Scenes/MapAuthoringWorkspace.unity";
        private const string DynamicRootName = "[MAP PREVIEW - NOT SAVED]";
        private const string SessionMapPathKey = "ANIMOL.MapAuthoringWorkspace.MapPath";
        private static StageMapDefinition currentMap;
        private static StageMapDefinition pendingRefreshMap;
        private static double refreshAfter;
        private static Sprite previewSprite;
        private static Material pathMaterial;
        private static bool handleDragging;
        private static Vector2Int pendingHandleCell;
        private static int handleProxyId;
        private static bool workspaceHidTools;
        private static bool toolsWereHidden;

        public static StageMapDefinition CurrentMap
        {
            get
            {
                if (currentMap == null && IsOpen)
                {
                    currentMap = Resources.FindObjectsOfTypeAll<StageMapAuthoringProxy>()
                        .Select(item => item.Map).FirstOrDefault(item => item != null);
                    if (currentMap == null)
                    {
                        var mapPath = SessionState.GetString(SessionMapPathKey, T01S01ManualMapWorkflow.MapPath);
                        currentMap = AssetDatabase.LoadAssetAtPath<StageMapDefinition>(mapPath);
                    }
                }
                return currentMap;
            }
        }
        public static bool IsOpen => SceneManager.GetActiveScene().path == ScenePath;
        public static bool IsOpenFor(StageMapDefinition map) => IsOpen && map != null && CurrentMap == map;

        static StageMapAuthoringWorkspace()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
            SceneView.duringSceneGui -= OnWorkspaceSceneGUI;
            SceneView.duringSceneGui += OnWorkspaceSceneGUI;
            EditorApplication.update -= MaintainToolVisibility;
            EditorApplication.update += MaintainToolVisibility;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += RestoreOpenWorkspaceAfterReload;
        }

        [MenuItem("ANIMOL/Map Editor/Open Focused Workspace %#w")]
        public static void OpenSelectedMap()
        {
            var map = Selection.activeObject as StageMapDefinition ?? currentMap ??
                      AssetDatabase.LoadAssetAtPath<StageMapDefinition>(T01S01ManualMapWorkflow.MapPath);
            if (map == null) EditorUtility.DisplayDialog("ANIMOL Map Workspace", "먼저 StageMapDefinition 맵 자산을 선택하세요.", "확인");
            else Open(map, true);
        }

        public static bool Open(StageMapDefinition map, bool askToSaveCurrentScene)
        {
            if (map == null || EditorApplication.isPlayingOrWillChangePlaymode) return false;
            var active = SceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (askToSaveCurrentScene && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
                EnsureWorkspaceSceneAsset();
                if (SceneManager.GetActiveScene().path != ScenePath)
                    EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            currentMap = map;
            SessionState.SetString(SessionMapPathKey, AssetDatabase.GetAssetPath(map));
            RebuildPreview(map);
            FocusMap(map);
            return true;
        }

        public static void ScheduleRefresh(StageMapDefinition map)
        {
            if (!IsOpenFor(map)) return;
            pendingRefreshMap = map;
            refreshAfter = EditorApplication.timeSinceStartup + .12d;
            EditorApplication.update -= FlushRefresh;
            EditorApplication.update += FlushRefresh;
        }

        public static void RebuildPreview(StageMapDefinition map)
        {
            if (!IsOpen || map == null) return;
            currentMap = map;
            var previous = GameObject.Find(DynamicRootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);

            var root = CreatePreviewObject(DynamicRootName, null);
            root.transform.position = Vector3.zero;
            var chunks = CreatePreviewObject("00 CHUNKS (16x16 CELLS)", root.transform);
            var background = CreatePreviewObject("01 BACKGROUND", root.transform);
            var terrain = CreatePreviewObject("02 TERRAIN", root.transform);
            var decoration = CreatePreviewObject("03 DECORATION", root.transform);
            var markers = CreatePreviewObject("04 SPECIAL BLOCKS", root.transform);
            var logic = CreatePreviewObject("05 LOGIC OBJECTS", root.transform);
            var paths = CreatePreviewObject("06 MOVEMENT PATHS", root.transform);

            foreach (var chunk in EnumerateChunks(map.EditorPreviewChunkBounds))
            {
                var chunkObject = CreatePreviewObject($"[CHUNK {chunk.x},{chunk.y}] cells {chunk.x * 16}..{chunk.x * 16 + 15} / {chunk.y * 16}..{chunk.y * 16 + 15}", chunks.transform);
                CreateChunkOutline(chunkObject, chunk, map);
            }

            foreach (var cell in map.Cells)
            {
                var parent = cell.Layer switch
                {
                    StageMapLayer.Background => background.transform,
                    StageMapLayer.Decoration => decoration.transform,
                    _ => terrain.transform
                };
                var proxy = CreateVisualProxy($"[{cell.Layer}] ({cell.X},{cell.Y}) {cell.TileId} / {cell.VariantId}",
                    parent, map, StageMapAuthoringProxyRole.Cell, cell.Layer, string.Empty, -1,
                    new Vector2Int(cell.X, cell.Y), Vector2.one, CellColor(cell), cell.Layer == StageMapLayer.Terrain ? 10 : 0,
                    StageMapScenePalette.ResolveTerrainSprite(cell.TileId, map.ThemeId));
                proxy.transform.position = CellCenter(cell.X, cell.Y, map);
            }

            foreach (var placement in map.Objects)
            {
                var marker = T01S01ManualMapWorkflow.IsCampaignMarker(placement.Kind);
                var footprint = placement.Settings.FootprintCells;
                var type = StageMapScenePalette.FindType(placement.DataKey);
                var proxy = CreateVisualProxy($"[{StageMapScenePalette.MarkerLabel(placement.Kind)}] {placement.StableId} @ ({placement.X},{placement.Y})",
                    marker ? markers.transform : logic.transform, map, StageMapAuthoringProxyRole.Object,
                    StageMapLayer.Object, placement.StableId, -1, new Vector2Int(placement.X, placement.Y), footprint,
                    StageMapScenePalette.ObjectColor(placement.Kind), 30, marker ? StageMapScenePalette.ResolvePreviewSprite(placement) : null);
                proxy.transform.position = FootprintCenter(placement.X, placement.Y, footprint, map,
                    placement.Kind == StageMapObjectKind.HalfBlock && placement.Settings.HalfPlacement == HalfBlockPlacement.Upper ? .5f : 0f);
                if (!marker && type?.Prefab != null)
                {
                    proxy.GetComponent<SpriteRenderer>().color = new Color(.1f, .9f, .85f, .12f);
                    CreatePrefabVisualPreview(proxy.transform, type.Prefab, footprint, map.GetEditorPreviewUnitsPerCell(), 30);
                    CreateOccupancyOutlines(placement, logic.transform, map);
                }

                var nodes = placement.Settings.PathCells;
                if (nodes.Count > 1) CreatePathLine(placement, paths.transform, map);
                for (var index = 0; index < nodes.Count; index++)
                {
                    var node = nodes[index];
                    var nodeProxy = CreateVisualProxy($"[PATH {index}] {placement.StableId} @ ({node.x},{node.y})",
                        paths.transform, map, StageMapAuthoringProxyRole.PathNode, StageMapLayer.Object,
                        placement.StableId, index, node, Vector2.one * .38f, new Color(1f, .22f, .85f, .95f), 50);
                    nodeProxy.transform.position = CellCenter(node.x, node.y, map) + Vector3.back * .02f;
                }
            }

            UpdatePreviewCamera(map);
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.isDirty) EditorSceneManager.SaveScene(activeScene, ScenePath);
            SceneView.RepaintAll();
        }

        public static int ExpectedProxyCount(StageMapDefinition map) => map == null ? 0 :
            map.Cells.Count + map.Objects.Count + map.Objects.Sum(item => item.Settings.PathCells.Count);

        public static void FocusMap(StageMapDefinition map)
        {
            if (map == null) return;
            var scene = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
            var bounds = map.EditorPreviewCellBounds;
            var units = map.GetEditorPreviewUnitsPerCell();
            scene.in2DMode = true;
            scene.LookAt(new Vector3(bounds.center.x * units, bounds.center.y * units, 0f), Quaternion.identity,
                Mathf.Max(8f, Mathf.Max(bounds.width, bounds.height) * units * .58f));
            scene.Focus();
            SceneView.RepaintAll();
        }

        public static StageMapAuthoringProxy FindProxy(string stableId, StageMapAuthoringProxyRole role, int pathNodeIndex,
            Vector2Int cell, StageMapLayer layer)
        {
            return Resources.FindObjectsOfTypeAll<StageMapAuthoringProxy>()
                .FirstOrDefault(item => item.Map == currentMap && item.Role == role && item.StableId == (stableId ?? string.Empty) &&
                                        item.PathNodeIndex == pathNodeIndex && item.AuthoredCell == cell && item.Layer == layer);
        }

        public static void RefreshAndReselect(StageMapAuthoringProxy source, Vector2Int destination)
        {
            if (source == null || source.Map == null) return;
            var map = source.Map;
            var role = source.Role;
            var stableId = source.StableId;
            var nodeIndex = source.PathNodeIndex;
            var cell = destination;
            var layer = source.Layer;
            EditorApplication.delayCall += () =>
            {
                if (!IsOpenFor(map)) return;
                RebuildPreview(map);
                var replacement = FindProxy(stableId, role, nodeIndex, cell, layer);
                if (replacement != null) Selection.activeGameObject = replacement.gameObject;
            };
        }

        private static void EnsureWorkspaceSceneAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("ANIMOL MAP AUTHORING WORKSPACE - EDITOR ONLY");
            var cameraObject = new GameObject("Authoring Preview Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 12f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.055f, .065f, .09f, 1f);
            cameraObject.transform.position = new Vector3(0f, 0f, -20f);
            cameraObject.transform.SetParent(root.transform);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.path != ScenePath) return;
            var mapPath = SessionState.GetString(SessionMapPathKey, T01S01ManualMapWorkflow.MapPath);
            var map = AssetDatabase.LoadAssetAtPath<StageMapDefinition>(mapPath);
            if (map != null) EditorApplication.delayCall += () => { RebuildPreview(map); FocusMap(map); };
        }

        private static void RestoreOpenWorkspaceAfterReload()
        {
            if (!IsOpen) return;
            var mapPath = SessionState.GetString(SessionMapPathKey, T01S01ManualMapWorkflow.MapPath);
            var map = AssetDatabase.LoadAssetAtPath<StageMapDefinition>(mapPath);
            if (map == null) return;
            currentMap = map;
            RebuildPreview(map);
            OnSelectionChanged();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode || !IsOpen) return;
            EditorApplication.delayCall += () =>
            {
                var map = CurrentMap;
                if (map == null || !IsOpen) return;
                RebuildPreview(map);
                FocusMap(map);
            };
        }

        private static void OnSelectionChanged()
        {
            var proxy = Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponentInParent<StageMapAuthoringProxy>();
            MaintainToolVisibility();
            if (proxy != null && IsOpen) CampaignMapEditorWindow.SelectWorkspaceProxy(proxy);
        }

        private static void MaintainToolVisibility()
        {
            if (IsOpen && Selection.activeObject is StageMapDefinition requestedMap && requestedMap != CurrentMap)
            {
                currentMap = requestedMap;
                SessionState.SetString(SessionMapPathKey, AssetDatabase.GetAssetPath(requestedMap));
                RebuildPreview(requestedMap);
                FocusMap(requestedMap);
                CampaignMapEditorWindow.OpenForWorkspace(requestedMap);
            }

            var active = Selection.activeGameObject;
            var proxySelected = IsOpen && active != null && active.GetComponentInParent<StageMapAuthoringProxy>() != null;
            if (proxySelected)
            {
                if (!workspaceHidTools)
                {
                    toolsWereHidden = Tools.hidden;
                    workspaceHidTools = true;
                }
                Tools.hidden = true;
            }
            else if (workspaceHidTools)
            {
                Tools.hidden = toolsWereHidden;
                workspaceHidTools = false;
                handleDragging = false;
                handleProxyId = 0;
            }
        }

        private static void OnWorkspaceSceneGUI(SceneView sceneView)
        {
            if (!IsOpen || Selection.activeGameObject == null) return;
            var proxy = Selection.activeGameObject.GetComponentInParent<StageMapAuthoringProxy>();
            if (proxy == null || proxy.Map == null) return;

            // A raw Transform handle only changes the disposable preview. This handle snaps to
            // cells and commits through the validated map authoring operations instead.
            Tools.hidden = true;
            var units = proxy.Map.GetEditorPreviewUnitsPerCell();
            DrawFocusedProxyHighlight(proxy, units);
            EditorGUI.BeginChangeCheck();
            var moved = Handles.PositionHandle(proxy.transform.position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                pendingHandleCell = new Vector2Int(Mathf.FloorToInt(moved.x / units), Mathf.FloorToInt(moved.y / units));
                var size = proxy.AuthoredSize;
                var yOffset = 0f;
                if (proxy.Role == StageMapAuthoringProxyRole.Object)
                {
                    var placement = proxy.Map.Objects.FirstOrDefault(item => item.StableId == proxy.StableId);
                    if (placement != null && placement.Kind == StageMapObjectKind.HalfBlock &&
                        placement.Settings.HalfPlacement == HalfBlockPlacement.Upper) yOffset = .5f;
                }
                proxy.transform.position = proxy.Role == StageMapAuthoringProxyRole.Object
                    ? new Vector3((pendingHandleCell.x + size.x * .5f) * units, (pendingHandleCell.y + yOffset + size.y * .5f) * units, proxy.transform.position.z)
                    : new Vector3((pendingHandleCell.x + .5f) * units, (pendingHandleCell.y + .5f) * units, proxy.transform.position.z);
                handleDragging = true;
                handleProxyId = proxy.GetInstanceID();
            }

            if (!handleDragging || handleProxyId != proxy.GetInstanceID() || GUIUtility.hotControl != 0 ||
                Event.current.type != EventType.Repaint) return;
            handleDragging = false;
            handleProxyId = 0;
            CommitProxyMove(proxy, pendingHandleCell);
        }

        private static void DrawFocusedProxyHighlight(StageMapAuthoringProxy proxy, float units)
        {
            if (proxy.Role != StageMapAuthoringProxyRole.Object) return;
            var placement = proxy.Map.Objects.FirstOrDefault(item => item.StableId == proxy.StableId);
            if (placement == null) return;
            var footprint = placement.Settings.FootprintCells;
            var yOffset = placement.Kind == StageMapObjectKind.HalfBlock && placement.Settings.HalfPlacement == HalfBlockPlacement.Upper ? .5f : 0f;
            var rect = new Rect(placement.X * units, (placement.Y + yOffset) * units,
                Mathf.Max(.5f, footprint.x) * units, Mathf.Max(.5f, footprint.y) * units);
            Handles.DrawSolidRectangleWithOutline(rect, new Color(0f, 1f, .95f, .08f), new Color(0f, 1f, .95f, 1f));
            Handles.Label(new Vector3(rect.center.x, rect.yMax + .18f * units), $"▶ {placement.DataKey}", EditorStyles.whiteBoldLabel);
        }

        private static void CommitProxyMove(StageMapAuthoringProxy proxy, Vector2Int destination)
        {
            var success = false;
            var message = string.Empty;
            if (proxy.Role == StageMapAuthoringProxyRole.Cell)
                success = StageMapAuthoringOperations.MoveCell(proxy.Map, proxy.AuthoredCell, destination, proxy.Layer, out message);
            else if (proxy.Role == StageMapAuthoringProxyRole.Object)
            {
                success = StageMapObjectAuthoringOperations.Move(proxy.Map, proxy.StableId, destination, out var validation);
                message = string.Join(" | ", validation.Errors.Concat(validation.Warnings));
            }
            else
            {
                var placement = proxy.Map.Objects.FirstOrDefault(item => item.StableId == proxy.StableId);
                if (placement != null && proxy.PathNodeIndex >= 0 && proxy.PathNodeIndex < placement.Settings.PathCells.Count)
                {
                    var path = placement.Settings.PathCells.ToArray();
                    path[proxy.PathNodeIndex] = destination;
                    success = StageMapObjectAuthoringOperations.SetPath(proxy.Map, proxy.StableId, path, out var validation);
                    message = string.Join(" | ", validation.Errors.Concat(validation.Warnings));
                }
            }

            if (!success)
            {
                Debug.LogWarning($"[ANIMOL][Map Workspace] Move rejected: {message}");
                RebuildPreview(proxy.Map);
                return;
            }
            if (!string.IsNullOrWhiteSpace(message)) Debug.Log($"[ANIMOL][Map Workspace] {message}");
            RefreshAndReselect(proxy, destination);
        }

        private static void FlushRefresh()
        {
            if (EditorApplication.timeSinceStartup < refreshAfter) return;
            EditorApplication.update -= FlushRefresh;
            var map = pendingRefreshMap;
            pendingRefreshMap = null;
            if (IsOpenFor(map)) RebuildPreview(map);
        }

        private static GameObject CreatePreviewObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name) { hideFlags = HideFlags.DontSaveInEditor };
            if (parent != null) gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static StageMapAuthoringProxy CreateVisualProxy(string name, Transform parent, StageMapDefinition map,
            StageMapAuthoringProxyRole role, StageMapLayer layer, string stableId, int nodeIndex, Vector2Int cell,
            Vector2 size, Color color, int sortingOrder, Sprite authoredSprite = null)
        {
            var gameObject = CreatePreviewObject(name, parent);
            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = authoredSprite != null ? authoredSprite : PreviewSprite();
            renderer.color = authoredSprite != null ? Color.white : color;
            renderer.sortingOrder = sortingOrder;
            var units = map.GetEditorPreviewUnitsPerCell();
            gameObject.transform.localScale = authoredSprite != null
                ? StageMapScenePalette.CalculateAspectFitScale(renderer.sprite, size, units)
                : new Vector3(Mathf.Max(.2f, size.x) * units, Mathf.Max(.2f, size.y) * units, 1f);
            var proxy = gameObject.AddComponent<StageMapAuthoringProxy>();
            proxy.EditorConfigure(map, role, layer, stableId, nodeIndex, cell);
            proxy.EditorSetAuthoredSize(size);
            return proxy;
        }

        private static void CreatePrefabVisualPreview(Transform proxy, GameObject prefab, Vector2 footprint, float units, int sortingOrder)
        {
            var visualRoot = CreatePreviewObject("ART PREVIEW (ALL ACTIVE PARTS)", proxy);
            var sourceRoot = prefab.transform;
            foreach (var source in prefab.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (source.sprite == null || !source.enabled || !IsActiveInPrefab(source.transform, sourceRoot)) continue;
                var part = CreatePreviewObject(source.gameObject.name, visualRoot.transform);
                part.transform.localPosition = sourceRoot.InverseTransformPoint(source.transform.position);
                part.transform.localRotation = Quaternion.Inverse(sourceRoot.rotation) * source.transform.rotation;
                var rootScale = sourceRoot.lossyScale;
                var sourceScale = source.transform.lossyScale;
                part.transform.localScale = new Vector3(
                    sourceScale.x / Mathf.Max(.0001f, rootScale.x),
                    sourceScale.y / Mathf.Max(.0001f, rootScale.y), 1f);
                var renderer = part.AddComponent<SpriteRenderer>();
                renderer.sprite = source.sprite;
                renderer.color = source.color;
                renderer.flipX = source.flipX;
                renderer.flipY = source.flipY;
                renderer.drawMode = source.drawMode;
                renderer.size = source.size;
                renderer.maskInteraction = source.maskInteraction;
                renderer.sortingOrder = sortingOrder + source.sortingOrder;
            }

            var renderers = visualRoot.GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            var target = new Vector2(Mathf.Max(.2f, footprint.x) * units, Mathf.Max(.2f, footprint.y) * units) * .9f;
            var uniform = Mathf.Min(target.x / Mathf.Max(.001f, bounds.size.x), target.y / Mathf.Max(.001f, bounds.size.y));
            visualRoot.transform.localScale = Vector3.one * uniform;
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            visualRoot.transform.position += proxy.position - bounds.center;
        }

        private static bool IsActiveInPrefab(Transform transform, Transform root)
        {
            for (var current = transform; current != null; current = current.parent)
            {
                if (!current.gameObject.activeSelf) return false;
                if (current == root) return true;
            }
            return false;
        }

        private static void CreateOccupancyOutlines(StageMapObjectPlacement placement, Transform parent, StageMapDefinition map)
        {
            var units = map.GetEditorPreviewUnitsPerCell();
            foreach (var cell in StageMapDefinition.EnumeratePlacementCells(placement).Distinct())
            {
                var outlineObject = CreatePreviewObject($"[OCCUPIED] {placement.StableId} ({cell.x},{cell.y})", parent);
                var line = outlineObject.AddComponent<LineRenderer>();
                line.sharedMaterial = PathMaterial();
                line.startColor = new Color(0f, 1f, .9f, .42f);
                line.endColor = line.startColor;
                line.startWidth = .035f * units;
                line.endWidth = line.startWidth;
                line.useWorldSpace = true;
                line.loop = true;
                line.positionCount = 4;
                var x = cell.x * units;
                var y = cell.y * units;
                line.SetPositions(new[]
                {
                    new Vector3(x, y, -.01f), new Vector3(x + units, y, -.01f),
                    new Vector3(x + units, y + units, -.01f), new Vector3(x, y + units, -.01f)
                });
                line.sortingOrder = 29;
            }
        }

        private static void CreatePathLine(StageMapObjectPlacement placement, Transform parent, StageMapDefinition map)
        {
            var lineObject = CreatePreviewObject($"[ROUTE] {placement.StableId}", parent);
            var line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = PathMaterial();
            line.startColor = new Color(1f, .22f, .85f, .75f);
            line.endColor = line.startColor;
            line.startWidth = .08f * map.GetEditorPreviewUnitsPerCell();
            line.endWidth = line.startWidth;
            line.positionCount = placement.Settings.PathCells.Count;
            line.useWorldSpace = true;
            line.sortingOrder = 45;
            for (var index = 0; index < placement.Settings.PathCells.Count; index++)
            {
                var cell = placement.Settings.PathCells[index];
                line.SetPosition(index, CellCenter(cell.x, cell.y, map) + Vector3.back * .01f);
            }
        }

        private static void CreateChunkOutline(GameObject chunkObject, Vector2Int chunk, StageMapDefinition map)
        {
            var units = map.GetEditorPreviewUnitsPerCell();
            var xMin = chunk.x * StageMapDefinition.TilesPerChunk * units;
            var yMin = chunk.y * StageMapDefinition.TilesPerChunk * units;
            var xMax = xMin + StageMapDefinition.TilesPerChunk * units;
            var yMax = yMin + StageMapDefinition.TilesPerChunk * units;
            var line = chunkObject.AddComponent<LineRenderer>();
            line.sharedMaterial = PathMaterial();
            line.startColor = new Color(.25f, .85f, 1f, .72f);
            line.endColor = line.startColor;
            line.startWidth = .045f * units;
            line.endWidth = line.startWidth;
            line.useWorldSpace = true;
            line.loop = false;
            line.positionCount = 5;
            line.SetPositions(new[]
            {
                new Vector3(xMin, yMin, .03f), new Vector3(xMax, yMin, .03f),
                new Vector3(xMax, yMax, .03f), new Vector3(xMin, yMax, .03f),
                new Vector3(xMin, yMin, .03f)
            });
            line.sortingOrder = -20;
        }

        private static Sprite PreviewSprite()
        {
            if (previewSprite != null) return previewSprite;
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            previewSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(.5f, .5f), 1f);
            previewSprite.hideFlags = HideFlags.HideAndDontSave;
            return previewSprite;
        }

        private static Material PathMaterial()
        {
            if (pathMaterial != null) return pathMaterial;
            pathMaterial = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.HideAndDontSave };
            return pathMaterial;
        }

        private static Color CellColor(StageMapCell cell)
        {
            if (cell.Layer == StageMapLayer.Background) return new Color(.18f, .35f, .65f, .4f);
            if (cell.Layer == StageMapLayer.Decoration) return new Color(1f, .7f, .18f, .55f);
            var hash = (cell.TileId ?? string.Empty).GetHashCode() & 0x7fffffff;
            var tint = .08f * (hash % 5);
            return new Color(.27f + tint, .55f + tint * .45f, .82f, .86f);
        }

        private static Vector3 CellCenter(int x, int y, StageMapDefinition map)
        {
            var units = map.GetEditorPreviewUnitsPerCell();
            return new Vector3((x + .5f) * units, (y + .5f) * units, 0f);
        }

        private static Vector3 FootprintCenter(int x, int y, Vector2 footprint, StageMapDefinition map, float yOffset)
        {
            var units = map.GetEditorPreviewUnitsPerCell();
            return new Vector3((x + footprint.x * .5f) * units, (y + yOffset + footprint.y * .5f) * units, -.02f);
        }

        private static void UpdatePreviewCamera(StageMapDefinition map)
        {
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null) return;
            var bounds = map.EditorPreviewCellBounds;
            var units = map.GetEditorPreviewUnitsPerCell();
            camera.transform.position = new Vector3(bounds.center.x * units, bounds.center.y * units, -20f);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(8f, bounds.height * units * .55f);
        }

        private static Vector2Int[] EnumerateChunks(RectInt bounds)
        {
            var result = new Vector2Int[bounds.width * bounds.height];
            var index = 0;
            for (var y = bounds.yMin; y < bounds.yMax; y++)
            for (var x = bounds.xMin; x < bounds.xMax; x++)
                result[index++] = new Vector2Int(x, y);
            return result;
        }
    }

    [CustomEditor(typeof(StageMapAuthoringProxy))]
    public sealed class StageMapAuthoringProxyEditor : UnityEditor.Editor
    {
        private void OnEnable()
        {
            if (target is StageMapAuthoringProxy proxy) CampaignMapEditorWindow.SelectWorkspaceProxy(proxy);
        }

        public override void OnInspectorGUI()
        {
            var proxy = (StageMapAuthoringProxy)target;
            EditorGUILayout.LabelField("ANIMOL Map Data Proxy", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("이 오브젝트는 편집용 프리뷰입니다. 위치 변경은 원본 맵 데이터에 셀 스냅으로 저장되며, 프리뷰 자체는 씬에 저장되지 않습니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Map", proxy.Map, typeof(StageMapDefinition), false);
                EditorGUILayout.EnumPopup("Role", proxy.Role);
                EditorGUILayout.TextField("Stable ID", proxy.StableId);
                EditorGUILayout.Vector2IntField("Authored cell", proxy.AuthoredCell);
                if (proxy.Role == StageMapAuthoringProxyRole.PathNode) EditorGUILayout.IntField("Path node", proxy.PathNodeIndex);
            }
            if (GUILayout.Button("맵 전체 포커스")) StageMapAuthoringWorkspace.FocusMap(proxy.Map);
            if (GUILayout.Button("제작기 창 열기")) CampaignMapEditorWindow.OpenForWorkspace(proxy.Map);
        }

    }
}
