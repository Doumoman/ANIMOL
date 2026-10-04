using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.Editor
{
    public static partial class UiBuildPipeline
    {
        private static void BuildM6MultiplayerFollowup()
        {
            ConfigureM6MultiplayerPreviewData();
            MutatePrefab("SC05_CompetitiveHub", root =>
            {
                EnsureButton(root, "RankedDevPreviewButton", "DEV ONLY · 랭크 20:9/3맵 미리보기", new Vector2(.75f, .22f), new Vector2(510, 82), Amber);
                var normal = root.transform.Find("CompetitiveDevPreviewButton")?.GetComponent<RectTransform>();
                if (normal != null) normal.anchorMin = normal.anchorMax = new Vector2(.42f, .22f);
            });
            MutatePrefab("SC06_MatchRoom", BuildM6RoomPreview);
            MutatePrefab("SC07_MultiplayerAnimalSelect", BuildM6SelectionPreview);
            MutatePrefab("HUD_Competitive", BuildM6CompetitiveHud);
            MutatePrefab("HUD_Coop", BuildM6CoopHud);
        }

        private static void ConfigureM6MultiplayerPreviewData()
        {
            var competitive = AssetDatabase.LoadAssetAtPath<MultiplayerPreviewDefinition>(CompetitivePreviewPath);
            if (competitive == null) throw new InvalidOperationException("Competitive development preview asset is missing.");
            var serialized = new SerializedObject(competitive);
            serialized.FindProperty("slotCount").intValue = 3;
            serialized.FindProperty("allowDuplicates").boolValue = false;
            var animals = serialized.FindProperty("allowedAnimalIds");
            animals.arraySize = 3;
            animals.GetArrayElementAtIndex(0).stringValue = "DEV_GROUND";
            animals.GetArrayElementAtIndex(1).stringValue = "DEV_GLIDER";
            animals.GetArrayElementAtIndex(2).stringValue = "DEV_SPECIAL";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var coop = AssetDatabase.LoadAssetAtPath<MultiplayerPreviewDefinition>(CoopPreviewPath);
            if (coop == null) throw new InvalidOperationException("Coop development preview asset is missing.");
            var coopSerialized = new SerializedObject(coop);
            if (coopSerialized.FindProperty("slotCount").intValue != 2 || coopSerialized.FindProperty("allowDuplicates").boolValue)
                throw new InvalidOperationException("Existing coop duplicate/slot rules changed unexpectedly.");
        }

        private static void BuildM6RoomPreview(GameObject root)
        {
            TrimNet02RoomRows(root);
            var old = root.transform.Find("RoomParticipants");
            if (old != null) old.gameObject.SetActive(false);
            SetRect(root.transform.Find("MatchRoomMode") as RectTransform, new Vector2(.5f, .82f), new Vector2(1300, 52));
            EnsureLabel(root, "RoomCapacity", "가변 참가자", 25, new Vector2(.5f, .75f), new Vector2(1300, 40));
            EnsureLabel(root, "RoomGrowthMode", "성장 적용", 24, new Vector2(.5f, .69f), new Vector2(1200, 38));
            EnsureLabel(root, "RoomDuplicateRule", "중복 규칙", 24, new Vector2(.5f, .63f), new Vector2(1250, 38));
            for (var i = 0; i < MultiplayerModeRules.CompetitiveParticipants.Maximum; i++)
            {
                EnsureLabel(root, $"RoomParticipant_{i + 1:00}", $"P{i + 1} · Connected · 선택 대기", 22,
                    new Vector2(.5f, .55f - i * .072f), new Vector2(700, 36));
            }
            EnsureLabel(root, "RoomAuthorityTruth", "DEV UI 상태만 변경 · 운영 성공 없음", 22, new Vector2(.5f, .21f), new Vector2(1250, 38));
            EnsureButton(root, "RoomParticipantCountToggleButton", "경쟁 4명 고정", new Vector2(.73f, .11f), new Vector2(300, 64), Cyan);
            root.transform.Find("RoomParticipantCountToggleButton").GetComponent<Button>().interactable = false;
            EnsureButton(root, "RoomConnectionPreviewButton", "끊김→복구 미리보기", new Vector2(.53f, .11f), new Vector2(330, 64), Panel);
            SetRect(root.transform.Find("RoomAnimalSelectButton") as RectTransform, new Vector2(.32f, .11f), new Vector2(350, 64));
            SetRect(root.transform.Find("MatchRoomBackButton") as RectTransform, new Vector2(.1f, .06f), new Vector2(280, 62));
        }

        [MenuItem("ANIMOL/NET02 Existing UI DEV/Apply TASK 02 Room Capacity")]
        public static void ApplyNet02RoomCapacity()
        {
            // Do not run the full UI builder over the user's portrait/layout changes.
            MutatePrefab("SC06_MatchRoom", root =>
            {
                TrimNet02RoomRows(root);
                var button = root.transform.Find("RoomParticipantCountToggleButton")?.GetComponent<Button>();
                var label = button == null ? null : button.GetComponentInChildren<Text>(true);
                if (label != null) label.text = "경쟁 4명 고정";
                if (button != null) button.interactable = false; // Runtime enables it for coop only.
            });
        }

        private static void TrimNet02RoomRows(GameObject root)
        {
            int maximum = Math.Max(MultiplayerModeRules.CompetitiveParticipants.Maximum, MultiplayerModeRules.CoopParticipants.Maximum);
            foreach (var row in root.GetComponentsInChildren<Text>(true))
                if (row.name.StartsWith("RoomParticipant_", StringComparison.Ordinal) &&
                    int.TryParse(row.name.Substring("RoomParticipant_".Length), out var slot) && slot > maximum)
                    UnityEngine.Object.DestroyImmediate(row.gameObject);
        }

        private static void BuildM6SelectionPreview(GameObject root)
        {
            EnsureButton(root, "AnimalCard_03", "DEV_SPECIAL", new Vector2(.65f, .47f), new Vector2(300, 180), Cyan);
            SetRect(root.transform.Find("AnimalCard_01") as RectTransform, new Vector2(.24f, .47f), new Vector2(300, 180));
            SetRect(root.transform.Find("AnimalCard_02") as RectTransform, new Vector2(.45f, .47f), new Vector2(300, 180));
            SetRect(root.transform.Find("AnimalCard_Locked") as RectTransform, new Vector2(.82f, .47f), new Vector2(250, 180));
            EnsureLabel(root, "CompetitiveDuplicateProof", "경쟁 참가자 간 동일 특수 동물 선택 허용 · 협동은 모드 데이터 유지", 23, new Vector2(.5f, .36f), new Vector2(1350, 44));
        }

        private static void BuildM6CompetitiveHud(GameObject root)
        {
            var legacyConnection = root.transform.Find("CompetitiveConnectionState");
            if (legacyConnection != null) legacyConnection.name = "CompetitiveHudConnectionState";
            EnsureLabel(root, "CompetitiveSeriesState", "현재 맵 1/3 · 3개 맵 연속 경기", 28, new Vector2(.5f, .84f), new Vector2(1100, 45));
            EnsureLabel(root, "CompetitiveMapResults", "맵1 InProgress | 맵2 Pending | 맵3 Pending\n총합 0점 · 순위 서버 미확정", 23, new Vector2(.5f, .75f), new Vector2(1450, 72));
            SetRect(root.transform.Find("HudObjective") as RectTransform, new Vector2(.5f, .64f), new Vector2(1400, 70));
            EnsureLabel(root, "CompetitiveBubbleAuthority", "동일 슬롯 선점: 서버 확정/재생성 대기", 23, new Vector2(.5f, .55f), new Vector2(1250, 42));
            EnsureLabel(root, "CompetitiveFinishWindow", "첫 완주/추가 완주 시간: 서버 대기", 23, new Vector2(.5f, .49f), new Vector2(1200, 42));
            EnsureLabel(root, "CompetitiveHudConnectionState", "연결: Connected", 23, new Vector2(.5f, .43f), new Vector2(1100, 42));
            EnsureLabel(root, "CompetitiveNoCombat", "몸 공격 ✕ · 넉백 ✕ · 시간 종료 수행 점수는 서버 확정", 23, new Vector2(.5f, .37f), new Vector2(1250, 42));
            SetRect(root.transform.Find("HudServerTruth") as RectTransform, new Vector2(.5f, .31f), new Vector2(1300, 42));
            EnsureButton(root, "CompetitiveSeriesAdvanceButton", "DEV 맵 결과 진행", new Vector2(.72f, .22f), new Vector2(300, 60), Amber);
            EnsureButton(root, "CompetitiveConnectionCycleButton", "끊김→재접속", new Vector2(.28f, .22f), new Vector2(300, 60), Panel);
            RepositionEmotes(root, "CompetitiveHudEmote_", .12f);
        }

        private static void BuildM6CoopHud(GameObject root)
        {
            SetRect(root.transform.Find("HudObjective") as RectTransform, new Vector2(.5f, .72f), new Vector2(1350, 80));
            EnsureLabel(root, "CoopRosterState", "팀 2~4명 · 이모티콘/위치 핑", 27, new Vector2(.5f, .58f), new Vector2(1200, 50));
            EnsureLabel(root, "CoopReconnectState", "팀원 연결: Connected", 25, new Vector2(.5f, .49f), new Vector2(1100, 45));
            SetRect(root.transform.Find("HudServerTruth") as RectTransform, new Vector2(.5f, .4f), new Vector2(1300, 50));
            EnsureButton(root, "CoopLocationPingButton", "위치 핑 · 서버 미연결", new Vector2(.68f, .3f), new Vector2(350, 64), Amber);
            EnsureButton(root, "CoopConnectionCycleButton", "팀원 끊김→복구", new Vector2(.32f, .3f), new Vector2(330, 64), Panel);
            RepositionEmotes(root, "CoopHudEmote_", .16f);
        }

        private static void RepositionEmotes(GameObject root, string prefix, float y)
        {
            for (var i = 0; i < 4; i++)
                SetRect(root.transform.Find($"{prefix}{i + 1:00}") as RectTransform, new Vector2(.32f + i * .12f, y), new Vector2(200, 76));
        }

        private static void EnsureButton(GameObject root, string name, string text, Vector2 anchor, Vector2 size, Color color)
        {
            var button = root.transform.Find(name)?.GetComponent<Button>();
            if (button == null) button = CreateButton(name, root.transform, text, anchor, size, color);
            else
            {
                var label = button.GetComponentInChildren<Text>(true);
                if (label != null) label.text = text;
            }
            SetRect(button.transform as RectTransform, anchor, size);
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }

        private static List<string> ValidateM6MultiplayerFollowup(List<string> errors)
        {
            var competitive = AssetDatabase.LoadAssetAtPath<MultiplayerPreviewDefinition>(CompetitivePreviewPath);
            var coop = AssetDatabase.LoadAssetAtPath<MultiplayerPreviewDefinition>(CoopPreviewPath);
            if (competitive == null || competitive.SlotCount != 3 || competitive.AllowedAnimalIds.Count != 3 || competitive.AllowDuplicates)
                errors.Add("Competitive preview must expose one ground/air/special slot without same-player duplicate roles.");
            if (coop == null || coop.SlotCount != 2 || coop.AllowDuplicates)
                errors.Add("Existing coop two-slot duplicate rule changed.");
            if (!MultiplayerModeRules.CompetitiveParticipants.Contains(4) || MultiplayerModeRules.CompetitiveParticipants.Contains(5) ||
                MultiplayerModeRules.CompetitiveParticipants.Contains(3) || !MultiplayerModeRules.CoopParticipants.Contains(2) ||
                !MultiplayerModeRules.CoopParticipants.Contains(4) || MultiplayerModeRules.CoopParticipants.Contains(5))
                errors.Add("Multiplayer participant ranges are invalid.");

            var ids = Enumerable.Range(1, 4).Select(x => $"P{x}").ToArray();
            var rules = new[]
            {
                new CampaignAnimalUnlockRule("G", MultiplayerAnimalRole.Ground, string.Empty),
                new CampaignAnimalUnlockRule("A", MultiplayerAnimalRole.Air, string.Empty),
                new CampaignAnimalUnlockRule("S", MultiplayerAnimalRole.Special, string.Empty)
            };
            var selection = new CompetitiveParticipantSelectionService(ids, rules, CampaignAnimalUnlockSnapshot.DevelopmentOnly("G", "A", "S"));
            foreach (var id in new[] { "P1", "P2" })
            {
                selection.TrySelect(id, MultiplayerAnimalRole.Ground, "G");
                selection.TrySelect(id, MultiplayerAnimalRole.Air, "A");
                selection.TrySelect(id, MultiplayerAnimalRole.Special, "S");
            }
            if (!selection.CanReady("P1") || !selection.CanReady("P2") || !selection.AllowsSameAnimalAcrossParticipants)
                errors.Add("Competitive cross-participant duplicate special selection did not remain ready-eligible.");

            var room = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/SC06_MatchRoom.prefab");
            var roomRows = room == null ? 0 : room.GetComponentsInChildren<Text>(true).Count(x => x.name.StartsWith("RoomParticipant_", StringComparison.Ordinal));
            var competitiveHud = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/HUD_Competitive.prefab");
            var coopHud = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/HUD_Coop.prefab");
            if (roomRows != 4) errors.Add("Match room must contain exactly four participant rows.");
            foreach (var required in new[] { "CompetitiveSeriesState", "CompetitiveMapResults", "CompetitiveBubbleAuthority", "CompetitiveFinishWindow", "CompetitiveHudConnectionState", "CompetitiveNoCombat" })
                if (competitiveHud == null || competitiveHud.GetComponentsInChildren<Transform>(true).All(x => x.name != required)) errors.Add("Competitive HUD missing " + required);
            foreach (var required in new[] { "CoopRosterState", "CoopReconnectState", "CoopLocationPingButton" })
                if (coopHud == null || coopHud.GetComponentsInChildren<Transform>(true).All(x => x.name != required)) errors.Add("Coop HUD missing " + required);

            var series = new CompetitiveSeriesState();
            if (series.Maps.Count != 3 || series.HasAuthoritativeRanking || MultiplayerModeRules.AllowCompetitiveBodyAttack || MultiplayerModeRules.AllowCompetitiveKnockback)
                errors.Add("Competitive series/no-combat authority contract mismatch.");
            foreach (var participantCount in new[] { 2, 4 })
            {
                var objective = new CoopTeamObjectiveState(participantCount);
                objective.ApplyServerProgress(3, participantCount - 1, true);
                if (objective.IsComplete) errors.Add($"Co-op {participantCount}P completed before every member exited.");
                objective.ApplyServerProgress(3, participantCount, true);
                if (!objective.IsComplete) errors.Add($"Co-op {participantCount}P did not complete with three bubbles and all members exited.");
            }

            return new List<string>
            {
                "competitiveParticipantRange=4-4", "customMinimum=server-policy", "coopParticipantRange=2-4", $"roomParticipantRows={roomRows}",
                "competitiveRoleSlots=3", "competitiveCrossParticipantDuplicates=true", "coopDuplicateRulePreserved=true", "coopCompletionRequiresThreeBubblesAndAllExits=true",
                "competitiveSeriesMaps=3", "competitiveBodyAttack=false", "competitiveKnockback=false",
                "rankedGrowth=MAX_PRESET", "casualCoopGrowth=OWNED_PROGRESS", "operationalReady=false",
                "serverRankingReward=false", "disconnectReconnectStates=4", "ranked20x9WorldView=expanded"
            };
        }

        public static string OpenM6CompetitiveRoomForCapture(bool ranked, bool disconnected)
        {
            InvokeRuntimeButton("CompetitiveButton");
            InvokeRuntimeButton(ranked ? "RankedDevPreviewButton" : "CompetitiveDevPreviewButton");
            if (disconnected) InvokeRuntimeButton("RoomConnectionPreviewButton");
            return $"competitiveRoom=4 ranked={ranked} disconnected={disconnected}";
        }

        public static string OpenM6CoopRoomForCapture(bool fourPlayers, bool disconnected)
        {
            InvokeRuntimeButton("CoopButton");
            InvokeRuntimeButton("CoopDevPreviewButton");
            if (fourPlayers) InvokeRuntimeButton("RoomParticipantCountToggleButton");
            if (disconnected) InvokeRuntimeButton("RoomConnectionPreviewButton");
            return $"coopRoom={(fourPlayers ? 4 : 2)} disconnected={disconnected}";
        }

        public static string OpenM6CompetitiveHudForCapture(bool ranked, bool completeSeries, bool restored)
        {
            OpenM6CompetitiveRoomForCapture(ranked, false);
            if (restored)
            {
                InvokeRuntimeButton("RoomConnectionPreviewButton");
                InvokeRuntimeButton("RoomConnectionPreviewButton");
                InvokeRuntimeButton("RoomConnectionPreviewButton");
            }
            InvokeRuntimeButton("RoomAnimalSelectButton");
            InvokeRuntimeButton("AnimalCard_01");
            InvokeRuntimeButton("AnimalCard_02");
            InvokeRuntimeButton("AnimalCard_03");
            InvokeRuntimeButton("MultiplayerHudPreviewButton");
            if (completeSeries)
            {
                InvokeRuntimeButton("CompetitiveSeriesAdvanceButton");
                InvokeRuntimeButton("CompetitiveSeriesAdvanceButton");
                InvokeRuntimeButton("CompetitiveSeriesAdvanceButton");
            }
            return $"competitiveHud ranked={ranked} seriesComplete={completeSeries} restored={restored}";
        }

        public static string OpenM6RankedWorldForCapture()
        {
            var host = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(x => x.name == "ScreenHost");
            if (host == null) throw new InvalidOperationException("Gameplay ScreenHost is missing.");
            foreach (Transform child in host) child.gameObject.SetActive(false);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiPrefabFolder}/HUD_Competitive.prefab");
            if (prefab == null) throw new InvalidOperationException("Competitive HUD prefab is missing.");
            var instance = UnityEngine.Object.Instantiate(prefab, host);
            instance.name = "HUD_Competitive_RankedWorldCapture";
            var background = instance.GetComponent<Image>();
            if (background != null) background.color = new Color(background.color.r, background.color.g, background.color.b, .18f);
            var title = instance.GetComponentsInChildren<Text>(true).FirstOrDefault(x => x.name == "HudModeTitle");
            if (title != null) title.text = "DEV 랭크 · 20:9 월드 시야 · 성장 최대치 프리셋";
            var truth = instance.GetComponentsInChildren<Text>(true).FirstOrDefault(x => x.name == "HudServerTruth");
            if (truth != null) truth.text = "운영 서버 미연결 · 순위/보상/Ready 미확정";
            return "rankedWorld=20:9 expanded operational=false";
        }

        private static void InvokeRuntimeButton(string name)
        {
            var button = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(x => x.name == name);
            if (button == null) throw new InvalidOperationException($"Runtime button '{name}' is missing.");
            button.onClick.Invoke();
        }
    }
}
