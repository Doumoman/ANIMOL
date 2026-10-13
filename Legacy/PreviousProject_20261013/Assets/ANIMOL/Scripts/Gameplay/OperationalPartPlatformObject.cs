using System.Linq;
using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    public abstract class OperationalPartPlatformObject : OccupancyPlatformObject
    {
        protected BoxCollider2D[] parts;
        protected void InitializeParts()
        {
            var root = transform.Find("OperationalPhysicsParts");
            parts = root == null ? System.Array.Empty<BoxCollider2D>() : root.GetComponentsInChildren<BoxCollider2D>(true)
                .OrderBy(x => x.transform.localPosition.x).ToArray();
            foreach (var part in parts) part.isTrigger = false;
        }
        protected bool TryEnablePart(int index)
        {
            if (index < 0 || index >= parts.Length) return false;
            if (!SolidificationSafety.IsClear(parts[index], transform)) return false;
            parts[index].enabled = true; return true;
        }
        protected void SetAllParts(bool enabled) { foreach (var part in parts) part.enabled = enabled; }
        public int EnabledPartCount => parts?.Count(x => x.enabled) ?? 0;
        public int PartCount => parts?.Length ?? 0;
    }
}
