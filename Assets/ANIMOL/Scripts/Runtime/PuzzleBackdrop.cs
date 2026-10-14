using UnityEngine;

namespace ANIMOL.Puzzle
{
    // An independent screen composition. Map size never stretches or pans this backdrop.
    [DefaultExecutionOrder(150)]
    public sealed class PuzzleBackdrop : MonoBehaviour
    {
        public const int BackgroundLayer = 30;
        Camera backgroundCamera, gameplayCamera;
        PuzzleTheme theme;
        Transform far, mid, near;
        SpriteRenderer farImage;
        Vector2 lastSize;
        public static float UniformCover(Vector2 frame, Vector2 source) => Mathf.Max(frame.x / source.x, frame.y / source.y);
        public void Configure(PuzzleTheme data, Camera gameplay, PuzzleCameraSettings settings)
        {
            theme = data; gameplayCamera = gameplay;
            var cameraObject = new GameObject("Background Camera"); cameraObject.transform.SetParent(transform, false); cameraObject.transform.localPosition = new Vector3(0, 0, -10);
            backgroundCamera = cameraObject.AddComponent<Camera>(); backgroundCamera.orthographic = true;
            backgroundCamera.orthographicSize = theme.far ? theme.far.bounds.size.y * .5f : 10.36875f;
            backgroundCamera.cullingMask = 1 << BackgroundLayer; backgroundCamera.depth = gameplay.depth - 1;
            backgroundCamera.backgroundColor = theme.background; backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
            gameplay.cullingMask &= ~(1 << BackgroundLayer); gameplay.clearFlags = CameraClearFlags.Depth;
            far = Layer("Far", settings.farParallax); mid = Layer("Mid", settings.midParallax); near = Layer("Near", settings.nearParallax);
            farImage = Native(far, "Original portrait backdrop", theme.far, Vector2.zero, -100);
            Native(mid, "Cloud left", theme.mid, new Vector2(-.37f, .22f), -70);
            Native(mid, "Cloud right", theme.mid, new Vector2(.36f, -.25f), -70);
            Native(near, "Star left", theme.near, new Vector2(-.41f, -.05f), -40);
            Native(near, "Star right", theme.near, new Vector2(.39f, .36f), -40);
            FitFrame();
        }
        Transform Layer(string name, float amount)
        {
            var root = new GameObject(name).transform; root.SetParent(transform, false);
            root.gameObject.AddComponent<PuzzleParallax>().Initialize(gameplayCamera.transform, amount, FrameSize()); return root;
        }
        SpriteRenderer Native(Transform parent, string name, Sprite sprite, Vector2 anchor, int order)
        {
            var root = new GameObject(name); root.layer = BackgroundLayer; root.transform.SetParent(parent, false);
            var renderer = root.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = order;
            // Store screen anchors without normalizing the sprite to a square.
            root.transform.localPosition = Vector2.Scale(anchor, FrameSize()); return renderer;
        }
        Vector2 FrameSize() => new Vector2(backgroundCamera.orthographicSize * 2 * backgroundCamera.aspect, backgroundCamera.orthographicSize * 2);
        void LateUpdate() { if (backgroundCamera) FitFrame(); }
        void FitFrame()
        {
            var size = FrameSize(); if (size == lastSize) return; lastSize = size;
            if (theme.far) farImage.transform.localScale = Vector3.one * (UniformCover(size, theme.far.bounds.size) * 1.04f);
            // Native dimensions/PPU and a single uniform scalar preserve all source aspect ratios.
            foreach (var layer in new[] { far, mid, near }) layer.GetComponent<PuzzleParallax>().frameSize = size;
            mid.GetChild(0).localPosition = Vector2.Scale(new Vector2(-.37f, .22f), size);
            mid.GetChild(1).localPosition = Vector2.Scale(new Vector2(.36f, -.25f), size);
            near.GetChild(0).localPosition = Vector2.Scale(new Vector2(-.41f, -.05f), size);
            near.GetChild(1).localPosition = Vector2.Scale(new Vector2(.39f, .36f), size);
        }
    }
}
