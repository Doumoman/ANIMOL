using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Art.StarSwellCoastV01
{
    /// <summary>
    /// Visual-only repeating distant ocean BELOW one world-anchored horizon.
    /// Recycles exact native carrier sprites around an orthographic XY camera.
    /// Never registers water/land cells, changes collision or introduces a current.
    /// The horizon, sunset sun and reflection are separate, NON-repeating sprites.
    /// This is a background underlay, not a substitute for host water-cell renderers.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10020)]
    public sealed class StarSwellCoastV01OceanField : MonoBehaviour
    {
        [SerializeField] private StarSwellCoastV01VisualCatalog sourceCatalog;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private string expectedCatalogSha256, carrierFile;
        private Sprite nativeCarrier;
        [SerializeField] private float worldOriginX, horizonTopY, authoredZ;
        private float tileWidth, tileHeight;
        [SerializeField] private string layerName;
        [SerializeField] private int order;
        private bool configured;
        [SerializeField] private List<SpriteRenderer> owned = new List<SpriteRenderer>();

        private void OnEnable()
        {
            configured = false; ClearVisuals();
            if (sourceCatalog != null) Configure(sourceCatalog, targetCamera, expectedCatalogSha256,
                carrierFile, worldOriginX, horizonTopY, authoredZ, layerName, order);
        }

        public bool Configure(StarSwellCoastV01VisualCatalog catalog, Camera camera,
            string expectedManifestSha256, string exactCarrierFile, float originX,
            float worldHorizonTopY, float worldZ, string sortingLayerName, int sortingOrder)
        {
            ClearVisuals(); configured = false;
            // Persist the attempted configuration, including a bad/null current catalog.
            // A later enable must never resurrect a previously valid older carrier.
            sourceCatalog = catalog; targetCamera = camera;
            expectedCatalogSha256 = expectedManifestSha256; carrierFile = exactCarrierFile;
            worldOriginX = originX; horizonTopY = worldHorizonTopY; authoredZ = worldZ;
            layerName = sortingLayerName; order = sortingOrder; nativeCarrier = null;
            if (catalog == null || camera == null || !camera.orthographic
                || !Finite(originX) || !Finite(worldHorizonTopY) || !Finite(worldZ)
                || sortingOrder < -32768 || sortingOrder > 32767)
                return Fail("Invalid ocean configuration; no legacy carrier is used.");
            StarSwellCoastV01VisualCatalog.Part part; string error;
            if (!catalog.TryGetPart(exactCarrierFile, expectedManifestSha256, out part, out error)) return Fail(error);
            if (part.Frame.File != exactCarrierFile || part.Frame.Sprite == null) return Fail("Ocean needs its exact latest carrier.");
            Vector3 size = part.Frame.Sprite.bounds.size;
            if (size.x <= 0f || size.y <= 0f) return Fail("Invalid ocean carrier size.");
            bool knownLayer = false;
            foreach (SortingLayer layer in SortingLayer.layers) if (layer.name == sortingLayerName) knownLayer = true;
            if (!knownLayer) return Fail("Ocean Sorting Layer does not exist: " + sortingLayerName);
            nativeCarrier = part.Frame.Sprite; tileWidth = size.x; tileHeight = size.y;
            configured = true; FitNow(); return true;
        }

        // Host may rebase the ONE authored horizon for a different full-map extent.
        // Do not change this every frame to camera-lock a world horizon.
        public void SetHorizonTop(float worldY)
        { if (!Finite(worldY)) throw new ArgumentOutOfRangeException("worldY"); horizonTopY = worldY; FitNow(); }

        private void LateUpdate() { FitNow(); }
        private void OnDisable() { ClearVisuals(); }
        private bool Fail(string error) { Debug.LogError("[Coast V01] " + error, this); return false; }
        private static bool Finite(float f) { return !float.IsNaN(f) && !float.IsInfinity(f); }
        private void ClearVisuals() { foreach (SpriteRenderer r in owned) if (r != null) r.enabled = false; }

        public void FitNow()
        {
            if (!configured || !isActiveAndEnabled || targetCamera == null || nativeCarrier == null) return;
            if (!targetCamera.orthographic || Mathf.Abs(targetCamera.transform.forward.z) < .9999f
                || Mathf.Abs(targetCamera.transform.up.y) < .9999f
                || Quaternion.Angle(transform.rotation, Quaternion.identity) > .01f
                || (transform.lossyScale - Vector3.one).sqrMagnitude > .000001f)
            { ClearVisuals(); return; }
            float halfY = targetCamera.orthographicSize, halfX = halfY * targetCamera.aspect;
            Vector3 camera = targetCamera.transform.position;
            if (!Finite(halfY) || !Finite(halfX) || halfY <= 0f || halfX <= 0f
                || !Finite(camera.x) || !Finite(camera.y)
                || Mathf.Abs(camera.x - worldOriginX) > 1000000f
                || Mathf.Abs(camera.y - horizonTopY) > 1000000f)
            { ClearVisuals(); return; }
            int firstX = Mathf.FloorToInt((camera.x - halfX - worldOriginX) / tileWidth) - 1;
            int lastX = Mathf.CeilToInt((camera.x + halfX - worldOriginX) / tileWidth) + 1;
            int firstRow = Mathf.Max(0, Mathf.FloorToInt((horizonTopY - camera.y - halfY) / tileHeight) - 1);
            int lastRow = Mathf.CeilToInt((horizonTopY - camera.y + halfY) / tileHeight) + 1;
            long count = lastRow < firstRow ? 0L : ((long)lastX - firstX + 1) * ((long)lastRow - firstRow + 1);
            if (count < 0 || count > 8192) { ClearVisuals(); return; }
            while (owned.Count < count)
            {
                var node = new GameObject("CoastOcean_VISUAL_ONLY_" + owned.Count);
                node.transform.SetParent(transform, false);
                owned.Add(node.AddComponent<SpriteRenderer>());
            }
            int index = 0;
            Vector3 centre = nativeCarrier.bounds.center;
            for (int row = firstRow; row <= lastRow; row++)
            for (int x = firstX; x <= lastX; x++)
            {
                SpriteRenderer r = owned[index++];
                r.sprite = nativeCarrier; r.sortingLayerName = layerName; r.sortingOrder = order;
                r.transform.position = new Vector3(worldOriginX + (x + .5f) * tileWidth - centre.x,
                    horizonTopY - (row + .5f) * tileHeight - centre.y, authoredZ);
                r.transform.rotation = Quaternion.identity; r.transform.localScale = Vector3.one;
                r.enabled = true;
            }
            for (; index < owned.Count; index++) if (owned[index] != null) owned[index].enabled = false;
        }
    }
}
