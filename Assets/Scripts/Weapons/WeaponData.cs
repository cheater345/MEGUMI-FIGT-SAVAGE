using UnityEngine;
using SteelTempest.Combat;

namespace SteelTempest.Weapons
{
    /// <summary>Weapon archetype identity feeding stat scaling. Data-driven.</summary>
    public enum WeaponClass
    {
        Sword, Greatsword, Katana, Dagger, Spear,
        Staff, Axe, Hammer, Fists, DualBlades,
    }

    /// <summary>
    /// ScriptableObject describing a weapon: base damage, reach, swing speed,
    /// knockback and the combo trees for ground, air and charged chains.
    /// </summary>
    [CreateAssetMenu(menuName = "SteelTempest/Weapons/Weapon Data", fileName = "WeaponData")]
    public sealed class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        public string weaponName = "Unnamed";
        public string weaponId = "sword";
        public WeaponClass weaponClass = WeaponClass.Sword;
        public Sprite icon;

[Header("Base Stats")]
        public float baseDamage = 10f;
        public float damageMultiplier = 1f;
        public float reachMultiplier = 1f;
        public float swingSpeedMultiplier = 1f;
        public float baseKnockback = 3f;
        public float knockbackMultiplier = 1f;

        [Header("Combo Trees")]
        public ComboTree groundCombos;
        public ComboTree airCombos;
        public ComboTree chargedCombos;

        [Header("Crit")]
        public float defaultCriticalChance = 0.05f;
        public float defaultCritMult = 1.5f;

        public string damageTag = "Enemy";
    }
}