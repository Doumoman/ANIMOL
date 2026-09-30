using UnityEngine;

namespace ANIMOL.Gameplay
{
    public static class SolidificationSafety
    {
        public static bool IsClear(Collider2D candidate, Transform owner)
        {
            if (candidate == null) return false;
            var bounds = candidate.bounds;
            var center = (Vector2)bounds.center;
            var size = (Vector2)bounds.size;
            if (candidate is BoxCollider2D box)
            {
                center = box.transform.TransformPoint(box.offset);
                var scale = box.transform.lossyScale;
                size = new Vector2(Mathf.Abs(box.size.x * scale.x), Mathf.Abs(box.size.y * scale.y));
            }
            foreach (var hit in Physics2D.OverlapBoxAll(center, size * .88f, candidate.transform.eulerAngles.z))
            {
                if (hit == null || hit == candidate || hit.isTrigger || hit.transform.IsChildOf(owner)) continue;
                return false;
            }
            return true;
        }
    }
}
