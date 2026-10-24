using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Presentation.Editor
{
    public sealed class PuzzleMapEditor : EditorWindow
    {
        enum Layer { Terrain, Device, Decoration, Markers, Water }
        enum Marker { Start, Bubble, Exit }
        [SerializeField] PuzzleMap map;
        [SerializeField] PuzzleTheme newMapTheme;
        [SerializeField] int newMapWidth = 12, newMapHeight = 18;
        Layer layer;
        Marker marker;
        TerrainMaterial material;
        DeviceKind device = DeviceKind.Conveyor;
        PrototypeDevice prototypeBrush;
        DeviceTemplate[] library = Array.Empty<DeviceTemplate>();
        bool useLibrary = true, inspectDevice = true, showConnections = true, linkTargetMode;
        int templateIndex;
        string selectedDeviceId;
        bool showScenario;
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
        void OnEnable()
        {
            Undo.undoRedoPerformed += Undone;
            EditorApplication.projectChanged += RefreshLibrary;
            RefreshLibrary();
            if (!newMapTheme) newMapTheme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>("Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset");
            if (map) { pendingWidth = map.Width; pendingHeight = map.Height; }
        }
        void OnDisable() { Undo.undoRedoPerformed -= Undone; EditorApplication.projectChanged -= RefreshLibrary; EndStroke(); }
        void Undone() { if (map) { map.InvalidateIndex(); pendingWidth = map.Width; pendingHeight = map.Height; } Repaint(); }
        void Select(PuzzleMap data)
        {
            EndStroke(); map = data; selectedDeviceId = null; linkTargetMode = false;
            if (map) { pendingWidth = map.Width; pendingHeight = map.Height; newMapWidth = map.Width; newMapHeight = map.Height; if (map.theme) newMapTheme = map.theme; }
            Repaint();
        }
        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var selected = (PuzzleMap)EditorGUILayout.ObjectField(map, typeof(PuzzleMap), false, GUILayout.Width(250));
            if (selected != map) Select(selected);
            if (GUILayout.Button("New", EditorStyles.toolbarButton)) Select(null);
            if (GUILayout.Button("Undo", EditorStyles.toolbarButton)) Undo.PerformUndo();
            if (GUILayout.Button("Redo", EditorStyles.toolbarButton)) Undo.PerformRedo();
            if (GUILayout.Button("Save", EditorStyles.toolbarButton)) AssetDatabase.SaveAssets();
            using (new EditorGUI.DisabledScope(!map || EditorApplication.isPlaying))
                if (GUILayout.Button("Play Preview", EditorStyles.toolbarButton)) PuzzlePreview.Play(map);
            EditorGUILayout.EndHorizontal();
            if (!map)
            {
                EditorGUILayout.HelpBox("Create a playable starter map with its own device scenario, or select an existing map. Left drag paints; right drag erases.", MessageType.Info);
                NewMapSettings();
                if (GUILayout.Button("Create starter map")) Create();
                return;
            }
            EditorGUILayout.BeginHorizontal();
            settingsScroll = EditorGUILayout.BeginScrollView(settingsScroll, GUILayout.Width(280));
            Settings(); EditorGUILayout.EndScrollView();
            EditorGUILayout.BeginVertical();
            cellSize = EditorGUILayout.Slider("Cell zoom", cellSize, 20, 80);
            EditorGUILayout.LabelField("Left: paint/select   Right: erase   Terrain: paint or erase any cell");
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
            EditorGUILayout.PropertyField(serialized.FindProperty("scenario"));
            EditorGUILayout.PropertyField(serialized.FindProperty("targetBubbles"));
            EditorGUILayout.PropertyField(serialized.FindProperty("cameraSettings"), true);
            if (serialized.ApplyModifiedProperties()) map.InvalidateIndex();
            if (map.scenario)
            {
                showScenario = EditorGUILayout.Foldout(showScenario, "Themed devices / water / goals", true);
                if (showScenario)
                {
                    var scenario = new SerializedObject(map.scenario); scenario.Update();
                    EditorGUILayout.PropertyField(scenario.FindProperty("title"));
                    EditorGUILayout.PropertyField(scenario.FindProperty("themeId"));
                    EditorGUILayout.PropertyField(scenario.FindProperty("costLimit"));
                    EditorGUILayout.PropertyField(scenario.FindProperty("devices"), true);
                    EditorGUILayout.PropertyField(scenario.FindProperty("water"), true);
                    EditorGUILayout.PropertyField(scenario.FindProperty("goals"), true);
                    EditorGUILayout.PropertyField(scenario.FindProperty("verificationRoute"));
                    EditorGUILayout.LabelField("Current device policies", EditorStyles.boldLabel);
                    foreach (var field in new[] { "holdOccupiedBridge", "shutterPhaseAdvancesWhenOccupied", "leapSkipsIntermediate",
                        "mirrorBumpRotates", "magnetSwitchLatches", "bombHurtsPlayer", "bombDestroysOrdinaryObjects",
                        "statueStopsAfterBreakingWall" })
                        EditorGUILayout.PropertyField(scenario.FindProperty(field));
                    scenario.ApplyModifiedProperties();
                }
            }
            EditorGUILayout.Space();
            layer = (Layer)GUILayout.Toolbar((int)layer, new[] { "Terrain", "Device", "Decor", "Markers", "Water" });
            erase = EditorGUILayout.Toggle("Erase brush", erase);
            if (layer == Layer.Terrain)
            {
                material = (TerrainMaterial)EditorGUILayout.EnumPopup("Material", material);
                blocked = EditorGUILayout.Toggle("Blocked cell", blocked);
                EditorGUILayout.HelpBox("Left drag paints the chosen terrain, right drag removes it. Erase brush makes left drag remove cells too. Empty cells are impassable; other layers remain saved.", MessageType.Info);
            }
            if (layer == Layer.Device)
            {
                if (map.scenario)
                {
                    DeviceSettings();
                }
                else
                {
                device = (DeviceKind)EditorGUILayout.EnumPopup("Device", device);
                direction = EditorGUILayout.Popup("Direction", direction, new[] { "North", "East", "South", "West" });
                if (device == DeviceKind.Portal) portalTarget = EditorGUILayout.Vector2IntField("Portal destination", portalTarget);
                }
            }
            if (layer == Layer.Decoration)
            { decoration = (Sprite)EditorGUILayout.ObjectField("Sprite", decoration, typeof(Sprite), false); decorationTint = EditorGUILayout.ColorField("Tint", decorationTint); }
            if (layer == Layer.Markers) marker = (Marker)EditorGUILayout.EnumPopup("Place", marker);
            if (layer == Layer.Water && !map.scenario) EditorGUILayout.HelpBox("Assign a device scenario to paint water cells.", MessageType.Info);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            var errors = map.ValidateMap();
            if (!map.theme) EditorGUILayout.HelpBox("Assign a puzzle theme.", MessageType.Error);
            foreach (var error in errors) EditorGUILayout.HelpBox(error, MessageType.Error);
            if (errors.Count == 0 && map.theme) EditorGUILayout.HelpBox("Ready to play. This validates placements, not puzzle solvability.", MessageType.Info);
            if (map.scenario && map.scenario.devices.Any(d => map.Contains(d.position) && !d.visualPrefab))
                EditorGUILayout.HelpBox("Some devices have no visual prefab. Their logic can run, but they will be invisible in preview.", MessageType.Warning);
            if (outside > 0)
            {
                EditorGUILayout.LabelField("Retained coordinates", EditorStyles.boldLabel);
                foreach (var p in map.terrain.Select(t => t.position).Concat(map.devices.Select(d => d.position)).Concat(map.decorations.Select(d => d.position)).Concat(map.bubbles).Concat(new[] { map.start, map.exit }).Where(p => !map.Contains(p)).Distinct().Take(40))
                    EditorGUILayout.LabelField(p.ToString());
            }
        }
        public sealed class DeviceTemplate
        {
            public PuzzleScenario source;
            public PrototypeDevice device;
            public string label;
        }
        public static DeviceTemplate[] LoadPracticeTemplates()
        {
            const string path = "Assets/ANIMOL/Data/Puzzle/Prototypes";
            return AssetDatabase.FindAssets("t:PuzzleScenario", new[] { path })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(assetPath => Path.GetFileName(assetPath).StartsWith("Device", StringComparison.Ordinal) &&
                    assetPath.EndsWith("_Scenario.asset", StringComparison.Ordinal))
                .OrderBy(assetPath => assetPath, StringComparer.Ordinal)
                .SelectMany(assetPath =>
                {
                    var source = AssetDatabase.LoadAssetAtPath<PuzzleScenario>(assetPath);
                    return source && source.devices != null
                        ? source.devices.Where(device => device != null).Select(device => new DeviceTemplate {
                            source = source, device = device,
                            label = Path.GetFileNameWithoutExtension(assetPath).Replace("_Scenario", "") + " / " +
                                device.role + (string.IsNullOrEmpty(device.variant) ? "" : " / " + device.variant) +
                                (device.visualPrefab ? "" : " [no art]")
                        })
                        : Enumerable.Empty<DeviceTemplate>();
                }).ToArray();
        }
        void RefreshLibrary() { library = LoadPracticeTemplates(); templateIndex = 0; Repaint(); }
        void DeviceSettings()
        {
            inspectDevice = GUILayout.Toggle(inspectDevice, "Select placed device (left click)");
            showConnections = GUILayout.Toggle(showConnections, "Show logic links and footprints");
            if (inspectDevice) EditorGUILayout.HelpBox("Click a device to inspect it. Turn selection off to stamp the chosen template. Right drag removes devices.", MessageType.Info);
            var source = GUILayout.Toolbar(useLibrary ? 0 : 1, new[] { "24-device library", "This map" });
            if (useLibrary != (source == 0)) { useLibrary = source == 0; templateIndex = 0; }
            var choices = useLibrary ? library.Select(entry => entry.device).ToArray() : map.scenario.devices.ToArray();
            var labels = useLibrary ? library.Select(entry => entry.label).ToArray() :
                choices.Select(device => device.id + " / " + device.role).ToArray();
            if (choices.Length == 0)
            {
                prototypeBrush = null;
                EditorGUILayout.HelpBox("No device templates in this source. Select the practice library or add a device to this map.", MessageType.Info);
            }
            else
            {
                templateIndex = EditorGUILayout.Popup("Stamp template", Mathf.Clamp(templateIndex, 0, choices.Length - 1), labels);
                prototypeBrush = choices[templateIndex];
                if (useLibrary && GUILayout.Button("Select template asset")) EditorGUIUtility.PingObject(library[templateIndex].source);
            }
            if (GUILayout.Button("Refresh device library")) RefreshLibrary();
            EditorGUILayout.HelpBox("Stamp copies the selected practice device with a new ID. Targets and rail cells move with it; group links and goals need review. Right click removes devices at a cell.", MessageType.Info);
            var selected = map.scenario.devices.FindIndex(device => device.id == selectedDeviceId);
            if (selected < 0) return;
            EditorGUILayout.LabelField("Selected placed device", EditorStyles.boldLabel);
            var placed = map.scenario.devices[selected];
            EditorGUILayout.LabelField(placed.id + " / " + placed.role);
            EditorGUILayout.LabelField("Facing");
            EditorGUILayout.BeginHorizontal();
            foreach (var heading in PuzzleAutotile.Cardinal)
                if (GUILayout.Button(DirectionGlyph(heading), GUILayout.Width(42)) && placed.direction != heading)
                {
                    Undo.RecordObject(map.scenario, "Turn puzzle device");
                    placed.direction = heading;
                    EditorUtility.SetDirty(map.scenario);
                }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Linked group", string.IsNullOrEmpty(placed.group) ? "None" : placed.group);
            if (!string.IsNullOrEmpty(placed.group))
                foreach (var peer in map.scenario.devices.Where(device => device != placed && device.group == placed.group))
                    EditorGUILayout.LabelField("  → " + peer.id + " / " + peer.role + " " + peer.position);
            if (GUILayout.Button(linkTargetMode ? "Cancel link" : "Connect to device...")) linkTargetMode = !linkTargetMode;
            if (linkTargetMode) EditorGUILayout.HelpBox("Click the receiving device on the grid. Both devices will share a logic group.", MessageType.Info);
            if (!string.IsNullOrEmpty(placed.group) && GUILayout.Button("Disconnect selected device"))
            {
                Undo.RecordObject(map.scenario, "Disconnect puzzle device");
                placed.group = string.Empty;
                EditorUtility.SetDirty(map.scenario);
            }
            if (placed.role == PrototypeRole.Portal) EditorGUILayout.LabelField("Portal destination", placed.target.ToString());
            if (placed.cells != null && placed.cells.Count > 0) EditorGUILayout.LabelField("Occupied / rail cells", placed.cells.Count.ToString());
            if (placed.role == PrototypeRole.Bomb) EditorGUILayout.LabelField("Blast offsets", (placed.blastOffsets?.Count ?? 0).ToString());
            var serialized = new SerializedObject(map.scenario); serialized.Update();
            EditorGUILayout.PropertyField(serialized.FindProperty("devices").GetArrayElementAtIndex(selected), true);
            serialized.ApplyModifiedProperties();
        }
        public static string ConnectDevices(PuzzleScenario scenario, PrototypeDevice source, PrototypeDevice target)
        {
            if (!scenario || source == null || target == null || source == target ||
                !scenario.devices.Contains(source) || !scenario.devices.Contains(target))
                throw new ArgumentException("Choose two distinct devices in the same scenario.");
            var group = !string.IsNullOrEmpty(source.group) ? source.group :
                !string.IsNullOrEmpty(target.group) ? target.group : "link_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            if (!string.IsNullOrEmpty(target.group) && target.group != group)
                foreach (var member in scenario.devices.Where(device => device.group == target.group)) member.group = group;
            source.group = group;
            target.group = group;
            return group;
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
            var retained = map.terrain.Select(t => t.position).Concat(map.devices.Select(d => d.position)).Concat(map.decorations.Select(d => d.position)).Concat(map.bubbles).Concat(new[] { map.start, map.exit }).Concat(map.scenario ? map.scenario.devices.Select(d => d.position).Concat(map.scenario.water).Concat(map.scenario.devices.SelectMany(d => d.cells)) : Enumerable.Empty<Vector2Int>()).ToArray();
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
                foreach (var decor in map.decorations.Where(a => a.position == p)) DrawSprite(rect, decor.sprite ? decor.sprite : map.theme ? map.theme.decoration : null);
                if (map.scenario && map.scenario.water.Contains(p)) GUI.Label(rect, "~");
                var d = map.DeviceAt(p); if (d.kind != DeviceKind.None) GUI.Label(rect, d.kind == DeviceKind.Portal ? "O" : d.kind.ToString().Substring(0, 1) + DirectionGlyph(d.direction));
                if (map.scenario) foreach (var configured in map.scenario.devices.Where(a => a.position == p)) DrawDevice(rect, configured);
                if (map.bubbles.Contains(p)) GUI.Label(rect, "B", EditorStyles.boldLabel);
                if (map.start == p) GUI.Label(rect, "S", EditorStyles.boldLabel);
                if (map.exit == p) GUI.Label(new Rect(rect.x + cellSize * .5f, rect.y, cellSize * .5f, cellSize), "E", EditorStyles.boldLabel);
                if (!map.Contains(p)) GUI.Label(rect, "/");
            }
            if (map.scenario && showConnections) DrawConnections(shownHeight);
            var e = Event.current;
            if (pointerInside && e.type == EventType.Repaint)
            {
                var hover = new Vector2Int(Mathf.FloorToInt(e.mousePosition.x / cellSize), shownHeight - 1 - Mathf.FloorToInt(e.mousePosition.y / cellSize));
                if (map.Contains(hover))
                {
                    var eraseHover = erase || e.button == 1;
                    Handles.DrawSolidRectangleWithOutline(new Rect(hover.x * cellSize, (shownHeight - hover.y - 1) * cellSize, cellSize, cellSize),
                        new Color(eraseHover ? 1f : .2f, eraseHover ? .25f : 1f, .3f, .12f),
                        eraseHover ? new Color(1f, .3f, .3f) : new Color(.3f, 1f, .5f));
                }
            }
            if (pointerInside && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && (e.button == 0 || e.button == 1))
            {
                var p = new Vector2Int(Mathf.FloorToInt(e.mousePosition.x / cellSize), shownHeight - 1 - Mathf.FloorToInt(e.mousePosition.y / cellSize));
                if (map.Contains(p))
                {
                    if (layer == Layer.Device && map.scenario && inspectDevice && e.button == 0)
                    {
                        var clicked = map.scenario.devices.FirstOrDefault(device => device.position == p);
                        if (linkTargetMode && clicked != null)
                        {
                            var source = map.scenario.devices.FirstOrDefault(device => device.id == selectedDeviceId);
                            if (source != null && source != clicked)
                            {
                                Undo.RecordObject(map.scenario, "Connect puzzle devices");
                                ConnectDevices(map.scenario, source, clicked);
                                EditorUtility.SetDirty(map.scenario);
                                linkTargetMode = false;
                            }
                        }
                        else if (e.type == EventType.MouseDown) selectedDeviceId = clicked?.id;
                    }
                    else
                    {
                        if (!stroke) { Undo.IncrementCurrentGroup(); undoGroup = Undo.GetCurrentGroup(); Undo.RegisterCompleteObjectUndo(map, "Paint puzzle layer"); stroke = true; }
                        if (lastPaint != p) { Paint(p, erase || e.button == 1); lastPaint = p; }
                    }
                    e.Use(); Repaint();
                }
            }
            GUI.EndScrollView();
        }
        static string DirectionGlyph(Vector2Int d) => d == Vector2Int.up ? "^" : d == Vector2Int.right ? ">" : d == Vector2Int.down ? "v" : "<";
        void DrawDevice(Rect cell, PrototypeDevice device)
        {
            var inset = cell.width * .12f;
            var visual = new Rect(cell.x + inset, cell.y + inset, cell.width - inset * 2, cell.height - inset * 2);
            var prefab = device.visualPrefab;
            var sprite = DeviceSprite(device);
            if (sprite) DrawSprite(visual, sprite);
            else if (prefab)
            {
                var preview = AssetPreview.GetAssetPreview(prefab) ?? AssetPreview.GetMiniThumbnail(prefab);
                if (preview) GUI.DrawTexture(visual, preview, ScaleMode.ScaleToFit, true);
            }
            var selected = device.id == selectedDeviceId;
            var strip = new Rect(cell.x, cell.y + cell.height * .72f, cell.width, cell.height * .28f);
            EditorGUI.DrawRect(strip, selected ? new Color(.1f, .55f, .9f, .9f) : new Color(.04f, .07f, .12f, .85f));
            GUI.Label(strip, ((int)device.role).ToString("00") + " " + DirectionGlyph(device.direction), EditorStyles.whiteMiniLabel);
            if (selected) Handles.DrawSolidRectangleWithOutline(cell, Color.clear, new Color(.2f, .8f, 1f));
            if (!prefab) GUI.Label(new Rect(cell.x, cell.y, cell.width, cell.height * .3f), "no art", EditorStyles.whiteMiniLabel);
        }
        static Sprite DeviceSprite(PrototypeDevice device)
        {
            var prefab = device.visualPrefab;
            if (!prefab) return null;
            var view = prefab.GetComponentInChildren<ANIMOL.Presentation.DeviceVisualView>(true);
            var state = view?.states?.FirstOrDefault(candidate => candidate.name == device.initialVisual) ??
                view?.states?.FirstOrDefault(candidate => candidate.name == view.initialState);
            if (state?.sprite) return state.sprite;
            if (state?.sequence && state.sequence.frames != null && state.sequence.frames.Length > 0 && state.sequence.frames[0])
                return state.sequence.frames[0];
            return prefab.GetComponentsInChildren<SpriteRenderer>(true).Select(renderer => renderer.sprite).FirstOrDefault(candidate => candidate);
        }
        void DrawConnections(int shownHeight)
        {
            var devices = map.scenario.devices.Where(device => map.Contains(device.position)).ToArray();
            Vector3 Center(Vector2Int p) => new Vector3((p.x + .5f) * cellSize, (shownHeight - p.y - .5f) * cellSize);
            var previous = Handles.color;
            foreach (var group in devices.Where(device => !string.IsNullOrEmpty(device.group)).GroupBy(device => device.group))
            {
                var members = group.ToArray();
                for (var i = 0; i < members.Length; i++) for (var j = i + 1; j < members.Length; j++)
                {
                    Handles.color = members[i].id == selectedDeviceId || members[j].id == selectedDeviceId
                        ? new Color(.2f, .9f, 1f, .95f) : new Color(.3f, .8f, 1f, .55f);
                    Handles.DrawAAPolyLine(3, Center(members[i].position), Center(members[j].position));
                }
            }
            foreach (var device in devices)
            {
                if (device.role == PrototypeRole.Portal && map.Contains(device.target))
                {
                    Handles.color = new Color(1f, .55f, 1f, device.id == selectedDeviceId ? 1f : .55f);
                    Handles.DrawDottedLine(Center(device.position), Center(device.target), 4);
                }
                if (device.id != selectedDeviceId) continue;
                Handles.color = new Color(1f, .72f, .22f, .95f);
                foreach (var cell in device.cells ?? Enumerable.Empty<Vector2Int>())
                    if (map.Contains(cell)) Handles.DrawDottedLine(Center(device.position), Center(cell), 5);
                if (device.role == PrototypeRole.Bomb)
                    foreach (var offset in device.blastOffsets ?? Enumerable.Empty<Vector2Int>())
                        if (map.Contains(device.position + offset))
                            Handles.DrawSolidRectangleWithOutline(new Rect((device.position.x + offset.x) * cellSize,
                                (shownHeight - device.position.y - offset.y - 1) * cellSize, cellSize, cellSize),
                                new Color(1f, .25f, .1f, .16f), new Color(1f, .35f, .1f, .9f));
            }
            Handles.color = previous;
        }
        static void DrawSprite(Rect r, Sprite sprite)
        { if (!sprite) return; var t = sprite.texture; var s = sprite.rect; GUI.DrawTextureWithTexCoords(r, t, new Rect(s.x / t.width, s.y / t.height, s.width / t.width, s.height / t.height)); }
        void Paint(Vector2Int p, bool remove)
        {
            if (layer == Layer.Terrain) { if (remove) map.RemoveTerrain(p); else map.SetTerrain(p, material, blocked); }
            if (layer == Layer.Device)
            {
                if (map.scenario)
                {
                    Undo.RecordObject(map.scenario, "Paint themed device");
                    if (remove) map.scenario.devices.RemoveAll(d => d.position == p);
                    else if (prototypeBrush != null && !map.scenario.devices.Any(d => d.position == p && d.role == prototypeBrush.role && d.variant == prototypeBrush.variant))
                        selectedDeviceId = StampTemplate(map.scenario, prototypeBrush, p).id;
                    EditorUtility.SetDirty(map.scenario);
                }
                else map.SetDevice(new DeviceCell { position = p, kind = remove ? DeviceKind.None : device, direction = PuzzleAutotile.Cardinal[direction], portalTarget = portalTarget });
            }
            if (layer == Layer.Decoration) { map.decorations.RemoveAll(d => d.position == p); if (!remove) map.decorations.Add(new DecorationCell { position = p, sprite = decoration, tint = decorationTint }); }
            if (layer == Layer.Markers)
            {
                if (marker == Marker.Bubble) { map.bubbles.RemoveAll(b => b == p); if (!remove) map.bubbles.Add(p); }
                else if (!remove) { if (marker == Marker.Start) map.start = p; else map.exit = p; }
            }
            if (layer == Layer.Water && map.scenario)
            {
                Undo.RecordObject(map.scenario, "Paint water cell");
                if (remove) map.scenario.water.RemoveAll(cell => cell == p);
                else if (!map.scenario.water.Contains(p)) map.scenario.water.Add(p);
                EditorUtility.SetDirty(map.scenario);
            }
            EditorUtility.SetDirty(map); map.InvalidateIndex();
        }
        public static PrototypeDevice StampTemplate(PuzzleScenario destination, PrototypeDevice template, Vector2Int position)
        {
            if (!destination || template == null) throw new ArgumentException("A destination scenario and template are required.");
            var stem = template.role.ToString().ToLowerInvariant();
            var number = 1;
            while (destination.devices.Any(device => device.id == stem + "_" + number)) number++;
            var shift = position - template.position;
            var copy = new PrototypeDevice {
                id = stem + "_" + number, role = template.role, variant = template.variant,
                // Practice examples reuse group names. New placements must be linked deliberately.
                group = string.Empty, artTheme = template.artTheme, visualPrefab = template.visualPrefab,
                initialVisual = template.initialVisual, position = position, direction = template.direction,
                target = template.role == PrototypeRole.Portal ? template.target + shift : template.target,
                orientation = template.orientation, number = template.number, active = template.active,
                cells = (template.cells ?? new List<Vector2Int>()).Select(cell => cell + shift).ToList(),
                blastOffsets = (template.blastOffsets ?? new List<Vector2Int>()).ToList()
            };
            destination.devices.Add(copy);
            return copy;
        }
        void EndStroke() { if (stroke) Undo.CollapseUndoOperations(undoGroup); stroke = false; lastPaint = new Vector2Int(-999, -999); }
        void Create()
        {
            if (!newMapTheme) { EditorUtility.DisplayDialog("Choose a theme", "Assign a theme for the new map.", "OK"); return; }
            var path = EditorUtility.SaveFilePanelInProject("New playable puzzle map", "PuzzleMap", "asset",
                "A paired device scenario will be created beside the map.", "Assets/ANIMOL/Data/Puzzle/Maps");
            if (path.Length == 0) return;
            try { Select(CreateStarterMap(path, newMapTheme, newMapWidth, newMapHeight)); }
            catch (Exception error) { EditorUtility.DisplayDialog("Cannot create map", error.Message, "OK"); }
        }
        void NewMapSettings()
        {
            newMapTheme = (PuzzleTheme)EditorGUILayout.ObjectField("Theme", newMapTheme, typeof(PuzzleTheme), false);
            newMapWidth = EditorGUILayout.IntField("Width", newMapWidth);
            newMapHeight = EditorGUILayout.IntField("Height", newMapHeight);
            EditorGUILayout.HelpBox("Starter maps use walkable grass, one bubble, a start and exit, and an empty scenario with the current prototype policy defaults.", MessageType.Info);
        }
        public static PuzzleMap CreateStarterMap(string path, PuzzleTheme theme, int width, int height)
        {
            if (!theme) throw new ArgumentException("A puzzle theme is required.");
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) ||
                !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Choose a project asset path ending in .asset.");
            var scenarioPath = path.Substring(0, path.Length - ".asset".Length) + "_Scenario.asset";
            if (AssetDatabase.LoadMainAssetAtPath(path) || AssetDatabase.LoadMainAssetAtPath(scenarioPath))
                throw new InvalidOperationException("The map or paired scenario path is already in use.");
            width = Mathf.Clamp(width, 4, 256); height = Mathf.Clamp(height, 4, 256);
            var scenario = CreateInstance<PuzzleScenario>();
            scenario.title = Path.GetFileNameWithoutExtension(path);
            scenario.themeId = theme.name;
            var data = CreateInstance<PuzzleMap>();
            data.Resize(width, height);
            data.theme = theme;
            data.scenario = scenario;
            data.start = new Vector2Int(1, 1);
            data.exit = new Vector2Int(width - 2, height - 2);
            data.targetBubbles = 1;
            data.bubbles.Add(new Vector2Int(width / 2, height / 2));
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
                data.terrain.Add(new TerrainCell(new Vector2Int(x, y), TerrainMaterial.Grass));
            data.InvalidateIndex();
            try
            {
                AssetDatabase.CreateAsset(scenario, scenarioPath);
                AssetDatabase.CreateAsset(data, path);
            }
            catch
            {
                if (AssetDatabase.LoadAssetAtPath<PuzzleMap>(path) == data) AssetDatabase.DeleteAsset(path);
                if (AssetDatabase.LoadAssetAtPath<PuzzleScenario>(scenarioPath) == scenario) AssetDatabase.DeleteAsset(scenarioPath);
                throw;
            }
            EditorGUIUtility.PingObject(data);
            return data;
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
