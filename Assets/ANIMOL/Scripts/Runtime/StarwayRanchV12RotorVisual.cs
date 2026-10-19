using UnityEngine;

namespace ANIMOL.Art.V12
{
    // Attach to a stationary visual owner. Only a strict DESCENDANT blade Transform may rotate.
    // Its local origin must already be at the authored hub. Never point this at gameplay root/body.
    [DisallowMultipleComponent]
    public sealed class StarwayRanchV12RotorVisual : MonoBehaviour
    {
        [SerializeField] private Transform rotor;
        [SerializeField] private float degreesPerSecond = 18f;
        private bool paused, reducedMotion, valid;
        private Quaternion baseline;
        private float angle;

        private void Start() { if (!valid) ValidateRotor(); }
        public bool Configure(Transform bladeChild, float angularSpeed)
        { rotor = bladeChild; degreesPerSecond = angularSpeed; return ValidateRotor(); }
        public void SetPaused(bool value) { paused = value; }
        public void SetReducedMotion(bool value) { reducedMotion = value; }
        public void Rebase() { if (valid) { baseline = rotor.localRotation; angle = 0; } }

        private bool ValidateRotor()
        {
            valid = rotor != null && rotor != transform && rotor.IsChildOf(transform)
                && rotor.GetComponent<SpriteRenderer>() != null
                && rotor.GetComponentsInChildren<Collider2D>(true).Length == 0
                && rotor.GetComponentsInChildren<Rigidbody2D>(true).Length == 0
                && rotor.GetComponentsInChildren<Collider>(true).Length == 0
                && rotor.GetComponentsInChildren<Rigidbody>(true).Length == 0
                && StarwayRanchV12VisualCatalog.Finite(degreesPerSecond);
            if (!valid)
            {
                Debug.LogError("[Starway Ranch V12] Rotor must be a strict visual descendant with finite speed; root/body rotation is forbidden.", this);
                return false;
            }
            baseline = rotor.localRotation; angle = 0;
            return true;
        }
        private void Update()
        {
            if (!valid || paused || reducedMotion || Time.deltaTime <= 0) return;
            angle = Mathf.Repeat(angle + degreesPerSecond * Time.deltaTime, 360f);
            rotor.localRotation = baseline * Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }
}
