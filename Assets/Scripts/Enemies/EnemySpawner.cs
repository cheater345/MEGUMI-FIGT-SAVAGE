using UnityEngine;
using SteelTempest.Combat;
using SteelTempest.Core.Events;
using SteelTempest.Modes;

namespace SteelTempest.Enemies
{
    /// <summary>
    /// Wave-based enemy spawner. Reads a <see cref="ModeDefinition"/>, spawns
    /// enemies at intervals up to the cap, and advances waves — inserting a
    /// boss wave at the configured interval. Tracks defeats via the event bus.
    /// </summary>
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyController lightEnemyPrefab;
        [SerializeField] private EnemyController heavyEnemyPrefab;
        [SerializeField] private EnemyController assassinEnemyPrefab;
        [SerializeField] private BossController bossPrefab;
        [SerializeField] private ModeSession session;

        [Header("Spawning")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private int maxAlive = 8;

        private int _alive;
        private float _nextSpawnAt;
        private int _spawnedThisWave;

        private void OnEnable()
        {
            EventBus.Instance.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
        }

        private void OnDisable()
        {
            EventBus.Instance.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
        {
            _alive = Mathf.Max(0, _alive - 1);
        }

        public void Configure(ModeSession modeSession)
        {
            session = modeSession;
            _alive = 0;
            _spawnedThisWave = 0;
            _nextSpawnAt = Time.time + 1f;
        }

        private void Update()
        {
            if (session == null || session.Definition == null) return;

            var def = session.Definition;
            var waveCap = def.enemyCap;

            if (_alive >= waveCap) return;
            if (Time.time < _nextSpawnAt) return;

            var isBossWave = session.Wave > 0 && session.Wave % Mathf.Max(1, def.wavesBeforeBoss) == 0;

            _nextSpawnAt = Time.time + def.enemySpawnInterval;
            if (isBossWave)
            {
                SpawnBoss();
                session.AdvanceWave();
            }
            else
            {
                SpawnEnemy();
                _spawnedThisWave++;
                if (_spawnedThisWave >= waveCap)
                {
                    _spawnedThisWave = 0;
                    session.AdvanceWave();
                }
            }
        }

        private void SpawnEnemy()
        {
            var roll = Random.value;
            var prefab = roll < 0.55f ? lightEnemyPrefab
                : roll < 0.8f ? heavyEnemyPrefab
                : assassinEnemyPrefab;

            if (prefab == null) return;

            var point = spawnPoints.Length > 0
                ? spawnPoints[Random.Range(0, spawnPoints.Length)]
                : transform;

            var enemy = Instantiate(prefab, point.position, Quaternion.identity);
            _alive++;

            var health = enemy.GetComponent<HealthComponent>();
            var scale = session.DifficultyScale;
            if (health != null && scale > 1f)
            {
                health.transform.localScale *= 1f + (scale - 1f) * 0.2f;
            }
        }

        private void SpawnBoss()
        {
            if (bossPrefab == null) return;
            var point = spawnPoints.Length > 0
                ? spawnPoints[Random.Range(0, spawnPoints.Length)]
                : transform;
            Instantiate(bossPrefab, point.position, Quaternion.identity);
            _alive++;
        }

        public void NotifyDefeated()
        {
            _alive = Mathf.Max(0, _alive - 1);
        }
    }
}