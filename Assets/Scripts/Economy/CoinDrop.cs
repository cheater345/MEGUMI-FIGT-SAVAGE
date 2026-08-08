using UnityEngine;
using SteelTempest.Core.Di;
using SteelTempest.Core.Events;
using SteelTempest.Enemies;

namespace SteelTempest.Economy
{
    /// <summary>
    /// Flying coin pickup. Bounces off the floor, then drifts toward the
    /// player when close. Collected coins hit the persisted wallet.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CoinDrop : MonoBehaviour
    {
        [SerializeField] private int value = 1;
        [SerializeField] private float magnetRange = 2.2f;
        [SerializeField] private float magnetSpeed = 10f;
        [SerializeField] private float lifetime = 8f;

        private Rigidbody2D _rb;
        private Transform _player;
        private float _despawnAt;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            _despawnAt = Time.time + lifetime;
        }

        public void Throw(Vector2 impulse)
        {
            if (_rb != null)
            {
                _rb.AddForce(impulse, ForceMode2D.Impulse);
            }
        }

        private void FixedUpdate()
        {
            if (_rb == null) return;

            if (_player == null)
            {
                _player = PlayerMarker.Instance != null ? PlayerMarker.Instance.transform : null;
            }
            if (_player == null) return;

            var diff = _player.position - transform.position;
            var distSqr = diff.sqrMagnitude;
            if (distSqr > magnetRange * magnetRange)
            {
                return;
            }

            var dir = diff.normalized;
            if (distSqr < 0.45f)
            {
                Collect();
                return;
            }
            _rb.linearVelocity = new Vector2(dir.x * magnetSpeed, dir.y * magnetSpeed * 0.6f + _rb.linearVelocity.y);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            Collect();
        }

        private void Collect()
        {
            ServiceLocator.Instance.Resolve<CurrencyManager>()?.GrantCoins(value);
            EventBus.Instance.Publish(new CoinEvent(value, true));
            Destroy(gameObject);
        }

        private void Update()
        {
            if (Time.time >= _despawnAt)
            {
                Destroy(gameObject);
            }
        }
    }
}