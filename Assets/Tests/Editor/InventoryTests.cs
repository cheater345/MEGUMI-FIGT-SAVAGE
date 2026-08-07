using NUnit.Framework;
using SteelTempest.Items;

namespace SteelTempest.Tests.Editor
{
    public class InventoryTests
    {
        [Test]
        public void EquipSwapsWithoutDuplicating()
        {
            var inv = new Inventory();
            var helmetA = new ItemInstance(TestTemplate(EquipSlot.Helmet));
            var helmetB = new ItemInstance(TestTemplate(EquipSlot.Helmet));
            inv.Add(helmetA);
            inv.Add(helmetB);

            inv.Equip(helmetA);
            inv.Equip(helmetB);

            Assert.AreSame(helmetB, inv.GetEquipped(EquipSlot.Helmet));
            Assert.That(inv.Items.Count, Is.EqualTo(2));
            Assert.That(inv.Equipped, Does.ContainKey(EquipSlot.Helmet));
        }

        [Test]
        public void Unequip_RemovesFromSlot()
        {
            var inv = new Inventory();
            var item = new ItemInstance(TestTemplate(EquipSlot.Armor));
            inv.Add(item);
            inv.Equip(item);
            inv.Unequip(EquipSlot.Armor);
            Assert.IsNull(inv.GetEquipped(EquipSlot.Armor));
        }

        [Test]
        public void UpgradeGrowsStatAndCost()
        {
            var inv = new Inventory();
            var item = new ItemInstance(TestTemplate(EquipSlot.Gloves));
            inv.Add(item);
            inv.Equip(item);

            var before = item.Attack;
            item.Upgrade();
            Assert.That(item.Attack, Is.GreaterThan(before));
            Assert.That(item.UpgradeCost(), Is.GreaterThan(0));
        }

        private static ItemTemplate TestTemplate(EquipSlot slot)
        {
            var t = new ItemTemplate();
            t.itemName = "Test";
            t.slot = slot;
            t.attackBonus = 10f;
            t.upgradeBaseCost = 50;
            return t;
        }
    }
}