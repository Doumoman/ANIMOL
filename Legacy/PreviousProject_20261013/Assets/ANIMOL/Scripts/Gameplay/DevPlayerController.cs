using System.Collections.Generic;
using ANIMOL.Core;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class DevPlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5.5f;
        [SerializeField] private float jumpVelocity = 8.5f;
        [SerializeField] private float stamina = 100f;

        private readonly Dictionary<int, float> touchDirections = new Dictionary<int, float>();
        private Rigidbody2D body;
        private CampaignRosterSnapshot roster;
        private bool jumpRequested;

        public static DevPlayerController Instance { get; private set; }
        public float Stamina => stamina;
        public string CurrentAnimalId { get; private set; } = "DEV_GROUND";

        private void Awake()
        {
            Instance = this;
            body = GetComponent<Rigidbody2D>();
            roster = new CampaignRosterSnapshot(new[] { "DEV_GROUND", "DEV_GLIDER" }, "DEV_GROUND");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            var keyboard = Input.GetAxisRaw("Horizontal");
            if (Input.GetKeyDown(KeyCode.Space)) RequestJump();
            if (Input.GetKeyDown(KeyCode.Tab)) ToggleAnimal();
            var touch = 0f;
            foreach (var value in touchDirections.Values) touch += value;
            var horizontal = Mathf.Clamp(keyboard + touch, -1f, 1f);
            body.linearVelocity = new Vector2(horizontal * moveSpeed, body.linearVelocity.y);

            var grounded = Physics2D.Raycast(transform.position + Vector3.down * .68f, Vector2.down, .12f, ~0).collider != null;
            if (jumpRequested && grounded) body.linearVelocity = new Vector2(body.linearVelocity.x, jumpVelocity);
            jumpRequested = false;

            var gliding = CurrentAnimalId == "DEV_GLIDER" && !grounded && Mathf.Abs(horizontal) > 0.01f;
            stamina = Mathf.Clamp(stamina + (gliding ? -22f : 15f) * Time.deltaTime, 0f, 100f);
            body.gravityScale = gliding && stamina > 0f ? 0.8f : 2.5f;
        }

        public void SetTouchDirection(int pointerId, float direction) => touchDirections[pointerId] = Mathf.Clamp(direction, -1f, 1f);
        public void ReleaseTouchDirection(int pointerId) => touchDirections.Remove(pointerId);
        public void RequestJump() => jumpRequested = true;
        public void RequestSpecial() => FindFirstObjectByType<DevTestSession>()?.ReportUnavailableAbility("특수 동작");
        public void RequestAbility() => FindFirstObjectByType<DevTestSession>()?.ReportUnavailableAbility("능력");

        public bool TrySetAnimal(string animalId)
        {
            if (!roster.CanTransformTo(animalId)) return false;
            CurrentAnimalId = animalId;
            return true;
        }

        public void ToggleAnimal() => TrySetAnimal(CurrentAnimalId == "DEV_GROUND" ? "DEV_GLIDER" : "DEV_GROUND");
    }
}
