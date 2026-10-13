using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class MoonPhaseStairObject : OccupancyPlatformObject
    {
        private BoxCollider2D solid; private SpriteRenderer visual, alternatePreview; private MoonPhasePatternPlate patternPlate; private bool pending, latched;
        public bool IsVertical { get; private set; } public bool PreviewVisible { get; private set; }
        public SpriteRenderer PreviewRenderer => alternatePreview;
        public MoonPhasePatternPlate PatternPlate => patternPlate;
        public bool SweptSpaceBlocked => FindBlockingColliderInSweptSpace() != null;
        public string SweptSpaceBlockerName => FindBlockingColliderInSweptSpace()?.name ?? string.Empty;
        public override void Configure(StageMapObjectPlacement value, float unitsPerCell)
        {
            base.Configure(value, unitsPerCell); solid = GetComponent<BoxCollider2D>(); visual = GetComponentInChildren<SpriteRenderer>();
            GetComponent<AnimolOperationalArtBinding>()?.SetVariant(Mathf.Abs(settings.PhaseSeed) % 3);
            BuildAlternateShapePreview(); BuildPatternPlate(); ResetRuntimeState();
        }
        public bool RequestToggle()
        {
            if (latched) return false;
            latched = true; ShowPreview(true); PlayPhase("turn");
            if ((IsOccupied && settings.DeferWhileOccupied) || HasBlockingColliderInSweptSpace()) { pending = true; return true; }
            ToggleNow(); return true;
        }
        public void ReleasePatternPlate() => latched = false;
        private void FixedUpdate() { if (pending && !IsOccupied && !HasBlockingColliderInSweptSpace()) ToggleNow(); }
        private void ToggleNow()
        {
            pending = false; IsVertical = !IsVertical; ShowPreview(false);
            transform.rotation = Quaternion.Euler(0f, 0f, IsVertical ? settings.RotationDegrees : 0f);
            if (solid != null) solid.enabled = true;
            if (visual != null) { var color = visual.color; color.a = 1f; visual.color = color; }
            PlayPhase(IsVertical ? "active" : "recover");
        }
        private bool HasBlockingColliderInSweptSpace() => FindBlockingColliderInSweptSpace() != null;
        private Collider2D FindBlockingColliderInSweptSpace()
        {
            if (solid == null) return null;
            var bounds = solid.bounds; var side = Mathf.Max(bounds.size.x, bounds.size.y) * 1.02f;
            foreach (var hit in Physics2D.OverlapBoxAll(bounds.center, new Vector2(side, side), 0f))
            {
                if (hit == null || hit.isTrigger || hit.transform.IsChildOf(transform)) continue;
                // Other authored devices are checked by the editor layout validator; at runtime
                // this gate protects players and non-device terrain entering the rotation sweep.
                if (hit.GetComponentInParent<StageMapRuntimeObject>() != null) continue;
                return hit;
            }
            return null;
        }
        private void BuildAlternateShapePreview()
        {
            if (visual == null || alternatePreview != null) return;
            var previewObject = new GameObject("AlternateShapePreview"); previewObject.transform.SetParent(transform, false);
            previewObject.transform.localPosition = visual.transform.localPosition; previewObject.transform.localScale = visual.transform.localScale;
            alternatePreview = previewObject.AddComponent<SpriteRenderer>(); alternatePreview.sprite = visual.sprite;
            alternatePreview.drawMode = visual.drawMode; alternatePreview.size = visual.size; alternatePreview.material = visual.sharedMaterial;
            alternatePreview.sortingLayerID = visual.sortingLayerID; alternatePreview.sortingOrder = visual.sortingOrder + 1;
            var color = visual.color; color.a = .25f; alternatePreview.color = color; alternatePreview.enabled = false;
        }
        private void BuildPatternPlate()
        {
            if (patternPlate != null || solid == null) return;
            var plateObject = new GameObject("PatternPlate"); plateObject.transform.SetParent(transform.parent, true);
            plateObject.transform.position = transform.TransformPoint(new Vector3(
                solid.offset.x - solid.size.x * .5f - .5f, solid.offset.y - solid.size.y * .5f + .1f, 0f));
            var plateCollider = plateObject.AddComponent<BoxCollider2D>(); plateCollider.size = new Vector2(1f, .2f); plateCollider.isTrigger = true;
            patternPlate = plateObject.AddComponent<MoonPhasePatternPlate>(); patternPlate.Configure(this);
            if (visual == null) return;
            var cue = plateObject.AddComponent<SpriteRenderer>(); cue.sprite = visual.sprite; cue.material = visual.sharedMaterial;
            cue.sortingLayerID = visual.sortingLayerID; cue.sortingOrder = visual.sortingOrder + 2; cue.color = new Color(1f, .82f, .25f, .8f);
            cue.transform.localScale = new Vector3(.45f, .12f, 1f);
        }
        private void ShowPreview(bool visible)
        {
            PreviewVisible = visible;
            if (alternatePreview == null) return;
            alternatePreview.transform.localRotation = Quaternion.Euler(0f, 0f, IsVertical ? -settings.RotationDegrees : settings.RotationDegrees);
            alternatePreview.enabled = visible;
        }
        public override void ResetRuntimeState()
        {
            ClearOccupancy(); pending = false; latched = false; IsVertical = false; transform.position = initialPosition; transform.rotation = Quaternion.identity;
            ShowPreview(false);
            PlayIdlePhase();
        }
    }
}
