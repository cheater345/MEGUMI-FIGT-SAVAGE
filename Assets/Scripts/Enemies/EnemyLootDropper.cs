using UnityEngine;
using SteelTempest.Combat;
using SteelTempest.Core.Events;
using SteelTempest.Economy;

namespace SteelTempest.Enemies
{
    /// <summary>
    /// Attached to enemy prefabs. On defeat, sprays coins toward the player —
    /// the classic "silhouette explodes into loot" beat — and reports the kill.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public sealed class EnemyLootDropper : MonoBehaviour
    {
        [SerializeField] private CoinDrop coinPrefab;
        [SerializeField] private int coinsPerKill = 2;
        [SerializeField] private int coinsPerBoss = 10;
        [SerializeField] private float maxThrow = 3.5f;

        private HealthComponent _health;
        private bool _dropped;
        private bool _isBoss;

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();
            _isBoss = GetComponent<BossController>() != null;
        }

        private void OnEnable()
        {
            _dropped = false;
            if (_health != null)
            {
                _health.OnDefeated += OnDefeated;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.OnDefeated -= OnDefeated;
            }
        }

        private void OnDefeated(bool _)
        {
            if (_dropped) return;
            _dropped = true;

            var toPlayer = PlayerMarker.Instance != null
                ? PlayerMarker.Instance.transform.position - transform.position
                : Vector3.right;
            var baseDir = toPlayer.x >= 0f ? Vector2.right : Vector2.left;

            var count = _isBoss ? coinsPerBoss : coinsPerKill;
            for (var i = 0; i < count; i++)
            {
                SpawnCoin(baseDir, i, count);
            }

            EventBus.Instance.Publish(new EnemyDefeatedEvent(gameObject));
        }

        private void SpawnCoin(Vector2 baseDir, int index, int count)
        {
            if (coinPrefab == null) return;
            var coin = Instantiate(coinPrefab, transform.position + Vector3.up * 0.6f, Quaternion.identity);
            var fan = count <= 1 ? 0.5f : index / (float)(count - 1);
            var dir = baseDir * (0.5f + fan * 0.5f) + Vector2.up * (0.6f + Random.value * 0.5f);
            coin.Throw(dir * (maxThrow * 0.5f + Random.value * maxThrow));
        }
    }
}