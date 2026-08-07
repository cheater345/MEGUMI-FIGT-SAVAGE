using System;
using SteelTempest.Core.Events;
using SteelTempest.Save;

namespace SteelTempest.Progression
{
    /// <summary>
    /// Player level and experience tracking. XP comes from defeating enemies;
    /// when the threshold is reached the character levels up, gaining skill
    /// points. State persists through <see cref="SaveData"/>.
    /// </summary>
    public sealed class PlayerProgress
    {
        private readonly SaveManager _saves;

        public event Action<int> OnLevelUp;

        public int Level => _saves.Data.playerLevel;
        public int Experience => _saves.Data.experience;
        public int SkillPoints => _saves.Data.skillPoints;

        public PlayerProgress(SaveManager saves) => _saves = saves;

        public int LevelUpThreshold(int level) => 100 + (level - 1) * 75;

        public void GrantXp(int amount)
        {
            if (amount < 0) return;
            _saves.Data.experience += amount;
            while (_saves.Data.experience >= LevelUpThreshold(Level))
            {
                _saves.Data.experience -= LevelUpThreshold(Level);
                _saves.Data.playerLevel++;
                _saves.Data.skillPoints += 2; // 2 skill points per level
                OnLevelUp?.Invoke(Level);
                EventBus.Instance.Publish(new NotificationEvent($"Level Up! Now level {Level}."));
            }
        }

        public bool SpendSkillPoint()
        {
            if (SkillPoints <= 0) return false;
            _saves.Data.skillPoints--;
            return true;
        }
    }

    /// <summary>Fired when the player reaches a new level.</summary>
    public readonly struct PlayerUpLevelEvent
    {
        public readonly int NewLevel;
        public PlayerUpLevelEvent(int level) => NewLevel = level;
    }
}