using UnityEngine;
using SteelTempest.Core.Events;
using SteelTempest.Enemies;

namespace SteelTempest.Modes
{
    /// <summary>
    /// Runtime binder that creates the shared ModeSession, feeds it to the
    /// EnemySpawner and publishes wave/HUD notifications. Configured
    /// from the generated boot scene.
    /// </summary>
    public sealed class ModeBinder : MonoBehaviour
    {
        [SerializeField] private EnemySpawner spawner;
        [SerializeField] private ModeDefinition definition;

        private ModeSession _session;

        public ModeSession Session => _session;

        private void Start()
        {
            if (definition == null) return;
            _session = new ModeSession { Definition = definition };
            _session.OnWaveChanged += (wave) =>
            {
                EventBus.Instance.Publish(new NotificationEvent("Wave " + wave));
            };
            if (spawner != null)
            {
                spawner.Configure(_session);
                EventBus.Instance.Publish(new NotificationEvent("Wave 1"));
            }
        }
    }
}