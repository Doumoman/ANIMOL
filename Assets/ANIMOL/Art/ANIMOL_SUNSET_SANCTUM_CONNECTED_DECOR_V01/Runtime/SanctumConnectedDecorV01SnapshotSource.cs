using System;
using UnityEngine;

namespace ANIMOL.Art.SanctumConnectedDecorV01
{
    /// <summary>Capture is a pure defensive read; Changed means a committed full snapshot or availability change.</summary>
    [ExecuteAlways]
    public abstract class SanctumConnectedDecorV01SnapshotSource : MonoBehaviour
    {
        public event Action Changed;
        public abstract bool IsExampleSource { get; }
        public abstract bool TryCaptureSnapshot(out SanctumConnectedDecorV01BoardSnapshot snapshot, out string error);
        protected void RaiseChanged()
        {
            Action callbacks = Changed;
            if (callbacks == null) return;
            foreach (Action callback in callbacks.GetInvocationList())
            {
                try { callback(); }
                catch (Exception exception) { Debug.LogException(exception, this); }
            }
        }
        protected virtual void OnEnable() { RaiseChanged(); }
        protected virtual void OnDisable() { RaiseChanged(); }
        protected virtual void OnDestroy() { RaiseChanged(); Changed = null; }
    }
}
