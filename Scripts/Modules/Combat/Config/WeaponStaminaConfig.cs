using UnityEngine;

namespace DRAUM.Modules.Combat
{
    /// <summary>
    /// Расход стамины за удар по направлению для одного оружия.
    /// </summary>
    [System.Serializable]
    public class WeaponStaminaEntry
    {
        public string weaponName = "stick";
        public float costLeft = 15f;
        public float costRight = 15f;
        public float costUp = 30f;
    }

    /// <summary>
    /// Конфиг расхода стамины по оружиям. Имя оружия — ItemData.name или "stick" по умолчанию.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponStaminaConfig", menuName = "DRAUM/Combat/Weapon Stamina Config")]
    public class WeaponStaminaConfig : ScriptableObject
    {
        public WeaponStaminaEntry defaultWeapon = new WeaponStaminaEntry { weaponName = "stick", costLeft = 15f, costRight = 15f, costUp = 30f };
        public WeaponStaminaEntry[] weaponEntries = new WeaponStaminaEntry[0];

        /// <summary>
        /// Возвращает стоимость стамины за удар по направлению. direction: "Left", "Right", "Up".
        /// </summary>
        public float GetStaminaCost(string weaponName, string direction)
        {
            var entry = GetEntry(weaponName);
            if (entry == null) return defaultWeapon.costLeft;

            switch (direction?.ToLowerInvariant())
            {
                case "left": return entry.costLeft;
                case "right": return entry.costRight;
                case "up": return entry.costUp;
                default: return entry.costLeft;
            }
        }

        private WeaponStaminaEntry GetEntry(string weaponName)
        {
            if (string.IsNullOrEmpty(weaponName)) return defaultWeapon;
            string key = weaponName.Trim().ToLowerInvariant();
            if (key == defaultWeapon.weaponName.Trim().ToLowerInvariant()) return defaultWeapon;
            foreach (var e in weaponEntries)
            {
                if (e == null) continue;
                if (e.weaponName.Trim().ToLowerInvariant() == key) return e;
            }
            return defaultWeapon;
        }
    }
}
