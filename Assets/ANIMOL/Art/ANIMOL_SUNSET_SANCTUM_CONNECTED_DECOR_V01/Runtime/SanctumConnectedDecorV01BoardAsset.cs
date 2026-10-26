using UnityEngine;

namespace ANIMOL.Art.SanctumConnectedDecorV01
{
    [CreateAssetMenu(menuName = "ANIMOL/Sanctum Connected Decor V01/Board snapshot")]
    public sealed class SanctumConnectedDecorV01BoardAsset : ScriptableObject
    {
        [SerializeField] private SanctumConnectedDecorV01BoardSnapshot snapshot;
        public bool TrySetSnapshot(SanctumConnectedDecorV01BoardSnapshot value, out string error)
        {
            if (value == null) { error = "Missing board snapshot."; return false; }
            var next = value.DeepCopy();
            if (!next.TryValidate(out error)) return false;
            snapshot = next; return true;
        }
        public bool TryGetSnapshot(out SanctumConnectedDecorV01BoardSnapshot value, out string error)
        {
            value = null;
            if (snapshot == null) { error = "Board asset has no approved snapshot."; return false; }
            var copy = snapshot.DeepCopy();
            if (!copy.TryValidate(out error)) return false;
            value = copy; return true;
        }
    }
}
