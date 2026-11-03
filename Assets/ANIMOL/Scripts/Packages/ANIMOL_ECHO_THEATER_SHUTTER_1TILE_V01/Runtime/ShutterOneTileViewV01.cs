using UnityEngine;

namespace ANIMOL.Presentation.ShutterOneTileV01
{
    // Presentation only. The host owns alternation, occupied-cell rules, collision and turns.
    [DisallowMultipleComponent]
    public sealed class ShutterOneTileViewV01 : MonoBehaviour
    {
        ShutterOneTileArtV01 art;
        SpriteRenderer target;
        Transform anchor;
        float cellSize;
        public bool Configure(ShutterOneTileArtV01 artSet, SpriteRenderer visualChild, Transform actualCellAnchor,
            float actualCellWorldSize, out string error)
        {
            error = null;
            if (artSet == null || visualChild == null || actualCellAnchor == null || !(actualCellWorldSize > 0)
                || float.IsInfinity(actualCellWorldSize))
            { error = "Assign art, a visual-only renderer, cell anchor and finite world cell size."; return false; }
            if (!artSet.Validate(out error)) return false;
            if (visualChild.transform == actualCellAnchor || actualCellAnchor.IsChildOf(visualChild.transform)
                || visualChild.GetComponent<Collider2D>() != null || visualChild.GetComponent<Rigidbody2D>() != null)
            { error = "Use a separate visual child. Do not scale the cell anchor or a physics object."; return false; }
            art = artSet; target = visualChild; anchor = actualCellAnchor; cellSize = actualCellWorldSize;
            return true;
        }
        public bool ShowState(bool isVertical, bool isOpen)
        { return ShowOpenProgress(isVertical, isOpen ? 1 : 0); }
        // Normalized OPEN amount: 0=closed and 1=open. Host supplies reverse progress when closing.
        public bool ShowOpenProgress(bool isVertical, float openProgress)
        {
            if (art == null || target == null || anchor == null || float.IsNaN(openProgress) || float.IsInfinity(openProgress)) return false;
            Sprite[] frames = isVertical ? art.vertical : art.horizontal;
            int i = Mathf.Min(3, Mathf.FloorToInt(Mathf.Clamp01(openProgress) * 4));
            target.sprite = frames[i]; target.drawMode = SpriteDrawMode.Simple;
            Transform t = target.transform; t.position = anchor.position; t.rotation = Quaternion.identity; t.localScale = Vector3.one;
            float x = t.TransformVector(Vector3.right).magnitude, y = t.TransformVector(Vector3.up).magnitude;
            if (x < .00001f || y < .00001f) return false;
            // Absolute placement from a one-unit canvas; never compound an existing scale.
            t.localScale = new Vector3(cellSize / x, cellSize / y, 1);
            return true;
        }
    }
}
