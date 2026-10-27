using UnityEngine;

namespace ANIMOL.Art.RanchDevicesV13
{
    // Attach to the logical owner, assign a dedicated child. Never animate the gameplay root.
    public sealed class RanchCommittedMotionV13 : MonoBehaviour
    {
        [SerializeField] Transform visualChild;
        [SerializeField] RanchDeviceVisualV13 visual;
        Vector3[] path;
        Vector3 restLocal;
        float elapsed, secondsPerCell, cellSize;
        string endKey;
        string[] segmentKeys;
        int activeSegment = -1;
        bool captured;
        public bool BeginCommittedPath(Vector3[] actualTraversedCenters, float worldCellSize, float seconds, string moveClip, string settledClip, out string error)
        {
            return BeginCommittedPath(actualTraversedCenters, worldCellSize, seconds, moveClip, settledClip, null, out error);
        }
        // Optional per-segment keys preserve animal facing when a committed route turns.
        // Root occupancy has already been committed by the host before this method is called.
        public bool BeginCommittedPath(Vector3[] actualTraversedCenters, float worldCellSize, float seconds, string moveClip, string settledClip, string[] perSegmentMoveClips, out string error)
        {
            error = null;
            if (visualChild == null || visualChild == transform || !visualChild.IsChildOf(transform) || visual == null || visual.transform != visualChild)
            { error = "Assign a dedicated visual child; gameplay root may not be tweened."; return false; }
            if (actualTraversedCenters == null || actualTraversedCenters.Length < 2 || actualTraversedCenters.Length > 4096 || !RanchDevicesV13Catalog.Finite(worldCellSize) || worldCellSize <= 0 || !RanchDevicesV13Catalog.Finite(seconds) || seconds <= 0)
            { error = "Invalid committed route/timing."; return false; }
            foreach (var p in actualTraversedCenters) if (!RanchDevicesV13Catalog.Finite(p.x) || !RanchDevicesV13Catalog.Finite(p.y) || !RanchDevicesV13Catalog.Finite(p.z))
            { error = "Nonfinite committed route."; return false; }
            if (perSegmentMoveClips != null && (perSegmentMoveClips.Length != actualTraversedCenters.Length - 1 || System.Array.Exists(perSegmentMoveClips, string.IsNullOrEmpty)))
            { error = "Supply exactly one nonempty clip key per route segment."; return false; }
            if (!captured) { restLocal = visualChild.localPosition; captured = true; }
            if (Vector3.Distance(actualTraversedCenters[actualTraversedCenters.Length - 1], transform.TransformPoint(restLocal)) > worldCellSize * .001f)
            { error = "Commit the gameplay root to the final visual center before playback."; return false; }
            if (!visual.SetState(moveClip, true)) { error = "Unbound moving clip."; return false; }
            path = (Vector3[])actualTraversedCenters.Clone(); cellSize = worldCellSize; secondsPerCell = seconds;
            segmentKeys = perSegmentMoveClips == null ? null : (string[])perSegmentMoveClips.Clone(); activeSegment = -1;
            endKey = settledClip; elapsed = 0; visualChild.position = path[0]; return true;
        }
        void Update()
        {
            if (path == null) return;
            elapsed += Time.deltaTime; float remaining = elapsed;
            for (int i = 1; i < path.Length; i++)
            {
                float duration = Mathf.Max(.001f, Vector3.Distance(path[i - 1], path[i]) / cellSize * secondsPerCell);
                if (remaining < duration)
                {
                    if (segmentKeys != null && activeSegment != i - 1)
                    {
                        if (!visual.SetState(segmentKeys[i - 1], true)) { CancelMotion(); return; }
                        activeSegment = i - 1;
                    }
                    float t = remaining / duration; t = t * t * (3f - 2f * t);
                    visualChild.position = Vector3.LerpUnclamped(path[i - 1], path[i], t); return;
                }
                remaining -= duration;
            }
            path = null; segmentKeys = null; visualChild.localPosition = restLocal;
            if (!string.IsNullOrEmpty(endKey)) visual.SetState(endKey, true);
        }
        public void CancelMotion() { path = null; segmentKeys = null; activeSegment = -1; elapsed = 0; if (captured && visualChild != null) visualChild.localPosition = restLocal; }
        void OnDisable() { CancelMotion(); }
    }
}
