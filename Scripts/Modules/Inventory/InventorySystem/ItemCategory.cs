using UnityEngine;

/// <summary>
/// Категория предметов (ScriptableObject)
/// Используется для фильтрации предметов по Grid'ам
/// </summary>
[CreateAssetMenu(fileName = "New Item Category", menuName = "Inventory/Item Category")]
public class ItemCategory : ScriptableObject
{
    [Header("Category Info")]
    [Tooltip("Название категории")]
    public string categoryName = "New Category";
    
    [Tooltip("Описание категории")]
    [TextArea(2, 5)]
    public string description = "";
    
    [Tooltip("Иконка категории (опционально)")]
    public Sprite icon;
    
    [Header("Visual")]
    [Tooltip("Цвет категории (для UI подсветки)")]
    public Color categoryColor = Color.white;
    
    [Header("Interactions (Future)")]
    [Tooltip("Кастомные взаимодействия с предметами этой категории")]
    public string[] customInteractions = new string[0];
    
    public override string ToString()
    {
        return categoryName;
    }
}

