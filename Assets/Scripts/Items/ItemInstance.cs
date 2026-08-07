using System;

namespace SteelTempest.Items
{
    /// <summary>
    /// Runtime item instance: a template plus an upgrade level. Stat bonuses
    /// grow with the upgrade level; legendary items accept the most upgrades.
    /// </summary>
    public sealed class ItemInstance
    {
        public ItemTemplate Template { get; }
        public int UpgradeLevel { get; private set; }

        public ItemInstance(ItemTemplate template, int upgradeLevel = 0)
        {
            Template = template ?? throw new ArgumentNullException(nameof(template));
            UpgradeLevel = Math.Max(0, upgradeLevel);
        }

        public bool CanUpgrade => UpgradeLevel < Template.maxUpgradeLevel;

        public int UpgradeCost()
        {
            var cost = Template.upgradeBaseCost;
            for (var i = 0; i < UpgradeLevel; i++)
            {
                cost = (int)(cost * Template.upgradeCostGrowth);
            }
            return cost;
        }

        public void Upgrade()
        {
            if (CanUpgrade) UpgradeLevel++;
        }

        private float Growth => 1f + Template.upgradeStatGrowth * UpgradeLevel;

        public float Defense => Template.defenseBonus * Growth;
        public float Health => Template.healthBonus * Growth;
        public float Attack => Template.attackBonus * Growth;
        public float Crit => Template.critBonus * Growth;
        public float MoveSpeed => Template.moveSpeedBonus * Growth;
    }
}