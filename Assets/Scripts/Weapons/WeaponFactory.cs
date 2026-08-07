using System.Collections.Generic;
using UnityEngine;

namespace SteelTempest.Weapons
{
    /// <summary>
    /// Authoring helper that builds WeaponData assets at edit-time, keeping
    /// the ten weapon classes tuned from a single table. Run from an editor
    /// menu: Tools > SteelTempest > Generate Starter Weapons.
    /// </summary>
    public static class WeaponFactory
    {
        public static WeaponData Create(WeaponClass weaponClass)
        {
            var data = ScriptableObject.CreateInstance<WeaponData>();
            data.weaponClass = weaponClass;
            data.weaponId = weaponClass.ToString();
            data.baseDamage = BaseDamage(weaponClass);
            data.baseKnockback = 3f;
            data.reachMultiplier = ReachFactor(weaponClass);
            data.swingSpeedMultiplier = SwingSpeedFactor(weaponClass);
            data.knockbackMultiplier = KnockbackFactor(weaponClass);
            return data;
        }

        private static float BaseDamage(WeaponClass c) => c switch
        {
            WeaponClass.Dagger => 7f,
            WeaponClass.Fists => 6f,
            WeaponClass.DualBlades => 9f,
            WeaponClass.Sword => 10f,
            WeaponClass.Katana => 11f,
            WeaponClass.Spear => 11f,
            WeaponClass.Axe => 13f,
            WeaponClass.Staff => 12f,
            WeaponClass.Greatsword => 15f,
            WeaponClass.Hammer => 16f,
            _ => 10f,
        };

        private static float ReachFactor(WeaponClass c) => c switch
        {
            WeaponClass.Dagger => 0.8f,
            WeaponClass.Fists => 0.75f,
            WeaponClass.DualBlades => 0.9f,
            WeaponClass.Sword => 1f,
            WeaponClass.Katana => 1.1f,
            WeaponClass.Spear => 1.6f,
            WeaponClass.Staff => 1.3f,
            WeaponClass.Axe => 1.15f,
            WeaponClass.Greatsword => 1.2f,
            WeaponClass.Hammer => 1.25f,
            _ => 1f,
        };

        private static float SwingSpeedFactor(WeaponClass c) => c switch
        {
            WeaponClass.Dagger => 1.6f,
            WeaponClass.Fists => 2f,
            WeaponClass.DualBlades => 1.4f,
            WeaponClass.Sword => 1.2f,
            WeaponClass.Katana => 1.3f,
            WeaponClass.Spear => 0.9f,
            WeaponClass.Staff => 0.85f,
            WeaponClass.Axe => 0.8f,
            WeaponClass.Greatsword => 0.6f,
            WeaponClass.Hammer => 0.5f,
            _ => 1f,
        };

        private static float KnockbackFactor(WeaponClass c) => c switch
        {
            WeaponClass.Dagger => 0.6f,
            WeaponClass.Fists => 0.9f,
            WeaponClass.DualBlades => 0.8f,
            WeaponClass.Sword => 1f,
            WeaponClass.Katana => 1.2f,
            WeaponClass.Spear => 1.1f,
            WeaponClass.Staff => 1.4f,
            WeaponClass.Axe => 1.5f,
            WeaponClass.Greatsword => 1.8f,
            WeaponClass.Hammer => 2f,
            _ => 1f,
        };
    }
}