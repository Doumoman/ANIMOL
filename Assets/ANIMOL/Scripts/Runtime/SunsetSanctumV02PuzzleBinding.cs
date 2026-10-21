using System;
using ANIMOL.Art.SunsetSanctumV02;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    // Adapts already committed puzzle events to the separately imported V02 art.
    // The simulation alone owns positions, occupancy, cost, turns and optical state.
    public sealed class SunsetSanctumV02PuzzleBinding
    {
        public const string ManifestSha256 = "6df7badeac05b62ed58c3f0916f7d9a872c1f6eb8cda11d13e532cbc5b793b56";
        readonly PrototypeDevice device;
        readonly SunsetSanctumV02AnimatedVisual animated;
        readonly SunsetSanctumV02StaticVisual still;
        string pendingIdle;
        long lastSequence;

        public Transform Root { get; private set; }

        public static bool Covers(PrototypeDevice d)
        {
            switch (d.role)
            {
                case PrototypeRole.CrescentLeap:
                case PrototypeRole.Conveyor:
                case PrototypeRole.OneWayGate:
                case PrototypeRole.Optics:
                case PrototypeRole.Magnet:
                case PrototypeRole.Portal:
                case PrototypeRole.Bounce:
                case PrototypeRole.Brake:
                case PrototypeRole.Glass:
                case PrototypeRole.MetalCrate:
                    return true;
                default: return false;
            }
        }

        public SunsetSanctumV02PuzzleBinding(PrototypeDevice definition, GameObject instance,
            SunsetSanctumV02VisualCatalog catalog)
        {
            device = definition;
            Root = instance.transform;
            var renderer = instance.GetComponent<SpriteRenderer>();
            if (!renderer || !catalog) throw new InvalidOperationException("V02 renderer and catalog are required: " + definition.id);
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = definition.role == PrototypeRole.Portal || definition.role == PrototypeRole.Conveyor ||
                definition.role == PrototypeRole.Bounce || definition.role == PrototypeRole.Brake ? 22 : 30;
            if (definition.role == PrototypeRole.MetalCrate)
            {
                still = instance.GetComponent<SunsetSanctumV02StaticVisual>();
                if (!still || !still.Configure(catalog, renderer, ManifestSha256) ||
                    !still.BindExactFile("devices/metal_box/idle.png"))
                    throw new InvalidOperationException("V02 metal box binding failed: " + definition.id);
            }
            else
            {
                animated = instance.GetComponent<SunsetSanctumV02AnimatedVisual>();
                if (!animated || !animated.Configure(catalog, renderer, ManifestSha256))
                    throw new InvalidOperationException("V02 animation binding failed: " + definition.id);
                animated.ResetActionSequence();
            }
        }

        static string Heading(Vector2Int v)
        {
            if (v == Vector2Int.right) return "east";
            if (v == Vector2Int.up) return "north";
            if (v == Vector2Int.left) return "west";
            if (v == Vector2Int.down) return "south";
            throw new ArgumentException("V02 heading must be cardinal.");
        }

        // The host's a/b variants distinguish the two ends of one portal pair.
        // Both endpoints use the same V02 motif; pair_02 is reserved for another pair.
        static string Pair(PrototypeDevice d) => "pair_01";

        public static string IdleClip(PrototypeDevice device, PrototypeDeviceState state)
        {
            string heading = Heading(state.direction);
            switch (device.role)
            {
                case PrototypeRole.CrescentLeap: return "moon_jump." + heading + ".idle";
                case PrototypeRole.Conveyor: return "conveyor." + heading + ".idle";
                case PrototypeRole.OneWayGate: return "oneway_gate." + heading + ".closed";
                case PrototypeRole.Optics:
                    if (device.variant == "mirror") return "mirror.none." + (state.phase == 0 ? "slash" : "backslash");
                    if (device.variant == "sensor") return "light_receiver.none." + (state.active ? "lit" : "unlit");
                    if (device.variant == "gate") return "light_gate.none." + (state.active ? "open" : "closed");
                    return "light_emitter." + heading + ".active";
                case PrototypeRole.Magnet:
                    return device.variant == "switch" ? "magnet_switch.none." + (state.active ? "press" : "release")
                        : "magnet." + heading + "." + (state.active ? "active" : "idle");
                case PrototypeRole.Portal: return "paired_portal.none." + Pair(device) + "_idle";
                case PrototypeRole.Bounce: return "bounce_corner." + PuzzleDeviceSession.Corner(device.orientation) + ".idle";
                case PrototypeRole.Brake: return "brake_pad." + (device.variant == "vertical" ? "vertical" : "horizontal") + ".idle";
                case PrototypeRole.Glass: return "glass_wall.none." + (state.removed ? "open" : "intact");
                default: return null;
            }
        }

        public static string TransitionClip(PrototypeDevice device, PrototypeEvent e, PrototypeDeviceState state)
        {
            string heading = Heading(state.direction);
            switch (e.eventName)
            {
                case "existing_leap_committed_prepare":
                case "existing_leap_launch": return "moon_jump." + heading + ".jump";
                case "conveyor_visual_running": return "conveyor." + heading + ".move";
                case "allowed_passage_begin": return "oneway_gate." + heading + ".opening";
                case "allowed_passage_clear": return "oneway_gate." + heading + ".closing";
                case "existing_rejected_gate_contact": return "oneway_gate." + heading + ".blocked";
                case "existing_mirror_rotation_committed": return "mirror.none." + (state.phase == 0 ? "backslash_to_slash" : "slash_to_backslash");
                case "existing_sensor_light_received": return "light_receiver.none.activate";
                case "existing_sensor_light_lost": return "light_receiver.none.deactivate";
                case "existing_linked_light_gate_opened": return "light_gate.none.opening";
                case "existing_linked_light_gate_closed": return "light_gate.none.closing";
                case "existing_magnet_switch_on": return device.variant == "switch" ? "magnet_switch.none.press" : "magnet." + heading + ".active";
                case "existing_magnet_switch_off": return device.variant == "switch" ? "magnet_switch.none.release" : "magnet." + heading + ".deactivate";
                case "existing_portal_entry_committed": return "paired_portal.none." + Pair(device) + "_enter";
                case "existing_portal_exit_committed": return "paired_portal.none." + Pair(device) + "_exit";
                case "existing_portal_destination_next_cell_blocked": return "paired_portal.none." + Pair(device) + "_blocked";
                case "existing_bounce_resolved": return "bounce_corner." + PuzzleDeviceSession.Corner(device.orientation) + ".rebound";
                case "forced_motion_stopped": return "brake_pad." + (device.variant == "vertical" ? "vertical" : "horizontal") + ".press";
                case "existing_glass_destroyed_by_bomb_or_statue": return "glass_wall.none.break";
                default: return null;
            }
        }

        public bool Notify(PrototypeEvent e, PrototypeDeviceState state, int committedCost, float duration)
        {
            if (still) return true;
            string key = TransitionClip(device, e, state);
            if (key == null) return true; // This native package has no clip for that host cue.
            if (e.eventName == "existing_rejected_gate_contact" ||
                e.eventName == "existing_portal_destination_next_cell_blocked")
                return animated.Play(key, duration);
            long sequence = (long)committedCost * 100000L + e.group;
            if (sequence <= lastSequence) return true;
            if (!animated.PlayForCommittedSequence(key, true, sequence, duration)) return false;
            lastSequence = sequence;
            return true;
        }

        public bool Settle(PrototypeDeviceState state)
        {
            Root.localPosition = (Vector2)state.cell;
            if (still) { Root.gameObject.SetActive(!state.removed); return true; }
            string key = IdleClip(device, state);
            if (key == null) return false;
            if (animated.IsPlaying) { pendingIdle = key; return true; }
            pendingIdle = null;
            return animated.SnapToState(key, device.variant == "switch" ? 1f : 0f);
        }

        public void Update()
        {
            if (pendingIdle == null || animated.IsPlaying) return;
            var key = pendingIdle; pendingIdle = null;
            animated.SnapToState(key, key == "magnet_switch.none.press" ? 1f : 0f);
        }

        public void Dispose()
        {
            if (!animated) return;
            animated.Cancel();
            animated.ResetActionSequence();
        }
    }
}
