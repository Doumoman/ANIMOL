using UnityEngine;
using ANIMOL.Gameplay.RanchDevicesR2;

namespace ANIMOL.Art.RanchDevicesV13
{
    // One dedicated pair renderer at the pair midpoint. Ground-aligned, never rotate the vertical art.
    // All authoritative changes happen in the host before playback. Completion emits no gameplay event.
    public sealed class RanchPondSluiceVisualR3 : MonoBehaviour
    {
        [SerializeField] RanchDeviceVisualV13 pairVisual;
        RanchPondPairR2 layout;
        string settledKey;
        public bool IsPlaying { get; private set; }
        bool OwnsVisual()
        {
            return pairVisual != null && pairVisual.transform != transform && pairVisual.transform.IsChildOf(transform);
        }

        bool SameLayout(RanchPondPairR2 pair)
        {
            return layout != null && pair != null && pair.instanceId == layout.instanceId
                && pair.a == layout.a && pair.b == layout.b;
        }
        public bool Configure(RanchDevicesV13Catalog catalog, RanchDeviceVisualV13 child, RanchPondPairR2 pair, out string error)
        {
            Clear(); layout = null;
            RanchPondCueR3 cue;
            if (!RanchPondPresentationR3.TryCue(pair, out cue, out error)) return false;
            if (child == null || child.transform == transform || !child.transform.IsChildOf(transform))
            { error = "Assign one dedicated pair visual child."; return false; }
            pairVisual = child;
            if (!pairVisual.Configure(catalog, out error)) { Clear(); return false; }
            layout = pair.Copy();
            if (!SetResolvedState(pair)) { error = "Could not display the resolved pair."; return false; }
            return true;
        }
        public bool SetResolvedState(RanchPondPairR2 resolved)
        {
            IsPlaying = false; settledKey = null;
            RanchPondCueR3 cue; string error;
            if (!OwnsVisual() || !SameLayout(resolved)
                || !RanchPondPresentationR3.TryCue(resolved, out cue, out error)) { Clear(); return false; }
            return pairVisual.SetState(cue.startKey, true);
        }
        public bool PlayCommittedTransfer(RanchPondPairR2 before, RanchPondPairR2 after, RanchRuleResultR2 committed, out string error)
        {
            error = null; RanchPondCueR3 cue;
            if (!SameLayout(before) || !SameLayout(after) || before.wetA == after.wetA)
            { error = "Use matching before/after snapshots with exactly one water exchange."; Clear(); return false; }
            if (!RanchPondPresentationR3.TryCue(before, out cue, out error)) { Clear(); return false; }
            if (!OwnsVisual() || committed == null || !committed.changed || committed.code != RanchRuleCodeR2.Applied
                || committed.eventKind != RanchRuleEventR2.PondCrossed || committed.instanceId != before.instanceId
                || committed.waterRoute == null || committed.waterRoute.Length != 2
                || committed.waterRoute[0] != cue.waterFrom || committed.waterRoute[1] != cue.waterTo
                || committed.actualRoute == null || committed.actualRoute.Length != 2
                || committed.actualRoute[0] != cue.waterTo || committed.actualRoute[1] != cue.waterFrom)
            { error = "Playback needs the committed wet-to-dry water route and opposite player route."; Clear(); return false; }
            if (!pairVisual.SetState(cue.transferKey, true)) { error = "Transfer clip missing."; Clear(); return false; }
            settledKey = cue.endKey; IsPlaying = true; return true;
        }
        void LateUpdate()
        {
            if (!IsPlaying || !OwnsVisual() || !pairVisual.Finished) return;
            string key = settledKey; IsPlaying = false; settledKey = null;
            if (!pairVisual.SetState(key, true)) Clear();
        }
        public void Clear() { IsPlaying = false; settledKey = null; if (OwnsVisual()) pairVisual.Clear(); }
        void OnDisable() { Clear(); }
    }
}
