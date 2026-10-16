using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using ANIMOL.Presentation;
namespace ANIMOL.Puzzle
{
    public sealed class PuzzlePrototypeController : MonoBehaviour
    {
        public PuzzleMap stage;
        public GameObject hudPrefab;
        public float secondsPerCell = .16f;
        public PuzzleDeviceSession Session { get; private set; }
        public PuzzleMap RuntimeMap { get; private set; }
        public Transform Player { get; private set; }
        public StarwayCharacterVisual CharacterVisual { get; private set; }
        public PuzzleFollowCamera FollowCamera { get; private set; }
        public PuzzleDeviceView Devices { get; private set; }
        public bool IsMoving { get; private set; }
        public bool BlocksInput => false;
        public string Status { get; private set; }
        int rejectedVisualSerial;
        Vector2Int queuedInput;
        Transform presentation;
        PuzzleMapView view;
        PuzzleHudView hud;
        void Start() { if (stage && Session == null) Load(stage); }
        public void Load(PuzzleMap data)
        {
            StopAllCoroutines(); IsMoving = false; queuedInput = Vector2Int.zero;
            if (presentation) { presentation.gameObject.SetActive(false); Destroy(presentation.gameObject); }
            if (RuntimeMap) Destroy(RuntimeMap);
            RuntimeMap = Instantiate(data); RuntimeMap.InvalidateIndex(); stage = data; Session = new PuzzleDeviceSession(RuntimeMap);
            presentation = new GameObject("Prototype presentation").transform; presentation.SetParent(transform, false);
            view = new GameObject("Terrain grid").AddComponent<PuzzleMapView>(); view.transform.SetParent(presentation, false); view.Bind(RuntimeMap);
            CharacterVisual = RuntimeMap.theme.CharacterCatalog
                ? StarwayCharacterVisual.Create(presentation, Session.Cell, 40, RuntimeMap.theme.CharacterCatalog,
                    RuntimeMap.theme.CharacterId)
                : null;
            var renderer = CharacterVisual
                ? CharacterVisual.visual
                : PuzzleMapView.Sprite(presentation, "Player", RuntimeMap.theme.player, Session.Cell, 40,
                    Vector2.one * .85f, Color.white);
            Player = CharacterVisual ? CharacterVisual.transform.parent : renderer.transform;
            var cam = Camera.main; if (!cam) { cam = new GameObject("Main Camera").AddComponent<Camera>(); cam.tag = "MainCamera"; }
            FollowCamera = cam.GetComponent<PuzzleFollowCamera>(); if (!FollowCamera) FollowCamera = cam.gameObject.AddComponent<PuzzleFollowCamera>(); FollowCamera.Configure(RuntimeMap, Player);
            var backdrop = new GameObject("Native pixel background").AddComponent<PuzzleBackdrop>(); backdrop.transform.SetParent(presentation, false); backdrop.Configure(RuntimeMap.theme, cam, RuntimeMap.cameraSettings);
            Devices = new GameObject("Committed devices").AddComponent<PuzzleDeviceView>(); Devices.transform.SetParent(presentation, false); Devices.Bind(Session, renderer); view.Present(Session);
            var input = GetComponent<PuzzleSwipeInput>(); if (!input) input = gameObject.AddComponent<PuzzleSwipeInput>(); input.SetReceiver(RequestMove);
            if (hudPrefab)
            {
                hud = Instantiate(hudPrefab, presentation).GetComponentInChildren<PuzzleHudView>();
                if (!EventSystem.current) new GameObject("Puzzle EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).transform.SetParent(transform, false);
            }
            Status = "";
        }
        public bool RequestMove(Vector2Int direction)
        {
            if (Session == null || Session.Won || Session.Failed || !PuzzleSession.IsCardinal(direction)) return false;
            if (IsMoving) { queuedInput = direction; return true; }
            var result = Session.Apply(direction);
            if (!result.accepted)
            {
                Status = result.reason;
                foreach (var cue in result.events) if (cue.kind == PrototypeEventKind.Visual) Devices.Notify(cue, "reject:" + (++rejectedVisualSerial));
                return false;
            }
            IsMoving = true; StartCoroutine(Animate(result)); return true;
        }
        IEnumerator Animate(PrototypeAction result)
        {
            var consumed = new System.Collections.Generic.HashSet<PrototypeEvent>();
            foreach (var e in result.events)
            {
                if (consumed.Contains(e)) continue;
                if (e.kind == PrototypeEventKind.Visual) { Devices.Notify(e, Session.Cost + ":" + e.group); continue; }
                if (e.kind != PrototypeEventKind.Move) continue;
                var actor = e.actor == "player" ? Player : Devices.Actor(e.actor); if (!actor) continue;
                if (e.actor == "player" && CharacterVisual) CharacterVisual.SetMotion(e.to - e.from, "move");
                Transform passenger = null;
                var definition = Session.Definition(e.actor);
                if (definition != null && (definition.role == PrototypeRole.Boat || definition.role == PrototypeRole.MovingStage))
                {
                    var paired = result.events.Find(p => p.kind == PrototypeEventKind.Move && p.actor == "player" && p.from == e.from && p.to == e.to && p.group > e.group && !consumed.Contains(p));
                    if (paired != null) { passenger = Player; consumed.Add(paired); }
                }
                // Play the existing committed body-motion clip while the grid transform actually moves.
                foreach (var cue in result.events.FindAll(p => p.kind == PrototypeEventKind.Visual && p.actor == e.actor && p.group == e.group + 1))
                { Devices.Notify(cue, Session.Cost + ":" + cue.group); consumed.Add(cue); }
                if (e.teleport) { actor.position = (Vector2)e.to; if (e.actor == "player") FollowCamera.Cut(); yield return null; }
                else
                {
                    var from = actor.position; var passengerFrom = passenger ? passenger.position : Vector3.zero; var elapsed = 0f;
                    while (elapsed < secondsPerCell) { elapsed += Time.deltaTime; var progress = Mathf.Clamp01(elapsed / secondsPerCell); actor.position = Vector3.Lerp(from, (Vector2)e.to, progress); if (passenger) passenger.position = Vector3.Lerp(passengerFrom, (Vector2)e.to, progress); yield return null; }
                    actor.position = (Vector2)e.to;
                    if (passenger) passenger.position = (Vector2)e.to;
                }
            }
            Devices.Settle(); view.Present(Session); Player.position = (Vector2)Session.Cell;
            if (CharacterVisual)
            {
                if (Session.Won) CharacterVisual.PlayPose(CharacterVisual.Direction, "victory");
                else CharacterVisual.SetIdle();
            }
            Status = Session.Won ? "Stage clear" : Session.Failed ? Session.Failure : "";
            IsMoving = false; var next = queuedInput; queuedInput = Vector2Int.zero; if (next != Vector2Int.zero) RequestMove(next);
        }
        void LateUpdate()
        {
            if (!hud || Session == null) return;
            hud.cost.text = "COST " + (RuntimeMap.scenario.costLimit > 0 ? Mathf.Max(0, RuntimeMap.scenario.costLimit - Session.Cost).ToString() : Session.Cost.ToString());
            hud.bubbles.text = Session.Collected + "/" + RuntimeMap.targetBubbles;
        }
        void OnDestroy() { if (RuntimeMap) Destroy(RuntimeMap); }
        public bool SelectStarwayCharacter(string id)
        {
            return SelectCharacter(id);
        }
        public bool SelectMemoryTheaterCharacter(string id) => SelectCharacter(id);
        public bool SelectCharacter(string id) => CharacterVisual &&
            CharacterVisual.Initialize(RuntimeMap.theme.CharacterCatalog, id);
    }
}
