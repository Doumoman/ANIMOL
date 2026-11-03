using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using William.NotebookUI;

namespace ANIMOL.Puzzle
{
    public sealed class EchoTheaterCampaignController : MonoBehaviour
    {
        public EchoTheaterMapSet mapSet;
        public PuzzlePrototypeController puzzle;
        public NotebookGameplayHud notebookHud;
        public NotebookEchoTheaterGateway notebookGateway;
        public GameObject collectibleStarPrefab;
        int selected;
        bool stageMenu;
        bool savedWin;
        bool clearPresented;
        int presentedCollected;
        readonly HashSet<Vector2Int> presentedStarCells = new HashSet<Vector2Int>();
        const string SelectionKey = "ANIMOL.EchoTheater.Selected";
        public string CurrentStageId => mapSet && selected >= 0 && selected < mapSet.maps.Count
            ? mapSet.maps[selected].stageId : "";
        static string ClearKey(string id) => "ANIMOL.EchoTheater.Cleared." + id;
        public static bool IsCleared(string id) => PlayerPrefs.GetInt(ClearKey(id), 0) == 1;

        void Start()
        {
            if (!mapSet || mapSet.maps.Count != 35) { Debug.LogError("Echo Theater 35-map set is missing.", this); enabled = false; return; }
            if (!puzzle) puzzle = gameObject.AddComponent<PuzzlePrototypeController>();
            if (notebookHud && notebookGateway)
            {
                notebookGateway.campaign = this;
                notebookHud.BindGateway(notebookGateway);
                if (!EventSystem.current)
                    new GameObject("Gameplay EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
            SelectStage(Mathf.Clamp(PlayerPrefs.GetInt(SelectionKey, 0), 0, mapSet.maps.Count - 1));
        }
        public void SelectStage(int index)
        {
            if (!mapSet || index < 0 || index >= mapSet.maps.Count || !mapSet.maps[index].map) return;
            selected = index; savedWin = clearPresented = false; stageMenu = false;
            PlayerPrefs.SetInt(SelectionKey, index);
            puzzle.Load(mapSet.maps[index].map);
            if (!notebookHud) return;
            if (collectibleStarPrefab && puzzle.View)
            {
                puzzle.View.UseNotebookCollectibleVisual(collectibleStarPrefab);
                puzzle.View.Present(puzzle.Session);
            }
            presentedStarCells.Clear();
            presentedCollected = puzzle.Session.Collected;
            foreach (var cell in PossibleStarCells()) if (puzzle.Session.IsCollected(cell)) presentedStarCells.Add(cell);
            notebookHud.BeginStage(mapSet.maps[index].stageId, mapSet.maps[index].stageId,
                DisplayedCost(), puzzle.Session.Collected, puzzle.RuntimeMap.targetBubbles);
            var input = puzzle.GetComponent<PuzzleSwipeInput>();
            if (input) input.gameplayHud = notebookHud;
        }
        void Update()
        {
            if (!puzzle || puzzle.Session == null) return;
            if (notebookHud)
            {
                notebookHud.SetCost(DisplayedCost());
                foreach (var cell in PossibleStarCells())
                {
                    if (!puzzle.Session.IsCollected(cell) || !presentedStarCells.Add(cell)) continue;
                    presentedCollected++;
                    notebookHud.ConfirmWorldStarCollected(mapSet.maps[selected].stageId + ":" + cell.x + ":" + cell.y,
                        new Vector3(cell.x, cell.y, 0f), puzzle.FollowCamera.GetComponent<Camera>(), presentedCollected);
                }
                if (presentedCollected != puzzle.Session.Collected)
                {
                    presentedCollected = puzzle.Session.Collected;
                    notebookHud.SynchronizeCollected(presentedCollected);
                }
            }
            if (savedWin || !puzzle.Session.Won) return;
            PlayerPrefs.SetInt(ClearKey(mapSet.maps[selected].stageId), 1);
            PlayerPrefs.Save(); savedWin = true;
            if (notebookHud && !puzzle.IsMoving)
                ShowVerifiedClear();
        }
        void LateUpdate()
        {
            if (savedWin && !clearPresented && notebookHud && puzzle && puzzle.Session != null && puzzle.Session.Won && !puzzle.IsMoving)
                ShowVerifiedClear();
        }
        void ShowVerifiedClear()
        {
            var next = selected + 1;
            bool hasNext = next < 30 && next < mapSet.maps.Count && mapSet.maps[next].map;
            clearPresented = notebookHud.ShowVerifiedClear(new NotebookGameplayClearSnapshot
            {
                verified = IsCleared(mapSet.maps[selected].stageId), exitReached = puzzle.Session.Cell == puzzle.RuntimeMap.exit,
                stageId = mapSet.maps[selected].stageId, stageLabel = mapSet.maps[selected].stageId,
                collectedStars = puzzle.Session.Collected, requiredStars = puzzle.RuntimeMap.targetBubbles,
                hasNextStage = hasNext, nextStageId = hasNext ? mapSet.maps[next].stageId : "",
                canEnterNext = false, nextBlockedReason = "다음 스테이지 입장 서비스가 연결되지 않았어요"
            });
        }
        int DisplayedCost() => puzzle.RuntimeMap.scenario && puzzle.RuntimeMap.scenario.costLimit > 0
            ? Mathf.Max(0, puzzle.RuntimeMap.scenario.costLimit - puzzle.Session.Cost) : puzzle.Session.Cost;
        IEnumerable<Vector2Int> PossibleStarCells()
        {
            var map = puzzle.RuntimeMap;
            return map.bubbles.Concat(map.scenario ? map.scenario.devices.Where(d =>
                d.role == PrototypeRole.StarSocket && d.variant == "socket" &&
                d.socketReward == StarSocketReward.RevealBubble).Select(d => d.socketBubbleCell)
                : Enumerable.Empty<Vector2Int>()).Distinct();
        }
        void OnGUI()
        {
            if (notebookHud) return;
            if (!mapSet || !puzzle || puzzle.Session == null) return;
            GUI.Box(new Rect(10, 10, 355, 74), "Echo Theater " + mapSet.maps[selected].stageId +
                "  " + puzzle.Session.Collected + "/3" + (puzzle.Session.Won ? "  CLEAR" : ""));
            if (GUI.Button(new Rect(18, 43, 74, 32), "Stages")) stageMenu = !stageMenu;
            if (GUI.Button(new Rect(96, 43, 60, 32), "Reset")) puzzle.ResetStage();
            if (GUI.Button(new Rect(160, 43, 60, 32), "Next")) SelectStage((selected + 1) % mapSet.maps.Count);
            if (GUI.Button(new Rect(224, 43, 65, 32), "Lobby")) SceneManager.LoadScene("NotebookUI");
            if (!stageMenu) return;
            GUI.Box(new Rect(10, 90, 355, 258), "Select stage");
            for (var i = 0; i < mapSet.maps.Count; i++)
            {
                var x = 18 + i % 5 * 67;
                var y = 114 + i / 5 * 32;
                var label = (i + 1).ToString("00") + (IsCleared(mapSet.maps[i].stageId) ? " *" : "");
                if (GUI.Button(new Rect(x, y, 61, 29), label)) SelectStage(i);
            }
        }
    }
}
