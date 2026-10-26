using UnityEngine;

namespace ANIMOL.Art.RanchConnectedDecorV02
{
    [CreateAssetMenu(menuName = "ANIMOL/Ranch Connected Decor V02/Board snapshot")]
    public sealed class RanchConnectedDecorV02BoardAsset : ScriptableObject
    {
        [SerializeField] private RanchConnectedDecorV02BoardSnapshot snapshot;
        public bool TrySetSnapshot(RanchConnectedDecorV02BoardSnapshot value, out string error)
        {
            if (value == null) { error = "Missing board snapshot."; return false; }
            var next = value.DeepCopy();
            if (!next.TryValidate(out error)) return false;
            snapshot = next; return true;
        }
        public bool TryGetSnapshot(out RanchConnectedDecorV02BoardSnapshot value, out string error)
        {
            value = null;
            if (snapshot == null) { error = "Board asset has no approved snapshot."; return false; }
            var copy = snapshot.DeepCopy();
            if (!copy.TryValidate(out error)) return false;
            value = copy; return true;
        }
    }
}
