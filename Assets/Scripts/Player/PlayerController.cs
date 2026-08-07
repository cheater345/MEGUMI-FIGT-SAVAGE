using UnityEngine;
using SteelTempest.Combat;

namespace SteelTempest.Player
{
    /// <summary>
    /// Movement core: walk/run, jump, air-control, crouch, dash and dodge.
    /// Works with a Rigidbody2D and a circle/box collider. The player sprite
    /// is childed to this object so flipping only affects visuals.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float runSpeed = 8.5f;
        [SerializeField] private float acceleration = 60f;
        [SerializeField] private float groundFriction = 25f;

        [Header("Jump")]
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBuffer = 0.12f;

        [Header("Dodge / Dash")]
        [SerializeField] private float dodgeForce = 14f;
        [SerializeField] private float dodgeDuration = 0.25f;
        [SerializeField] private float dodgeCooldown = 0.6f;
        [SerializeField] private float dashForce = 18f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashCooldown = 1.2f;
        [SerializeField] private bool dashInAirAllowed = true;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask groundMask = ~0;

        private Rigidbody2D _rb;
        private Collider2D _collider;
        private HealthComponent _health;

        private float _lastGroundedTime;
        private float _lastJumpPressedTime;
        private float _dodgeEndTime;
        private float _dodgeReadyAt;
        private float _dashEndTime;
        private float _dashReadyAt;
        private bool _facingRight = true;

        public bool IsGrounded { get; private set; }
        public bool IsDodging { get; private set; }
        public bool IsDashing { get; private set; }
        public bool IsCrouching { get; private set; }
        public float FacingSign => _facingRight ? 1f : -1f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            _health = GetComponent<HealthComponent>();
        }

        private void Update()
        {
            var move = Controls.MoveAxis;
            IsCrouching = Controls.CrouchHeld && IsGrounded && Mathf.Approximately(move, 0f);

            if (Controls.JumpPressed)
            {
                _lastJumpPressedTime = Time.time;
            }

            // Dodge has priority over dash, and dash wins over normal movement.
            if (Controls.DodgePressed && Time.time >= _dodgeReadyAt && !IsDodging && !IsDashing)
            {
                BeginDodge();
            }
            else if (Controls.DashPressed && Time.time >= _dashReadyAt && !IsDashing && !IsDodging
                     && (IsGrounded || dashInAirAllowed))
            {
                BeginDash();
            }

            HandleFlip(move);
        }

        private void FixedUpdate()
        {
            var grounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask) != null;
            if (grounded)
            {
                IsGrounded = true;
                _lastGroundedTime = Time.time;
            }
            else if (Time.time - _lastGroundedTime > 0.1f)
            {
                IsGrounded = false;
            }

            if (IsDodging && Time.time < _dodgeEndTime)
            {
                _rb.linearVelocity = new Vector2(FacingSign * dodgeForce, 0f);
                return;
            }
            if (IsDodging) IsDodging = false;

            if (IsDashing && Time.time < _dashEndTime)
            {
                _rb.linearVelocity = new Vector2(FacingSign * dashForce, 0f);
                return;
            }
            if (IsDashing) IsDashing = false;

            var speed = Controls.RunHeld ? runSpeed : walkSpeed;
            var target = Controls.MoveAxis * speed;

            if (Mathf.Abs(target) > 0.01f)
            {
                _rb.linearVelocity = new Vector2(
                    Mathf.MoveTowards(_rb.linearVelocity.x, target, acceleration * Time.fixedDeltaTime),
                    _rb.linearVelocity.y);
            }
            else if (IsGrounded)
            {
                _rb.linearVelocity = new Vector2(
                    Mathf.MoveTowards(_rb.linearVelocity.x, 0f, groundFriction * Time.fixedDeltaTime),
                    _rb.linearVelocity.y);
            }

            // Jump with coyote time and input buffering.
            if (Time.time - _lastJumpPressedTime <= jumpBuffer && Time.time - _lastGroundedTime <= coyoteTime)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
                _lastGroundedTime = -1f;
                _lastJumpPressedTime = -1f;
            }
        }

        private void BeginDodge()
        {
            IsDodging = true;
            _dodgeEndTime = Time.time + dodgeDuration;
            _dodgeReadyAt = Time.time + dodgeCooldown;
            _health?.SetInvulnerable(dodgeDuration);
        }

        private void BeginDash()
        {
            IsDashing = true;
            _dashEndTime = Time.time + dashDuration;
            _dashReadyAt = Time.time + dashCooldown;
        }

        private void HandleFlip(float move)
        {
            if (Mathf.Abs(move) < 0.01f || IsDodging || IsDashing) return;
            var wantRight = move > 0f;
            if (wantRight == _facingRight) return;
            _facingRight = wantRight;
            var s = transform.localScale;
            transform.localScale = new Vector3(_facingRight ? Mathf.Abs(s.x) : -Mathf.Abs(s.x), s.y, s.z);
        }

        /// <summary>Knockback applied by the damage service on hit.</summary>
        public void Knockback(float xForce, float yForce)
        {
            _rb.linearVelocity = new Vector2(FacingSign * xForce, yForce);
        }
    }
}