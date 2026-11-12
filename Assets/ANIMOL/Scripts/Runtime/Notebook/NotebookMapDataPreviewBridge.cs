using System.Collections.Generic;
using System.Linq;
using ANIMOL.Puzzle;
using UnityEngine;
using UnityEngine.EventSystems;

namespace William.NotebookUI
{
    /// <summary>Reads the selected MapData session for the editor preview without changing its rules or assets.</summary>
    public sealed class NotebookMapDataPreviewBridge : MonoBehaviour
    {
        public PuzzleMap sourceMap;
        public string stageIdOverride;
        public PuzzleController puzzle;
        public NotebookGameplayHud hud;
        public NotebookGameplayGateway gateway;
        public GameObject collectibleStarPrefab;

        PuzzlePrototypeController prototype;
        PuzzleDeviceSession observedSession;
        PuzzleMap observedMap;
        PuzzleMapView visualizedView;
        PuzzleSwipeInput input;
        Camera worldCamera;
        string stageId;
        int presentedCollected, observedCost;
        bool clearPresented;
        readonly HashSet<Vector2Int> presentedCells = new HashSet<Vector2Int>();

        public string StageId => stageId;
        public bool Ready => observedSession != null && prototype && prototype.Session == observedSession;
        public bool CanAdvance => Ready && observedSession.Won && !prototype.IsMoving;

        void Start()
        {
            if (!EventSystem.current)
                new GameObject("Gameplay EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        void Update()
        {
            if (!sourceMap || !puzzle || !hud || !gateway) return;
            if (!prototype) prototype = GetComponent<PuzzlePrototypeController>();
            if (!prototype || prototype.Session == null || !prototype.RuntimeMap || !prototype.View) return;
            var session = prototype.Session;
            if (session != observedSession || prototype.RuntimeMap != observedMap ||
                session.Cost < observedCost || session.Collected < presentedCollected)
                Begin(session);

            observedCost = session.Cost;
            hud.SetCost(DisplayedCost(session));
            foreach (var cell in PossibleStarCells())
            {
                if (!session.IsCollected(cell) || !presentedCells.Add(cell)) continue;
                presentedCollected++;
                hud.ConfirmWorldStarCollected(stageId + ":" + cell.x + ":" + cell.y,
                    new Vector3(cell.x, cell.y, 0), worldCamera, presentedCollected);
            }
            if (presentedCollected != session.Collected)
            {
                presentedCollected = session.Collected;
                hud.SynchronizeCollected(presentedCollected);
                presentedCells.Clear();
                foreach (var cell in PossibleStarCells()) if (session.IsCollected(cell)) presentedCells.Add(cell);
            }
            if (!clearPresented && session.Won && !prototype.IsMoving)
            {
                bool hasNext = gateway.TryGetNextStage(stageId, out string nextStage);
                clearPresented = hud.ShowVerifiedClear(new NotebookGameplayClearSnapshot
                {
                    verified = true, exitReached = session.Cell == prototype.RuntimeMap.exit,
                    stageId = stageId, stageLabel = stageId,
                    collectedStars = session.Collected, requiredStars = prototype.RuntimeMap.targetBubbles,
                    hasNextStage = hasNext, canEnterNext = hasNext, nextStageId = nextStage
                });
            }
        }

        void Begin(PuzzleDeviceSession session)
        {
            observedSession = session;
            observedMap = prototype.RuntimeMap;
            observedCost = session.Cost;
            stageId = !string.IsNullOrEmpty(stageIdOverride) ? stageIdOverride : NotebookCampaignStages.DisplayId(sourceMap);
            presentedCollected = session.Collected;
            clearPresented = false;
            presentedCells.Clear();
            foreach (var cell in PossibleStarCells()) if (session.IsCollected(cell)) presentedCells.Add(cell);
            if (collectibleStarPrefab && prototype.View != visualizedView)
            {
                prototype.View.UseNotebookCollectibleVisual(collectibleStarPrefab);
                visualizedView = prototype.View;
            }
            prototype.View.Present(session);
            input = prototype.GetComponent<PuzzleSwipeInput>();
            if (input) { input.enabled = true; input.gameplayHud = hud; }
            prototype.SetUiPaused(false);
            worldCamera = prototype.FollowCamera ? prototype.FollowCamera.GetComponent<Camera>() : null;
            hud.BindGateway(gateway);
            hud.BeginStage(stageId, stageId, DisplayedCost(session), session.Collected, prototype.RuntimeMap.targetBubbles);
        }

        int DisplayedCost(PuzzleDeviceSession session) => observedMap.scenario && observedMap.scenario.costLimit > 0
            ? Mathf.Max(0, observedMap.scenario.costLimit - session.Cost) : session.Cost;

        IEnumerable<Vector2Int> PossibleStarCells() => observedMap.bubbles.Concat(observedMap.scenario.devices.Where(d =>
            d.role == PrototypeRole.StarSocket && d.variant == "socket" &&
            d.socketReward == StarSocketReward.RevealBubble).Select(d => d.socketBubbleCell)).Distinct();

        public bool TrySetPaused(bool paused)
        {
            if (!Ready || !input || prototype.IsMoving || observedSession.Won) return false;
            input.CancelGesture();
            prototype.SetUiPaused(paused);
            input.enabled = !paused;
            return true;
        }
        public bool TryRestartCurrentStage()
        {
            if (!Ready || !sourceMap || prototype.UiPaused || observedSession.Won) return false;
            if (input) input.CancelGesture();
            prototype.ResetStage();
            Begin(prototype.Session);
            return true;
        }
        public bool OpenNextMap(PuzzleMap map, string nextStageId)
        {
            if (!Ready || !map || !observedSession.Won || prototype.IsMoving ||
                !NotebookCampaignStages.TryNext(StageId, out string expected) || expected != nextStageId ||
                NotebookCampaignStages.DisplayId(map) != nextStageId ||
                map.ValidateMap().Count != 0) return false;
            sourceMap = map; stageIdOverride = nextStageId; puzzle.stage = map;
            prototype.Load(map); Begin(prototype.Session); return true;
        }
    }
}
