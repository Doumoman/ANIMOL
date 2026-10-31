using UnityEngine;

namespace ANIMOL.Presentation.DiceHintV01
{
    // Renders committed host states only; never creates support, clears occupancy or spends a turn.
    [DisallowMultipleComponent]
    public sealed class DiceGoalPlatformViewV03 : MonoBehaviour
    {
        enum Pose { Ready, Pressing, Walkable }
        DiceGoalPlatformArtV03 art;
        DiceGoalFaceViewV02 requiredFace;
        SpriteRenderer originalGoalBase;
        Transform cellAnchor;
        float cellWorldSize, progress;
        Pose pose;
        Sprite capturedDie;
        GameObject floorRoot, dieRoot;
        SpriteRenderer floor, die;
        bool configured;

        public bool Configure(DiceGoalPlatformArtV03 artSet, DiceGoalFaceViewV02 faceView,
            SpriteRenderer goalBase, Transform anchor, float size, out string error)
        {
            configured = false; error = null;
            if (artSet == null || faceView == null || goalBase == null || anchor == null || !(size > 0) || float.IsInfinity(size))
            { error = "Assign platform art, required-face view, original goal renderer, cell anchor and finite world cell size."; return false; }
            if (!artSet.Validate(out error)) return false;
            if (requiredFace != null) requiredFace.PreviewVisibilityChanged -= OnPreviewVisibilityChanged;
            art = artSet; requiredFace = faceView; originalGoalBase = goalBase; cellAnchor = anchor; cellWorldSize = size;
            requiredFace.PreviewVisibilityChanged += OnPreviewVisibilityChanged;
            EnsureOwned();
            // Old numeric artwork must have no other writer after installing this visual binding.
            originalGoalBase.sprite = null;
            configured = true; ShowReady(); return true;
        }
        public void ShowReady()
        { pose = Pose.Ready; progress = 0; capturedDie = null; Render(); }
        public bool ShowPressing(Sprite actualResolvedDieSprite, int committedTop, float normalizedProgress, out string error)
        {
            error = null;
            if (!configured || actualResolvedDieSprite == null || requiredFace == null || committedTop != requiredFace.RequiredFace
                || float.IsNaN(normalizedProgress) || float.IsInfinity(normalizedProgress))
            { error = "Press presentation requires a real committed matching die and finite progress."; return false; }
            capturedDie = actualResolvedDieSprite; progress = Mathf.Clamp01(normalizedProgress); pose = Pose.Pressing; Render(); return true;
        }
        public void ShowWalkable()
        { pose = Pose.Walkable; progress = 1; capturedDie = null; Render(); }
        void EnsureOwned()
        {
            if (floorRoot == null) floorRoot = Owned("__DiceGoalPlatform_VisualOnly", out floor);
            if (dieRoot == null) dieRoot = Owned("__DiceGoalPressCopy_VisualOnly", out die);
        }
        GameObject Owned(string childName, out SpriteRenderer renderer)
        {
            Transform existing = transform.Find(childName);
            GameObject go = existing == null ? new GameObject(childName) : existing.gameObject;
            go.SetActive(false); go.transform.SetParent(transform, false);
            renderer = go.GetComponent<SpriteRenderer>(); if (renderer == null) renderer = go.AddComponent<SpriteRenderer>();
            return go;
        }
        void Render()
        {
            if (!configured || !isActiveAndEnabled || cellAnchor == null || originalGoalBase == null) return;
            EnsureOwned(); floorRoot.SetActive(!(pose == Pose.Ready && requiredFace.HasPreviewOwners));
            Sprite plate = pose == Pose.Ready || pose == Pose.Pressing && progress < .15f ? art.ready : pose == Pose.Walkable || progress >= .90f ? art.walkable : progress < .45f ? art.halfPressed : art.nearlyFlush;
            floor.sprite = plate; floor.color = Color.white;
            floor.sortingLayerID = originalGoalBase.sortingLayerID; floor.sortingOrder = originalGoalBase.sortingOrder;
            if (originalGoalBase.sharedMaterial != null) floor.sharedMaterial = originalGoalBase.sharedMaterial;
            // Plate PNG: 320 canvas / 256 visible registration width / PPU256.
            DiceFaceLayoutV02.Place(floorRoot.transform, plate, cellAnchor.position, cellWorldSize, 320f / 256f);
            requiredFace.SetConvertedPresentation(pose != Pose.Ready);
            bool showDie = pose == Pose.Pressing && capturedDie != null && progress < .90f;
            dieRoot.SetActive(showDie);
            if (!showDie) return;
            die.sprite = capturedDie; die.sortingLayerID = floor.sortingLayerID; die.sortingOrder = floor.sortingOrder + 2;
            if (originalGoalBase.sharedMaterial != null) die.sharedMaterial = originalGoalBase.sharedMaterial;
            float sink = Mathf.SmoothStep(0, 1, progress / .90f);
            DiceFaceLayoutV02.Place(dieRoot.transform, capturedDie, cellAnchor.position + Vector3.down * (cellWorldSize * .16f * sink), cellWorldSize, 320f / 256f);
            Vector3 scale = dieRoot.transform.localScale; scale.y *= Mathf.Lerp(1, .24f, sink); dieRoot.transform.localScale = scale;
            die.color = new Color(1, 1, 1, 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.60f, .90f, progress)));
        }
        void LateUpdate() { Render(); }
        void OnPreviewVisibilityChanged(bool ignored) { Render(); }
        void OnEnable() { if (configured) Render(); }
        void OnDisable() { if (floorRoot != null) floorRoot.SetActive(false); if (dieRoot != null) dieRoot.SetActive(false); }
        void OnDestroy()
        {
            if (requiredFace != null) requiredFace.PreviewVisibilityChanged -= OnPreviewVisibilityChanged;
            foreach (GameObject go in new[] { floorRoot, dieRoot })
            { if (go == null) continue; go.SetActive(false); if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
        }
    }
}
