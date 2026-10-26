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
        [SerializeField] string newMapName = "NewMap";
        Layer layer;
        Marker marker;
        TerrainMaterial material;
        PrototypeDevice prototypeBrush;
        DeviceTemplate[] library = Array.Empty<DeviceTemplate>();
        bool useLibrary = true, inspectDevice, showConnections = true, linkTargetMode, scenarioCreationQueued;
        int templateIndex;
        string selectedDeviceId;
        string scenarioCreationError;
        bool showScenario;
        Sprite decoration;
        Color decorationTint = Color.white;
        bool blocked, erase, stroke;
        int pendingWidth, pendingHeight, undoGroup;
        float cellSize = 42;
        Vector2 gridScroll, settingsScroll, deviceListScroll;
        Vector2Int lastPaint = new Vector2Int(-999, -999);
        [MenuItem("ANIMOL/퍼즐/맵 제작기")]
        public static void Open() => GetWindow<PuzzleMapEditor>("퍼즐 맵 제작기");
        public static void Edit(PuzzleMap data) { var window = GetWindow<PuzzleMapEditor>("퍼즐 맵 제작기"); window.Select(data); }
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
            EndStroke(); map = data; selectedDeviceId = null; linkTargetMode = false; scenarioCreationError = null;
            if (map) { pendingWidth = map.Width; pendingHeight = map.Height; if (map.theme) newMapTheme = map.theme; }
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
            EditorGUILayout.LabelField("맵 설정", EditorStyles.boldLabel);
            pendingWidth = EditorGUILayout.IntField("가로 칸 수", pendingWidth);
            pendingHeight = EditorGUILayout.IntField("세로 칸 수", pendingHeight);
            if (GUILayout.Button("크기 적용 (잘린 배치 보관)") && Resize(map, pendingWidth, pendingHeight, true)) { pendingWidth = map.Width; pendingHeight = map.Height; }
            var outside = map.OutsideCount(map.Width, map.Height);
            if (outside > 0) EditorGUILayout.HelpBox("맵 밖 배치 " + outside + "개가 비활성 상태로 보관됩니다. 맵을 넓히면 복구됩니다. 시작점·출구가 밖에 있으면 미리보기를 실행할 수 없습니다.", MessageType.Warning);
            var previousTheme = map.theme;
            var serialized = new SerializedObject(map); serialized.Update();
            EditorGUILayout.PropertyField(serialized.FindProperty("theme"), new GUIContent("테마"));
            EditorGUILayout.PropertyField(serialized.FindProperty("targetBubbles"), new GUIContent("목표 방울 수"));
            DrawCameraSettings(serialized.FindProperty("cameraSettings"));
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
                    DrawVectorArray(scenario.FindProperty("water"), "물 칸");
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
            if (layer == Layer.Decoration)
            { decoration = (Sprite)EditorGUILayout.ObjectField("장식 그림", decoration, typeof(Sprite), false); decorationTint = EditorGUILayout.ColorField("색상", decorationTint); }
            if (layer == Layer.Markers) marker = (Marker)EditorGUILayout.IntPopup("배치할 표식", (int)marker, new[] { "시작점", "방울", "출구" }, new[] { 0, 1, 2 });
            if (layer == Layer.Water && !map.scenario)
            {
                QueueScenarioCreation();
                EditorGUILayout.HelpBox(scenarioCreationError ?? (string.IsNullOrEmpty(ThemeId(map.theme)) ?
                    "물 칸을 배치하려면 테마를 선택하세요." : "물 칸 데이터를 자동으로 준비하는 중입니다."),
                    scenarioCreationError == null ? MessageType.Info : MessageType.Error);
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("배치 검사", EditorStyles.boldLabel);
            var errors = map.ValidateMap();
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
                "Portal destination outside map: ", "Moving stage needs a rail: " };
            var replacements = new[] { "방울 칸에 통행 가능한 지형이 없습니다: ", "장치 칸에 통행 가능한 지형이 없습니다: ", "포털 목적지가 맵 밖이거나 막혔습니다: ",
                "장치 방향은 상하좌우여야 합니다: ", "방향이 올바르지 않습니다: ", "폭탄의 폭발 범위를 지정하세요: ",
                "포털 목적지가 맵 밖입니다: ", "이동 발판의 선로가 필요합니다: " };
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
                (string.IsNullOrEmpty(entry.device.artTheme) || entry.device.artTheme == themeId)).ToArray();
        public static string ThemeName(string id) => id == "starway" ? "별길 목장" : id == "memory_theater" ? "잔영 극장" :
            id == "letter_observatory" ? "편지 천문대" : id == "twilight_sea" ? "별너울 해안" : "미지정";
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
                case PrototypeRole.Wind: return "바람";
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
            variant == "field" ? "바람 칸" : variant == "blower" ? "송풍기" : variant == "square" ? "사각형" :
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
        void RefreshLibrary() { library = LoadPracticeTemplates(); templateIndex = 0; Repaint(); }
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
                    (string.IsNullOrEmpty(entry.artTheme) || entry.artTheme == themeId)).ToArray();
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
                    if (GUI.Button(row, GUIContent.none, EditorStyles.helpBox)) templateIndex = i;
                    if (i == templateIndex) EditorGUI.DrawRect(new Rect(row.x, row.y, 4, row.height), new Color(.2f, .8f, 1f));
                    DrawDeviceThumbnail(new Rect(row.x + 8, row.y + 7, 48, 48), choices[i]);
                    GUI.Label(new Rect(row.x + 62, row.y + 9, row.width - 66, 25), DeviceLabel(choices[i]), EditorStyles.boldLabel);
                    GUI.Label(new Rect(row.x + 62, row.y + 34, row.width - 66, 24),
                        useLibrary ? "방향 " + DirectionGlyph(choices[i].direction) + " · " + (choices[i].visualPrefab ? "그림 있음" : "그림 없음") :
                            "장치 ID: " + choices[i].id, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndScrollView();
                prototypeBrush = choices[templateIndex];
                if (useLibrary && GUILayout.Button("원본 장치 에셋 보기")) EditorGUIUtility.PingObject(templates[templateIndex].source);
            }
            if (GUILayout.Button("장치 목록 새로고침")) RefreshLibrary();
            EditorGUILayout.HelpBox("장치를 배치하면 새 ID가 만들어집니다. 연결 그룹과 목표 조건을 확인하세요. 포털 목적지와 선로는 배치한 위치만큼 이동합니다.", MessageType.Info);
            var selected = map.scenario.devices.FindIndex(device => device.id == selectedDeviceId);
            if (selected < 0) return;
            EditorGUILayout.LabelField("선택한 장치", EditorStyles.boldLabel);
            var placed = map.scenario.devices[selected];
            EditorGUILayout.LabelField(placed.id + " / " + DeviceName(placed.role));
            EditorGUILayout.LabelField("바라보는 방향");
            EditorGUILayout.BeginHorizontal();
            foreach (var heading in PuzzleAutotile.Cardinal)
                if (GUILayout.Button(DirectionGlyph(heading), GUILayout.Width(42)) && placed.direction != heading)
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
            if (placed.cells != null && placed.cells.Count > 0) EditorGUILayout.LabelField("점유·선로 칸", placed.cells.Count.ToString());
            if (placed.role == PrototypeRole.Bomb) EditorGUILayout.LabelField("폭발 범위", (placed.blastOffsets?.Count ?? 0).ToString());
            var serialized = new SerializedObject(map.scenario); serialized.Update();
            DrawDeviceProperties(serialized.FindProperty("devices").GetArrayElementAtIndex(selected));
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
            if (confirm && count > 0 && !EditorUtility.DisplayDialog("맵 밖 배치 보관", count + "개 배치가 새 맵의 바깥에 남습니다. 데이터는 삭제되지 않으며 맵을 넓히면 복구됩니다. 시작점과 출구가 밖에 있다면 플레이 전에 옮겨야 합니다.", "크기 변경", "취소")) return false;
            Undo.RegisterCompleteObjectUndo(data, "퍼즐 맵 크기 변경"); data.Resize(width, height); EditorUtility.SetDirty(data); return true;
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
                var d = map.DeviceAt(p);
                if (d.kind != DeviceKind.None)
                    GUI.Label(rect, (d.kind == DeviceKind.Portal ? "문" : d.kind == DeviceKind.Conveyor ? "무" :
                        d.kind == DeviceKind.Wind ? "바" : d.kind == DeviceKind.Ice ? "얼" : "멈") + DirectionGlyph(d.direction));
                if (map.scenario) foreach (var configured in map.scenario.devices.Where(a => a.position == p)) DrawDevice(rect, configured);
                if (map.bubbles.Contains(p)) GUI.Label(rect, "방", EditorStyles.boldLabel);
                if (map.start == p) GUI.Label(rect, "시", EditorStyles.boldLabel);
                if (map.exit == p) GUI.Label(new Rect(rect.x + cellSize * .5f, rect.y, cellSize * .5f, cellSize), "출", EditorStyles.boldLabel);
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
                                Undo.RecordObject(map.scenario, "퍼즐 장치 연결");
                                ConnectDevices(map.scenario, source, clicked);
                                EditorUtility.SetDirty(map.scenario);
                                linkTargetMode = false;
                            }
                        }
                        else if (e.type == EventType.MouseDown) selectedDeviceId = clicked?.id;
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
        static string DirectionGlyph(Vector2Int d) => d == Vector2Int.up ? "^" : d == Vector2Int.right ? ">" : d == Vector2Int.down ? "v" : "<";
        void DrawDevice(Rect cell, PrototypeDevice device)
        {
            var inset = cell.width * .12f;
            var visual = new Rect(cell.x + inset, cell.y + inset, cell.width - inset * 2, cell.height - inset * 2);
            DrawDeviceThumbnail(visual, device);
            var selected = device.id == selectedDeviceId;
            var strip = new Rect(cell.x, cell.y + cell.height * .72f, cell.width, cell.height * .28f);
            EditorGUI.DrawRect(strip, selected ? new Color(.1f, .55f, .9f, .9f) : new Color(.04f, .07f, .12f, .85f));
            GUI.Label(strip, ((int)device.role).ToString("00") + " " + DirectionGlyph(device.direction), EditorStyles.whiteMiniLabel);
            if (selected) Handles.DrawSolidRectangleWithOutline(cell, Color.clear, new Color(.2f, .8f, 1f));
            if (!device.visualPrefab) GUI.Label(new Rect(cell.x, cell.y, cell.width, cell.height * .3f), "그림 없음", EditorStyles.whiteMiniLabel);
        }
        static void DrawDeviceThumbnail(Rect rect, PrototypeDevice device)
        {
            var sprite = DeviceSprite(device);
            if (sprite) DrawSprite(rect, sprite);
            else if (device.visualPrefab)
            {
                var preview = AssetPreview.GetAssetPreview(device.visualPrefab) ?? AssetPreview.GetMiniThumbnail(device.visualPrefab);
                if (preview) GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit, true);
            }
            else GUI.Label(rect, "?", EditorStyles.boldLabel);
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
                    Undo.RecordObject(map.scenario, "테마 장치 배치");
                    if (remove) map.scenario.devices.RemoveAll(d => d.position == p);
                    else if (prototypeBrush != null && !string.IsNullOrEmpty(ThemeId(map.theme)) &&
                        (string.IsNullOrEmpty(prototypeBrush.artTheme) || ThemeId(map.theme) == prototypeBrush.artTheme) &&
                        !map.scenario.devices.Any(d => d.position == p && d.role == prototypeBrush.role && d.variant == prototypeBrush.variant))
                        selectedDeviceId = StampTemplate(map.scenario, prototypeBrush, p).id;
                    EditorUtility.SetDirty(map.scenario);
                }
                else if (remove) map.SetDevice(new DeviceCell { position = p, kind = DeviceKind.None });
            }
            if (layer == Layer.Decoration) { map.decorations.RemoveAll(d => d.position == p); if (!remove) map.decorations.Add(new DecorationCell { position = p, sprite = decoration, tint = decorationTint }); }
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
            if (!newMapTheme) { EditorUtility.DisplayDialog("테마 선택", "새 맵에 사용할 테마를 지정하세요.", "확인"); return; }
            var name = newMapName?.Trim();
            if (string.IsNullOrEmpty(name) || name == "." || name == ".." || name.EndsWith(".", StringComparison.Ordinal) ||
                name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                EditorUtility.DisplayDialog("맵 이름 확인", "파일 이름으로 사용할 수 있는 맵 이름을 입력하세요.", "확인");
                return;
            }
            var path = "Assets/ANIMOL/Data/Puzzle/Maps/" + name + ".asset";
            try { Select(CreateStarterMap(path, newMapTheme, 12, 18)); }
            catch (InvalidOperationException) { EditorUtility.DisplayDialog("맵 생성 실패", "같은 이름의 맵이 이미 있습니다. 다른 이름을 입력하세요.", "확인"); }
            catch (Exception error) { EditorUtility.DisplayDialog("맵 생성 실패", error.Message, "확인"); }
        }
        void NewMapSettings()
        {
            newMapName = EditorGUILayout.TextField("맵 이름", newMapName);
            var themePaths = new[] { "Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset",
                "Assets/ANIMOL/Data/Puzzle/Prototypes/memory_theater_Theme.asset",
                "Assets/ANIMOL/Data/Puzzle/Prototypes/letter_observatory_Theme.asset",
                "Assets/ANIMOL/Data/Puzzle/Prototypes/twilight_sea_Theme.asset" };
            var themes = themePaths.Select(AssetDatabase.LoadAssetAtPath<PuzzleTheme>).ToArray();
            var index = Array.IndexOf(themes, newMapTheme);
            index = EditorGUILayout.Popup("테마", Mathf.Max(0, index), new[] { "별길 목장", "잔영 극장", "편지 천문대", "별너울 해안" });
            newMapTheme = themes[index];
            EditorGUILayout.HelpBox("12×18 기본 맵을 만듭니다. 크기는 생성 후 맵 설정에서 바꿀 수 있습니다. 장치 데이터는 자동으로 준비됩니다.", MessageType.Info);
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
