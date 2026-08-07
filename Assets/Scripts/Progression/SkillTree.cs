using System.Collections.Generic;
using UnityEngine;

namespace SteelTempest.Progression
{
    /// <summary>
    /// A single passive or active talent in the skill tree, defined as data.
    /// Talents require a parent talent and a point cost; learning one may
    /// unlock further rows.
    /// </summary>
    [CreateAssetMenu(menuName = "SteelTempest/Progression/Talent", fileName = "Talent")]
    public sealed class Talent : ScriptableObject
    {
        public string talentName;
        [TextArea] public string description;
        public Sprite icon;
        public Talent parent;
        public int pointCost = 1;

        [Header("Effects (additive)")]
        public float damageBonus;
        public float attackSpeedBonus;
        public float maxHealthBonus;
        public float critChanceBonus;
        public float dodgeCooldownBonus;

        public bool IsRoot => parent == null;
    }

    /// <summary>
    /// Serializable record of learned talents and spent points, persisted
    /// through the save system. Resolves against the talent registry.
    /// </summary>
    public sealed class SkillTree
    {
        private readonly List<string> _learned = new();
        private readonly Dictionary<string, Talent> _registry = new();

        public IReadOnlyList<string> Learned => _learned;

        public void Register(Talent talent)
        {
            if (talent != null && !_registry.ContainsKey(talent.name))
            {
                _registry.Add(talent.name, talent);
            }
        }

        public bool CanLearn(Talent talent) =>
            talent != null &&
            !_learned.Contains(talent.name) &&
            (talent.parent == null || _learned.Contains(talent.parent.name));

        public bool Learn(Talent talent)
        {
            if (!CanLearn(talent)) return false;
            _learned.Add(talent.name);
            return true;
        }

        public bool HasLearned(string talentName) => _learned.Contains(talentName);

        /// <summary>Sums the stat bonuses across all learned talents.</summary>
        public (float damage, float attackSpeed, float maxHealth, float critChance, float dodgeCooldown)
            TotalBonuses()
        {
            float damage = 0f, attackSpeed = 0f, maxHealth = 0f, critChance = 0f, dodgeCooldown = 0f;
            foreach (var id in _learned)
            {
                if (!_registry.TryGetValue(id, out var t)) continue;
                damage += t.damageBonus;
                attackSpeed += t.attackSpeedBonus;
                maxHealth += t.maxHealthBonus;
                critChance += t.critChanceBonus;
                dodgeCooldown += t.dodgeCooldownBonus;
            }
            return (damage, attackSpeed, maxHealth, critChance, dodgeCooldown);
        }
    }
}