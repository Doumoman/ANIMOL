using System.Collections;
using System.Linq;
using ANIMOL.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ANIMOL.Tests
{
    public sealed class M6MultiplayerUiFlowTests
    {
        [UnityTest]
        public IEnumerator CompetitiveFourPlayerThreeMapPreview_RejectsCountToggleAndRemainsNonOperational()
        {
            SceneManager.LoadScene("Lobby");
            // Allow the current portrait / Missing UI installers to finish.
            for (var frame = 0; frame < 6; frame++) yield return null;
            var navigation = Object.FindFirstObjectByType<UiNavigationService>();
            var presenter = Object.FindFirstObjectByType<MultiplayerUiPresenter>();
            var root = navigation.transform;

            // Exercise the isolated legacy DEV route; operational entry remains unavailable.
            Assert.That(navigation.Navigate("SC05_CompetitiveHub"), Is.True);
            Click(root, "CompetitiveDevPreviewButton");
            Assert.That(presenter.PreviewParticipantCount, Is.EqualTo(4));
            Assert.That(ActiveParticipantRows(root), Is.EqualTo(4));
            Assert.That(Text(root, "RoomGrowthMode"), Does.Contain("보유 성장치"));
            Assert.That(FindButton(root, "RoomParticipantCountToggleButton").interactable, Is.False);
            Click(root, "RoomParticipantCountToggleButton");
            Assert.That(presenter.PreviewParticipantCount, Is.EqualTo(4));
            Assert.That(ActiveParticipantRows(root), Is.EqualTo(4));
            Assert.That(Text(root, "MatchRoomMode"), Does.Not.Contain("8명"));
            Click(root, "RoomConnectionPreviewButton");
            Assert.That(Text(root, "RoomParticipant_02"), Does.Contain("Disconnected"));

            Click(root, "RoomAnimalSelectButton");
            Click(root, "AnimalCard_01");
            Click(root, "AnimalCard_02");
            Click(root, "AnimalCard_03");
            Assert.That(Text(root, "ReadyReason"), Does.Contain("Ready 가능"));
            Assert.That(FindButton(root, "MultiplayerReadyButton").interactable, Is.False);
            Assert.That(Text(root, "CompetitiveDuplicateProof"), Does.Contain("동일 특수 동물 선택 허용"));

            Click(root, "MultiplayerHudPreviewButton");
            Assert.That(navigation.CurrentScreenId, Is.EqualTo("HUD_Competitive"));
            Assert.That(Text(root, "CompetitiveHudConnectionState"), Does.Contain("Disconnected"));
            Assert.That(Text(root, "CompetitiveSeriesState"), Does.Contain("1/3"));
            Click(root, "CompetitiveSeriesAdvanceButton");
            Click(root, "CompetitiveSeriesAdvanceButton");
            Click(root, "CompetitiveSeriesAdvanceButton");
            Assert.That(Text(root, "CompetitiveMapResults"), Does.Contain("총합 600점"));
            Assert.That(Text(root, "CompetitiveMapResults"), Does.Contain("순위 서버 미확정"));
            Assert.That(Text(root, "CompetitiveNoCombat"), Does.Contain("몸 공격 ✕"));
            Assert.That(Text(root, "CompetitiveNoCombat"), Does.Contain("넉백 ✕"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RankedPreview_UsesMaximumGrowthAndSupportsDisconnectRecoveryStates()
        {
            SceneManager.LoadScene("Lobby");
            // Allow the current portrait / Missing UI installers to finish.
            for (var frame = 0; frame < 6; frame++) yield return null;
            var navigation = Object.FindFirstObjectByType<UiNavigationService>();
            var presenter = Object.FindFirstObjectByType<MultiplayerUiPresenter>();
            var root = navigation.transform;

            // Exercise the isolated legacy DEV route; operational entry remains unavailable.
            Assert.That(navigation.Navigate("SC05_CompetitiveHub"), Is.True);
            Click(root, "RankedDevPreviewButton");
            Assert.That(presenter.IsRankedPreview, Is.True);
            Assert.That(presenter.PreviewParticipantCount, Is.EqualTo(4));
            Assert.That(ActiveParticipantRows(root), Is.EqualTo(4));
            Assert.That(FindButton(root, "RoomParticipantCountToggleButton").interactable, Is.False);
            Assert.That(Text(root, "RoomGrowthMode"), Does.Contain("최대치 동일 프리셋"));
            Assert.That(presenter.OperationalReadyEnabled, Is.False);
            Click(root, "RoomConnectionPreviewButton");
            Assert.That(Text(root, "RoomParticipant_02"), Does.Contain("Disconnected"));
            Click(root, "RoomConnectionPreviewButton");
            Assert.That(Text(root, "RoomParticipant_02"), Does.Contain("Reconnecting"));
            Click(root, "RoomConnectionPreviewButton");
            Assert.That(Text(root, "RoomParticipant_02"), Does.Contain("Restored"));
            Click(root, "RoomAnimalSelectButton");
            Click(root, "MultiplayerHudPreviewButton");
            Assert.That(Text(root, "CompetitiveHudConnectionState"), Does.Contain("Restored"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CoopTwoAndFourPlayerHud_ShowsTeamObjectiveReconnectEmoteAndPingPorts()
        {
            SceneManager.LoadScene("Lobby");
            // Allow the current portrait / Missing UI installers to finish.
            for (var frame = 0; frame < 6; frame++) yield return null;
            var navigation = Object.FindFirstObjectByType<UiNavigationService>();
            var presenter = Object.FindFirstObjectByType<MultiplayerUiPresenter>();
            var root = navigation.transform;

            Assert.That(navigation.Navigate("SC08_CoopHub"), Is.True);
            Click(root, "CoopDevPreviewButton");
            Assert.That(presenter.PreviewParticipantCount, Is.EqualTo(2));
            Assert.That(ActiveParticipantRows(root), Is.EqualTo(2));
            Assert.That(Text(root, "RoomDuplicateRule"), Does.Contain("모드 데이터"));
            Assert.That(FindButton(root, "RoomParticipantCountToggleButton").interactable, Is.True);
            Click(root, "RoomParticipantCountToggleButton");
            Assert.That(presenter.PreviewParticipantCount, Is.EqualTo(4));
            Assert.That(ActiveParticipantRows(root), Is.EqualTo(4));
            Click(root, "RoomAnimalSelectButton");
            Click(root, "MultiplayerHudPreviewButton");
            Assert.That(navigation.CurrentScreenId, Is.EqualTo("HUD_Coop"));
            Assert.That(Text(root, "HudObjective"), Does.Contain("팀 방울 0/3"));
            Assert.That(Text(root, "HudObjective"), Does.Contain("탈출한 팀원 0/4"));
            Click(root, "CoopConnectionCycleButton");
            Assert.That(Text(root, "CoopReconnectState"), Does.Contain("Disconnected"));
            Click(root, "CoopLocationPingButton");
            Assert.That(Find(root, "ServiceErrorModal").activeSelf, Is.True);
            Assert.That(Text(root, "ServiceErrorTitle"), Does.Contain("위치 핑 전송 실패"));
            yield return null;
        }

        private static int ActiveParticipantRows(Transform root) => root.GetComponentsInChildren<Text>(true)
            .Count(x => x.name.StartsWith("RoomParticipant_") && x.gameObject.activeInHierarchy);
        private static string Text(Transform root, string name) => root.GetComponentsInChildren<Text>(true).First(x => x.name == name).text;
        private static void Click(Transform root, string name) => FindButton(root, name).onClick.Invoke();
        private static Button FindButton(Transform root, string name) => root.GetComponentsInChildren<Button>(true).First(x => x.name == name);
        private static GameObject Find(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).First(x => x.name == name).gameObject;
    }
}
