using System;
using ANIMOL.Art.StarSwellCoastV01;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    // The existing PuzzleDeviceSession owns all cells, turns, costs and movement.
    // This binding only presents its committed state and event stream.
    public sealed class StarSwellCoastV01PuzzleBinding
    {
        public const string ManifestSha256 = "1ed830968ef21165b574f9534903fd2a7ad94957c381aed1879d319f2bffa9d2";
        readonly PrototypeDevice device;
        readonly StarSwellCoastV01AnimatedVisual visual;
        readonly Transform root;
        string pendingIdle;
        bool pendingRemoval;
        bool pendingOpen;
        bool openShown;
        bool gateHeldOpen;
        bool everSettled;
        long lastSequence;

        public static bool Covers(PrototypeDevice d)
        {
            switch (d.role)
            {
                case PrototypeRole.DepartureSpikes:
                case PrototypeRole.Conveyor:
                case PrototypeRole.OneWayGate:
                case PrototypeRole.Boat:
                case PrototypeRole.Barrel:
                case PrototypeRole.Bomb:
                case PrototypeRole.Bounce:
                case PrototypeRole.Brake:
                case PrototypeRole.CrackedWall: return true;
                default: return false;
            }
        }

        public StarSwellCoastV01PuzzleBinding(PrototypeDevice definition, GameObject instance,
            StarSwellCoastV01VisualCatalog catalog)
        {
            device = definition;
            root = instance.transform;
            var renderer = instance.GetComponent<SpriteRenderer>();
            if (!renderer) renderer = instance.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = definition.role == PrototypeRole.Conveyor ||
                definition.role == PrototypeRole.Bounce || definition.role == PrototypeRole.Brake ||
                definition.role == PrototypeRole.DepartureSpikes ? 22 : 30;
            visual = instance.GetComponent<StarSwellCoastV01AnimatedVisual>();
            if (!visual) visual = instance.AddComponent<StarSwellCoastV01AnimatedVisual>();
            if (!visual.Configure(catalog, renderer, ManifestSha256))
                throw new InvalidOperationException("Coast V01 catalog binding failed: " + definition.id);
            visual.ResetActionSequence();
        }

        static string Heading(Vector2Int v)
        {
            if (v == Vector2Int.right) return "east";
            if (v == Vector2Int.up) return "north";
            if (v == Vector2Int.left) return "west";
            if (v == Vector2Int.down) return "south";
            throw new ArgumentException("Coast heading must be cardinal.");
        }

        public static string IdleClip(PrototypeDevice d, PrototypeDeviceState s)
        {
            var heading = Heading(s.direction);
            switch (d.role)
            {
                case PrototypeRole.DepartureSpikes:
                    return "leave_spike.none." + (s.phase == 2 ? "raised" : s.phase == 1 ? "occupied_armed" : "flat");
                case PrototypeRole.Conveyor: return "conveyor." + heading + ".idle";
                case PrototypeRole.OneWayGate: return "oneway_gate." + heading + ".closed";
                case PrototypeRole.Boat: return "steerable_ferry." + heading + ".idle";
                case PrototypeRole.Barrel: return "rolling_barrel." + heading + ".idle";
                case PrototypeRole.Bomb: return "three_turn_bomb.none." + (s.timer > 0 ? "count_" + s.timer : "inert");
                case PrototypeRole.Bounce: return "bounce_corner." + PuzzleDeviceSession.Corner(d.orientation) + ".idle";
                case PrototypeRole.Brake: return "brake_pad." + (d.variant == "vertical" ? "vertical" : "horizontal") + ".idle";
                case PrototypeRole.CrackedWall: return "cracked_wall.none.intact";
                default: return null;
            }
        }

        public static string EventClip(PrototypeDevice d, PrototypeEvent e, PrototypeDeviceState s)
        {
            var heading = Heading(s.direction);
            switch (e.eventName)
            {
                case "existing_player_left_armed_plate": return "leave_spike.none.departure_rise";
                case "conveyor_visual_running": return "conveyor." + heading + ".move";
                case "allowed_passage_begin": return "oneway_gate." + heading + ".opening";
                case "allowed_passage_clear": return "oneway_gate." + heading + ".closing";
                case "existing_rejected_gate_contact": return "oneway_gate." + heading + ".blocked";
                case "existing_boat_boarded": return "steerable_ferry." + heading + ".boarding";
                case "existing_boat_one_cell_committed": return "steerable_ferry." + heading + ".rowing";
                case "existing_boat_disembarked": return "steerable_ferry." + heading + ".disembarking";
                case "existing_boat_or_disembark_rejected": return "steerable_ferry." + heading + ".blocked";
                case "existing_barrel_push_committed": return "rolling_barrel." + heading + ".roll";
                case "existing_barrel_blocker_reached": return "rolling_barrel." + heading + ".settle";
                case "existing_bomb_first_successful_push": return "three_turn_bomb.none.ignite";
                case "existing_third_valid_action_resolved_bomb_explosion": return "three_turn_bomb.none.detonate";
                case "existing_bounce_resolved": return "bounce_corner." + PuzzleDeviceSession.Corner(d.orientation) + ".rebound";
                case "forced_motion_stopped": return "brake_pad." + (d.variant == "vertical" ? "vertical" : "horizontal") + ".press";
                default: return null;
            }
        }

        public bool Notify(PrototypeEvent e, PrototypeDeviceState state, int cost, float duration)
        {
            var key = EventClip(device, e, state);
            if (key == null) return true;
            if (device.role == PrototypeRole.OneWayGate && e.eventName == "allowed_passage_clear")
                return true; // Close only after the host's actor has cleared the gate cell.
            if (e.eventName == "existing_rejected_gate_contact" ||
                e.eventName == "existing_boat_or_disembark_rejected")
                return visual.Play(key, duration);
            long sequence = (long)cost * 100000L + ++lastSequence;
            return visual.PlayForCommittedSequence(key, true, sequence, duration);
        }

        public bool Settle(PrototypeDeviceState state, Vector2Int playerCell, int committedCost, float duration)
        {
            root.localPosition = (Vector2)state.cell;
            if (state.removed && device.role == PrototypeRole.CrackedWall)
            {
                if (pendingOpen) return true;
                if (openShown) return true;
                if (everSettled)
                {
                    long sequence = (long)committedCost * 100000L + ++lastSequence;
                    if (!visual.PlayForCommittedSequence("cracked_wall.none.break", true, sequence, duration)) return false;
                    pendingOpen = true;
                    return true;
                }
                openShown = true;
                return visual.SnapToState("cracked_wall.none.open");
            }
            if (state.removed)
            {
                if (visual.IsPlaying) { pendingRemoval = true; return true; }
                root.gameObject.SetActive(false); return true;
            }
            root.gameObject.SetActive(true);
            openShown = false;
            everSettled = true;
            bool onGate = device.role == PrototypeRole.OneWayGate && state.cell == playerCell;
            var key = onGate ? "oneway_gate." + Heading(state.direction) + ".open" : IdleClip(device, state);
            if (device.role == PrototypeRole.OneWayGate && gateHeldOpen && !onGate)
            {
                gateHeldOpen = false;
                pendingIdle = key;
                long sequence = (long)committedCost * 100000L + ++lastSequence;
                return visual.PlayForCommittedSequence("oneway_gate." + Heading(state.direction) + ".closing",
                    true, sequence, duration);
            }
            if (onGate) gateHeldOpen = true;
            if (key == null) return false;
            if (visual.IsPlaying) { pendingIdle = key; return true; }
            pendingIdle = null;
            return visual.SnapToState(key);
        }

        public void Update()
        {
            if (visual.IsPaused || visual.IsPlaying) return;
            if (pendingOpen) { pendingOpen = false; openShown = true; visual.SnapToState("cracked_wall.none.open"); return; }
            if (pendingRemoval) { root.gameObject.SetActive(false); pendingRemoval = false; return; }
            if (pendingIdle == null) return;
            var key = pendingIdle; pendingIdle = null;
            visual.SnapToState(key);
        }
        public void ApplyProgress(float value) { if (visual.IsPlaying) visual.ApplyNormalizedProgress(value); }
        public void SetPaused(bool paused) { visual.SetPaused(paused); }
        public void Dispose() { visual.Cancel(); visual.ResetActionSequence(); }
    }
}
