using UnityEngine;
using SteelTempest.Core.Events;
using SteelTempest.Player;
using SteelTempest.Pooling;
using SteelTempest.Weapons;

namespace SteelTempest.Combat
{
    /// <summary>
    /// Player attack state machine. Consumes Controls, advances the weapon's
    /// combo tree, spawns Hitboxes during active frames and drives
    /// blocking/parrying against incoming hits.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [Tooltip("Hitbox prefab spawned at attack active frames.")]
        [SerializeField] private Hitbox hitboxPrefab;

        private HealthComponent _health;
        private WeaponData _weapon;

        private int _comboIndex;
        private float _attackEndTime;
        private float _comboResetTime;
        private bool _isCharging;
        private bool _blocking;
        private AttackData _pendingDamage;
        private float _pendingSpawnAt;

        public bool CanAct => !_health.InHitStun && !_health.IsDead;

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();
        }

        public void SetWeapon(WeaponData weapon)
        {
            _weapon = weapon;
            _comboIndex = 0;
        }

        private void Update()
        {
            if (_weapon == null || !CanAct) return;

            // Block / parry handling.
            if (Controls.BlockHeld)
            {
                if (!_blocking)
                {
                    _blocking = true;
                    _health.StartBlocking(0.15f);
                }
            }
            else if (_blocking)
            {
                _blocking = false;
                _health.StopBlocking();
            }

            // Charged attack: heavy press starts the charge, release swings.
            if (!_isCharging && Controls.HeavyHeld && !Controls.BlockHeld)
            {
                _isCharging = true;
                return;
            }
            if (_isCharging && Controls.HeavyHeld)
            {
                return;
            }
            if (_isCharging)
            {
                _isCharging = false;
                BeginAttack(true);
                return;
            }

            if (_attackEndTime > Time.time) return; // animating/swinging

            if (Controls.LightPressed || Controls.HeavyPressed)
            {
                BeginAttack(Controls.HeavyPressed);
            }
        }

        private void BeginAttack(bool charged)
        {
            var tree = charged ? _weapon.chargedCombos : _weapon.groundCombos;
            if (tree == null || tree.Count == 0) return;

            var attack = tree.Get(_comboIndex);
            if (attack == null) return;

            var duration = attack.startupSeconds + attack.activeSeconds + attack.recoverySeconds;
            _attackEndTime = Time.time + duration;
            _comboResetTime = _attackEndTime + 0.15f;
            _comboIndex = (_comboIndex + 1) % tree.Count;

            _pendingDamage = attack;
            _pendingSpawnAt = Time.time + attack.startupSeconds;

            EventBus.Instance.Publish(new NotificationEvent("Swing"));
        }

        private void UpdateSpawn()
        {
            if (_pendingDamage == null) return;
            if (Time.time < _pendingSpawnAt) return;
            if (hitboxPrefab == null) return;

            var hb = ObjectPool.Spawn(hitboxPrefab, transform, Vector3.zero);
            var reach = _weapon.reachMultiplier * _pendingDamage.reach;
            var facing = transform.localScale.x >= 0f ? 1f : -1f;
            hb.transform.localPosition = new Vector3(reach * facing, 0f, 0f);
            hb.SetUp(gameObject,
                _weapon.baseDamage * _weapon.damageMultiplier * _pendingDamage.damage,
                _weapon.baseKnockback * _weapon.knockbackMultiplier * _pendingDamage.knockbackForce,
                _pendingDamage.critChance,
                _pendingDamage.critMultiplier,
                _pendingDamage.launches,
                _pendingDamage.isFinisher,
                _weapon.damageTag);
            hb.gameObject.SetActive(true);
            hb.DespawnAfter(_pendingDamage.activeSeconds);
            _pendingDamage = null;
        }

        private void LateUpdate()
        {
            UpdateSpawn();

            if (Time.time >= _comboResetTime)
            {
                _comboIndex = 0;
            }
        }
    }
}