using UnityEngine;

namespace Animol
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public sealed class AnimolPartColliderFollower : MonoBehaviour
    {
        [SerializeField] private bool oneWay;
        private SpriteRenderer spriteRenderer;
        private BoxCollider2D boxCollider;

        public bool OneWay => oneWay;

        private void Awake()
        {
            Cache();
            SyncNow();
        }

        private void LateUpdate() => SyncNow();

        public void Configure(bool useOneWay)
        {
            oneWay = useOneWay;
            Cache();
            boxCollider.usedByEffector = oneWay;
            if (oneWay && GetComponent<PlatformEffector2D>() == null)
                gameObject.AddComponent<PlatformEffector2D>();
            SyncNow();
        }

        public void SyncNow()
        {
            Cache();
            var sprite = spriteRenderer.sprite;
            if (sprite == null) return;
            boxCollider.offset = sprite.bounds.center;
            boxCollider.size = sprite.bounds.size;
            boxCollider.usedByEffector = oneWay;
        }

        private void Cache()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
        }
    }
}
