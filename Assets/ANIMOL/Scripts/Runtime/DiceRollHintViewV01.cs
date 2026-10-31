using UnityEngine;

namespace ANIMOL.Presentation.DiceHintV01
{
    // R2 replaces the former numeric TextMesh with one complete flat die-face Sprite.
    [DisallowMultipleComponent]
    public sealed class DiceRollHintViewV01 : MonoBehaviour
    {
        [SerializeField] DiceRollHintSourceV01 source;
        [SerializeField] SpriteRenderer originalDiceRenderer;
        [SerializeField] DiceFaceSetV02 faceSet;
        [SerializeField, Range(.05f, .8f)] float minimumAlpha = .25f;
        [SerializeField, Range(.1f, .9f)] float maximumAlpha = .55f;
        [SerializeField, Range(.6f, 2f)] float pulseSeconds = 1.2f;
        GameObject ownedRoot;
        SpriteRenderer owned;
        DiceGoalFaceViewV02 sharedGoal;
        long lastRevision;
        DiceRollHint previous;
        bool showing, setupValid;
        float shownAt;

        public bool Configure(DiceRollHintSourceV01 value, SpriteRenderer dice, DiceFaceSetV02 faces, out string error)
        {
            HideImmediately(); setupValid = false; source = value; originalDiceRenderer = dice; faceSet = faces;
            error = null;
            // Remove only the previous version's owned numeric child, preserving the actual die.
            Transform legacy = transform.Find("__DiceNextTopHint_VisualOnly");
            if (legacy != null) { legacy.gameObject.SetActive(false); if (Application.isPlaying) Destroy(legacy.gameObject); else DestroyImmediate(legacy.gameObject); }
            if (source == null || dice == null || faces == null) { error = "Assign the actual board source, original dice renderer and the six-face set."; return false; }
            if (!faces.Validate(out error)) return false;
            EnsureOwned(); setupValid = true; return true;
        }
        void EnsureOwned()
        {
            if (ownedRoot != null) return;
            Transform existing = transform.Find("__DiceNextFaceHint_VisualOnly");
            if (existing != null)
            {
                ownedRoot = existing.gameObject; owned = ownedRoot.GetComponent<SpriteRenderer>();
                if (owned == null) owned = ownedRoot.AddComponent<SpriteRenderer>();
                return;
            }
            ownedRoot = new GameObject("__DiceNextFaceHint_VisualOnly"); ownedRoot.SetActive(false);
            ownedRoot.transform.SetParent(transform, false); owned = ownedRoot.AddComponent<SpriteRenderer>();
        }
        void LateUpdate()
        {
            DiceHintSnapshot snapshot; DiceRollHint hint; Sprite face;
            if (!setupValid || source == null || faceSet == null || originalDiceRenderer == null
                || !originalDiceRenderer.enabled || !originalDiceRenderer.gameObject.activeInHierarchy
                || !source.TryRead(out snapshot) || !DiceRollHintPolicyV01.TryEvaluate(snapshot, out hint)
                || !faceSet.TryGet(hint.NextTop, out face)) { HideImmediately(); return; }
            EnsureOwned();
            if (!showing || lastRevision != snapshot.Revision || previous.NextTop != hint.NextTop
                || previous.Direction != hint.Direction || previous.Destination != hint.Destination)
            { shownAt = Time.unscaledTime; showing = true; previous = hint; lastRevision = snapshot.Revision; }
            DiceGoalFaceViewV02 goal = snapshot.DestinationGoal;
            if (goal != null && (goal.LogicalCell != hint.Destination || !goal.CanDisplay)) goal = null;
            if (sharedGoal != goal) { ReleaseGoal(); sharedGoal = goal; }
            if (sharedGoal != null) sharedGoal.SetPreviewOwner(GetInstanceID(), true);
            Vector3 center = source.CellToWorld(hint.Destination);
            float size = Vector3.Distance(center, source.CellToWorld(hint.Destination + Vector2Int.up));
            if (!(size > .00001f) || float.IsInfinity(size)) { HideImmediately(); return; }
            ownedRoot.SetActive(true); owned.sprite = face;
            owned.sortingLayerID = originalDiceRenderer.sortingLayerID; owned.sortingOrder = originalDiceRenderer.sortingOrder + 2;
            if (originalDiceRenderer.sharedMaterial != null) owned.sharedMaterial = originalDiceRenderer.sharedMaterial;
            // Exactly one full face at the destination center; any required-face overlay is hidden above.
            DiceFaceLayoutV02.Place(ownedRoot.transform, face, center, size, DiceFaceLayoutV02.HintWidth);
            float elapsed = Mathf.Max(0, Time.unscaledTime - shownAt - .15f);
            float wave = .5f + .5f * Mathf.Cos(elapsed * Mathf.PI * 2 / Mathf.Max(.6f, pulseSeconds));
            owned.color = new Color(1, 1, 1, Mathf.Lerp(Mathf.Min(minimumAlpha, maximumAlpha), Mathf.Max(minimumAlpha, maximumAlpha), wave));
        }
        void ReleaseGoal() { if (sharedGoal != null) sharedGoal.SetPreviewOwner(GetInstanceID(), false); sharedGoal = null; }
        public void HideImmediately() { showing = false; ReleaseGoal(); if (ownedRoot != null) ownedRoot.SetActive(false); }
        void OnEnable()
        {
            setupValid = false;
            if (source != null && originalDiceRenderer != null && faceSet != null)
            { string error; if (!Configure(source, originalDiceRenderer, faceSet, out error)) Debug.LogError(error, this); }
        }
        void OnDisable() { HideImmediately(); }
        void OnDestroy() { HideImmediately(); if (ownedRoot == null) return; if (Application.isPlaying) Destroy(ownedRoot); else DestroyImmediate(ownedRoot); }
    }
}
