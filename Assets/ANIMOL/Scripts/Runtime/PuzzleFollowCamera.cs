using System;
using System.Linq;
using ANIMOL.StarwayMaps;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    [DefaultExecutionOrder(100), RequireComponent(typeof(Camera))]
    public sealed class PuzzleFollowCamera : MonoBehaviour
    {
        public Transform player;
        public PuzzleMap map;
        Camera lens;
        bool starway20;
        Rect starwayBounds;
        public static Vector2 Clamp(Vector2 target, int width, int height, float halfHeight, float aspect)
        {
            var halfWidth = halfHeight * aspect;
            return new Vector2(width <= halfWidth * 2 ? (width - 1) * .5f : Mathf.Clamp(target.x, halfWidth - .5f, width - .5f - halfWidth),
                height <= halfHeight * 2 ? (height - 1) * .5f : Mathf.Clamp(target.y, halfHeight - .5f, height - .5f - halfHeight));
        }
        public static Rect StarwayFollowBounds(PuzzleMap data)
        {
            var bounds = StarwayCameraMath.CellBounds(data.terrain.Select(t =>
                new StarwayCell(t.position.x, t.position.y)).ToArray(), Vector2.zero, Vector2.one);
            // World-space headroom is independent of the HUD pixel inset.
            bounds.yMax += Mathf.Clamp(data.cameraSettings.topBlankRows, 0, 8);
            return bounds;
        }
        public void Configure(PuzzleMap data, Transform target)
        {
            map = data; player = target; lens = GetComponent<Camera>(); lens.orthographic = true;
            starway20 = data.scenario && data.scenario.themeId == "starway" &&
                data.name.StartsWith("SW", StringComparison.Ordinal) && data.terrain.Count > 0;
            if (starway20)
                starwayBounds = StarwayFollowBounds(data);
            ApplyLens(); Cut();
        }
        void ApplyLens()
        {
            var safe = Screen.safeArea; var scale = Mathf.Min(Screen.width / 540f, Screen.height / 960f);
            var top = Mathf.Min(map.cameraSettings.topInset * scale, safe.height * .25f);
            var bottom = Mathf.Min(map.cameraSettings.bottomInset * scale, safe.height * .4f);
            if (starway20)
            {
                var available = new Rect(safe.x, safe.y + bottom, safe.width, safe.height - top - bottom);
                var focus = StarwayCameraMath.FocusWorldSize(Vector2.one);
                lens.rect = StarwayCameraMath.NormalizePixels(
                    StarwayCameraMath.FitInsidePixels(available, focus.x / focus.y), Screen.width, Screen.height);
                lens.orthographicSize = focus.y * .5f;
                return;
            }
            lens.rect = new Rect(safe.x / Screen.width, (safe.y + bottom) / Screen.height, safe.width / Screen.width, (safe.height - top - bottom) / Screen.height);
            var rows = map.cameraSettings.fitWholeBoard
                ? Mathf.Max(map.Height + 1f, (map.Width + 1f) / Mathf.Max(.1f, lens.aspect))
                : Mathf.Clamp(map.cameraSettings.visibleRows, 5, 80);
            lens.orthographicSize = rows * .5f;
        }
        Vector2 Bounded(Vector2 p) => starway20
            ? StarwayCameraMath.ClampCenter(p, starwayBounds, StarwayCameraMath.FocusWorldSize(Vector2.one))
            : Clamp(p, map.Width, map.Height, lens.orthographicSize, lens.aspect);
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
