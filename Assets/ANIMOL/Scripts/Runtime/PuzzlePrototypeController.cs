using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using ANIMOL.Presentation;
using Animol.Art.StarwayRetouch;
namespace ANIMOL.Puzzle
{
    public sealed class PuzzlePrototypeController : MonoBehaviour
    {
        public PuzzleMap stage;
        public GameObject hudPrefab;
        public float secondsPerCell = .16f;
        public PuzzleDeviceSession Session { get; private set; }
        public StarwayV3StrictPolicy Strict { get; private set; }
        public PuzzleMap RuntimeMap { get; private set; }
        public Transform Player { get; private set; }
        public StarwayCharacterVisual CharacterVisual { get; private set; }
        public PuzzleFollowCamera FollowCamera { get; private set; }
        public PuzzleDeviceView Devices { get; private set; }
        public bool IsMoving { get; private set; }
        public bool BlocksInput => false;
        public string Status { get; private set; }
        int rejectedVisualSerial;
        int presentationGeneration, committedActionSerial;
        bool pressingGoal;
        Vector2Int queuedInput;
        Transform presentation;
        PuzzleMapView view;
        PuzzleHudView hud;
        void Start() { if (stage && Session == null) Load(stage); }
        public void Load(PuzzleMap data)
        {
            if (CharacterVisual && CharacterVisual.PushPresenter) CharacterVisual.PushPresenter.ResetPresentation();
            if (Devices) Devices.InvalidateDiceHints();
            presentationGeneration++;
            StopAllCoroutines(); IsMoving = pressingGoal = false; queuedInput = Vector2Int.zero;
            if (presentation) { presentation.gameObject.SetActive(false); Destroy(presentation.gameObject); }
            if (RuntimeMap) Destroy(RuntimeMap);
            RuntimeMap = Instantiate(data); RuntimeMap.InvalidateIndex(); stage = data; Session = new PuzzleDeviceSession(RuntimeMap);
            Strict = RuntimeMap.starwayV3Certificate ? new StarwayV3StrictPolicy(RuntimeMap) : null;
            presentation = new GameObject("Prototype presentation").transform; presentation.SetParent(transform, false);
            view = new GameObject("Terrain grid").AddComponent<PuzzleMapView>(); view.transform.SetParent(presentation, false); view.Bind(RuntimeMap);
            CharacterVisual = RuntimeMap.theme.CharacterCatalog
                ? StarwayCharacterVisual.Create(presentation, Session.Cell, 40, RuntimeMap.theme.CharacterCatalog,
                    RuntimeMap.theme.CharacterId, RuntimeMap.theme.CharacterCostumeId)
                : null;
            var renderer = CharacterVisual
                ? CharacterVisual.visual
                : PuzzleMapView.Sprite(presentation, "Player", RuntimeMap.theme.player, Session.Cell, 40,
                    Vector2.one * .85f, Color.white);
            Player = CharacterVisual ? CharacterVisual.transform.parent : renderer.transform;
            var cam = Camera.main; if (!cam) { cam = new GameObject("Main Camera").AddComponent<Camera>(); cam.tag = "MainCamera"; }
            FollowCamera = cam.GetComponent<PuzzleFollowCamera>(); if (!FollowCamera) FollowCamera = cam.gameObject.AddComponent<PuzzleFollowCamera>(); FollowCamera.Configure(RuntimeMap, Player);
            if (view.HasStarSwellCoastV01)
                view.ConfigureStarSwellCoastV01Background(cam);
            else if (view.HasSunsetSanctumV02)
                view.ConfigureSunsetSanctumV02Background(cam);
            else if (RuntimeMap.theme.starwayRanch)
                StarwayRanchBackground.Create(presentation, cam, RuntimeMap.theme.starwayRanch);
            else
            {
                var backdrop = new GameObject("Native pixel background").AddComponent<PuzzleBackdrop>();
                backdrop.transform.SetParent(presentation, false);
                backdrop.Configure(RuntimeMap.theme, cam, RuntimeMap.cameraSettings,
                    RuntimeMap.scenario && RuntimeMap.scenario.echoTheaterV3Rules);
            }
            Devices = new GameObject("Committed devices").AddComponent<PuzzleDeviceView>(); Devices.transform.SetParent(presentation, false); Devices.Bind(Session, renderer, secondsPerCell); view.Present(Session); view.RestoreRanchNests(Session);
            var input = GetComponent<PuzzleSwipeInput>(); if (!input) input = gameObject.AddComponent<PuzzleSwipeInput>(); input.SetReceiver(RequestMove);
            if (hudPrefab)
            {
                hud = Instantiate(hudPrefab, presentation).GetComponentInChildren<PuzzleHudView>();
                if (!EventSystem.current) new GameObject("Puzzle EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).transform.SetParent(transform, false);
            }
            Status = "";
            ContinueWind();
            if (!IsMoving) Devices.PublishDiceHints(!Session.Won && !Session.Failed && Strict == null);
        }
        public bool RequestMove(Vector2Int direction)
        {
            if (Session == null || Session.Won || Session.Failed || Strict != null && Strict.Failed ||
                !PuzzleSession.IsCardinal(direction)) return false;
            if (IsMoving) { if (pressingGoal) return false; queuedInput = direction; return true; }
            Devices.InvalidateDiceHints();
            PrototypeAction result = null;
            if (Strict != null && !Strict.TryApply(Session, direction, out result))
            {
                Status = Strict.Error ?? result.reason;
                Devices.PublishDiceHints(false);
                return false;
            }
            if (Strict == null) result = Session.Apply(direction);
            if (!result.accepted)
            {
                Status = result.reason;
                foreach (var cue in result.events) if (cue.kind == PrototypeEventKind.Visual) Devices.Notify(cue, "reject:" + (++rejectedVisualSerial));
                Devices.PublishDiceHints(!Session.Won && !Session.Failed && Strict == null);
                return false;
            }
            Devices.CommitSanctumBeamAction();
            IsMoving = true;
            pressingGoal = result.events.Exists(e => e.eventName == "dice_goal_absorbed");
            var eventPrefix = GetInstanceID() + ":" + presentationGeneration + ":" + (++committedActionSerial) + ":";
            StartCoroutine(Animate(result, eventPrefix, presentationGeneration, direction)); return true;
        }
        public bool UndoLast()
        {
            if (Session == null || Strict != null) return false;
            if (CharacterVisual && CharacterVisual.PushPresenter) CharacterVisual.PushPresenter.ResetPresentation();
            Devices.InvalidateDiceHints();
            presentationGeneration++;
            StopAllCoroutines(); IsMoving = pressingGoal = false; queuedInput = Vector2Int.zero;
            if (!Session.Undo())
            {
                Devices.PublishDiceHints(!Session.Won && !Session.Failed);
                return false;
            }
            PresentRestoredState(); return true;
        }
        public void ResetStage()
        {
            if (Session == null) return;
            if (CharacterVisual && CharacterVisual.PushPresenter) CharacterVisual.PushPresenter.ResetPresentation();
            Devices.InvalidateDiceHints();
            presentationGeneration++;
            StopAllCoroutines(); IsMoving = pressingGoal = false; queuedInput = Vector2Int.zero;
            Session.Reset(); PresentRestoredState();
            Strict?.Reset();
        }
        public void RestoreSnapshot(PuzzleDeviceSession snapshot)
        {
            if (Session == null) return;
            if (CharacterVisual && CharacterVisual.PushPresenter) CharacterVisual.PushPresenter.ResetPresentation();
            Devices.InvalidateDiceHints();
            presentationGeneration++;
            StopAllCoroutines(); IsMoving = pressingGoal = false; queuedInput = Vector2Int.zero;
            Session.RestoreSnapshot(snapshot); PresentRestoredState();
            Strict?.Reset();
        }
        void PresentRestoredState()
        {
            view.CancelRanchEffects(); Devices.Settle(true); view.Present(Session); view.RestoreRanchNests(Session);
            Player.position = (Vector2)Session.Cell;
            if (CharacterVisual) CharacterVisual.SetIdle();
            if (FollowCamera) FollowCamera.Cut();
            Status = Session.Won ? "Stage clear" : Session.Failed ? Session.Failure : "";
            ContinueWind();
            if (!IsMoving) Devices.PublishDiceHints(!Session.Won && !Session.Failed && Strict == null);
        }
        void ContinueWind()
        {
            // Certified V3 stages compare every state with their authored input transcript.
            if (IsMoving || Session == null || Session.Won || Session.Failed || Strict != null) return;
            Devices.InvalidateDiceHints();
            var result = Session.AdvanceWind();
            if (!result.accepted) { Devices.PublishDiceHints(true); return; }
            IsMoving = true;
            var eventPrefix = GetInstanceID() + ":" + presentationGeneration + ":" + (++committedActionSerial) + ":";
            StartCoroutine(Animate(result, eventPrefix, presentationGeneration, Vector2Int.zero));
        }
        IEnumerator Animate(PrototypeAction result, string eventPrefix, int generation, Vector2Int inputDirection)
        {
            var consumed = new HashSet<PrototypeEvent>();
            var pushStarted = false;
            int dominoPanelsRemaining = result.events.FindAll(e =>
                e.kind == PrototypeEventKind.Visual && e.eventName == "this_domino_piece_fold_committed").Count;
            foreach (var e in result.events)
            {
                if (generation != presentationGeneration) yield break;
                if (consumed.Contains(e)) continue;
                if (e.kind == PrototypeEventKind.Visual)
                {
                    if (e.eventName == "ChickReturned")
                    {
                        var chick = Devices.Actor(e.actor);
                        if (chick) chick.gameObject.SetActive(false);
                        if (!view.PlayCommittedChickReturn(e, eventPrefix + e.actor + ":" + e.group, Session))
                            Debug.LogError("R4 committed chick return has no valid nest presentation: " + e.actor, this);
                        continue;
                    }
                    var ranchSeconds = view.PlayRanchCue(e);
                    Devices.Notify(e, Session.Cost + ":" + e.group);
                    if (ranchSeconds > 0) yield return new WaitForSeconds(ranchSeconds);
                    if (e.eventName == "this_domino_piece_fold_committed" && --dominoPanelsRemaining > 0)
                        yield return new WaitForSeconds(Mathf.Min(.1f, secondsPerCell * .5f));
                    continue;
                }
                if (e.kind != PrototypeEventKind.Move) continue;
                var actor = e.actor == "player" ? Player : Devices.Actor(e.actor);
                if (!actor) actor = view.RanchActor(e.actor);
                if (!pushStarted && inputDirection != Vector2Int.zero && e.actor != "player" &&
                    CharacterVisual && CharacterVisual.PushPresenter && CharacterVisual.PushPresenter.enabled &&
                    e.from == Vector2Int.RoundToInt(Player.position) + inputDirection &&
                    IsCommittedPlayerPush(result, e, inputDirection))
                {
                    CharacterVisual.SetMotion(inputDirection, "push");
                    // Keep the committed PUSH clip through the player event even when the
                    // destination consumes the target and no contact renderer remains.
                    pushStarted = CharacterVisual.State == "push";
                    if (pushStarted && !actor)
                        yield return new WaitForSeconds(secondsPerCell);
                    if (actor)
                    {
                        var worldDirection = presentation.TransformVector(
                            new Vector3(inputDirection.x, inputDirection.y, 0f));
                        var contact = CreatePushVisualContact(actor, worldDirection);
                        if (contact)
                        {
                            CharacterVisual.PushPresenter.BeginPush(actor.GetComponentInChildren<Collider2D>(),
                                worldDirection, contact);
                            // The session has already committed this move. Let the hand reach the
                            // stationary visual face before animating the object away from its cell.
                            var preparationDeadline = Time.time + .14f;
                            while (CharacterVisual && CharacterVisual.PushPresenter &&
                                CharacterVisual.PushPresenter.PoseActive &&
                                !CharacterVisual.PushPresenter.ContactEstablished &&
                                Time.time < preparationDeadline)
                                yield return null;
                        }
                    }
                }
                if (!actor) continue;
                if (Devices.HasRanchV13Motion(e.actor))
                {
                    var path = new List<Vector2Int> { e.from };
                    var windBySegment = new List<bool>();
                    bool contiguous = true;
                    foreach (var step in result.events)
                    {
                        if (step.kind != PrototypeEventKind.Move || step.actor != e.actor || consumed.Contains(step)) continue;
                        consumed.Add(step);
                        if (step.teleport || step.from != path[path.Count - 1] ||
                            (step.to - step.from).sqrMagnitude != 1) contiguous = false;
                        path.Add(step.to);
                        bool wind = false;
                        foreach (var cue in result.events)
                        {
                            if (cue.kind != PrototypeEventKind.Visual || cue.eventName != "valid_action_wind_resolved" ||
                                cue.group > step.group || cue.cells == null || !cue.cells.Contains(step.from) ||
                                !cue.cells.Contains(step.to)) continue;
                            wind = true; break;
                        }
                        windBySegment.Add(wind);
                    }
                    var final = Session.States[e.actor];
                    if (contiguous && !final.removed &&
                        Devices.BeginRanchV13Motion(e.actor, path, windBySegment, secondsPerCell))
                        yield return new WaitForSeconds(secondsPerCell * windBySegment.Count);
                    else actor.localPosition = (Vector2)final.cell;
                    continue;
                }
                if (e.actor == "player" && CharacterVisual && !pushStarted)
                    CharacterVisual.SetMotion(e.to - e.from, "move");
                var pondCue = e.actor == "player" ? result.events.Find(p =>
                    p.kind == PrototypeEventKind.Visual && p.eventName == "PondCrossed" &&
                    p.from == e.from && p.to == e.to && !consumed.Contains(p)) : null;
                var moveSeconds = secondsPerCell;
                if (pondCue != null)
                {
                    consumed.Add(pondCue);
                    moveSeconds = view.PlayRanchCue(pondCue);
                    if (moveSeconds <= 0f) moveSeconds = secondsPerCell;
                    Devices.Notify(pondCue, Session.Cost + ":" + pondCue.group);
                }
                if (view.RanchActor(e.actor))
                {
                    var wind = result.events.Exists(cue => cue.kind == PrototypeEventKind.Visual &&
                        cue.eventName == "valid_action_wind_resolved" && cue.group <= e.group &&
                        cue.cells != null && cue.cells.Contains(e.from) && cue.cells.Contains(e.to));
                    view.BeginRanchMotion(e.actor, e.to - e.from, wind);
                }
                Transform passenger = null;
                var passengerDestination = e.to;
                var definition = Session.Definition(e.actor);
                if (definition != null && definition.role == PrototypeRole.Boat)
                {
                    var paired = result.events.Find(p => p.kind == PrototypeEventKind.Move && p.actor == "player" && p.from == e.from && p.to == e.to && p.group > e.group && !consumed.Contains(p));
                    if (paired != null) { passenger = Player; consumed.Add(paired); }
                }
                else if (definition != null && definition.role == PrototypeRole.MovingStage)
                {
                    var heading = e.to - e.from;
                    var pushed = result.events.Find(p => p.kind == PrototypeEventKind.Move && p.actor == "player" &&
                        p.from == e.to && p.to - p.from == heading && p.group > e.group && !consumed.Contains(p));
                    if (pushed != null)
                    {
                        passenger = Player;
                        passengerDestination = pushed.to;
                        consumed.Add(pushed);
                        if (CharacterVisual) CharacterVisual.SetMotion(heading, "move");
                    }
                }
                // Play the existing committed body-motion clip while the grid transform actually moves.
                foreach (var cue in result.events.FindAll(p => p.kind == PrototypeEventKind.Visual &&
                    p.actor == e.actor && p.group == e.group + 1 && p.eventName != "ChickReturned"))
                {
                    if (view.RanchActor(e.actor)) continue;
                    Devices.Notify(cue, Session.Cost + ":" + cue.group); consumed.Add(cue);
                }
                if (e.teleport) { actor.position = (Vector2)e.to; if (e.actor == "player") FollowCamera.Cut(); yield return null; }
                else
                {
                    var from = actor.position; var passengerFrom = passenger ? passenger.position : Vector3.zero; var elapsed = 0f;
                    while (elapsed < moveSeconds) { elapsed += Time.deltaTime; var progress = Mathf.Clamp01(elapsed / moveSeconds); actor.position = Vector3.Lerp(from, (Vector2)e.to, progress); if (passenger) passenger.position = Vector3.Lerp(passengerFrom, (Vector2)passengerDestination, progress); Devices.SetWindActionProgress01(progress); yield return null; }
                    actor.position = (Vector2)e.to;
                    if (passenger) passenger.position = (Vector2)passengerDestination;
                }
                if (view.RanchActor(e.actor))
                    foreach (var cue in result.events.FindAll(p => p.kind == PrototypeEventKind.Visual &&
                        p.actor == e.actor && p.group == e.group + 1 && !consumed.Contains(p)))
                    { view.PlayRanchCue(cue); consumed.Add(cue); }
            }
            if (result.events.Exists(e => e.eventName == "dice_goal_absorbed"))
            {
                queuedInput = Vector2Int.zero;
                yield return Devices.PlayGoalPresses();
                pressingGoal = false;
            }
            Devices.Settle(); view.Present(Session); Player.position = (Vector2)Session.Cell;
            if (CharacterVisual && CharacterVisual.PushPresenter) CharacterVisual.PushPresenter.EndPush();
            if (CharacterVisual)
            {
                if (Session.Won) CharacterVisual.PlayPose(CharacterVisual.Direction, "victory");
                else CharacterVisual.SetIdle();
            }
            Status = Session.Won ? "Stage clear" : Session.Failed ? Session.Failure : "";
            IsMoving = false; var next = queuedInput; queuedInput = Vector2Int.zero;
            if (next != Vector2Int.zero && RequestMove(next)) yield break;
            ContinueWind();
            if (!IsMoving) Devices.PublishDiceHints(!Session.Won && !Session.Failed && Strict == null);
        }
        bool IsCommittedPlayerPush(PrototypeAction action, PrototypeEvent movement, Vector2Int heading)
        {
            var device = Session.Definition(movement.actor);
            if (device != null && movement.to - movement.from == heading)
                switch (device.role)
                {
                    case PrototypeRole.StarSocket:
                    case PrototypeRole.WoodCrate:
                    case PrototypeRole.MetalCrate:
                    case PrototypeRole.Barrel:
                    case PrototypeRole.Bomb:
                    case PrototypeRole.Dice:
                    case PrototypeRole.Glass:
                        return true;
                }
            return movement.to - movement.from == heading && action.events.Exists(cue =>
                cue.kind == PrototypeEventKind.Visual && cue.actor == movement.actor &&
                (cue.eventName == "SheepPushed" || cue.eventName == "SheepAteAndRemoved"));
        }

        Transform CreatePushVisualContact(Transform actor, Vector3 worldHeading)
        {
            if (!CharacterVisual || !CharacterVisual.visual ||
                !CharacterVisual.catalog || worldHeading.sqrMagnitude < 0.0001f) return null;
            var definition = CharacterVisual.catalog.Find(CharacterVisual.characterId);
            StarwayPushSpriteContacts.Entry anchor;
            if (definition == null || !definition.pushContacts ||
                !definition.pushContacts.TryGet(CharacterVisual.visual.sprite, out anchor)) return null;
            var handLocal = anchor.handLocal;
            if (CharacterVisual.visual.flipX) handLocal.x = -handLocal.x;
            var handWorld = CharacterVisual.visual.transform.TransformPoint(handLocal);
            var direction = new Vector2(worldHeading.x, worldHeading.y).normalized;
            var tangent = new Vector2(-direction.y, direction.x);
            var face = float.PositiveInfinity;
            var tangentMin = float.PositiveInfinity;
            var tangentMax = float.NegativeInfinity;
            // The session may have already marked a stone removed after it entered its socket.
            // Its starting visual still supplies the contact face for this committed push.
            foreach (var renderer in actor.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!renderer.enabled || !renderer.sprite) continue;
                var sprite = renderer.sprite;
                var vertices = new List<Vector2>();
                var shapeCount = sprite.GetPhysicsShapeCount();
                for (var shape = 0; shape < shapeCount; shape++)
                {
                    vertices.Clear();
                    sprite.GetPhysicsShape(shape, vertices);
                    foreach (var vertex in vertices)
                    {
                        var local = new Vector3(renderer.flipX ? -vertex.x : vertex.x,
                            renderer.flipY ? -vertex.y : vertex.y, 0f);
                        var corner = (Vector2)renderer.transform.TransformPoint(local);
                        face = Mathf.Min(face, Vector2.Dot(corner, direction));
                        var projection = Vector2.Dot(corner, tangent);
                        tangentMin = Mathf.Min(tangentMin, projection);
                        tangentMax = Mathf.Max(tangentMax, projection);
                    }
                }
                if (shapeCount != 0) continue;
                // A few procedural sprites have no generated alpha outline.
                var bounds = renderer.bounds;
                foreach (var x in new[] { bounds.min.x, bounds.max.x })
                    foreach (var y in new[] { bounds.min.y, bounds.max.y })
                    {
                        var corner = new Vector2(x, y);
                        face = Mathf.Min(face, Vector2.Dot(corner, direction));
                        var projection = Vector2.Dot(corner, tangent);
                        tangentMin = Mathf.Min(tangentMin, projection);
                        tangentMax = Mathf.Max(tangentMax, projection);
                    }
            }
            if (float.IsInfinity(face)) return null;
            var alongFace = Mathf.Clamp(Vector2.Dot(handWorld, tangent), tangentMin, tangentMax);
            var point = actor.Find("PushVisualContact");
            if (!point)
            {
                point = new GameObject("PushVisualContact").transform;
                point.SetParent(actor, false);
            }
            point.position = direction * face + tangent * alongFace;
            return point;
        }

        void LateUpdate()
        {
            if (!hud || Session == null) return;
            hud.cost.text = Strict != null && Strict.Failed
                ? Strict.LastDecision == StarwayV3StrictPolicy.Decision.TransitionMismatch ? "MAP ERROR" : "WRONG INPUT · RESET"
                : "COST " + (RuntimeMap.scenario.costLimit > 0 ? Mathf.Max(0, RuntimeMap.scenario.costLimit - Session.Cost).ToString() : Session.Cost.ToString());
            hud.bubbles.text = Session.Collected + "/" + RuntimeMap.targetBubbles;
        }
        void OnDestroy() { if (Devices) Devices.InvalidateDiceHints(); if (RuntimeMap) Destroy(RuntimeMap); }
        public bool SelectStarwayCharacter(string id)
        {
            return SelectCharacter(id);
        }
        public bool SelectMemoryTheaterCharacter(string id) => SelectCharacter(id);
        public bool SelectLetterObservatoryCharacter(string id) => RuntimeMap && RuntimeMap.theme &&
            RuntimeMap.theme.letterObservatoryCharacters && SelectCharacter(id);
        public bool SelectTwilightSeaCharacter(string id) => RuntimeMap && RuntimeMap.theme &&
            RuntimeMap.theme.twilightSeaCharacters && SelectCharacter(id);
        public bool SelectTwilightSeaCostume(string costume) => RuntimeMap && RuntimeMap.theme &&
            RuntimeMap.theme.twilightSeaCharacters && CharacterVisual && CharacterVisual.SetCostume(costume);
        public bool SelectCharacter(string id) => CharacterVisual &&
            CharacterVisual.Initialize(RuntimeMap.theme.CharacterCatalog, id);
    }
}
