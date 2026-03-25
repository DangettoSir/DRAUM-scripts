using UnityEngine;
using System.Collections;

/// <summary>
/// Представляет один слот экипировки (оружие с аниматором и FPHands)
/// </summary>
public class EquipmentSlot : MonoBehaviour
{
    [Header("Inventory Integration")]
    [Tooltip("Ссылка на Inventory (автопоиск если не назначен)")]
    public Inventory inventory;
    
    [Tooltip("Индекс Equipment слота в инвентаре (0 = первый слот, 1 = второй, 2 = третий)")]
    [Range(0, 2)]
    public int equipmentSlotIndex = 0;
    
    [Header("Weapon Settings")]
    [Tooltip("Аниматор оружия (должен иметь триггеры Equip/Unequip)")]
    public Animator weaponAnimator;
    
    [Tooltip("FPHands объект (включается/выключается вместе с экипировкой)")]
    public GameObject fpHands;
    
    [Tooltip("Скрипт оружия (FPMAxe или другой)")]
    public MonoBehaviour weaponScript;
    
    [Header("Animation Settings")]
    [Tooltip("Длительность анимации Unequip в секундах (для автоматического выключения)")]
    public float unequipAnimationDuration = 1.0f;
    
    [Header("Debug")]
    [HideInInspector] public bool showDebugLogs = false;
    
    private EquipmentManager equipmentManager;
    private bool isEquipped = false;
    private bool isTransitioning = false;
    
    /// <summary>
    /// Проверяет, экипирован ли слот
    /// </summary>
    public bool IsEquipped => isEquipped;
    
    /// <summary>
    /// Проверяет, идет ли переход (Equip/Unequip)
    /// </summary>
    public bool IsTransitioning => isTransitioning;
    
    /// <summary>
    /// Предмет из инвентаря, который экипирован в этом слоте (null если ничего не экипировано)
    /// </summary>
    public Item EquippedItem { get; private set; }
    
    private void Awake()
    {
        equipmentManager = GetComponentInParent<EquipmentManager>();
        if (equipmentManager == null)
        {
            equipmentManager = FindFirstObjectByType<EquipmentManager>();
        }
        
        if (equipmentManager == null && showDebugLogs)
        {
            Debug.LogWarning($"[EquipmentSlot] EquipmentManager не найден для {name}!");
        }
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventory>();
        }
        
        if (inventory == null && showDebugLogs)
        {
            Debug.LogWarning($"[EquipmentSlot] Inventory не найден для {name}!");
        }
        
        SetEquipmentActive(false);
    }
    
    /// <summary>
    /// Проверяет, есть ли предмет в соответствующем Equipment слоте инвентаря
    /// </summary>
    public bool HasItemInInventory()
    {
        if (inventory == null) return false;
        
        InventoryGrid equipmentGrid;
        Item item = inventory.GetItemFromEquipmentSlot(equipmentSlotIndex, out equipmentGrid);
        
        return item != null;
    }
    
    /// <summary>
    /// Получает предмет из соответствующего Equipment слота инвентаря
    /// </summary>
    public Item GetItemFromInventory()
    {
        if (inventory == null) return null;
        
        InventoryGrid equipmentGrid;
        Item item = inventory.GetItemFromEquipmentSlot(equipmentSlotIndex, out equipmentGrid);
        
        return item;
    }
    
    /// <summary>
    /// Экипирует слот (запускает анимацию Equip)
    /// </summary>
    public void Equip()
    {
        if (isEquipped || isTransitioning)
        {
            if (showDebugLogs) Debug.LogWarning($"[EquipmentSlot] {name} уже экипирован или идет переход!");
            return;
        }
        
        Item item = GetItemFromInventory();
        if (item == null)
        {
            if (showDebugLogs) Debug.LogWarning($"[EquipmentSlot] {name}: Нет предмета в Equipment слоте {equipmentSlotIndex} инвентаря!");
            return;
        }
        
        EquippedItem = item;
        
        if (showDebugLogs) Debug.Log($"[EquipmentSlot] Запускаем Equip для {name} (предмет: {item.data?.name ?? "NULL"})");
        
        isTransitioning = true;
        
        SetEquipmentActive(true);
        
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("Equip");
        }
        
        StartCoroutine(WaitForEquipAnimation());
    }
    
    /// <summary>
    /// Снимает экипировку (запускает анимацию Unequip)
    /// </summary>
    public void Unequip()
    {
        if (!isEquipped || isTransitioning)
        {
            if (showDebugLogs) Debug.LogWarning($"[EquipmentSlot] {name} не экипирован или идет переход!");
            return;
        }
        
        if (showDebugLogs) Debug.Log($"[EquipmentSlot] Запускаем Unequip для {name}");
        
        isTransitioning = true;
        
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("Unequip");
        }

        StartCoroutine(WaitForUnequipAnimation());
    }
    
    /// <summary>
    /// Устанавливает активность оружия и FPHands
    /// </summary>
    private void SetEquipmentActive(bool active)
    {
        if (weaponAnimator != null)
        {
            weaponAnimator.enabled = active;
            if (active)
            {
                weaponAnimator.gameObject.SetActive(true);
            }
        }
        
        if (fpHands != null)
        {
            fpHands.SetActive(active);
        }
        
        if (weaponScript != null)
        {
            weaponScript.enabled = active;
        }
    }
    
    /// <summary>
    /// Ждет завершения анимации Equip
    /// </summary>
    private IEnumerator WaitForEquipAnimation()
    {
        yield return null;
        float elapsed = 0f;
        float waitTime = 1.0f;
        
        while (elapsed < waitTime && weaponAnimator != null)
        {
            if (weaponAnimator.GetCurrentAnimatorStateInfo(0).IsName("Equip"))
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            else if (weaponAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.95f)
            {
                break;
            }
            else
            {
                yield return null;
            }
        }
        
        isEquipped = true;
        isTransitioning = false;
        
        if (weaponScript != null)
        {
            var method = weaponScript.GetType().GetMethod("OnEquipped", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(weaponScript, null);
            }
        }
        
        if (showDebugLogs) Debug.Log($"[EquipmentSlot] {name} экипирован!");
    }
    
    /// <summary>
    /// Ждет завершения анимации Unequip и выключает всё
    /// </summary>
    private IEnumerator WaitForUnequipAnimation()
    {
        yield return null;
        
        float elapsed = 0f;
        float waitTime = unequipAnimationDuration;

        while (elapsed < waitTime && weaponAnimator != null)
        {
            if (weaponAnimator.GetCurrentAnimatorStateInfo(0).IsName("Unequip"))
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            else if (weaponAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.95f)
            {
                break;
            }
            else
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        
        if (weaponScript != null)
        {
            var method = weaponScript.GetType().GetMethod("OnUnequipped", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(weaponScript, null);
            }
        }
        
        SetEquipmentActive(false);
        
        if (weaponAnimator != null)
        {
            weaponAnimator.gameObject.SetActive(false);
        }
        
        isEquipped = false;
        isTransitioning = false;
        EquippedItem = null;

        if (equipmentManager != null)
        {
            equipmentManager.OnSlotUnequipped(this);
        }
        
        if (showDebugLogs) Debug.Log($"[EquipmentSlot] {name} снят и выключен!");
    }
    
    /// <summary>
    /// Метод для вызова из анимации через Animation Event
    /// </summary>
    public void OnEquipAnimationFinished()
    {
        if (showDebugLogs) Debug.Log($"[EquipmentSlot] Анимация Equip завершена для {name}");
        isEquipped = true;
        isTransitioning = false;
    }
    
    /// <summary>
    /// Метод для вызова из анимации через Animation Event
    /// </summary>
    public void OnUnequipAnimationFinished()
    {
        if (showDebugLogs) Debug.Log($"[EquipmentSlot] Анимация Unequip завершена для {name}");
        
        SetEquipmentActive(false);
        
        if (weaponAnimator != null)
        {
            weaponAnimator.gameObject.SetActive(false);
        }
        
        isEquipped = false;
        isTransitioning = false;
        
        if (equipmentManager != null)
        {
            equipmentManager.OnSlotUnequipped(this);
        }
    }
}

