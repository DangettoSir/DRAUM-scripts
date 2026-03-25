using UnityEngine;

namespace DRAUM.Modules.Audio
{
    /// <summary>
    /// Конфигурация звуков для боевой системы (удары оружием)
    /// </summary>
    [CreateAssetMenu(fileName = "CombatAudioConfig", menuName = "DRAUM/Audio/Combat Config")]
    public class CombatAudioConfig : ScriptableObject
    {
        [System.Serializable]
        public class WeaponHitSounds
        {
            [Tooltip("Имя оружия/предмета для сопоставления (ItemData.name или название оружия)")]
            public string weaponName;
            
            [Tooltip("Звуки ударов для этого оружия")]
            public AudioClip[] hitClips;
            
            [Tooltip("Звуки ударов по разным материалам (опционально)")]
            public MaterialHitSounds[] materialHits;
            
            [Range(0f, 2f)]
            [Tooltip("Громкость звуков ударов для этого оружия")]
            public float volume = 1f;
        }
        
        [System.Serializable]
        public class MaterialHitSounds
        {
            [Tooltip("Имя материала для сопоставления")]
            public string materialName;
            
            [Tooltip("Звуки ударов по этому материалу")]
            public AudioClip[] clips;
            
            [Range(0f, 2f)]
            [Tooltip("Громкость звуков ударов по этому материалу")]
            public float volume = 1f;
        }
        
        [Header("Default Hit Sounds")]
        [Tooltip("Звуки ударов по умолчанию (если оружие не найдено в weaponHitSounds)")]
        public AudioClip[] defaultHitClips;
        
        [Tooltip("Звуки ударов по умолчанию для разных материалов")]
        public MaterialHitSounds[] defaultMaterialHits;
        
        [Range(0f, 2f)]
        [Tooltip("Громкость звуков ударов по умолчанию")]
        public float defaultHitVolume = 0.8f;
        
        [Header("Weapon-Specific Sounds")]
        [Tooltip("Звуки ударов для конкретных оружий")]
        public WeaponHitSounds[] weaponHitSounds;
        
        [Header("Direction-Specific Sounds (Optional)")]
        [Tooltip("Звуки для ударов влево (если пусто, используется defaultHitClips)")]
        public AudioClip[] leftSwingClips;
        
        [Tooltip("Звуки для ударов вправо (если пусто, используется defaultHitClips)")]
        public AudioClip[] rightSwingClips;
        
        [Tooltip("Звуки для ударов вверх (если пусто, используется defaultHitClips)")]
        public AudioClip[] upSwingClips;
        
        [Range(0f, 2f)]
        [Tooltip("Громкость звуков ударов по направлению")]
        public float swingVolume = 0.9f;
    }
}
