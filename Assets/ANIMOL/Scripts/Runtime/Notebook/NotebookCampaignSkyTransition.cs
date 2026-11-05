using System;
using System.Collections.Generic;
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
        private static readonly int[] LinkIds = {
            Shader.PropertyToID("_Link0"), Shader.PropertyToID("_Link1"), Shader.PropertyToID("_Link2"), Shader.PropertyToID("_Link3"),
            Shader.PropertyToID("_Link4"), Shader.PropertyToID("_Link5"), Shader.PropertyToID("_Link6"), Shader.PropertyToID("_Link7")
        };
        // These coordinates mirror the preserved 27 shader stars, solely to choose clean short links.
        private static readonly Vector2[] LinkStars = {
            new Vector2(-.42f,.15f), new Vector2(-.25f,.34f), new Vector2(-.05f,.25f), new Vector2(.13f,.42f), new Vector2(.32f,.31f),
            new Vector2(-.24f,-.35f), new Vector2(.02f,-.19f), new Vector2(.23f,-.30f), new Vector2(.42f,-.05f),
            new Vector2(-.48f,.62f), new Vector2(.12f,.70f), new Vector2(.44f,.52f), new Vector2(-.34f,-.58f), new Vector2(.39f,-.55f),
            new Vector2(-.08f,.06f), new Vector2(.27f,.03f), new Vector2(-.50f,-.06f), new Vector2(.03f,-.73f),
            new Vector2(-.18f,.78f), new Vector2(.52f,.82f), new Vector2(-.48f,-.80f), new Vector2(.49f,-.86f), new Vector2(-.03f,.49f),
            new Vector2(.53f,.13f), new Vector2(-.51f,.40f), new Vector2(.04f,-.47f), new Vector2(.26f,.91f)
        };

        private RawImage overlay;
        private Material ownedMaterial;
        private Action commitMap, settled;
        private bool playing, committed, scopeCaptured, oldInteractable, oldBlocksRaycasts;
        private float elapsed;
        private GameObject previousSelection;
        private string linkSignature = string.Empty;
        public bool IsPlaying => playing;
        public bool HasCommitted => committed;
        public string LinkSignature => linkSignature;
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
                ConfigureEntryLinks();
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

        private void ConfigureEntryLinks()
        {
            Rect rect = overlay.rectTransform.rect;
            float aspect = Mathf.Clamp(Mathf.Max(1f, rect.width) / Mathf.Max(1f, rect.height), .25f, 4f);
            uint seed = BitConverter.ToUInt32(Guid.NewGuid().ToByteArray(), 0);
            List<Vector2Int> links = GenerateEntryLinks(aspect, seed, linkSignature);
            var ids = new List<int>(links.Count);
            for (int i = 0; i < links.Count; i++)
            {
                Vector2Int pair = links[i];
                ownedMaterial.SetVector(LinkIds[i], new Vector4(LinkStars[pair.x].x + 2 * (pair.x / 9), LinkStars[pair.x].y, LinkStars[pair.y].x, LinkStars[pair.y].y));
                ids.Add(pair.x * 27 + pair.y);
            }
            ids.Sort(); linkSignature = string.Join(",", ids);
        }

        // A local RNG avoids consuming UnityEngine.Random state used by gameplay or artwork selection.
        private static uint NextLinkRandom(ref uint state)
        {
            if (state == 0) state = 0x6D2B79F5u;
            state ^= state << 13; state ^= state >> 17; state ^= state << 5; return state;
        }
        private static Vector2 LinkProjection(int index, float travel, float aspect)
        {
            float depth = .90f + .46f * (index / 9) - travel * .43f;
            return new Vector2(LinkStars[index].x * aspect, LinkStars[index].y) / depth;
        }
        private static float LinkOrientation(Vector2 a, Vector2 b, Vector2 c)
            => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        private static bool LinksCross(Vector2Int a, Vector2Int b, float aspect)
        {
            if (a.x == b.x || a.x == b.y || a.y == b.x || a.y == b.y) return false;
            for (int sample = 0; sample < 3; sample++)
            {
                float travel = sample * .5f;
                Vector2 p = LinkProjection(a.x, travel, aspect), q = LinkProjection(a.y, travel, aspect);
                Vector2 r = LinkProjection(b.x, travel, aspect), s = LinkProjection(b.y, travel, aspect);
                if (LinkOrientation(p, q, r) * LinkOrientation(p, q, s) < -1e-9f
                    && LinkOrientation(r, s, p) * LinkOrientation(r, s, q) < -1e-9f) return true;
            }
            return false;
        }
        private static List<Vector2Int> GenerateEntryLinks(float aspect, uint seed, string previousSignature)
        {
            var candidates = new List<Vector2Int>();
            float maxLength = Mathf.Max(.68f, .45f * aspect);
            for (int a = 0; a < 27; a++)
                for (int b = a + 1; b < 27; b++)
                {
                    // Links stay within a depth layer so their endpoints share the same motion.
                    if (a / 9 != b / 9) continue;
                    float initialLength = Vector2.Distance(LinkProjection(a, 0, aspect), LinkProjection(b, 0, aspect));
                    float finalLength = Vector2.Distance(LinkProjection(a, 1, aspect), LinkProjection(b, 1, aspect));
                    if (initialLength >= .08f && finalLength <= maxLength) candidates.Add(new Vector2Int(a, b));
                }
            for (int attempt = 0; attempt < 64; attempt++)
            {
                var pool = new List<Vector2Int>(candidates);
                for (int i = pool.Count - 1; i > 0; i--)
                {
                    int other = (int)(NextLinkRandom(ref seed) % (uint)(i + 1));
                    Vector2Int temp = pool[i]; pool[i] = pool[other]; pool[other] = temp;
                }
                var chosen = new List<Vector2Int>(8); var degree = new int[27];
                foreach (Vector2Int pair in pool)
                {
                    if (degree[pair.x] >= 2 || degree[pair.y] >= 2) continue;
                    bool crosses = false;
                    foreach (Vector2Int existing in chosen) if (LinksCross(pair, existing, aspect)) { crosses = true; break; }
                    if (crosses) continue;
                    chosen.Add(pair); degree[pair.x]++; degree[pair.y]++;
                    if (chosen.Count == 8) break;
                }
                if (chosen.Count != 8) continue;
                var ids = new List<int>(8); foreach (Vector2Int pair in chosen) ids.Add(pair.x * 27 + pair.y); ids.Sort();
                if (string.Join(",", ids) != previousSignature) return chosen;
            }
            // An invalid layout must use the controller's immediate-map fallback, never a partial/duplicated pattern.
            throw new InvalidOperationException("A fresh set of eight short constellation links could not be generated.");
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
