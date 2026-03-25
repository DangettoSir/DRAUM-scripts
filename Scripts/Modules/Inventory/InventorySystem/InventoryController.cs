using System;
using UnityEngine;
using DRAUM.Core.Infrastructure.Logger;

[RequireComponent(typeof(Inventory))]
public class InventoryController : MonoBehaviour
{
    public Inventory inventory { get; private set; }
    
    [Header("Context Menu")]
    [Tooltip("InventoryContextMenuController для контекстного меню (автопоиск если не назначен)")]
    public InventoryContextMenuController contextMenuController;

    [Header("Settings")]
    [Tooltip("Работает только когда инвентарь открыт")]
    public bool requireInventoryOpen = true;

    [Header("World Space Settings")]
    [Tooltip("Camera для raycast (автоматически находит Main Camera)")]
    public Camera playerCamera;
    
    [Tooltip("Слой для raycast в World Space (только если Canvas = World Space)")]
    public LayerMask worldSpaceLayerMask = ~0;
    
    [Tooltip("Максимальная дистанция raycast")]
    public float maxRaycastDistance = 100f;
    
    [Tooltip("Показывать debug лучи")]
    public bool showDebugRays = false;
    
    [Header("Item Drag Offset")]
    [Tooltip("Смещение предмета по X относительно курсора при перетаскивании (в пикселях экрана, добавляется к базовому смещению)")]
    public float itemDragOffsetX = 0f;
    
    [Tooltip("Смещение предмета по Y относительно курсора при перетаскивании (в пикселях экрана, добавляется к базовому смещению -50, отрицательное значение = вниз)")]
    public float itemDragOffsetY = 0f;
    
    [Tooltip("Показывать debug информацию о позиционировании предмета")]
    public bool showDragDebug = false;
    
    private const float BASE_DRAG_OFFSET_Y = -50f;
    
    [Header("Item Rotation Settings")]
    [Tooltip("Включить поворот предмета при наведении на grid")]
    public bool enableItemRotation = true;
    
    [Tooltip("Скорость поворота предмета (Slerp)")]
    [Range(1f, 30f)]
    public float itemRotationSpeed = 10f;
    
    [Tooltip("Поворачивать предмет к камере когда наводим на мир")]
    public bool rotateTowardsCameraInWorld = true;
    
    [Tooltip("Скорость поворота к камере")]
    [Range(1f, 30f)]
    public float cameraRotationSpeed = 10f;
    
    [Header("Item Placement Physics")]
    [Tooltip("Использовать физику при размещении в мир (предмет падает)")]
    public bool usePhysicsPlacement = true;
    
    [Tooltip("Сила импульса от камеры (направление взгляда)")]
    [Range(0f, 10f)]
    public float throwForce = 2f;
    
    [Tooltip("Высота над точкой raycast для размещения")]
    [Range(0f, 2f)]
    public float placementHeight = 0.5f;
    
    [Tooltip("Дистанция выброса от камеры (минимальная дистанция)")]
    [Range(0.5f, 10f)]
    public float throwDistanceFromCamera = 2f;

    private Canvas canvas;
    private bool isWorldSpace;
    
    /// <summary>
    /// Вызывается при загрузке экземпляра скрипта.
    /// </summary>
    private void Awake()
    {
        inventory = GetComponent<Inventory>();
        
        if (contextMenuController == null)
        {
            contextMenuController = FindFirstObjectByType<InventoryContextMenuController>();
        }
        
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            isWorldSpace = canvas.renderMode == RenderMode.WorldSpace;
            DraumLogger.Info(this, $"[InventoryController] Canvas type: {(isWorldSpace ? "World Space" : "Screen Space")}");
        }
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    /// <summary>
    /// Вызывается каждый кадр, если `MonoBehaviour` включён.
    /// </summary>
    private void Update()
    {
        if (requireInventoryOpen)
        {
            if (canvas != null && !canvas.gameObject.activeSelf)
            {
                return;
            }
        }
        
        InventoryCameraController cameraController = UnityEngine.Object.FindFirstObjectByType<InventoryCameraController>();
        bool isTransitioning = (cameraController != null && cameraController.enabled && 
                               (cameraController.currentSection != cameraController.targetSection));
        
        if (isTransitioning)
        {
            return;
        }

        if (isWorldSpace)
        {
            UpdateGridOnMouseWorldSpace();
        }

        if (Input.GetMouseButtonDown(0))
        {
            bool contextMenuOpen = contextMenuController != null && 
                                   contextMenuController.contextMenuPlane != null && 
                                   contextMenuController.contextMenuPlane.activeSelf;
            
            if (contextMenuOpen)
            {
                bool clickedOnContextMenu = IsPointerOverContextMenu();
                
                if (clickedOnContextMenu)
                {
                    if (showDebugRays)
                    {
                        DraumLogger.Info(this, "[InventoryController] Клик по открытому контекстному меню - пропускаем обработку");
                    }
                    return;
                }
            }
            else
            {
                if (IsPointerOverUI())
                {
                    if (showDebugRays)
                    {
                        DraumLogger.Info(this, "[InventoryController] Клик по UI элементу - пропускаем обработку");
                    }
                    return;
                }
            }
            
            DraumLogger.Info(this, "[InventoryController] ЛКМ НАЖАТА!");
            
            if (DRAUM.Core.EventBus.Instance != null)
            {
                DRAUM.Core.EventBus.Instance.Publish(new DRAUM.Modules.Player.Events.UISoundEvent 
                { 
                    SoundType = DRAUM.Modules.Player.Events.UISoundType.LeftClick,
                    Context = "Inventory"
                });
            }
            
            if (inventory.gridOnMouse == null)
            {
                DraumLogger.Info(this, "[InventoryController] gridOnMouse = NULL");
                
                if (inventory.selectedItem != null)
                {
                    PlaceItemInWorld(inventory.selectedItem);
                }
                return;
            }

            DraumLogger.Info(this, $"[InventoryController] gridOnMouse = {inventory.gridOnMouse.name}");
            
            Vector2Int slotPos = inventory.GetSlotAtMouseCoords();
            DraumLogger.Info(this, $"[InventoryController] Slot Position: {slotPos}");
            
            bool outOfBounds = inventory.ReachedBoundary(slotPos, inventory.gridOnMouse);
            DraumLogger.Info(this, $"[InventoryController] Out of bounds: {outOfBounds}");

            if (!outOfBounds)
            {
                DraumLogger.Info(this, "[InventoryController] Внутри границ, обрабатываем клик");
                
                if (inventory.selectedItem)
                {
                    DraumLogger.Info(this, $"[InventoryController] Есть выбранный предмет: {inventory.selectedItem.name}");
                    Item oldSelectedItem = inventory.selectedItem;
                    Item overlapItem = inventory.GetItemAtMouseCoords();

                    if (overlapItem != null)
                    {
                        DraumLogger.Info(this, $"[InventoryController] Swap с {overlapItem.name}");
                        inventory.SwapItem(overlapItem, oldSelectedItem);
                    }
                    else
                    {
                        DraumLogger.Info(this, "[InventoryController] Перемещение предмета");
                        inventory.MoveItem(oldSelectedItem);
                    }
                }
                else
                {
                    DraumLogger.Info(this, "[InventoryController] Попытка выбрать предмет");
                    
                    Item itemAtMouse = inventory.GetItemAtMouseCoords();
                    if (itemAtMouse != null)
                    {
                        InventoryGridSettings settings = inventory.GetGridSettings(inventory.gridOnMouse);
                        if (settings != null && settings.IsImmutable())
                        {
                            inventory.UseItem(itemAtMouse);
                            return;
                        }
                    }
                    
                    SelectItemWithMouse();
                }
            }
            else
            {
                DraumLogger.Info(this, "[InventoryController] ВНЕ ГРАНИЦ! Клик игнорируется");
            }
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            if (inventory.gridOnMouse != null && inventory.selectedItem == null)
            {
                Item itemAtMouse = inventory.GetItemAtMouseCoords();
                if (itemAtMouse != null)
                {
                    ShowContextMenuForItem(itemAtMouse);
                }
            }
        }

        if (inventory.selectedItem != null)
        {
            MoveSelectedItemToMouse();

            if (Input.GetKeyDown(KeyCode.R))
            {
                inventory.selectedItem.Rotate();
            }
        }
    }

    /// <summary>
    /// Выбирает новый предмет в инвентаре.
    /// </summary>
    private void SelectItemWithMouse()
    {
        Item item = inventory.GetItemAtMouseCoords();

        DraumLogger.Info(this, $"[InventoryController] GetItemAtMouseCoords вернул: {(item != null ? item.name : "NULL")}");

        if (item != null)
        {
            DraumLogger.Info(this, $"[InventoryController] Выбираем предмет: {item.name}");
            inventory.SelectItem(item);
        }
        else
        {
            DraumLogger.Info(this, "[InventoryController] Предмет не найден на этой позиции");
            
            if (inventory.gridOnMouse != null)
            {
                DraumLogger.Info(this, "=== СОДЕРЖИМОЕ ГРИДА ===");
                int foundItems = 0;
                for (int x = 0; x < inventory.gridOnMouse.items.GetLength(0); x++)
                {
                    for (int y = 0; y < inventory.gridOnMouse.items.GetLength(1); y++)
                    {
                        Item gridItem = inventory.gridOnMouse.items[x, y];
                        if (gridItem != null)
                        {
                            DraumLogger.Info(this, $"  [{x}, {y}]: {gridItem.name} (data: {(gridItem.data != null ? gridItem.data.name : "NULL")})");
                            foundItems++;
                        }
                    }
                }
                if (foundItems == 0)
                {
                    DraumLogger.Warning(this, "  ГРИД ПУСТОЙ! Нет ни одного предмета!");
                }
                DraumLogger.Info(this, $"=== Всего предметов: {foundItems} ===");
            }
        }
    }

    /// <summary>
    /// Показать контекстное меню для предмета
    /// </summary>
    private void ShowContextMenuForItem(Item item)
    {
        if (contextMenuController == null)
        {
            DraumLogger.Warning(this, "[InventoryController] ContextMenuController не найден! Назначь в Inspector.");
            return;
        }
        
        contextMenuController.ShowContextMenu(item);
    }

    /// <summary>
    /// Перемещает текущий выбранный объект к позиции мыши.
    /// </summary>
    private void MoveSelectedItemToMouse()
    {
        if (isWorldSpace)
        {
            MoveSelectedItemToMouseWorldSpace();
        }
        else
        {
            if (inventory.selectedItem == null) return;
            
            RectTransform itemRect = inventory.selectedItem.rectTransform;
            Canvas canvas = itemRect.GetComponentInParent<Canvas>();
            Vector2 mouseScreenPos = new Vector2(
                Input.mousePosition.x + itemDragOffsetX,
                Input.mousePosition.y + BASE_DRAG_OFFSET_Y + itemDragOffsetY
            );
            
            if (showDragDebug)
            {
                DraumLogger.Info(this, $"[InventoryController] Mouse: {Input.mousePosition}, BaseOffsetY: {BASE_DRAG_OFFSET_Y}, CustomOffset: ({itemDragOffsetX}, {itemDragOffsetY}), Final: {mouseScreenPos}");
            }
            
            if (canvas != null)
            {
                Vector2 localPoint;
                RectTransform canvasRect = canvas.transform as RectTransform;
                
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    mouseScreenPos,
                    canvas.worldCamera ?? Camera.main,
                    out localPoint))
                {
                    Vector3 worldPos = canvas.transform.TransformPoint(localPoint);
                    itemRect.position = new Vector3(worldPos.x, worldPos.y, itemRect.position.z);
                    
                    if (showDragDebug)
                    {
                        DraumLogger.Info(this, $"[InventoryController] Screen Space: LocalPoint: {localPoint}, WorldPos: {worldPos}, Final Position: {itemRect.position}");
                    }
                }
                else
                {
                    itemRect.position = new Vector3(
                        mouseScreenPos.x,
                        mouseScreenPos.y,
                        itemRect.position.z
                    );
                }
            }
            else
            {
                itemRect.position = new Vector3(
                    mouseScreenPos.x,
                    mouseScreenPos.y,
                    itemRect.position.z
                );
            }
        }
        
        UpdateSlotHighlighting();
    }
    
    /// <summary>
    /// Обновляет превью предмета под курсором (показывает куда будет поставлен предмет)
    /// </summary>
    private void UpdateSlotHighlighting()
    {
        if (inventory.selectedItem == null || inventory.gridOnMouse == null) return;
        
        Vector2Int slotPosition = inventory.GetSlotAtMouseCoords();
        Vector2Int itemSize = new Vector2Int(inventory.selectedItem.correctedSize.width, inventory.selectedItem.correctedSize.height);
        inventory.gridOnMouse.ShowItemPreviewUnderCursor(slotPosition, itemSize, inventory.selectedItem.data);
    }

    /// <summary>
    /// Перемещает выбранный предмет в World Space (следует за курсором в 3D)
    /// </summary>
    private void MoveSelectedItemToMouseWorldSpace()
    {
        if (playerCamera == null || inventory.selectedItem == null)
        {
            return;
        }

        Vector2 mouseScreenPos = new Vector2(
            Input.mousePosition.x + itemDragOffsetX,
            Input.mousePosition.y + BASE_DRAG_OFFSET_Y + itemDragOffsetY
        );

        Ray ray = playerCamera.ScreenPointToRay(mouseScreenPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxRaycastDistance, worldSpaceLayerMask))
        {
            Vector3 worldPosition = hit.point;
            
            worldPosition += Vector3.up * inventory.selectedItem.worldPlacementYOffset;
            
            inventory.selectedItem.rectTransform.position = worldPosition;

            if (showDebugRays)
            {
                Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.cyan, 0.1f);
            }
        }
        else
        {
            Vector3 worldPosition = ray.GetPoint(2f);
            inventory.selectedItem.rectTransform.position = worldPosition;
            
            if (showDebugRays)
            {
                Debug.DrawRay(ray.origin, ray.direction * 2f, Color.red, 0.1f);
            }
        }
        
        RotateItemTowardsCamera(inventory.selectedItem);
    }

    /// <summary>
    /// Поворачивает предмет параллельно гриду ИЛИ к камере (если наводим на мир)
    /// </summary>
    private void RotateItemTowardsCamera(Item item)
    {
        if (item == null) return;

        Quaternion targetRotation = item.rectTransform.rotation;

        if (inventory.gridOnMouse != null)
        {
            item.DisableHighlight();
        }
        else
        {
            if (rotateTowardsCameraInWorld && playerCamera != null)
            {
                Vector3 directionToCamera = (playerCamera.transform.position - item.rectTransform.position).normalized;
                targetRotation = Quaternion.LookRotation(-directionToCamera, Vector3.up);
            }
            else
            {
                return;
            }
            
            item.EnableHighlight();
        }
        
        if (enableItemRotation || (rotateTowardsCameraInWorld && inventory.gridOnMouse == null))
        {
            float rotSpeed = inventory.gridOnMouse != null ? itemRotationSpeed : cameraRotationSpeed;
            item.rectTransform.rotation = Quaternion.Slerp(
                item.rectTransform.rotation,
                targetRotation,
                rotSpeed * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// Размещает предмет в мире (конвертирует из инвентаря в GameObject в сцене)
    /// </summary>
    private void PlaceItemInWorld(Item item)
    {
        if (item == null || item.data == null || item.data.model3D == null)
        {
            DraumLogger.Warning(this, "[InventoryController] Не могу положить предмет в мир - нет 3D модели!");
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        Vector3 targetPosition;
        Vector3 spawnPosition;
        Vector3 throwDirection;
        
        Quaternion worldRotation = item.rectTransform.rotation;
        
        if (Physics.Raycast(ray, out hit, maxRaycastDistance, worldSpaceLayerMask))
        {
            targetPosition = hit.point;
            
            if (usePhysicsPlacement)
            {
                Vector3 cameraToTarget = (targetPosition - playerCamera.transform.position).normalized;
                spawnPosition = playerCamera.transform.position + cameraToTarget * throwDistanceFromCamera;
                
                throwDirection = (targetPosition - spawnPosition).normalized;
            }
            else
            {
                spawnPosition = targetPosition + hit.normal * item.worldPlacementYOffset;
                throwDirection = Vector3.zero;
            }
        }
        else
        {
            spawnPosition = item.rectTransform.position;
            targetPosition = spawnPosition;
            throwDirection = playerCamera.transform.forward;
        }

        GameObject worldObject = Instantiate(item.data.model3D, spawnPosition, worldRotation);
        
        worldObject.transform.localScale = item.data.model3D.transform.localScale;
        
        Rigidbody rb = worldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            
            if (usePhysicsPlacement)
            {
                rb.AddForce(throwDirection * throwForce, ForceMode.Impulse);
                
                Vector3 randomTorque = new Vector3(
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f)
                ) * 0.5f;
                rb.AddTorque(randomTorque, ForceMode.Impulse);
            }
        }
        
        Collider[] colliders = worldObject.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = true;
        }

        MonoBehaviour[] scripts = worldObject.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            script.enabled = true;
        }

        InteractableObject interactable = worldObject.GetComponent<InteractableObject>();
        if (interactable == null)
        {
            interactable = worldObject.AddComponent<InteractableObject>();
        }
        
        interactable.interactionType = InteractionType.Pickable;
        interactable.itemData = item.data;
        interactable.interactionName = $"Подобрать {item.data.name}";

        DraumLogger.Info(this, $"[InventoryController] Предмет {item.data.name} размещён в мире. Spawn: {spawnPosition}, Target: {targetPosition}");

        inventory.RemoveItem(item);
        
        inventory.DeselectItem();
    }

    /// <summary>
    /// Проверяет находится ли курсор над UI элементом (для WorldSpace и ScreenSpace)
    /// </summary>
    private bool IsPointerOverUI()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null) return false;
        
        bool contextMenuOpen = contextMenuController != null && 
                               contextMenuController.contextMenuPlane != null && 
                               contextMenuController.contextMenuPlane.activeSelf;
        
        UnityEngine.EventSystems.PointerEventData pointerData = 
            new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        pointerData.position = Input.mousePosition;
        
        var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            GameObject obj = result.gameObject;
            
            if (obj.GetComponent<InventoryGrid>() != null || 
                obj.GetComponentInParent<InventoryGrid>() != null)
            {
                continue;
            }
            
            if (!contextMenuOpen && obj.name.Contains("ContextMenu"))
            {
                continue;
            }
            
            UnityEngine.UI.Button button = obj.GetComponent<UnityEngine.UI.Button>();
            UnityEngine.UI.Image image = obj.GetComponent<UnityEngine.UI.Image>();

            if (button != null)
            {
                if (showDebugRays)
                {
                    DraumLogger.Info(this, $"[InventoryController] Клик по кнопке: {obj.name}");
                }
                return true;
            }
            
            if (image != null && image.raycastTarget)
            {
                if (obj.GetComponentInParent<InventoryGrid>() != null)
                {
                    continue;
                }
                
                if (contextMenuOpen && obj.name.Contains("ContextMenu"))
                {
                    if (showDebugRays)
                    {
                        DraumLogger.Info(this, $"[InventoryController] Клик по контекстному меню: {obj.name}");
                    }
                    return true;
                }
                
                if (obj.name.Contains("Button"))
                {
                    if (showDebugRays)
                    {
                        DraumLogger.Info(this, $"[InventoryController] Клик по кнопке (по имени): {obj.name}");
                    }
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Проверяет находится ли курсор над контекстным меню или его кнопками
    /// </summary>
    private bool IsPointerOverContextMenu()
    {
        if (contextMenuController == null || 
            contextMenuController.contextMenuPlane == null || 
            !contextMenuController.contextMenuPlane.activeSelf)
        {
            return false;
        }
        
        if (UnityEngine.EventSystems.EventSystem.current == null) return false;
        
        UnityEngine.EventSystems.PointerEventData pointerData = 
            new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        pointerData.position = Input.mousePosition;
        
        var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerData, results);
        
        foreach (var result in results)
        {
            GameObject obj = result.gameObject;
            
            if (obj.name.Contains("ContextMenu") || 
                obj.name.Contains("Button") ||
                obj.GetComponent<UnityEngine.UI.Button>() != null)
            {
                if (obj.transform.IsChildOf(contextMenuController.contextMenuPlane.transform) ||
                    obj == contextMenuController.contextMenuPlane)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Обновляет gridOnMouse для World Space через raycast
    /// </summary>
    private void UpdateGridOnMouseWorldSpace()
    {
        if (playerCamera == null)
        {
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (showDebugRays)
        {
            Debug.DrawRay(ray.origin, ray.direction * maxRaycastDistance, Color.yellow, 0.1f);
        }

        if (Physics.Raycast(ray, out hit, maxRaycastDistance, worldSpaceLayerMask))
        {
            InventoryGrid grid = hit.collider.GetComponent<InventoryGrid>();
            if (grid != null)
            {
                inventory.gridOnMouse = grid;
                
                if (showDebugRays)
                {
                    Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green, 0.1f);
                }
            }
            else
            {
                inventory.gridOnMouse = null;
            }
        }
        else
        {
            inventory.gridOnMouse = null;
        }
    }
}