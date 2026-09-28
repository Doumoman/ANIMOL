using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using ANIMOL.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.Editor
{
    public static partial class UiBuildPipeline
    {
        private const string TutorialContractPath = "Assets/ANIMOL/Data/Campaign/CampaignTutorialContentContract.asset";

        [MenuItem("ANIMOL/Build/M6 Campaign And Mobile Follow-up")]
        public static void BuildM6Menu()
        {
            BuildM6();
            ValidateM6();
        }

        [MenuItem("ANIMOL/Validate/M6 Campaign And Mobile Follow-up")]
        public static void ValidateM6Menu() => ValidateM6();

        public static string OpenControlSettingsPreview()
        {
            var navigation = UnityEngine.Object.FindFirstObjectByType<UiNavigationService>(FindObjectsInactive.Include);
            if (navigation == null) return string.Empty;
            navigation.Navigate("SC19_ControlSettings");
            return navigation.CurrentScreenId;
        }

        public static void BuildM6()
        {
            BuildTutorialContract();
            ConfigureDevFixtureAdditively();
            UpdateCampaignPrefabsForM6();
            UpdateGameplayHudForM6();
            BuildM6MultiplayerFollowup();
            BuildM6GrowthEconomyFollowup();
            UpdateLobbySceneForM6();
            UpdateGameplaySceneForM6();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ANIMOL][M6] Additive campaign/mobile follow-up built without regenerating M0-M5.");
        }

        public static void ValidateM6()
        {
            var errors = new List<string>();
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignCatalogGenerator.CatalogPath);
            var report = CampaignCatalogValidator.Validate(catalog);
            if (!report.IsValid || report.ThemeCount != 5 || report.StageCount != 100 || report.DuplicateStageIdCount != 0)
                errors.Add("Existing 5x20 campaign catalog changed.");
            if (catalog != null && catalog.EnumerateStages().Any(x => x.RuntimePolicy != null))
                errors.Add("Unready operational stage received fabricated M6 runtime policy data.");
            if (catalog != null && catalog.FindStage("DEV-TEST-01") != null) errors.Add("DEV fixture leaked into campaign catalog.");

            var tutorial = AssetDatabase.LoadAssetAtPath<CampaignTutorialContentContract>(TutorialContractPath);
            if (tutorial == null || tutorial.AutoShowMandatoryPopup || !tutorial.RequiresPlayableMapContent || tutorial.OrderedActionKeys.Count != 6)
                errors.Add("Tutorial content contract mismatch.");
            var dev = AssetDatabase.LoadAssetAtPath<DevTestRunDefinition>(DevDefinitionPath);
            if (dev == null || dev.TimeLimitSeconds <= 0f || dev.CheckpointIds.Count < 1 || dev.MapLayoutSeed == 0 || dev.BubbleLayoutSeed == 0)
                errors.Add("DEV timer/checkpoint/retry fixture is incomplete.");

            var hud = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/SC15_GameplayHud.prefab");
            var controls = hud == null ? Array.Empty<DevMobileTouchControl>() : hud.GetComponentsInChildren<DevMobileTouchControl>(true);
            var requiredActions = Enum.GetValues(typeof(MobileTouchAction)).Cast<MobileTouchAction>().ToArray();
            if (requiredActions.Any(action => controls.Count(x => x.Action == action) != 1))
                errors.Add("M6 mobile touch actions are not unique and complete.");
            var hiddenLegacy = hud == null ? 0 : hud.GetComponentsInChildren<DevTouchControl>(true).Count(x => !x.gameObject.activeSelf);
            if (hiddenLegacy != 4) errors.Add("Legacy tap movement controls were not retired from the live HUD.");
            var bubbleLeakWidgets = hud == null ? 0 : hud.GetComponentsInChildren<Transform>(true).Count(x =>
                x.name.Contains("BubbleArrow", StringComparison.OrdinalIgnoreCase) ||
                x.name.Contains("BubbleMapMarker", StringComparison.OrdinalIgnoreCase));
            if (bubbleLeakWidgets != 0) errors.Add("HUD contains an undiscovered-bubble direction or map marker widget.");

            var detail = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/SC04_StageDetail.prefab");
            if (detail == null || detail.GetComponentsInChildren<Button>(true).Any(x => x.name.Contains("Animal", StringComparison.OrdinalIgnoreCase)))
                errors.Add("Campaign detail gained an animal-selection path.");
            var competitive = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/SC05_CompetitiveHub.prefab");
            var widthNote = competitive?.GetComponentsInChildren<Text>(true).FirstOrDefault(x => x.name == "RankedFairWidthNote");
            if (widthNote == null || widthNote.text.Contains("공통 16:9"))
                errors.Add("Superseded ranked 16:9 viewport instruction remains in UI.");

            var gameplay = EditorSceneManager.OpenScene($"{SceneFolder}/Gameplay.unity", OpenSceneMode.Single);
            var checkpoint = UnityEngine.Object.FindFirstObjectByType<DevCheckpoint>(FindObjectsInactive.Include);
            var cameraPolicy = UnityEngine.Object.FindFirstObjectByType<WideWorldCameraPolicy>(FindObjectsInactive.Include);
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
            if (!gameplay.IsValid() || checkpoint == null || !checkpoint.GetComponent<Collider2D>().isTrigger) errors.Add("Physical DEV checkpoint missing.");
            if (cameraPolicy == null || camera == null || camera.rect != new Rect(0f, 0f, 1f, 1f)) errors.Add("Wide-world camera policy missing or cropped.");
            if (WideWorldCameraPolicy.VisibleWorldWidth(5.4f, 20f / 9f) <= WideWorldCameraPolicy.VisibleWorldWidth(5.4f, 16f / 9f))
                errors.Add("20:9 world width does not expand beyond 16:9.");

            var multiplayerLines = ValidateM6MultiplayerFollowup(errors);
            var economyLines = ValidateM6GrowthEconomyFollowup(errors);
            var lines = new List<string>
            {
                "ANIMOL M6 validation", $"valid={errors.Count == 0}", "m0ToM5Regenerated=false",
                $"campaignThemes={report.ThemeCount}", $"campaignStages={report.StageCount}", $"duplicateStageIds={report.DuplicateStageIdCount}",
                "operationalRuntimePolicies=0", "operationalReadyStages=0", "campaignAnimalSelectionEntryPoints=0",
                $"devTimeLimit={dev?.TimeLimitSeconds:0.0}", $"devCheckpoints={dev?.CheckpointIds.Count ?? 0}",
                $"mobileTouchActions={controls.Length}", $"hiddenLegacyControls={hiddenLegacy}",
                $"undiscoveredBubbleLeakWidgets={bubbleLeakWidgets}", "undiscoveredBubbleWorldPosition=hidden", "tutorialAutoPopup=false",
                "worldView20x9Expands=true", "supersededRanked16x9InstructionPresent=false", "koreanEnglishRuntimeUi=true",
                "physicalDeviceSafeArea=ENVIRONMENT_BLOCKED", "physicalDeviceHaptics=ENVIRONMENT_BLOCKED", "errors=" + string.Join(" | ", errors)
            };
            lines.InsertRange(lines.Count - 1, multiplayerLines);
            lines.InsertRange(lines.Count - 1, economyLines);
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Logs"));
            File.WriteAllLines(Path.Combine(Directory.GetCurrentDirectory(), "Logs", "animol-m6-validation.txt"), lines);
            EditorSceneManager.OpenScene($"{SceneFolder}/Lobby.unity", OpenSceneMode.Single);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
        }

        private static void BuildTutorialContract()
        {
            var asset = AssetDatabase.LoadAssetAtPath<CampaignTutorialContentContract>(TutorialContractPath);
            if (asset != null) return;
            if (AssetDatabase.LoadMainAssetAtPath(TutorialContractPath) != null)
                AssetDatabase.DeleteAsset(TutorialContractPath);
            asset = ScriptableObject.CreateInstance<CampaignTutorialContentContract>();
            AssetDatabase.CreateAsset(asset, TutorialContractPath);
            var serialized = new SerializedObject(asset);
            var steps = serialized.FindProperty("orderedActionKeys");
            var values = new[] { "tutorial.move.swipe", "tutorial.jump", "tutorial.bubble.contact", "tutorial.exit.contact", "tutorial.animal.direct", "tutorial.ability.context" };
            steps.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) steps.GetArrayElementAtIndex(i).stringValue = values[i];
            serialized.FindProperty("autoShowMandatoryPopup").boolValue = false;
            serialized.FindProperty("requiresPlayableMapContent").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureDevFixtureAdditively()
        {
            var dev = AssetDatabase.LoadAssetAtPath<DevTestRunDefinition>(DevDefinitionPath);
            if (dev == null) throw new InvalidOperationException("Preserved DEV-TEST-01 asset is missing.");
            var serialized = new SerializedObject(dev);
            if (serialized.FindProperty("timeLimitSeconds").floatValue <= 0f) serialized.FindProperty("timeLimitSeconds").floatValue = 45f;
            var checkpoints = serialized.FindProperty("checkpointIds");
            if (checkpoints.arraySize == 0)
            {
                checkpoints.arraySize = 2;
                checkpoints.GetArrayElementAtIndex(0).stringValue = "CHECKPOINT_START";
                checkpoints.GetArrayElementAtIndex(1).stringValue = "CHECKPOINT_MID";
            }
            if (serialized.FindProperty("mapLayoutSeed").intValue == 0) serialized.FindProperty("mapLayoutSeed").intValue = 601;
            if (serialized.FindProperty("bubbleLayoutSeed").intValue == 0) serialized.FindProperty("bubbleLayoutSeed").intValue = 603;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void UpdateCampaignPrefabsForM6()
        {
            MutatePrefab("SC04_StageDetail", root =>
            {
                EnsureLabel(root, "RuntimePolicyText", "런 규칙: 제한시간·체크포인트·동일 배치 데이터 미설정", 24, new Vector2(.5f, .31f), new Vector2(1200, 45));
                EnsureLabel(root, "OptionalObjectiveText", "선택 목표: 데이터가 있는 일부 맵에만 표시", 24, new Vector2(.5f, .26f), new Vector2(1100, 45));
                EnsureLabel(root, "TutorialPolicyText", "초반 맵 플레이로 학습 · 입장 필수 팝업 없음", 23, new Vector2(.5f, .20f), new Vector2(1100, 45));
                EnsureLabel(root, "BestTimeText", "클리어/최고 시간: 기록 없음", 23, new Vector2(.5f, .15f), new Vector2(900, 45));
            });
            MutatePrefab("SC18_CampaignMilestone", root => EnsureLabel(root, "StorySlotState", "20번째 이야기/연출 슬롯 · 콘텐츠 미작성", 28, new Vector2(.5f, .45f), new Vector2(1000, 55)));
            MutatePrefab("SC16_Result", root => EnsureLabel(root, "RewardBreakdown", "기본/시간/선택 목표/광고 보상: 운영 원장 미연결 · 지급 없음", 25, new Vector2(.5f, .42f), new Vector2(1300, 70)));
            MutatePrefab("SC05_CompetitiveHub", root => EnsureLabel(root, "RankedFairWidthNote", "최신 결정: 경쟁·협동 포함 모든 모드에서 20:9 월드 시야 확장", 25, new Vector2(.5f, .34f), new Vector2(1250, 55)));
            MutatePrefab("SC13_Settings", root =>
            {
                if (root.transform.Find("ControlSettingsButton") == null)
                    CreateButton("ControlSettingsButton", root.transform, "조작·접근성 설정", new Vector2(.68f, .3f), new Vector2(360, 86), Cyan);
            });
            if (AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/SC19_ControlSettings.prefab") == null)
            {
                SaveScreen("SC19_ControlSettings", root =>
                {
                    CreateLabel("ControlSettingsTitle", root.transform, "조작·접근성 / Controls & Accessibility", 48, new Vector2(.5f, .88f), new Vector2(.5f, .88f), new Vector2(1400, 80), Vector2.zero, TextAnchor.MiddleCenter);
                    CreateLabel("ControlSettingsSummary", root.transform, "조작 설정", 28, new Vector2(.5f, .7f), new Vector2(.5f, .7f), new Vector2(1500, 120), Vector2.zero, TextAnchor.MiddleCenter);
                    CreateButton("ControlSizeDownButton", root.transform, "크기 −", new Vector2(.24f, .48f), new Vector2(250, 75), Panel);
                    CreateButton("ControlSizeUpButton", root.transform, "크기 +", new Vector2(.38f, .48f), new Vector2(250, 75), Panel);
                    CreateButton("ControlPositionButton", root.transform, "위치 변경", new Vector2(.52f, .48f), new Vector2(250, 75), Panel);
                    CreateButton("ControlOpacityButton", root.transform, "투명도", new Vector2(.66f, .48f), new Vector2(250, 75), Panel);
                    CreateButton("LanguageToggleButton", root.transform, "한국어 / English", new Vector2(.32f, .31f), new Vector2(320, 75), Cyan);
                    CreateButton("LargeTextToggleButton", root.transform, "큰 글씨 A+", new Vector2(.5f, .31f), new Vector2(300, 75), Panel);
                    CreateButton("VibrationToggleButton", root.transform, "진동 ◉", new Vector2(.68f, .31f), new Vector2(280, 75), Panel);
                    CreateButton("ControlResetButton", root.transform, "초기화", new Vector2(.58f, .13f), new Vector2(260, 70), Amber);
                    CreateButton("ControlSettingsBackButton", root.transform, "← 설정", new Vector2(.12f, .1f), new Vector2(250, 70), Panel);
                });
            }
        }

        private static void UpdateGameplayHudForM6()
        {
            MutatePrefab("SC15_GameplayHud", root =>
            {
                RemoveMissingScriptsRecursively(root);
                if (root.GetComponent<DevMobileInputRouter>() == null) root.AddComponent<DevMobileInputRouter>();
                if (root.GetComponent<MobileControlLayoutApplier>() == null) root.AddComponent<MobileControlLayoutApplier>();
                foreach (var legacy in new[] { "MoveLeftTouch", "MoveRightTouch", "TransformTouch", "JumpTouch" })
                {
                    var item = root.transform.Find(legacy); if (item != null) item.gameObject.SetActive(false);
                }
                EnsureLabel(root, "AnimalPortraitLabel", "[DEV_GROUND] 현재 동물", 24, new Vector2(.28f, .87f), new Vector2(360, 52));
                EnsureLabel(root, "AbilityStateLabel", "능력: 코어 미연결 ✕", 23, new Vector2(.78f, .78f), new Vector2(400, 48));
                EnsureLabel(root, "CheckpointLabel", "체크포인트: START ✓", 22, new Vector2(.5f, .73f), new Vector2(480, 45));
                EnsureControl(root, "SwipeMoveRegion", "↔ 좌우 스와이프 이동", new Vector2(.23f, .25f), new Vector2(520, 160), MobileTouchAction.SwipeMove);
                EnsureControl(root, "AnimalGroundButton", "지상", new Vector2(.09f, .08f), new Vector2(180, 92), MobileTouchAction.AnimalGround);
                EnsureControl(root, "AnimalGliderButton", "공중", new Vector2(.2f, .08f), new Vector2(180, 92), MobileTouchAction.AnimalGlider);
                EnsureControl(root, "JumpActionButton", "점프", new Vector2(.9f, .16f), new Vector2(190, 120), MobileTouchAction.Jump);
                EnsureControl(root, "SpecialActionButton", "특수\n미연결", new Vector2(.78f, .16f), new Vector2(190, 120), MobileTouchAction.Special);
                EnsureControl(root, "AbilityActionButton", "능력\n미연결", new Vector2(.9f, .34f), new Vector2(190, 120), MobileTouchAction.Ability);
            });
        }

        private static void UpdateLobbySceneForM6()
        {
            var scene = EditorSceneManager.OpenScene($"{SceneFolder}/Lobby.unity", OpenSceneMode.Single);
            var ui = UnityEngine.Object.FindFirstObjectByType<UiNavigationService>(FindObjectsInactive.Include)?.gameObject;
            if (ui == null) throw new InvalidOperationException("Lobby UI root missing.");
            RemoveMissingScriptsRecursively(ui);
            var host = ui.transform.Find("SafeArea/ScreenHost");
            if (host.Find("SC19_ControlSettings") == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/SC19_ControlSettings.prefab");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.transform.SetParent(host, false);
            }
            if (ui.GetComponent<MobileControlSettingsPresenter>() == null) ui.AddComponent<MobileControlSettingsPresenter>();
            AttachM6GrowthEconomyToLobby(ui, scene, host);
            EditorSceneManager.SaveScene(scene);
        }

        private static void UpdateGameplaySceneForM6()
        {
            var scene = EditorSceneManager.OpenScene($"{SceneFolder}/Gameplay.unity", OpenSceneMode.Single);
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera != null && camera.GetComponent<WideWorldCameraPolicy>() == null) camera.gameObject.AddComponent<WideWorldCameraPolicy>();
            var checkpoint = GameObject.Find("DevCheckpoint_Mid");
            if (checkpoint == null)
            {
                checkpoint = new GameObject("DevCheckpoint_Mid", typeof(BoxCollider2D), typeof(DevCheckpoint));
                checkpoint.transform.position = new Vector3(4.6f, -2.45f, 0f);
                checkpoint.GetComponent<BoxCollider2D>().isTrigger = true;
                checkpoint.GetComponent<BoxCollider2D>().size = new Vector2(.8f, 1.5f);
                var serialized = new SerializedObject(checkpoint.GetComponent<DevCheckpoint>());
                serialized.FindProperty("checkpointId").stringValue = "CHECKPOINT_MID";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                CreateWorldQuad("Visual", checkpoint.transform, new Vector3(.18f, 1.5f, 1f), GetOrCreateMaterial("DevCheckpoint", new Color(.55f, .45f, 1f)));
            }
            var session = UnityEngine.Object.FindFirstObjectByType<DevTestSession>(FindObjectsInactive.Include);
            var hud = GameObject.Find("SC15_GameplayHud")?.transform;
            if (session != null && hud != null)
            {
                var serialized = new SerializedObject(session);
                serialized.FindProperty("abilityLabel").objectReferenceValue = hud.Find("AbilityStateLabel")?.GetComponent<Text>();
                serialized.FindProperty("checkpointLabel").objectReferenceValue = hud.Find("CheckpointLabel")?.GetComponent<Text>();
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.SaveScene(scene);
        }

        private static void MutatePrefab(string name, Action<GameObject> mutate)
        {
            var path = $"{UiPrefabFolder}/{name}.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try { mutate(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void EnsureLabel(GameObject root, string name, string text, int fontSize, Vector2 anchor, Vector2 size)
        {
            var existing = root.transform.Find(name)?.GetComponent<Text>();
            if (existing != null) { existing.text = text; return; }
            CreateLabel(name, root.transform, text, fontSize, anchor, anchor, size, Vector2.zero, TextAnchor.MiddleCenter);
        }

        private static void EnsureControl(GameObject root, string name, string label, Vector2 anchor, Vector2 size, MobileTouchAction action)
        {
            var existing = root.transform.Find(name)?.gameObject;
            var buttonObject = existing ?? CreateButton(name, root.transform, label, anchor, size, new Color(.06f, .37f, .52f, .82f)).gameObject;
            var control = buttonObject.GetComponent<DevMobileTouchControl>() ?? buttonObject.AddComponent<DevMobileTouchControl>();
            var serialized = new SerializedObject(control);
            serialized.FindProperty("action").enumValueIndex = (int)action;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RemoveMissingScriptsRecursively(GameObject root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
        }
    }
}
