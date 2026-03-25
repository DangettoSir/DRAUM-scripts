using UnityEngine;

/// <summary>
/// Режим работы слота
/// </summary>
public enum SlotMode
{
    Normal,
    Equipment,
    Consumable
}

/// <summary>
/// Настройки для конкретного InventoryGrid
/// Хранится в Inventory.cs
/// </summary>
[System.Serializable]
public class InventoryGridSettings
{
    [Header("Grid Reference")]
    [Tooltip("Grid который настраиваем")]
    public InventoryGrid grid;
    
    [Header("Slot Mode")]
    [Tooltip("Режим работы слотов:\n" +
             "Normal - обычные слоты (учитывается размер)\n" +
             "Equipment - экипировка (размер игнорируется, 1 предмет = 1 слот)\n" +
             "Consumable - расходники (размер игнорируется, 1 предмет = 1 слот)")]
    public SlotMode slotMode = SlotMode.Normal;
    
    [Header("Visual Settings")]
    [Tooltip("Скорость появления/исчезновения (переопределяет глобальную)")]
    [Range(1f, 100f)]
    public float customFadeSpeed = 5f;
    
    [Tooltip("Использовать кастомную скорость вместо глобальной")]
    public bool useCustomSpeed = false;
    
    [Header("Category Filter")]
    [Tooltip("Режим фильтрации категорий")]
    public CategoryFilterMode filterMode = CategoryFilterMode.AllowAll;
    
    [Tooltip("Список категорий для фильтра (зависит от режима)")]
    public ItemCategory[] filterCategories = new ItemCategory[0];
    
    /// <summary>
    /// Проверяет, может ли предмет быть помещен в этот Grid
    /// </summary>
    public bool CanAcceptItem(ItemData itemData)
    {
        if (itemData == null) return false;
        
        switch (filterMode)
        {
            case CategoryFilterMode.AllowAll:
                return true;
                
            case CategoryFilterMode.AllowOnly:
                return itemData.HasAnyCategory(filterCategories);
                
            case CategoryFilterMode.DenyOnly:
                if (filterCategories == null || filterCategories.Length == 0) return true;
                return !itemData.HasAnyCategory(filterCategories);
                
            default:
                return true;
        }
    }
    
    /// <summary>
    /// Проверяет, игнорируется ли размер предмета в этом Grid (для Equipment/Consumable)
    /// </summary>
    public bool IgnoreItemSize()
    {
        return slotMode == SlotMode.Equipment || slotMode == SlotMode.Consumable;
    }
    
    /// <summary>
    /// Проверяет, является ли Grid неизменяемым (Equipment/Consumable)
    /// </summary>
    public bool IsImmutable()
    {
        return slotMode == SlotMode.Equipment || slotMode == SlotMode.Consumable;
    }
}

/// <summary>
/// Режим фильтрации категорий для Grid'а
/// </summary>
public enum CategoryFilterMode
{
    AllowAll,
    AllowOnly,
    DenyOnly
}

