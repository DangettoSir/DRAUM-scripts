using UnityEngine;

[System.Serializable]
public class GridFadeSettings
{
    [Tooltip("Grid который настраиваем")]
    public InventoryGrid grid;
    
    [Tooltip("Скорость появления/исчезновения (переопределяет gridFadeSpeed) [DEPRECATED: используй Inventory.gridSettings]")]
    [Range(1f, 100f)]
    public float customFadeSpeed = 5f;
    
    [Tooltip("Использовать кастомную скорость вместо глобальной")]
    public bool useCustomSpeed = false;
}

