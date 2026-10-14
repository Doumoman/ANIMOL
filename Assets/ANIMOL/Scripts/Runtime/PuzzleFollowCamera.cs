using UnityEngine;

namespace ANIMOL.Puzzle
{
    [DefaultExecutionOrder(100), RequireComponent(typeof(Camera))]
    public sealed class PuzzleFollowCamera : MonoBehaviour
    {
        public Transform player;
        public PuzzleMap map;
        Camera lens;
        public static Vector2 Clamp(Vector2 target, int width, int height, float halfHeight, float aspect)
        {
            var halfWidth = halfHeight * aspect;
            return new Vector2(width <= halfWidth * 2 ? (width - 1) * .5f : Mathf.Clamp(target.x, halfWidth - .5f, width - .5f - halfWidth),
                height <= halfHeight * 2 ? (height - 1) * .5f : Mathf.Clamp(target.y, halfHeight - .5f, height - .5f - halfHeight));
        }
        public void Configure(PuzzleMap data, Transform target)
        { map = data; player = target; lens = GetComponent<Camera>(); lens.orthographic = true; ApplyLens(); Cut(); }
        void ApplyLens()
        {
            lens.orthographicSize = Mathf.Clamp(map.cameraSettings.visibleRows, 5, 25) * .5f;
            var safe = Screen.safeArea; var scale = Mathf.Min(Screen.width / 540f, Screen.height / 960f);
            var top = Mathf.Min(map.cameraSettings.topInset * scale, safe.height * .25f);
            var bottom = Mathf.Min(map.cameraSettings.bottomInset * scale, safe.height * .4f);
            lens.rect = new Rect(safe.x / Screen.width, (safe.y + bottom) / Screen.height, safe.width / Screen.width, (safe.height - top - bottom) / Screen.height);
        }
        Vector2 Bounded(Vector2 p) => Clamp(p, map.Width, map.Height, lens.orthographicSize, lens.aspect);
        public void Cut()
        { if (!lens || !map || !player) return; ApplyLens(); var p = Bounded(player.position); transform.position = new Vector3(p.x, p.y, -10); }
        void LateUpdate()
        {
            if (!map || !player) return;
            ApplyLens();
            var target = Bounded(player.position);
            var next = Vector2.Lerp(transform.position, target, 1 - Mathf.Exp(-Mathf.Max(1, map.cameraSettings.followSharpness) * Time.deltaTime));
            next = Bounded(next); transform.position = new Vector3(next.x, next.y, -10);
        }
    }
}
