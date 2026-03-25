using UnityEngine;
using System.Collections;

/// <summary>
/// Управляет экипировкой оружия в 3 слотах (First, Second, Third)
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    [Header("Equipment Slots")]
    [Tooltip("Слот экипировки 1 (назначается в сцене)")]
    public EquipmentSlot slotFirst;
    
    [Tooltip("Слот экипировки 2 (назначается в сцене)")]
    public EquipmentSlot slotSecond;
    
    [Tooltip("Слот экипировки 3 (назначается в сцене, опционально)")]
    public EquipmentSlot slotThird;
    
    [Header("Debug")]
    [HideInInspector] public bool showDebugLogs = false;
    
    private EquipmentSlot currentEquippedSlot = null;
    
    /// <summary>
    /// Текущий экипированный слот (null если ничего не экипировано)
    /// </summary>
    public EquipmentSlot CurrentEquippedSlot => currentEquippedSlot;
    
    /// <summary>
    /// Проверяет, экипировано ли что-то
    /// </summary>
    public bool IsEquipped => currentEquippedSlot != null && currentEquippedSlot.IsEquipped;
    
    /// <summary>
    /// Переключает экипировку в указанном слоте
    /// Проверяет инвентарь: если предмет есть в соответствующем Equipment слоте - экипирует, иначе снимает
    /// </summary>
    public void ToggleEquipSlot(EquipmentSlot slot)
    {
        if (slot == null)
        {
            if (showDebugLogs) Debug.LogWarning("[EquipmentManager] Попытка переключить NULL слот!");
            return;
        }
        
        bool hasItem = slot.HasItemInInventory();
        
        if (showDebugLogs)
        {
            Item item = slot.GetItemFromInventory();
            Debug.Log($"[EquipmentManager] ToggleEquipSlot для {slot.name}: hasItem={hasItem}, item={(item != null ? item.data?.name : "NULL")}, isEquipped={slot.IsEquipped}");
        }
        
        if (!hasItem && slot.IsEquipped)
        {
            if (showDebugLogs) Debug.Log($"[EquipmentManager] Предмета нет в инвентаре, снимаем экипировку со {slot.name}");
            UnequipSlot(slot);
            return;
        }
        
        if (!hasItem && !slot.IsEquipped)
        {
            if (showDebugLogs) Debug.Log($"[EquipmentManager] Предмета нет в инвентаре и слот не экипирован - ничего не делаем");
            return;
        }
        
        if (currentEquippedSlot == slot && slot.IsEquipped)
        {
            if (showDebugLogs) Debug.Log($"[EquipmentManager] Снимаем экипировку со {slot.name} (повторное нажатие)");
            UnequipSlot(slot);
        }
        else if (currentEquippedSlot != slot)
        {
            if (currentEquippedSlot != null && currentEquippedSlot.IsEquipped)
            {
                if (showDebugLogs) Debug.Log($"[EquipmentManager] Снимаем {currentEquippedSlot.name}, экипируем {slot.name}");
                UnequipSlot(currentEquippedSlot);
            }
            
            EquipSlot(slot);
        }
        else
        {
            if (showDebugLogs) Debug.Log($"[EquipmentManager] Экипируем {slot.name}");
            EquipSlot(slot);
        }
    }
    
    /// <summary>
    /// Экипирует указанный слот
    /// </summary>
    private void EquipSlot(EquipmentSlot slot)
    {
        if (slot == null || slot.IsEquipped) return;
        
        if (showDebugLogs) Debug.Log($"[EquipmentManager] Экипируем слот: {slot.name}");
        
        slot.Equip();
        currentEquippedSlot = slot;
    }
    
    /// <summary>
    /// Снимает экипировку с указанного слота
    /// </summary>
    private void UnequipSlot(EquipmentSlot slot)
    {
        if (slot == null || !slot.IsEquipped) return;
        
        if (showDebugLogs) Debug.Log($"[EquipmentManager] Снимаем экипировку со слота: {slot.name}");
        
        slot.Unequip();
        
        if (currentEquippedSlot == slot)
        {
            currentEquippedSlot = null;
        }
    }
    
    /// <summary>
    /// Вызывается слотом после завершения анимации Unequip
    /// </summary>
    public void OnSlotUnequipped(EquipmentSlot slot)
    {
        if (showDebugLogs) Debug.Log($"[EquipmentManager] Слот {slot.name} успешно снят");
        
        if (currentEquippedSlot == slot)
        {
            currentEquippedSlot = null;
        }
    }
}

