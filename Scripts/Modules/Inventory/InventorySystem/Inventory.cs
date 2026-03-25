using System;
using UnityEngine;
using UnityEngine.UI;
using DRAUM.Core.Infrastructure.Logger;

public static class InventorySettings
{
    /// <summary>
    /// Размер, который занимает каждый слот.
    /// </summary>
    public static readonly Vector2Int slotSize = new(96, 96);

    /// <summary>
    /// Масштаб слота для внешних изменений. Не трогать.
    /// </summary>
    public static readonly float slotScale = 1f;

    /// <summary>
    /// Скорость, с которой предмет возвращается в целевую позицию.
    /// </summary>
    public static readonly float rotationAnimationSpeed = 30f;
}

public class Inventory : MonoBehaviour
{
    /// <summary>
    /// Список данных для каждого предмета в игре.
    /// </summary>
    [Header("Settings")]
    public ItemData[] itemsData;

    /// <summary>
    /// Префаб, который используется для создания новых предметов.
    /// </summary>
    public Item itemPrefab;
    
    /// <summary>
    /// Настройки для каждого грид-а (размеры, фильтры, скорость затухания)
    /// </summary>
    [Header("Grid Settings")]
    [Tooltip("Индивидуальные настройки для каждого Grid'а")]
    public InventoryGridSettings[] gridSettings = new InventoryGridSettings[0];

    /// <summary>
    /// Возвращает `InventoryGrid`, над которым сейчас находится мышь.
    /// </summary>
    public InventoryGrid gridOnMouse { get; set; }

    /// <summary>
    /// Динамический список всех `InventoryGrid`, который автоматически собирается при старте игры.
    /// </summary>
    public InventoryGrid[] grids { get; private set; }

    /// <summary>
    /// Текущий выбранный предмет.
    /// </summary>
    public Item selectedItem { get; private set; }
    
    /// <summary>
    /// Предыдущий грид и позиция (для отмены перемещения)
    /// </summary>
    private InventoryGrid previousGrid;
    private Vector2Int previousPosition;
    
    /// <summary>
    /// Быстрый доступ к настройкам гридов
    /// </summary>
    private System.Collections.Generic.Dictionary<InventoryGrid, InventoryGridSettings> gridSettingsMap = new System.Collections.Generic.Dictionary<InventoryGrid, InventoryGridSettings>();

    /// <summary>
    /// Вызывается при загрузке экземпляра скрипта.
    /// </summary>
    private void Awake()
    {
        grids = UnityEngine.Object.FindObjectsByType<InventoryGrid>(FindObjectsSortMode.None);
        SortGridsByPriority();
        InitializeGridSettings();
    }
    
    /// <summary>
    /// Инициализирует словарь для быстрого доступа к настройкам гридов.
    /// Сначала заполняем из своего gridSettings, затем дополняем из других Inventory в сцене,
    /// чтобы все гриды (в т.ч. экипировка-Л/П с другого инвентаря) имели настройки.
    /// </summary>
    private void InitializeGridSettings()
    {
        gridSettingsMap.Clear();
        
        if (gridSettings != null)
        {
            foreach (InventoryGridSettings settings in gridSettings)
            {
                if (settings != null && settings.grid != null)
                {
                    gridSettingsMap[settings.grid] = settings;
                }
            }
        }
        
        if (grids != null)
        {
            Inventory[] allInventories = UnityEngine.Object.FindObjectsByType<Inventory>(FindObjectsSortMode.None);
            foreach (InventoryGrid grid in grids)
            {
                if (grid == null || gridSettingsMap.ContainsKey(grid)) continue;
                
                foreach (Inventory otherInv in allInventories)
                {
                    if (otherInv == null || otherInv == this) continue;
                    if (otherInv.gridSettings == null) continue;
                    
                    foreach (InventoryGridSettings settings in otherInv.gridSettings)
                    {
                        if (settings == null || settings.grid == null) continue;
                        if (settings.grid == grid || settings.grid.name == grid.name)
                        {
                            gridSettingsMap[grid] = settings;
                            DraumLogger.Info(this, $"[Inventory] Grid {grid.name} получил настройки от другого Inventory (Slot Mode: {settings.slotMode})");
                            break;
                        }
                    }
                    if (gridSettingsMap.ContainsKey(grid)) break;
                }
            }
        }
        
        DraumLogger.Info(this, $"[Inventory] Grid settings initialized: {gridSettingsMap.Count} grids configured");
    }

    /// <summary>
    /// Сортирует грид по приоритету (меньшее значение = выше приоритет)
    /// </summary>
    private void SortGridsByPriority()
    {
        System.Array.Sort(grids, (a, b) => a.priority.CompareTo(b.priority));
        DraumLogger.Info(this, $"Inventory grids sorted. Total grids: {grids.Length}");
        for (int i = 0; i < grids.Length; i++)
        {
            DraumLogger.Info(this, $"Grid {i}: {grids[i].name}, Priority: {grids[i].priority}");
        }
    }

    /// <summary>
    /// Выбирает предмет и включает/выключает всё необходимое, чтобы освободить место под другой предмет.
    /// </summary>
    /// <param name="item">Предмет, который нужно выбрать.</param>
    public void SelectItem(Item item)
    {
        previousGrid = item.inventoryGrid;
        previousPosition = item.indexPosition;
        
        ClearItemReferences(item);
        selectedItem = item;
        selectedItem.rectTransform.SetParent(transform);
        selectedItem.rectTransform.SetAsLastSibling();
        
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            if (selectedItem.background != null)
            {
                Color bgColor = selectedItem.background.color;
                bgColor.a = 0f;
                selectedItem.background.color = bgColor;
            }
        }
    }

    /// <summary>
    /// Снимает выбор с текущего предмета.
    /// </summary>
    public void DeselectItem()
    {
        if (gridOnMouse != null)
        {
            gridOnMouse.ClearSlotHighlighting();
        }
        
        selectedItem = null;
    }
    
    /// <summary>
    /// Отменяет перемещение предмета (возвращает на старое место)
    /// </summary>
    public void CancelItemMove()
    {
        if (selectedItem == null || previousGrid == null)
        {
            DraumLogger.Info(this, "[Inventory] CancelItemMove: нечего отменять");
            return;
        }
        
        DraumLogger.Info(this, $"[Inventory] Возвращаем предмет {selectedItem.name} обратно в {previousGrid.name} на позицию {previousPosition}");
        
        selectedItem.indexPosition = previousPosition;
        selectedItem.rectTransform.SetParent(previousGrid.rectTransform);
        selectedItem.inventoryGrid = previousGrid;
        
        for (int x = 0; x < selectedItem.correctedSize.width; x++)
        {
            for (int y = 0; y < selectedItem.correctedSize.height; y++)
            {
                int slotX = selectedItem.indexPosition.x + x;
                int slotY = selectedItem.indexPosition.y + y;
                
                previousGrid.items[slotX, slotY] = selectedItem;
            }
        }
        
        selectedItem.rectTransform.localPosition = IndexToInventoryPosition(selectedItem);
        
        selectedItem.DisableHighlight();
        
        selectedItem = null;
    }

    /// <summary>
    /// Добавляет предмет в инвентарь, динамически подбирая место, где он поместится.
    /// </summary>
    /// <param name="itemData">Данные предмета, который будет добавлен в инвентарь.</param>
    public bool AddItem(ItemData itemData)
    {
        if (itemPrefab == null)
        {
            DraumLogger.Error(this, "[Inventory] itemPrefab не назначен! Перейди к Inventory GameObject и назначь Item Prefab в Inspector!");
            return false;
        }

        if (itemData == null)
        {
            DraumLogger.Error(this, "[Inventory] itemData null! Проверь что ItemData назначен на InteractableObject!");
            return false;
        }

        string itemCategoriesStr = itemData.categories != null && itemData.categories.Length > 0
            ? string.Join(", ", System.Array.ConvertAll(itemData.categories, cat => cat != null ? cat.name : "null"))
            : "нет категорий";

        DraumLogger.Info(this, $"[Inventory] Попытка добавить предмет: {itemData.name} (категории: [{itemCategoriesStr}], размер: {itemData.size.width}x{itemData.size.height})");
        DraumLogger.Info(this, $"[Inventory] Всего Grid'ов в инвентаре: {grids.Length}");

        for (int g = 0; g < grids.Length; g++)
        {
            InventoryGridSettings gridSettings = GetGridSettings(grids[g]);
            string slotModeStr = gridSettings != null ? gridSettings.slotMode.ToString() : "Normal (нет настроек)";
            DraumLogger.Info(this, $"[Inventory] Проверяем Grid [{g}]: {grids[g].name} (Slot Mode: {slotModeStr}, размер: {grids[g].gridSize.x}x{grids[g].gridSize.y})");

            if (!CanPlaceItemInGrid(itemData, grids[g]))
            {
                DraumLogger.Info(this, $"[Inventory] ✗ Grid {grids[g].name} не принимает предмет с категориями [{itemCategoriesStr}] - пропускаем");
                continue;
            }

            DraumLogger.Info(this, $"[Inventory] ✓ Grid {grids[g].name} принимает предмет - ищем свободный слот...");

            InventoryGridSettings settings = GetGridSettings(grids[g]);
            bool ignoreSize = settings != null && settings.IgnoreItemSize();

            for (int y = 0; y < grids[g].gridSize.y; y++)
            {
                for (int x = 0; x < grids[g].gridSize.x; x++)
                {
                    Vector2Int slotPosition = new Vector2Int(x, y);

                    if (ignoreSize)
                    {
                        if (!ExistsItem(slotPosition, grids[g], 1, 1))
                        {
                            DraumLogger.Info(this, $"[Inventory] ✓ Найден свободный слот в {grids[g].name} на позиции ({slotPosition.x}, {slotPosition.y})");
                            Item newItem = CreateItemAtPosition(grids[g], slotPosition, itemData, 1, 1, false);
                            if (newItem != null)
                            {
                                DraumLogger.Info(this, $"[Inventory] ✓ Предмет {itemData.name} успешно добавлен в {grids[g].name}");
                                return true;
                            }
                        }
                    }
                    else
                    {
                        for (int r = 0; r < 2; r++)
                        {
                            if (r == 0)
                            {
                                if (!ExistsItem(slotPosition, grids[g], itemData.size.width, itemData.size.height))
                                {
                                    DraumLogger.Info(this, $"[Inventory] ✓ Найден свободный слот в {grids[g].name} на позиции ({slotPosition.x}, {slotPosition.y}) [не повернут]");
                                    Item newItem = CreateItemAtPosition(grids[g], slotPosition, itemData, itemData.size.width, itemData.size.height, false);
                                    if (newItem != null)
                                    {
                                        DraumLogger.Info(this, $"[Inventory] ✓ Предмет {itemData.name} успешно добавлен в {grids[g].name}");
                                        return true;
                                    }
                                }
                            }

                            if (r == 1)
                            {
                                if (!ExistsItem(slotPosition, grids[g], itemData.size.height, itemData.size.width))
                                {
                                    DraumLogger.Info(this, $"[Inventory] ✓ Найден свободный слот в {grids[g].name} на позиции ({slotPosition.x}, {slotPosition.y}) [повернут]");
                                    Item newItem = CreateItemAtPosition(grids[g], slotPosition, itemData, itemData.size.height, itemData.size.width, true);
                                    if (newItem != null)
                                    {
                                        DraumLogger.Info(this, $"[Inventory] ✓ Предмет {itemData.name} успешно добавлен в {grids[g].name}");
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        DraumLogger.Warning(this, $"[Inventory] ✗ Не удалось найти свободный слот для предмета {itemData.name}!");
        DraumLogger.Warning(this, $"[Inventory] Проверено Grid'ов: {grids.Length}");
        DraumLogger.Warning(this, $"[Inventory] Категории предмета: [{itemCategoriesStr}]");
        DraumLogger.Warning(this, $"[Inventory] Размер предмета: {itemData.size.width}x{itemData.size.height}");

        for (int g = 0; g < grids.Length; g++)
        {
            InventoryGridSettings settings = GetGridSettings(grids[g]);
            bool accepts = CanPlaceItemInGrid(itemData, grids[g]);
            string slotModeStr = settings != null ? settings.slotMode.ToString() : "Normal";
            int freeSlots = CountFreeSlots(grids[g], settings);

            DraumLogger.Warning(this, $"[Inventory] Grid [{g}] {grids[g].name}: Slot Mode={slotModeStr}, Accepts={accepts}, Free Slots={freeSlots}/{grids[g].gridSize.x * grids[g].gridSize.y}");
        }

        return false;
    }
			
			/// <summary>
			/// Подсчитывает количество свободных слотов в грид-е
			/// </summary>
			private int CountFreeSlots(InventoryGrid grid, InventoryGridSettings settings)
			{
        if (grid == null || grid.items == null) return 0;

        int freeCount = 0;
        bool ignoreSize = settings != null && settings.IgnoreItemSize();

        for (int y = 0; y < grid.gridSize.y; y++)
        {
            for (int x = 0; x < grid.gridSize.x; x++)
            {
                if (ignoreSize)
                {
                    if (grid.items[x, y] == null)
                    {
                        freeCount++;
                    }
                }
                else
                {
                    if (grid.items[x, y] == null)
                    {
                        freeCount++;
                    }
                }
            }
        }

        return freeCount;
    }
			
			/// <summary>
			/// Создает предмет в указанной позиции грида
			/// </summary>
			private Item CreateItemAtPosition(InventoryGrid grid, Vector2Int slotPosition, ItemData itemData, int width, int height, bool rotated)
    {
        Item newItem = Instantiate(itemPrefab);
        if (rotated) newItem.Rotate();

        newItem.rectTransform = newItem.GetComponent<RectTransform>();
        newItem.rectTransform.SetParent(grid.rectTransform);

        InventoryGridSettings settings = GetGridSettings(grid);
        bool ignoreSize = settings != null && settings.IgnoreItemSize();

        if (ignoreSize)
        {
            newItem.rectTransform.sizeDelta = new Vector2(
                InventorySettings.slotSize.x,
                InventorySettings.slotSize.y
            );
        }
        else
        {
            newItem.rectTransform.sizeDelta = new Vector2(
                itemData.size.width * InventorySettings.slotSize.x,
                itemData.size.height * InventorySettings.slotSize.y
            );
        }

        newItem.indexPosition = slotPosition;
        newItem.inventory = this;

        for (int xx = 0; xx < width; xx++)
        {
            for (int yy = 0; yy < height; yy++)
            {
                int slotX = slotPosition.x + xx;
                int slotY = slotPosition.y + yy;

                grid.items[slotX, slotY] = newItem;
                grid.items[slotX, slotY].data = itemData;
            }
        }

        newItem.rectTransform.localPosition = IndexToInventoryPosition(newItem);

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            Vector3 canvasScale = parentCanvas.transform.localScale;
            newItem.rectTransform.localScale = new Vector3(
                1f / canvasScale.x,
                1f / canvasScale.y,
                1f / canvasScale.z
            );
        }
        else
        {
            newItem.rectTransform.localScale = Vector3.one;
        }

        newItem.inventoryGrid = grid;

        if (newItem.background != null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            bool isWorldSpace = (canvas != null && canvas.renderMode == RenderMode.WorldSpace);

            if (isWorldSpace)
            {
                Color bgColor = itemData.backgroundColor;
                bgColor.a = 0f;
                newItem.background.color = bgColor;
            }
            else
            {
                newItem.background.color = itemData.backgroundColor;
            }
        }

        return newItem;
    }

    /// <summary>
    /// Удаляет предмет из инвентаря полностью.
    /// </summary>
    /// <param name="item">Предмет, который нужно удалить.</param>
    public void RemoveItem(Item item)
    {
        if (item != null)
        {
            ClearItemReferences(item);
            Destroy(item.gameObject);
        }
    }

    /// <summary>
    /// Перемещает предмет в новую позицию: сначала проверяет выход за границы сетки и занятость целевого слота.
    /// </summary>
    /// <param name="slotPosition">Позиция, которую будет занимать предмет в инвентаре.</param>
    /// <param name="item">Предмет, который будет перемещён.</param>
    /// <param name="deselectItemInEnd">Флаг: нужно ли снять выделение в конце перемещения.</param>
    public void MoveItem(Item item, bool deselectItemInEnd = true)
    {
        if (item == null || gridOnMouse == null) return;
        
        Vector2Int slotPosition = GetSlotAtMouseCoords();

        InventoryGridSettings sourceSettings = GetGridSettings(item.inventoryGrid);
        InventoryGridSettings targetSettings = GetGridSettings(gridOnMouse);
        
        bool sourceIsImmutable = sourceSettings != null && sourceSettings.IsImmutable();
        bool targetIsImmutable = targetSettings != null && targetSettings.IsImmutable();
        
        if (sourceIsImmutable && targetIsImmutable && item.inventoryGrid != gridOnMouse)
        {
            if (sourceSettings.slotMode == targetSettings.slotMode)
            {
                DraumLogger.Info(this, $"[Inventory] Нельзя перемещать предметы между {sourceSettings.slotMode} слотами!");
                return;
            }
        }
        
        if (!CanPlaceItemInGrid(item.data, gridOnMouse))
        {
            DraumLogger.Info(this, $"[Inventory] Предмет не может быть помещен в Grid {gridOnMouse.name} (не проходит фильтр категорий)");
            return;
        }
        
        int checkWidth = item.correctedSize.width;
        int checkHeight = item.correctedSize.height;
        
        if (targetIsImmutable)
        {
            checkWidth = 1;
            checkHeight = 1;
        }

        if (ReachedBoundary(slotPosition, gridOnMouse, checkWidth, checkHeight))
        {
            DraumLogger.Info(this, "Bounds");
            return;
        }

        if (ExistsItem(slotPosition, gridOnMouse, checkWidth, checkHeight))
        {
            DraumLogger.Info(this, "Item");
            return;
        }

        item.indexPosition = slotPosition;
        item.inventoryGrid = gridOnMouse;
        item.rectTransform.SetParent(gridOnMouse.rectTransform);

        if (targetIsImmutable)
        {
            item.rectTransform.sizeDelta = new Vector2(
                InventorySettings.slotSize.x,
                InventorySettings.slotSize.y
            );
        }

        for (int x = 0; x < checkWidth; x++)
        {
            for (int y = 0; y < checkHeight; y++)
            {
                int slotX = item.indexPosition.x + x;
                int slotY = item.indexPosition.y + y;

                gridOnMouse.items[slotX, slotY] = item;
            }
        }

        item.rectTransform.localPosition = IndexToInventoryPosition(item);
        item.inventoryGrid = gridOnMouse;

        if (deselectItemInEnd)
        {
            DeselectItem();
        }
    }

    /// <summary>
    /// Меняет местами выбранный предмет и предмет, находящийся под курсором.
    /// </summary>
    /// <param name="overlapItem">Предмет, пересекающийся с текущим (на который наведен курсор).</param>
    public void SwapItem(Item overlapItem, Item oldSelectedItem)
    {
        if (overlapItem == null || oldSelectedItem == null || gridOnMouse == null) return;
        
        InventoryGridSettings targetSettings = GetGridSettings(gridOnMouse);
        bool targetIsImmutable = targetSettings != null && targetSettings.IgnoreItemSize();
        
        int checkWidth = oldSelectedItem.correctedSize.width;
        int checkHeight = oldSelectedItem.correctedSize.height;
        
        if (targetIsImmutable)
        {
            checkWidth = 1;
            checkHeight = 1;
        }
        
        if (ReachedBoundary(overlapItem.indexPosition, gridOnMouse, checkWidth, checkHeight))
        {
            return;
        }

        ClearItemReferences(overlapItem);

        if (ExistsItem(overlapItem.indexPosition, gridOnMouse, checkWidth, checkHeight))
        {
            RevertItemReferences(overlapItem);
            return;
        }

        SelectItem(overlapItem);
        MoveItem(oldSelectedItem, false);
    }

    /// <summary>
    /// Очищает ссылки на позицию предмета в инвентаре.
    /// </summary>
    /// <param name="item">Предмет, для которого нужно удалить ссылки.</param>
    public void ClearItemReferences(Item item)
    {
        if (item == null || item.inventoryGrid == null) return;
        
        InventoryGridSettings settings = GetGridSettings(item.inventoryGrid);
        bool ignoreSize = settings != null && settings.IgnoreItemSize();
        
        int clearWidth = item.correctedSize.width;
        int clearHeight = item.correctedSize.height;
        
        if (ignoreSize)
        {
            clearWidth = 1;
            clearHeight = 1;
        }
        
        for (int x = 0; x < clearWidth; x++)
        {
            for (int y = 0; y < clearHeight; y++)
            {
                int slotX = item.indexPosition.x + x;
                int slotY = item.indexPosition.y + y;

                if (slotX >= 0 && slotX < item.inventoryGrid.items.GetLength(0) &&
                    slotY >= 0 && slotY < item.inventoryGrid.items.GetLength(1))
                {
                    item.inventoryGrid.items[slotX, slotY] = null;
                }
            }
        }
    }

    /// <summary>
    /// Восстанавливает ссылки на позицию предмета в инвентаре (после неудачного свапа).
    /// </summary>
    /// <param name="item">Предмет, для которого нужно восстановить ссылки.</param>
    public void RevertItemReferences(Item item)
    {
        if (item == null || item.inventoryGrid == null) return;
        
        InventoryGridSettings settings = GetGridSettings(item.inventoryGrid);
        bool ignoreSize = settings != null && settings.IgnoreItemSize();
        
        int revertWidth = item.correctedSize.width;
        int revertHeight = item.correctedSize.height;
        
        if (ignoreSize)
        {
            revertWidth = 1;
            revertHeight = 1;
        }
        
        for (int x = 0; x < revertWidth; x++)
        {
            for (int y = 0; y < revertHeight; y++)
            {
                int slotX = item.indexPosition.x + x;
                int slotY = item.indexPosition.y + y;

                if (slotX >= 0 && slotX < item.inventoryGrid.items.GetLength(0) &&
                    slotY >= 0 && slotY < item.inventoryGrid.items.GetLength(1))
                {
                    item.inventoryGrid.items[slotX, slotY] = item;
                }
            }
        }
    }

    /// <summary>
    /// Проверяет, есть ли предмет в указанной позиции.
    /// </summary>
    /// <param name="slotPosition">Позиция, которую нужно проверить.</param>
    /// <param name="width">Ширина предмета.</param>
    /// <param name="height">Высота предмета.</param>
    /// <param name="grid">Сетка, в которой выполняется проверка.</param>
    /// <returns></returns>
    public bool ExistsItem(Vector2Int slotPosition, InventoryGrid grid, int width = 1, int height = 1)
    {
        if (ReachedBoundary(slotPosition, grid, width, height))
        {
            DraumLogger.Info(this, "Bounds2");
            return true;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int slotX = slotPosition.x + x;
                int slotY = slotPosition.y + y;

                if (grid.items[slotX, slotY] != null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Проверяет, находится ли указанная позиция за границами инвентаря.
    /// </summary>
    /// <param name="slotPosition">Позиция для проверки.</param>
    /// <param name="width">Ширина предмета.</param>
    /// <param name="height">Высота предмета.</param>
    /// <param name="gridReference">Сетка, относительно которой выполняется проверка.</param>
    /// <returns></returns>
    public bool ReachedBoundary(Vector2Int slotPosition, InventoryGrid gridReference, int width = 1, int height = 1)
    {
        if (slotPosition.x + width > gridReference.gridSize.x || slotPosition.x < 0)
        {
            return true;
        }

        if (slotPosition.y + height > gridReference.gridSize.y || slotPosition.y < 0)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Возвращает позицию предмета в пространстве инвентаря по координатам в матрице.
    /// </summary>
    /// <param name="item">Предмет, для которого вычисляется позиция.</param>
    /// <returns></returns>
    public Vector3 IndexToInventoryPosition(Item item)
    {
        Vector3 inventorizedPosition =
            new()
            {
                x = item.indexPosition.x * InventorySettings.slotSize.x
                    + InventorySettings.slotSize.x * item.correctedSize.width / 2,
                y = -(item.indexPosition.y * InventorySettings.slotSize.y
                    + InventorySettings.slotSize.y * item.correctedSize.height / 2
                )
            };

        return inventorizedPosition;
    }

    /// <summary>
    /// Возвращает координаты слота, на который наведен курсор мыши.
    /// </summary>
    /// <returns></returns>
    public Vector2Int GetSlotAtMouseCoords()
    {
        if (gridOnMouse == null)
        {
            DraumLogger.Warning(this, "[Inventory] GetSlotAtMouseCoords: gridOnMouse is null");
            return Vector2Int.zero;
        }

        Vector2 localPoint;
        
        Canvas canvas = gridOnMouse.GetComponentInParent<Canvas>();
        
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            DraumLogger.Info(this, "[Inventory] World Space режим");
            
            Camera cam = canvas.worldCamera ?? Camera.main;
            
            if (cam == null)
            {
                DraumLogger.Error(this, "[Inventory] World Space Canvas требует Camera! Назначь Event Camera на Canvas!");
                return Vector2Int.zero;
            }
            
            DraumLogger.Info(this, $"[Inventory] Camera: {cam.name}, Mouse: {Input.mousePosition}");
            DraumLogger.Info(this, $"[Inventory] RectTransform Pivot: {gridOnMouse.rectTransform.pivot}");
            DraumLogger.Info(this, $"[Inventory] RectTransform AnchorMin: {gridOnMouse.rectTransform.anchorMin}, AnchorMax: {gridOnMouse.rectTransform.anchorMax}");
            DraumLogger.Info(this, $"[Inventory] RectTransform Position: {gridOnMouse.rectTransform.position}");
            DraumLogger.Info(this, $"[Inventory] RectTransform LocalPosition: {gridOnMouse.rectTransform.localPosition}");
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                gridOnMouse.rectTransform, 
                Input.mousePosition, 
                cam, 
                out localPoint
            );
            
            DraumLogger.Info(this, $"[Inventory] Local Point (from RectTransformUtility): {localPoint}");
        }
        else
        {
            DraumLogger.Info(this, "[Inventory] Screen Space режим");
            
            Vector2 gridPosition =
                new(
                    Input.mousePosition.x - gridOnMouse.rectTransform.position.x,
                    gridOnMouse.rectTransform.position.y - Input.mousePosition.y
                );
            
            localPoint = gridPosition;
        }

        Vector2 adjustedPoint;
        
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {

            Vector2 pivot = gridOnMouse.rectTransform.pivot;
            Vector2 rectSize = gridOnMouse.rectTransform.rect.size;
            
            adjustedPoint = new Vector2(
                localPoint.x + (rectSize.x * pivot.x),
                (rectSize.y * (1 - pivot.y)) - localPoint.y
            );
            
            DraumLogger.Info(this, $"[Inventory] Pivot: {pivot}, Calc: x={localPoint.x} + {rectSize.x * pivot.x}, y={rectSize.y * (1 - pivot.y)} - {localPoint.y}");
        }
        else
        {
            adjustedPoint = new Vector2(
                localPoint.x + (gridOnMouse.rectTransform.rect.width / 2),
                -(localPoint.y - (gridOnMouse.rectTransform.rect.height / 2))
            );
        }
        
        DraumLogger.Info(this, $"[Inventory] Adjusted Point: {adjustedPoint}");
        DraumLogger.Info(this, $"[Inventory] Rect width: {gridOnMouse.rectTransform.rect.width}, height: {gridOnMouse.rectTransform.rect.height}");
        DraumLogger.Info(this, $"[Inventory] Slot size: {InventorySettings.slotSize}, scale: {InventorySettings.slotScale}");

        Vector2Int slotPosition =
            new(
                (int)(adjustedPoint.x / (InventorySettings.slotSize.x * InventorySettings.slotScale)),
                (int)(adjustedPoint.y / (InventorySettings.slotSize.y * InventorySettings.slotScale))
            );

        DraumLogger.Info(this, $"[Inventory] Final Slot Position: {slotPosition}");
        return slotPosition;
    }


    /// <summary>
    /// Возвращает предмет, который находится под курсором мыши.
    /// </summary>
    /// <returns></returns>
    public Item GetItemAtMouseCoords()
    {
        Vector2Int slotPosition = GetSlotAtMouseCoords();

        if (!ReachedBoundary(slotPosition, gridOnMouse))
        {
            return GetItemFromSlotPosition(slotPosition);
        }

        return null;
    }

    /// <summary>
    /// Возвращает предмет по позиции слота.
    /// </summary>
    /// <param name="slotPosition">Позиция слота для проверки.</param>
    /// <returns></returns>
    public Item GetItemFromSlotPosition(Vector2Int slotPosition)
    {
        if (gridOnMouse == null)
        {
            DraumLogger.Error(this, "[Inventory] GetItemFromSlotPosition: gridOnMouse is NULL!");
            return null;
        }
        
        DraumLogger.Info(this, $"[Inventory] GetItemFromSlotPosition: slot ({slotPosition.x}, {slotPosition.y})");
        DraumLogger.Info(this, $"[Inventory] Grid size: {gridOnMouse.gridSize}, items array: {gridOnMouse.items.GetLength(0)}x{gridOnMouse.items.GetLength(1)}");
        
        if (slotPosition.x < 0 || slotPosition.x >= gridOnMouse.items.GetLength(0) ||
            slotPosition.y < 0 || slotPosition.y >= gridOnMouse.items.GetLength(1))
        {
            DraumLogger.Warning(this, $"[Inventory] Slot position ({slotPosition.x}, {slotPosition.y}) out of items array bounds!");
            return null;
        }
        
        Item item = gridOnMouse.items[slotPosition.x, slotPosition.y];
        DraumLogger.Info(this, $"[Inventory] Item at [{slotPosition.x}, {slotPosition.y}]: {(item != null ? item.name : "NULL")}");
        
        return item;
    }
    
    /// <summary>
    /// Получить настройки для конкретного грида
    /// </summary>
    public InventoryGridSettings GetGridSettings(InventoryGrid grid)
    {
        if (grid == null) return null;
        
        if (gridSettingsMap.ContainsKey(grid))
        {
            return gridSettingsMap[grid];
        }
        
        return null;
    }
    
    /// <summary>
    /// Получить скорость затухания для конкретного грида
    /// </summary>
    public float GetGridFadeSpeed(InventoryGrid grid, float defaultSpeed)
    {
        InventoryGridSettings settings = GetGridSettings(grid);
        
        if (settings != null && settings.useCustomSpeed)
        {
            return settings.customFadeSpeed;
        }
        
        return defaultSpeed;
    }
    
    /// <summary>
    /// Проверяет, может ли предмет быть помещен в указанный грид (с учетом фильтров категорий)
    /// </summary>
    public bool CanPlaceItemInGrid(ItemData itemData, InventoryGrid grid)
    {
        if (itemData == null || grid == null) return false;
        
        InventoryGridSettings settings = GetGridSettings(grid);
        
        if (settings == null) return true;
        
        return settings.CanAcceptItem(itemData);
    }
    
    /// <summary>
    /// Перемещает предмет в указанный грид на указанную позицию
    /// </summary>
    public void MoveItemToGrid(Item item, InventoryGrid targetGrid, Vector2Int position)
    {
        if (item == null || targetGrid == null || item.data == null) return;
        
        if (item.inventoryGrid != null)
        {
            ClearItemReferences(item);
        }
        
        InventoryGridSettings targetSettings = GetGridSettings(targetGrid);
        bool ignoreSize = targetSettings != null && targetSettings.IgnoreItemSize();
        
        int width = ignoreSize ? 1 : item.correctedSize.width;
        int height = ignoreSize ? 1 : item.correctedSize.height;
        
        if (ExistsItem(position, targetGrid, width, height))
        {
            DraumLogger.Warning(this, $"[Inventory] MoveItemToGrid: позиция ({position.x}, {position.y}) занята в {targetGrid.name}");
            return;
        }
        
        if (ReachedBoundary(position, targetGrid, width, height))
        {
            DraumLogger.Warning(this, $"[Inventory] MoveItemToGrid: позиция ({position.x}, {position.y}) выходит за границы {targetGrid.name}");
            return;
        }
        
        item.indexPosition = position;
        item.inventoryGrid = targetGrid;
        item.rectTransform.SetParent(targetGrid.rectTransform);
        
        if (ignoreSize)
        {
            item.rectTransform.sizeDelta = new Vector2(
                InventorySettings.slotSize.x,
                InventorySettings.slotSize.y
            );
        }
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int slotX = position.x + x;
                int slotY = position.y + y;
                targetGrid.items[slotX, slotY] = item;
            }
        }
        
        item.rectTransform.localPosition = IndexToInventoryPosition(item);
        
        DraumLogger.Info(this, $"[Inventory] Предмет {item.data.name} перемещён в {targetGrid.name} на позицию ({position.x}, {position.y})");
    }
    
    /// <summary>
    /// Использует предмет из слота экипировки/расходников (заглушка)
    /// </summary>
    public void UseItem(Item item)
    {
        if (item == null || item.inventoryGrid == null || item.data == null)
        {
            DraumLogger.Warning(this, "[Inventory] UseItem: item или его данные null!");
            return;
        }
        
        InventoryGridSettings settings = GetGridSettings(item.inventoryGrid);
        if (settings == null || !settings.IsImmutable())
        {
            DraumLogger.Warning(this, $"[Inventory] UseItem: предмет не находится в Equipment/Consumable слоте!");
            return;
        }
        
        if (settings.slotMode == SlotMode.Equipment)
        {
            DraumLogger.Info(this, $"[Inventory] Предмет экипирован: {item.data.name}");
        }
        else if (settings.slotMode == SlotMode.Consumable)
        {
            DraumLogger.Info(this, $"[Inventory] Предмет использован: {item.data.name}");
        }
    }
    
    /// <summary>
    /// Получить все гриды с режимом экипировки
    /// </summary>
    public InventoryGrid[] GetEquipmentGrids()
    {
        System.Collections.Generic.List<InventoryGrid> equipmentGrids = new System.Collections.Generic.List<InventoryGrid>();
        
        if (grids == null) return equipmentGrids.ToArray();
        
        foreach (InventoryGrid grid in grids)
        {
            if (grid == null) continue;
            
            InventoryGridSettings settings = GetGridSettings(grid);
            if (settings != null && settings.slotMode == SlotMode.Equipment)
            {
                equipmentGrids.Add(grid);
            }
        }
        
        return equipmentGrids.ToArray();
    }
    
    /// <summary>
    /// Получить предмет из гридов экипировки по индексу слота (0 = первый слот, 1 = второй и т.д.)
    /// Проверяет все гриды экипировки и возвращает первый найденный предмет в указанной позиции
    /// </summary>
    /// <param name="slotIndex">Индекс слота (0 = первый, 1 = второй, 2 = третий)</param>
    /// <param name="equipmentGrid">Out-параметр: грид, в котором найден предмет</param>
    /// <returns>Предмет из указанного слота или null</returns>
    public Item GetItemFromEquipmentSlot(int slotIndex, out InventoryGrid equipmentGrid)
    {
        equipmentGrid = null;
        
        if (slotIndex < 0)
        {
            DraumLogger.Warning(this, $"[Inventory] GetItemFromEquipmentSlot: некорректный индекс слота {slotIndex}");
            return null;
        }
        
        InventoryGrid[] equipmentGrids = GetEquipmentGrids();
        
        if (equipmentGrids == null || equipmentGrids.Length == 0)
        {
            return null;
        }
        
        foreach (InventoryGrid grid in equipmentGrids)
        {
            if (grid == null || grid.items == null) continue;
            
            int x = slotIndex;
            int y = 0;
            
            if (x >= 0 && x < grid.gridSize.x && y >= 0 && y < grid.gridSize.y)
            {
                Item item = grid.items[x, y];
                if (item != null)
                {
                    equipmentGrid = grid;
                    return item;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Получить предмет из конкретного грида экипировки по позиции
    /// </summary>
    /// <param name="equipmentGrid">Грид для проверки</param>
    /// <param name="slotIndex">Индекс слота (x координата, y = 0)</param>
    /// <returns>Предмет из указанного слота или null</returns>
    public Item GetItemFromEquipmentGrid(InventoryGrid equipmentGrid, int slotIndex)
    {
        if (equipmentGrid == null || equipmentGrid.items == null)
        {
            return null;
        }
        
        InventoryGridSettings settings = GetGridSettings(equipmentGrid);
        if (settings == null || settings.slotMode != SlotMode.Equipment)
        {
            DraumLogger.Warning(this, $"[Inventory] GetItemFromEquipmentGrid: Grid {equipmentGrid.name} не является Equipment Grid!");
            return null;
        }
        
        int x = slotIndex;
        int y = 0;

        if (x >= 0 && x < equipmentGrid.gridSize.x && y >= 0 && y < equipmentGrid.gridSize.y)
        {
            return equipmentGrid.items[x, y];
        }
        
        return null;
    }
}
