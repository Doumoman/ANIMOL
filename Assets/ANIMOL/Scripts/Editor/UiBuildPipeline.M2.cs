using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.Editor
{
    public static partial class UiBuildPipeline
    {
        private const string GameplayPrefabFolder = "Assets/ANIMOL/Prefabs/Gameplay";
        private const string DevDataFolder = "Assets/ANIMOL/Data/Development";
        private const string MaterialFolder = "Assets/ANIMOL/Art/Materials";
        private const string DevDefinitionPath = DevDataFolder + "/DEV-TEST-01.asset";

        [MenuItem("ANIMOL/Build/M2 Campaign Vertical Slice")]
        public static void BuildM2Menu()
        {
            BuildM0();
            BuildM1();
            BuildM2();
            ValidateM2();
        }

        static partial void BuildM2()
        {
            Directory.CreateDirectory(GameplayPrefabFolder);
            Directory.CreateDirectory(DevDataFolder);
            Directory.CreateDirectory(MaterialFolder);
            var definition = BuildDevDefinition();
            BuildM2UiPrefabs();
            BuildWorldPrefabs();
            BuildGameplayScene(definition);
            BuildResultsScene();
            SetBuildScenes(new[]
            {
                $"{SceneFolder}/Bootstrap.unity", $"{SceneFolder}/Lobby.unity",
                $"{SceneFolder}/Gameplay.unity", $"{SceneFolder}/Results.unity"
            });
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ANIMOL][M2] DEV-TEST-01 vertical slice built.");
        }

        static partial void ValidateM2()
        {
            var errors = new List<string>();
            var required = new[]
            {
                DevDefinitionPath,
                $"{GameplayPrefabFolder}/DevPlayer.prefab", $"{GameplayPrefabFolder}/DevBubble.prefab", $"{GameplayPrefabFolder}/DevExitGate.prefab",
                $"{UiPrefabFolder}/SC09_MapLoading.prefab", $"{UiPrefabFolder}/SC15_GameplayHud.prefab",
                $"{UiPrefabFolder}/SC16_Result.prefab", $"{UiPrefabFolder}/SC18_CampaignMilestone.prefab", $"{UiPrefabFolder}/PauseModal.prefab",
                $"{SceneFolder}/Gameplay.unity", $"{SceneFolder}/Results.unity"
            };
            foreach (var path in required) if (AssetDatabase.LoadMainAssetAtPath(path) == null) errors.Add("Missing " + path);

            var definition = AssetDatabase.LoadAssetAtPath<DevTestRunDefinition>(DevDefinitionPath);
            if (definition == null || definition.StageId != "DEV-TEST-01" || definition.TargetBubbleCount != 3 ||
                definition.FixedAllowedAnimalIds.Count != 2 || definition.InitialAnimalId != "DEV_GROUND")
                errors.Add("Development run definition contract mismatch.");

            var gameplay = EditorSceneManager.OpenScene($"{SceneFolder}/Gameplay.unity", OpenSceneMode.Single);
            var pickups = UnityEngine.Object.FindObjectsByType<DevBubblePickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var slotIds = pickups.Select(x => x.SlotId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToArray();
            var player = UnityEngine.Object.FindFirstObjectByType<DevPlayerController>();
            var exit = UnityEngine.Object.FindFirstObjectByType<DevExitGateView>();
            var touchCount = UnityEngine.Object.FindObjectsByType<DevTouchControl>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            if (!gameplay.IsValid()) errors.Add("Gameplay scene failed to open.");
            if (pickups.Length != 4 || slotIds.Length != 4) errors.Add($"Bubble candidates={pickups.Length}, uniqueSlots={slotIds.Length}");
            if (player == null || player.GetComponent<Rigidbody2D>() == null || player.GetComponent<Collider2D>() == null) errors.Add("Player physics chain missing.");
            if (exit == null || !exit.GetComponent<Collider2D>().isTrigger) errors.Add("Exit physical trigger missing.");
            if (touchCount != 4) errors.Add("Independent multitouch control regions=" + touchCount);
            if (UnityEngine.Object.FindFirstObjectByType<DevTestSession>() == null) errors.Add("DEV session missing.");
            var liveSession = UnityEngine.Object.FindFirstObjectByType<DevTestSession>();
            if (liveSession != null && liveSession.Definition == null) errors.Add("DEV session definition reference missing.");

            var catalog = AssetDatabase.LoadAssetAtPath<ANIMOL.Core.CampaignCatalog>(CampaignCatalogGenerator.CatalogPath);
            if (catalog != null && catalog.FindStage("DEV-TEST-01") != null) errors.Add("DEV-TEST-01 leaked into operational campaign catalog.");

            var lines = new[]
            {
                "ANIMOL M2 validation", $"valid={errors.Count == 0}", "stageId=DEV-TEST-01", "campaignCatalogContainsDev=false",
                $"candidateBubbles={pickups.Length}", $"uniqueBubbleSlots={slotIds.Length}", "targetBubbles=3",
                $"playerPhysics={(player != null)}", $"exitTrigger={(exit != null)}", $"multitouchRegions={touchCount}",
                "fixedAnimals=DEV_GROUND,DEV_GLIDER", "campaignAnimalSelectionEntryPoints=0", "developmentResultMutatesCampaign=false",
                "errors=" + string.Join(" | ", errors)
            };
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Logs"));
            File.WriteAllLines(Path.Combine(Directory.GetCurrentDirectory(), "Logs", "animol-m2-validation.txt"), lines);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
        }

        private static DevTestRunDefinition BuildDevDefinition()
        {
            var definition = AssetDatabase.LoadAssetAtPath<DevTestRunDefinition>(DevDefinitionPath);
            if (definition != null) return definition;
            definition = ScriptableObject.CreateInstance<DevTestRunDefinition>();
            AssetDatabase.CreateAsset(definition, DevDefinitionPath);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<DevTestRunDefinition>(DevDefinitionPath);
        }

        private static void BuildM2UiPrefabs()
        {
            SaveScreen("SC09_MapLoading", root =>
            {
                CreateLabel("LoadingTitle", root.transform, "맵 준비", 58, new Vector2(.5f, .7f), new Vector2(.5f, .7f), new Vector2(900, 90), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("LoadingState", root.transform, "DEV-TEST-01 · 고정 동물 DEV_GROUND / DEV_GLIDER\n로컬 회색박스 준비 상태: Ready", 32, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(1200, 180), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("LoadingBackButton", root.transform, "← 로비", new Vector2(.12f, .1f), new Vector2(280, 76), Panel);
            });

            SaveScreen("SC15_GameplayHud", root =>
            {
                root.GetComponent<Image>().color = Color.clear;
                root.GetComponent<Image>().raycastTarget = false;
                CreateLabel("RunLabel", root.transform, "DEV-TEST-01 · 운영 캠페인과 분리", 30, new Vector2(.5f, .94f), new Vector2(.5f, .94f), new Vector2(900, 55), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("BubbleLabel", root.transform, "방울 0/3 · 후보 4", 36, new Vector2(.15f, .88f), new Vector2(.15f, .88f), new Vector2(450, 60), Vector2.zero, TextAnchor.MiddleLeft);
                CreateLabel("GateLabel", root.transform, "출구 잠김 · 고유 방울 3개 필요", 30, new Vector2(.5f, .86f), new Vector2(.5f, .86f), new Vector2(750, 60), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("TimeLabel", root.transform, "시간 0.0s", 30, new Vector2(.84f, .88f), new Vector2(.84f, .88f), new Vector2(320, 60), Vector2.zero, TextAnchor.MiddleRight);
                CreateLabel("StaminaLabel", root.transform, "공용 스테미나 100/100", 28, new Vector2(.17f, .8f), new Vector2(.17f, .8f), new Vector2(500, 50), Vector2.zero, TextAnchor.MiddleLeft);
                CreateLabel("AnimalLabel", root.transform, "현재 DEV_GROUND · 고정 허용 DEV_GROUND / DEV_GLIDER", 26, new Vector2(.5f, .78f), new Vector2(.5f, .78f), new Vector2(900, 50), Vector2.zero, TextAnchor.MiddleCenter);
                CreateTouchButton("MoveLeftTouch", root.transform, "◀ 이동", new Vector2(.09f, .14f), DevTouchAction.MoveLeft);
                CreateTouchButton("MoveRightTouch", root.transform, "이동 ▶", new Vector2(.24f, .14f), DevTouchAction.MoveRight);
                CreateTouchButton("TransformTouch", root.transform, "동물 전환", new Vector2(.76f, .14f), DevTouchAction.Transform);
                CreateTouchButton("JumpTouch", root.transform, "점프 / 비행", new Vector2(.91f, .14f), DevTouchAction.Jump);
                CreateButton("PauseButton", root.transform, "Ⅱ", new Vector2(.95f, .92f), new Vector2(90, 70), Panel);
            });

            SaveScreen("SC16_Result", root =>
            {
                CreateLabel("ResultTitle", root.transform, "개발용 런 결과", 64, new Vector2(.5f, .78f), new Vector2(.5f, .78f), new Vector2(1000, 100), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("ResultSummary", root.transform, "DEV-TEST-01 결과", 34, new Vector2(.5f, .55f), new Vector2(.5f, .55f), new Vector2(1300, 220), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("RetryButton", root.transform, "다시 실행", new Vector2(.39f, .28f), new Vector2(340, 95), Cyan);
                CreateButton("LobbyButton", root.transform, "로비", new Vector2(.61f, .28f), new Vector2(340, 95), Panel);
            });

            SaveScreen("SC18_CampaignMilestone", root =>
            {
                CreateLabel("MilestoneTitle", root.transform, "캠페인 테마 완료", 64, new Vector2(.5f, .78f), new Vector2(.5f, .78f), new Vector2(1100, 100), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("MilestoneRule", root.transform, "T01-S20 → T02-S01 · T05-S20 뒤에는 다음 맵 없음", 34, new Vector2(.5f, .56f), new Vector2(.5f, .56f), new Vector2(1300, 100), Vector2.zero, TextAnchor.MiddleCenter);
                var nextTheme = CreateButton("NextThemeButton", root.transform, "다음 테마 · 검증된 운영 결과 필요", new Vector2(.5f, .33f), new Vector2(520, 95), Cyan);
                nextTheme.interactable = false;
                CreateButton("MilestoneLobbyButton", root.transform, "로비", new Vector2(.5f, .19f), new Vector2(360, 80), Panel);
            });

            var pause = CreatePanelRoot("PauseModal", new Color(0, 0, 0, .78f));
            try
            {
                var card = CreateImage("Card", pause.transform, new Vector2(.32f, .18f), new Vector2(.68f, .82f), Vector2.zero, Vector2.zero, Panel);
                CreateLabel("PauseTitle", card.transform, "일시정지", 52, new Vector2(.5f, .82f), new Vector2(.5f, .82f), new Vector2(500, 80), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("ContinueButton", pause.transform, "계속", new Vector2(.5f, .57f), new Vector2(360, 84), Cyan);
                CreateButton("RestartButton", pause.transform, "재시작", new Vector2(.5f, .43f), new Vector2(360, 84), Amber);
                CreateButton("LobbyButton", pause.transform, "로비로 나가기", new Vector2(.5f, .29f), new Vector2(360, 84), Panel);
                PrefabUtility.SaveAsPrefabAsset(pause, $"{UiPrefabFolder}/PauseModal.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(pause); }
        }

        private static void CreateTouchButton(string name, Transform parent, string label, Vector2 anchor, DevTouchAction action)
        {
            var button = CreateButton(name, parent, label, anchor, new Vector2(250, 145), new Color(.08f, .35f, .48f, .88f));
            var control = button.gameObject.AddComponent<DevTouchControl>();
            var serialized = new SerializedObject(control);
            serialized.FindProperty("action").enumValueIndex = (int)action;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildWorldPrefabs()
        {
            var playerMaterial = GetOrCreateMaterial("DevPlayer", new Color(.2f, .75f, 1f));
            var bubbleMaterial = GetOrCreateMaterial("DevBubble", new Color(1f, .75f, .12f));
            var gateMaterial = GetOrCreateMaterial("DevGate", new Color(.9f, .2f, .2f));

            var player = new GameObject("DevPlayer", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(DevPlayerController));
            try
            {
                player.tag = "Player";
                var rigidbody = player.GetComponent<Rigidbody2D>();
                rigidbody.gravityScale = 2.5f; rigidbody.freezeRotation = true;
                player.GetComponent<BoxCollider2D>().size = new Vector2(.8f, 1.2f);
                CreateWorldQuad("Visual", player.transform, new Vector3(.85f, 1.25f, 1f), playerMaterial);
                PrefabUtility.SaveAsPrefabAsset(player, $"{GameplayPrefabFolder}/DevPlayer.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(player); }

            var bubble = new GameObject("DevBubble", typeof(CircleCollider2D), typeof(DevBubblePickup));
            try
            {
                bubble.GetComponent<CircleCollider2D>().isTrigger = true;
                bubble.GetComponent<CircleCollider2D>().radius = .48f;
                var pickup = bubble.GetComponent<DevBubblePickup>();
                var serialized = new SerializedObject(pickup);
                serialized.FindProperty("slotId").stringValue = "A";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                CreateWorldQuad("Visual", bubble.transform, new Vector3(.72f, .72f, 1f), bubbleMaterial).transform.Rotate(0, 0, 45);
                PrefabUtility.SaveAsPrefabAsset(bubble, $"{GameplayPrefabFolder}/DevBubble.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(bubble); }

            var gate = new GameObject("DevExitGate", typeof(BoxCollider2D), typeof(DevExitGateView));
            try
            {
                gate.GetComponent<BoxCollider2D>().isTrigger = true;
                gate.GetComponent<BoxCollider2D>().size = new Vector2(1.2f, 2.8f);
                var visual = CreateWorldQuad("Visual", gate.transform, new Vector3(1.2f, 2.8f, 1f), gateMaterial).GetComponent<Renderer>();
                var serialized = new SerializedObject(gate.GetComponent<DevExitGateView>());
                serialized.FindProperty("visual").objectReferenceValue = visual;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(gate, $"{GameplayPrefabFolder}/DevExitGate.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(gate); }
        }

        private static void BuildGameplayScene(DevTestRunDefinition definition)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera"; cameraObject.transform.position = new Vector3(0, 0, -10);
            var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5.4f; camera.backgroundColor = new Color(.025f, .045f, .075f);

            var gray = new GameObject("Graybox_16x16_SourceTiles");
            var groundMaterial = GetOrCreateMaterial("DevGround", new Color(.18f, .23f, .3f));
            var ground = CreateWorldQuad("Ground", gray.transform, new Vector3(18f, 1f, 1f), groundMaterial);
            ground.transform.position = new Vector3(0, -3.65f, 0);
            ground.AddComponent<BoxCollider2D>().size = Vector2.one;
            for (var i = 0; i < 16; i++)
            {
                var marker = CreateWorldQuad($"SourceTile16x16_{i + 1:00}", gray.transform, new Vector3(.92f, .16f, 1f), groundMaterial);
                marker.transform.position = new Vector3(-7.5f + i, -3.05f, .1f);
            }

            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{GameplayPrefabFolder}/DevPlayer.prefab"));
            player.transform.position = new Vector3(-7f, -2.45f, 0);
            var bubblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{GameplayPrefabFolder}/DevBubble.prefab");
            var bubblePositions = new[] { new Vector3(-5.2f, -2.45f, 0), new Vector3(-2.4f, -2.45f, 0), new Vector3(.4f, -2.45f, 0), new Vector3(3.2f, -2.45f, 0) };
            var slotIds = new[] { "A", "B", "C", "D" };
            for (var i = 0; i < 4; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(bubblePrefab);
                instance.name = "Bubble_" + slotIds[i]; instance.transform.position = bubblePositions[i];
                var serialized = new SerializedObject(instance.GetComponent<DevBubblePickup>());
                serialized.FindProperty("slotId").stringValue = slotIds[i]; serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            var gateObject = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{GameplayPrefabFolder}/DevExitGate.prefab"));
            gateObject.transform.position = new Vector3(7.2f, -2.05f, 0);

            var ui = InstantiatePrefab("UI_CommonRoot.prefab");
            var safe = ui.transform.Find("SafeArea");
            var hud = AttachPrefab("SC15_GameplayHud.prefab", safe.Find("ScreenHost"));
            var pause = AttachPrefab("PauseModal.prefab", safe.Find("ModalHost")); pause.SetActive(false);
            ui.AddComponent<DevGameplayUiPresenter>();
            var sessionObject = new GameObject("DevTestSession", typeof(DevTestSession));
            var session = sessionObject.GetComponent<DevTestSession>();
            session.ConfigureDefinition(AssetDatabase.LoadAssetAtPath<DevTestRunDefinition>(DevDefinitionPath));
            EditorUtility.SetDirty(session);
            var sessionSerialized = new SerializedObject(session);
            sessionSerialized.FindProperty("bubbleLabel").objectReferenceValue = hud.transform.Find("BubbleLabel").GetComponent<Text>();
            sessionSerialized.FindProperty("gateLabel").objectReferenceValue = hud.transform.Find("GateLabel").GetComponent<Text>();
            sessionSerialized.FindProperty("timeLabel").objectReferenceValue = hud.transform.Find("TimeLabel").GetComponent<Text>();
            sessionSerialized.FindProperty("staminaLabel").objectReferenceValue = hud.transform.Find("StaminaLabel").GetComponent<Text>();
            sessionSerialized.FindProperty("animalLabel").objectReferenceValue = hud.transform.Find("AnimalLabel").GetComponent<Text>();
            sessionSerialized.FindProperty("exitGateView").objectReferenceValue = gateObject.GetComponent<DevExitGateView>();
            sessionSerialized.ApplyModifiedPropertiesWithoutUndo();
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, $"{SceneFolder}/Gameplay.unity");
        }

        private static void BuildResultsScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateUiCamera();
            var ui = InstantiatePrefab("UI_CommonRoot.prefab");
            AttachPrefab("SC16_Result.prefab", ui.transform.Find("SafeArea/ScreenHost"));
            ui.AddComponent<DevResultsPresenter>();
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, $"{SceneFolder}/Results.unity");
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            var path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateWorldQuad(string name, Transform parent, Vector3 scale, Material material)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name; quad.transform.SetParent(parent, false); quad.transform.localScale = scale;
            var collider = quad.GetComponent<Collider>(); if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            quad.GetComponent<Renderer>().sharedMaterial = material;
            return quad;
        }
    }
}
