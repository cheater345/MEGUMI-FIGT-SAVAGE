using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SteelTempest.Combat;
using SteelTempest.Enemies;

namespace SteelTempest.UI.Hud
{
    /// <summary>
    /// Black defeat overlay shown when the player falls. Any keyboard/touch
    /// input restarts the current scene. Also prints the wave reached.
    /// </summary>
    public sealed class DefeatOverlay : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text title;
        [SerializeField] private Text detail;

        private HealthComponent _playerHealth;
        private bool _active;
private void Awake()
        {
            var player = PlayerMarker.Instance != null ? PlayerMarker.Instance.gameObject : null;
            if (player != null)
            {
                _playerHealth = player.GetComponent<HealthComponent>();
            }
        }

        private void Start()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnDefeated += OnDefeated;
            }
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnDefeated -= OnDefeated;
            }
        }

        private void Update()
        {
            if (!_active) return;
            if (Input.anyKeyDown || Input.touchCount > 0)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        private void OnDefeated(bool _)
        {
            _active = true;
            SetVisible(true);
            if (title != null) title.text = "KNOCKED OUT";
            if (detail != null) detail.text = "You fought well, Shadow...\nTap any key to rise again.";
        }

        private void SetVisible(bool visible)
        {
            if (panel != null) panel.SetActive(visible);
        }
    }
}