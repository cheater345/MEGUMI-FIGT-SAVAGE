using UnityEngine;
using UnityEngine.UI;
using SteelTempest.Combat;
using SteelTempest.Core.Di;
using SteelTempest.Core.Events;
using SteelTempest.Enemies;
using SteelTempest.Economy;

namespace SteelTempest.UI.Hud
{
    /// <summary>
    /// Pushes the persisted wallet into a coin label every frame — cheap and
    /// robust while the wallet has no change events of its own.
    /// </summary>
    public sealed class CoinCounterHud : MonoBehaviour
    {
        [SerializeField] private Text coinText;

        private CurrencyManager _wallet;
        private int _showing = -1;

        private void Awake()
        {
            if (coinText == null)
            {
                coinText = GetComponentInChildren<Text>();
            }
            _wallet = ServiceLocator.Instance.Resolve<CurrencyManager>();
        }

        private void Update()
        {
            if (_wallet == null || coinText == null) return;
            var coins = _wallet.Coins;
            if (coins != _showing)
            {
                _showing = coins;
                coinText.text = $"COINS  {coins:0000}";
            }
        }
    }
}