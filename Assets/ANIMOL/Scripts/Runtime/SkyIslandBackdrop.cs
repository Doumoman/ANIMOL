using UnityEngine;

namespace ANIMOL.Puzzle
{
    // Native 80px world raster + point-sampled integer display. No gameplay camera/actor transform writes.
    [DefaultExecutionOrder(220)]
    public sealed class SkyIslandBackdrop : MonoBehaviour
    {
        const int BackgroundLayer = 30, ForegroundDisplayLayer = 29, BackgroundDisplayLayer = 28;
        public bool overrideSample;
        public Vector2 sampleCameraPixels;
        public bool backgroundOnly, hideBackground;
        public int IntegerScale { get; private set; }
        public RenderTexture ForegroundTexture { get; private set; }
        public RenderTexture BackgroundTexture { get; private set; }
        public Vector2Int[] Phases { get; } = new Vector2Int[3];
        Camera gameplay, foregroundCapture, backgroundCapture, foregroundDisplay, backgroundDisplay;
        Sprite farSprite, midSprite, nearSprite;
        Material compositor;
        Transform[] layers;
        SpriteRenderer[][] copies;
        Renderer foregroundQuad, backgroundQuad;
        int originalMask;
        CameraClearFlags originalClear;
        Vector2Int foregroundSize, backgroundSize;
        public static readonly float[] Parallax = { .15f, .35f, .65f };
        public Vector3 ParallaxFactors { get; private set; } = new Vector3(.15f, .35f, .65f);
        public static int FloorMod(int value, int period) => value - Mathf.FloorToInt((float)value / period) * period;
        public static Vector2Int Phase(Vector2 cameraPixels, float factor) => new Vector2Int(FloorMod(Mathf.RoundToInt(cameraPixels.x * factor), 960), FloorMod(Mathf.RoundToInt(cameraPixels.y * factor), 960));
        public void Configure(SkyIslandArt source, Camera camera)
            => Configure(source.far, source.mid, source.near, source.compositorMaterial, camera);
        public void Configure(Sprite far, Sprite mid, Sprite near, Material material, Camera camera, Vector3? parallax = null)
        {
            ParallaxFactors = parallax ?? new Vector3(.15f, .35f, .65f);
            farSprite = far; midSprite = mid; nearSprite = near; compositor = material;
            gameplay = camera; originalMask = camera.cullingMask; originalClear = camera.clearFlags;
            foregroundCapture = Camera("Native foreground capture", camera.depth - 8, 0, false);
            backgroundCapture = Camera("Native background capture", camera.depth - 10, 1 << BackgroundLayer, false);
            foregroundDisplay = Camera("Integer foreground display", camera.depth + 1, 1 << ForegroundDisplayLayer, true);
            backgroundDisplay = Camera("Integer background display", camera.depth - 2, 1 << BackgroundDisplayLayer, true);
            foregroundDisplay.clearFlags = CameraClearFlags.Depth;
            backgroundDisplay.clearFlags = CameraClearFlags.SolidColor; backgroundDisplay.backgroundColor = new Color32(0x1A, 0x1C, 0x2C, 255);
            foregroundQuad = Quad(foregroundDisplay.transform, ForegroundDisplayLayer);
            backgroundQuad = Quad(backgroundDisplay.transform, BackgroundDisplayLayer);
            layers = new Transform[3]; copies = new SpriteRenderer[3][];
            for (var layer = 0; layer < 3; layer++)
            {
                layers[layer] = new GameObject(new[] { "Far", "Mid", "Near" }[layer]).transform; layers[layer].SetParent(transform, false);
                copies[layer] = new SpriteRenderer[0];
            }
            LateUpdate();
        }
        Camera Camera(string name, float depth, int mask, bool display)
        {
            var root = new GameObject(name); root.transform.SetParent(transform, false); root.transform.localPosition = new Vector3(0, 0, -10);
            var camera = root.AddComponent<Camera>(); camera.orthographic = true; camera.depth = depth; camera.cullingMask = mask;
            camera.allowHDR = false; camera.allowMSAA = false; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear;
            if (display) camera.orthographicSize = .5f;
            return camera;
        }
        Renderer Quad(Transform camera, int layer)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.name = "Point sampled presentation"; quad.layer = layer;
            quad.transform.SetParent(camera, false); quad.transform.localPosition = new Vector3(0, 0, 10);
            var collider = quad.GetComponent<Collider>(); if (Application.isPlaying) Destroy(collider); else DestroyImmediate(collider);
            var renderer = quad.GetComponent<Renderer>(); renderer.sharedMaterial = compositor ? new Material(compositor) : new Material(Shader.Find("ANIMOL/SkyIslandComposite")); return renderer;
        }
        public static int ChooseScale(float viewportWidth, float viewportHeight, int nativeWidth, int nativeHeight)
        {
            var fit = Mathf.Min(viewportWidth / nativeWidth, viewportHeight / nativeHeight);
            var lower = Mathf.Max(1, Mathf.FloorToInt(fit)); var upper = lower + 1;
            // At 1080x1920 the authored 11 rows exceed the viewport by just 20px at 2x.
            // Allow at most 5% border crop, otherwise keep the whole field with background margins.
            return nativeWidth * upper <= viewportWidth * 1.05f && nativeHeight * upper <= viewportHeight * 1.05f ? upper : lower;
        }
        void LateUpdate()
        {
            if (!gameplay || !farSprite) return;
            var rect = gameplay.pixelRect;
            var h = Mathf.Max(1, Mathf.RoundToInt(gameplay.orthographicSize * 160));
            var w = Mathf.Max(1, Mathf.RoundToInt(h * gameplay.aspect));
            IntegerScale = ChooseScale(rect.width, rect.height, w, h);
            var fg = new Vector2Int(w, h); var bg = new Vector2Int(Mathf.CeilToInt((float)Screen.width / IntegerScale), Mathf.CeilToInt((float)Screen.height / IntegerScale));
            if (!ForegroundTexture || fg != foregroundSize || bg != backgroundSize)
            {
                Release(); foregroundSize = fg; backgroundSize = bg;
                ForegroundTexture = Texture(fg); BackgroundTexture = Texture(bg);
                foregroundQuad.sharedMaterial.mainTexture = ForegroundTexture; backgroundQuad.sharedMaterial.mainTexture = BackgroundTexture;
                foregroundCapture.targetTexture = ForegroundTexture; backgroundCapture.targetTexture = BackgroundTexture;
                RebuildCopies();
            }
            foregroundCapture.CopyFrom(gameplay); foregroundCapture.tag = "Untagged";
            foregroundCapture.depth = gameplay.depth - 8; foregroundCapture.rect = new Rect(0, 0, 1, 1);
            foregroundCapture.targetTexture = ForegroundTexture; foregroundCapture.orthographic = true; foregroundCapture.orthographicSize = h / 160f;
            foregroundCapture.aspect = (float)w / h; foregroundCapture.allowHDR = false; foregroundCapture.allowMSAA = false;
            foregroundCapture.cullingMask = originalMask & ~((1 << BackgroundLayer) | (1 << BackgroundDisplayLayer) | (1 << ForegroundDisplayLayer));
            foregroundCapture.clearFlags = CameraClearFlags.SolidColor; foregroundCapture.backgroundColor = Color.clear;
            foregroundCapture.transform.SetPositionAndRotation(new Vector3(Mathf.Round(gameplay.transform.position.x * 80) / 80, Mathf.Round(gameplay.transform.position.y * 80) / 80, gameplay.transform.position.z), gameplay.transform.rotation);
            backgroundCapture.orthographicSize = bg.y / 160f; backgroundCapture.aspect = (float)bg.x / bg.y;
            foregroundDisplay.rect = gameplay.rect; foregroundDisplay.depth = gameplay.depth + 1;
            foregroundQuad.transform.localScale = new Vector3(w * IntegerScale / rect.height, h * IntegerScale / rect.height, 1);
            backgroundQuad.transform.localScale = new Vector3(bg.x * IntegerScale / (float)Screen.height, bg.y * IntegerScale / (float)Screen.height, 1);
            gameplay.cullingMask = 0; gameplay.clearFlags = CameraClearFlags.Nothing;
            var sample = overrideSample ? sampleCameraPixels : new Vector2(gameplay.transform.position.x * 80, -gameplay.transform.position.y * 80);
            for (var layer = 0; layer < 3; layer++)
            {
                var phase = Phase(sample, ParallaxFactors[layer]); Phases[layer] = phase;
                var index = 0;
                for (var y = -1; y <= Mathf.CeilToInt(bg.y / 960f) + 1; y++) for (var x = -1; x <= Mathf.CeilToInt(bg.x / 960f) + 1; x++)
                    copies[layer][index++].transform.localPosition = new Vector3((-bg.x * .5f - phase.x + 480 + x * 960) / 80f, (bg.y * .5f + phase.y - 480 - y * 960) / 80f, 0);
            }
            foregroundCapture.enabled = !backgroundOnly; foregroundDisplay.enabled = !backgroundOnly;
            backgroundCapture.enabled = !hideBackground; backgroundQuad.enabled = !hideBackground;
            backgroundDisplay.backgroundColor = hideBackground ? Color.black : new Color32(0x1A, 0x1C, 0x2C, 255);
        }
        RenderTexture Texture(Vector2Int size)
        { var texture = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point, antiAliasing = 1, wrapMode = TextureWrapMode.Clamp }; texture.Create(); return texture; }
        void RebuildCopies()
        {
            for (var layer = 0; layer < 3; layer++)
            {
                foreach (var old in copies[layer]) { old.gameObject.SetActive(false); if (Application.isPlaying) Destroy(old.gameObject); else DestroyImmediate(old.gameObject); }
                var columns = Mathf.CeilToInt(backgroundSize.x / 960f) + 3; var rows = Mathf.CeilToInt(backgroundSize.y / 960f) + 3;
                copies[layer] = new SpriteRenderer[columns * rows];
                for (var i = 0; i < copies[layer].Length; i++)
                {
                    var root = new GameObject("Repeat 960px"); root.layer = BackgroundLayer; root.transform.SetParent(layers[layer], false);
                    var image = root.AddComponent<SpriteRenderer>(); image.sprite = layer == 0 ? farSprite : layer == 1 ? midSprite : nearSprite; image.color = Color.white; image.sortingOrder = -100 + layer * 10;
                    copies[layer][i] = image;
                }
            }
        }
        void Release()
        {
            if (ForegroundTexture) { ForegroundTexture.Release(); if (Application.isPlaying) Destroy(ForegroundTexture); else DestroyImmediate(ForegroundTexture); }
            if (BackgroundTexture) { BackgroundTexture.Release(); if (Application.isPlaying) Destroy(BackgroundTexture); else DestroyImmediate(BackgroundTexture); }
        }
        void OnDisable() { if (gameplay) { gameplay.cullingMask = originalMask; gameplay.clearFlags = originalClear; } }
        void OnDestroy() { Release(); if (foregroundQuad) Destroy(foregroundQuad.sharedMaterial); if (backgroundQuad) Destroy(backgroundQuad.sharedMaterial); }
    }
}
