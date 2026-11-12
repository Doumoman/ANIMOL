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
        public const string SelectionKey = "ANIMOL.EchoTheater.Selected";
        public string CurrentStageId => mapSet && selected >= 0 && selected < mapSet.maps.Count
            ? mapSet.maps[selected].stageId : "";
        static string ClearKey(string id) => "ANIMOL.EchoTheater.Cleared." + id;
        public int PlayableCount => mapSet ? Mathf.Min(NotebookCampaignStages.PerTheme, mapSet.maps.Count) : 0;
        public bool TryGetNextStage(out string nextStageId)
        {
            nextStageId = null;
            if (!puzzle || puzzle.Session == null || !puzzle.Session.Won || puzzle.IsMoving ||
                !NotebookCampaignStages.TryNext(CurrentStageId, out string expected) || selected + 1 >= PlayableCount ||
                !mapSet.maps[selected + 1].map || mapSet.maps[selected + 1].stageId != expected) return false;
            nextStageId = expected; return true;
        }
        public bool OpenNextStage(string current, string target)
        {
            if (current != CurrentStageId || !TryGetNextStage(out string next) || next != target) return false;
            SelectStage(selected + 1); return true;
        }
        public static bool IsCleared(string id) => NotebookCampaignPreview.IsTemporarilyCleared(id) || IsSavedCleared(id);
        public static bool IsSavedCleared(string id) => PlayerPrefs.GetInt(ClearKey(id), 0) == 1;

        void Start()
        {
            if (!mapSet || mapSet.maps.Count < NotebookCampaignStages.PerTheme) { Debug.LogError("Echo Theater 20-map campaign is missing.", this); enabled = false; return; }
            if (!puzzle) puzzle = gameObject.AddComponent<PuzzlePrototypeController>();
            if (notebookHud && notebookGateway)
            {
                notebookGateway.campaign = this;
                notebookHud.BindGateway(notebookGateway);
                if (!EventSystem.current)
                    new GameObject("Gameplay EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
            SelectStage(Mathf.Clamp(PlayerPrefs.GetInt(SelectionKey, 0), 0, PlayableCount - 1));
        }
        public void SelectStage(int index)
        {
            if (!mapSet || index < 0 || index >= PlayableCount || !mapSet.maps[index].map) return;
            selected = index; savedWin = clearPresented = false; stageMenu = false;
            PlayerPrefs.SetInt(SelectionKey, index);
            puzzle.Load(mapSet.maps[index].map);
            PresentCurrentStage();
        }
        public bool TryRestartCurrentStage()
        {
            if (!puzzle || puzzle.Session == null || puzzle.UiPaused || puzzle.Session.Won) return false;
            var input = puzzle.GetComponent<PuzzleSwipeInput>();
            if (input) input.CancelGesture();
            puzzle.ResetStage();
            savedWin = clearPresented = stageMenu = false;
            PresentCurrentStage();
            return true;
        }
        void PresentCurrentStage()
        {
            if (!notebookHud) return;
            if (collectibleStarPrefab && puzzle.View)
            {
                puzzle.View.UseNotebookCollectibleVisual(collectibleStarPrefab);
                puzzle.View.Present(puzzle.Session);
            }
            presentedStarCells.Clear();
            presentedCollected = puzzle.Session.Collected;
            foreach (var cell in PossibleStarCells()) if (puzzle.Session.IsCollected(cell)) presentedStarCells.Add(cell);
            notebookHud.BeginStage(CurrentStageId, CurrentStageId,
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
            bool hasNext = TryGetNextStage(out string nextStage);
            clearPresented = notebookHud.ShowVerifiedClear(new NotebookGameplayClearSnapshot
            {
                verified = IsCleared(mapSet.maps[selected].stageId), exitReached = puzzle.Session.Cell == puzzle.RuntimeMap.exit,
                stageId = mapSet.maps[selected].stageId, stageLabel = mapSet.maps[selected].stageId,
                collectedStars = puzzle.Session.Collected, requiredStars = puzzle.RuntimeMap.targetBubbles,
                hasNextStage = hasNext, nextStageId = hasNext ? nextStage : "", canEnterNext = hasNext
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
            if (selected + 1 < PlayableCount && GUI.Button(new Rect(160, 43, 60, 32), "Next")) SelectStage(selected + 1);
            if (GUI.Button(new Rect(224, 43, 65, 32), "Lobby")) SceneManager.LoadScene("NotebookUI");
            if (!stageMenu) return;
            GUI.Box(new Rect(10, 90, 355, 258), "Select stage");
            for (var i = 0; i < PlayableCount; i++)
            {
                var x = 18 + i % 5 * 67;
                var y = 114 + i / 5 * 32;
                var label = (i + 1).ToString("00") + (IsCleared(mapSet.maps[i].stageId) ? " *" : "");
                if (GUI.Button(new Rect(x, y, 61, 29), label)) SelectStage(i);
            }
        }
    }
}
