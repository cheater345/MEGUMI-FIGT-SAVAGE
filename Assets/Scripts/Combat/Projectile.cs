using UnityEngine;
using SteelTempest.Pooling;

namespace SteelTempest.Combat
{
    /// <summary>
    /// Pooled straight-line projectile (shuriken / magic orb). Moves along a
    /// fixed direction, damages the first matching victim (or pierces), then
    /// returns to the pool.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Projectile : MonoBehaviour
    {
        private static readonly int RotateSpeed = 720;

        [SerializeField] private float lifetime = 3.5f;

        private Rigidbody2D _rb;
        private Vector2 _dir = Vector2.right;
        private float _speed = 14f;
        private float _damage;
        private float _knockback;
        private string _damageTag = "Enemy";
        private bool _launches;
        private float _despawnAt;
        private int _pierceLeft;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            _despawnAt = Time.time + lifetime;
        }

        public void Launch(
            Vector2 direction,
            float launchSpeed,
            float damage,
            float knockback,
            bool launches,
            int pierceCount,
            string damageTag = "Enemy")
        {
            _dir = direction.normalized;
            _speed = launchSpeed;
            _damage = damage;
            _knockback = knockback;
            _launches = launches;
            _damageTag = damageTag;
            _pierceLeft = pierceCount;
        }

        private void Update()
        {
            if (Time.time >= _despawnAt)
            {
                Despawn();
                return;
            }
            _dir = _dir.normalized;
            _rb.linearVelocity = _dir * _speed;
            transform.Rotate(0f, 0f, RotateSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(_damageTag)) return;

            if (DamageService.ApplyDamage(gameObject, other.gameObject, new DamagePayload
            {
                amount = _damage,
                knockback = _knockback,
                critChance = 0.02f,
                critMultiplier = 1.5f,
                launches = _launches,
            }))
            {
                _pierceLeft--;
                if (_pierceLeft <= 0)
                {
                    Despawn();
                    return;
                }
            }

            if (!other.isTrigger)
            {
                Despawn();
            }
        }

        private void Despawn()
        {
            _rb.linearVelocity = Vector2.zero;
            ObjectPool.Despawn(this);
        }
    }
}