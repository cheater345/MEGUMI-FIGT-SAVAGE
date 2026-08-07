using System;
using UnityEngine;

namespace SteelTempest.Save
{
    /// <summary>
    /// Serializable save payload. Add fields as progression features land.
    /// Stored as JSON in persistentDataPath.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;
        public int playerLevel = 1;
        public int experience;
        public int coins;
        public int gems;
        public int skillPoints;
        public string equippedWeaponId = "sword";
    }
}