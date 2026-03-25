using UnityEngine;

namespace DRAUM.Modules.Audio
{
    /// <summary>
    /// Конфигурация звуков для интеракции (подбор предметов и другие взаимодействия)
    /// </summary>
    [CreateAssetMenu(fileName = "InteractionAudioConfig", menuName = "DRAUM/Audio/Interaction Config")]
    public class InteractionAudioConfig : ScriptableObject
    {
        [System.Serializable]
        public class ItemPickupSounds
        {
            [Tooltip("Имя предмета (ItemData.name) для сопоставления")]
            public string itemName;
            
            [Tooltip("Конкретные звуки для этого предмета (если пусто, используется общий список)")]
            public AudioClip[] specificClips;
            
            [Range(0f, 2f)]
            [Tooltip("Громкость звука подбора для этого предмета")]
            public float volume = 1f;
        }
        
        [Header("Pickup Sounds")]
        [Tooltip("Звуки подбора предметов по умолчанию (используются если предмет не найден в itemPickupSounds)")]
        public AudioClip[] defaultPickupClips;
        
        [Tooltip("Звуки подбора для конкретных предметов")]
        public ItemPickupSounds[] itemPickupSounds;
        
        [Range(0f, 2f)]
        [Tooltip("Громкость звуков подбора по умолчанию")]
        public float defaultPickupVolume = 0.8f;
        
        [Header("Other Interaction Sounds")]
        [Tooltip("Звуки для кнопок")]
        public AudioClip[] buttonClips;
        
        [Tooltip("Звуки для рычагов")]
        public AudioClip[] leverClips;
        
        [Tooltip("Звуки для дверей")]
        public AudioClip[] doorClips;
        
        [Tooltip("Звуки для контейнеров")]
        public AudioClip[] containerClips;
        
        [Range(0f, 2f)]
        [Tooltip("Громкость звуков других взаимодействий")]
        public float defaultInteractionVolume = 0.7f;
    }
}
