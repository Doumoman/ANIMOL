using System;
using System.Linq;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Presentation.Editor
{
    public sealed class PuzzleMapEditor : EditorWindow
    {
        enum Layer { Terrain, Device, Decoration, Markers }
        enum Marker { Start, Bubble, Exit }
        [SerializeField] PuzzleMap map;
        Layer layer;
        Marker marker;
        TerrainMaterial material;
        DeviceKind device = DeviceKind.Conveyor;
        int direction;
        Vector2Int portalTarget;
        Sprite decoration;
        Color decorationTint = Color.white;
        bool blocked, erase, stroke;
        int pendingWidth, pendingHeight, undoGroup;
        float cellSize = 42;
        Vector2 gridScroll, settingsScroll;
        Vector2Int lastPaint = new Vector2Int(-999, -999);
        [MenuItem("ANIMOL/Puzzle/Map Editor")]
        public static void Open() => GetWindow<PuzzleMapEditor>("Puzzle Map");
        public static void Edit(PuzzleMap data) { var window = GetWindow<PuzzleMapEditor>("Puzzle Map"); window.Select(data); }
        void OnEnable() { Undo.undoRedoPerformed += Undone; if (map) { pendingWidth = map.Width; pendingHeight = map.Height; } }
        void OnDisable() { Undo.undoRedoPerformed -= Undone; EndStroke(); }
        void Undone() { if (map) { map.InvalidateIndex(); pendingWidth = map.Width; pendingHeight = map.Height; } Repaint(); }
        void Select(PuzzleMap data) { EndStroke(); map = data; if (map) { pendingWidth = map.Width; pendingHeight = map.Height; } Repaint(); }
        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var selected = (PuzzleMap)EditorGUILayout.ObjectField(map, typeof(PuzzleMap), false, GUILayout.Width(250));
            if (selected != map) Select(selected);
            if (GUILayout.Button("New", EditorStyles.toolbarButton)) Create();
            if (GUILayout.Button("Undo", EditorStyles.toolbarButton)) Undo.PerformUndo();
            if (GUILayout.Button("Redo", EditorStyles.toolbarButton)) Undo.PerformRedo();
            if (GUILayout.Button("Save", EditorStyles.toolbarButton)) AssetDatabase.SaveAssets();
            using (new EditorGUI.DisabledScope(!map || EditorApplication.isPlaying))
                if (GUILayout.Button("Play Preview", EditorStyles.toolbarButton)) PuzzlePreview.Play(map);
            EditorGUILayout.EndHorizontal();
            if (!map) { EditorGUILayout.HelpBox("Create or select a Puzzle Map. Left drag paints; right drag erases. All layers are independent.", MessageType.Info); return; }
            EditorGUILayout.BeginHorizontal();
            settingsScroll = EditorGUILayout.BeginScrollView(settingsScroll, GUILayout.Width(280));
            Settings(); EditorGUILayout.EndScrollView();
            EditorGUILayout.BeginVertical();
            cellSize = EditorGUILayout.Slider("Cell zoom", cellSize, 20, 80);
            EditorGUILayout.LabelField("Left: paint   Right: erase   Gray hatch: retained outside map");
            var viewport = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawGrid(viewport);
            EditorGUILayout.EndVertical(); EditorGUILayout.EndHorizontal();
            if (Event.current.rawType == EventType.MouseUp) EndStroke();
        }
        void Settings()
        {
            EditorGUILayout.LabelField("Stage", EditorStyles.boldLabel);
            pendingWidth = EditorGUILayout.IntField("Width", pendingWidth);
            pendingHeight = EditorGUILayout.IntField("Height", pendingHeight);
            if (GUILayout.Button("Apply dimensions (retain cropped data)") && Resize(map, pendingWidth, pendingHeight, true)) { pendingWidth = map.Width; pendingHeight = map.Height; }
            var outside = map.OutsideCount(map.Width, map.Height);
            if (outside > 0) EditorGUILayout.HelpBox(outside + " records outside bounds are retained and inactive. Expand the map to recover them. Start/exit outside prevents preview.", MessageType.Warning);
            var serialized = new SerializedObject(map); serialized.Update();
            EditorGUILayout.PropertyField(serialized.FindProperty("theme"));
            EditorGUILayout.PropertyField(serialized.FindProperty("targetBubbles"));
            EditorGUILayout.PropertyField(serialized.FindProperty("cameraSettings"), true);
            if (serialized.ApplyModifiedProperties()) map.InvalidateIndex();
            EditorGUILayout.Space();
            layer = (Layer)GUILayout.Toolbar((int)layer, new[] { "Terrain", "Device", "Decor", "Markers" });
            erase = EditorGUILayout.Toggle("Erase brush", erase);
            if (layer == Layer.Terrain)
            { material = (TerrainMaterial)EditorGUILayout.EnumPopup("Material", material); blocked = EditorGUILayout.Toggle("Blocked cell", blocked); }
            if (layer == Layer.Device)
            {
                device = (DeviceKind)EditorGUILayout.EnumPopup("Device", device);
                direction = EditorGUILayout.Popup("Direction", direction, new[] { "North", "East", "South", "West" });
                if (device == DeviceKind.Portal) portalTarget = EditorGUILayout.Vector2IntField("Portal destination", portalTarget);
            }
            if (layer == Layer.Decoration)
            { decoration = (Sprite)EditorGUILayout.ObjectField("Sprite", decoration, typeof(Sprite), false); decorationTint = EditorGUILayout.ColorField("Tint", decorationTint); }
            if (layer == Layer.Markers) marker = (Marker)EditorGUILayout.EnumPopup("Place", marker);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            var errors = map.ValidateMap();
            if (!map.theme) EditorGUILayout.HelpBox("Assign a puzzle theme.", MessageType.Error);
            foreach (var error in errors) EditorGUILayout.HelpBox(error, MessageType.Error);
            if (errors.Count == 0 && map.theme) EditorGUILayout.HelpBox("Ready to play. This validates placements, not puzzle solvability.", MessageType.Info);
            if (outside > 0)
            {
                EditorGUILayout.LabelField("Retained coordinates", EditorStyles.boldLabel);
                foreach (var p in map.terrain.Select(t => t.position).Concat(map.devices.Select(d => d.position)).Concat(map.decorations.Select(d => d.position)).Concat(map.bubbles).Concat(new[] { map.start, map.exit }).Where(p => !map.Contains(p)).Distinct().Take(40))
                    EditorGUILayout.LabelField(p.ToString());
            }
        }
        public static bool Resize(PuzzleMap data, int width, int height, bool confirm)
        {
            width = Mathf.Clamp(width, 1, 256); height = Mathf.Clamp(height, 1, 256);
            var count = data.OutsideCount(width, height);
            if (confirm && count > 0 && !EditorUtility.DisplayDialog("Retain cropped placements", count + " records will be outside the new map. They will remain saved but inactive. Expanding recovers them; invalid start/exit must be moved before playing.", "Resize and retain", "Cancel")) return false;
            Undo.RegisterCompleteObjectUndo(data, "Resize puzzle map"); data.Resize(width, height); EditorUtility.SetDirty(data); return true;
        }
        void DrawGrid(Rect viewport)
        {
            var retained = map.terrain.Select(t => t.position).Concat(map.devices.Select(d => d.position)).Concat(map.decorations.Select(d => d.position)).Concat(map.bubbles).Concat(new[] { map.start, map.exit }).ToArray();
            var shownWidth = Mathf.Max(map.Width, retained.Max(p => p.x) + 1);
            var shownHeight = Mathf.Max(map.Height, retained.Max(p => p.y) + 1);
            var content = new Rect(0, 0, (shownWidth + 1) * cellSize, (shownHeight + 1) * cellSize);
            var pointerInside = viewport.Contains(Event.current.mousePosition);
            gridScroll = GUI.BeginScrollView(viewport, gridScroll, content);
            var xMin = Mathf.Max(0, Mathf.FloorToInt(gridScroll.x / cellSize)); var xMax = Mathf.Min(shownWidth, Mathf.CeilToInt((gridScroll.x + viewport.width) / cellSize));
            var yMin = Mathf.Max(0, Mathf.FloorToInt(gridScroll.y / cellSize)); var yMax = Mathf.Min(shownHeight, Mathf.CeilToInt((gridScroll.y + viewport.height) / cellSize));
            var islandArt = map.theme ? map.theme.skyIsland : null;
            if (islandArt)
            {
                // Paint every base first, then the entire underside before any grass.
                for (var row = yMin; row < yMax; row++) for (var x = xMin; x < xMax; x++)
                {
                    var p = new Vector2Int(x, shownHeight - 1 - row);
                    EditorGUI.DrawRect(new Rect(x * cellSize, row * cellSize, cellSize, cellSize), map.Contains(p) ? new Color(.08f, .11f, .16f) : new Color(.25f, .12f, .16f));
                }
                for (var row = Mathf.Max(0, yMin - 1); row < yMax; row++) for (var x = xMin; x < xMax; x++)
                {
                    var p = new Vector2Int(x, shownHeight - 1 - row);
                    if (!SkyIslandAutotile.UsesArt(map, p)) continue;
                    var shape = SkyIslandAutotile.Resolve(map, p);
                    var pair = islandArt.Find(shape.exposure, shape.innerCorners);
                    DrawSprite(new Rect(x * cellSize, (row - SkyIslandArt.UndersideY) * cellSize, cellSize, cellSize), pair.underside);
                }
            }
            for (var row = yMin; row < yMax; row++) for (var x = xMin; x < xMax; x++)
            {
                var p = new Vector2Int(x, shownHeight - 1 - row); var rect = new Rect(x * cellSize, row * cellSize, cellSize - 1, cellSize - 1);
                if (!islandArt) EditorGUI.DrawRect(rect, map.Contains(p) ? new Color(.08f, .11f, .16f) : new Color(.25f, .12f, .16f));
                if (map.TryTerrain(p, out var tile))
                {
                    var shape = PuzzleAutotile.Resolve(map, p); var art = map.theme ? map.theme.Art(tile.material) : null;
                    if (islandArt && SkyIslandAutotile.UsesArt(map, p))
                    {
                        var islandShape = SkyIslandAutotile.Resolve(map, p);
                        DrawSprite(new Rect(rect.x, rect.y, cellSize, cellSize), islandArt.Find(islandShape.exposure, islandShape.innerCorners).grass);
                    }
                    else if (art != null && art.outer.Length == 16)
                    {
                        for (var i = 0; i < 4; i++)
                        {
                            var source = (shape.innerCorners & (1 << i)) != 0 && i < art.inner.Length ? art.inner[i] : art.outer[shape.exposure];
                            var offset = PuzzleTerrainSprites.Offset(i);
                            DrawSprite(new Rect(rect.x + (offset.x + .25f) * rect.width, rect.y + (.25f - offset.y) * rect.height, rect.width * .5f, rect.height * .5f), PuzzleTerrainSprites.Quadrant(source, i));
                        }
                    }
                    else EditorGUI.DrawRect(rect, tile.material == TerrainMaterial.Grass ? new Color(.25f, .5f, .38f) : new Color(.5f, .5f, .6f));
                    if (tile.blocked) GUI.Label(rect, "#");
                }
                var d = map.DeviceAt(p); if (d.kind != DeviceKind.None) GUI.Label(rect, d.kind == DeviceKind.Portal ? "O" : d.kind.ToString().Substring(0, 1) + DirectionGlyph(d.direction));
                foreach (var decor in map.decorations.Where(a => a.position == p)) DrawSprite(rect, decor.sprite ? decor.sprite : map.theme ? map.theme.decoration : null);
                if (map.bubbles.Contains(p)) GUI.Label(rect, "B", EditorStyles.boldLabel);
                if (map.start == p) GUI.Label(rect, "S", EditorStyles.boldLabel);
                if (map.exit == p) GUI.Label(new Rect(rect.x + cellSize * .5f, rect.y, cellSize * .5f, cellSize), "E", EditorStyles.boldLabel);
                if (!map.Contains(p)) GUI.Label(rect, "/");
            }
            var e = Event.current;
            if (pointerInside && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && (e.button == 0 || e.button == 1))
            {
                var p = new Vector2Int(Mathf.FloorToInt(e.mousePosition.x / cellSize), shownHeight - 1 - Mathf.FloorToInt(e.mousePosition.y / cellSize));
                if (map.Contains(p))
                {
                    if (!stroke) { Undo.IncrementCurrentGroup(); undoGroup = Undo.GetCurrentGroup(); Undo.RegisterCompleteObjectUndo(map, "Paint puzzle layer"); stroke = true; }
                    if (lastPaint != p) { Paint(p, erase || e.button == 1); lastPaint = p; }
                    e.Use(); Repaint();
                }
            }
            GUI.EndScrollView();
        }
        static string DirectionGlyph(Vector2Int d) => d == Vector2Int.up ? "^" : d == Vector2Int.right ? ">" : d == Vector2Int.down ? "v" : "<";
        static void DrawSprite(Rect r, Sprite sprite)
        { if (!sprite) return; var t = sprite.texture; var s = sprite.rect; GUI.DrawTextureWithTexCoords(r, t, new Rect(s.x / t.width, s.y / t.height, s.width / t.width, s.height / t.height)); }
        void Paint(Vector2Int p, bool remove)
        {
            if (layer == Layer.Terrain) { if (remove) map.RemoveTerrain(p); else map.SetTerrain(p, material, blocked); }
            if (layer == Layer.Device) map.SetDevice(new DeviceCell { position = p, kind = remove ? DeviceKind.None : device, direction = PuzzleAutotile.Cardinal[direction], portalTarget = portalTarget });
            if (layer == Layer.Decoration) { map.decorations.RemoveAll(d => d.position == p); if (!remove) map.decorations.Add(new DecorationCell { position = p, sprite = decoration, tint = decorationTint }); }
            if (layer == Layer.Markers)
            {
                if (marker == Marker.Bubble) { map.bubbles.RemoveAll(b => b == p); if (!remove) map.bubbles.Add(p); }
                else if (!remove) { if (marker == Marker.Start) map.start = p; else map.exit = p; }
            }
            EditorUtility.SetDirty(map); map.InvalidateIndex();
        }
        void EndStroke() { if (stroke) Undo.CollapseUndoOperations(undoGroup); stroke = false; lastPaint = new Vector2Int(-999, -999); }
        void Create()
        {
            var path = EditorUtility.SaveFilePanelInProject("New puzzle map", "PuzzleMap", "asset", "Select a map asset destination", "Assets/ANIMOL/Data");
            if (path.Length == 0) return; var data = CreateInstance<PuzzleMap>(); AssetDatabase.CreateAsset(data, path); Select(data);
        }
    }
    [CustomEditor(typeof(PuzzleMap))]
    public sealed class PuzzleMapInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open Puzzle Map Editor")) PuzzleMapEditor.Edit((PuzzleMap)target);
            EditorGUILayout.HelpBox("Resize in the map editor to review cropped records. Terrain, device, decoration and markers remain independent.", MessageType.Info);
            DrawDefaultInspector();
            var map = (PuzzleMap)target;
            var count = map.OutsideCount(map.Width, map.Height);
            if (count > 0) EditorGUILayout.HelpBox(count + " out-of-bounds records retained.", MessageType.Warning);
        }
    }
}
