using SteelTempest.Core.Di;
using SteelTempest.Save;

namespace SteelTempest.Economy
{
    /// <summary>
    /// Arbitrates the two player currencies (coins, gems) on top of the
    /// persisted <see cref="SaveData"/>. All mutations flow through here so
    /// the save file stays the single source of truth.
    /// </summary>
    public sealed class CurrencyManager
    {
        private SaveManager _saves;

        public void Initialize()
        {
            _saves = ServiceLocator.Instance.Resolve<SaveManager>();
        }

        public int Coins => _saves?.Data.coins ?? 0;
        public int Gems => _saves?.Data.gems ?? 0;

        public bool SpendCoins(int amount)
        {
            if (amount < 0 || _saves == null || _saves.Data.coins < amount) return false;
            _saves.Data.coins -= amount;
            return true;
        }

        public void GrantCoins(int amount)
        {
            if (amount > 0 && _saves != null) _saves.Data.coins += amount;
        }

        public void GrantGems(int amount)
        {
            if (amount > 0 && _saves != null) _saves.Data.gems += amount;
        }
    }
}