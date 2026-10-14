using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ANIMOL.Puzzle
{
    public sealed class PuzzleSwipeInput : MonoBehaviour
    {
        [Range(.02f, .2f)] public float minimumScreenWidthFraction = .06f;
        PuzzleController controller;
        Vector2 start;
        bool tracking, multiTouch;
        int finger;
        readonly List<RaycastResult> hits = new List<RaycastResult>();
        public static Vector2Int Resolve(Vector2 delta, float screenWidth, float threshold = .06f)
        {
            if (Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) < Mathf.Max(24, screenWidth * threshold)) return Vector2Int.zero;
            return Mathf.Abs(delta.x) > Mathf.Abs(delta.y) ? new Vector2Int(delta.x > 0 ? 1 : -1, 0) : new Vector2Int(0, delta.y > 0 ? 1 : -1);
        }
        public bool OverUI(Vector2 point)
        {
            if (!EventSystem.current) return false;
            hits.Clear(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits); return hits.Count > 0;
        }
        public void BeginGesture(Vector2 point, bool startsOnUI)
        { start = point; tracking = !startsOnUI; }
        public bool EndGesture(Vector2 point)
        {
            if (!tracking) return false; tracking = false;
            if (!controller) controller = GetComponent<PuzzleController>();
            var direction = Resolve(point - start, Screen.width, minimumScreenWidthFraction);
            return direction != Vector2Int.zero && controller && controller.RequestMove(direction);
        }
        public void CancelGesture() { tracking = false; }
        void Update()
        {
            if (Input.touchCount > 1) { multiTouch = true; CancelGesture(); return; }
            if (multiTouch) { if (Input.touchCount == 0) multiTouch = false; return; }
            if (Input.touchCount == 1)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began) { finger = touch.fingerId; BeginGesture(touch.position, OverUI(touch.position)); }
                else if (touch.fingerId == finger && touch.phase == TouchPhase.Ended) EndGesture(touch.position);
                else if (touch.phase == TouchPhase.Canceled) CancelGesture();
                return;
            }
            // Mouse drags exercise exactly the same swipe path in the Editor/Game View.
            if (Input.GetMouseButtonDown(0)) BeginGesture(Input.mousePosition, OverUI(Input.mousePosition));
            if (Input.GetMouseButtonUp(0)) EndGesture(Input.mousePosition);
        }
        void OnDisable() { CancelGesture(); multiTouch = false; }
    }
}
