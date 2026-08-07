using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SteelTempest.Combat;
using SteelTempest.Core.Events;
using SteelTempest.Enemies;

namespace SteelTempest.UI.Hud
{
    /// <summary>
    /// Root HUD controller. Subscribes to combat/progression events and pushes
    /// values into UI widgets (health bar, coin counter, toast). All refs are
    /// optional so the scene can be wired incrementally.
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private Image healthFill;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Economy")]
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private TextMeshProUGUI gemText;

        [Header("Toasts")]
        [SerializeField] private TextMeshProUGUI notificationText;

        private HealthComponent _playerHealth;

        private void Start()
        {
            if (PlayerMarker.Instance != null)
            {
                _playerHealth = PlayerMarker.Instance.GetComponent<HealthComponent>();
            }

            if (_playerHealth != null)
            {
                _playerHealth.OnHealthChanged += OnHealthChanged;
                OnHealthChanged(_playerHealth.Current, _playerHealth.Max);
            }

            EventBus.Instance.Subscribe<NotificationEvent>(OnNotification);
        }

        private void OnDestroy()
        {
            if (_playerHealth != null) _playerHealth.OnHealthChanged -= OnHealthChanged;
            EventBus.Instance.Unsubscribe<NotificationEvent>(OnNotification);
        }

        private void OnNotification(NotificationEvent evt)
        {
            if (notificationText != null)
            {
                notificationText.text = evt.Text;
                notificationText.canvasRenderer.SetAlpha(1f);
            }
        }

        private void OnHealthChanged(float current, float max)
        {
            if (healthFill != null) healthFill.fillAmount = max <= 0f ? 0f : current / max;
            if (healthText != null) healthText.text = $"{current:0}/{max:0}";
        }

        public void RefreshCurrency(int coins, int gems)
        {
            if (coinText != null) coinText.text = coins.ToString();
            if (gemText != null) gemText.text = gems.ToString();
        }
    }
}