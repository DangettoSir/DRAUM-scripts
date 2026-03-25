using UnityEngine;
using UnityEngine.Events;
using DRAUM.Core.Infrastructure.Logger;

/// <summary>
/// Типы взаимодействия с объектами
/// </summary>
public enum InteractionType
{
    Pickable,
    Button,
    Lever,
    Door,
    Container,
    Brutality,
    Custom
}

/// <summary>
/// Универсальный скрипт для всех интерактивных объектов
/// </summary>
public class InteractableObject : MonoBehaviour
{
    [Header("Interaction Settings")]
    public InteractionType interactionType = InteractionType.Pickable;
    public string interactionName = "Взаимодействовать";

    [Header("Pickable Settings")]
    public ItemData itemData;
    [Tooltip("Legacy поле из старых скриптов (NEED FIX/InteractableObject2). Используется как fallback если itemData не задан.")]
    public string legacyItemName;
    public Inventory inventorySystem;

    [Header("Button Settings")]
    public bool canPressMultipleTimes = false;
    public float buttonCooldown = 1f;
    private float lastPressTime = -999f;

    [Header("Button Events")]
    public UnityEvent onButtonPressed;


    [Header("Lever Settings")]
    public bool isLeverActivated = false;
    public Transform leverTransform;
    public Vector3 leverActivatedRotation = new Vector3(-45f, 0f, 0f);
    public Vector3 leverDeactivatedRotation = new Vector3(45f, 0f, 0f);
    public float leverRotationSpeed = 5f;

    private Quaternion leverTargetRotation;

    [Header("Lever Events")]
    public UnityEvent onLeverActivated;
    public UnityEvent onLeverDeactivated;

    [Header("Door Settings")]
    public bool isDoorOpen = false;

    [Header("Door Events")]
    public UnityEvent onDoorOpened;
    public UnityEvent onDoorClosed;

    [Header("Custom Events")]
    public UnityEvent onCustomInteract;

    void Start()
    {
        if (interactionType == InteractionType.Pickable && inventorySystem == null)
        {
            DRAUM.Modules.Inventory.InventoryModule inventoryModule = 
                UnityEngine.Object.FindFirstObjectByType<DRAUM.Modules.Inventory.InventoryModule>();
            
            if (inventoryModule != null)
            {
                if (inventoryModule.inventory != null)
                {
                    inventorySystem = inventoryModule.inventory;
                    DraumLogger.Info(this, $"[InteractableObject] {name}: Inventory найден через InventoryModule (основной: {inventorySystem.name})");
                }
                else
                {
                    var allInventories = inventoryModule.GetAllInventories();
                    if (allInventories != null && allInventories.Length > 0)
                    {
                        inventorySystem = allInventories[0];
                        DraumLogger.Info(this, $"[InteractableObject] {name}: Inventory найден через InventoryModule (первый из списка: {inventorySystem.name})");
                    }
                }
            }
            
            if (inventorySystem == null)
            {
                Inventory[] allInventories = UnityEngine.Object.FindObjectsByType<Inventory>(FindObjectsSortMode.None);
                foreach (Inventory inv in allInventories)
                {
                    if (inv.name.Contains("Equipment", System.StringComparison.OrdinalIgnoreCase) || 
                        inv.name.Contains("equipment", System.StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    
                    inventorySystem = inv;
                    DraumLogger.Info(this, $"[InteractableObject] {name}: Inventory найден напрямую: {inv.name}");
                    break;
                }
                
                if (inventorySystem == null)
                {
                    DraumLogger.Warning(this, $"[InteractableObject] {name}: Inventory не найден! Назначь вручную в Inspector или добавь в InventoryModule.");
                }
            }
        }

        if (interactionType == InteractionType.Lever && leverTransform != null)
        {
            leverTargetRotation = Quaternion.Euler(
                isLeverActivated ? leverActivatedRotation : leverDeactivatedRotation
            );
            leverTransform.localRotation = leverTargetRotation;
        }
    }

    void Update()
    {
        if (interactionType == InteractionType.Lever && leverTransform != null)
        {
            leverTransform.localRotation = Quaternion.Slerp(
                leverTransform.localRotation,
                leverTargetRotation,
                leverRotationSpeed * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// Вызывается из FirstPersonController когда игрок нажимает F
    /// </summary>
    public void TryInteract()
    {
        switch (interactionType)
        {
            case InteractionType.Pickable:
                HandlePickable();
                break;

            case InteractionType.Button:
                HandleButton();
                break;

            case InteractionType.Lever:
                HandleLever();
                break;

            case InteractionType.Door:
                HandleDoor();
                break;

            case InteractionType.Container:
                HandleContainer();
                break;

            case InteractionType.Brutality:
                HandleBrutality();
                break;

            case InteractionType.Custom:
                HandleCustom();
                break;
        }
    }

    private void HandlePickable()
    {
        if (itemData == null && !string.IsNullOrWhiteSpace(legacyItemName))
        {
            // Fallback для старых префабов: пытаемся подхватить ItemData из Resources по имени.
            itemData = Resources.Load<ItemData>(legacyItemName);
            if (itemData != null)
            {
                DraumLogger.Info(this, $"[InteractableObject] {name}: itemData подхвачен из legacyItemName={legacyItemName}");
            }
        }

        if (itemData == null)
        {
            DraumLogger.Error(this, $"[InteractableObject] {name}: ItemData не назначен! Назначь ScriptableObject в Inspector.");
            return;
        }

        Inventory targetInventory = null;
        
        if (inventorySystem != null)
        {
            bool canAccept = false;
            if (inventorySystem.grids != null)
            {
                foreach (var grid in inventorySystem.grids)
                {
                    if (grid != null && inventorySystem.CanPlaceItemInGrid(itemData, grid))
                    {
                        canAccept = true;
                        break;
                    }
                }
            }
            
            if (canAccept)
            {
                targetInventory = inventorySystem;
                DraumLogger.Info(this, $"[InteractableObject] {name}: Используется назначенный инвентарь: {inventorySystem.name}");
            }
            else
            {
                DraumLogger.Warning(this, $"[InteractableObject] {name}: Назначенный инвентарь {inventorySystem.name} не может принять предмет {itemData.name} (не проходит фильтры категорий). Ищем подходящий...");
            }
        }
        
        if (targetInventory == null)
        {
            DRAUM.Modules.Inventory.InventoryModule inventoryModule = 
                UnityEngine.Object.FindFirstObjectByType<DRAUM.Modules.Inventory.InventoryModule>();
            
            if (inventoryModule != null)
            {
                targetInventory = inventoryModule.FindSuitableInventory(itemData);
                
                if (targetInventory != null)
                {
                    DraumLogger.Info(this, $"[InteractableObject] {name}: Найден подходящий инвентарь через InventoryModule: {targetInventory.name}");
                }
                else
                {
                    DraumLogger.Warning(this, $"[InteractableObject] {name}: InventoryModule не нашёл подходящий инвентарь для {itemData.name} (нет свободных слотов или не проходит фильтры)");
                }
            }
            else
            {
                DraumLogger.Error(this, $"[InteractableObject] {name}: InventoryModule не найден! Назначь Inventory вручную в Inspector.");
            }
        }
        
        if (targetInventory != null)
        {
            bool success = targetInventory.AddItem(itemData);
            
            if (success)
            {
                DraumLogger.Info(this, $"[InteractableObject] Подобран предмет: {itemData.name} → {targetInventory.name}");
                Destroy(gameObject);
            }
            else
            {
                DraumLogger.Warning(this, $"[InteractableObject] Не удалось добавить {itemData.name} в {targetInventory.name} (инвентарь полон)");
            }
        }
        else
        {
            DraumLogger.Error(this, $"[InteractableObject] {name}: Не найден подходящий инвентарь для {itemData.name}!");
        }
    }

    private void HandleButton()
    {
        if (!canPressMultipleTimes && Time.time - lastPressTime < buttonCooldown)
        {
            DraumLogger.Info(this, $"[InteractableObject] Кнопка на cooldown!");
            return;
        }

        lastPressTime = Time.time;
        onButtonPressed?.Invoke();
        DraumLogger.Info(this, $"[InteractableObject] Кнопка нажата: {interactionName}");

        if (!canPressMultipleTimes)
        {
            DraumLogger.Info(this, $"[InteractableObject] Кнопка деактивирована (одноразовая)");
        }
    }

    private void HandleLever()
    {
        isLeverActivated = !isLeverActivated;

        leverTargetRotation = Quaternion.Euler(
            isLeverActivated ? leverActivatedRotation : leverDeactivatedRotation
        );

        if (isLeverActivated)
        {
            onLeverActivated?.Invoke();
            DraumLogger.Info(this, $"[InteractableObject] Рычаг активирован: {interactionName}");
        }
        else
        {
            onLeverDeactivated?.Invoke();
            DraumLogger.Info(this, $"[InteractableObject] Рычаг деактивирован: {interactionName}");
        }
    }

    private void HandleDoor()
    {
        isDoorOpen = !isDoorOpen;

        if (isDoorOpen)
        {
            onDoorOpened?.Invoke();
            DraumLogger.Info(this, $"[InteractableObject] Дверь открыта: {interactionName}");
        }
        else
        {
            onDoorClosed?.Invoke();
            DraumLogger.Info(this, $"[InteractableObject] Дверь закрыта: {interactionName}");
        }
    }

    private void HandleContainer()
    {
        DraumLogger.Info(this, $"[InteractableObject] Container interaction (not implemented yet): {interactionName}");
    }

    private void HandleBrutality()
    {
        EnemyEntity enemyEntity = GetComponent<EnemyEntity>();
        if (enemyEntity == null)
        {
            enemyEntity = GetComponentInParent<EnemyEntity>();
        }
        
        if (enemyEntity == null)
        {
            DraumLogger.Error(this, "[InteractableObject] HandleBrutality: EnemyEntity не найден!");
            return;
        }
        
        DRAUM.Modules.Combat.BrutalityInteractable brutalityInteractable = GetComponent<DRAUM.Modules.Combat.BrutalityInteractable>();
        if (brutalityInteractable == null)
        {
            brutalityInteractable = GetComponentInParent<DRAUM.Modules.Combat.BrutalityInteractable>();
        }
        
        DRAUM.Modules.Combat.Animation.BrutalityController brutalityController = null;
        
        if (brutalityInteractable != null && brutalityInteractable.brutalityController != null)
        {
            brutalityController = brutalityInteractable.brutalityController;
        }
        else
        {
            brutalityController = FindFirstObjectByType<DRAUM.Modules.Combat.Animation.BrutalityController>();
        }
        
        if (brutalityController == null)
        {
            DraumLogger.Error(this, "[InteractableObject] HandleBrutality: BrutalityController не найден! Убедитесь что BrutalityController добавлен в сцену и назначен в BrutalityInteractable.");
            return;
        }
        
        brutalityController.ExecuteBrutality(enemyEntity);
    }

    private void HandleCustom()
    {
        if (onCustomInteract == null)
        {
            DraumLogger.Warning(this, $"[InteractableObject] Custom interaction has no UnityEvent assigned on {name}");
            return;
        }

        onCustomInteract?.Invoke();
        DraumLogger.Info(this, $"[InteractableObject] Custom interaction: {interactionName}");
    }

    public void ResetButton()
    {
        lastPressTime = -999f;
    }

    public void SetLeverState(bool activated)
    {
        isLeverActivated = activated;
        leverTargetRotation = Quaternion.Euler(
            isLeverActivated ? leverActivatedRotation : leverDeactivatedRotation
        );
    }

    public void SetDoorState(bool open)
    {
        isDoorOpen = open;
    }

    /// <summary>
    /// Возвращает имя взаимодействия для UI
    /// </summary>
    public string GetInteractionName()
    {
        return interactionName;
    }

    /// <summary>
    /// Legacy-compatible имя предмета для старых UI/селекторов.
    /// </summary>
    public string GetItemName()
    {
        if (itemData != null && !string.IsNullOrWhiteSpace(itemData.name))
            return itemData.name;
        if (!string.IsNullOrWhiteSpace(legacyItemName))
            return legacyItemName;
        return interactionName;
    }
    
    /// <summary>
    /// Проверяет доступно ли взаимодействие для отображения в UI
    /// </summary>
    public bool IsInteractionAvailable()
    {
        if (string.IsNullOrEmpty(interactionName))
        {
            return false;
        }

        if (interactionType == InteractionType.Custom && string.IsNullOrEmpty(interactionName))
        {
            return false;
        }
        
        if (interactionType == InteractionType.Brutality && string.IsNullOrEmpty(interactionName))
        {
            return false;
        }
        
        return true;
    }
}

