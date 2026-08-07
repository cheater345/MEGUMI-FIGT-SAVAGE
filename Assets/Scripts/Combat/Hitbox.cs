using System.Collections.Generic;
using UnityEngine;

namespace SteelTempest.Combat
{
    /// <summary>
    /// Overlap-triggered hit zone attached to an attack frame.
    /// Damages each victim once per swing.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Hitbox : MonoBehaviour
    {
        private readonly HashSet<GameObject> _hit = new();
        private GameObject _source;
        private float _damage;
        private float _knockback;
        private float _critChance;
        private float _critMultiplier;
        private bool _launches;
        private bool _finisher;
        private string _damageTag = "Enemy";

        private void OnEnable()
        {
            _hit.Clear();
        }

        /// <summary>Schedules a return to the pool after <paramref name="seconds"/>.</summary>
        public void DespawnAfter(float seconds)
        {
            Invoke(nameof(Reset), seconds);
        }

        /// <summary>Returns this object to its pool (or deactivates it).</summary>
        public void Reset()
        {
            CancelInvoke(nameof(Reset));
            _source = null;
            _hit.Clear();
            gameObject.SetActive(false);
        }

        public void SetUp(
            GameObject source,
            float damage,
            float knockback,
            float critChance,
            float critMultiplier,
            bool launches,
            bool finisher,
            string damageTag)
        {
            _source = source;
            _damage = damage;
            _knockback = knockback;
            _critChance = critChance;
            _critMultiplier = critMultiplier;
            _launches = launches;
            _finisher = finisher;
            _damageTag = damageTag;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_source == null || !other.CompareTag(_damageTag)) return;
            if (_hit.Contains(other.gameObject)) return;

            _hit.Add(other.gameObject);
            DamageService.ApplyDamage(_source, other.gameObject, new DamagePayload
            {
                amount = _damage,
                knockback = _knockback,
                critChance = _critChance,
                critMultiplier = _critMultiplier,
                launches = _launches,
                finisher = _finisher,
            });
        }
    }
}