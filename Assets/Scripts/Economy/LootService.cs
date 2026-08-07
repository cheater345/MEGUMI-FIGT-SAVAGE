using System;
using UnityEngine;
using SteelTempest.Economy;
using SteelTempest.Progression;

namespace SteelTempest.Economy
{
    /// <summary>
    /// Drop tables and kill rewards. Enemies grant XP and coins; rare elites
    /// have a chance to drop gems. Tuned per archetype via one method.
    /// </summary>
    public static class LootService
    {
        /// <summary>Applies kill rewards to the player. Called by spawn systems.</summary>
        public static void ApplyKillReward(string archetype, PlayerProgress progress, CurrencyManager currency)
        {
            var (xp, coins, gemChance) = RewardFor(archetype);
            progress.GrantXp(xp);
            currency.GrantCoins(coins);
            if (UnityEngine.Random.value < gemChance)
            {
                currency.GrantGems(1);
            }
        }

        private static (int xp, int coins, float gemChance) RewardFor(string archetype) => archetype switch
        {
            "Light" => (20, 15, 0.01f),
            "Heavy" => (35, 30, 0.02f),
            "Assassin" => (40, 35, 0.03f),
            "Elite" => (80, 75, 0.08f),
            "MiniBoss" => (150, 140, 0.15f),
            "Boss" => (500, 450, 1f),
            _ => (20, 15, 0.01f),
        };
    }

    /// <summary>Daily login reward schedule. One claim per calendar day.</summary>
    public sealed class DailyRewards
    {
        private readonly Func<DateTime> _clock;
        private readonly Action<int, int> _grant; // coins, gems

        private string _lastClaimKey;
        private DateTime _lastClaim;

        public DailyRewards(Func<DateTime> clock, Action<int, int> grant)
        {
            _clock = clock;
            _grant = grant;
        }

        public bool CanClaim()
        {
            if (_lastClaimKey == null) return true;
            return _lastClaim.Date != _clock().Date;
        }

        public bool TryClaim()
        {
            if (!CanClaim()) return false;
            var coins = UnityEngine.Random.Range(50, 121);
            var gems = UnityEngine.Random.Range(1, 4);
            _grant(coins, gems);
            _lastClaim = _clock();
            _lastClaimKey = _lastClaim.ToString("yyyy-MM-dd");
            return true;
        }

        public string LastClaimDate => _lastClaimKey ?? string.Empty;
    }
}