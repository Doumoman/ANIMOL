using UnityEngine;

namespace ANIMOL.Art.RanchDevicesV13
{
    // Assign two one-cell visual children at the actual pair cells. Root/occupancy never move here.
    // Reapply an authoritative resolved state after enabling, loading, undo or pooling.
    public sealed class RanchPondPairVisualR2 : MonoBehaviour
    {
        [SerializeField] RanchDeviceVisualV13 cellA;
        [SerializeField] RanchDeviceVisualV13 cellB;
        [SerializeField] RanchDevicesV13Catalog catalog;
        public bool Configure(RanchDevicesV13Catalog value, RanchDeviceVisualV13 a, RanchDeviceVisualV13 b, out string error)
        {
            error = null; Clear();
            if (a == null || b == null || a == b || a.transform == transform || b.transform == transform
                || !a.transform.IsChildOf(transform) || !b.transform.IsChildOf(transform))
            { catalog = null; cellA = null; cellB = null; error = "Assign two distinct dedicated cell children."; return false; }
            catalog = value; cellA = a; cellB = b;
            if (!a.Configure(value, out error) || !b.Configure(value, out error)) { Clear(); return false; }
            return true;
        }
        public bool SetResolvedState(bool wetA)
        {
            if (cellA == null || cellB == null || catalog == null) { Clear(); return false; }
            if (!cellA.SetState(wetA ? "pond_cell/wet" : "pond_cell/dry", true)
                || !cellB.SetState(wetA ? "pond_cell/dry" : "pond_cell/wet", true)) { Clear(); return false; }
            return true;
        }
        public bool PlayCommittedCrossing(bool previouslyWetA)
        {
            if (cellA == null || cellB == null || catalog == null) { Clear(); return false; }
            if (!cellA.SetState(previouslyWetA ? "pond_cell/drain" : "pond_cell/fill", true)
                || !cellB.SetState(previouslyWetA ? "pond_cell/fill" : "pond_cell/drain", true)) { Clear(); return false; }
            return true;
        }
        public void Clear() { if (cellA != null) cellA.Clear(); if (cellB != null) cellB.Clear(); }
        void OnDisable() { Clear(); }
    }
}
