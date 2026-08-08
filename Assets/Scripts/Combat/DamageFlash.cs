using UnityEngine;
using SteelTempest.Combat;

namespace SteelTempest.Combat
{
    /// <summary>
    /// Flashes the sprite white for a beat whenever the owner takes damage,
    /// then eases back to the base tint. Cheap hit feedback for silhouettes.
    /// </summary>
    public sealed class DamageFlash : MonoBehaviour
    {
        [SerializeField] private float _flashSeconds = 0.12f;

        private SpriteRenderer _sr;
        private HealthComponent _health;
        private Color _baseColor = Color.white;
        private float _flashUntil;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
            _health = GetComponent<HealthComponent>();
            if (_health != null)
            {
                _health.OnHealthChanged += OnHealthChanged;
            }
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnHealthChanged -= OnHealthChanged;
            }
        }

        private void OnHealthChanged(float current, float max)
        {
            _flashUntil = Time.time + _flashSeconds;
        }

        private void Update()
        {
            if (_sr == null) return;
            var k = Mathf.Clamp01((_flashUntil - Time.time) / Mathf.Max(0.001f, _flashSeconds));
            _sr.color = Color.Lerp(_baseColor, Color.white, Mathf.Pow(k, 0.5f));
        }
    }
}