using UnityEngine;

namespace SteelTempest.Items
{
    /// <summary>Equipment slots available to the player.</summary>
    public enum EquipSlot
    {
        Helmet, Armor, Gloves, Boots, Accessory
    }

    /// <summary>Rarity tiers controlling stat ranges and upgrade ceilings.</summary>
    public enum Rarity
    {
        Common, Uncommon, Rare, Epic, Legendary
    }

    /// <summary>
    /// ScriptableObject template for an equippable item. Instances are created
    /// at runtime with rolled stats; this asset supplies base values.
    /// </summary>
    [CreateAssetMenu(menuName = "SteelTempest/Items/Item Template", fileName = "ItemTemplate")]
    public sealed class ItemTemplate : ScriptableObject
    {
        public string itemName;
        public EquipSlot slot;
        public Rarity rarity = Rarity.Common;
        public Sprite icon;

        [Header("Base Stats")]
        public float defenseBonus;
        public float healthBonus;
        public float attackBonus;
        public float critBonus;
        public float moveSpeedBonus;

        [Header("Upgrade")]
        public int maxUpgradeLevel = 10;
        public int upgradeBaseCost = 50;
        public float upgradeCostGrowth = 1.15f;
        public float upgradeStatGrowth = 0.12f;

        public static string RarityLabel(Rarity r) => r switch
        {
            Rarity.Uncommon => "Uncommon",
            Rarity.Rare => "Rare",
            Rarity.Epic => "Epic",
            Rarity.Legendary => "Legendary",
            _ => "Common",
        };
    }
}