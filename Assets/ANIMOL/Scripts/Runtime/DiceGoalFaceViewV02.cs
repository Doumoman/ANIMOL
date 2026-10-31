using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Presentation.DiceHintV01
{
    // Fixed required face on an existing goal tile. The host alone decides matching/unlocking.
    [DisallowMultipleComponent]
    public sealed class DiceGoalFaceViewV02 : MonoBehaviour
    {
        [SerializeField] DiceFaceSetV02 faceSet;
        [SerializeField] SpriteRenderer originalGoalBase;
        [SerializeField] Transform cellAnchor;
        [SerializeField] float cellWorldSize = 1;
        [SerializeField, Range(1, 6)] int requiredFace = 4;
        public Vector2Int LogicalCell { get; private set; }
        public int RequiredFace { get { return requiredFace; } }
        public bool CanDisplay { get { return ready && !convertedPresentation && isActiveAndEnabled && cellAnchor != null && originalGoalBase != null && originalGoalBase.enabled && originalGoalBase.gameObject.activeInHierarchy; } }
        public bool RequiredFaceVisible { get { return CanDisplay && previewOwners.Count == 0; } }
        public bool HasPreviewOwners { get { return previewOwners.Count > 0; } }
        public event System.Action<bool> PreviewVisibilityChanged;
        readonly HashSet<int> previewOwners = new HashSet<int>();
        GameObject ownedRoot;
        SpriteRenderer owned;
        bool ready;
        bool convertedPresentation;

        public bool Configure(DiceFaceSetV02 faces, SpriteRenderer goalBase, Transform anchor,
            float worldCellSize, Vector2Int cell, int actualRequiredFace, out string error)
        {
            ready = false; convertedPresentation = false; HideOwned(); previewOwners.Clear();
            faceSet = faces; originalGoalBase = goalBase; cellAnchor = anchor;
            cellWorldSize = worldCellSize; LogicalCell = cell; requiredFace = actualRequiredFace;
            error = null; Sprite ignored;
            if (faces == null || goalBase == null || anchor == null || !(worldCellSize > 0) || float.IsInfinity(worldCellSize))
            { error = "Assign faces, original goal floor, cell anchor and a positive finite cell size."; return false; }
            if (!faces.Validate(out error)) return false;
            if (!faces.TryGet(actualRequiredFace, out ignored)) { error = "Goal required face must be 1..6."; return false; }
            EnsureOwned(); ready = true; Refresh(); return true;
        }
        public void SetPreviewOwner(int owner, bool present)
        {
            bool before = HasPreviewOwners;
            if (present) previewOwners.Add(owner); else previewOwners.Remove(owner);
            Refresh();
            if (before != HasPreviewOwners && PreviewVisibilityChanged != null) PreviewVisibilityChanged(HasPreviewOwners);
        }
        // Presentation suppression only. The authoritative goal state belongs to the host.
        public void SetConvertedPresentation(bool converted) { convertedPresentation = converted; Refresh(); }
        void EnsureOwned()
        {
            if (ownedRoot != null) return;
            Transform existing = transform.Find("__DiceGoalFace_VisualOnly");
            if (existing != null)
            {
                ownedRoot = existing.gameObject; owned = ownedRoot.GetComponent<SpriteRenderer>();
                if (owned == null) owned = ownedRoot.AddComponent<SpriteRenderer>();
                return;
            }
            ownedRoot = new GameObject("__DiceGoalFace_VisualOnly"); ownedRoot.SetActive(false);
            ownedRoot.transform.SetParent(transform, false); owned = ownedRoot.AddComponent<SpriteRenderer>();
        }
        void Refresh()
        {
            Sprite face;
            if (!RequiredFaceVisible || faceSet == null || !faceSet.TryGet(requiredFace, out face)) { HideOwned(); return; }
            EnsureOwned(); ownedRoot.SetActive(true); owned.sprite = face; owned.color = Color.white;
            owned.sortingLayerID = originalGoalBase.sortingLayerID;
            owned.sortingOrder = originalGoalBase.sortingOrder + 1;
            if (originalGoalBase.sharedMaterial != null) owned.sharedMaterial = originalGoalBase.sharedMaterial;
            DiceFaceLayoutV02.Place(ownedRoot.transform, face, cellAnchor.position, cellWorldSize, DiceFaceLayoutV02.GoalWidth);
        }
        void HideOwned() { if (ownedRoot != null) ownedRoot.SetActive(false); }
        void LateUpdate() { Refresh(); }
        void OnDisable() { previewOwners.Clear(); HideOwned(); }
        void OnDestroy() { if (ownedRoot == null) return; ownedRoot.SetActive(false); if (Application.isPlaying) Destroy(ownedRoot); else DestroyImmediate(ownedRoot); }
    }
}
