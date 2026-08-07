using UnityEngine;
using SteelTempest.Combat;

namespace SteelTempest.Enemies
{
    /// <summary>Basic combat dispositions shared by enemy archetypes.</summary>
    public enum EnemyArchetype
    {
        Light, Heavy, Assassin, Elite, MiniBoss, Boss
    }

    /// <summary>
    /// Root enemy behaviour: handles movement toward the player, melee
    /// attacks and simple defensive reactions (block, dodge). Heavier
    /// archetypes take longer startups and hit harder; assassins dodge
    /// more often. Bosses are driven by <see cref="BossController"/>.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public sealed class EnemyController : MonoBehaviour
    {
        [Header("Archetype")]
        public EnemyArchetype archetype = EnemyArchetype.Light;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float attackRange = 1.4f;
        [SerializeField] private float chaseRange = 12f;
        [SerializeField] private float leashRange = 16f;

        [Header("Combat")]
        [SerializeField] private float attackDamage = 8f;
        [SerializeField] private float attackStartup = 0.35f;
        [SerializeField] private float attackCooldown = 1.2f;
        [SerializeField] private float blockChance = 0.1f;
        [SerializeField] private float dodgeChance = 0.1f;

        private HealthComponent _health;
        private Transform _player;
        private Rigidbody2D _rb;

        private float _nextAttackAt;
        private float _blockUntil;
        private float _dodgeUntil;

        public bool IsAlive => _health != null && !_health.IsDead;

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            _player = FindObjectOfType<PlayerMarker>()?.transform;
        }

        private void Update()
        {
            if (!IsAlive || _player == null) return;

            var toPlayer = _player.position - transform.position;
            var dist = toPlayer.magnitude;

            if (dist > leashRange)
            {
                // De-spawn or retreat past the leash.
                return;
            }

            FacePlayer(toPlayer);

            // Defensive reactions.
            var wantBlock = Time.time < _blockUntil;
            var wantDodge = Time.time < _dodgeUntil;

            if (wantDodge)
            {
                _rb.linearVelocity = new Vector2(-Mathf.Sign(toPlayer.x) * moveSpeed * 2f, _rb.linearVelocity.y);
                return;
            }

            if (wantBlock)
            {
                _health.StartBlocking(0.1f);
                _rb.linearVelocity = Vector2.zero;
                return;
            }
            _health.StopBlocking();

            if (dist > attackRange)
            {
                _rb.linearVelocity = new Vector2(Mathf.Sign(toPlayer.x) * moveSpeed, _rb.linearVelocity.y);
                return;
            }

            _rb.linearVelocity = Vector2.zero;
            if (Time.time >= _nextAttackAt)
            {
                _nextAttackAt = Time.time + attackCooldown;
                TryAttack();
            }
        }

        private void FacePlayer(Vector3 toPlayer)
        {
            var s = transform.localScale;
            transform.localScale = new Vector3(toPlayer.x >= 0f ? Mathf.Abs(s.x) : -Mathf.Abs(s.x), s.y, s.z);
        }

        /// <summary>Called by other systems (e.g. boss controllers) when hit.</summary>
        public void OnTookHit()
        {
            if (Random.value < dodgeChance)
            {
                _dodgeUntil = Time.time + 0.4f;
            }
            else if (Random.value < blockChance)
            {
                _blockUntil = Time.time + 0.5f;
            }
        }

        private void TryAttack()
        {
            var hit = Physics2D.OverlapCircle(
                transform.position + new Vector3(transform.localScale.x >= 0 ? 0.8f : -0.8f, 0f, 0f),
                attackRange * 0.8f);

            if (hit == null || !hit.TryGetComponent<HealthComponent>(out var targetHealth)) return;
            if (targetHealth.IsDead) return;

            DamageService.ApplyDamage(gameObject, hit.gameObject, new DamagePayload
            {
                amount = attackDamage,
                knockback = 2f,
                critChance = 0.02f,
                critMultiplier = 1.5f,
            });
        }
    }
}