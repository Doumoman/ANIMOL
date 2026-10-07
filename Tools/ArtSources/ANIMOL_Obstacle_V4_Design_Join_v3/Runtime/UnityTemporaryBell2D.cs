using UnityEngine;

namespace Animol.QA
{
    [RequireComponent(typeof(CircleCollider2D), typeof(SpriteRenderer))]
    public sealed class UnityTemporaryBell2D : MonoBehaviour
    {
        public int BellId;
        public UnityTemporaryRun2D Run;
        bool broken;
        void OnTriggerEnter2D(Collider2D other) { Touch(other); }
        void OnTriggerStay2D(Collider2D other) { Touch(other); }
        void Touch(Collider2D other)
        {
            var player = other.GetComponentInParent<UnityTemporaryPlayer2D>();
            if (!broken && Run != null && player != null) Run.TryBreakBell(this, player);
        }
        public void HideBroken()
        {
            broken = true; GetComponent<SpriteRenderer>().enabled = false;
            GetComponent<CircleCollider2D>().enabled = false;
        }
        public void ResetBell()
        {
            broken = false; GetComponent<SpriteRenderer>().enabled = true;
            GetComponent<CircleCollider2D>().enabled = true;
        }
    }
}
