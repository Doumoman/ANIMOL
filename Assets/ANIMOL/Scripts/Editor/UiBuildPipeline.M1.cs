using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.Editor
{
    public static partial class UiBuildPipeline
    {
        private const string UiPrefabFolder = "Assets/ANIMOL/Prefabs/UI";
        private const string SceneFolder = "Assets/ANIMOL/Scenes";
        private static readonly Color Navy = new Color(0.035f, 0.07f, 0.12f, 1f);
        private static readonly Color Panel = new Color(0.075f, 0.14f, 0.22f, 0.96f);
        private static readonly Color Cyan = new Color(0.12f, 0.68f, 0.85f, 1f);
        private static readonly Color Amber = new Color(0.95f, 0.63f, 0.18f, 1f);

        [MenuItem("ANIMOL/Build/M1 Common UI")]
        public static void BuildM1Menu()
        {
            BuildM0();
            BuildM1();
            ValidateM1();
        }

        static partial void BuildM1()
        {
            PlayerSettings.productName = "ANIMOL";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            BuildCommonRootPrefab();
            BuildScreenPrefabs();
            BuildModalAndOverlayPrefabs();
            BuildBootstrapScene();
            BuildLobbyScene();
            SetBuildScenes(new[] { $"{SceneFolder}/Bootstrap.unity", $"{SceneFolder}/Lobby.unity" });
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ANIMOL][M1] Common UI built.");
        }

        static partial void ValidateM1()
        {
            var errors = new List<string>();
            var requiredAssets = new[]
            {
                $"{UiPrefabFolder}/UI_CommonRoot.prefab", $"{UiPrefabFolder}/SC00_Boot.prefab",
                $"{UiPrefabFolder}/SC01_Lobby.prefab", $"{UiPrefabFolder}/SC02_ThemeSelect.prefab",
                $"{UiPrefabFolder}/SC03_StageSelect.prefab", $"{UiPrefabFolder}/SC04_StageDetail.prefab",
                $"{UiPrefabFolder}/SC13_Settings.prefab", $"{UiPrefabFolder}/SC14_Help.prefab",
                $"{UiPrefabFolder}/ConfirmExitModal.prefab", $"{UiPrefabFolder}/ToastAndErrorOverlay.prefab",
                $"{SceneFolder}/Bootstrap.unity", $"{SceneFolder}/Lobby.unity"
            };
            foreach (var path in requiredAssets) if (AssetDatabase.LoadMainAssetAtPath(path) == null) errors.Add($"Missing {path}");

            var common = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/UI_CommonRoot.prefab");
            var scaler = common == null ? null : common.GetComponent<CanvasScaler>();
            if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
                scaler.referenceResolution != new Vector2(1920, 1080) || Math.Abs(scaler.matchWidthOrHeight - 1f) > 0.001f)
                errors.Add("CanvasScaler must be 1920x1080, ScaleWithScreenSize, height matched.");

            var theme = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/SC02_ThemeSelect.prefab");
            var stages = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/SC03_StageSelect.prefab");
            var themeCount = theme == null ? 0 : theme.GetComponentsInChildren<Button>(true).Count(x => x.name.StartsWith("ThemeCard_", StringComparison.Ordinal));
            var stageCount = stages == null ? 0 : stages.GetComponentsInChildren<Button>(true).Count(x => x.name.StartsWith("StageCard_", StringComparison.Ordinal));
            if (themeCount != 5) errors.Add($"Theme card count={themeCount}");
            if (stageCount != 20) errors.Add($"Stage card count={stageCount}");

            var detail = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/SC04_StageDetail.prefab");
            if (detail != null && detail.GetComponentsInChildren<Button>(true).Any(x => x.name.Contains("Animal", StringComparison.OrdinalIgnoreCase)))
                errors.Add("Campaign detail contains an animal selection button.");

            var lines = new[]
            {
                "ANIMOL M1 validation", $"valid={errors.Count == 0}", "themeCards=" + themeCount,
                "stageCards=" + stageCount, "campaignAnimalSelectButtons=0", "errors=" + string.Join(" | ", errors)
            };
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Logs"));
            File.WriteAllLines(Path.Combine(Directory.GetCurrentDirectory(), "Logs", "animol-m1-validation.txt"), lines);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
        }

        private static void BuildCommonRootPrefab()
        {
            var root = new GameObject("UIRoot", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster),
                typeof(UiNavigationService), typeof(UiModalStack), typeof(LandscapeDisplayController));
            try
            {
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 0;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 1f;

                var safe = CreateRect("SafeArea", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                safe.gameObject.AddComponent<SafeAreaLayout>();
                CreateRect("ScreenHost", safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                CreateRect("ModalHost", safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                CreateRect("OverlayHost", safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                PrefabUtility.SaveAsPrefabAsset(root, $"{UiPrefabFolder}/UI_CommonRoot.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void BuildScreenPrefabs()
        {
            SaveScreen("SC00_Boot", root =>
            {
                CreateLabel("Title", root.transform, "ANIMOL", 92, new Vector2(0.5f, 0.64f), new Vector2(0.5f, 0.64f), new Vector2(800, 130), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("Status", root.transform, "설정 · 콘텐츠 · 계정 준비 완료", 32, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900, 60), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("ContinueButton", root.transform, "ANIMOL 시작", new Vector2(0.5f, 0.32f), new Vector2(420, 100), Cyan);
            });
            SaveScreen("SC01_Lobby", root =>
            {
                CreateLabel("LobbyTitle", root.transform, "ANIMOL", 72, new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), new Vector2(800, 100), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("LobbyStatus", root.transform, "로컬 개발 모드 · 운영 계정/코인 미연결", 28, new Vector2(0.5f, 0.79f), new Vector2(0.5f, 0.79f), new Vector2(1000, 50), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("CampaignButton", root.transform, "캠페인", new Vector2(0.28f, 0.58f), new Vector2(430, 150), Cyan);
                CreateButton("CompetitiveButton", root.transform, "경쟁\n서비스 준비 중", new Vector2(0.5f, 0.58f), new Vector2(430, 150), new Color(0.22f, 0.3f, 0.4f, 1));
                CreateButton("CoopButton", root.transform, "협동\n서비스 준비 중", new Vector2(0.72f, 0.58f), new Vector2(430, 150), new Color(0.22f, 0.3f, 0.4f, 1));
                CreateButton("DevTestButton", root.transform, "DEV-TEST-01\n개발용 물리 런", new Vector2(0.5f, 0.38f), new Vector2(520, 120), Amber);
                CreateLabel("LobbyAccountStatus", root.transform, "계정·코인: 미연결", 22, new Vector2(0.5f, 0.31f), new Vector2(0.5f, 0.31f), new Vector2(1000, 40), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("UpgradeButton", root.transform, "업그레이드", new Vector2(0.24f, 0.21f), new Vector2(230, 72), Panel);
                CreateButton("EmoteButton", root.transform, "이모티콘", new Vector2(0.37f, 0.21f), new Vector2(230, 72), Panel);
                CreateButton("StoreButton", root.transform, "상점", new Vector2(0.5f, 0.21f), new Vector2(230, 72), Panel);
                CreateButton("ProfileButton", root.transform, "기록", new Vector2(0.63f, 0.21f), new Vector2(230, 72), Panel);
                CreateButton("RewardedAdButton", root.transform, "선택 광고", new Vector2(0.76f, 0.21f), new Vector2(230, 72), Amber);
                CreateButton("SettingsButton", root.transform, "설정", new Vector2(0.36f, 0.09f), new Vector2(250, 72), Panel);
                CreateButton("HelpButton", root.transform, "도움말", new Vector2(0.5f, 0.09f), new Vector2(250, 72), Panel);
                CreateButton("ExitButton", root.transform, "종료", new Vector2(0.64f, 0.09f), new Vector2(250, 72), Panel);
            });
            SaveScreen("SC02_ThemeSelect", root =>
            {
                CreateLabel("ThemeTitle", root.transform, "캠페인 테마 · 정확히 5개", 54, new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), new Vector2(1200, 80), Vector2.zero, TextAnchor.MiddleCenter);
                for (var i = 1; i <= 5; i++)
                    CreateButton($"ThemeCard_{i:00}", root.transform, $"테마 {i:00}\n0/20 · 제작 중", new Vector2(0.12f + (i - 1) * 0.19f, 0.52f), new Vector2(300, 430), new Color(0.09f + i * 0.018f, 0.22f, 0.31f + i * 0.025f, 1));
                CreateButton("ThemeBackButton", root.transform, "← 로비", new Vector2(0.1f, 0.1f), new Vector2(240, 76), Panel);
            });
            SaveScreen("SC03_StageSelect", root =>
            {
                CreateLabel("StageThemeTitle", root.transform, "테마 01 · 20개 스테이지", 48, new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.92f), new Vector2(1200, 70), Vector2.zero, TextAnchor.MiddleCenter);
                for (var i = 1; i <= 20; i++)
                {
                    var column = (i - 1) % 5;
                    var row = (i - 1) / 5;
                    var anchor = new Vector2(0.19f + column * 0.155f, 0.75f - row * 0.18f);
                    CreateButton($"StageCard_{i:00}", root.transform, $"{i:00}\n제작 중", anchor, new Vector2(240, 130), new Color(0.11f, 0.26f, 0.36f, 1));
                }
                CreateButton("StageBackButton", root.transform, "← 테마", new Vector2(0.1f, 0.08f), new Vector2(240, 72), Panel);
            });
            SaveScreen("SC04_StageDetail", root =>
            {
                CreateLabel("StageDetailTitle", root.transform, "T01-S01", 60, new Vector2(0.5f, 0.84f), new Vector2(0.5f, 0.84f), new Vector2(1000, 90), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("StageObjectiveText", root.transform, "목표: 방울 3개 획득 → 출구 개방 → 실제 출구 도착", 34, new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.66f), new Vector2(1450, 80), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("FixedAnimalsText", root.transform, "고정 사용 동물: 미설정 (읽기 전용)", 32, new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), new Vector2(1300, 100), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("AvailabilityText", root.transform, "제작 중: 맵/고정 동물/출구 검증 필요", 30, new Vector2(0.5f, 0.39f), new Vector2(0.5f, 0.39f), new Vector2(1300, 80), Vector2.zero, TextAnchor.MiddleCenter);
                var start = CreateButton("StageStartButton", root.transform, "시작 불가 · 제작 중", new Vector2(0.5f, 0.23f), new Vector2(480, 100), new Color(0.25f, 0.28f, 0.32f, 1));
                start.interactable = false;
                CreateButton("DetailBackButton", root.transform, "← 스테이지 목록", new Vector2(0.12f, 0.1f), new Vector2(310, 76), Panel);
            });
            SaveScreen("SC13_Settings", root =>
            {
                CreateLabel("SettingsTitle", root.transform, "설정", 60, new Vector2(0.5f, 0.86f), new Vector2(0.5f, 0.86f), new Vector2(800, 90), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("SettingsSummary", root.transform, "BGM 100%   ·   SFX 100%   ·   진동 켜짐\n터치 감도 1.0   ·   언어 한국어\n로컬 설정 저장 구조 준비", 32, new Vector2(0.5f, 0.54f), new Vector2(0.5f, 0.54f), new Vector2(1100, 260), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("SettingsResetButton", root.transform, "설정 초기화", new Vector2(0.5f, 0.3f), new Vector2(340, 86), Amber);
                CreateButton("SettingsBackButton", root.transform, "← 뒤로", new Vector2(0.12f, 0.1f), new Vector2(260, 76), Panel);
            });
            SaveScreen("SC14_Help", root =>
            {
                CreateLabel("HelpTitle", root.transform, "도움말", 60, new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), new Vector2(800, 90), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("HelpBody", root.transform, "걷기·질주·점프·비행·동물 전환\n\n캠페인 목표\n1. 후보 방울 중 고유한 3개 획득\n2. 출구가 열린 뒤 실제 출구까지 이동\n3. 캠페인 동물은 스테이지 고정 구성만 사용\n\n개발 테스트는 운영 진행과 코인을 변경하지 않습니다.", 30, new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), new Vector2(1300, 560), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("HelpBackButton", root.transform, "← 뒤로", new Vector2(0.12f, 0.1f), new Vector2(260, 76), Panel);
            });
        }

        private static void BuildModalAndOverlayPrefabs()
        {
            var modal = CreatePanelRoot("ConfirmExitModal", new Color(0, 0, 0, 0.72f));
            try
            {
                var card = CreateImage("Card", modal.transform, new Vector2(0.25f, 0.28f), new Vector2(0.75f, 0.72f), Vector2.zero, Vector2.zero, Panel);
                CreateLabel("Message", card.transform, "ANIMOL을 종료할까요?", 40, new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.66f), new Vector2(750, 90), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("ExitCancelButton", card.transform, "취소", new Vector2(0.34f, 0.28f), new Vector2(260, 80), Cyan);
                CreateButton("ExitConfirmButton", card.transform, "확인", new Vector2(0.66f, 0.28f), new Vector2(260, 80), Amber);
                PrefabUtility.SaveAsPrefabAsset(modal, $"{UiPrefabFolder}/ConfirmExitModal.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(modal); }

            var toast = CreatePanelRoot("ToastAndErrorOverlay", new Color(0, 0, 0, 0));
            try
            {
                var card = CreateImage("ToastCard", toast.transform, new Vector2(0.27f, 0.04f), new Vector2(0.73f, 0.15f), Vector2.zero, Vector2.zero, new Color(0.02f, 0.04f, 0.07f, 0.94f));
                var label = CreateLabel("ToastMessage", card.transform, "알림", 28, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
                var presenter = toast.AddComponent<ToastAndErrorPresenter>();
                var serialized = new SerializedObject(presenter);
                serialized.FindProperty("messageLabel").objectReferenceValue = label;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(toast, $"{UiPrefabFolder}/ToastAndErrorOverlay.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(toast); }
        }

        private static void BuildBootstrapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateUiCamera();
            var ui = InstantiatePrefab("UI_CommonRoot.prefab");
            var screenHost = ui.transform.Find("SafeArea/ScreenHost");
            AttachPrefab("SC00_Boot.prefab", screenHost);
            ui.AddComponent<BootstrapPresenter>();
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, $"{SceneFolder}/Bootstrap.unity");
        }

        private static void BuildLobbyScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateUiCamera();
            var ui = InstantiatePrefab("UI_CommonRoot.prefab");
            var safe = ui.transform.Find("SafeArea");
            var screenHost = safe.Find("ScreenHost");
            var modalHost = safe.Find("ModalHost");
            var overlayHost = safe.Find("OverlayHost");
            foreach (var name in new[]
            {
                "SC01_Lobby", "SC02_ThemeSelect", "SC03_StageSelect", "SC04_StageDetail", "SC13_Settings", "SC14_Help",
                "SC09_MapLoading", "SC18_CampaignMilestone",
                "SC10_UpgradeHub", "SC10A_AccountUpgrade", "SC10B_CharacterUpgrade", "SC11_Store", "SC12_EmoteCollection", "SC17_ProfileRecords",
                "SC05_CompetitiveHub", "SC08_CoopHub", "SC06_MatchRoom", "SC07_MultiplayerAnimalSelect", "HUD_Competitive", "HUD_Coop"
            }) AttachPrefabIfExists(name + ".prefab", screenHost);
            var modal = AttachPrefab("ConfirmExitModal.prefab", modalHost); modal.SetActive(false);
            var toast = AttachPrefab("ToastAndErrorOverlay.prefab", overlayHost); toast.SetActive(false);
            foreach (var name in new[] { "RewardedAdModal", "ProductDetailModal", "ServiceErrorModal", "MultiplayerPauseModal" })
            {
                var optional = AttachPrefabIfExists(name + ".prefab", modalHost);
                if (optional != null) optional.SetActive(false);
            }
            var presenter = ui.AddComponent<CampaignUiPresenter>();
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignCatalogGenerator.CatalogPath);
            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("catalog").objectReferenceValue = catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var meta = ui.AddComponent<MetaUiPresenter>();
            var metaSerialized = new SerializedObject(meta);
            metaSerialized.FindProperty("accountUpgradeCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AccountUpgradeCatalog>("Assets/ANIMOL/Data/Meta/AccountUpgradeCatalog.asset");
            metaSerialized.FindProperty("characterUpgradeCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CharacterUpgradeCatalog>("Assets/ANIMOL/Data/Meta/CharacterUpgradeCatalog.asset");
            metaSerialized.FindProperty("emoteCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EmoteCatalog>("Assets/ANIMOL/Data/Meta/EmoteCatalog.asset");
            metaSerialized.FindProperty("services").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ExternalServiceConfiguration>("Assets/ANIMOL/Data/Services/ExternalServiceConfiguration.asset");
            metaSerialized.ApplyModifiedPropertiesWithoutUndo();
            var multiplayer = ui.AddComponent<MultiplayerUiPresenter>();
            var multiplayerSerialized = new SerializedObject(multiplayer);
            multiplayerSerialized.FindProperty("competitivePreview").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MultiplayerPreviewDefinition>("Assets/ANIMOL/Data/Development/DEV-MULTI-COMPETITIVE.asset");
            multiplayerSerialized.FindProperty("coopPreview").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MultiplayerPreviewDefinition>("Assets/ANIMOL/Data/Development/DEV-MULTI-COOP.asset");
            multiplayerSerialized.FindProperty("services").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ExternalServiceConfiguration>("Assets/ANIMOL/Data/Services/ExternalServiceConfiguration.asset");
            multiplayerSerialized.ApplyModifiedPropertiesWithoutUndo();
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, $"{SceneFolder}/Lobby.unity");
        }

        private static void SaveScreen(string name, Action<GameObject> build)
        {
            var root = CreatePanelRoot(name, Navy);
            try
            {
                root.AddComponent<UiScreenState>();
                build(root);
                PrefabUtility.SaveAsPrefabAsset(root, $"{UiPrefabFolder}/{name}.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static GameObject CreatePanelRoot(string name, Color color) => CreateImage(name, null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, color);

        private static GameObject CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (parent != null) go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
            return rect;
        }

        private static Text CreateLabel(string name, Transform parent, string text, int fontSize, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.sizeDelta = size; rect.anchoredPosition = position;
            var label = go.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text; label.fontSize = fontSize; label.alignment = alignment; label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        private static Button CreateButton(string name, Transform parent, string text, Vector2 anchor, Vector2 size, Color color)
        {
            var go = CreateImage(name, parent, anchor, anchor, Vector2.zero, Vector2.zero, color);
            var rect = (RectTransform)go.transform; rect.sizeDelta = size; rect.anchoredPosition = Vector2.zero;
            var button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>();
            CreateLabel("Label", go.transform, text, 28, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
            return button;
        }

        private static GameObject InstantiatePrefab(string fileName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/{fileName}");
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        private static GameObject AttachPrefab(string fileName, Transform parent)
        {
            var instance = InstantiatePrefab(fileName);
            instance.transform.SetParent(parent, false);
            return instance;
        }

        private static GameObject AttachPrefabIfExists(string fileName, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/{fileName}");
            if (prefab == null) return null;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent, false);
            return instance;
        }

        private static void CreateEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static void CreateUiCamera()
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Navy;
        }

        private static void SetBuildScenes(IEnumerable<string> scenePaths)
        {
            EditorBuildSettings.scenes = scenePaths.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
        }
    }
}
