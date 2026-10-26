using System;
using UnityEngine;

namespace ANIMOL.Art.SanctumConnectedDecorV01
{
    /// <summary>Concrete event-driven caller of the existing Controller. No polling or gameplay authority.</summary>
    [DefaultExecutionOrder(100)]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SanctumConnectedDecorV01Controller))]
    public sealed class SanctumConnectedDecorV01Binding : MonoBehaviour
    {
        [Serializable] public sealed class RefreshReport
        {
            public bool success, isExampleOnly;
            public string boardId, error;
            public long revision;
            public int networkCount, groupCount, claimCount, visualCount;
            public RefreshReport Copy() { return (RefreshReport)MemberwiseClone(); }
        }
        [SerializeField] private SanctumConnectedDecorV01Controller controller;
        [SerializeField] private SanctumConnectedDecorV01Catalog catalog;
        [SerializeField] private string expectedManifestSha256, sortingLayerName;
        [SerializeField] private SanctumConnectedDecorV01SnapshotSource source;
        [SerializeField] private bool allowExampleSource;
        private SanctumConnectedDecorV01SnapshotSource subscribedSource;
        private SanctumDecorPlan lastAppliedPlan;
        private string lastAppliedHash;
        private SanctumConnectedDecorV01Catalog lastAppliedCatalog;
        private SanctumConnectedDecorV01SnapshotSource lastAppliedSource;
        private string lastAppliedPin, lastAppliedSortingLayer;
        private Matrix4x4 lastAppliedWorldMatrix;
        private bool hasAppliedTransform, lastAppliedControllerActive;
        private bool refreshing, refreshRequested;
        private RefreshReport lastReport = new RefreshReport();
        public SanctumConnectedDecorV01SnapshotSource Source { get { return source; } }
        public SanctumConnectedDecorV01Controller Controller { get { return controller; } }
        public SanctumConnectedDecorV01Catalog Catalog { get { return catalog; } }
        public string ExpectedManifestSha256 { get { return expectedManifestSha256; } }
        public string SortingLayerName { get { return sortingLayerName; } }
        public bool AllowExampleSource { get { return allowExampleSource; } }
        public string LastDiagnostic { get { return lastReport.error; } }
        public RefreshReport LastReport { get { return lastReport.Copy(); } }

        public bool Bind(SanctumConnectedDecorV01Controller target, SanctumConnectedDecorV01Catalog latest,
            string pinnedManifestSha256, string actualSortingLayerName, SanctumConnectedDecorV01SnapshotSource snapshotSource,
            out string error, bool allowExampleSource = false)
        {
            bool same = controller == target && catalog == latest && expectedManifestSha256 == pinnedManifestSha256
                && sortingLayerName == actualSortingLayerName && source == snapshotSource && this.allowExampleSource == allowExampleSource;
            if (!same)
            {
                Unsubscribe(); ClearVisuals(); controller = target; catalog = latest;
                expectedManifestSha256 = pinnedManifestSha256; sortingLayerName = actualSortingLayerName;
                source = snapshotSource; this.allowExampleSource = allowExampleSource;
            }
            ReconcileSubscription(); return RefreshNow(out error);
        }
        private void OnEnable() { ReconcileSubscription(); string ignored; RefreshNow(out ignored); }
        private void OnDisable() { Unsubscribe(); ClearVisuals(); }
        private void OnDestroy() { Unsubscribe(); ClearVisuals(); }
        private void ReconcileSubscription()
        {
            if (subscribedSource == source && isActiveAndEnabled) return;
            Unsubscribe();
            if (!isActiveAndEnabled || source == null) return;
            subscribedSource = source; subscribedSource.Changed += OnSourceChanged;
        }
        private void Unsubscribe()
        { if (subscribedSource != null) subscribedSource.Changed -= OnSourceChanged; subscribedSource = null; }
        private void OnSourceChanged()
        {
            if (!isActiveAndEnabled) return;
            if (refreshing) { refreshRequested = true; return; }
            string ignored; RefreshNow(out ignored);
        }
        public bool RefreshNow(out string error)
        {
            error = null; ReconcileSubscription();
            if (refreshing) { refreshRequested = true; error = "A decoration refresh is already in progress."; return false; }
            refreshing = true;
            try
            {
                // A pure source will need one pass. Bound reentrant sources cannot loop indefinitely.
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    refreshRequested = false;
                    bool success = RefreshOnce(out error);
                    if (!refreshRequested) return success;
                }
                return Fail("Snapshot source changed repeatedly during capture; no stale layout committed.", null, out error);
            }
            catch (Exception exception) { return Fail("Decoration binding failed closed: " + exception.Message, null, out error); }
            finally { refreshing = false; refreshRequested = false; }
        }
        private bool RefreshOnce(out string error)
        {
            error = null;
            if (!enabled || controller == null || controller.gameObject != gameObject || !controller.enabled)
                return Fail("Binding requires its own enabled same-GameObject Controller.", null, out error);
            if (source == null || !source.enabled || (isActiveAndEnabled && !source.isActiveAndEnabled))
                return Fail("Snapshot source is missing or disabled.", null, out error);
            if (catalog == null || !string.Equals(expectedManifestSha256, SanctumConnectedDecorV01BoardSnapshot.ArtManifestSha256, StringComparison.OrdinalIgnoreCase))
                return Fail("Binding requires the current catalog and exact art manifest pin.", null, out error);
            bool layerExists = false;
            foreach (SortingLayer layer in SortingLayer.layers) if (layer.name == sortingLayerName) layerExists = true;
            if (!layerExists) return Fail("Select an existing actual decoration Sorting Layer explicitly.", null, out error);
            string reason;
            if (!catalog.ValidateIntegrity(expectedManifestSha256, out reason)) return Fail(reason, null, out error);
            SanctumConnectedDecorV01BoardSnapshot captured;
            if (!source.TryCaptureSnapshot(out captured, out reason) || captured == null)
                return Fail(reason ?? "Snapshot source returned no data.", null, out error);
            var snapshot = captured.DeepCopy();
            if (!snapshot.TryValidate(out reason)) return Fail(reason, snapshot, out error);
            if (snapshot.isExampleOnly != source.IsExampleSource || (snapshot.isExampleOnly && !allowExampleSource))
                return Fail("Example source/data mismatch or example board was not explicitly permitted.", snapshot, out error);
            Matrix4x4 worldMatrix;
            if (!ValidateHierarchy(snapshot.cellSize, out worldMatrix, out reason)) return Fail(reason, snapshot, out error);
            string content = snapshot.ComputeContentHash();
            if (lastAppliedHash == content && lastAppliedCatalog == catalog && lastAppliedSource == source
                && lastAppliedPin == expectedManifestSha256 && lastAppliedSortingLayer == sortingLayerName
                && hasAppliedTransform && lastAppliedWorldMatrix.Equals(worldMatrix)
                && lastAppliedControllerActive == controller.isActiveAndEnabled
                && lastAppliedPlan != null && ReferenceEquals(controller.CurrentPlan, lastAppliedPlan))
            { lastReport.error = null; lastReport.success = true; return true; }
            // These are the previously missing concrete calls. Both are required; no legacy image fallback.
            if (!controller.Configure(catalog, expectedManifestSha256, sortingLayerName))
                return Fail(controller.LastDiagnostic, snapshot, out error);
            if (!controller.SetLayout(snapshot.networkCells, snapshot.groupPlacements,
                new Vector3(snapshot.originX, snapshot.originY, snapshot.originZ), snapshot.cellSize,
                snapshot.CreateApprovedCellQuery(), snapshot.seed, snapshot.postSpacing))
                return Fail(controller.LastDiagnostic, snapshot, out error);
            lastAppliedHash = content; lastAppliedPlan = controller.CurrentPlan;
            lastAppliedCatalog = catalog; lastAppliedSource = source;
            lastAppliedPin = expectedManifestSha256; lastAppliedSortingLayer = sortingLayerName;
            lastAppliedWorldMatrix = transform.localToWorldMatrix; hasAppliedTransform = true;
            lastAppliedControllerActive = controller.isActiveAndEnabled;
            lastReport = new RefreshReport { success = true, isExampleOnly = snapshot.isExampleOnly,
                boardId = snapshot.boardId, revision = snapshot.revision, networkCount = lastAppliedPlan.NetworkCount,
                groupCount = lastAppliedPlan.GroupCount, claimCount = lastAppliedPlan.ClaimCount, visualCount = controller.OwnedVisualCount };
            return true;
        }
        public void ClearVisuals()
        {
            // Serialized Inspector edits can replace/clear the field before Refresh.
            // Resolve the required OWN component so that such edits cannot strand old visuals.
            var ownedController = GetComponent<SanctumConnectedDecorV01Controller>();
            if (ownedController != null) ownedController.ClearLayout();
            lastAppliedHash = null; lastAppliedPlan = null; lastReport = new RefreshReport();
            lastAppliedCatalog = null; lastAppliedSource = null; lastAppliedPin = null; lastAppliedSortingLayer = null;
            hasAppliedTransform = false; lastAppliedControllerActive = false;
        }
        private bool ValidateHierarchy(float cellSize, out Matrix4x4 worldMatrix, out string error)
        {
            worldMatrix = transform.localToWorldMatrix; error = null;
            for (Transform node = transform; node != null; node = node.parent)
            {
                Vector3 scale = node.localScale, position = node.localPosition; Quaternion rotation = node.localRotation;
                if (!Finite(scale.x) || !Finite(scale.y) || !Finite(scale.z) || scale.x <= 0 || scale.y <= 0
                    || !Finite(position.x) || !Finite(position.y) || !Finite(position.z)
                    || !Finite(rotation.x) || !Finite(rotation.y) || !Finite(rotation.z) || !Finite(rotation.w)
                    || Quaternion.Angle(rotation, Quaternion.identity) > .01f)
                { error = "Decoration hierarchy requires finite transforms, identity ancestor rotations and positive XY scales."; return false; }
            }
            Vector3 parentScale = transform.lossyScale;
            float localX = cellSize / parentScale.x, localY = cellSize / parentScale.y;
            if (!Finite(parentScale.x) || !Finite(parentScale.y) || parentScale.x <= 0 || parentScale.y <= 0
                || !Finite(localX) || !Finite(localY) || localX <= 0 || localY <= 0)
            { error = "Decoration cell scale cannot be represented in the current hierarchy."; return false; }
            for (int row = 0; row < 4; row++) for (int column = 0; column < 4; column++)
                if (!Finite(worldMatrix[row, column])) { error = "Decoration world matrix is not finite."; return false; }
            return true;
        }
        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private bool Fail(string reason, SanctumConnectedDecorV01BoardSnapshot snapshot, out string error)
        {
            ClearVisuals(); error = string.IsNullOrWhiteSpace(reason) ? "Decoration binding rejected current state." : reason;
            lastReport = new RefreshReport { error = error, boardId = snapshot == null ? null : snapshot.boardId,
                revision = snapshot == null ? 0 : snapshot.revision, isExampleOnly = snapshot != null && snapshot.isExampleOnly };
            return false;
        }
    }
}
