using UnityEngine;

namespace ANIMOL.StarwayMaps
{
    // Optional standalone camera. Use ONLY when the project has no existing camera controller.
    // Never auto-attached by this pack. With DirectionalCameraFollow/Cinemachine, reuse the math instead.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class StarwayGridCamera : MonoBehaviour
    {
        [SerializeField, Min(.01f)] private float followSharpness = 16f;
        [SerializeField] private bool respectSafeArea = true;
        private Camera view;
        private Transform followed;
        private Vector2 focusSize;
        private Rect bounds;
        private Rect gameplayPixels;
        private bool hasGameplayRect;
        private bool bound;
        private bool captured;
        private bool previousOrthographic;
        private float previousSize;
        private float previousAspect;
        private Rect previousRect;

        public Rect FocusBounds { get { return bounds; } }
        public Vector2 FocusWorldSize { get { return focusSize; } }

        private void OnEnable()
        {
            view = GetComponent<Camera>();
            previousOrthographic = view.orthographic; previousSize = view.orthographicSize;
            previousAspect = view.aspect; previousRect = view.rect; captured = true;
        }

        // Caller provides actual world grid metrics, not sprite pixels or PPU guesses.
        public void Bind(Transform target, StarwayCell[] region, Vector2 cellZeroCenter, Vector2 worldUnitsPerCell)
        {
            if (target == null) throw new System.ArgumentNullException("target");
            if (view == null) view = GetComponent<Camera>();
            if (view.targetTexture != null) throw new System.InvalidOperationException("Use the existing RenderTexture camera pipeline; do not attach this fallback.");
            followed = target;
            focusSize = StarwayCameraMath.FocusWorldSize(worldUnitsPerCell, 7, 11);
            bounds = StarwayCameraMath.CellBounds(region, cellZeroCenter, worldUnitsPerCell);
            bound = true;
            if (isActiveAndEnabled) { ApplyProjection(); SnapToTarget(); }
        }

        // Recompute when the UI layout/screen orientation changes. Pixel origin is bottom left.
        // Exclude top/bottom HUD; provide the real gameplay area, not the full Canvas rect.
        public void SetGameplayViewportPixels(Rect pixels)
        {
            if (pixels.width <= 0f || pixels.height <= 0f) throw new System.ArgumentException("Empty gameplay viewport.");
            gameplayPixels = pixels; hasGameplayRect = true;
        }

        public void ClearGameplayViewportOverride() { hasGameplayRect = false; }
        public void Unbind() { bound = false; followed = null; }

        public void SnapToTarget()
        {
            if (!bound || followed == null) return;
            Vector2 p = StarwayCameraMath.ClampCenter(followed.position, bounds, focusSize);
            view.transform.position = new Vector3(p.x, p.y, view.transform.position.z);
        }

        private void LateUpdate()
        {
            if (!bound || followed == null) return;
            ApplyProjection();
            Vector2 desired = StarwayCameraMath.ClampCenter(followed.position, bounds, focusSize);
            float t = 1f - Mathf.Exp(-Mathf.Max(.01f, followSharpness) * Time.unscaledDeltaTime);
            Vector2 p = Vector2.Lerp(view.transform.position, desired, t);
            p = StarwayCameraMath.ClampCenter(p, bounds, focusSize);
            view.transform.position = new Vector3(p.x, p.y, view.transform.position.z);
        }

        private void ApplyProjection()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            Rect screen = new Rect(0, 0, Screen.width, Screen.height);
            Rect available = hasGameplayRect ? StarwayCameraMath.Intersect(gameplayPixels, screen) : screen;
            if (respectSafeArea) available = StarwayCameraMath.Intersect(available, Screen.safeArea);
            float aspect = focusSize.x / focusSize.y;
            Rect pixels = StarwayCameraMath.FitInsidePixels(available, aspect);
            view.orthographic = true;
            view.rect = StarwayCameraMath.NormalizePixels(pixels, Screen.width, Screen.height);
            view.aspect = aspect;
            view.orthographicSize = focusSize.y * .5f;
        }

        private void OnDisable()
        {
            if (captured && view != null)
            {
                view.orthographic = previousOrthographic; view.orthographicSize = previousSize;
                view.rect = previousRect; view.aspect = previousAspect;
            }
            captured = false;
        }
    }
}
