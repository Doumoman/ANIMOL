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
            // World-space blank rows are independent of the HUD pixel insets.
            bounds.yMax += Mathf.Clamp(data.cameraSettings.topBlankRows, 0, 8);
            bounds.yMin -= Mathf.Clamp(data.cameraSettings.bottomBlankRows, 0, 8);
            return bounds;
        }
        public static float FullScreenRows(float preferredRows, float minimumWidth, float aspect)
        {
            return Mathf.Max(preferredRows, minimumWidth / Mathf.Max(.01f, aspect));
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
            // The HUD already handles the safe area. Reserving pixels in the world camera
            // exposes the camera clear color as a strip above the game on portrait screens.
            lens.rect = new Rect(0f, 0f, 1f, 1f);
            if (starway20)
            {
                var focus = StarwayCameraMath.FocusWorldSize(Vector2.one);
                lens.orthographicSize = FullScreenRows(focus.y, focus.x, lens.aspect) * .5f;
                return;
            }
            var rows = map.cameraSettings.fitWholeBoard
                ? Mathf.Max(map.Height + 1f, (map.Width + 1f) / Mathf.Max(.1f, lens.aspect))
                : Mathf.Clamp(map.cameraSettings.visibleRows, 5, 80);
            if (!map.cameraSettings.fitWholeBoard)
            {
                // Preserve at least the authored portrait view's horizontal span while
                // allowing taller and wider displays to reveal additional world area.
                var referenceHeight = 960f - Mathf.Clamp(map.cameraSettings.topInset, 0f, 150f)
                    - Mathf.Clamp(map.cameraSettings.bottomInset, 0f, 300f);
                rows = FullScreenRows(rows, rows * 540f / Mathf.Max(1f, referenceHeight), lens.aspect);
            }
            lens.orthographicSize = rows * .5f;
        }
        Vector2 Bounded(Vector2 p) => starway20
            ? StarwayCameraMath.ClampCenter(p, starwayBounds,
                new Vector2(lens.orthographicSize * 2f * lens.aspect, lens.orthographicSize * 2f))
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
