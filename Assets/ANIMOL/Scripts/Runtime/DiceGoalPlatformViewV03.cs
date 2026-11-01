using UnityEngine;

namespace ANIMOL.Presentation.DiceHintV01
{
    // R5: 75% rigid native die + front-lip occlusion + sliding floor lids. Presentation only.
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
        GameObject floorRoot, dieRoot, frontRoot, ringRoot, leftRoot, rightRoot;
        SpriteRenderer floor, die, front, ring, left, right;
        Material clipMaterial;
        MaterialPropertyBlock clipProperties;
        bool configured;

        public bool Configure(DiceGoalPlatformArtV03 artSet, DiceGoalFaceViewV02 faceView,
            SpriteRenderer goalBase, Transform anchor, float size, out string error)
        {
            configured = false; error = null; EnsureOwned(); OnDisable();
            if (artSet == null || faceView == null || goalBase == null || anchor == null || !(size > 0) || float.IsInfinity(size))
            { error = "Assign R5 platform art, required face, goal renderer, anchor and finite world cell size."; return false; }
            if (!artSet.Validate(out error)) return false;
            if (requiredFace != null) requiredFace.PreviewVisibilityChanged -= OnPreviewVisibilityChanged;
            if (clipMaterial != null) Release(clipMaterial);
            art = artSet; requiredFace = faceView; originalGoalBase = goalBase; cellAnchor = anchor; cellWorldSize = size;
            requiredFace.PreviewVisibilityChanged += OnPreviewVisibilityChanged;
            clipMaterial = new Material(art.rigidClipShader) { name = "DiceRigidReceiverClip_Instance" };
            clipProperties = new MaterialPropertyBlock(); EnsureOwned(); originalGoalBase.sprite = null;
            configured = true; ShowReady(); return true;
        }
        public void ShowReady() { pose = Pose.Ready; progress = 0; capturedDie = null; Render(); }
        public bool ShowPressing(Sprite actualResolvedDieSprite, int committedTop, float normalizedProgress, out string error)
        {
            error = null;
            if (!configured || actualResolvedDieSprite == null || requiredFace == null || committedTop != requiredFace.RequiredFace
                || float.IsNaN(normalizedProgress) || float.IsInfinity(normalizedProgress))
            { error = "Absorption requires a real committed matching die and finite progress."; return false; }
            capturedDie = actualResolvedDieSprite; progress = Mathf.Clamp01(normalizedProgress); pose = Pose.Pressing; Render(); return true;
        }
        public void ShowWalkable() { pose = Pose.Walkable; progress = 1; capturedDie = null; Render(); }
        GameObject Owned(string name, out SpriteRenderer renderer)
        {
            Transform existing = transform.Find(name); GameObject go = existing == null ? new GameObject(name) : existing.gameObject;
            go.SetActive(false); go.transform.SetParent(transform, false);
            renderer = go.GetComponent<SpriteRenderer>(); if (renderer == null) renderer = go.AddComponent<SpriteRenderer>(); return go;
        }
        void EnsureOwned()
        {
            if (floorRoot == null) floorRoot = Owned("__DiceGoalPlatform_VisualOnly", out floor);
            if (dieRoot == null) dieRoot = Owned("__DiceGoalPressCopy_VisualOnly", out die);
            if (frontRoot == null) frontRoot = Owned("__DiceReceiverFrontRim_R4", out front);
            if (ringRoot == null) ringRoot = Owned("__DiceReceiverClosingRim_R4", out ring);
            if (leftRoot == null) leftRoot = Owned("__DiceReceiverLeftLid_R4", out left);
            if (rightRoot == null) rightRoot = Owned("__DiceReceiverRightLid_R4", out right);
        }
        void Put(SpriteRenderer renderer, Sprite sprite, Vector3 center, int order, bool clipped, Vector4 rect)
        {
            renderer.sprite = sprite; renderer.color = Color.white; renderer.gameObject.layer = originalGoalBase.gameObject.layer;
            renderer.sortingLayerID = originalGoalBase.sortingLayerID;
            renderer.sortingOrder = originalGoalBase.sortingOrder + order;
            renderer.sharedMaterial = clipped ? clipMaterial : originalGoalBase.sharedMaterial;
            float widthInCells = renderer == die ? DiceVisualSizeV05.CanvasWidthInCells : 320f / 256f;
            DiceFaceLayoutV02.Place(renderer.transform, sprite, center, cellWorldSize, widthInCells);
            if (clipped)
            { clipProperties.Clear(); clipProperties.SetVector("_ClipRect", rect); renderer.SetPropertyBlock(clipProperties); }
            else renderer.SetPropertyBlock(null);
        }
        void Render()
        {
            if (!configured || !isActiveAndEnabled || cellAnchor == null || originalGoalBase == null) return;
            EnsureOwned(); Vector3 center = cellAnchor.position;
            bool absorbing = pose == Pose.Pressing && progress < .94f;
            floorRoot.SetActive(!(pose == Pose.Ready && requiredFace.HasPreviewOwners));
            Put(floor, pose == Pose.Walkable || pose == Pose.Pressing && progress >= .94f ? art.walkable : art.ready, center, 0, false, Vector4.zero);
            requiredFace.SetConvertedPresentation(pose != Pose.Ready);
            dieRoot.SetActive(absorbing && capturedDie != null && progress < .67f);
            frontRoot.SetActive(absorbing); ringRoot.SetActive(false); leftRoot.SetActive(false); rightRoot.SetActive(false);
            if (!absorbing) return;
            float dieY, closed; DiceAbsorbMotionV04.Sample(progress, out dieY, out closed);
            Put(front, art.frontRim, center, 4, false, Vector4.zero);
            if (dieRoot.activeSelf)
            {
                Vector4 bodyRect = new Vector4(center.x - 2 * cellWorldSize, center.y + DiceAbsorbMotionV04.FrontPlaneY * cellWorldSize,
                    center.x + 2 * cellWorldSize, center.y + 2 * cellWorldSize);
                // Constant scale/aspect and full opacity: only translation and physical clipping change visibility.
                Put(die, capturedDie, center + Vector3.up * (dieY * cellWorldSize), 2, true, bodyRect);
            }
            // Once the upper body reaches the opening, the side/back bezel also occludes its edges.
            if (-dieY >= .18f || closed > 0)
            { ringRoot.SetActive(true); Put(ring, art.closingRim, center, 3, false, Vector4.zero); }
            if (closed <= 0) return;
            leftRoot.SetActive(true); rightRoot.SetActive(true); ringRoot.SetActive(true);
            Vector4 a = art.apertureCells;
            Vector4 lidRect = new Vector4(center.x + a.x * cellWorldSize, center.y + a.y * cellWorldSize,
                center.x + a.z * cellWorldSize, center.y + a.w * cellWorldSize);
            float travel = (a.z - a.x) * .5f * (1 - closed) * cellWorldSize;
            Put(left, art.lidLeft, center + Vector3.left * travel, 1, true, lidRect);
            Put(right, art.lidRight, center + Vector3.right * travel, 1, true, lidRect);
            Put(ring, art.closingRim, center, 3, false, Vector4.zero);
        }
        void LateUpdate() { Render(); }
        void OnPreviewVisibilityChanged(bool ignored) { Render(); }
        void OnEnable() { if (configured) Render(); }
        void OnDisable()
        { foreach (GameObject go in new[] { floorRoot, dieRoot, frontRoot, ringRoot, leftRoot, rightRoot }) if (go != null) go.SetActive(false); }
        static void Release(Object value) { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
        void OnDestroy()
        {
            if (requiredFace != null) requiredFace.PreviewVisibilityChanged -= OnPreviewVisibilityChanged;
            foreach (GameObject go in new[] { floorRoot, dieRoot, frontRoot, ringRoot, leftRoot, rightRoot })
            { if (go == null) continue; go.SetActive(false); Release(go); }
            if (clipMaterial != null) Release(clipMaterial);
        }
    }
}
