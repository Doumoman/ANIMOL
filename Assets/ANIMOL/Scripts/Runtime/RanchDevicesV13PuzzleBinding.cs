using System;
using System.Collections.Generic;
using System.Reflection;
using ANIMOL.Art.DevicePatch01;
using ANIMOL.Art.RanchDevicesV13;
using ANIMOL.Presentation;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    // Presentation adapter for an existing logical prefab. It never owns occupancy, cost, or collision.
    public sealed class RanchDevicesV13PuzzleBinding : IDisposable
    {
        readonly PrototypeDevice definition;
        readonly GameObject root;
        readonly Transform visualChild;
        readonly Vector3 restLocal;
        readonly Vector3 restScale;
        readonly SpriteRenderer renderer;
        readonly RanchDeviceVisualV13 visual;
        readonly RanchCommittedMotionV13 motion;
        readonly RanchResolvedWindVisualV13 windVisual;
        readonly RanchDevicesV13Catalog catalog;
        readonly PuzzleDeviceSession session;
        readonly float cellSize;
        static readonly FieldInfo motionChildField = typeof(RanchCommittedMotionV13).GetField(
            "visualChild", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo motionVisualField = typeof(RanchCommittedMotionV13).GetField(
            "visual", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo motionCapturedField = typeof(RanchCommittedMotionV13).GetField(
            "captured", BindingFlags.Instance | BindingFlags.NonPublic);
        string settledKey;
        string windSignature;
        string pendingSettledKey;

        public static bool Covers(PrototypeDevice d)
        {
            if (d == null) return false;
            switch (d.role)
            {
                case PrototypeRole.StarSocket:
                case PrototypeRole.Conveyor:
                case PrototypeRole.OneWayGate:
                case PrototypeRole.Bounce:
                case PrototypeRole.Brake:
                case PrototypeRole.Chick:
                case PrototypeRole.WoodCrate:
                case PrototypeRole.MetalCrate: return true;
                case PrototypeRole.Wind: return d.variant == "blower";
                default: return false;
            }
        }

        public RanchDevicesV13PuzzleBinding(PrototypeDevice definition, GameObject existingRoot,
            RanchDevicesV13Catalog catalog, PuzzleDeviceSession session, float cellSize = 1f)
        {
            if (!Covers(definition)) throw new ArgumentException("No V13 clip family for this device.", nameof(definition));
            if (!existingRoot || session == null || session.Rules.themeId != "starway" ||
                float.IsNaN(cellSize) || float.IsInfinity(cellSize) || cellSize <= 0f)
                throw new ArgumentException("Existing Starway logical root, session, and positive cell size are required.");
            string catalogError = null;
            if (!catalog || !catalog.Validate(out catalogError))
                throw new ArgumentException("V13 catalog is missing or incomplete: " + catalogError, nameof(catalog));
            this.definition = definition;
            root = existingRoot;
            this.catalog = catalog;
            this.session = session;
            this.cellSize = cellSize;
            var legacy = root.GetComponent<DeviceVisualView>();
            renderer = legacy && legacy.visual ? legacy.visual : root.transform.Find("Visual")?.GetComponent<SpriteRenderer>();
            if (!renderer || renderer.transform == root.transform || !renderer.transform.IsChildOf(root.transform))
                throw new ArgumentException("Existing prefab needs a dedicated Visual child with a SpriteRenderer.", nameof(existingRoot));
            visualChild = renderer.transform;
            restLocal = visualChild.localPosition;
            restScale = visualChild.localScale;
            visualChild.localScale = restScale * cellSize;
            if (legacy) legacy.enabled = false;
            var oldV12 = root.GetComponent<ANIMOL.Art.V12.StarwayRanchV12AnimatedVisual>();
            if (oldV12) oldV12.enabled = false;
            var rootAnimator = root.GetComponent<Animator>();
            if (rootAnimator) rootAnimator.enabled = false;
            foreach (var animator in visualChild.GetComponents<Animator>()) animator.enabled = false;
            visual = visualChild.GetComponent<RanchDeviceVisualV13>();
            if (!visual) visual = visualChild.gameObject.AddComponent<RanchDeviceVisualV13>();
            if (!visual.Configure(catalog, out var error))
                throw new InvalidOperationException("V13 visual configuration failed: " + error);
            motion = root.GetComponent<RanchCommittedMotionV13>();
            if (!motion) motion = root.AddComponent<RanchCommittedMotionV13>();
            if (motionChildField == null || motionVisualField == null || motionCapturedField == null)
                throw new InvalidOperationException("V13 committed motion serialized fields changed.");
            motion.CancelMotion();
            motionChildField.SetValue(motion, visualChild);
            motionVisualField.SetValue(motion, visual);
            motionCapturedField.SetValue(motion, false);
            if (definition.role == PrototypeRole.Wind)
            {
                var routeRoot = new GameObject("V13 Resolved Wind");
                routeRoot.transform.SetParent(root.transform, false);
                windVisual = routeRoot.AddComponent<RanchResolvedWindVisualV13>();
            }
        }

        // The number is already the puzzle's matching rule. This mapping chooses art only;
        // triangle/diamond/cross are family keys, not new device IDs.
        static bool TryStarShape(int number, out string shape)
        {
            shape = number == 1 ? "triangle" : number == 2 ? "diamond" : number == 3 ? "cross" : null;
            return shape != null;
        }

        static bool TryDirection(Vector2Int direction, out string value)
        {
            value = direction == Vector2Int.up ? "north" : direction == Vector2Int.right ? "east" :
                direction == Vector2Int.down ? "south" : direction == Vector2Int.left ? "west" : null;
            return value != null;
        }

        public static bool TrySettledKey(PrototypeDevice d, PrototypeDeviceState state,
            out string key, out string error)
        {
            key = null; error = null;
            if (!Covers(d) || state == null) { error = "Missing supported device or resolved state."; return false; }
            if (!TryDirection(state.direction, out var direction))
            { error = "Resolved device direction must be cardinal."; return false; }
            switch (d.role)
            {
                case PrototypeRole.StarSocket:
                    if (!TryStarShape(d.number, out var shape))
                    { error = "Star pattern number must be 1, 2, or 3."; return false; }
                    key = "star/" + shape + "/" + (d.variant == "socket" ? state.active ? "filled" : "empty" : "stone"); break;
                case PrototypeRole.Conveyor: key = "conveyor/" + direction + "/idle"; break;
                case PrototypeRole.OneWayGate: key = "oneway_gate/" + direction + "/idle"; break;
                case PrototypeRole.Bounce: key = "bounce_corner/" + PuzzleDeviceSession.Corner(d.orientation) + "/idle"; break;
                case PrototypeRole.Brake: key = "brake/" + direction + "/idle"; break;
                case PrototypeRole.Wind:
                    if (state.phase < 0 || state.phase > 3)
                    { error = "Wind phase must be the resolved off/3/6/9 setting."; return false; }
                    key = "windmill/" + direction + "/" + (state.phase == 0 ? "off" : (state.phase * 3).ToString()); break;
                case PrototypeRole.Chick: key = "chick/" + direction + "/" + (state.active ? "sit" : "idle"); break;
                case PrototypeRole.WoodCrate: key = "wood_crate/idle"; break;
                case PrototypeRole.MetalCrate: key = "metal_crate/idle"; break;
            }
            return key != null;
        }

        public bool Settle(PrototypeDeviceState state)
        {
            if (state == null) return false;
            motion.CancelMotion();
            root.transform.localPosition = new Vector3(state.cell.x * cellSize, state.cell.y * cellSize, root.transform.localPosition.z);
            visualChild.localPosition = restLocal;
            root.SetActive(!state.removed);
            if (state.removed) { windVisual?.ClearOwned(); windSignature = null; pendingSettledKey = null; return true; }
            if (!TrySettledKey(definition, state, out var key, out var error))
            { Debug.LogError("V13 settle: " + error + " " + definition.id, root); return false; }
            settledKey = key;
            if (pendingSettledKey != key || visual.Finished) pendingSettledKey = null;
            if (pendingSettledKey == null && visual.CurrentKey != key && !visual.SetState(key)) return false;
            return RefreshWind(session.WindRouteFor(definition.id), state);
        }

        public bool Notify(PrototypeEvent cue, PrototypeDeviceState state)
        {
            if (cue == null || state == null) return false;
            if (definition.role == PrototypeRole.Wind && cue.eventName == "valid_action_wind_resolved")
                return RefreshWind(cue.windRoute, state);
            string key = null;
            if (definition.role == PrototypeRole.StarSocket)
            {
                if (!TryStarShape(definition.number, out var shape)) return false;
                if (cue.eventName == "pattern_mismatch_rejected") key = "star/" + shape + "/reject";
                else if (cue.eventName == "matching_star_socket_committed") key = "star/" + shape + "/seat";
            }
            else if (definition.role == PrototypeRole.Conveyor && cue.eventName == "conveyor_visual_running")
            { if (!TryDirection(state.direction, out var direction)) return false; key = "conveyor/" + direction + "/run"; }
            else if (definition.role == PrototypeRole.OneWayGate)
            {
                if (!TryDirection(state.direction, out var direction)) return false;
                if (cue.eventName == "allowed_passage_begin") key = "oneway_gate/" + direction + "/open";
                else if (cue.eventName == "allowed_passage_clear") key = "oneway_gate/" + direction + "/close";
                else if (cue.eventName == "existing_rejected_gate_contact") key = "oneway_gate/" + direction + "/reject";
            }
            else if (definition.role == PrototypeRole.Bounce && cue.eventName == "existing_bounce_resolved")
                key = "bounce_corner/" + PuzzleDeviceSession.Corner(definition.orientation) + "/bounce";
            else if (definition.role == PrototypeRole.Brake && cue.eventName == "forced_motion_stopped")
            { if (!TryDirection(state.direction, out var direction)) return false; key = "brake/" + direction + "/stop"; }
            else if (definition.role == PrototypeRole.Chick && cue.eventName == "existing_recorded_path_following")
            { if (!TryDirection(state.direction, out var direction)) return false; key = "chick/" + direction + "/walk"; }
            if (key == null) return true;
            if (!visual.SetState(key, true)) return false;
            pendingSettledKey = catalog.TryClip(key, out var clip) && !clip.loop &&
                TrySettledKey(definition, state, out var after, out _) ? after : null;
            return true;
        }

        public void Tick()
        {
            if (pendingSettledKey == null || !visual.Finished) return;
            var next = pendingSettledKey;
            pendingSettledKey = null;
            visual.SetState(next);
        }

        public bool BeginCommittedMove(IReadOnlyList<Vector2Int> actualTraversedCells,
            PrototypeDeviceState finalState, float secondsPerCell, bool wind, out string error)
        {
            return BeginCommittedMoveCore(actualTraversedCells, finalState, secondsPerCell, wind, null, out error);
        }

        // One flag per traversed edge. A direct push followed by wind uses push, then wind.
        public bool BeginCommittedMove(IReadOnlyList<Vector2Int> actualTraversedCells,
            PrototypeDeviceState finalState, float secondsPerCell,
            IReadOnlyList<bool> windBySegment, out string error)
        {
            return BeginCommittedMoveCore(actualTraversedCells, finalState, secondsPerCell,
                false, windBySegment, out error);
        }

        bool BeginCommittedMoveCore(IReadOnlyList<Vector2Int> actualTraversedCells,
            PrototypeDeviceState finalState, float secondsPerCell, bool wind,
            IReadOnlyList<bool> windBySegment, out string error)
        {
            error = null;
            if (actualTraversedCells == null || actualTraversedCells.Count < 2 || finalState == null ||
                finalState.removed || actualTraversedCells[actualTraversedCells.Count - 1] != finalState.cell)
            { error = "Motion needs the actual contiguous committed path and final occupied cell."; return false; }
            if (windBySegment != null && windBySegment.Count != actualTraversedCells.Count - 1)
            { error = "Supply one wind flag per traversed segment."; return false; }
            if (definition.role != PrototypeRole.Chick && definition.role != PrototypeRole.WoodCrate &&
                definition.role != PrototypeRole.StarSocket && definition.role != PrototypeRole.MetalCrate)
            { error = "This device has no V13 committed movement presentation."; return false; }
            if (!TrySettledKey(definition, finalState, out var endKey, out error)) return false;
            root.transform.localPosition = new Vector3(finalState.cell.x * cellSize, finalState.cell.y * cellSize,
                root.transform.localPosition.z);
            var centers = new Vector3[actualTraversedCells.Count];
            var segmentKeys = new string[centers.Length - 1];
            var parent = root.transform.parent;
            for (int i = 0; i < centers.Length; i++)
            {
                var cell = actualTraversedCells[i];
                var local = new Vector3(cell.x * cellSize, cell.y * cellSize, root.transform.localPosition.z);
                centers[i] = (parent ? parent.TransformPoint(local) : local) + root.transform.TransformVector(restLocal);
                if (i == 0) continue;
                var delta = cell - actualTraversedCells[i - 1];
                if (!TryDirection(delta, out var direction))
                { error = "Committed motion contains a jump or noncardinal segment."; return false; }
                bool segmentWind = windBySegment == null ? wind : windBySegment[i - 1];
                if (definition.role == PrototypeRole.MetalCrate && segmentWind)
                { error = "Metal crate does not travel on wind."; return false; }
                segmentKeys[i - 1] = definition.role == PrototypeRole.Chick ?
                    "chick/" + direction + "/" + (segmentWind ? "wind" : "walk") :
                    definition.role == PrototypeRole.WoodCrate ? "wood_crate/" + (segmentWind ? "wind" : "push") : endKey;
            }
            var step = parent ? parent.TransformVector(Vector3.right * cellSize).magnitude : cellSize;
            return motion.BeginCommittedPath(centers, step, secondsPerCell, segmentKeys[0], endKey, segmentKeys, out error);
        }

        bool RefreshWind(WindRoute route, PrototypeDeviceState state)
        {
            if (!windVisual) return true;
            int range = state.phase * 3;
            if (range != 0 && range != 3 && range != 6 && range != 9) return false;
            if (route != null && route.maxRangeCells != range) return false;
            var signature = range.ToString();
            if (route != null) foreach (var cell in route.cells)
                signature += ";" + cell.cell.x + "," + cell.cell.y + "," + cell.localForceHeading;
            if (signature == windSignature) return true;
            var cells = route == null ? Array.Empty<RanchWindCellV13>() : new RanchWindCellV13[route.cells.Count];
            var parent = root.transform.parent;
            for (int i = 0; i < cells.Length; i++)
            {
                var c = route.cells[i];
                var local = new Vector3(c.cell.x * cellSize, c.cell.y * cellSize, root.transform.localPosition.z);
                var heading = c.localForceHeading == WindHeading.North ? "north" :
                    c.localForceHeading == WindHeading.East ? "east" :
                    c.localForceHeading == WindHeading.South ? "south" : "west";
                cells[i] = new RanchWindCellV13 { center = parent ? parent.TransformPoint(local) : local, direction = heading };
            }
            float worldCellSize = parent ? parent.TransformVector(Vector3.right * cellSize).magnitude : cellSize;
            if (!windVisual.SetResolvedPath(catalog, cells, range, worldCellSize,
                renderer.sortingLayerID, renderer.sortingOrder - 1, out var error))
            { Debug.LogError("V13 resolved wind: " + error + " " + definition.id, root); return false; }
            windSignature = signature;
            return true;
        }

        public void Dispose()
        {
            motion?.CancelMotion();
            windVisual?.ClearOwned();
        }
    }
}
