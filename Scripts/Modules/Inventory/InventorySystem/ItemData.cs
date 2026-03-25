using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DRAUM.Modules.Player.Effects;

[CreateAssetMenu(fileName = "ItemData", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    /// <summary>
    /// Размер предмета по ширине и высоте.
    /// </summary>
    [Header("Main")]
    public SizeInt size = new();

    /// <summary>
    /// Цвет фона предмета.
    /// </summary>
    [Header("Visual")]
    public Color backgroundColor;
    
    /// <summary>
    /// Описание предмета (для tooltip)
    /// </summary>
    [Header("Tooltip")]
    [TextArea(3, 10)]
    public string description = "Описание предмета";
    
    /// <summary>
    /// Категории предмета (можно выбрать несколько)
    /// Используется для фильтрации по Grid'ам
    /// </summary>
    [Header("Categories")]
    [Tooltip("Категории этого предмета (HolyTrinkets, DemonicTrinkets, Weapons, etc.)")]
    public ItemCategory[] categories = new ItemCategory[0];

    /// <summary>
    /// Имя слоя в Animator камеры (AC_Camera) для этого оружия. Например "Stick" — набор анимаций под дубины.
    /// Если пусто — слои оружия в камере не включаются.
    /// </summary>
    [Header("Camera Animation (Weapon)")]
    [Tooltip("Имя слоя в Animator камеры (например Stick). Пусто = слой не используется.")]
    public string cameraAnimationLayerName = "";

    /// <summary>
    /// Эффект при употреблении (Consumable). Если задан — при "Употребить" накладывается на игрока и предмет удаляется.
    /// </summary>
    [Header("Consumable")]
    [Tooltip("Эффект при употреблении (например Apple Effect = AttackSpeed). Создай ассет через DRAUM/Player/Player Effect Data.")]
    public PlayerEffectData consumableEffect;
    
    /// <summary>
    /// Префаб для показа в руках при использовании (например яблоко в jointApple). Спавнится при старте анимации, удаляется после завершения.
    /// </summary>
    [Tooltip("Префаб для показа в руках при использовании (например яблоко). Если пусто — префаб не показывается.")]
    public GameObject consumablePrefab;

    /// <summary>
    /// Имя анимационного состояния на FPHands, которое должно воспроизводиться при использовании расходника (например Beginning-Amulet).
    /// </summary>
    [Tooltip("Имя анимационного состояния на FPHands, которое должно воспроизводиться при использовании расходника (например Beginning-Amulet).")]
    public string consumableAnimationStateName = "IsConsuming"; // По умолчанию существующее состояние

    /// <summary>
    /// Способность, которая будет активирована при использовании этого расходника (например ForceGripAbility для амулета).
    /// </summary>
    [Tooltip("Имя события способности, которое будет опубликовано через EventBus при использовании этого расходника (например ForceGrip).")]
    public string activatedAbilityEventName;

    /// <summary>
    /// Для Weapon Oil: материал, который применится к палке (stick) после использования.
    /// Настрой в ItemData масла (например WeaponOil1) — выбери материал зачарования/масла.
    /// </summary>
    [Tooltip("Материал для stick при использовании масла. Если пусто — материал не меняется.")]
    public Material weaponOilMaterial;

    /// <summary>
    /// Опциональная 3D модель для отображения в World Space инвентаре.
    /// Если не назначена - показывается только иконка (2D Image).
    /// </summary>
    [Header("3D Model (Optional)")]
    [Tooltip("3D модель предмета для World Space. Если пусто - используется только иконка.")]
    public GameObject model3D;
    
    /// <summary>
    /// Масштаб 3D модели в инвентаре (исправляет разницу размеров)
    /// </summary>
    [Tooltip("Масштаб 3D модели в инвентаре (1 = нормальный размер)")]
    public Vector3 inventoryScale = Vector3.one;
    
    /// <summary>
    /// Позиция 3D модели в инвентаре (локальная позиция)
    /// </summary>
    [Tooltip("Позиция 3D модели в инвентаре (локальная позиция)")]
    public Vector3 inventoryPosition = Vector3.zero;
    
    /// <summary>
    /// Поворот 3D модели в инвентаре (Euler angles)
    /// </summary>
    [Tooltip("Поворот 3D модели в инвентаре (Euler angles)")]
    public Vector3 inventoryRotation = Vector3.zero;
    
    /// <summary>
    /// Проверяет, принадлежит ли предмет к указанной категории
    /// </summary>
    public bool HasCategory(ItemCategory category)
    {
        if (category == null || categories == null) return false;
        
        foreach (ItemCategory cat in categories)
        {
            if (cat == category) return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Проверяет, принадлежит ли предмет к любой из указанных категорий
    /// </summary>
    public bool HasAnyCategory(ItemCategory[] checkCategories)
    {
        if (checkCategories == null || checkCategories.Length == 0) return true;
        if (categories == null || categories.Length == 0) return false;
        
        foreach (ItemCategory checkCat in checkCategories)
        {
            if (checkCat != null && HasCategory(checkCat)) return true;
        }
        
        return false;
    }
}