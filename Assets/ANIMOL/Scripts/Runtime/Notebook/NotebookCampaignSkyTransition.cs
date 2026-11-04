using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace William.NotebookUI
{
    /// <summary>A temporary, unscaled campaign-entry overlay. It never enters a stage or spends currency.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(RawImage))]
    public sealed class NotebookCampaignSkyTransition : MonoBehaviour
    {
        public const float DurationSeconds = 1.10f, MapCommitProgress = .62f;
        // A serialized shader reference keeps the dedicated shader in player builds.
        public Shader transitionShader;
        public Texture destinationTexture;
        public CanvasGroup inputScope;

        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int SourceId = Shader.PropertyToID("_SourceTex");
        private static readonly int DestinationId = Shader.PropertyToID("_DestinationTex");
        private static readonly int SourceUvId = Shader.PropertyToID("_SourceUvRect");
        private static readonly int DestinationUvId = Shader.PropertyToID("_DestinationUvRect");
        private static readonly int SourceAspectId = Shader.PropertyToID("_SourceAspect");
        private static readonly int DestinationAspectId = Shader.PropertyToID("_DestinationAspect");
        private static readonly int ViewAspectId = Shader.PropertyToID("_ViewAspect");

        private RawImage overlay;
        private Material ownedMaterial;
        private Action commitMap, settled;
        private bool playing, committed, scopeCaptured, oldInteractable, oldBlocksRaycasts;
        private float elapsed;
        private GameObject previousSelection;
        public bool IsPlaying => playing;
        public bool HasCommitted => committed;
        public float Progress => playing ? Mathf.Clamp01(elapsed / DurationSeconds) : 0f;

        private void Awake()
        {
            overlay = GetComponent<RawImage>();
            overlay.enabled = false; overlay.raycastTarget = false;
        }

        /// <returns>False when unavailable; the caller must open the map immediately.</returns>
        public bool Play(Sprite source, Action mapCommit, Action onSettled)
        {
            if (playing || !isActiveAndEnabled || mapCommit == null || !transitionShader || !transitionShader.isSupported
                || !destinationTexture || !inputScope || !source || !source.texture)
                return false;
            // Atlas rectangles are supported; rotated/tight packing must fall back rather than sample unrelated art.
            if (source.packed && (source.packingRotation != SpritePackingRotation.None || source.packingMode == SpritePackingMode.Tight)) return false;
            Rect sourceRect;
            try { sourceRect = source.textureRect; }
            catch (InvalidOperationException) { return false; }
            catch (UnityException) { return false; }
            if (sourceRect.width <= 0 || sourceRect.height <= 0) return false;
            try
            {
                if (!overlay) overlay = GetComponent<RawImage>();
                if (!ownedMaterial || ownedMaterial.shader != transitionShader)
                {
                    ReleaseMaterial();
                    ownedMaterial = new Material(transitionShader) { name = "Campaign Sky Transition (Runtime)", hideFlags = HideFlags.HideAndDontSave };
                }
                Texture texture = source.texture;
                ownedMaterial.SetTexture(SourceId, texture); ownedMaterial.SetTexture(DestinationId, destinationTexture);
                ownedMaterial.SetVector(SourceUvId, new Vector4(sourceRect.x / texture.width, sourceRect.y / texture.height, sourceRect.width / texture.width, sourceRect.height / texture.height));
                ownedMaterial.SetVector(DestinationUvId, new Vector4(0, 0, 1, 1));
                ownedMaterial.SetFloat(SourceAspectId, source.rect.width / Mathf.Max(1f, source.rect.height));
                ownedMaterial.SetFloat(DestinationAspectId, destinationTexture.width / Mathf.Max(1f, destinationTexture.height));
                ownedMaterial.SetFloat(ProgressId, 0);
                overlay.texture = Texture2D.whiteTexture; overlay.material = ownedMaterial; overlay.color = Color.white;
                oldInteractable = inputScope.interactable; oldBlocksRaycasts = inputScope.blocksRaycasts; scopeCaptured = true;
                previousSelection = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
                commitMap = mapCommit; settled = onSettled; elapsed = 0; committed = false; playing = true;
                overlay.enabled = true; overlay.raycastTarget = true;
                transform.SetAsLastSibling();
                inputScope.interactable = false; inputScope.blocksRaycasts = false;
                if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
                UpdateViewAspect();
                return true;
            }
            catch (Exception exception)
            {
                try { Cancel(); }
                finally
                {
                    if (overlay) { overlay.enabled = false; overlay.raycastTarget = false; }
                    ReleaseMaterial();
                }
                Debug.LogWarning("Campaign sky transition could not start; opening the map directly. " + exception.Message, this);
                return false;
            }
        }

        private void Update()
        {
            if (!playing) return;
            if (!ownedMaterial || !transitionShader || !transitionShader.isSupported) { CompleteImmediately(); return; }
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / DurationSeconds);
            ownedMaterial.SetFloat(ProgressId, progress); UpdateViewAspect();
            if (progress >= MapCommitProgress && !CommitOnce()) return;
            if (progress >= 1) Finish();
        }

        private void UpdateViewAspect()
        {
            if (!ownedMaterial || !overlay) return;
            Rect rect = overlay.rectTransform.rect;
            ownedMaterial.SetFloat(ViewAspectId, Mathf.Max(1f, rect.width) / Mathf.Max(1f, rect.height));
        }

        private bool CommitOnce()
        {
            if (committed) return playing;
            committed = true;
            Action action = commitMap; commitMap = null;
            try { action?.Invoke(); }
            catch (Exception exception) { Debug.LogException(exception, this); Finish(); return false; }
            return playing;
        }

        /// <summary>Reduced motion or a renderer failure settles directly on the destination once.</summary>
        public void CompleteImmediately()
        {
            if (!playing) return;
            if (CommitOnce()) Finish();
        }

        /// <summary>Cancellation preserves whichever page was already committed and always releases input.</summary>
        public void Cancel()
        {
            if (playing) Finish();
        }

        private void Finish()
        {
            if (!playing) return;
            // Clear before callbacks so disable/reentry cannot invoke them twice.
            playing = false; commitMap = null;
            Action onSettled = settled; settled = null;
            if (overlay) { overlay.enabled = false; overlay.raycastTarget = false; }
            if (scopeCaptured && inputScope)
            {
                inputScope.interactable = oldInteractable; inputScope.blocksRaycasts = oldBlocksRaycasts;
            }
            scopeCaptured = false;
            if (!committed && previousSelection && previousSelection.activeInHierarchy && EventSystem.current)
                EventSystem.current.SetSelectedGameObject(previousSelection);
            previousSelection = null;
            ReleaseMaterial();
            onSettled?.Invoke();
        }

        private void OnApplicationFocus(bool focused) { if (!focused) Cancel(); }
        private void OnApplicationPause(bool paused) { if (paused) Cancel(); }
        private void OnDisable() { Cancel(); ReleaseMaterial(); }
        private void OnDestroy() { Cancel(); ReleaseMaterial(); }
        private void ReleaseMaterial()
        {
            if (!ownedMaterial) return;
            if (overlay && overlay.material == ownedMaterial) overlay.material = null;
            if (Application.isPlaying) Destroy(ownedMaterial); else DestroyImmediate(ownedMaterial);
            ownedMaterial = null;
        }
    }
}
