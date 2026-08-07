using UnityEngine;
using SteelTempest.Combat;

namespace SteelTempest.Enemies
{
    /// <summary>
    /// Boss behaviour driven by a health-threshold phase state machine.
    /// Phase 1: telegraphed heavy swings. Phase 2: faster, adds a forward
    /// lunge. Phase 3: enrage — faster attacks, wider reach, occasional
    /// blocks. Health fraction is read from the shared HealthComponent.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public sealed class BossController : MonoBehaviour
    {
        public enum BossPhase { PhaseOne, PhaseTwo, PhaseThree }

        [Header("Phase Thresholds (health fraction)")]
        [SerializeField] private float phase2At = 0.66f;
        [SerializeField] private float phase3At = 0.33f;

        [Header("Combat")]
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float timeBetweenAttacks = 1.6f;
        [SerializeField] private float attackDamage = 18f;

        private HealthComponent _health;
        private Transform _player;
        private BossPhase _currentPhase;
        private float _nextAttackAt;

        public BossPhase CurrentPhase => _currentPhase;

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();
        }

        private void Start()
        {
            _player = PlayerMarker.Instance != null ? PlayerMarker.Instance.transform : null;
            _currentPhase = BossPhase.PhaseOne;
            _health.OnHealthChanged += OnHealthChanged;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnHealthChanged -= OnHealthChanged;
        }

        private void OnHealthChanged(float current, float max)
        {
            var fraction = current / max;
            var target = fraction <= phase3At ? BossPhase.PhaseThree
                : fraction <= phase2At ? BossPhase.PhaseTwo
                : BossPhase.PhaseOne;

            if (target != _currentPhase)
            {
                _currentPhase = target;
                Debug.Log($"[SteelTempest] Boss entered {_currentPhase}");
            }
        }

        private void Update()
        {
            if (_health == null || _health.IsDead || _player == null) return;

            var toPlayer = _player.position - transform.position;
            FacePlayer(toPlayer.x);

            var speed = _currentPhase == BossPhase.PhaseThree ? moveSpeed * 1.35f
                : _currentPhase == BossPhase.PhaseTwo ? moveSpeed * 1.15f
                : moveSpeed;

            if (toPlayer.magnitude > attackRange)
            {
                var rb = GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = new Vector2(Mathf.Sign(toPlayer.x) * speed, rb.linearVelocity.y);
                }
                return;
            }

            var rb2 = GetComponent<Rigidbody2D>();
            if (rb2 != null) rb2.linearVelocity = Vector2.zero;

            var interval = _currentPhase == BossPhase.PhaseThree ? timeBetweenAttacks * 0.6f
                : _currentPhase == BossPhase.PhaseTwo ? timeBetweenAttacks * 0.8f
                : timeBetweenAttacks;

            if (Time.time >= _nextAttackAt)
            {
                _nextAttackAt = Time.time + interval;
                Swing();
            }
        }

        private void Swing()
        {
            var reach = attackRange * (_currentPhase == BossPhase.PhaseThree ? 1.25f : 1f);
            var hit = Physics2D.OverlapCircle(
                transform.position + new Vector3(transform.localScale.x >= 0 ? reach * 0.5f : -reach * 0.5f, 0f, 0f),
                reach);

            if (hit == null || !hit.TryGetComponent<HealthComponent>(out var targetHealth)) return;
            if (targetHealth.IsDead) return;

            DamageService.ApplyDamage(gameObject, hit.gameObject, new DamagePayload
            {
                amount = attackDamage,
                knockback = 5f,
                critChance = 0.03f,
                critMultiplier = 1.5f,
                launches = false,
            });
        }

        private void FacePlayer(float sign)
        {
            var s = transform.localScale;
            transform.localScale = new Vector3(sign >= 0f ? Mathf.Abs(s.x) : -Mathf.Abs(s.x), s.y, s.z);
        }
    }
}