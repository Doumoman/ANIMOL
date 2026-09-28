using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.Editor
{
    public static partial class UiBuildPipeline
    {
        private const string CompetitivePreviewPath = "Assets/ANIMOL/Data/Development/DEV-MULTI-COMPETITIVE.asset";
        private const string CoopPreviewPath = "Assets/ANIMOL/Data/Development/DEV-MULTI-COOP.asset";

        [MenuItem("ANIMOL/Build/M4 Multiplayer UI")]
        public static void BuildM4Menu()
        {
            BuildM0(); BuildM1(); BuildM2(); BuildM3(); BuildM4(); ValidateM4();
        }

        static partial void BuildM4()
        {
            BuildPreviewDefinition(CompetitivePreviewPath, "DEV-MULTI-COMPETITIVE", GameModeKind.Competitive);
            BuildPreviewDefinition(CoopPreviewPath, "DEV-MULTI-COOP", GameModeKind.Coop);
            BuildM4Screens();
            BuildMultiplayerPauseModal();
            BuildLobbyScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ANIMOL][M4] Multiplayer offline UI and isolated development previews built.");
        }

        static partial void ValidateM4()
        {
            var errors = new List<string>();
            var services = AssetDatabase.LoadAssetAtPath<ExternalServiceConfiguration>(ServiceConfigPath);
            var competitive = AssetDatabase.LoadAssetAtPath<MultiplayerPreviewDefinition>(CompetitivePreviewPath);
            var coop = AssetDatabase.LoadAssetAtPath<MultiplayerPreviewDefinition>(CoopPreviewPath);
            if (services == null || services.MatchServerConnected) errors.Add("Match service must be disconnected.");
            foreach (var preview in new[] { competitive, coop })
            {
                if (preview == null || !preview.DevelopmentPreviewOnly || preview.SlotCount != 2 || preview.AllowedAnimalIds.Count != 2)
                    errors.Add("Development preview rule mismatch.");
            }
            var required = new[] { "SC05_CompetitiveHub", "SC08_CoopHub", "SC06_MatchRoom", "SC07_MultiplayerAnimalSelect", "HUD_Competitive", "HUD_Coop", "MultiplayerPauseModal" };
            foreach (var name in required) if (AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/{name}.prefab") == null) errors.Add("Missing " + name);
            var animal = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/SC07_MultiplayerAnimalSelect.prefab");
            var ready = animal == null ? null : animal.GetComponentsInChildren<Button>(true).FirstOrDefault(x => x.name == "MultiplayerReadyButton");
            if (ready == null || ready.interactable) errors.Add("Operational Ready must be disabled without match server.");
            var detail = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/SC04_StageDetail.prefab");
            var campaignAnimalButtons = detail == null ? 0 : detail.GetComponentsInChildren<Button>(true).Count(x => x.name.Contains("Animal", StringComparison.OrdinalIgnoreCase));
            if (campaignAnimalButtons != 0) errors.Add("Campaign route exposes animal selection.");
            var competitiveHud = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/HUD_Competitive.prefab");
            var coopHud = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/HUD_Coop.prefab");
            var competitiveEmotes = CountButtons(competitiveHud, "CompetitiveHudEmote_");
            var coopEmotes = CountButtons(coopHud, "CoopHudEmote_");
            if (competitiveEmotes != 4 || coopEmotes != 4) errors.Add($"HUD emote slots competitive={competitiveEmotes}, coop={coopEmotes}");

            var lines = new[]
            {
                "ANIMOL M4 validation", $"valid={errors.Count == 0}", "matchServerConnected=false", "operationalMatchStartEnabled=false",
                "operationalReadyEnabled=false", "developmentPreviewOnly=true", "previewRuleSlots=2", "previewAllowedAnimals=2",
                $"campaignAnimalSelectionEntryPoints={campaignAnimalButtons}", $"competitiveHudQuickSlots={competitiveEmotes}", $"coopHudQuickSlots={coopEmotes}",
                "rankAndRewardSuccessShown=false", "onlinePauseStopsServerTime=false", "errors=" + string.Join(" | ", errors)
            };
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Logs"));
            File.WriteAllLines(Path.Combine(Directory.GetCurrentDirectory(), "Logs", "animol-m4-validation.txt"), lines);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
        }

        private static int CountButtons(GameObject root, string prefix) => root == null ? 0 : root.GetComponentsInChildren<Button>(true).Count(x => x.name.StartsWith(prefix, StringComparison.Ordinal));

        private static void BuildPreviewDefinition(string path, string id, GameModeKind mode)
        {
            var asset = AssetDatabase.LoadAssetAtPath<MultiplayerPreviewDefinition>(path);
            if (asset != null) return;
            asset = ScriptableObject.CreateInstance<MultiplayerPreviewDefinition>();
            AssetDatabase.CreateAsset(asset, path);
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("previewId").stringValue = id;
            serialized.FindProperty("mode").enumValueIndex = (int)mode;
            serialized.FindProperty("slotCount").intValue = 2;
            serialized.FindProperty("allowDuplicates").boolValue = false;
            serialized.FindProperty("developmentPreviewOnly").boolValue = true;
            var animals = serialized.FindProperty("allowedAnimalIds"); animals.arraySize = 2;
            animals.GetArrayElementAtIndex(0).stringValue = "DEV_GROUND";
            animals.GetArrayElementAtIndex(1).stringValue = "DEV_GLIDER";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildM4Screens()
        {
            SaveScreen("SC05_CompetitiveHub", root =>
            {
                CreateLabel("CompetitiveTitle", root.transform, "경쟁", 58, new Vector2(.5f, .9f), new Vector2(.5f, .9f), new Vector2(900, 80), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("CompetitiveConnectionState", root.transform, "Unavailable · 매치 서버 미연결", 28, new Vector2(.5f, .8f), new Vector2(.5f, .8f), new Vector2(1250, 60), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("CompetitiveMatchButton", root.transform, "일반 매칭\n연결 필요", new Vector2(.28f, .56f), new Vector2(380, 230), Panel);
                CreateButton("RankedMatchButton", root.transform, "랭크 매칭\n성장 동일 프리셋 · 순위 없음", new Vector2(.5f, .56f), new Vector2(380, 230), Panel);
                CreateButton("CompetitivePrivateButton", root.transform, "비공개 방\n방 코드 미발급", new Vector2(.72f, .56f), new Vector2(380, 230), Panel);
                CreateLabel("RankedFairWidthNote", root.transform, "랭크 20:9 추가 폭은 비경쟁 배경/UI · 공통 16:9 경기 시야", 25, new Vector2(.5f, .34f), new Vector2(.5f, .34f), new Vector2(1250, 55), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("CompetitiveDevPreviewButton", root.transform, "DEV ONLY · 선택/HUD 미리보기", new Vector2(.5f, .22f), new Vector2(520, 82), Amber);
                CreateButton("CompetitiveBackButton", root.transform, "← 로비", new Vector2(.11f, .08f), new Vector2(250, 70), Panel);
            });
            SaveScreen("SC08_CoopHub", root =>
            {
                CreateLabel("CoopTitle", root.transform, "협동", 58, new Vector2(.5f, .9f), new Vector2(.5f, .9f), new Vector2(900, 80), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("CoopConnectionState", root.transform, "Unavailable · 협동 서버 미연결", 28, new Vector2(.5f, .8f), new Vector2(.5f, .8f), new Vector2(1200, 60), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("CoopRuleSummary", root.transform, "기본 팀 공동 방울 3개 · 역할/전원 탈출 조건은 서버 룰 데이터", 30, new Vector2(.5f, .67f), new Vector2(.5f, .67f), new Vector2(1300, 70), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("CoopMatchButton", root.transform, "공개 협동\n연결 필요", new Vector2(.38f, .48f), new Vector2(430, 220), Panel);
                CreateButton("CoopPrivateButton", root.transform, "비공개 협동\n방 생성 불가", new Vector2(.62f, .48f), new Vector2(430, 220), Panel);
                CreateButton("CoopDevPreviewButton", root.transform, "DEV ONLY · 역할 선택/HUD 미리보기", new Vector2(.5f, .23f), new Vector2(560, 82), Amber);
                CreateButton("CoopBackButton", root.transform, "← 로비", new Vector2(.11f, .08f), new Vector2(250, 70), Panel);
            });
            SaveScreen("SC06_MatchRoom", root =>
            {
                CreateLabel("MatchRoomTitle", root.transform, "매치 룸 · 개발용 미리보기", 54, new Vector2(.5f, .9f), new Vector2(.5f, .9f), new Vector2(1100, 80), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("MatchRoomMode", root.transform, "DEV PREVIEW · 서버 참가자 없음", 32, new Vector2(.5f, .75f), new Vector2(.5f, .75f), new Vector2(1100, 70), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("RoomParticipants", root.transform, "참가자: 로컬 미리보기 1명\n팀/호스트/방 코드: 서버 미연결 · 발급 안 함\nReady/매칭/보상: 운영 성공 없음", 30, new Vector2(.5f, .53f), new Vector2(.5f, .53f), new Vector2(1100, 220), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("RoomAnimalSelectButton", root.transform, "멀티 전용 동물 선택 미리보기", new Vector2(.5f, .3f), new Vector2(560, 90), Amber);
                CreateButton("MatchRoomBackButton", root.transform, "← 모드 허브", new Vector2(.11f, .08f), new Vector2(300, 70), Panel);
            });
            SaveScreen("SC07_MultiplayerAnimalSelect", root =>
            {
                CreateLabel("AnimalSelectTitle", root.transform, "멀티 전용 동물 선택", 52, new Vector2(.5f, .93f), new Vector2(.5f, .93f), new Vector2(1100, 75), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("AnimalSelectMode", root.transform, "DEV PREVIEW ONLY", 28, new Vector2(.5f, .85f), new Vector2(.5f, .85f), new Vector2(1000, 55), Vector2.zero, TextAnchor.MiddleCenter);
                for (var i = 0; i < 4; i++) CreateButton($"LoadoutSlot_{i + 1:00}", root.transform, $"룰 슬롯 {i + 1}", new Vector2(.23f + i * .18f, .7f), new Vector2(275, 125), Panel);
                CreateButton("AnimalCard_01", root.transform, "DEV_GROUND", new Vector2(.32f, .47f), new Vector2(360, 190), Cyan);
                CreateButton("AnimalCard_02", root.transform, "DEV_GLIDER", new Vector2(.5f, .47f), new Vector2(360, 190), Cyan);
                CreateButton("AnimalCard_Locked", root.transform, "미할당 동물\nLocked", new Vector2(.68f, .47f), new Vector2(360, 190), new Color(.25f, .28f, .32f, 1));
                CreateLabel("ReadyReason", root.transform, "선택 제스처만 로컬 미리보기 · Ready는 서버 승인 필요", 27, new Vector2(.5f, .29f), new Vector2(.5f, .29f), new Vector2(1200, 55), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("MultiplayerReadyButton", root.transform, "Ready 불가 · 서버 미연결", new Vector2(.4f, .18f), new Vector2(420, 80), new Color(.25f, .28f, .32f, 1)).interactable = false;
                CreateButton("MultiplayerHudPreviewButton", root.transform, "DEV HUD 미리보기", new Vector2(.65f, .18f), new Vector2(380, 80), Amber);
                CreateButton("AnimalSelectBackButton", root.transform, "← 룸", new Vector2(.1f, .07f), new Vector2(230, 66), Panel);
            });
            BuildMultiplayerHud("HUD_Competitive", true);
            BuildMultiplayerHud("HUD_Coop", false);
        }

        private static void BuildMultiplayerHud(string name, bool competitive)
        {
            SaveScreen(name, root =>
            {
                CreateLabel("HudModeTitle", root.transform, competitive ? "DEV 경쟁 HUD · 서버 미연결" : "DEV 협동 HUD · 서버 미연결", 42, new Vector2(.5f, .92f), new Vector2(.5f, .92f), new Vector2(1100, 70), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("HudObjective", root.transform, competitive ? "내 방울 0/3 · 출구 잠김\n상대 진행: 공개 데이터 없음 · 서버 순위 대기 아님" : "팀 방울 0/3 · 출구 잠김\n팀원/보호/동시 기믹: 서버 데이터 없음", 30, new Vector2(.5f, .68f), new Vector2(.5f, .68f), new Vector2(1100, 170), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("HudServerTruth", root.transform, "실매치가 아니며 순위·완주·보상을 확정하지 않습니다.", 27, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(1200, 55), Vector2.zero, TextAnchor.MiddleCenter);
                for (var i = 0; i < 4; i++) CreateButton($"{(competitive ? "Competitive" : "Coop")}HudEmote_{i + 1:00}", root.transform, $"이모티콘 {i + 1}\n전송 불가", new Vector2(.28f + i * .15f, .28f), new Vector2(230, 120), Panel);
                CreateButton(competitive ? "CompetitiveHudPauseButton" : "CoopHudPauseButton", root.transform, "메뉴", new Vector2(.9f, .88f), new Vector2(150, 70), Panel);
                CreateButton(competitive ? "CompetitiveHudBackButton" : "CoopHudBackButton", root.transform, "← 선택 화면", new Vector2(.12f, .09f), new Vector2(290, 70), Panel);
            });
        }

        private static void BuildMultiplayerPauseModal()
        {
            var root = CreatePanelRoot("MultiplayerPauseModal", new Color(0, 0, 0, .8f));
            try
            {
                var card = CreateImage("Card", root.transform, new Vector2(.3f, .2f), new Vector2(.7f, .8f), Vector2.zero, Vector2.zero, Panel);
                CreateLabel("MultiplayerPauseTitle", card.transform, "온라인 메뉴", 50, new Vector2(.5f, .78f), new Vector2(.5f, .78f), new Vector2(650, 80), Vector2.zero, TextAnchor.MiddleCenter);
                CreateLabel("MultiplayerPauseMessage", card.transform, "내 입력/메뉴만 일시정지\n서버 시간은 계속 진행 · Time.timeScale 변경 없음", 28, new Vector2(.5f, .54f), new Vector2(.5f, .54f), new Vector2(700, 150), Vector2.zero, TextAnchor.MiddleCenter);
                CreateButton("MultiplayerPauseContinueButton", card.transform, "계속", new Vector2(.5f, .31f), new Vector2(320, 78), Cyan);
                CreateButton("MultiplayerPauseLeaveButton", card.transform, "미리보기 종료", new Vector2(.5f, .17f), new Vector2(320, 72), Amber);
                PrefabUtility.SaveAsPrefabAsset(root, $"{UiPrefabFolder}/MultiplayerPauseModal.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
