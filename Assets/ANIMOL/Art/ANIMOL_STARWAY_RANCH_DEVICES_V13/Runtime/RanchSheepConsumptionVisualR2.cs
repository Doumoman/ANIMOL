using UnityEngine;

namespace ANIMOL.Art.RanchDevicesV13
{
    // Use on a temporary PRESENTATION effect, after the logical sheep has been removed.
    // Never keep a gameplay sheep/collider alive merely to wait for this animation.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RanchDeviceVisualV13))]
    public sealed class RanchSheepConsumptionVisualR2 : MonoBehaviour
    {
        RanchDeviceVisualV13 visual;
        public bool IsPlaying { get; private set; }
        // Use a separate temporary instance for the grass visual removed by the same transaction.
        public bool PlayGrass(RanchDevicesV13Catalog catalog, out string error)
        {
            error = null; IsPlaying = false;
            if (visual == null) visual = GetComponent<RanchDeviceVisualV13>();
            if (!visual.Configure(catalog, out error)) return false;
            if (!visual.SetState("tall_grass/eaten", true)) { error = "Missing grass-consumed animation."; return false; }
            IsPlaying = true; return true;
        }
        public bool Play(RanchDevicesV13Catalog catalog, string direction, out string error)
        {
            error = null; IsPlaying = false;
            if (visual == null) visual = GetComponent<RanchDeviceVisualV13>();
            if (direction != "south" && direction != "east" && direction != "north" && direction != "west")
            { visual.Clear(); error = "Use a cardinal sheep direction."; return false; }
            if (!visual.Configure(catalog, out error)) return false;
            if (!visual.SetState("sheep/" + direction + "/eat_and_disappear", true)) { error = "Missing consumption animation."; return false; }
            IsPlaying = true; return true;
        }
        void Update()
        {
            if (IsPlaying && visual != null && visual.Finished) { visual.Clear(); IsPlaying = false; }
        }
        public void Cancel()
        {
            if (visual == null) visual = GetComponent<RanchDeviceVisualV13>();
            if (visual != null) visual.Clear(); IsPlaying = false;
        }
        void OnDisable() { Cancel(); }
    }
}
