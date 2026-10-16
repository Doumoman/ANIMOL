using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using ANIMOL.Presentation;

namespace ANIMOL.Puzzle
{
    public sealed class PuzzleController : MonoBehaviour
    {
        public PuzzleMap stage;
        public GameObject hudPrefab;
        [Range(.05f, .6f)] public float secondsPerCell = .18f;
        public PuzzleSession Session { get; private set; }
        public PuzzleMap RuntimeMap { get; private set; }
        public Transform Player { get; private set; }
        public StarwayCharacterVisual CharacterVisual { get; private set; }
        public PuzzleFollowCamera FollowCamera { get; private set; }
        public PuzzleMapView View { get; private set; }
        public bool IsMoving { get; private set; }
        public int PortalCuts { get; private set; }
        public bool BlocksInput => false;
        public string Status { get; private set; } = "";
        public event Action PortalCut;
        Vector2Int queuedInput;
        Transform presentation;
        PuzzleHudView hud;
        void Start()
        {
            if (stage && stage.scenario)
            {
                var prototype = gameObject.AddComponent<PuzzlePrototypeController>(); prototype.stage = stage; prototype.hudPrefab = hudPrefab;
                enabled = false; return;
            }
            if (stage) Load(stage);
        }
        public void Load(PuzzleMap data)
        {
            StopAllCoroutines(); IsMoving = false; queuedInput = Vector2Int.zero; PortalCuts = 0;
            if (presentation) { presentation.gameObject.SetActive(false); Destroy(presentation.gameObject); }
            if (RuntimeMap) Destroy(RuntimeMap);
            RuntimeMap = Instantiate(data); RuntimeMap.name = data.name + " (runtime)"; RuntimeMap.InvalidateIndex();
            var errors = RuntimeMap.ValidateMap();
            if (errors.Count != 0 || !RuntimeMap.theme) { Session = null; Status = "Invalid map: " + string.Join("; ", errors); Debug.LogError(Status); return; }
            stage = data; Session = new PuzzleSession(RuntimeMap); Status = "";
            presentation = new GameObject("Puzzle Presentation").transform; presentation.SetParent(transform, false);
            View = new GameObject("Terrain Grid").AddComponent<PuzzleMapView>(); View.transform.SetParent(presentation, false); View.Bind(RuntimeMap);
            CharacterVisual = RuntimeMap.theme.CharacterCatalog
                ? StarwayCharacterVisual.Create(presentation, Session.Cell, 40, RuntimeMap.theme.CharacterCatalog,
                    RuntimeMap.theme.CharacterId)
                : null;
            Player = CharacterVisual
                ? CharacterVisual.transform.parent
                : PuzzleMapView.Sprite(presentation, "Player", RuntimeMap.theme.player, Session.Cell, 40,
                    Vector2.one * .85f, Color.white).transform;
            var cam = Camera.main;
            if (!cam) { cam = new GameObject("Puzzle Camera").AddComponent<Camera>(); cam.tag = "MainCamera"; }
            cam.backgroundColor = RuntimeMap.theme.background;
            FollowCamera = cam.GetComponent<PuzzleFollowCamera>(); if (!FollowCamera) FollowCamera = cam.gameObject.AddComponent<PuzzleFollowCamera>();
            FollowCamera.Configure(RuntimeMap, Player);
            Background(); View.Present(Session);
            Session.Changed += () => View.Present(Session);
            RuntimeMap.Resized += FollowCamera.Cut;
            if (!GetComponent<PuzzleSwipeInput>()) gameObject.AddComponent<PuzzleSwipeInput>();
            GetComponent<PuzzleSwipeInput>().CancelGesture();
            hud = null;
            if (hudPrefab)
            {
                var instance = Instantiate(hudPrefab, presentation);
                hud = instance.GetComponentInChildren<PuzzleHudView>();
                if (!EventSystem.current) new GameObject("Puzzle EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).transform.SetParent(transform, false);
            }
        }
        void Background()
        {
            var backdrop = new GameObject("Fixed portrait background").AddComponent<PuzzleBackdrop>(); backdrop.transform.SetParent(presentation, false);
            backdrop.Configure(RuntimeMap.theme, FollowCamera.GetComponent<Camera>(), RuntimeMap.cameraSettings);
        }
        public bool RequestMove(Vector2Int direction)
        {
            if (Session == null || !PuzzleSession.IsCardinal(direction) || Session.Won) return false;
            if (IsMoving) { queuedInput = direction; return true; }
            if (!Session.BeginMove(direction)) { Status = "Blocked"; return false; }
            if (CharacterVisual) CharacterVisual.SetMotion(direction, "move");
            IsMoving = true; Status = ""; StartCoroutine(MoveChain(Session.Cell + direction)); return true;
        }
        IEnumerator MoveChain(Vector2Int destination)
        {
            var visited = new HashSet<string>(); var teleport = false;
            do
            {
                if (teleport)
                {
                    Player.position = (Vector2)destination; Session.Arrive(destination);
                    FollowCamera.Cut(); PortalCuts++; PortalCut?.Invoke();
                    // Render the exit before continuing preserved-direction movement next frame.
                    yield return null;
                }
                else
                {
                    var from = Player.position; float elapsed = 0;
                    while (elapsed < secondsPerCell)
                    {
                        elapsed += Time.deltaTime; Player.position = Vector3.Lerp(from, (Vector2)destination, Mathf.Clamp01(elapsed / secondsPerCell));
                        yield return null;
                    }
                    // Terrain may change while traveling: never commit an invalid arrival.
                    if (!RuntimeMap.Walkable(destination)) { Player.position = (Vector2)Session.Cell; Status = "Route changed"; break; }
                    Player.position = (Vector2)destination; Session.Arrive(destination);
                }
                var key = Session.Cell + ":" + Session.Direction + ":" + Session.Sliding + ":" + teleport;
                if (!visited.Add(key)) { Status = "Automatic loop stopped"; break; }
                if (!Session.NextAutomatic(teleport, out destination, out teleport)) break;
                if (CharacterVisual) CharacterVisual.SetMotion(Session.Direction, Session.Sliding ? "sliding" : "move");
            } while (true);
            Session.StopSliding(); IsMoving = false;
            if (CharacterVisual)
            {
                if (Session.Won) CharacterVisual.PlayPose(CharacterVisual.Direction, "victory");
                else CharacterVisual.SetIdle();
            }
            if (Session.Won) Status = "Stage clear";
            var next = queuedInput; queuedInput = Vector2Int.zero;
            if (next != Vector2Int.zero) RequestMove(next);
        }
        void LateUpdate()
        {
            if (hud && Session != null) hud.bubbles.text = Session.Collected + "/" + RuntimeMap.targetBubbles;
        }
        public bool SelectStarwayCharacter(string id)
        {
            return SelectCharacter(id);
        }
        public bool SelectMemoryTheaterCharacter(string id) => SelectCharacter(id);
        public bool SelectCharacter(string id) => CharacterVisual &&
            CharacterVisual.Initialize(RuntimeMap.theme.CharacterCatalog, id);
        void OnDestroy() { if (RuntimeMap) Destroy(RuntimeMap); }
    }
}
