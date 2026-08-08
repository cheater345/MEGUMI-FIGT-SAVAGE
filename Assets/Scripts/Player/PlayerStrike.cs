using UnityEngine;
using SteelTempest.Combat;
using SteelTempest.Core.Events;
using SteelTempest.Pooling;

namespace SteelTempest.Player
{
    /// <summary>
    /// Shadow-Fight-style ranged kit: a spam-able shuriken (Skill button)
    /// plus a magic meter that fills from dealing/taking damage. At 100%
    /// the next Skill unleashes a piercing shadow orb instead.
    /// </summary>
    public sealed class PlayerStrike : MonoBehaviour
    {
        [Header("Ranged weapon")]
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private float shurikenCooldown = 0.7f;
        [SerializeField] private float shurikenDamage = 12f;
        [SerializeField] private float shurikenKnockback = 5f;
        [SerializeField] private float shurikenSpeed = 13f;

        [Header("Shadow magic")]
        [SerializeField] private float magicMeterOnHit = 7f;
        [SerializeField] private float magicMeterOnTaken = 9f;
        [SerializeField] private float magicDamage = 26f;
        [SerializeField] private float magicKnockback = 14f;
        [SerializeField] private float magicSpeed = 20f;
        [SerializeField] private float magicPierce = 2;

        [Header("Constants")]
        [SerializeField] private float meterMax = 100f;
        [SerializeField] private float launchChance = 0.25f;

        private HealthComponent _health;
        private float _meter;
        private float _nextShurikenAt;
        private bool _magicNotified;

        public float MeterNormalized => meterMax <= 0f ? 0f : Mathf.Clamp01(_meter / meterMax);

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();
        }

        private void OnEnable()
        {
            EventBus.Instance.Subscribe<DamageEvent>(OnDamageEvent);
        }

        private void OnDisable()
        {
            EventBus.Instance.Unsubscribe<DamageEvent>(OnDamageEvent);
        }

        private void OnDamageEvent(DamageEvent evt)
        {
            var isMine = evt.Source == gameObject;
            var onMe = evt.Target == gameObject;
            if (!isMine && !onMe) return;

            var gain = isMine ? magicMeterOnHit : magicMeterOnTaken;
            if (gain <= 0f) return;

            _meter = Mathf.Min(meterMax, _meter + gain);
            if (_meter >= meterMax && !_magicNotified)
            {
                _magicNotified = true;
                EventBus.Instance.Publish(new NotificationEvent("SHADOW MAGIC READY - [L] TO UNLEASH"));
            }
        }

        private void Update()
        {
            if (_health != null && _health.IsDead) return;
            if (!Controls.SkillPressed) return;
            if (Time.time < _nextShurikenAt) return;

            _nextShurikenAt = Time.time + shurikenCooldown;

            if (_meter >= meterMax)
            {
                _meter = 0f;
                _magicNotified = false;
                FireProjectile(magicSpeed, magicDamage, magicKnockback, UnityEngine.Random.value < launchChance, magicPierce);
                EventBus.Instance.Publish(new NotificationEvent("SHADOW MAGIC!"));
            }
            else
            {
                FireProjectile(shurikenSpeed, shurikenDamage, shurikenKnockback, false, 0);
            }
        }

        private void FireProjectile(float speed, float damage, float knockback, bool launches, int pierce)
        {
            if (projectilePrefab == null) return;
            var facing = transform.localScale.x >= 0f ? 1f : -1f;
            var shot = ObjectPool.Spawn(projectilePrefab, transform, new Vector3(facing * 0.9f, 0.45f, 0f));
            shot.transform.localScale = Vector3.one;
            shot.Launch(new Vector2(facing, 0f), speed, damage, knockback, launches, pierce);
        }
    }
}