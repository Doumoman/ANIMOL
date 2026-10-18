using ANIMOL.Presentation;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    // Replaces only verified visual states after the existing committed event player runs.
    // Unrepresented directions and transitions continue to show their original art.
    [DefaultExecutionOrder(1000)]
    public sealed class StarwayRanchDeviceArt : MonoBehaviour
    {
        public StarwayRanchArt art;
        public DeviceVisualView view;
        public PrototypeRole role;
        public string variant;
        public bool horizontalTwoCellBridge;
        SpriteRenderer visual;

        public void Bind(StarwayRanchArt catalog, DeviceVisualView source, PrototypeDevice device)
        {
            art = catalog; view = source; role = device.role; variant = device.variant;
            horizontalTwoCellBridge = role == PrototypeRole.VineBridge && variant != "plate" &&
                device.direction.x != 0 && device.cells.Count == 2;
            visual = view.visual;
            if (role == PrototypeRole.Chick) visual.transform.localScale *= .62f;
            Apply();
        }

        void LateUpdate() => Apply();
        public void ApplyNow() => Apply();

        void Apply()
        {
            if (!art || !view || !view.visual) return;
            if (!visual) visual = view.visual;
            var state = view.CurrentState;
            var file = FileFor(state);
            if (file == null) { if (role == PrototypeRole.Chick) visual.transform.localPosition = Vector3.zero; return; }
            var sprite = art.Find("devices/" + file + ".png");
            if (role == PrototypeRole.Chick) sprite = art.Find("props/chick_front.png");
            if (!sprite) return;
            visual.sprite = sprite;
            visual.enabled = true;
            if (role == PrototypeRole.Chick)
                visual.transform.localPosition = new Vector3(0, .30515625f, 0);
        }

        string FileFor(string state)
        {
            switch (role)
            {
                case PrototypeRole.Conveyor:
                    return state == "idle_e" ? "conveyor/idle" : state == "run_e" ? "conveyor/active" : null;
                case PrototypeRole.OneWayGate:
                    return state == "closed_e" || state == "close_e" ? "oneway_gate/closed" :
                        state == "open_e" ? "oneway_gate/open" : null;
                case PrototypeRole.Bounce:
                    return state == "idle_ne" ? "bounce_corner/idle" :
                        state == "impact_ne" ? "bounce_corner/pressed" : null;
                case PrototypeRole.Brake:
                    return state == "idle_horizontal" ? "brake_patch/idle" :
                        state == "brake_horizontal" || state == "pressed_horizontal" ? "brake_patch/pressed" : null;
                case PrototypeRole.StarSocket:
                    if (variant == "socket") return state == "socket_empty" ? "star_socket/empty" :
                        state == "socket_filled" || state == "socket_insert" ? "star_socket/filled" : null;
                    return state == "stone" ? "star_stone/idle" :
                        state == "stone_push_e" ? "star_stone/pushed" : null;
                case PrototypeRole.VineBridge:
                    if (variant == "plate") return state == "plate_idle" || state == "plate_release" ? "pressure_plate/released" :
                        state == "plate_pressed" || state == "plate_press" ? "pressure_plate/pressed" : null;
                    if (!horizontalTwoCellBridge) return null;
                    return state == "bridge_idle_horizontal" || state == "bridge_extend_e" ? "vine_bridge/extended" :
                        state == "retracted_e" || state == "bridge_retract_e" ? "vine_bridge/retracted" : null;
                case PrototypeRole.Wind:
                    if (variant == "blower") return null;
                    return state == "wind_flow_e" ? "wind_corridor/idle" :
                        state == "wind_push_e" ? "wind_corridor/active" : null;
                case PrototypeRole.Chick:
                    return state == "idle_s" ? "chick/front" : null;
                default: return null;
            }
        }
    }
}
