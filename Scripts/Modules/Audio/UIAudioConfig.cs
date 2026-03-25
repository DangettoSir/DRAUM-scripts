using UnityEngine;

namespace DRAUM.Modules.Audio
{
    /// <summary>
    /// Конфигурация звуков для UI и инвентаря
    /// </summary>
    [CreateAssetMenu(fileName = "UIAudioConfig", menuName = "DRAUM/Audio/UI Config")]
    public class UIAudioConfig : ScriptableObject
    {
        [System.Serializable]
        public class InventorySounds
        {
            [Header("Backpack")]
            [Tooltip("Звук открытия рюкзака")]
            public AudioClip backpackOpenClip;
            
            [Tooltip("Звук закрытия рюкзака")]
            public AudioClip backpackCloseClip;
            
            [Header("Section Transitions")]
            [Tooltip("Звуки перехода между секциями инвентаря (WASD)")]
            public AudioClip[] sectionTransitionClips;
            
            [Header("Mouse Interactions")]
            [Tooltip("Звуки ЛКМ в инвентаре (выбор предмета, перемещение)")]
            public AudioClip[] leftClickClips;
            
            [Tooltip("Звуки ПКМ в инвентаре (контекстное меню, действия)")]
            public AudioClip[] rightClickClips;
        }
        
        [Header("Inventory")]
        public InventorySounds inventorySounds;
        
        [Header("Volume Settings")]
        [Range(0f, 2f)]
        [Tooltip("Громкость звуков открытия/закрытия рюкзака")]
        public float backpackVolume = 0.8f;
        
        [Range(0f, 2f)]
        [Tooltip("Громкость звуков перехода между секциями")]
        public float sectionTransitionVolume = 0.6f;
        
        [Range(0f, 2f)]
        [Tooltip("Громкость звуков кликов мыши")]
        public float clickVolume = 0.5f;
    }
}
