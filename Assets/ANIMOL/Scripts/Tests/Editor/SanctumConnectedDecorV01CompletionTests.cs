using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ANIMOL.Art.SanctumConnectedDecorV01;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Copied from the add-on's optional Tests~ into the host TestRunner assembly.
// The asset-backed tests require the generated current catalog.
public sealed class SanctumConnectedDecorV01CompletionTests
{
    private static SanctumConnectedDecorV01BoardSnapshot Altar(bool example = false)
    {
        var cells = new List<SanctumDecorAllowedCell>();
        for (int y = 0; y < 2; y++) for (int x = 0; x < 3; x++) cells.Add(new SanctumDecorAllowedCell(x, y));
        return new SanctumConnectedDecorV01BoardSnapshot {
            schemaVersion = 1, packageId = SanctumConnectedDecorV01Catalog.PackageId,
            sourceManifestSha256 = SanctumConnectedDecorV01BoardSnapshot.ArtManifestSha256,
            boardId = "test_altar", revision = 1, isWholeBoard = true, isExampleOnly = example,
            cellSize = 1, postSpacing = 4, networkCells = new SanctumDecorNetworkCell[0],
            groupPlacements = new[] { new SanctumDecorGroupPlacement("one_altar", "sanctum_altar_3x2", 0, 0) },
            allowedDecorCells = cells.ToArray()
        };
    }
    [Test] public void FlatSnapshotJsonRoundTripKeepsRowsAndLargeRevision()
    {
        var original = Altar(); original.revision = 2147483648L;
        var restored = JsonUtility.FromJson<SanctumConnectedDecorV01BoardSnapshot>(JsonUtility.ToJson(original));
        string error; Assert.IsTrue(restored.TryValidate(out error), error);
        Assert.AreEqual(original.revision, restored.revision); Assert.AreEqual(6, restored.allowedDecorCells.Length);
        Assert.AreEqual("sanctum_altar_3x2", restored.groupPlacements[0].groupKey);
    }
    [Test] public void DeepCopyClonesOuterArraysAndEveryMutableRow()
    {
        var original = Altar(); var copy = original.DeepCopy();
        copy.groupPlacements[0].anchorX = 90; copy.allowedDecorCells[0].x = 90;
        Assert.AreEqual(0, original.groupPlacements[0].anchorX); Assert.AreEqual(0, original.allowedDecorCells[0].x);
        Assert.AreNotSame(original.groupPlacements, copy.groupPlacements);
        Assert.AreNotSame(original.groupPlacements[0], copy.groupPlacements[0]);
    }
    [Test] public void ApprovedCellQueryCannotBeChangedByLaterDtoMutation()
    {
        var data = Altar(); var query = data.CreateApprovedCellQuery();
        data.allowedDecorCells[0].x = 700;
        Assert.IsTrue(query(new SanctumDecorClaim(new SanctumDecorCell(0, 0), 0, 0, "a", "sanctum_altar_3x2", true)));
        Assert.IsFalse(query(new SanctumDecorClaim(new SanctumDecorCell(700, 0), 0, 0, "a", "sanctum_altar_3x2", true)));
    }
    [Test] public void MissingArraysFailWhileExplicitEmptyBoardCanClear()
    {
        string error; var data = Altar(); data.networkCells = null; Assert.IsFalse(data.TryValidate(out error));
        data.networkCells = new SanctumDecorNetworkCell[0]; data.groupPlacements = new SanctumDecorGroupPlacement[0];
        data.allowedDecorCells = new SanctumDecorAllowedCell[0]; Assert.IsTrue(data.TryValidate(out error), error);
    }
    [Test] public void OneDeniedCellRejectsTheWholeThreeByTwoProp()
    {
        var data = Altar(); Array.Resize(ref data.allowedDecorCells, 5); string error;
        Assert.IsFalse(data.TryValidate(out error)); StringAssert.Contains("Host rejected", error);
    }
    [Test] public void DuplicateApprovedCellsOrPlacementIdsFail()
    {
        var data = Altar(); data.allowedDecorCells[5] = new SanctumDecorAllowedCell(0, 0); string error;
        Assert.IsFalse(data.TryValidate(out error));
        data = Altar(); data.groupPlacements = new[] { data.groupPlacements[0], new SanctumDecorGroupPlacement("one_altar", "sanctum_altar_3x2", 5, 5) };
        Assert.IsFalse(data.TryValidate(out error));
    }
    [Test] public void ExactHeightAndLayerApprovalCannotBeBorrowedFromAnotherPlane()
    {
        var data = Altar(); data.groupPlacements[0].heightKey = 1; string error;
        Assert.IsFalse(data.TryValidate(out error));
    }
    [Test] public void WrongPinPartialBoardAndWorldOverflowFail()
    {
        string error; var data = Altar(); data.sourceManifestSha256 = new string('0', 64); Assert.IsFalse(data.TryValidate(out error));
        data = Altar(); data.isWholeBoard = false; Assert.IsFalse(data.TryValidate(out error));
        data = Altar(); data.cellSize = float.MaxValue; Assert.IsFalse(data.TryValidate(out error));
    }
    [Test] public void BoardAssetPublishesAndCapturesDefensiveCopies()
    {
        var asset = ScriptableObject.CreateInstance<SanctumConnectedDecorV01BoardAsset>();
        try
        {
            var data = Altar(); string error; Assert.IsTrue(asset.TrySetSnapshot(data, out error), error);
            data.groupPlacements[0].anchorX = 99;
            SanctumConnectedDecorV01BoardSnapshot captured; Assert.IsTrue(asset.TryGetSnapshot(out captured, out error), error);
            Assert.AreEqual(0, captured.groupPlacements[0].anchorX); captured.allowedDecorCells[0].x = 99;
            Assert.IsTrue(asset.TryGetSnapshot(out captured, out error), error); Assert.AreEqual(0, captured.allowedDecorCells[0].x);
        }
        finally { UnityEngine.Object.DestroyImmediate(asset); }
    }
    [Test] public void PublishedSourceRejectsExampleAndInvalidatesThePreviousState()
    {
        var node = new GameObject("source_test");
        try
        {
            var source = node.AddComponent<SanctumConnectedDecorV01PublishedSnapshotSource>(); int events = 0;
            source.Changed += () => events++; string error; Assert.IsTrue(source.PublishSnapshot(Altar(), out error), error);
            Assert.IsFalse(source.PublishSnapshot(Altar(true), out error));
            SanctumConnectedDecorV01BoardSnapshot captured; Assert.IsFalse(source.TryCaptureSnapshot(out captured, out error));
            Assert.IsNull(captured); Assert.AreEqual(2, events);
        }
        finally { UnityEngine.Object.DestroyImmediate(node); }
    }
    [Test] public void PublishedCaptureIsDefensiveAndDisableRaisesAvailabilityChange()
    {
        var node = new GameObject("source_test");
        try
        {
            var source = node.AddComponent<SanctumConnectedDecorV01PublishedSnapshotSource>(); string error;
            Assert.IsTrue(source.PublishSnapshot(Altar(), out error), error);
            SanctumConnectedDecorV01BoardSnapshot captured; Assert.IsTrue(source.TryCaptureSnapshot(out captured, out error), error);
            captured.groupPlacements[0].anchorX = 99;
            Assert.IsTrue(source.TryCaptureSnapshot(out captured, out error), error); Assert.AreEqual(0, captured.groupPlacements[0].anchorX);
            int events = 0; source.Changed += () => events++; source.enabled = false; Assert.AreEqual(1, events);
        }
        finally { UnityEngine.Object.DestroyImmediate(node); }
    }
    [Test] public void AuthoredSourceRefreshesOnlyAfterExplicitBoardNotification()
    {
        var node = new GameObject("authored_test"); var asset = ScriptableObject.CreateInstance<SanctumConnectedDecorV01BoardAsset>();
        try
        {
            var data = Altar(true); string error; Assert.IsTrue(asset.TrySetSnapshot(data, out error), error);
            var source = node.AddComponent<SanctumConnectedDecorV01AuthoredSnapshotSource>(); Assert.IsTrue(source.AssignBoard(asset, out error), error);
            int events = 0; source.Changed += () => events++; data.revision = 2; Assert.IsTrue(asset.TrySetSnapshot(data, out error), error);
            Assert.AreEqual(0, events); Assert.IsTrue(source.NotifyBoardChanged(out error), error); Assert.AreEqual(1, events);
            Assert.IsTrue(source.IsExampleSource);
        }
        finally { UnityEngine.Object.DestroyImmediate(node); UnityEngine.Object.DestroyImmediate(asset); }
    }
    [Test] public void PublishedSourceRequiresFreshPublicationAfterPoolDisable()
    {
        var node = new GameObject("published_pool_test");
        try
        {
            var source = node.AddComponent<SanctumConnectedDecorV01PublishedSnapshotSource>(); string error;
            Assert.IsTrue(source.PublishSnapshot(Altar(), out error), error); node.SetActive(false); node.SetActive(true);
            SanctumConnectedDecorV01BoardSnapshot snapshot; Assert.IsFalse(source.TryCaptureSnapshot(out snapshot, out error));
            node.SetActive(false); Assert.IsTrue(source.PublishSnapshot(Altar(), out error), error); node.SetActive(true);
            Assert.IsTrue(source.TryCaptureSnapshot(out snapshot, out error), error);
        }
        finally { UnityEngine.Object.DestroyImmediate(node); }
    }
    [Test] public void BindingRejectsAControllerOwnedByAnotherGameObject()
    {
        var own = new GameObject("binding_test"); var other = new GameObject("foreign_controller");
        try
        {
            var binding = own.AddComponent<SanctumConnectedDecorV01Binding>();
            var controller = other.AddComponent<SanctumConnectedDecorV01Controller>(); string error;
            Assert.IsFalse(binding.Bind(controller, null, SanctumConnectedDecorV01BoardSnapshot.ArtManifestSha256, "Default", null, out error));
            StringAssert.Contains("same-GameObject", error);
        }
        finally { UnityEngine.Object.DestroyImmediate(own); UnityEngine.Object.DestroyImmediate(other); }
    }
#if UNITY_EDITOR
    private static SanctumConnectedDecorV01Catalog ActualCatalog()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:SanctumConnectedDecorV01Catalog"))
        {
            var value = AssetDatabase.LoadAssetAtPath<SanctumConnectedDecorV01Catalog>(AssetDatabase.GUIDToAssetPath(guid)); string error;
            if (value != null && value.ValidateIntegrity(SanctumConnectedDecorV01BoardSnapshot.ArtManifestSha256, out error)) return value;
        }
        Assert.Ignore("Build the real current add-on catalog in Unity before running asset-backed binding tests."); return null;
    }
    [Test] public void InactiveBindingCallsRealControllerAndIdenticalRefreshKeepsOnePlan()
    {
        var catalog = ActualCatalog(); var node = new GameObject("inactive_binding_test"); node.SetActive(false);
        var asset = ScriptableObject.CreateInstance<SanctumConnectedDecorV01BoardAsset>();
        try
        {
            string error; Assert.IsTrue(asset.TrySetSnapshot(Altar(true), out error), error);
            var source = node.AddComponent<SanctumConnectedDecorV01AuthoredSnapshotSource>(); Assert.IsTrue(source.AssignBoard(asset, out error), error);
            var binding = node.AddComponent<SanctumConnectedDecorV01Binding>(); var controller = node.GetComponent<SanctumConnectedDecorV01Controller>();
            Assert.IsTrue(binding.Bind(controller, catalog, SanctumConnectedDecorV01BoardSnapshot.ArtManifestSha256, "Default", source, out error, true), error);
            Assert.AreEqual(1, controller.OwnedVisualCount); node.SetActive(true);
            // Explicit Editor refresh must also work without assuming edit-mode lifecycle callbacks ran.
            Assert.IsTrue(binding.RefreshNow(out error), error);
            Assert.IsTrue(node.GetComponentInChildren<SpriteRenderer>().enabled);
            var plan = controller.CurrentPlan; Assert.IsTrue(binding.RefreshNow(out error), error);
            Assert.AreSame(plan, controller.CurrentPlan); Assert.AreEqual(1, controller.OwnedVisualCount);
        }
        finally { UnityEngine.Object.DestroyImmediate(node); UnityEngine.Object.DestroyImmediate(asset); }
    }
    [Test] public void SourceDisableAndBindingPoolReuseClearThenCaptureFreshState()
    {
        var catalog = ActualCatalog(); var node = new GameObject("lifecycle_binding_test"); var asset = ScriptableObject.CreateInstance<SanctumConnectedDecorV01BoardAsset>();
        try
        {
            string error; Assert.IsTrue(asset.TrySetSnapshot(Altar(true), out error), error);
            var source = node.AddComponent<SanctumConnectedDecorV01AuthoredSnapshotSource>(); Assert.IsTrue(source.AssignBoard(asset, out error), error);
            var binding = node.AddComponent<SanctumConnectedDecorV01Binding>(); var controller = node.GetComponent<SanctumConnectedDecorV01Controller>();
            Assert.IsTrue(binding.Bind(controller, catalog, SanctumConnectedDecorV01BoardSnapshot.ArtManifestSha256, "Default", source, out error, true), error);
            source.enabled = false; Assert.AreEqual(0, controller.OwnedVisualCount);
            source.enabled = true; Assert.AreEqual(1, controller.OwnedVisualCount);
            node.SetActive(false); Assert.AreEqual(0, controller.OwnedVisualCount);
            node.SetActive(true); Assert.AreEqual(1, controller.OwnedVisualCount);
        }
        finally { UnityEngine.Object.DestroyImmediate(node); UnityEngine.Object.DestroyImmediate(asset); }
    }
    [Test] public void SwitchingSourcesUnsubscribesThePreviousSource()
    {
        var catalog = ActualCatalog(); var own = new GameObject("switch_binding_test"); var other = new GameObject("second_source");
        var a = ScriptableObject.CreateInstance<SanctumConnectedDecorV01BoardAsset>(); var b = ScriptableObject.CreateInstance<SanctumConnectedDecorV01BoardAsset>();
        try
        {
            string error; var data = Altar(true); Assert.IsTrue(a.TrySetSnapshot(data, out error), error);
            data.revision = 2; Assert.IsTrue(b.TrySetSnapshot(data, out error), error);
            var first = own.AddComponent<SanctumConnectedDecorV01AuthoredSnapshotSource>(); Assert.IsTrue(first.AssignBoard(a, out error), error);
            var second = other.AddComponent<SanctumConnectedDecorV01AuthoredSnapshotSource>(); Assert.IsTrue(second.AssignBoard(b, out error), error);
            var binding = own.AddComponent<SanctumConnectedDecorV01Binding>(); var controller = own.GetComponent<SanctumConnectedDecorV01Controller>();
            Assert.IsTrue(binding.Bind(controller, catalog, SanctumConnectedDecorV01BoardSnapshot.ArtManifestSha256, "Default", first, out error, true), error);
            Assert.IsTrue(binding.Bind(controller, catalog, SanctumConnectedDecorV01BoardSnapshot.ArtManifestSha256, "Default", second, out error, true), error);
            var plan = controller.CurrentPlan; Assert.IsTrue(first.NotifyBoardChanged(out error), error);
            Assert.AreSame(plan, controller.CurrentPlan); Assert.AreEqual(2, binding.LastReport.revision);
        }
        finally { UnityEngine.Object.DestroyImmediate(own); UnityEngine.Object.DestroyImmediate(other); UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b); }
    }
    [Test] public void RefreshRepositionsMovedParentAndRejectsInvalidChangedHierarchy()
    {
        var catalog = ActualCatalog(); var parent = new GameObject("transform_parent"); var node = new GameObject("transform_binding_test");
        node.transform.SetParent(parent.transform, false); var asset = ScriptableObject.CreateInstance<SanctumConnectedDecorV01BoardAsset>();
        try
        {
            string error; Assert.IsTrue(asset.TrySetSnapshot(Altar(true), out error), error);
            var source = node.AddComponent<SanctumConnectedDecorV01AuthoredSnapshotSource>(); Assert.IsTrue(source.AssignBoard(asset, out error), error);
            var binding = node.AddComponent<SanctumConnectedDecorV01Binding>(); var controller = node.GetComponent<SanctumConnectedDecorV01Controller>();
            Assert.IsTrue(binding.Bind(controller, catalog, SanctumConnectedDecorV01BoardSnapshot.ArtManifestSha256, "Default", source, out error, true), error);
            parent.transform.position = new Vector3(5, 7, 0); Assert.IsTrue(binding.RefreshNow(out error), error);
            var renderer = node.GetComponentInChildren<SpriteRenderer>();
            Assert.AreEqual(1.5f, renderer.transform.position.x, .0001f); Assert.AreEqual(1f, renderer.transform.position.y, .0001f);
            parent.transform.localScale = new Vector3(-1, 1, 1); Assert.IsFalse(binding.RefreshNow(out error));
            Assert.AreEqual(0, controller.OwnedVisualCount);
        }
        finally { UnityEngine.Object.DestroyImmediate(parent); UnityEngine.Object.DestroyImmediate(asset); }
    }
    [Test] public void InspectorControllerReferenceChangeCannotStrandOldOrClearForeignVisuals()
    {
        var catalog = ActualCatalog(); var node = new GameObject("edited_binding_test"); var foreignNode = new GameObject("foreign_visuals");
        var asset = ScriptableObject.CreateInstance<SanctumConnectedDecorV01BoardAsset>();
        try
        {
            string error; var data = Altar(true); Assert.IsTrue(asset.TrySetSnapshot(data, out error), error);
            var source = node.AddComponent<SanctumConnectedDecorV01AuthoredSnapshotSource>(); Assert.IsTrue(source.AssignBoard(asset, out error), error);
            var binding = node.AddComponent<SanctumConnectedDecorV01Binding>(); var own = node.GetComponent<SanctumConnectedDecorV01Controller>();
            var foreign = foreignNode.AddComponent<SanctumConnectedDecorV01Controller>();
            Assert.IsTrue(foreign.Configure(catalog, SanctumConnectedDecorV01BoardSnapshot.ArtManifestSha256, "Default"));
            Assert.IsTrue(foreign.SetLayout(data.networkCells, data.groupPlacements, Vector3.zero, 1, data.CreateApprovedCellQuery()));
            foreach (SanctumConnectedDecorV01Controller invalidReference in new[] { (SanctumConnectedDecorV01Controller)null, foreign })
            {
                Assert.IsTrue(binding.Bind(own, catalog, SanctumConnectedDecorV01BoardSnapshot.ArtManifestSha256, "Default", source, out error, true), error);
                var serialized = new SerializedObject(binding); serialized.FindProperty("controller").objectReferenceValue = invalidReference;
                serialized.ApplyModifiedPropertiesWithoutUndo(); Assert.IsFalse(binding.RefreshNow(out error));
                Assert.AreEqual(0, own.OwnedVisualCount); Assert.AreEqual(1, foreign.OwnedVisualCount);
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(node); UnityEngine.Object.DestroyImmediate(foreignNode); UnityEngine.Object.DestroyImmediate(asset); }
    }
#endif
}
