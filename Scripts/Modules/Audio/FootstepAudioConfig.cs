using UnityEngine;

namespace DRAUM.Modules.Audio
{
    [CreateAssetMenu(fileName = "FootstepAudioConfig", menuName = "DRAUM/Audio/Footstep Config")]
    public class FootstepAudioConfig : ScriptableObject
    {
        [System.Serializable]
        public class MaterialSounds
        {
            [Tooltip("PhysicsMaterial для определения материала (опционально)")]
            public PhysicsMaterial physicsMaterial;
            
            [Tooltip("Имя материала для сопоставления (если PhysicsMaterial не назначен, используется имя из Raycast)")]
            public string materialName;
            
            [Tooltip("Звуки шагов для этого материала")]
            public AudioClip[] walkClips;
            
            [Tooltip("Звуки бега для этого материала (если пусто, используются walkClips)")]
            public AudioClip[] sprintClips;
            
            [Tooltip("Звуки ползания для этого материала (если пусто, используются walkClips)")]
            public AudioClip[] crouchClips;
            
            [Range(0f, 2f)] 
            [Tooltip("Громкость звуков для этого материала")]
            public float volume = 1f;
            
            [Tooltip("Множитель громкости для бега (1.0 = такая же громкость как ходьба)")]
            [Range(0.5f, 2f)]
            public float sprintVolumeMultiplier = 1.2f;
            
            [Tooltip("Множитель громкости для ползания (1.0 = такая же громкость как ходьба)")]
            [Range(0.3f, 1.5f)]
            public float crouchVolumeMultiplier = 0.7f;
        }

        [Header("Material Sounds")]
        [Tooltip("Настройки звуков для разных материалов")]
        public MaterialSounds[] materialSounds;
        
        [Header("Default Settings")]
        [Range(0f, 2f)] 
        [Tooltip("Громкость по умолчанию для всех материалов")]
        public float defaultVolume = 0.8f;
        
        [Tooltip("Звуки шагов по умолчанию (если материал не найден)")]
        public AudioClip[] defaultWalkClips;
        
        [Tooltip("Звуки бега по умолчанию (если материал не найден)")]
        public AudioClip[] defaultSprintClips;
        
        [Tooltip("Звуки ползания по умолчанию (если материал не найден)")]
        public AudioClip[] defaultCrouchClips;
    }
}   