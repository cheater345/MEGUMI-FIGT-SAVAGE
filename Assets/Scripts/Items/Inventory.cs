using System.Collections.Generic;

namespace SteelTempest.Items
{
    /// <summary>
    /// Player inventory: owns a list of items and an equipped set (one item per
    /// slot). Equipping swaps the slot's occupant without destroying it.
    /// </summary>
    public sealed class Inventory
    {
        private readonly List<ItemInstance> _items = new();
        private readonly Dictionary<EquipSlot, ItemInstance> _equipped = new();

        public IReadOnlyList<ItemInstance> Items => _items;
        public IReadOnlyDictionary<EquipSlot, ItemInstance> Equipped => _equipped;

        public void Add(ItemInstance item)
        {
            if (item != null && !_items.Contains(item)) _items.Add(item);
        }

        public ItemInstance GetEquipped(EquipSlot slot) =>
            _equipped.TryGetValue(slot, out var item) ? item : null;

        public void Equip(ItemInstance item)
        {
            if (item == null || !_items.Contains(item)) return;

            // Swap: old item goes back to the bag.
            if (_equipped.TryGetValue(item.Template.slot, out var current) && current != item)
            {
                _equipped[item.Template.slot] = item;
            }
            else
            {
                _equipped[item.Template.slot] = item;
            }
        }

        public void Unequip(EquipSlot slot) => _equipped.Remove(slot);
    }
}