using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Animol.QA
{
    // Attach only to the isolated QA player. No production input/stamina/animal owner is replaced.
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class UnityTemporaryPlayer2D : MonoBehaviour
    {
        public UnityTemporaryRun2D Run;
        public SpriteRenderer Visual;
        public Sprite RightSprite, LeftSprite;
        public float WalkSpeed = 4.2f, Acceleration = 32f, AirAcceleration = 24f;
        public float Friction = 30f, IceFriction = 2.5f, Gravity = 22f;
        public float JumpSpeed = 8.5f, SpringSpeed = 13f, MochiSpeed = 6f;
        public float CoyoteSeconds = .1f, JumpBufferSeconds = .1f;
        public const float BodyWidth = .55f, BodyHeight = .88f;
        public Rigidbody2D Body { get; private set; }
        public Collider2D Shape { get; private set; }
        public bool Grounded { get; private set; }
        readonly ContactPoint2D[] contacts = new ContactPoint2D[16];
        readonly HashSet<UnityTemporaryObstacle2D> winds = new HashSet<UnityTemporaryObstacle2D>();
        readonly List<Collider2D> ignoredTops = new List<Collider2D>();
        UnityTemporaryObstacle2D support;
        PhysicsMaterial2D ownMaterial;
        Vector2 incomingVelocity;
        float axis, coyote, jumpBuffer, ignoreUntil;
        bool uiLeft, uiRight, uiDown, jumpQueued, down;

        void Awake()
        {
            Body = GetComponent<Rigidbody2D>(); Shape = GetComponent<CapsuleCollider2D>();
            var capsule = (CapsuleCollider2D)Shape;
            capsule.size = new Vector2(BodyWidth, BodyHeight); capsule.direction = CapsuleDirection2D.Vertical;
            Body.bodyType = RigidbodyType2D.Dynamic; Body.gravityScale = 0;
            Body.constraints = RigidbodyConstraints2D.FreezeRotation;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Body.linearDamping = 0;
            ownMaterial = new PhysicsMaterial2D("ANIMOL_QA_ZeroFriction") { friction = 0, bounciness = 0 };
            Shape.sharedMaterial = ownMaterial;
        }

        void Update()
        {
            bool left = false, right = false, jump = false, reset = false;
            down = uiDown;
#if ENABLE_INPUT_SYSTEM
            var keys = Keyboard.current;
            if (keys != null)
            {
                left = keys.leftArrowKey.isPressed || keys.aKey.isPressed;
                right = keys.rightArrowKey.isPressed || keys.dKey.isPressed;
                down |= keys.downArrowKey.isPressed || keys.sKey.isPressed;
                jump = keys.spaceKey.wasPressedThisFrame || keys.upArrowKey.wasPressedThisFrame || keys.wKey.wasPressedThisFrame;
                reset = keys.rKey.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            left = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
            right = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
            down |= Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
            jump = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
            reset = Input.GetKeyDown(KeyCode.R);
#endif
            axis = ((right || uiRight) ? 1 : 0) - ((left || uiLeft) ? 1 : 0);
            jumpQueued |= jump;
            if (reset && Run != null) Run.ResetRun();
            if (Visual != null && axis != 0) Visual.sprite = axis < 0 ? LeftSprite : RightSprite;
        }

        void FixedUpdate()
        {
            if (Run == null || Run.IsResetting) return;
            if (Run.IsCleared) { Body.linearVelocity = Vector2.zero; jumpQueued = false; return; }
            float dt = Time.fixedDeltaTime;
            if (ignoredTops.Count > 0 && Time.time >= ignoreUntil) RestoreIgnoredTops();
            Grounded = false; support = null;
            int count = Shape.GetContacts(contacts);
            for (int i = 0; i < count; i++) if (contacts[i].normal.y > .5f && Body.linearVelocity.y <= .05f)
            {
                Grounded = true;
                var a = contacts[i].collider;
                var b = contacts[i].otherCollider;
                var other = a == Shape ? b : a;
                if (other != null) support = other.GetComponentInParent<UnityTemporaryObstacle2D>();
                if (support != null) break;
            }
            coyote = Grounded ? CoyoteSeconds : Mathf.Max(0, coyote - dt);
            jumpBuffer = jumpQueued ? JumpBufferSeconds : Mathf.Max(0, jumpBuffer - dt);
            jumpQueued = false;
            Vector2 velocity = Body.linearVelocity;
            bool dropped = down && Grounded && support != null && support.CanDropThrough;
            if (dropped)
            {
                IgnoreTop(support.Surface); Grounded = false; coyote = jumpBuffer = 0;
                Body.position += Vector2.down * .02f;
            }
            float belt = Grounded && support != null && support.Kind == "C08" ? (support.Facing == "LEFT" ? -1.2f : 1.2f) : 0;
            if (axis != 0) velocity.x = Mathf.MoveTowards(velocity.x, axis * WalkSpeed + belt, (Grounded ? Acceleration : AirAcceleration) * dt);
            else velocity.x = Mathf.MoveTowards(velocity.x, belt, (Grounded ? (support != null && support.Kind == "C10" ? IceFriction : Friction) : 1.5f) * dt);
            if (jumpBuffer > 0 && coyote > 0)
            {
                velocity.y = JumpSpeed; Grounded = false; coyote = jumpBuffer = 0;
            }
            velocity.y = Mathf.Max(-18, velocity.y - Gravity * dt);
            foreach (var wind in winds) if (wind != null && wind.isActiveAndEnabled)
            {
                if (wind.Facing == "UP") velocity.y += 8 * dt;
                else velocity.x += (wind.Facing == "LEFT" ? -6 : 6) * dt;
            }
            incomingVelocity = velocity; Body.linearVelocity = velocity;
            if (Body.position.y - BodyHeight * .5f < -3) Run.ResetRun();
        }

        void OnCollisionEnter2D(Collision2D collision) { ApplyDevice(collision); }
        void OnCollisionStay2D(Collision2D collision) { ApplyDevice(collision); }
        void ApplyDevice(Collision2D collision)
        {
            if (Run == null || Run.IsResetting || Run.IsCleared) return;
            var device = collision.collider.GetComponentInParent<UnityTemporaryObstacle2D>();
            if (device == null) return;
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector2 normal = collision.GetContact(i).normal;
                bool front = device.Facing == "LEFT" ? normal.x < -.5f && incomingVelocity.x > 0 : normal.x > .5f && incomingVelocity.x < 0;
                if (device.Kind == "C02" && front)
                {
                    incomingVelocity.x = -incomingVelocity.x;
                    Body.linearVelocity = incomingVelocity; device.Press(); return;
                }
                if (normal.y <= .5f || incomingVelocity.y > .05f) continue;
                if (device.Kind == "C05") device.PlayerSupported(this);
                if (device.Kind == "C03" || device.Kind == "C06" && incomingVelocity.y < -.5f)
                {
                    incomingVelocity.y = device.Kind == "C03" ? SpringSpeed : MochiSpeed;
                    Body.linearVelocity = incomingVelocity; Grounded = false; coyote = jumpBuffer = 0;
                    device.Press(); return;
                }
            }
        }

        void IgnoreTop(Collider2D surface)
        {
            if (surface == null) return;
            if (!ignoredTops.Contains(surface)) { ignoredTops.Add(surface); Physics2D.IgnoreCollision(Shape, surface, true); }
            ignoreUntil = Time.time + .25f;
        }
        void RestoreIgnoredTops()
        {
            if (Shape != null) foreach (var top in ignoredTops) if (top != null) Physics2D.IgnoreCollision(Shape, top, false);
            ignoredTops.Clear();
        }
        public void RespawnAtFeet(Vector2 feet)
        {
            if (Body == null) Awake();
            RestoreIgnoredTops(); winds.Clear(); ReleaseInput(); Grounded = false; support = null;
            coyote = jumpBuffer = 0; incomingVelocity = Vector2.zero;
            Body.position = feet + Vector2.up * (BodyHeight * .5f);
            Body.linearVelocity = Vector2.zero; Body.angularVelocity = 0;
        }
        public void EnterWind(UnityTemporaryObstacle2D wind) { if (wind != null) winds.Add(wind); }
        public void ExitWind(UnityTemporaryObstacle2D wind) { winds.Remove(wind); }
        public void SetLeftHeld(bool held) { uiLeft = held; }
        public void SetRightHeld(bool held) { uiRight = held; }
        public void SetDownHeld(bool held) { uiDown = held; }
        public void QueueJump() { jumpQueued = true; }
        public void ReleaseInput() { uiLeft = uiRight = uiDown = down = jumpQueued = false; axis = 0; }
        void OnApplicationFocus(bool focused) { if (!focused) ReleaseInput(); }
        void OnDisable() { ReleaseInput(); RestoreIgnoredTops(); winds.Clear(); }
        void OnDestroy() { if (ownMaterial != null) Destroy(ownMaterial); }
    }
}
