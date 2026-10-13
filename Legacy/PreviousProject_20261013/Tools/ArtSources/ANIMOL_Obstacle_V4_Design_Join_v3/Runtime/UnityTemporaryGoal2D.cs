using UnityEngine;

namespace Animol.QA
{
    [RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public sealed class UnityTemporaryGoal2D : MonoBehaviour
    {
        public UnityTemporaryRun2D Run;
        public Sprite LockedSprite, OpenSprite;
        public void SetOpen(bool open) { GetComponent<SpriteRenderer>().sprite = open ? OpenSprite : LockedSprite; }
        void OnTriggerEnter2D(Collider2D other) { Touch(other); }
        void OnTriggerStay2D(Collider2D other) { Touch(other); }
        void Touch(Collider2D other)
        {
            var player = other.GetComponentInParent<UnityTemporaryPlayer2D>();
            if (Run != null && player != null) Run.TryComplete(player);
        }
    }
}
