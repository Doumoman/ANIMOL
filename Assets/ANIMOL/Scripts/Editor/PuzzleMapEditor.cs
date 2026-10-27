using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Art.EchoTheaterV03;
using ANIMOL.Art.StarSwellCoastV01;
using ANIMOL.Art.SunsetSanctumV02;
using ANIMOL.Art.RanchConnectedDecorV02;
using ANIMOL.Art.TheaterConnectedDecorV01;
using ANIMOL.Art.SanctumConnectedDecorV01;
using ANIMOL.Art.CoastConnectedDecorV01;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Presentation.Editor
{
    public sealed class PuzzleMapEditor : EditorWindow
    {
        static readonly string[] waterEdgeKeys = { "water/bank/edge_n", "water/bank/edge_e",
            "water/bank/edge_s", "water/bank/edge_w" };
        static readonly string[] waterOuterKeys = { "water/bank/outer_ne", "water/bank/outer_se",
            "water/bank/outer_sw", "water/bank/outer_nw" };
        static readonly string[] waterInnerKeys = { "water/bank/inner_ne", "water/bank/inner_se",
            "water/bank/inner_sw", "water/bank/inner_nw" };
        static readonly int[] waterEdgeBits = { 1, 4, 16, 64 };
        static readonly int[] waterDiagonalBits = { 2, 8, 32, 128 };
        static readonly string[] v13GalleryNames = { "둥지", "키 큰 풀", "수로" };
        static readonly string[] v13GalleryKeys = { "nest/idle", "tall_grass/idle", "water/wet" };
        enum Layer { Terrain, Device, Decoration, Markers, Water }
        enum Marker { Start, Bubble, Exit }
        [SerializeField] PuzzleMap map;
        [SerializeField] PuzzleTheme newMapTheme;
        [SerializeField] string newMapName = "NewMap";
        [SerializeField] int newMapWidth = 12, newMapHeight = 18;
        Layer layer;
        Marker marker;
        TerrainMaterial material;
        PrototypeDevice prototypeBrush;
        string ranchArtBrush, ranchArtError;
        RanchV13ArtPlacement selectedRanchArt;
        DeviceTemplate[] library = Array.Empty<DeviceTemplate>();
        readonly Dictionary<string, Sprite> legacyDeviceSprites = new Dictionary<string, Sprite>();
        bool useLibrary = true, inspectDevice, showConnections = true, linkTargetMode, nestTargetMode, scenarioCreationQueued;
        bool showV13Art = true;
        int templateIndex;
        string selectedDeviceId;
        string scenarioCreationError;
        bool showScenario;
        Sprite decoration;
        Color decorationTint = Color.white;
        string decorationConnectionGroup = "";
        int decorationLogicalLayer, decorationHeightKey;
        bool inspectDecoration;
        Vector2Int selectedDecorationCell;
        string decorationError;
        bool blocked, erase, stroke;
        int pendingWidth, pendingHeight, undoGroup;
        Vector2Int cameraPreviewCell;
        bool showCameraBounds = true;
        float cellSize = 42;
        Vector2 gridScroll, settingsScroll, deviceListScroll, decorationListScroll;
        Vector2Int lastPaint = new Vector2Int(-999, -999);
        PuzzleMap previewMap;
        PuzzleTheme previewTheme;
        PuzzleScenario previewScenario;
        int previewMapDirtyCount, previewScenarioDirtyCount, previewThemeDirtyCount;
        bool previewCacheValid;
        RanchDecorPlan ranchPreview;
        TheaterDecorPlan theaterPreview;
        SanctumDecorPlan sanctumPreview;
        CoastDecorPlan coastPreview;
        PuzzleTheme decorationChoicesTheme;
        DecorationChoice[] cachedDecorationChoices;
        PuzzleMap validatedMap;
        PuzzleScenario validatedScenario;
        PuzzleTheme validatedTheme;
        int validatedMapDirtyCount, validatedScenarioDirtyCount, validatedThemeDirtyCount, cachedOutsideCount;
        List<string> cachedValidationErrors;
        [MenuItem("ANIMOL/퍼즐/맵 제작기")]
        public static void Open() => GetWindow<PuzzleMapEditor>("퍼즐 맵 제작기");
        public static void Edit(PuzzleMap data) { var window = GetWindow<PuzzleMapEditor>("퍼즐 맵 제작기"); window.Select(data); }
        void OnEnable()
        {
            Undo.undoRedoPerformed += Undone;
            EditorApplication.projectChanged += RefreshLibrary;
            RefreshLibrary();
            if (!newMapTheme) newMapTheme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>("Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset");
            if (map) { pendingWidth = map.Width; pendingHeight = map.Height; cameraPreviewCell = map.start; }
        }
        void OnDisable() { Undo.undoRedoPerformed -= Undone; EditorApplication.projectChanged -= RefreshLibrary; EndStroke(); }
        void Undone() { if (map) { map.InvalidateIndex(); map.NotifyDecorationsChanged(); pendingWidth = map.Width; pendingHeight = map.Height; } previewCacheValid = false; cachedValidationErrors = null; legacyDeviceSprites.Clear(); RanchDevicesV13EditorPreview.ClearCache(); Repaint(); }
        void Select(PuzzleMap data)
        {
            EndStroke(); map = data; selectedDeviceId = null; selectedRanchArt = null; ranchArtBrush = ranchArtError = null;
            linkTargetMode = nestTargetMode = false; scenarioCreationError = decorationError = null;
            if (map) { pendingWidth = map.Width; pendingHeight = map.Height; cameraPreviewCell = map.start; if (map.theme) newMapTheme = map.theme; }
            Repaint();
        }
        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var selected = (PuzzleMap)EditorGUILayout.ObjectField(map, typeof(PuzzleMap), false, GUILayout.Width(250));
            if (selected != map) Select(selected);
            if (GUILayout.Button("새 맵", EditorStyles.toolbarButton)) Select(null);
            if (GUILayout.Button("실행 취소", EditorStyles.toolbarButton)) Undo.PerformUndo();
            if (GUILayout.Button("다시 실행", EditorStyles.toolbarButton)) Undo.PerformRedo();
            if (GUILayout.Button("저장", EditorStyles.toolbarButton)) AssetDatabase.SaveAssets();
            using (new EditorGUI.DisabledScope(!map || EditorApplication.isPlaying))
                if (GUILayout.Button("플레이 미리보기", EditorStyles.toolbarButton)) PuzzlePreview.Play(map);
            EditorGUILayout.EndHorizontal();
            if (!map)
            {
                EditorGUILayout.HelpBox("기존 맵을 선택하거나 새 맵을 만드세요. 왼쪽 드래그로 배치하고 오른쪽 드래그로 지웁니다.", MessageType.Info);
                NewMapSettings();
                if (GUILayout.Button("기본 맵 만들기")) Create();
                return;
            }
            EditorGUILayout.BeginHorizontal();
            settingsScroll = EditorGUILayout.BeginScrollView(settingsScroll, GUILayout.Width(320));
            Settings(); EditorGUILayout.EndScrollView();
            EditorGUILayout.BeginVertical();
            cellSize = EditorGUILayout.Slider("격자 확대", cellSize, 20, 80);
            EditorGUILayout.LabelField("왼쪽: 배치/선택    오른쪽: 지우기");
            var viewport = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawGrid(viewport);
            EditorGUILayout.EndVertical(); EditorGUILayout.EndHorizontal();
            if (Event.current.rawType == EventType.MouseUp) EndStroke();
        }
        void Settings()
        {
            RefreshValidation();
            EditorGUILayout.LabelField("맵 설정", EditorStyles.boldLabel);
            pendingWidth = EditorGUILayout.IntField("가로 칸 수", pendingWidth);
            pendingHeight = EditorGUILayout.IntField("세로 칸 수", pendingHeight);
            if (GUILayout.Button("크기 적용 (잘린 배치 보관)") && Resize(map, pendingWidth, pendingHeight, true)) { pendingWidth = map.Width; pendingHeight = map.Height; }
            EditorGUILayout.HelpBox("가로·세로 칸 수를 바꿔 크기 적용을 누르세요. 넓힌 칸은 풀 지형으로 채워지며, 줄여서 밖으로 나간 배치는 보관됩니다.", MessageType.Info);
            var outside = cachedOutsideCount;
            if (outside > 0) EditorGUILayout.HelpBox("맵 밖 배치 " + outside + "개가 비활성 상태로 보관됩니다. 맵을 넓히면 복구됩니다. 시작점·출구가 밖에 있으면 미리보기를 실행할 수 없습니다.", MessageType.Warning);
            var previousTheme = map.theme;
            var serialized = new SerializedObject(map); serialized.Update();
            EditorGUILayout.PropertyField(serialized.FindProperty("theme"), new GUIContent("테마"));
            EditorGUILayout.PropertyField(serialized.FindProperty("targetBubbles"), new GUIContent("목표 방울 수"));
            DrawCameraSettings(serialized.FindProperty("cameraSettings"));
            showCameraBounds = EditorGUILayout.Toggle("카메라 테두리 표시", showCameraBounds);
            if (showCameraBounds)
            {
                cameraPreviewCell = EditorGUILayout.Vector2IntField("카메라 초점 칸", cameraPreviewCell);
                if (GUILayout.Button("시작점으로 초점 이동")) cameraPreviewCell = map.start;
                EditorGUILayout.LabelField("540×960 화면 기준", EditorStyles.miniLabel);
            }
            if (serialized.ApplyModifiedProperties()) map.InvalidateIndex();
            if (map.theme != previousTheme)
            {
                scenarioCreationError = null;
                var themeId = ThemeId(map.theme);
                if (map.scenario && !string.IsNullOrEmpty(themeId) && map.scenario.themeId != themeId)
                {
                    Undo.RecordObject(map.scenario, "맵 테마 변경");
                    map.scenario.themeId = themeId;
                    EditorUtility.SetDirty(map.scenario);
                }
            }
            if (map.scenario)
            {
                showScenario = EditorGUILayout.Foldout(showScenario, "장치·물·목표 설정", true);
                if (showScenario)
                {
                    var scenario = new SerializedObject(map.scenario); scenario.Update();
                    EditorGUILayout.PropertyField(scenario.FindProperty("costLimit"), new GUIContent("비용 제한 (0: 무제한)"));
                    EditorGUILayout.PropertyField(scenario.FindProperty("beamPrefab"), new GUIContent("광선 프리팹"));
                    DrawScenarioDevices(scenario.FindProperty("devices"));
                    DrawVectorArray(scenario.FindProperty("water"), ThemeId(map.theme) == "starway" ?
                        "고정 물 칸 (A/B 전환 아님)" : "물 칸");
                    DrawGoals(scenario.FindProperty("goals"));
                    EditorGUILayout.PropertyField(scenario.FindProperty("verificationRoute"), new GUIContent("검수 경로"));
                    EditorGUILayout.LabelField("장치 판정 설정", EditorStyles.boldLabel);
                    var policyFields = new[] { "holdOccupiedBridge", "shutterPhaseAdvancesWhenOccupied", "leapSkipsIntermediate",
                        "mirrorBumpRotates", "magnetSwitchLatches", "bombHurtsPlayer", "bombDestroysOrdinaryObjects", "statueStopsAfterBreakingWall" };
                    var policyLabels = new[] { "점유된 다리 유지", "점유 중 셔터 위상 진행", "도약 중간 칸 건너뛰기",
                        "거울 충돌 시 회전", "자석 스위치 켜짐 유지", "폭탄이 플레이어에게 피해", "폭탄이 일반 물체 파괴", "석상이 벽 파괴 칸에서 정지" };
                    for (var i = 0; i < policyFields.Length; i++)
                        EditorGUILayout.PropertyField(scenario.FindProperty(policyFields[i]), new GUIContent(policyLabels[i]));
                    scenario.ApplyModifiedProperties();
                }
            }
            EditorGUILayout.Space();
            layer = (Layer)GUILayout.Toolbar((int)layer, new[] { "지형", "장치", "장식", "표식", "물" });
            erase = EditorGUILayout.Toggle("지우개", erase);
            if (layer == Layer.Terrain)
            {
                material = (TerrainMaterial)EditorGUILayout.IntPopup("지형 재질", (int)material, new[] { "풀", "돌", "모래" }, new[] { 0, 1, 2 });
                blocked = EditorGUILayout.Toggle("통행 금지 칸", blocked);
                EditorGUILayout.HelpBox("왼쪽 드래그로 지형을 칠하고 오른쪽 드래그로 지웁니다. 지우개를 켜면 왼쪽 드래그도 지웁니다. 빈 칸은 지나갈 수 없으며 다른 레이어는 보존됩니다.", MessageType.Info);
            }
            if (layer == Layer.Device)
            {
                if (map.scenario)
                {
                    DeviceSettings();
                }
                else
                {
                    QueueScenarioCreation();
                    EditorGUILayout.HelpBox(scenarioCreationError ?? (string.IsNullOrEmpty(ThemeId(map.theme)) ?
                        "장치를 배치하려면 테마를 선택하세요." : "장치 데이터를 자동으로 준비하는 중입니다."),
                        scenarioCreationError == null ? MessageType.Info : MessageType.Error);
                }
            }
            if (layer == Layer.Decoration) DecorationSettings();
            if (layer == Layer.Markers) marker = (Marker)EditorGUILayout.IntPopup("배치할 표식", (int)marker, new[] { "시작점", "방울", "출구" }, new[] { 0, 1, 2 });
            if (layer == Layer.Water && !map.scenario)
            {
                QueueScenarioCreation();
                EditorGUILayout.HelpBox(scenarioCreationError ?? (string.IsNullOrEmpty(ThemeId(map.theme)) ?
                    "물 칸을 배치하려면 테마를 선택하세요." : "물 칸 데이터를 자동으로 준비하는 중입니다."),
                    scenarioCreationError == null ? MessageType.Info : MessageType.Error);
            }
            if (layer == Layer.Water && ThemeId(map.theme) == "starway" && map.scenario)
                EditorGUILayout.HelpBox("V13 수로 그림으로 표시됩니다. 현재 물 칸은 고정 데이터이며 수문 A/B 전환 판정은 아직 연결되지 않았습니다.", MessageType.Info);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("배치 검사", EditorStyles.boldLabel);
            RefreshValidation();
            var errors = cachedValidationErrors;
            if (!map.theme) EditorGUILayout.HelpBox("퍼즐 테마를 지정하세요.", MessageType.Error);
            foreach (var error in errors) EditorGUILayout.HelpBox(TranslateValidation(error), MessageType.Error);
            if (errors.Count == 0 && map.theme) EditorGUILayout.HelpBox("배치 검사를 통과했습니다. 퍼즐이 실제로 풀리는지는 플레이 미리보기로 확인하세요.", MessageType.Info);
            if (map.scenario && map.scenario.devices.Any(d => map.Contains(d.position) && !d.visualPrefab))
                EditorGUILayout.HelpBox("일부 장치에는 그림 프리팹이 없습니다. 논리는 작동해도 미리보기에서는 보이지 않습니다.", MessageType.Warning);
            if (outside > 0)
            {
                EditorGUILayout.LabelField("맵 밖에 보관된 좌표", EditorStyles.boldLabel);
                foreach (var p in map.terrain.Select(t => t.position).Concat(map.devices.Select(d => d.position)).Concat(map.decorations.Select(d => d.position)).Concat(map.bubbles).Concat(new[] { map.start, map.exit }).Where(p => !map.Contains(p)).Distinct().Take(40))
                    EditorGUILayout.LabelField(p.ToString());
            }
        }
        void RefreshValidation()
        {
            var mapDirtyCount = EditorUtility.GetDirtyCount(map);
            var scenarioDirtyCount = map.scenario ? EditorUtility.GetDirtyCount(map.scenario) : 0;
            var themeDirtyCount = map.theme ? EditorUtility.GetDirtyCount(map.theme) : 0;
            if (cachedValidationErrors != null && validatedMap == map && validatedScenario == map.scenario &&
                validatedTheme == map.theme && validatedMapDirtyCount == mapDirtyCount &&
                validatedScenarioDirtyCount == scenarioDirtyCount && validatedThemeDirtyCount == themeDirtyCount) return;
            validatedMap = map; validatedScenario = map.scenario; validatedTheme = map.theme;
            validatedMapDirtyCount = mapDirtyCount; validatedScenarioDirtyCount = scenarioDirtyCount;
            validatedThemeDirtyCount = themeDirtyCount;
            cachedOutsideCount = map.OutsideCount(map.Width, map.Height);
            cachedValidationErrors = map.ValidateMap();
        }
        static void DrawCameraSettings(SerializedProperty camera)
        {
            camera.isExpanded = EditorGUILayout.Foldout(camera.isExpanded, "카메라 설정", true);
            if (!camera.isExpanded) return;
            var fields = new[] { "visibleRows", "followSharpness", "topInset", "bottomInset", "farParallax", "midParallax", "nearParallax" };
            var labels = new[] { "화면에 보이는 세로 칸", "추적 속도", "위쪽 여백", "아래쪽 여백", "먼 배경 시차", "중간 배경 시차", "가까운 배경 시차" };
            for (var i = 0; i < fields.Length; i++) EditorGUILayout.PropertyField(camera.FindPropertyRelative(fields[i]), new GUIContent(labels[i]));
        }
        static void DrawVectorArray(SerializedProperty array, string label)
        {
            array.isExpanded = EditorGUILayout.Foldout(array.isExpanded, label + " (" + array.arraySize + ")", true);
            if (!array.isExpanded) return;
            array.arraySize = Mathf.Max(0, EditorGUILayout.IntField("칸 개수", array.arraySize));
            for (var i = 0; i < array.arraySize; i++)
                EditorGUILayout.PropertyField(array.GetArrayElementAtIndex(i), new GUIContent("칸 " + (i + 1)));
        }
        static void DrawGoals(SerializedProperty goals)
        {
            goals.isExpanded = EditorGUILayout.Foldout(goals.isExpanded, "추가 목표 (" + goals.arraySize + ")", true);
            if (!goals.isExpanded) return;
            goals.arraySize = Mathf.Max(0, EditorGUILayout.IntField("목표 개수", goals.arraySize));
            for (var i = 0; i < goals.arraySize; i++)
            {
                var goal = goals.GetArrayElementAtIndex(i);
                goal.isExpanded = EditorGUILayout.Foldout(goal.isExpanded, "목표 " + (i + 1), true);
                if (!goal.isExpanded) continue;
                EditorGUILayout.PropertyField(goal.FindPropertyRelative("deviceId"), new GUIContent("장치 ID"));
                var kind = goal.FindPropertyRelative("kind");
                kind.enumValueIndex = EditorGUILayout.Popup("조건", kind.enumValueIndex, new[] { "활성화", "제거", "위치", "주사위 윗면", "방문" });
                EditorGUILayout.PropertyField(goal.FindPropertyRelative("cell"), new GUIContent("목표 칸"));
                EditorGUILayout.PropertyField(goal.FindPropertyRelative("number"), new GUIContent("목표 숫자"));
            }
        }
        static void DrawScenarioDevices(SerializedProperty devices)
        {
            devices.isExpanded = EditorGUILayout.Foldout(devices.isExpanded, "배치된 장치 (" + devices.arraySize + ")", true);
            if (!devices.isExpanded) return;
            for (var i = 0; i < devices.arraySize; i++)
            {
                var device = devices.GetArrayElementAtIndex(i);
                device.isExpanded = EditorGUILayout.Foldout(device.isExpanded, "장치 " + (i + 1) + " · " + device.FindPropertyRelative("id").stringValue, true);
                if (device.isExpanded) DrawDeviceProperties(device);
            }
        }
        static void DrawDeviceProperties(SerializedProperty device)
        {
            var role = device.FindPropertyRelative("role");
            var roles = Enum.GetValues(typeof(PrototypeRole)).Cast<PrototypeRole>().ToArray();
            role.intValue = EditorGUILayout.IntPopup("장치 종류", role.intValue,
                roles.Select(value => DeviceName(value)).ToArray(), roles.Select(value => (int)value).ToArray());
            var fields = new[] { "id", "variant", "group", "artTheme", "visualPrefab", "initialVisual", "position", "direction", "target", "orientation", "number", "active" };
            var labels = new[] { "장치 ID", "구성 종류", "연결 그룹", "그림 테마 ID", "장치 그림 프리팹", "첫 그림 상태", "배치 좌표", "방향 벡터", "목적지", "회전 값", "숫자", "처음 활성화" };
            for (var i = 0; i < fields.Length; i++) EditorGUILayout.PropertyField(device.FindPropertyRelative(fields[i]), new GUIContent(labels[i]));
            DrawVectorArray(device.FindPropertyRelative("cells"), "점유·선로 칸");
            DrawVectorArray(device.FindPropertyRelative("blastOffsets"), "폭발 상대 좌표");
        }
        public static string TranslateValidation(string error)
        {
            var exact = new Dictionary<string, string> {
                { "Start must be on an in-bounds walkable tile.", "시작점은 맵 안의 통행 가능한 지형에 놓아야 합니다." },
                { "Exit must be on an in-bounds walkable tile.", "출구는 맵 안의 통행 가능한 지형에 놓아야 합니다." },
                { "Not enough in-bounds walkable bubble placements for the target.", "목표 수만큼 방울을 통행 가능한 칸에 놓으세요." },
                { "Every prototype device needs an ID.", "모든 장치에 ID가 필요합니다." },
                { "Prototype device IDs must be unique.", "장치 ID가 중복되었습니다." },
                { "Goal references missing device.", "추가 목표가 없는 장치를 참조합니다." },
                { "A required goal device is outside map bounds; expand or reposition it before playing.", "목표 장치가 맵 밖에 있습니다. 맵을 넓히거나 장치를 옮기세요." }
            };
            if (exact.TryGetValue(error, out var translated)) return translated;
            var prefixes = new[] { "Bubble has no walkable terrain: ", "Device has no walkable terrain: ", "Portal destination is outside or blocked: ",
                "Device direction must be cardinal: ", "Invalid direction: ", "Author explicit bomb footprint offsets: ",
                "Portal destination outside map: ", "Moving stage needs a rail: ",
                "Socket bridge target is missing or outside map: ", "Socket bubble needs walkable terrain: ",
                "Socket bubble is already visible: " };
            var replacements = new[] { "방울 칸에 통행 가능한 지형이 없습니다: ", "장치 칸에 통행 가능한 지형이 없습니다: ", "포털 목적지가 맵 밖이거나 막혔습니다: ",
                "장치 방향은 상하좌우여야 합니다: ", "방향이 올바르지 않습니다: ", "폭탄의 폭발 범위를 지정하세요: ",
                "포털 목적지가 맵 밖입니다: ", "이동 발판의 선로가 필요합니다: ",
                "소켓이 펼칠 덩굴 다리를 선택하세요: ", "소켓 방울 칸에 통행 가능한 지형이 필요합니다: ",
                "소켓 방울이 이미 일반 방울로 배치되어 있습니다: " };
            for (var i = 0; i < prefixes.Length; i++) if (error.StartsWith(prefixes[i], StringComparison.Ordinal)) return replacements[i] + error.Substring(prefixes[i].Length);
            return error;
        }
        public sealed class DeviceTemplate
        {
            public PuzzleScenario source;
            public PrototypeDevice device;
            public string label;
        }
        public static string ThemeId(PuzzleTheme theme)
        {
            if (!theme) return string.Empty;
            var name = theme.name;
            if (name == "StarGarden" || name == "starway_Theme" || theme.starwayRanch || theme.skyIsland) return "starway";
            if (name == "memory_theater_Theme" || theme.memorySkyTheater) return "memory_theater";
            if (name == "letter_observatory_Theme" || theme.clockDome || theme.sunsetSanctumV02Catalog) return "letter_observatory";
            if (name == "twilight_sea_Theme" || theme.twilightCoast || theme.starSwellCoastV01Catalog) return "twilight_sea";
            return string.Empty;
        }
        public static PuzzleScenario EnsureScenario(PuzzleMap data)
        {
            if (!data) throw new ArgumentException("맵을 먼저 선택하세요.");
            if (data.scenario) return data.scenario;
            var themeId = ThemeId(data.theme);
            if (string.IsNullOrEmpty(themeId)) throw new ArgumentException("지원되는 테마를 먼저 선택하세요.");
            var mapPath = AssetDatabase.GetAssetPath(data);
            if (string.IsNullOrEmpty(mapPath)) throw new ArgumentException("맵을 에셋으로 저장한 뒤 장치를 배치하세요.");
            var scenarioPath = Path.ChangeExtension(mapPath, null) + "_Scenario.asset";
            var scenario = AssetDatabase.LoadAssetAtPath<PuzzleScenario>(scenarioPath);
            if (!scenario)
            {
                if (AssetDatabase.LoadMainAssetAtPath(scenarioPath))
                    scenarioPath = AssetDatabase.GenerateUniqueAssetPath(scenarioPath);
                scenario = CreateInstance<PuzzleScenario>();
                scenario.title = data.name;
                scenario.themeId = themeId;
                AssetDatabase.CreateAsset(scenario, scenarioPath);
            }
            data.scenario = scenario;
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssetIfDirty(data);
            return scenario;
        }
        void QueueScenarioCreation()
        {
            if (scenarioCreationQueued || scenarioCreationError != null || !map || map.scenario ||
                string.IsNullOrEmpty(ThemeId(map.theme))) return;
            scenarioCreationQueued = true;
            var target = map;
            EditorApplication.delayCall += () =>
            {
                scenarioCreationQueued = false;
                if (!this || !target || map != target || target.scenario) return;
                try { EnsureScenario(target); }
                catch (Exception error) { scenarioCreationError = error.Message; Debug.LogException(error); }
                Repaint();
            };
        }
        public static DeviceTemplate[] TemplatesForTheme(IEnumerable<DeviceTemplate> templates, string themeId)
            => string.IsNullOrEmpty(themeId) ? Array.Empty<DeviceTemplate>() : templates.Where(entry =>
                entry?.source && entry.device != null && entry.source.themeId == themeId &&
                (string.IsNullOrEmpty(entry.device.artTheme) || entry.device.artTheme == themeId) &&
                !(entry.device.role == PrototypeRole.Wind && entry.device.variant == "field") &&
                !(themeId == "starway" && entry.device.role == PrototypeRole.VineBridge)).ToArray();
        public static string ThemeName(string id) => id == "starway" ? "별길 목장" : id == "memory_theater" ? "잔영 극장" :
            id == "letter_observatory" ? "편지 천문대" : id == "twilight_sea" ? "별너울 해안" : "미지정";
        public sealed class DecorationChoice
        {
            public string label;
            public Sprite sprite;
            public Sprite preview;
            public string sourceId;
        }
        public static DecorationChoice[] DecorationChoices(PuzzleTheme theme)
        {
            if (!theme) return Array.Empty<DecorationChoice>();
            string[] files, labels;
            string root;
            switch (ThemeId(theme))
            {
                case "starway":
                    if (!theme.starwayRanch || !theme.ranchConnectedDecorV02Catalog ||
                        !theme.ranchConnectedDecorV02ManifestPin) return Array.Empty<DecorationChoice>();
                    var ranchCatalog = theme.ranchConnectedDecorV02Catalog;
                    var ranchPin = theme.ranchConnectedDecorV02ManifestPin.text.Trim();
                    var ranchChoices = new List<DecorationChoice>();
                    foreach (var family in RanchConnectedDecorV02Planner.NetworkFamilies)
                        if (ranchCatalog.TryGetExact(RanchConnectedDecorV02Planner.NetworkFile(family, 0),
                            ranchPin, out var art, out _))
                            ranchChoices.Add(new DecorationChoice { label = family.Replace('_', ' '),
                                sourceId = "ranch:" + family, sprite = art.Sprite, preview = art.Sprite });
                    foreach (var key in RanchConnectedDecorV02Planner.GroupKeys)
                        if (ranchCatalog.TryGetGroup(key, ranchPin, out _, out var art, out _))
                            ranchChoices.Add(new DecorationChoice { label = key.Replace('_', ' '),
                                sourceId = "ranch:" + key, sprite = art.Sprite, preview = art.Sprite });
                    var windmill = theme.starwayRanch.Find("props/windmill_body.png");
                    if (windmill) ranchChoices.Add(new DecorationChoice { label = "Windmill (functional visual)",
                        sourceId = "props/windmill_body.png", sprite = windmill,
                        preview = theme.starwayRanch.Find("props/windmill.png") });
                    return ranchChoices.ToArray();
                case "memory_theater":
                    if (!theme.theaterConnectedDecorV01Catalog || !theme.theaterConnectedDecorV01ManifestPin)
                        return Array.Empty<DecorationChoice>();
                    var theaterCatalog = theme.theaterConnectedDecorV01Catalog;
                    var theaterPin = theme.theaterConnectedDecorV01ManifestPin.text.Trim();
                    var theaterNetworkLabels = new[] { "벨벳 객석 통로 울타리", "조각 무대 난간", "연결 소품 진열대",
                        "벨벳 장막 파티션", "황동 무대 발치등", "덩굴 화분 띠" };
                    var theaterGroupLabels = new[] { "그랜드피아노 무대", "객석 벤치", "매표소", "무대 의상 행거",
                        "분장대", "축음기 탁자", "무대 배경 가벽", "무대 소품 보관 묶음", "가면 진열대",
                        "꼭두각시 보관함", "무대 뒤 조명 묶음", "무대 계단", "입장권 안내대", "접이식 의자",
                        "악보대", "장식 촛대", "가면 받침대", "극본 더미", "무대 모래주머니", "극장 꽃 항아리" };
                    var theaterChoices = new List<DecorationChoice>();
                    var families = TheaterConnectedDecorV01Planner.NetworkFamilies;
                    for (var i = 0; i < families.Length; i++)
                        if (theaterCatalog.TryGetExact(TheaterConnectedDecorV01Planner.NetworkFile(families[i], 0),
                            theaterPin, out var art, out _))
                            theaterChoices.Add(new DecorationChoice { label = theaterNetworkLabels[i],
                                sourceId = "theater:" + families[i], sprite = art.Sprite, preview = art.Sprite });
                    var groupKeys = TheaterConnectedDecorV01Planner.GroupKeys;
                    for (var i = 0; i < groupKeys.Length; i++)
                        if (theaterCatalog.TryGetGroup(groupKeys[i], theaterPin, out _, out var art, out _))
                            theaterChoices.Add(new DecorationChoice { label = theaterGroupLabels[i],
                                sourceId = "theater:" + groupKeys[i], sprite = art.Sprite, preview = art.Sprite });
                    return theaterChoices.ToArray();
                case "letter_observatory":
                    if (!theme.sanctumConnectedDecorV01Catalog || !theme.sanctumConnectedDecorV01ManifestPin)
                        return Array.Empty<DecorationChoice>();
                    var sanctumCatalog = theme.sanctumConnectedDecorV01Catalog;
                    var sanctumPin = theme.sanctumConnectedDecorV01ManifestPin.text.Trim();
                    var sanctumChoices = new List<DecorationChoice>();
                    foreach (var family in SanctumConnectedDecorV01Planner.NetworkFamilies)
                        if (sanctumCatalog.TryGetExact(SanctumConnectedDecorV01Planner.NetworkFile(family, 0),
                            sanctumPin, out var art, out _))
                            sanctumChoices.Add(new DecorationChoice { label = family.Replace('_', ' '),
                                sourceId = "sanctum:" + family, sprite = art.Sprite, preview = art.Sprite });
                    foreach (var key in SanctumConnectedDecorV01Planner.GroupKeys)
                        if (sanctumCatalog.TryGetGroup(key, sanctumPin, out _, out var art, out _))
                            sanctumChoices.Add(new DecorationChoice { label = key.Replace('_', ' '),
                                sourceId = "sanctum:" + key, sprite = art.Sprite, preview = art.Sprite });
                    return sanctumChoices.ToArray();
                case "twilight_sea":
                    if (!theme.coastConnectedDecorV01Catalog || !theme.coastConnectedDecorV01ManifestPin)
                        return Array.Empty<DecorationChoice>();
                    var coastCatalog = theme.coastConnectedDecorV01Catalog;
                    var coastPin = theme.coastConnectedDecorV01ManifestPin.text.Trim();
                    var coastChoices = new List<DecorationChoice>();
                    foreach (var family in CoastConnectedDecorV01Planner.NetworkFamilies)
                        if (coastCatalog.TryGetExact(CoastConnectedDecorV01Planner.NetworkFile(family, 0),
                            coastPin, out var art, out _))
                            coastChoices.Add(new DecorationChoice { label = family.Replace('_', ' '),
                                sourceId = "coast:" + family, sprite = art.Sprite, preview = art.Sprite });
                    foreach (var key in CoastConnectedDecorV01Planner.GroupKeys)
                        if (coastCatalog.TryGetGroup(key, coastPin, out _, out var art, out _))
                            coastChoices.Add(new DecorationChoice { label = key.Replace('_', ' '),
                                sourceId = "coast:" + key, sprite = art.Sprite, preview = art.Sprite });
                    return coastChoices.ToArray();
                default: return Array.Empty<DecorationChoice>();
            }
            return files.Select((file, i) => {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(root + file);
                return new DecorationChoice { label = labels[i], sprite = sprite, preview = sprite };
            }).Where(choice => choice.sprite).ToArray();
        }
        void DecorationSettings()
        {
            if (cachedDecorationChoices == null || decorationChoicesTheme != map.theme)
            {
                decorationChoicesTheme = map.theme;
                cachedDecorationChoices = DecorationChoices(map.theme);
            }
            var choices = cachedDecorationChoices;
            if (choices.Length == 0)
            {
                decoration = null;
                EditorGUILayout.HelpBox("이 테마에 배치할 장식 그림이 없습니다. 테마를 확인하세요.", MessageType.Info);
                return;
            }
            if (!choices.Any(choice => choice.sprite == decoration)) decoration = choices[0].sprite;
            EditorGUILayout.LabelField("배치할 장식 선택", EditorStyles.boldLabel);
            decorationListScroll = EditorGUILayout.BeginScrollView(decorationListScroll, GUILayout.Height(240));
            foreach (var choice in choices)
            {
                var row = GUILayoutUtility.GetRect(250, 58, GUILayout.ExpandWidth(true));
                if (GUI.Button(row, GUIContent.none, EditorStyles.helpBox))
                {
                    decoration = choice.sprite;
                    var mapped = RanchConnectedDecorV02Host.SourceMapping(map, choice.sprite);
                    if (mapped != null && mapped.family != null) decorationConnectionGroup = mapped.family;
                    var theater = TheaterConnectedDecorHost.SourceMapping(map.theme, choice.sprite);
                    if (theater != null && theater.network) decorationConnectionGroup = theater.key;
                    var sanctum = SanctumConnectedDecorHost.SourceMapping(map.theme, choice.sprite);
                    if (sanctum != null && sanctum.network) decorationConnectionGroup = sanctum.key;
                    var coast = CoastConnectedDecorHost.SourceMapping(map.theme, choice.sprite);
                    if (coast != null && coast.network) decorationConnectionGroup = coast.key;
                }
                if (row.yMax < decorationListScroll.y || row.yMin > decorationListScroll.y + 240) continue;
                if (decoration == choice.sprite)
                    EditorGUI.DrawRect(new Rect(row.x, row.y, 4, row.height), new Color(.2f, .8f, 1f));
                DrawSpriteFitted(new Rect(row.x + 8, row.y + 5, 48, 48), choice.preview);
                GUI.Label(new Rect(row.x + 64, row.y + 16, row.width - 68, 28), choice.label, EditorStyles.boldLabel);
            }
            EditorGUILayout.EndScrollView();
            var selectedMapping = RanchConnectedDecorV02Host.SourceMapping(map, decoration);
            var selectedTheaterMapping = TheaterConnectedDecorHost.SourceMapping(map.theme, decoration);
            var selectedSanctumMapping = SanctumConnectedDecorHost.SourceMapping(map.theme, decoration);
            var selectedCoastMapping = CoastConnectedDecorHost.SourceMapping(map.theme, decoration);
            if (selectedMapping != null || selectedTheaterMapping != null || selectedSanctumMapping != null || selectedCoastMapping != null)
            {
                if (selectedMapping != null && (!map.theme.ranchConnectedDecorV02Catalog || !map.theme.ranchConnectedDecorV02ManifestPin))
                    EditorGUILayout.HelpBox("새 목장 장식 catalog와 manifest pin이 연결되지 않았습니다.", MessageType.Error);
                var width = selectedTheaterMapping != null ? selectedTheaterMapping.width :
                    selectedSanctumMapping != null ? selectedSanctumMapping.width :
                    selectedCoastMapping != null ? selectedCoastMapping.width : selectedMapping.occupiedWidth;
                var height = selectedTheaterMapping != null ? selectedTheaterMapping.height :
                    selectedSanctumMapping != null ? selectedSanctumMapping.height :
                    selectedCoastMapping != null ? selectedCoastMapping.height : selectedMapping.occupiedHeight;
                EditorGUILayout.LabelField("통행 점유", width + " × " + height + "칸");
                if (selectedMapping != null && (selectedMapping.width != width || selectedMapping.height != height))
                    EditorGUILayout.LabelField("그림 지면", selectedMapping.width + " × " + selectedMapping.height + "칸");
                if (selectedTheaterMapping != null ? selectedTheaterMapping.network :
                    selectedSanctumMapping != null ? selectedSanctumMapping.network :
                    selectedCoastMapping != null ? selectedCoastMapping.network : selectedMapping.IsVisual && !selectedMapping.IsGroup)
                    decorationConnectionGroup = EditorGUILayout.TextField("연결 그룹", decorationConnectionGroup);
                decorationLogicalLayer = EditorGUILayout.IntField("논리 장식 층", decorationLogicalLayer);
                decorationHeightKey = EditorGUILayout.IntField("정수 높이", decorationHeightKey);
            }
            else decorationTint = EditorGUILayout.ColorField("색상", decorationTint);
            inspectDecoration = GUILayout.Toggle(inspectDecoration, "배치된 장식 선택·연결 설정");
            if (inspectDecoration) DrawSelectedDecoration();
            if (!string.IsNullOrEmpty(decorationError)) EditorGUILayout.HelpBox(decorationError, MessageType.Warning);
        }
        void DrawSelectedDecoration()
        {
            var index = map.decorations.FindIndex(cell => RanchConnectedDecorV02Host.Occupies(map, cell, selectedDecorationCell) ||
                TheaterConnectedDecorHost.Occupies(map, cell, selectedDecorationCell) ||
                SanctumConnectedDecorHost.Occupies(map, cell, selectedDecorationCell) ||
                CoastConnectedDecorHost.Occupies(map, cell, selectedDecorationCell));
            if (index < 0) return;
            var current = map.decorations[index];
            var ranch = RanchConnectedDecorV02Host.TryMap(map, current, out var mapping);
            var theater = TheaterConnectedDecorHost.TryMap(map, current, out var theaterMapping);
            var sanctum = SanctumConnectedDecorHost.TryMap(map, current, out var sanctumMapping);
            var coast = CoastConnectedDecorHost.TryMap(map, current, out var coastMapping);
            if (!ranch && !theater && !sanctum && !coast) return;
            EditorGUILayout.LabelField("선택 장식", (theater ? theaterMapping.key : sanctum ? sanctumMapping.key : coast ? coastMapping.key : mapping.sourceId) + " @ " + current.position);
            EditorGUILayout.LabelField("배치 ID", string.IsNullOrEmpty(current.placementId) ? "기존 장식" : current.placementId);
            var next = current;
            if (theater ? theaterMapping.network : sanctum ? sanctumMapping.network : coast ? coastMapping.network : !mapping.IsGroup)
                next.connectionGroup = EditorGUILayout.TextField("연결 그룹", string.IsNullOrEmpty(current.connectionGroup) ?
                    (theater ? theaterMapping.key : sanctum ? sanctumMapping.key : coast ? coastMapping.key : mapping.family) : current.connectionGroup);
            next.logicalLayer = EditorGUILayout.IntField("논리 장식 층", current.logicalLayer);
            next.heightKey = EditorGUILayout.IntField("정수 높이", current.heightKey);
            if (next.connectionGroup == current.connectionGroup && next.logicalLayer == current.logicalLayer && next.heightKey == current.heightKey) return;
            var candidate = new List<DecorationCell>(map.decorations); candidate[index] = next;
            var theme = map.theme;
            string error = null;
            bool valid;
            if (theater)
                valid = TheaterConnectedDecorHost.TryCreateSnapshot(map, candidate,
                    new Vector3(-.5f, -.5f), 1, 0, out _, out error);
            else if (sanctum)
                valid = SanctumConnectedDecorHost.TryCreateSnapshot(map, candidate,
                    new Vector3(-.5f, -.5f), 1, 0, out _, out error);
            else if (coast)
                valid = CoastConnectedDecorHost.TryCreateSnapshot(map, candidate,
                    new Vector3(-.5f, -.5f), 1, 0, out _, out error);
            else
                valid = RanchConnectedDecorV02Host.TryCreateSnapshot(map, candidate,
                    new Vector3(-.5f, -.5f), 1, 0, out _, out error);
            if (!valid) { decorationError = error; return; }
            Undo.RecordObject(map, theater ? "극장 장식 연결 변경" : sanctum ? "성소 장식 연결 변경" :
                coast ? "해안 장식 연결 변경" : "목장 장식 연결 변경");
            map.decorations[index] = next; map.NotifyDecorationsChanged(); EditorUtility.SetDirty(map);
            decorationError = null;
        }
        static string DeviceName(PrototypeRole role)
        {
            switch (role)
            {
                case PrototypeRole.StarSocket: return "별돌·별 소켓";
                case PrototypeRole.VineBridge: return "압력판·덩굴 다리";
                case PrototypeRole.CrescentLeap: return "초승달 도약대";
                case PrototypeRole.Shutter: return "셔터";
                case PrototypeRole.DepartureSpikes: return "이탈 가시";
                case PrototypeRole.MovingStage: return "이동 발판";
                case PrototypeRole.Conveyor: return "무빙워크";
                case PrototypeRole.Bookshelf: return "책장·압력판";
                case PrototypeRole.OneWayGate: return "일방통행 문";
                case PrototypeRole.Optics: return "광학 장치";
                case PrototypeRole.Boat: return "배";
                case PrototypeRole.Barrel: return "통";
                case PrototypeRole.Domino: return "도미노";
                case PrototypeRole.Magnet: return "자석·스위치";
                case PrototypeRole.Bomb: return "폭탄";
                case PrototypeRole.Portal: return "포털";
                case PrototypeRole.Statue: return "석상";
                case PrototypeRole.Bounce: return "반사판";
                case PrototypeRole.Brake: return "브레이크";
                case PrototypeRole.Wind: return "바람개비·바람";
                case PrototypeRole.Glass: return "유리";
                case PrototypeRole.Dice: return "주사위·목표";
                case PrototypeRole.CowardDoll: return "겁쟁이 인형";
                case PrototypeRole.Chick: return "병아리";
                case PrototypeRole.Ice: return "얼음";
                case PrototypeRole.Wall: return "벽";
                case PrototypeRole.CrackedWall: return "금 간 벽";
                case PrototypeRole.WoodCrate: return "나무 상자";
                case PrototypeRole.MetalCrate: return "금속 상자";
                default: return role.ToString();
            }
        }
        static string VariantName(string variant) => variant == "plate" ? "압력판" : variant == "socket" ? "소켓" :
            variant == "source" ? "광원" : variant == "mirror" ? "거울" : variant == "sensor" ? "감지기" :
            variant == "gate" ? "문" : variant == "switch" ? "스위치" : variant == "target" ? "목표" :
            variant == "field" ? "기존 바람 칸" : variant == "blower" ? "밀어서 켜기" : variant == "square" ? "사각형" :
            variant == "a" ? "A" : variant == "b" ? "B" : variant;
        static string DeviceLabel(PrototypeDevice device) => ((int)device.role).ToString("00") + " " + DeviceName(device.role) +
            (string.IsNullOrEmpty(device.variant) ? "" : " · " + VariantName(device.variant));
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
                            label = DeviceLabel(device) + (device.visualPrefab ? "" : " · 그림 없음")
                        })
                        : Enumerable.Empty<DeviceTemplate>();
                }).ToArray();
        }
        void RefreshLibrary()
        {
            library = LoadPracticeTemplates(); templateIndex = 0;
            legacyDeviceSprites.Clear(); RanchDevicesV13EditorPreview.ClearCache();
            previewCacheValid = false; cachedDecorationChoices = null; cachedValidationErrors = null;
            Repaint();
        }
        void DeviceSettings()
        {
            var themeId = ThemeId(map.theme);
            EditorGUILayout.LabelField("현재 테마", ThemeName(themeId));
            if (!string.IsNullOrEmpty(themeId) && map.scenario.themeId != themeId && map.scenario.themeId != map.theme.name)
                EditorGUILayout.HelpBox("장치 시나리오의 테마 저장 ID가 현재 맵 테마와 다릅니다. 시나리오 설정을 확인하세요.", MessageType.Warning);
            inspectDevice = GUILayout.Toggle(inspectDevice, "왼쪽 클릭으로 배치된 장치 선택");
            showConnections = GUILayout.Toggle(showConnections, "연결선과 작동 범위 표시");
            if (inspectDevice) EditorGUILayout.HelpBox("장치를 누르면 설정을 볼 수 있습니다. 새 장치를 놓으려면 선택 모드를 끄세요. 오른쪽 드래그로 지웁니다.", MessageType.Info);
            var source = GUILayout.Toolbar(useLibrary ? 0 : 1, new[] { "테마 장치 목록", "이 맵의 장치" });
            if (useLibrary != (source == 0)) { useLibrary = source == 0; templateIndex = 0; }
            var templates = TemplatesForTheme(library, themeId);
            var choices = useLibrary ? templates.Select(entry => entry.device).ToArray() :
                map.scenario.devices.Where(entry => !string.IsNullOrEmpty(themeId) &&
                    (string.IsNullOrEmpty(entry.artTheme) || entry.artTheme == themeId) &&
                    !(entry.role == PrototypeRole.Wind && entry.variant == "field")).ToArray();
            if (choices.Length == 0)
            {
                prototypeBrush = null;
                EditorGUILayout.HelpBox(string.IsNullOrEmpty(themeId) ? "테마를 지정하면 해당 테마의 장치가 표시됩니다." :
                    "선택한 테마에 맞는 장치가 없습니다. 다른 목록을 선택하거나 이 맵에 장치를 추가하세요.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField("배치할 장치 선택", EditorStyles.boldLabel);
                templateIndex = Mathf.Clamp(templateIndex, 0, choices.Length - 1);
                deviceListScroll = EditorGUILayout.BeginScrollView(deviceListScroll, GUILayout.Height(260));
                for (var i = 0; i < choices.Length; i++)
                {
                    var row = GUILayoutUtility.GetRect(250, 62, GUILayout.ExpandWidth(true));
                    if (GUI.Button(row, GUIContent.none, EditorStyles.helpBox))
                    { templateIndex = i; ranchArtBrush = null; selectedRanchArt = null; }
                    if (row.yMax < deviceListScroll.y || row.yMin > deviceListScroll.y + 260) continue;
                    if (i == templateIndex) EditorGUI.DrawRect(new Rect(row.x, row.y, 4, row.height), new Color(.2f, .8f, 1f));
                    DrawDeviceThumbnail(new Rect(row.x + 8, row.y + 7, 48, 48), choices[i]);
                    GUI.Label(new Rect(row.x + 62, row.y + 9, row.width - 66, 25), DeviceLabel(choices[i]), EditorStyles.boldLabel);
                    GUI.Label(new Rect(row.x + 62, row.y + 34, row.width - 66, 24),
                        useLibrary ? "방향 " + DirectionGlyph(choices[i].direction) + " · " + (choices[i].visualPrefab ? "그림 있음" : "그림 없음") :
                            "장치 ID: " + choices[i].id, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndScrollView();
                prototypeBrush = ranchArtBrush == null ? choices[templateIndex] : null;
                if (useLibrary && GUILayout.Button("원본 장치 에셋 보기")) EditorGUIUtility.PingObject(templates[templateIndex].source);
            }
            if (GUILayout.Button("장치 목록 새로고침")) RefreshLibrary();
            EditorGUILayout.HelpBox("장치를 배치하면 새 ID가 만들어집니다. 연결 그룹과 목표 조건을 확인하세요. 포털 목적지와 선로는 배치한 위치만큼 이동합니다.", MessageType.Info);
            if (themeId == "starway")
            {
                DrawRanchArtPlacementSettings();
                DrawV13ArtGallery();
            }
            var selected = map.scenario.devices.FindIndex(device => device.id == selectedDeviceId);
            if (selected < 0) return;
            EditorGUILayout.LabelField("선택한 장치", EditorStyles.boldLabel);
            var placed = map.scenario.devices[selected];
            EditorGUILayout.LabelField(placed.id + " / " + DeviceName(placed.role));
            EditorGUILayout.LabelField("바라보는 방향");
            EditorGUILayout.BeginHorizontal();
            foreach (var heading in PuzzleAutotile.Cardinal)
                if (GUILayout.Toggle(placed.direction == heading, DirectionName(heading),
                    EditorStyles.miniButton, GUILayout.Width(58)) && placed.direction != heading)
                {
                    Undo.RecordObject(map.scenario, "장치 방향 변경");
                    placed.direction = heading;
                    EditorUtility.SetDirty(map.scenario);
                }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("연결 그룹", string.IsNullOrEmpty(placed.group) ? "없음" : placed.group);
            if (!string.IsNullOrEmpty(placed.group))
                foreach (var peer in map.scenario.devices.Where(device => device != placed && device.group == placed.group))
                    EditorGUILayout.LabelField("  → " + peer.id + " / " + DeviceName(peer.role) + " " + peer.position);
            if (GUILayout.Button(linkTargetMode ? "연결 취소" : "다른 장치와 연결...")) linkTargetMode = !linkTargetMode;
            if (linkTargetMode) EditorGUILayout.HelpBox("격자에서 연결할 장치를 클릭하세요. 두 장치는 같은 논리 그룹에 속하게 됩니다.", MessageType.Info);
            if (!string.IsNullOrEmpty(placed.group) && GUILayout.Button("선택한 장치 연결 해제"))
            {
                Undo.RecordObject(map.scenario, "장치 연결 해제");
                placed.group = string.Empty;
                EditorUtility.SetDirty(map.scenario);
            }
            if (placed.role == PrototypeRole.Portal) EditorGUILayout.LabelField("포털 목적지", placed.target.ToString());
            if (placed.role == PrototypeRole.Chick)
            {
                EditorGUILayout.LabelField("둥지 위치", placed.target == Vector2Int.zero ? "미설정" : placed.target.ToString());
                if (GUILayout.Button(nestTargetMode ? "둥지 위치 선택 취소" : "격자에서 둥지 위치 선택"))
                { nestTargetMode = !nestTargetMode; if (nestTargetMode) { inspectDevice = true; linkTargetMode = false; } }
                if (nestTargetMode) EditorGUILayout.HelpBox("통행 가능한 칸을 클릭하면 병아리의 둥지가 됩니다.", MessageType.Info);
            }
            if (placed.role == PrototypeRole.Wind && placed.variant == "blower")
                EditorGUILayout.HelpBox("플레이 중 풍차를 밀 때 바람이 꺼짐 → 3칸 → 6칸 → 9칸 → 꺼짐 순서로 바뀝니다.", MessageType.Info);
            if (placed.role == PrototypeRole.StarSocket && placed.variant == "socket") DrawSocketRewardSettings(placed);
            if (placed.cells != null && placed.cells.Count > 0) EditorGUILayout.LabelField("점유·선로 칸", placed.cells.Count.ToString());
            if (placed.role == PrototypeRole.Bomb) EditorGUILayout.LabelField("폭발 범위", (placed.blastOffsets?.Count ?? 0).ToString());
            var serialized = new SerializedObject(map.scenario); serialized.Update();
            DrawDeviceProperties(serialized.FindProperty("devices").GetArrayElementAtIndex(selected));
            serialized.ApplyModifiedProperties();
        }
        void DrawV13ArtGallery()
        {
            showV13Art = EditorGUILayout.Foldout(showV13Art, "V13 신규 장치 아트", true);
            if (!showV13Art) return;
            EditorGUILayout.HelpBox("아래 그림은 패키지에 포함되지만 현재 맵의 장치 판정에는 아직 연결되지 않았습니다. 미리보기만 가능합니다.", MessageType.Info);
            for (var i = 0; i < v13GalleryKeys.Length; i++)
            {
                var row = GUILayoutUtility.GetRect(250, 58, GUILayout.ExpandWidth(true));
                if (RanchDevicesV13EditorPreview.TryClip(map.theme, v13GalleryKeys[i], out var sprite))
                    DrawSpriteFitted(new Rect(row.x + 5, row.y + 5, 48, 48), sprite);
                else GUI.Label(new Rect(row.x + 5, row.y + 5, 48, 48), "그림 없음", EditorStyles.whiteMiniLabel);
                GUI.Label(new Rect(row.x + 62, row.y + 7, row.width - 66, 22), v13GalleryNames[i], EditorStyles.boldLabel);
                GUI.Label(new Rect(row.x + 62, row.y + 29, row.width - 66, 23), v13GalleryKeys[i], EditorStyles.miniLabel);
            }
        }
        void DrawRanchArtPlacementSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("V13 별양 · 수문 배치", EditorStyles.boldLabel);
            foreach (var family in new[] { "sheep", "sluice" })
            {
                var key = family == "sheep" ? "sheep/east/idle" : "sluice/horizontal/left";
                var row = GUILayoutUtility.GetRect(250, 58, GUILayout.ExpandWidth(true));
                if (GUI.Button(row, GUIContent.none, EditorStyles.helpBox))
                { ranchArtBrush = family; prototypeBrush = null; selectedDeviceId = null; inspectDevice = false; }
                if (ranchArtBrush == family)
                    EditorGUI.DrawRect(new Rect(row.x, row.y, 4, row.height), new Color(.2f, .8f, 1f));
                if (RanchDevicesV13EditorPreview.TryClip(map.theme, key, out var sprite))
                    DrawSpriteFitted(new Rect(row.x + 8, row.y + 5, 48, 48), sprite);
                else GUI.Label(new Rect(row.x + 8, row.y + 5, 48, 48), "그림 없음", EditorStyles.whiteMiniLabel);
                GUI.Label(new Rect(row.x + 62, row.y + 7, row.width - 66, 22),
                    family == "sheep" ? "별양 (1칸)" : "수문 (가로 2칸)", EditorStyles.boldLabel);
                GUI.Label(new Rect(row.x + 62, row.y + 29, row.width - 66, 23), key, EditorStyles.miniLabel);
            }
            EditorGUILayout.HelpBox("선택 후 격자에 배치합니다. 현재는 맵에 저장되는 V13 그림 배치이며 양 밀기·풀 먹기·수문 A/B 전환 게임 판정은 아직 연결되지 않았습니다.", MessageType.Warning);
            if (!string.IsNullOrEmpty(ranchArtError)) EditorGUILayout.HelpBox(ranchArtError, MessageType.Warning);
            if (selectedRanchArt == null || !map.scenario.ranchV13Art.Contains(selectedRanchArt)) return;
            EditorGUILayout.LabelField("선택한 배치", selectedRanchArt.family + " @ " + selectedRanchArt.position);
            if (selectedRanchArt.family == "sheep")
            {
                EditorGUILayout.BeginHorizontal();
                foreach (var heading in PuzzleAutotile.Cardinal)
                    if (GUILayout.Toggle(selectedRanchArt.direction == heading, DirectionName(heading),
                        EditorStyles.miniButton, GUILayout.Width(58)) && selectedRanchArt.direction != heading)
                    {
                        Undo.RecordObject(map.scenario, "별양 방향 변경");
                        selectedRanchArt.direction = heading;
                        EditorUtility.SetDirty(map.scenario);
                    }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                var right = EditorGUILayout.Toggle("수문 오른쪽 그림", selectedRanchArt.rightState);
                if (right != selectedRanchArt.rightState)
                {
                    Undo.RecordObject(map.scenario, "수문 그림 상태 변경");
                    selectedRanchArt.rightState = right;
                    EditorUtility.SetDirty(map.scenario);
                }
            }
        }

        public static bool TryPlaceRanchArt(PuzzleMap target, string family, Vector2Int anchor, out string error)
        {
            error = null;
            if (!target || !target.scenario || !target.theme || !target.theme.starwayRanch ||
                !target.theme.starwayRanch.v13Devices || (family != "sheep" && family != "sluice"))
            { error = "V13 목장 테마·카탈로그와 별양/수문 종류가 필요합니다."; return false; }
            var art = new RanchV13ArtPlacement { family = family, position = anchor };
            if (!RanchDevicesV13EditorPreview.TryClip(target.theme, art.ClipKey, out _))
            { error = "V13 별양/수문 그림을 카탈로그에서 찾지 못했습니다."; return false; }
            foreach (var cell in art.Footprint())
            {
                if (!target.Contains(cell) || !target.Walkable(cell))
                { error = "전체 점유 칸에 통행 가능한 지형이 필요합니다."; return false; }
                if (target.scenario.devices.Any(device => device.position == cell) ||
                    target.scenario.ranchV13Art.Any(existing => existing != null && existing.Footprint().Contains(cell)))
                { error = "다른 장치나 V13 배치와 점유 칸이 겹칩니다."; return false; }
            }
            target.scenario.ranchV13Art.Add(art);
            return true;
        }

        public static bool TryEraseRanchArt(PuzzleMap target, Vector2Int cell)
        {
            if (!target || !target.scenario) return false;
            var index = target.scenario.ranchV13Art.FindIndex(art => art != null && art.Footprint().Contains(cell));
            if (index < 0) return false;
            target.scenario.ranchV13Art.RemoveAt(index);
            return true;
        }
        void DrawSocketRewardSettings(PrototypeDevice socket)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("별돌 소켓 작동", EditorStyles.boldLabel);
            var v13Ranch = map.theme && map.theme.starwayRanch && map.theme.starwayRanch.v13Devices;
            StarSocketReward reward;
            if (v13Ranch && socket.socketReward != StarSocketReward.RevealBridge)
            {
                var choice = EditorGUILayout.Popup("별돌이 들어오면",
                    socket.socketReward == StarSocketReward.RevealBubble ? 1 : 0,
                    new[] { "소켓만 켜기", "방울 나타내기" });
                reward = choice == 1 ? StarSocketReward.RevealBubble : StarSocketReward.None;
            }
            else
                reward = (StarSocketReward)EditorGUILayout.Popup("별돌이 들어오면", (int)socket.socketReward,
                    new[] { "소켓만 켜기", "기존 덩굴 다리 펼치기", "방울 나타내기" });
            if (reward != socket.socketReward)
            {
                Undo.RecordObject(map.scenario, "별돌 소켓 작동 변경");
                socket.socketReward = reward;
                if (reward == StarSocketReward.RevealBubble) socket.socketBubbleCell = socket.position;
                EditorUtility.SetDirty(map.scenario);
            }
            if (reward == StarSocketReward.RevealBridge)
            {
                if (v13Ranch) EditorGUILayout.HelpBox("이 연결은 기존 맵 데이터 보존용입니다. V13 장치 목록에는 덩굴 다리를 새로 추가하지 않습니다.", MessageType.Info);
                var bridges = map.scenario.devices.Where(device => device.role == PrototypeRole.VineBridge &&
                    device.variant != "plate" && device != socket).ToArray();
                if (bridges.Length == 0)
                    EditorGUILayout.HelpBox("먼저 맵에 덩굴 다리를 배치하세요.", MessageType.Info);
                else
                {
                    var index = Array.FindIndex(bridges, bridge => bridge.id == socket.socketBridgeId);
                    index = EditorGUILayout.Popup("펼칠 덩굴 다리", Mathf.Max(0, index),
                        bridges.Select(bridge => bridge.id + " " + bridge.position).ToArray());
                    if (socket.socketBridgeId != bridges[index].id)
                    {
                        Undo.RecordObject(map.scenario, "소켓 덩굴 다리 선택");
                        socket.socketBridgeId = bridges[index].id;
                        EditorUtility.SetDirty(map.scenario);
                    }
                }
            }
            if (reward == StarSocketReward.RevealBubble)
            {
                var cell = EditorGUILayout.Vector2IntField("방울이 나타날 칸", socket.socketBubbleCell);
                if (cell != socket.socketBubbleCell)
                {
                    Undo.RecordObject(map.scenario, "소켓 방울 위치 변경");
                    socket.socketBubbleCell = cell;
                    EditorUtility.SetDirty(map.scenario);
                }
                EditorGUILayout.HelpBox("방울은 별돌이 들어간 뒤에만 보이고 수집됩니다. 승리 조건에 포함하려면 목표 방울 수를 확인하세요.", MessageType.Info);
            }
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
            if (confirm && count > 0 && !EditorUtility.DisplayDialog("맵 밖 배치 보관", count + "개 배치가 새 맵의 바깥에 남습니다. 데이터는 삭제되지 않으며 맵을 넓히면 복구됩니다. 시작점과 출구가 밖에 있다면 플레이 전에 옮겨야 합니다.", "크기 변경", "취소")) return false;
            Undo.RegisterCompleteObjectUndo(data, "퍼즐 맵 크기 변경");
            var oldWidth = data.Width; var oldHeight = data.Height;
            if (width > oldWidth || height > oldHeight)
            {
                var occupied = new HashSet<Vector2Int>(data.terrain.Select(tile => tile.position));
                for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
                {
                    if (x < oldWidth && y < oldHeight) continue;
                    var cell = new Vector2Int(x, y);
                    if (occupied.Add(cell)) data.terrain.Add(new TerrainCell(cell, TerrainMaterial.Grass));
                }
            }
            data.Resize(width, height); EditorUtility.SetDirty(data); return true;
        }
        public static Rect CameraBounds(PuzzleMap data, Vector2 focus)
        {
            var settings = data.cameraSettings ?? new PuzzleCameraSettings();
            var rows = Mathf.Clamp(settings.visibleRows, 5, 25);
            var aspect = 540f / (960f - Mathf.Clamp(settings.topInset, 0, 150) -
                Mathf.Clamp(settings.bottomInset, 0, 300));
            var halfHeight = rows * .5f;
            var halfWidth = halfHeight * aspect;
            var center = PuzzleFollowCamera.Clamp(focus, data.Width, data.Height, halfHeight, aspect);
            return new Rect(center.x - halfWidth, center.y - halfHeight, halfWidth * 2, rows);
        }
        public static string[] TheaterTerrainRows(PuzzleMap data)
        {
            var occupancy = new bool[data.Width, data.Height];
            for (var y = 0; y < data.Height; y++) for (var x = 0; x < data.Width; x++)
                occupancy[x, y] = data.TryTerrain(new Vector2Int(x, data.Height - 1 - y), out _);
            var layout = data.theme.memorySkyTheater.defaultLayout;
            var deck = layout ? new HashSet<Vector2Int>(layout.visualDeck) : new HashSet<Vector2Int>();
            var carpet = layout ? new HashSet<Vector2Int>(layout.velvet) : new HashSet<Vector2Int>();
            return EchoTheaterV03MaterialSelector.BuildVisualMaterialRows(occupancy,
                (x, y) => deck.Contains(new Vector2Int(x, data.Height - 1 - y)),
                (x, y) => carpet.Contains(new Vector2Int(x, data.Height - 1 - y)));
        }
        public static Sprite TerrainPreviewSprite(PuzzleMap data, Vector2Int p, string[] theaterRows = null)
        {
            if (!data.theme || !data.TryTerrain(p, out var tile)) return null;
            var theme = data.theme;
            if (theme.memorySkyTheater && theme.memorySkyTheater.v02)
            {
                var rows = theaterRows ?? TheaterTerrainRows(data);
                return theme.memorySkyTheater.v02.Find(EchoTheaterV03MaterialSelector.GetTopSpritePath(
                    rows, p.x, data.Height - 1 - p.y));
            }
            if (theme.sunsetSanctumV02Catalog)
            {
                var material = tile.material == TerrainMaterial.Stone ? SunsetSanctumV02TerrainSelector.Material.StoneBridge :
                    tile.material == TerrainMaterial.Sand ? SunsetSanctumV02TerrainSelector.Material.WoodBridge :
                    SunsetSanctumV02TerrainSelector.Material.SanctumFloor;
                var mask = SunsetSanctumV02TerrainSelector.NeighborMask(
                    (x, y) => data.TryTerrain(new Vector2Int(x, y), out _), p.x, p.y);
                var file = SunsetSanctumV02TerrainSelector.File(material, mask);
                return theme.sunsetSanctumV02Catalog.TryGetPart(file, SunsetSanctumV02PuzzleBinding.ManifestSha256,
                    out var part, out _) ? part.Frame.Sprite : null;
            }
            if (theme.starSwellCoastV01Catalog)
            {
                var material = tile.material == TerrainMaterial.Stone ? StarSwellCoastV01TerrainSelector.Material.StoneBridge :
                    tile.material == TerrainMaterial.Sand ? StarSwellCoastV01TerrainSelector.Material.WoodBridge :
                    StarSwellCoastV01TerrainSelector.Material.CoastSand;
                var mask = StarSwellCoastV01TerrainSelector.NeighborMask(
                    (x, y) => data.TryTerrain(new Vector2Int(x, y), out _), p.x, p.y);
                var file = StarSwellCoastV01TerrainSelector.File(material, mask);
                return theme.starSwellCoastV01Catalog.TryGetPart(file, StarSwellCoastV01PuzzleBinding.ManifestSha256,
                    out var part, out _) ? part.Frame.Sprite : null;
            }
            return null;
        }
        // Snapshot planning scans the whole board; redo it only after an edited asset changes.
        void RefreshPreviewCache()
        {
            var mapDirtyCount = EditorUtility.GetDirtyCount(map);
            var scenarioDirtyCount = map.scenario ? EditorUtility.GetDirtyCount(map.scenario) : 0;
            var themeDirtyCount = map.theme ? EditorUtility.GetDirtyCount(map.theme) : 0;
            if (previewCacheValid && previewMap == map && previewScenario == map.scenario &&
                previewTheme == map.theme && previewMapDirtyCount == mapDirtyCount &&
                previewScenarioDirtyCount == scenarioDirtyCount && previewThemeDirtyCount == themeDirtyCount) return;
            previewMap = map; previewScenario = map.scenario; previewTheme = map.theme;
            previewMapDirtyCount = mapDirtyCount; previewScenarioDirtyCount = scenarioDirtyCount;
            previewThemeDirtyCount = themeDirtyCount;
            previewCacheValid = false;
            ranchPreview = null; theaterPreview = null; sanctumPreview = null; coastPreview = null;
        }
        void DrawGrid(Rect viewport)
        {
            var retained = map.terrain.Select(t => t.position).Concat(map.devices.Select(d => d.position))
                .Concat(map.decorations.Select(d => d.position)).Concat(map.bubbles).Concat(new[] { map.start, map.exit })
                .Concat(map.scenario ? map.scenario.devices.Select(d => d.position).Concat(map.scenario.water)
                    .Concat(map.scenario.devices.SelectMany(d => d.cells))
                    .Concat(map.scenario.devices.Where(d => d.role == PrototypeRole.StarSocket && d.variant == "socket" &&
                        d.socketReward == StarSocketReward.RevealBubble).Select(d => d.socketBubbleCell))
                    : Enumerable.Empty<Vector2Int>()).ToArray();
            var shownWidth = Mathf.Max(map.Width, retained.Max(p => p.x) + 1);
            var shownHeight = Mathf.Max(map.Height, retained.Max(p => p.y) + 1);
            var cameraBounds = CameraBounds(map, cameraPreviewCell);
            // Native Ranch sprites can extend beyond their anchor and the map's top edge.
            // Give the scroll content room to show that art instead of clipping its roof/rotor.
            var ranchOverhang = map.theme && (map.theme.starwayRanch || map.theme.sunsetSanctumV02Catalog) &&
                map.decorations.Any(d => map.Contains(d.position));
            var leftPad = Mathf.Max(ranchOverhang ? 1 : 0,
                showCameraBounds ? Mathf.Max(0, Mathf.CeilToInt(-cameraBounds.xMin - .5f)) : 0);
            var rightPad = showCameraBounds ? Mathf.Max(0, Mathf.CeilToInt(cameraBounds.xMax - shownWidth + .5f)) : 0;
            var topPad = Mathf.Max(ranchOverhang ? 2 : 0,
                showCameraBounds ? Mathf.Max(0, Mathf.CeilToInt(cameraBounds.yMax - shownHeight + .5f)) : 0);
            var bottomPad = showCameraBounds ? Mathf.Max(0, Mathf.CeilToInt(-cameraBounds.yMin - .5f)) : 0;
            var origin = new Vector2(leftPad * cellSize, topPad * cellSize);
            var content = new Rect(0, 0, (leftPad + shownWidth + rightPad + 1) * cellSize,
                (topPad + shownHeight + bottomPad + 1) * cellSize);
            var pointerInside = viewport.Contains(Event.current.mousePosition);
            gridScroll = GUI.BeginScrollView(viewport, gridScroll, content);
            var xMin = Mathf.Max(0, Mathf.FloorToInt((gridScroll.x - origin.x) / cellSize));
            var xMax = Mathf.Min(shownWidth, Mathf.CeilToInt((gridScroll.x + viewport.width - origin.x) / cellSize));
            var yMin = Mathf.Max(0, Mathf.FloorToInt((gridScroll.y - origin.y) / cellSize));
            var yMax = Mathf.Min(shownHeight, Mathf.CeilToInt((gridScroll.y + viewport.height - origin.y) / cellSize));
            var islandArt = map.theme ? map.theme.skyIsland : null;
            var latestTerrain = map.theme && (map.theme.memorySkyTheater && map.theme.memorySkyTheater.v02 ||
                map.theme.sunsetSanctumV02Catalog || map.theme.starSwellCoastV01Catalog);
            if (Event.current.type == EventType.Repaint)
            {
                var theaterRows = latestTerrain && map.theme.memorySkyTheater && map.theme.memorySkyTheater.v02
                    ? TheaterTerrainRows(map) : null;
                var decorationsByCell = map.decorations.ToLookup(d => d.position);
                var configuredByCell = map.scenario ? map.scenario.devices.ToLookup(d => d.position) :
                    Enumerable.Empty<PrototypeDevice>().ToLookup(d => d.position);
                var nestCells = map.scenario ? new HashSet<Vector2Int>(map.scenario.devices.Where(d =>
                    d.role == PrototypeRole.Chick && d.target != Vector2Int.zero).Select(d => d.target)) : null;
                var rewardCells = map.scenario ? new HashSet<Vector2Int>(map.scenario.devices.Where(d =>
                    d.role == PrototypeRole.StarSocket && d.variant == "socket" &&
                    d.socketReward == StarSocketReward.RevealBubble).Select(d => d.socketBubbleCell)) : null;
                var waterCells = map.scenario ? new HashSet<Vector2Int>(map.scenario.water) : null;
                var bubbleCells = new HashSet<Vector2Int>(map.bubbles);
                if (islandArt)
                {
                    // Paint every base first, then the entire underside before any grass.
                    for (var row = yMin; row < yMax; row++) for (var x = xMin; x < xMax; x++)
                    {
                        var p = new Vector2Int(x, shownHeight - 1 - row);
                        EditorGUI.DrawRect(new Rect(origin.x + x * cellSize, origin.y + row * cellSize, cellSize, cellSize), map.Contains(p) ? new Color(.08f, .11f, .16f) : new Color(.25f, .12f, .16f));
                    }
                    for (var row = Mathf.Max(0, yMin - 1); row < yMax; row++) for (var x = xMin; x < xMax; x++)
                    {
                        var p = new Vector2Int(x, shownHeight - 1 - row);
                        if (!SkyIslandAutotile.UsesArt(map, p)) continue;
                        var shape = SkyIslandAutotile.Resolve(map, p);
                        var pair = islandArt.Find(shape.exposure, shape.innerCorners);
                        DrawSprite(new Rect(origin.x + x * cellSize, origin.y + (row - SkyIslandArt.UndersideY) * cellSize, cellSize, cellSize), pair.underside);
                    }
                }
                if (latestTerrain && Event.current.type == EventType.Repaint)
                    DrawThemedUndersides(theaterRows, shownHeight, origin,
                        new Rect(gridScroll.x, gridScroll.y, viewport.width, viewport.height));
                for (var row = yMin; row < yMax; row++) for (var x = xMin; x < xMax; x++)
                {
                    var p = new Vector2Int(x, shownHeight - 1 - row);
                    var rect = new Rect(origin.x + x * cellSize, origin.y + row * cellSize, cellSize - 1, cellSize - 1);
                    if (!islandArt) EditorGUI.DrawRect(rect, map.Contains(p) ? new Color(.08f, .11f, .16f) : new Color(.25f, .12f, .16f));
                    if (map.TryTerrain(p, out var tile))
                    {
                        if (latestTerrain)
                        {
                            var sprite = TerrainPreviewSprite(map, p, theaterRows);
                            if (sprite)
                            {
                                var previousColor = GUI.color;
                                GUI.color = tile.blocked ? new Color(.42f, .45f, .55f) : Color.white;
                                DrawWorldSprite(rect, sprite);
                                GUI.color = previousColor;
                            }
                            else GUI.Label(rect, "그림 없음", EditorStyles.whiteMiniLabel);
                        }
                        else
                        {
                            var shape = PuzzleAutotile.Resolve(map, p);
                            var art = map.theme ? map.theme.Art(tile.material) : null;
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
                        }
                        if (tile.blocked) GUI.Label(rect, "#");
                    }
                    foreach (var decor in decorationsByCell[p])
                    {
                        if (!string.IsNullOrEmpty(decor.visualFamily) || RanchConnectedDecorV02Host.TryMap(map, decor, out _)) continue;
                        if (TheaterConnectedDecorHost.TryMap(map, decor, out _)) continue;
                        if (SanctumConnectedDecorHost.TryMap(map, decor, out _)) continue;
                        if (CoastConnectedDecorHost.TryMap(map, decor, out _)) continue;
                        if (map.theme && map.theme.starwayRanch)
                            DrawRanchDecorationPreview(decor, shownHeight, origin,
                                new Rect(gridScroll.x, gridScroll.y, viewport.width, viewport.height));
                        else DrawSpriteFitted(rect, decor.sprite ? decor.sprite : map.theme ? map.theme.decoration : null);
                    }
                    if (nestCells != null && nestCells.Contains(p))
                    {
                        if (map.theme && map.theme.starwayRanch && map.theme.starwayRanch.v13Devices)
                        {
                            if (RanchDevicesV13EditorPreview.TryClip(map.theme, "nest/idle", out var nest))
                                DrawWorldSprite(rect.center, nest);
                            else GUI.Label(rect, "V13 둥지 없음", EditorStyles.whiteMiniLabel);
                        }
                        else
                        {
                            var nestSprite = map.theme && map.theme.starwayRanch ? map.theme.starwayRanch.Find("props/birdhouse.png") : null;
                            if (nestSprite) DrawSprite(rect, nestSprite);
                            else GUI.Label(rect, "둥지", EditorStyles.boldLabel);
                        }
                    }
                    if (waterCells != null && waterCells.Contains(p))
                    {
                        if (map.theme && map.theme.starwayRanch && map.theme.starwayRanch.v13Devices)
                            DrawV13WaterPreview(rect, p, waterCells);
                        else GUI.Label(rect, "~");
                    }
                    var d = map.DeviceAt(p);
                    if (d.kind != DeviceKind.None)
                        GUI.Label(rect, (d.kind == DeviceKind.Portal ? "문" : d.kind == DeviceKind.Conveyor ? "무" :
                            d.kind == DeviceKind.Wind ? "바" : d.kind == DeviceKind.Ice ? "얼" : "멈") + DirectionGlyph(d.direction));
                    foreach (var configured in configuredByCell[p]) DrawDevice(rect, configured);
                    if (bubbleCells.Contains(p)) GUI.Label(rect, "방", EditorStyles.boldLabel);
                    else if (rewardCells != null && rewardCells.Contains(p))
                        GUI.Label(rect, "방+", EditorStyles.boldLabel);
                    if (map.start == p) GUI.Label(rect, "시", EditorStyles.boldLabel);
                    if (map.exit == p) GUI.Label(new Rect(rect.x + cellSize * .5f, rect.y, cellSize * .5f, cellSize), "출", EditorStyles.boldLabel);
                    if (!map.Contains(p)) GUI.Label(rect, "/");
                }
                var visible = new Rect(gridScroll.x, gridScroll.y, viewport.width, viewport.height);
                if (map.scenario && map.theme && map.theme.starwayRanch)
                    foreach (var art in map.scenario.ranchV13Art)
                    {
                        if (art == null || !map.Contains(art.position)) continue;
                        if (RanchDevicesV13EditorPreview.TryClip(map.theme, art.ClipKey, out var sprite))
                            DrawWorldSpriteAt(new Vector2(art.position.x + (art.family == "sluice" ? .5f : 0f),
                                art.position.y), sprite, shownHeight, origin, visible);
                        if (art == selectedRanchArt)
                            Handles.DrawSolidRectangleWithOutline(new Rect(origin.x + art.position.x * cellSize,
                                origin.y + (shownHeight - art.position.y - 1) * cellSize,
                                cellSize * (art.family == "sluice" ? 2 : 1), cellSize),
                                Color.clear, new Color(.2f, .8f, 1f));
                    }
                DrawOffscreenRanchDecorationPreviews(xMin, xMax, yMin, yMax, shownHeight, origin, visible);
                RefreshPreviewCache();
                DrawConnectedDecorPreview(shownHeight, origin, visible);
                DrawTheaterConnectedDecorPreview(shownHeight, origin, visible);
                DrawSanctumConnectedDecorPreview(shownHeight, origin, visible);
                DrawCoastConnectedDecorPreview(shownHeight, origin, visible);
                previewCacheValid = true;
                DrawDeviceDirectionBadges(xMin, xMax, yMin, yMax, shownHeight, origin);
                if (map.scenario && showConnections) DrawConnections(shownHeight, origin);
                if (showCameraBounds) DrawCameraBounds(cameraBounds, shownHeight, origin);
            }
            var e = Event.current;
            if (pointerInside && e.type == EventType.Repaint)
            {
                var hover = new Vector2Int(Mathf.FloorToInt((e.mousePosition.x - origin.x) / cellSize),
                    shownHeight - 1 - Mathf.FloorToInt((e.mousePosition.y - origin.y) / cellSize));
                if (map.Contains(hover))
                {
                    var eraseHover = erase || e.button == 1;
                    Handles.DrawSolidRectangleWithOutline(new Rect(origin.x + hover.x * cellSize,
                        origin.y + (shownHeight - hover.y - 1) * cellSize, cellSize, cellSize),
                        new Color(eraseHover ? 1f : .2f, eraseHover ? .25f : 1f, .3f, .12f),
                        eraseHover ? new Color(1f, .3f, .3f) : new Color(.3f, 1f, .5f));
                }
            }
            if (pointerInside && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && (e.button == 0 || e.button == 1))
            {
                var p = new Vector2Int(Mathf.FloorToInt((e.mousePosition.x - origin.x) / cellSize),
                    shownHeight - 1 - Mathf.FloorToInt((e.mousePosition.y - origin.y) / cellSize));
                if (map.Contains(p))
                {
                    if (layer == Layer.Decoration && inspectDecoration && e.button == 0)
                    {
                        if (e.type == EventType.MouseDown) selectedDecorationCell = p;
                    }
                    else if (layer == Layer.Device && map.scenario && inspectDevice && e.button == 0)
                    {
                        var clicked = map.scenario.devices.FirstOrDefault(device => device.position == p);
                        var selectedChick = map.scenario.devices.FirstOrDefault(device => device.id == selectedDeviceId && device.role == PrototypeRole.Chick);
                        if (nestTargetMode && selectedChick != null && e.type == EventType.MouseDown)
                        {
                            if (map.Walkable(p))
                            {
                                Undo.RecordObject(map.scenario, "병아리 둥지 위치 변경");
                                selectedChick.target = p;
                                EditorUtility.SetDirty(map.scenario);
                                nestTargetMode = false;
                            }
                        }
                        else if (linkTargetMode && clicked != null)
                        {
                            var source = map.scenario.devices.FirstOrDefault(device => device.id == selectedDeviceId);
                            if (source != null && source != clicked)
                            {
                                Undo.RecordObject(map.scenario, "퍼즐 장치 연결");
                                ConnectDevices(map.scenario, source, clicked);
                                EditorUtility.SetDirty(map.scenario);
                                linkTargetMode = false;
                            }
                        }
                        else if (e.type == EventType.MouseDown)
                        {
                            selectedDeviceId = clicked?.id;
                            selectedRanchArt = map.scenario.ranchV13Art.FirstOrDefault(art => art != null && art.Footprint().Contains(p));
                        }
                    }
                    else
                    {
                        if (!stroke) { Undo.IncrementCurrentGroup(); undoGroup = Undo.GetCurrentGroup(); Undo.RegisterCompleteObjectUndo(map, "퍼즐 맵 배치"); stroke = true; }
                        if (lastPaint != p) { Paint(p, erase || e.button == 1); lastPaint = p; }
                    }
                    e.Use(); Repaint();
                }
            }
            GUI.EndScrollView();
        }
        static string DirectionGlyph(Vector2Int d) => d == Vector2Int.up ? "^" : d == Vector2Int.right ? ">" : d == Vector2Int.down ? "v" : d == Vector2Int.left ? "<" : "?";
        void DrawV13WaterPreview(Rect cell, Vector2Int position, ISet<Vector2Int> sameState)
        {
            if (!RanchDevicesV13EditorPreview.TryClip(map.theme, "water/wet", out var water))
            { GUI.Label(cell, "V13 수로 없음", EditorStyles.whiteMiniLabel); return; }
            DrawWorldSprite(cell.center, water);
            var mask = RanchDevicesV13EditorPreview.WaterNeighborMask(sameState, position);
            for (var i = 0; i < 4; i++)
                if ((mask & waterEdgeBits[i]) == 0) DrawV13WaterPart(cell, waterEdgeKeys[i]);
            for (var i = 0; i < 4; i++)
            {
                var a = (mask & waterEdgeBits[i]) != 0;
                var b = (mask & waterEdgeBits[(i + 1) % 4]) != 0;
                if (!a && !b) DrawV13WaterPart(cell, waterOuterKeys[i]);
                else if (a && b && (mask & waterDiagonalBits[i]) == 0)
                    DrawV13WaterPart(cell, waterInnerKeys[i]);
            }
        }
        void DrawV13WaterPart(Rect cell, string key)
        {
            if (RanchDevicesV13EditorPreview.TryPart(map.theme, key, out var sprite))
                DrawWorldSprite(cell.center, sprite);
        }
        static string DirectionName(Vector2Int d) => d == Vector2Int.up ? "북 N" : d == Vector2Int.right ? "동 E" :
            d == Vector2Int.down ? "남 S" : d == Vector2Int.left ? "서 W" : "방향 없음";
        void DrawDevice(Rect cell, PrototypeDevice device)
        {
            if (RanchDevicesV13EditorPreview.Covers(map.theme, device))
            {
                if (RanchDevicesV13EditorPreview.TryDevice(map.theme, device, out var v13Sprite, out _))
                    DrawWorldSprite(cell.center, v13Sprite);
                else GUI.Label(cell, "V13 그림 없음", EditorStyles.whiteMiniLabel);
            }
            else
            {
                var inset = cell.width * .12f;
                var visual = new Rect(cell.x + inset, cell.y + inset, cell.width - inset * 2, cell.height - inset * 2);
                DrawDeviceThumbnail(visual, device);
            }
            var selected = device.id == selectedDeviceId;
            var strip = new Rect(cell.x, cell.y + cell.height * .72f, cell.width, cell.height * .28f);
            EditorGUI.DrawRect(strip, selected ? new Color(.1f, .55f, .9f, .9f) : new Color(.04f, .07f, .12f, .85f));
            GUI.Label(strip, ((int)device.role).ToString("00") + " " + DirectionName(device.direction), EditorStyles.whiteMiniLabel);
            if (selected) Handles.DrawSolidRectangleWithOutline(cell, Color.clear, new Color(.2f, .8f, 1f));
            if (!device.visualPrefab) GUI.Label(new Rect(cell.x, cell.y, cell.width, cell.height * .3f), "그림 없음", EditorStyles.whiteMiniLabel);
        }
        void DrawDeviceThumbnail(Rect rect, PrototypeDevice device)
        {
            var sprite = DeviceSprite(device);
            if (RanchDevicesV13EditorPreview.Covers(map.theme, device))
            {
                if (sprite) DrawSpriteFitted(rect, sprite);
                else GUI.Label(rect, "V13 그림 없음", EditorStyles.whiteMiniLabel);
                return;
            }
            if (device.role == PrototypeRole.StarSocket && device.variant != "socket")
            {
                var inset = rect.width * .12f;
                rect = new Rect(rect.x + inset, rect.y + inset, rect.width - inset * 2, rect.height - inset * 2);
            }
            if (sprite) DrawSpriteFitted(rect, sprite);
            else if (device.visualPrefab)
            {
                var preview = AssetPreview.GetAssetPreview(device.visualPrefab) ?? AssetPreview.GetMiniThumbnail(device.visualPrefab);
                if (preview) GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit, true);
            }
            else GUI.Label(rect, "?", EditorStyles.boldLabel);
        }
        Sprite DeviceSprite(PrototypeDevice device)
        {
            if (RanchDevicesV13EditorPreview.Covers(map.theme, device))
                return RanchDevicesV13EditorPreview.TryDevice(map.theme, device, out var current, out _) ? current : null;
            var prefab = device.visualPrefab;
            if (!prefab) return null;
            var cacheKey = prefab.GetInstanceID() + ":" + device.initialVisual;
            if (legacyDeviceSprites.TryGetValue(cacheKey, out var cached)) return cached;
            var view = prefab.GetComponentInChildren<ANIMOL.Presentation.DeviceVisualView>(true);
            var state = view?.states?.FirstOrDefault(candidate => candidate.name == device.initialVisual) ??
                view?.states?.FirstOrDefault(candidate => candidate.name == view.initialState);
            if (state?.sprite) return CacheLegacyDeviceSprite(cacheKey, state.sprite);
            if (state?.sequence && state.sequence.frames != null && state.sequence.frames.Length > 0 && state.sequence.frames[0])
                return CacheLegacyDeviceSprite(cacheKey, state.sequence.frames[0]);
            return CacheLegacyDeviceSprite(cacheKey, prefab.GetComponentsInChildren<SpriteRenderer>(true)
                .Select(renderer => renderer.sprite).FirstOrDefault(candidate => candidate));
        }
        Sprite CacheLegacyDeviceSprite(string key, Sprite sprite) { legacyDeviceSprites[key] = sprite; return sprite; }
        void DrawCameraBounds(Rect world, int shownHeight, Vector2 origin)
        {
            var frame = new Rect(origin.x + (world.xMin + .5f) * cellSize,
                origin.y + (shownHeight - world.yMax - .5f) * cellSize,
                world.width * cellSize, world.height * cellSize);
            var color = new Color(.2f, .9f, 1f, .95f);
            EditorGUI.DrawRect(new Rect(frame.xMin, frame.yMin, frame.width, 3), color);
            EditorGUI.DrawRect(new Rect(frame.xMin, frame.yMax - 3, frame.width, 3), color);
            EditorGUI.DrawRect(new Rect(frame.xMin, frame.yMin, 3, frame.height), color);
            EditorGUI.DrawRect(new Rect(frame.xMax - 3, frame.yMin, 3, frame.height), color);
            var label = new Rect(frame.xMin + 4, frame.yMin + 4, 110, 18);
            EditorGUI.DrawRect(label, new Color(.04f, .13f, .2f, .85f));
            GUI.Label(label, "카메라 화면", EditorStyles.whiteMiniLabel);
        }
        void DrawConnections(int shownHeight, Vector2 origin)
        {
            var devices = map.scenario.devices.Where(device => map.Contains(device.position)).ToArray();
            Vector3 Center(Vector2Int p) => new Vector3(origin.x + (p.x + .5f) * cellSize,
                origin.y + (shownHeight - p.y - .5f) * cellSize);
            var previous = Handles.color;
            foreach (var group in devices.Where(device => !string.IsNullOrEmpty(device.group)).GroupBy(device => device.group))
            {
                var members = group.ToArray();
                if (members.Length < 2) continue;
                // A shared group is one logical connection. A hub shows every member
                // without drawing a quadratic number of duplicate lines.
                var hub = members.FirstOrDefault(device => device.id == selectedDeviceId) ?? members[0];
                foreach (var member in members)
                {
                    if (member == hub) continue;
                    Handles.color = hub.id == selectedDeviceId || member.id == selectedDeviceId
                        ? new Color(.2f, .9f, 1f, .95f) : new Color(.3f, .8f, 1f, .55f);
                    Handles.DrawAAPolyLine(3, Center(hub.position), Center(member.position));
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
                if (device.role == PrototypeRole.StarSocket && device.variant == "socket")
                {
                    var rewardCell = device.socketReward == StarSocketReward.RevealBubble ? device.socketBubbleCell :
                        devices.FirstOrDefault(d => d.id == device.socketBridgeId)?.position ?? device.position;
                    if (device.socketReward != StarSocketReward.None && map.Contains(rewardCell))
                        Handles.DrawDottedLine(Center(device.position), Center(rewardCell), 5);
                }
                foreach (var cell in device.cells ?? Enumerable.Empty<Vector2Int>())
                    if (map.Contains(cell)) Handles.DrawDottedLine(Center(device.position), Center(cell), 5);
                if (device.role == PrototypeRole.Bomb)
                    foreach (var offset in device.blastOffsets ?? Enumerable.Empty<Vector2Int>())
                        if (map.Contains(device.position + offset))
                            Handles.DrawSolidRectangleWithOutline(new Rect(origin.x + (device.position.x + offset.x) * cellSize,
                                origin.y + (shownHeight - device.position.y - offset.y - 1) * cellSize, cellSize, cellSize),
                                new Color(1f, .25f, .1f, .16f), new Color(1f, .35f, .1f, .9f));
            }
            Handles.color = previous;
        }
        void DrawRanchDecorationPreview(DecorationCell decor, int shownHeight, Vector2 origin, Rect visible)
        {
            var source = RanchConnectedDecorV02Host.SourceMapping(map, decor.sprite);
            var center = (Vector2)decor.position + new Vector2(
                source == null ? 0 : (source.width - 1) * .5f,
                source == null ? 0 : (source.height - 1) * .5f);
            var preview = decor.sprite == map.theme.starwayRanch.Find("props/windmill_body.png")
                ? map.theme.starwayRanch.Find("props/windmill.png") : decor.sprite;
            DrawWorldSpriteAt(center, preview ? preview : map.theme.decoration,
                shownHeight, origin, visible);
        }
        void DrawOffscreenRanchDecorationPreviews(int xMin, int xMax, int yMin, int yMax,
            int shownHeight, Vector2 origin, Rect visible)
        {
            if (!map.theme || !map.theme.starwayRanch) return;
            foreach (var decor in map.decorations)
            {
                if (!map.Contains(decor.position) || !string.IsNullOrEmpty(decor.visualFamily) ||
                    RanchConnectedDecorV02Host.TryMap(map, decor, out _)) continue;
                var row = shownHeight - 1 - decor.position.y;
                if (decor.position.x >= xMin && decor.position.x < xMax && row >= yMin && row < yMax) continue;
                DrawRanchDecorationPreview(decor, shownHeight, origin, visible);
            }
        }
        void DrawDeviceDirectionBadges(int xMin, int xMax, int yMin, int yMax,
            int shownHeight, Vector2 origin)
        {
            if (!map.scenario) return;
            var previous = Handles.color;
            foreach (var device in map.scenario.devices)
            {
                var p = device.position;
                var row = shownHeight - 1 - p.y;
                if (p.x < xMin || p.x >= xMax || row < yMin || row >= yMax ||
                    !PuzzleSession.IsCardinal(device.direction)) continue;
                var size = Mathf.Clamp(cellSize * .34f, 12f, 22f);
                var box = new Rect(origin.x + (p.x + 1) * cellSize - size - 2,
                    origin.y + row * cellSize + 2, size, size);
                EditorGUI.DrawRect(box, new Color(.02f, .08f, .14f, .94f));
                var vector = new Vector2(device.direction.x, -device.direction.y);
                var normal = new Vector2(-vector.y, vector.x);
                var tip = box.center + vector * (size * .32f);
                Handles.color = device.id == selectedDeviceId ? new Color(.2f, 1f, .9f) : Color.white;
                Handles.DrawAAPolyLine(3f, box.center - vector * (size * .25f), tip);
                Handles.DrawAAPolyLine(3f, tip - vector * (size * .18f) + normal * (size * .16f),
                    tip, tip - vector * (size * .18f) - normal * (size * .16f));
            }
            Handles.color = previous;
        }
        void DrawConnectedDecorPreview(int shownHeight, Vector2 origin, Rect visible)
        {
            if (!RanchConnectedDecorV02Host.IsRanch(map) || !map.theme.ranchConnectedDecorV02Catalog ||
                !map.theme.ranchConnectedDecorV02ManifestPin) return;
            var catalog = map.theme.ranchConnectedDecorV02Catalog;
            var pin = map.theme.ranchConnectedDecorV02ManifestPin.text.Trim();
            if (!previewCacheValid)
            {
                if (RanchConnectedDecorV02Host.TryCreateSnapshot(map, map.decorations,
                    new Vector3(-.5f, -.5f), 1, 0, out var snapshot, out _))
                    try
                    {
                        ranchPreview = RanchConnectedDecorV02Planner.CreatePlan(snapshot.networkCells, snapshot.groupPlacements,
                            key => {
                                if (!catalog.TryGetGroup(key, pin, out var group, out _, out var reason))
                                    throw new InvalidOperationException(reason);
                                return new RanchDecorGroupSize(group.WidthCells, group.HeightCells);
                            }, snapshot.CreateApprovedCellQuery(), snapshot.seed, snapshot.postSpacing);
                    }
                    catch (Exception) { ranchPreview = null; }
            }
            var plan = ranchPreview;
            if (plan == null) return;
            for (var i = 0; i < plan.NetworkCount; i++)
            {
                var row = plan.GetNetwork(i);
                var position = new Vector2(row.x, row.y);
                if (catalog.TryGetExact(row.file, pin, out var record, out _))
                    DrawWorldSpriteAt(position, record.Sprite, shownHeight, origin, visible);
                if (!string.IsNullOrEmpty(row.periodicPostFile) &&
                    catalog.TryGetExact(row.periodicPostFile, pin, out var post, out _))
                    DrawWorldSpriteAt(position, post.Sprite, shownHeight, origin, visible);
                if (!string.IsNullOrEmpty(row.flowerAccentFile) &&
                    catalog.TryGetExact(row.flowerAccentFile, pin, out var flower, out _))
                    DrawWorldSpriteAt(position, flower.Sprite, shownHeight, origin, visible);
            }
            for (var i = 0; i < plan.GroupCount; i++)
            {
                var row = plan.GetGroup(i);
                if (catalog.TryGetGroup(row.groupKey, pin, out _, out var art, out _))
                    DrawWorldSpriteAt(new Vector2((float)row.CenterX - .5f, (float)row.CenterY - .5f),
                        art.Sprite, shownHeight, origin, visible);
            }
        }
        void DrawTheaterConnectedDecorPreview(int shownHeight, Vector2 origin, Rect visible)
        {
            if (!TheaterConnectedDecorHost.IsTheater(map) || !map.theme.theaterConnectedDecorV01Catalog ||
                !map.theme.theaterConnectedDecorV01ManifestPin) return;
            var catalog = map.theme.theaterConnectedDecorV01Catalog;
            var pin = map.theme.theaterConnectedDecorV01ManifestPin.text.Trim();
            if (!previewCacheValid)
            {
                if (TheaterConnectedDecorHost.TryCreateSnapshot(map, map.decorations,
                    new Vector3(-.5f, -.5f), 1, 0, out var snapshot, out _))
                    try
                    {
                        theaterPreview = TheaterConnectedDecorV01Planner.CreatePlan(snapshot.networkCells, snapshot.groupPlacements,
                            key => {
                                if (!catalog.TryGetGroup(key, pin, out var group, out _, out var error))
                                    throw new InvalidOperationException(error);
                                return new TheaterDecorGroupSize(group.WidthCells, group.HeightCells);
                            }, snapshot.CreateApprovedCellQuery(), snapshot.seed, snapshot.postSpacing,
                            family => TheaterConnectedDecorV01Planner.SupportsPeriodicPost(family));
                    }
                    catch (Exception) { theaterPreview = null; }
            }
            var plan = theaterPreview;
            if (plan == null) return;
            for (var i = 0; i < plan.NetworkCount; i++)
            {
                var row = plan.GetNetwork(i);
                if (catalog.TryGetExact(row.file, pin, out var art, out _))
                    DrawWorldSpriteAt(new Vector2(row.x, row.y), art.Sprite, shownHeight, origin, visible);
                if (!string.IsNullOrEmpty(row.periodicPostFile) &&
                    catalog.TryGetExact(row.periodicPostFile, pin, out var post, out _))
                    DrawWorldSpriteAt(new Vector2(row.x, row.y), post.Sprite, shownHeight, origin, visible);
            }
            for (var i = 0; i < plan.GroupCount; i++)
            {
                var row = plan.GetGroup(i);
                if (catalog.TryGetGroup(row.groupKey, pin, out _, out var art, out _))
                    DrawWorldSpriteAt(new Vector2((float)row.CenterX - .5f, (float)row.CenterY - .5f),
                        art.Sprite, shownHeight, origin, visible);
            }
        }
        void DrawSanctumConnectedDecorPreview(int shownHeight, Vector2 origin, Rect visible)
        {
            if (!SanctumConnectedDecorHost.IsSanctum(map) || !map.theme.sanctumConnectedDecorV01Catalog ||
                !map.theme.sanctumConnectedDecorV01ManifestPin) return;
            var catalog = map.theme.sanctumConnectedDecorV01Catalog;
            var pin = map.theme.sanctumConnectedDecorV01ManifestPin.text.Trim();
            if (!previewCacheValid)
            {
                if (SanctumConnectedDecorHost.TryCreateSnapshot(map, map.decorations,
                    new Vector3(-.5f, -.5f), 1, 0, out var snapshot, out _))
                    try
                    {
                        sanctumPreview = SanctumConnectedDecorV01Planner.CreatePlan(snapshot.networkCells, snapshot.groupPlacements,
                            key => {
                                if (!catalog.TryGetGroup(key, pin, out var group, out _, out var error))
                                    throw new InvalidOperationException(error);
                                return new SanctumDecorGroupSize(group.WidthCells, group.HeightCells);
                            }, snapshot.CreateApprovedCellQuery(), snapshot.seed, snapshot.postSpacing,
                            SanctumConnectedDecorV01Planner.SupportsPeriodicPost);
                    }
                    catch (Exception) { sanctumPreview = null; }
            }
            var plan = sanctumPreview;
            if (plan == null) return;
            for (var i = 0; i < plan.NetworkCount; i++)
            {
                var row = plan.GetNetwork(i);
                if (catalog.TryGetExact(row.file, pin, out var art, out _))
                    DrawWorldSpriteAt(new Vector2(row.x, row.y), art.Sprite, shownHeight, origin, visible);
                if (!string.IsNullOrEmpty(row.periodicPostFile) &&
                    catalog.TryGetExact(row.periodicPostFile, pin, out var post, out _))
                    DrawWorldSpriteAt(new Vector2(row.x, row.y), post.Sprite, shownHeight, origin, visible);
            }
            for (var i = 0; i < plan.GroupCount; i++)
            {
                var row = plan.GetGroup(i);
                if (catalog.TryGetGroup(row.groupKey, pin, out _, out var art, out _))
                    DrawWorldSpriteAt(new Vector2((float)row.CenterX - .5f, (float)row.CenterY - .5f),
                        art.Sprite, shownHeight, origin, visible);
            }
        }
        void DrawCoastConnectedDecorPreview(int shownHeight, Vector2 origin, Rect visible)
        {
            if (!CoastConnectedDecorHost.IsCoast(map) || !map.theme.coastConnectedDecorV01Catalog ||
                !map.theme.coastConnectedDecorV01ManifestPin) return;
            var catalog = map.theme.coastConnectedDecorV01Catalog;
            var pin = map.theme.coastConnectedDecorV01ManifestPin.text.Trim();
            if (!previewCacheValid)
            {
                if (CoastConnectedDecorHost.TryCreateSnapshot(map, map.decorations,
                    new Vector3(-.5f, -.5f), 1, 0, out var snapshot, out _))
                    try
                    {
                        coastPreview = CoastConnectedDecorV01Planner.CreatePlan(snapshot.networkCells, snapshot.groupPlacements,
                            key => {
                                if (!catalog.TryGetGroup(key, pin, out var group, out _, out var error))
                                    throw new InvalidOperationException(error);
                                return new CoastDecorGroupSize(group.WidthCells, group.HeightCells);
                            }, snapshot.CreateApprovedCellQuery(), snapshot.seed, snapshot.postSpacing,
                            CoastConnectedDecorV01Planner.SupportsPeriodicPost);
                    }
                    catch (Exception) { coastPreview = null; }
            }
            var plan = coastPreview;
            if (plan == null) return;
            for (var i = 0; i < plan.NetworkCount; i++)
            {
                var row = plan.GetNetwork(i);
                if (catalog.TryGetExact(row.file, pin, out var art, out _))
                    DrawWorldSpriteAt(new Vector2(row.x, row.y), art.Sprite, shownHeight, origin, visible);
                if (!string.IsNullOrEmpty(row.periodicPostFile) &&
                    catalog.TryGetExact(row.periodicPostFile, pin, out var post, out _))
                    DrawWorldSpriteAt(new Vector2(row.x, row.y), post.Sprite, shownHeight, origin, visible);
            }
            for (var i = 0; i < plan.GroupCount; i++)
            {
                var row = plan.GetGroup(i);
                if (catalog.TryGetGroup(row.groupKey, pin, out _, out var art, out _))
                    DrawWorldSpriteAt(new Vector2((float)row.CenterX - .5f, (float)row.CenterY - .5f),
                        art.Sprite, shownHeight, origin, visible);
            }
        }
        static void DrawSprite(Rect r, Sprite sprite)
        { if (!sprite) return; var t = sprite.texture; var s = sprite.rect; GUI.DrawTextureWithTexCoords(r, t, new Rect(s.x / t.width, s.y / t.height, s.width / t.width, s.height / t.height)); }
        static void DrawSpriteFitted(Rect bounds, Sprite sprite)
        {
            if (!sprite) return;
            var rect = sprite.rect;
            var scale = Mathf.Min(bounds.width / rect.width, bounds.height / rect.height);
            var width = rect.width * scale;
            var height = rect.height * scale;
            DrawSprite(new Rect(bounds.center.x - width * .5f, bounds.center.y - height * .5f,
                width, height), sprite);
        }
        void DrawWorldSprite(Rect cell, Sprite sprite)
        { DrawWorldSprite(cell.center, sprite); }
        void DrawWorldSpriteAt(Vector2 position, Sprite sprite, int shownHeight, Vector2 origin, Rect visible)
        {
            if (!sprite) return;
            var pivot = new Vector2(origin.x + (position.x + .5f) * cellSize,
                origin.y + (shownHeight - position.y - .5f) * cellSize);
            var rect = WorldSpriteRect(pivot, sprite);
            if (rect.Overlaps(visible)) DrawSprite(rect, sprite);
        }
        void DrawWorldSprite(Vector2 screenPivot, Sprite sprite)
        {
            if (!sprite) return;
            DrawSprite(WorldSpriteRect(screenPivot, sprite), sprite);
        }
        Rect WorldSpriteRect(Vector2 screenPivot, Sprite sprite)
        {
            var pixelsToGrid = cellSize / sprite.pixelsPerUnit;
            var rect = sprite.rect;
            return new Rect(screenPivot.x - sprite.pivot.x * pixelsToGrid,
                screenPivot.y - (rect.height - sprite.pivot.y) * pixelsToGrid,
                rect.width * pixelsToGrid, rect.height * pixelsToGrid);
        }
        void DrawThemedUndersides(string[] theaterRows, int shownHeight, Vector2 origin, Rect visible)
        {
            var theme = map.theme;
            if (theaterRows != null)
            {
                foreach (var finish in EchoTheaterV03MaterialSelector.GetBottomFinishes(theaterRows))
                    DrawWorldSpriteAt(new Vector2(finish.CenterCellX, map.Height - 1 - finish.CenterCellY),
                        theme.memorySkyTheater.v02.Find(finish.File), shownHeight, origin, visible);
                return;
            }
            if (theme.sunsetSanctumV02Catalog)
            {
                char MaterialAt(int x, int y) => map.TryTerrain(new Vector2Int(x, y), out var tile) ?
                    tile.material == TerrainMaterial.Stone ? 'S' : tile.material == TerrainMaterial.Sand ? 'W' : 'G' : '0';
                foreach (var run in SunsetSanctumV02UndersideSelector.SelectRuns(MaterialAt, 0, 0,
                    map.Width - 1, map.Height - 1))
                    foreach (var placement in SunsetSanctumV02UndersideSelector.Plan(run, 0))
                        if (theme.sunsetSanctumV02Catalog.TryGetPart(placement.file,
                            SunsetSanctumV02PuzzleBinding.ManifestSha256, out var part, out _))
                            DrawWorldSpriteAt(new Vector2(placement.logicalCenterX - .5f, placement.logicalCenterY - .5f),
                                part.Frame.Sprite, shownHeight, origin, visible);
                return;
            }
            if (theme.starSwellCoastV01Catalog)
            {
                char MaterialAt(int x, int y) => map.TryTerrain(new Vector2Int(x, y), out var tile) ?
                    tile.material == TerrainMaterial.Stone ? 'S' : tile.material == TerrainMaterial.Sand ? 'W' : 'A' : '0';
                foreach (var run in StarSwellCoastV01UndersideSelector.SelectRuns(MaterialAt, 0, 0,
                    map.Width - 1, map.Height - 1))
                    foreach (var placement in StarSwellCoastV01UndersideSelector.Plan(run, 0))
                        if (theme.starSwellCoastV01Catalog.TryGetPart(placement.file,
                            StarSwellCoastV01PuzzleBinding.ManifestSha256, out var part, out _))
                            DrawWorldSpriteAt(new Vector2(placement.logicalCenterX - .5f, placement.logicalCenterY - .5f),
                                part.Frame.Sprite, shownHeight, origin, visible);
            }
        }
        void Paint(Vector2Int p, bool remove)
        {
            if (layer == Layer.Terrain) { if (remove) map.RemoveTerrain(p); else map.SetTerrain(p, material, blocked); }
            if (layer == Layer.Device)
            {
                if (map.scenario)
                {
                    Undo.RecordObject(map.scenario, "테마 장치 배치");
                    if (remove)
                    {
                        if (!TryEraseRanchArt(map, p)) map.scenario.devices.RemoveAll(d => d.position == p);
                        if (selectedRanchArt != null && !map.scenario.ranchV13Art.Contains(selectedRanchArt)) selectedRanchArt = null;
                    }
                    else if (ranchArtBrush != null)
                    {
                        if (!TryPlaceRanchArt(map, ranchArtBrush, p, out ranchArtError)) return;
                        selectedRanchArt = map.scenario.ranchV13Art[map.scenario.ranchV13Art.Count - 1];
                    }
                    else if (prototypeBrush != null && !string.IsNullOrEmpty(ThemeId(map.theme)) &&
                        (string.IsNullOrEmpty(prototypeBrush.artTheme) || ThemeId(map.theme) == prototypeBrush.artTheme) &&
                        !map.scenario.ranchV13Art.Any(art => art != null && art.Footprint().Contains(p)) &&
                        !map.scenario.devices.Any(d => d.position == p && d.role == prototypeBrush.role && d.variant == prototypeBrush.variant))
                        selectedDeviceId = StampTemplate(map.scenario, prototypeBrush, p).id;
                    EditorUtility.SetDirty(map.scenario);
                }
                else if (remove) map.SetDevice(new DeviceCell { position = p, kind = DeviceKind.None });
            }
            if (layer == Layer.Decoration)
            {
                if (remove) { TryEraseDecoration(map, p); decorationError = null; }
                else if (decoration && !TryPlaceDecoration(map, decoration, p, decorationTint,
                    decorationConnectionGroup, decorationLogicalLayer, decorationHeightKey, out decorationError)) return;
            }
            if (layer == Layer.Markers)
            {
                if (marker == Marker.Bubble) { map.bubbles.RemoveAll(b => b == p); if (!remove) map.bubbles.Add(p); }
                else if (!remove) { if (marker == Marker.Start) map.start = p; else map.exit = p; }
            }
            if (layer == Layer.Water && map.scenario)
            {
                Undo.RecordObject(map.scenario, "물 칸 배치");
                if (remove) map.scenario.water.RemoveAll(cell => cell == p);
                else if (!map.scenario.water.Contains(p)) map.scenario.water.Add(p);
                EditorUtility.SetDirty(map.scenario);
                map.NotifyDecorationsChanged();
            }
            EditorUtility.SetDirty(map); map.InvalidateIndex();
        }
        public static bool TryEraseDecoration(PuzzleMap target, Vector2Int p)
        {
            var index = target.decorations.FindIndex(cell =>
                RanchConnectedDecorV02Host.Occupies(target, cell, p) || TheaterConnectedDecorHost.Occupies(target, cell, p) ||
                SanctumConnectedDecorHost.Occupies(target, cell, p) || CoastConnectedDecorHost.Occupies(target, cell, p));
            if (index < 0) return false;
            target.decorations.RemoveAt(index);
            target.NotifyDecorationsChanged();
            return true;
        }
        public static bool TryPlaceDecoration(PuzzleMap target, Sprite sprite, Vector2Int anchor, Color tint,
            string connectionGroup, int logicalLayer, int heightKey, out string error)
        {
            error = null;
            if (!target || !sprite) { error = "맵 또는 장식 그림이 없습니다."; return false; }
            if (SanctumConnectedDecorHost.IsSanctum(target) &&
                SanctumConnectedDecorHost.IsLegacySanctumProp(target.theme, sprite))
            { error = "기존 V02 장식은 새 V01 지면 크기와 배치 위치를 검토한 뒤 이관해야 합니다."; return false; }
            if (CoastConnectedDecorHost.IsCoast(target) && CoastConnectedDecorHost.IsLegacyCoastProp(target.theme, sprite))
            { error = "기존 해안 장식은 새 지면 크기와 배치 위치를 검토한 뒤 이관해야 합니다."; return false; }
            var source = RanchConnectedDecorV02Host.SourceMapping(target, sprite);
            var theaterSource = TheaterConnectedDecorHost.IsTheater(target)
                ? TheaterConnectedDecorHost.SourceMapping(target.theme, sprite) : null;
            var sanctumSource = SanctumConnectedDecorHost.IsSanctum(target)
                ? SanctumConnectedDecorHost.SourceMapping(target.theme, sprite) : null;
            var coastSource = CoastConnectedDecorHost.IsCoast(target)
                ? CoastConnectedDecorHost.SourceMapping(target.theme, sprite) : null;
            var width = theaterSource != null ? theaterSource.width :
                sanctumSource != null ? sanctumSource.width : coastSource != null ? coastSource.width : source == null ? 1 : source.occupiedWidth;
            var height = theaterSource != null ? theaterSource.height :
                sanctumSource != null ? sanctumSource.height : coastSource != null ? coastSource.height : source == null ? 1 : source.occupiedHeight;
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
            {
                var p = anchor + new Vector2Int(x, y);
                if (!target.Contains(p)) { error = "장식의 전체 지면이 맵 안에 들어와야 합니다."; return false; }
                if (target.decorations.Any(cell => RanchConnectedDecorV02Host.Occupies(target, cell, p) ||
                    TheaterConnectedDecorHost.Occupies(target, cell, p) || SanctumConnectedDecorHost.Occupies(target, cell, p) ||
                    CoastConnectedDecorHost.Occupies(target, cell, p)))
                { error = "장식의 지면이 다른 장식과 겹칩니다. 먼저 기존 장식을 지우세요."; return false; }
                if (source != null && source.IsVisual && !RanchConnectedDecorV02Host.AllowDecorCell(target, target.decorations, p))
                { error = "All Ranch decoration ground cells must be allowed."; return false; }
                if (theaterSource != null && !TheaterConnectedDecorHost.AllowDecorCell(target, target.decorations, p))
                { error = "극장 장식은 기존 다리 난간이나 장식이 소유한 칸에 배치할 수 없습니다."; return false; }
                if (sanctumSource != null && !SanctumConnectedDecorHost.AllowDecorCell(target, target.decorations, p))
                { error = "성소 장식은 기존 다리 난간이나 장식이 소유한 칸에 배치할 수 없습니다."; return false; }
                if (coastSource != null && !CoastConnectedDecorHost.AllowDecorCell(target, target.decorations, p))
                { error = "해안 장식은 물칸, 다리 난간, 다른 장식이 소유한 칸에 배치할 수 없습니다."; return false; }
            }
            var mapped = source != null && (source.family != null || source.groupKey != null);
            var cellToAdd = new DecorationCell { position = anchor, sprite = sprite,
                tint = mapped || theaterSource != null || sanctumSource != null || coastSource != null ? Color.white : tint, placementId = Guid.NewGuid().ToString("N"),
                visualFamily = theaterSource != null ? theaterSource.key : sanctumSource != null ? sanctumSource.key :
                    coastSource != null ? coastSource.key :
                    mapped ? source.Key : null,
                connectionGroup = theaterSource != null && theaterSource.network ?
                    (string.IsNullOrWhiteSpace(connectionGroup) ? theaterSource.key : connectionGroup.Trim()) :
                    sanctumSource != null && sanctumSource.network ?
                        (string.IsNullOrWhiteSpace(connectionGroup) ? sanctumSource.key : connectionGroup.Trim()) :
                    coastSource != null && coastSource.network ?
                        (string.IsNullOrWhiteSpace(connectionGroup) ? coastSource.key : connectionGroup.Trim()) :
                    mapped && !source.IsGroup ?
                        (string.IsNullOrWhiteSpace(connectionGroup) ? source.family : connectionGroup.Trim()) : null,
                logicalLayer = logicalLayer, heightKey = heightKey };
            if (mapped)
            {
                var theme = target.theme;
                if (!theme.ranchConnectedDecorV02Catalog || !theme.ranchConnectedDecorV02ManifestPin)
                { error = "새 목장 장식 catalog 또는 manifest pin이 없습니다."; return false; }
                var candidate = new List<DecorationCell>(target.decorations) { cellToAdd };
                if (!RanchConnectedDecorV02Host.TryCreateSnapshot(target, candidate,
                    new Vector3(-.5f, -.5f), 1, 0, out _, out error)) return false;
            }
            if (theaterSource != null)
            {
                var candidate = new List<DecorationCell>(target.decorations) { cellToAdd };
                if (!TheaterConnectedDecorHost.TryCreateSnapshot(target, candidate,
                    new Vector3(-.5f, -.5f), 1, 0, out _, out error)) return false;
            }
            if (sanctumSource != null)
            {
                var candidate = new List<DecorationCell>(target.decorations) { cellToAdd };
                if (!SanctumConnectedDecorHost.TryCreateSnapshot(target, candidate,
                    new Vector3(-.5f, -.5f), 1, 0, out _, out error)) return false;
            }
            if (coastSource != null)
            {
                var candidate = new List<DecorationCell>(target.decorations) { cellToAdd };
                if (!CoastConnectedDecorHost.TryCreateSnapshot(target, candidate,
                    new Vector3(-.5f, -.5f), 1, 0, out _, out error)) return false;
            }
            target.decorations.Add(cellToAdd);
            target.NotifyDecorationsChanged();
            return true;
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
                target = template.role == PrototypeRole.Portal ? template.target + shift :
                    template.role == PrototypeRole.Chick ? Vector2Int.zero : template.target,
                orientation = template.orientation, number = template.number, active = template.active,
                socketReward = template.socketReward, socketBridgeId = template.socketBridgeId,
                socketBubbleCell = template.socketBubbleCell + shift,
                cells = (template.cells ?? new List<Vector2Int>()).Select(cell => cell + shift).ToList(),
                blastOffsets = (template.blastOffsets ?? new List<Vector2Int>()).ToList()
            };
            destination.devices.Add(copy);
            return copy;
        }
        void EndStroke() { if (stroke) Undo.CollapseUndoOperations(undoGroup); stroke = false; lastPaint = new Vector2Int(-999, -999); }
        void Create()
        {
            if (!newMapTheme) { EditorUtility.DisplayDialog("테마 선택", "새 맵에 사용할 테마를 지정하세요.", "확인"); return; }
            var name = newMapName?.Trim();
            if (string.IsNullOrEmpty(name) || name == "." || name == ".." || name.EndsWith(".", StringComparison.Ordinal) ||
                name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                EditorUtility.DisplayDialog("맵 이름 확인", "파일 이름으로 사용할 수 있는 맵 이름을 입력하세요.", "확인");
                return;
            }
            var path = "Assets/ANIMOL/Data/Puzzle/Maps/" + name + ".asset";
            try { Select(CreateStarterMap(path, newMapTheme, newMapWidth, newMapHeight)); }
            catch (InvalidOperationException) { EditorUtility.DisplayDialog("맵 생성 실패", "같은 이름의 맵이 이미 있습니다. 다른 이름을 입력하세요.", "확인"); }
            catch (Exception error) { EditorUtility.DisplayDialog("맵 생성 실패", error.Message, "확인"); }
        }
        void NewMapSettings()
        {
            newMapName = EditorGUILayout.TextField("맵 이름", newMapName);
            newMapWidth = Mathf.Clamp(EditorGUILayout.IntField("가로 칸 수", newMapWidth), 4, 256);
            newMapHeight = Mathf.Clamp(EditorGUILayout.IntField("세로 칸 수", newMapHeight), 4, 256);
            var themePaths = new[] { "Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset",
                "Assets/ANIMOL/Data/Puzzle/Prototypes/memory_theater_Theme.asset",
                "Assets/ANIMOL/Data/Puzzle/Prototypes/letter_observatory_Theme.asset",
                "Assets/ANIMOL/Data/Puzzle/Prototypes/twilight_sea_Theme.asset" };
            var themes = themePaths.Select(AssetDatabase.LoadAssetAtPath<PuzzleTheme>).ToArray();
            var index = Array.IndexOf(themes, newMapTheme);
            index = EditorGUILayout.Popup("테마", Mathf.Max(0, index), new[] { "별길 목장", "잔영 극장", "편지 천문대", "별너울 해안" });
            newMapTheme = themes[index];
            EditorGUILayout.HelpBox("선택한 크기로 풀 지형 맵을 만듭니다. 크기는 생성 후에도 맵 설정에서 바꿀 수 있습니다. 장치 데이터는 자동으로 준비됩니다.", MessageType.Info);
        }
        public static PuzzleMap CreateStarterMap(string path, PuzzleTheme theme, int width, int height)
        {
            if (!theme) throw new ArgumentException("퍼즐 테마가 필요합니다.");
            var themeId = ThemeId(theme);
            if (string.IsNullOrEmpty(themeId)) throw new ArgumentException("지원하는 네 가지 퍼즐 테마 중 하나를 선택하세요.");
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) ||
                !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("프로젝트 안에서 .asset으로 끝나는 저장 경로를 선택하세요.");
            var scenarioPath = path.Substring(0, path.Length - ".asset".Length) + "_Scenario.asset";
            if (AssetDatabase.LoadMainAssetAtPath(path) || AssetDatabase.LoadMainAssetAtPath(scenarioPath))
                throw new InvalidOperationException("맵 또는 연결된 시나리오의 저장 경로가 이미 사용 중입니다.");
            width = Mathf.Clamp(width, 4, 256); height = Mathf.Clamp(height, 4, 256);
            var scenario = CreateInstance<PuzzleScenario>();
            scenario.title = Path.GetFileNameWithoutExtension(path);
            scenario.themeId = themeId;
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
            if (GUILayout.Button("퍼즐 맵 제작기 열기")) PuzzleMapEditor.Edit((PuzzleMap)target);
            EditorGUILayout.HelpBox("맵 제작기에서 지형, 장치, 장식과 표식을 배치하고 크기를 변경할 수 있습니다.", MessageType.Info);
            var map = (PuzzleMap)target;
            EditorGUILayout.LabelField("테마", map.theme ? PuzzleMapEditor.ThemeName(PuzzleMapEditor.ThemeId(map.theme)) : "없음");
            EditorGUILayout.LabelField("크기", map.Width + " × " + map.Height);
            var count = map.OutsideCount(map.Width, map.Height);
            if (count > 0) EditorGUILayout.HelpBox("맵 밖 배치 " + count + "개가 보관되어 있습니다.", MessageType.Warning);
        }
    }
}
