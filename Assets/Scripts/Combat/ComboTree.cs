using System.Collections.Generic;
using UnityEngine;

namespace SteelTempest.Combat
{
    /// <summary>
    /// Ordered chain of attacks. The player advances through the chain
    /// when pressing attack inside the combo window; otherwise it resets.
    /// </summary>
    [CreateAssetMenu(menuName = "SteelTempest/Combat/Combo Tree", fileName = "ComboTree")]
    public sealed class ComboTree : ScriptableObject
    {
        public List<AttackData> attacks = new();

        public int Count => attacks.Count;

        public AttackData Get(int index) =>
            index >= 0 && index < attacks.Count ? attacks[index] : null;
    }
}