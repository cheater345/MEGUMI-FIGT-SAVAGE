using System;
using UnityEngine;
using SteelTempest.Core.Events;

namespace SteelTempest.Combat
{
    /// <summary>
    /// Character health, hitstun, blocking and invulnerability primitives.
    /// Shared by the player, elites, mini-bosses and bosses alike.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class HealthComponent : MonoBehaviour
    {
        [Header("Vitals")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float hitStunSeconds = 0.25f;
        [SerializeField] private bool isPlayer;

        private float _invulnerableUntil;
        private bool _blocking;
        private float _blockParryActiveUntil;

        public float Current { get; private set; }
        public float Max => maxHealth;
        public bool IsDead { get; private set; }
        public bool InHitStun { get; private set; }

        public event Action<float, float> OnHealthChanged;
        public event Action<bool> OnDefeated;

        private void Awake()
        {
            Current = maxHealth;
        }

        /// <summary>True while the actor is immune to damage (dodge i-frames, parry grace).</summary>
        public bool IsInvulnerable => Time.time < _invulnerableUntil;

        public void SetInvulnerable(float seconds) => _invulnerableUntil = Time.time + seconds;

        /// <summary>
        /// Begins blocking. While the block is active the next incoming hit is a
        /// PERFECT BLOCK (parry): damage is fully negated and the defender gains
        /// a short invulnerability for a counter. Outside the parry window,
        /// blocking only reduces the damage taken.
        /// </summary>
        public void StartBlocking(float parryWindowSeconds = 0.15f)
        {
            _blocking = true;
            _blockParryActiveUntil = Time.time + parryWindowSeconds;
        }

        public void StopBlocking() => _blocking = false;

        /// <summary>
        /// Applies an incoming hit. Returns true when the hit connected at all
        /// (including a parried hit). The <paramref name="blocked"/> out value is
        /// the amount absorbed by the defender through blocking/parrying.
        /// </summary>
        public bool TakeDamage(float amount, out float blocked)
        {
            blocked = 0f;
            if (IsDead || IsInvulnerable) return false;

            if (_blocking)
            {
                if (Time.time < _blockParryActiveUntil)
                {
                    // Perfect block / parry: negate the hit, expose the attacker.
                    SetInvulnerable(0.5f);
                    blocked = amount;
                    EventBus.Instance.Publish(new ParryEvent(gameObject));
                    return true;
                }
                // Passive block: absorb 75% of the damage.
                blocked = amount * 0.75f;
                amount *= 0.25f;
            }

            Current = Mathf.Max(0f, Current - amount);
            OnHealthChanged?.Invoke(Current, Max);

            if (Current <= 0f)
            {
                IsDead = true;
                OnDefeated?.Invoke(true);
            }
            else
            {
                EnterHitStun();
            }
            return true;
        }

        private void EnterHitStun()
        {
            InHitStun = true;
            Invoke(nameof(ExitHitStun), hitStunSeconds);
        }

        private void ExitHitStun() => InHitStun = false;

        public void Heal(float amount)
        {
            if (IsDead) return;
            Current = Mathf.Min(Max, Current + amount);
            OnHealthChanged?.Invoke(Current, Max);
        }
    }
}