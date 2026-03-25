using DRAUM.Core;
using DRAUM.Core.Infrastructure.Logger;
using DRAUM.Modules.Inventory.Events;
using System;
using UnityEngine;

namespace DRAUM.Modules.Inventory
{
    /// <summary>
    /// Модуль управления инвентарем.
    /// Интегрирует Inventory систему в архитектуру Core и управляет взаимодействием с игроком через события.
    /// </summary>
    public class InventoryModule : ManagerModuleBase
    {
        [Header("Inventory References")]
        [Tooltip("Основной Inventory компонент (автопоиск если не назначен)")]
        public global::Inventory inventory;

        [Tooltip("Все инвентари модуля (основной + Equipment + другие)")]
        public global::Inventory[] allInventories = new global::Inventory[0];

        [Tooltip("BackpackStateMachine для управления рюкзаком")]
        public BackpackStateMachine backpackStateMachine;

        [Tooltip("InventoryCameraController для управления камерой в инвентаре")]
        public InventoryCameraController inventoryCameraController;

        [Tooltip("Автоматически найти Inventory в сцене, если не назначен")]
        public bool autoFindInventory = true;
        
        [Tooltip("Автоматически найти все инвентари в сцене (включая Equipment)")]
        public bool autoFindAllInventories = true;

        [Header("UI")]
        [Tooltip("Объект crosshair/cursor, который надо скрывать при открытом инвентаре.")]
        public GameObject crosshairToToggle;

        /// <summary>
        /// Имя модуля
        /// </summary>
        public override string ModuleName => "Inventory";

        /// <summary>
        /// Приоритет инициализации - инвентарь инициализируется после игрока
        /// </summary>
        public override int InitializationPriority => 25;

        /// <summary>
        /// Флаги модуля - стандартное обновление, может быть приостановлен
        /// </summary>
        public override ModuleFlags Flags => ModuleFlags.StandardUpdate | ModuleFlags.CanBePaused;

        #region Properties

        /// <summary>
        /// Открыт ли инвентарь
        /// </summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// Есть ли выбранный предмет
        /// </summary>
        public bool HasSelectedItem => inventory != null && inventory.selectedItem != null;

        /// <summary>
        /// Выбранный предмет
        /// </summary>
        public Item SelectedItem => inventory != null ? inventory.selectedItem : null;

        #endregion

        #region Initialization

        protected override void OnInitialize()
        {
            if (inventory == null && autoFindInventory)
            {
                inventory = FindFirstObjectByType<global::Inventory>();
            }

            if (inventory == null)
            {
                DraumLogger.Error(this, "[InventoryModule] Inventory not found! Module cannot work.");
                return;
            }

            if ((allInventories == null || allInventories.Length == 0) && autoFindAllInventories)
            {
                global::Inventory[] foundInventories = FindObjectsByType<global::Inventory>(FindObjectsSortMode.None);
                
                if (foundInventories.Length > 0)
                {
                    allInventories = foundInventories;
                    DraumLogger.Info(this, $"[InventoryModule] Found inventories: {foundInventories.Length}");
                    
                    bool mainInventoryIncluded = false;
                    foreach (var inv in allInventories)
                    {
                        if (inv == inventory)
                        {
                            mainInventoryIncluded = true;
                            break;
                        }
                    }
                    
                    if (!mainInventoryIncluded)
                    {
                        var newList = new System.Collections.Generic.List<global::Inventory> { inventory };
                        newList.AddRange(allInventories);
                        allInventories = newList.ToArray();
                    }
                }
                else
                {
                    allInventories = new global::Inventory[] { inventory };
                }
            }
            else if (allInventories == null || allInventories.Length == 0)
            {
                allInventories = new global::Inventory[] { inventory };
            }

            if (backpackStateMachine == null)
            {
                backpackStateMachine = FindFirstObjectByType<BackpackStateMachine>();
            }

            if (inventoryCameraController == null)
            {
                inventoryCameraController = FindFirstObjectByType<InventoryCameraController>();
            }

            IsOpen = false;

            DraumLogger.Info(this, $"[InventoryModule] Inventory module initialized. Total inventories: {allInventories.Length}");

            SubscribeToEvents();
        }

        protected override void OnShutdown()
        {
            UnsubscribeFromEvents();
            
            if (IsOpen)
            {
                CloseInventory();
            }

            inventory = null;
            backpackStateMachine = null;
            inventoryCameraController = null;

            DraumLogger.Info(this, "[InventoryModule] Inventory module unloaded.");
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeToEvents()
        {
            Events.Subscribe<InventoryOpenRequestEvent>(OnInventoryOpenRequested);
            Events.Subscribe<InventoryCloseRequestEvent>(OnInventoryCloseRequested);
        }

        private void UnsubscribeFromEvents()
        {
            if (EventBus.Instance == null) return;
            Events.Unsubscribe<InventoryOpenRequestEvent>(OnInventoryOpenRequested);
            Events.Unsubscribe<InventoryCloseRequestEvent>(OnInventoryCloseRequested);
        }

        private void OnInventoryOpenRequested(InventoryOpenRequestEvent evt)
        {
            if (IsOpen)
            {
                CloseInventory();
            }
            else
            {
                OpenInventory();
            }
        }

        private void OnInventoryCloseRequested(InventoryCloseRequestEvent evt)
        {
            CloseInventory();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Открывает инвентарь
        /// </summary>
        public void OpenInventory()
        {
            if (IsOpen || inventory == null) return;

            IsOpen = true;
            SetCrosshairVisible(false);

            if (backpackStateMachine != null)
            {
                backpackStateMachine.OpenBackpack();
            }
            else if (inventoryCanvas != null)
            {
                inventoryCanvas.gameObject.SetActive(true);
            }

            Events.Publish(new InventoryOpenedEvent());

            DraumLogger.Info(this, "[InventoryModule] Inventory opened.");
        }

        /// <summary>
        /// Закрывает инвентарь
        /// </summary>
        public void CloseInventory()
        {
            if (!IsOpen || inventory == null) return;

            IsOpen = false;

            if (backpackStateMachine != null)
            {
                backpackStateMachine.CloseBackpack();
            }
            else if (inventoryCanvas != null)
            {
                inventoryCanvas.gameObject.SetActive(false);
            }

            if (inventory.selectedItem != null)
            {
                inventory.DeselectItem();
            }

            var questDialog = FindFirstObjectByType<DRAUM.Modules.Quest.UI.QuestDialogModule>();
            if (questDialog == null || !questDialog.dialogOpen)
            {
                SetCrosshairVisible(true);
            }

            Events.Publish(new InventoryClosedEvent());

            DraumLogger.Info(this, "[InventoryModule] Inventory closed.");
        }

        /// <summary>
        /// Переключает состояние инвентаря (открыть/закрыть)
        /// </summary>
        public void ToggleInventory()
        {
            if (IsOpen)
                CloseInventory();
            else
                OpenInventory();
        }

        /// <summary>
        /// Добавляет предмет в инвентарь
        /// </summary>
        public bool AddItem(ItemData itemData)
        {
            if (inventory == null || itemData == null) return false;

            bool success = inventory.AddItem(itemData);
            
            if (success)
            {
                Events.Publish(new ItemAddedEvent { ItemData = itemData });
                DraumLogger.Info(this, $"[InventoryModule] AddItem OK: {itemData.name} → {inventory.name}");
            }
            else
            {
                DraumLogger.Warning(this, $"[InventoryModule] AddItem FAILED: {itemData.name} → {inventory.name}");
            }

            return success;
        }

        /// <summary>
        /// Удаляет предмет из инвентаря
        /// </summary>
        public void RemoveItem(Item item)
        {
            if (inventory == null || item == null) return;

            ItemData itemData = item.data;
            string itemName = itemData != null ? itemData.name : item.name;
            inventory.RemoveItem(item);

            Events.Publish(new ItemRemovedEvent { ItemData = itemData });
            DraumLogger.Info(this, $"[InventoryModule] RemoveItem: {itemName} from {inventory.name}");
        }

        /// <summary>
        /// Получает предмет по данным (ищет во всех инвентарях модуля)
        /// </summary>
        public Item GetItem(ItemData itemData)
        {
            if (itemData == null) return null;

            var inventoriesToSearch = GetAllInventories();
            
            foreach (var inv in inventoriesToSearch)
            {
                if (inv == null || inv.grids == null) continue;

                foreach (var grid in inv.grids)
                {
                    if (grid == null || grid.items == null) continue;

                    for (int x = 0; x < grid.gridSize.x; x++)
                    {
                        for (int y = 0; y < grid.gridSize.y; y++)
                        {
                            Item item = grid.items[x, y];
                            if (item != null && item.data == itemData)
                            {
                                return item;
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Проверяет, есть ли предмет в инвентаре (проверяет все инвентари модуля)
        /// </summary>
        public bool HasItem(ItemData itemData)
        {
            return GetItem(itemData) != null;
        }

        /// <summary>
        /// Получает инвентарь по имени (например, "EquipmentInventory")
        /// </summary>
        public global::Inventory GetInventoryByName(string inventoryName)
        {
            if (allInventories == null || string.IsNullOrEmpty(inventoryName)) return null;
            
            foreach (var inv in allInventories)
            {
                if (inv != null && inv.name.Contains(inventoryName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return inv;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Получает все инвентари модуля
        /// </summary>
        public global::Inventory[] GetAllInventories()
        {
            if (autoFindAllInventories)
            {
                RefreshInventoriesSnapshot();
            }

            return allInventories != null && allInventories.Length > 0
                ? allInventories
                : new global::Inventory[] { inventory };
        }

        /// <summary>
        /// Считает количество предметов по ItemData во всех инвентарях.
        /// </summary>
        public int CountItems(ItemData itemData)
        {
            if (itemData == null) return 0;

            int count = 0;
            var inventoriesToSearch = GetAllInventories();
            if (inventoriesToSearch == null) return 0;

            foreach (var inv in inventoriesToSearch)
            {
                if (inv == null || inv.grids == null) continue;

                foreach (var grid in inv.grids)
                {
                    if (grid == null || grid.items == null) continue;

                    for (int x = 0; x < grid.gridSize.x; x++)
                    {
                        for (int y = 0; y < grid.gridSize.y; y++)
                        {
                            var item = grid.items[x, y];
                            if (item == null || item.data == null) continue;
                            if (IsSameItem(item.data, itemData)) count++;
                        }
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Ищет первый предмет, совпадающий с ItemData (ref или имя без учета регистра).
        /// </summary>
        public Item FindMatchingItem(ItemData itemData)
        {
            if (itemData == null) return null;

            var inventoriesToSearch = GetAllInventories();
            if (inventoriesToSearch == null) return null;

            foreach (var inv in inventoriesToSearch)
            {
                if (inv == null || inv.grids == null) continue;

                foreach (var grid in inv.grids)
                {
                    if (grid == null || grid.items == null) continue;

                    for (int x = 0; x < grid.gridSize.x; x++)
                    {
                        for (int y = 0; y < grid.gridSize.y; y++)
                        {
                            var item = grid.items[x, y];
                            if (item == null || item.data == null) continue;
                            if (IsSameItem(item.data, itemData)) return item;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Пытается снять нужное количество предметов из инвентаря.
        /// </summary>
        public bool TryConsumeItems(ItemData itemData, int amount)
        {
            if (itemData == null || amount <= 0) return false;
            int have = CountItems(itemData);
            if (have < amount)
            {
                DraumLogger.Warning(this, $"[InventoryModule] TryConsumeItems: need {amount}× {itemData.name}, have {have}");
                return false;
            }

            for (int i = 0; i < amount; i++)
            {
                var item = FindMatchingItem(itemData);
                if (item == null)
                {
                    DraumLogger.Warning(this, $"[InventoryModule] TryConsumeItems: FindMatchingItem null at step {i + 1}/{amount} ({itemData.name})");
                    return false;
                }
                RemoveItem(item);
            }

            Events.Publish(new InventoryChangedEvent());
            DraumLogger.Info(this, $"[InventoryModule] TryConsumeItems OK: removed {amount}× {itemData.name}");
            return true;
        }

        /// <summary>
        /// Находит подходящий инвентарь для предмета на основе категорий и фильтров
        /// Проверяет все инвентари модуля и возвращает первый, который может принять предмет
        /// </summary>
        public global::Inventory FindSuitableInventory(ItemData itemData)
        {
            if (itemData == null) return null;
            
            var inventoriesToSearch = GetAllInventories();
            if (inventoriesToSearch == null || inventoriesToSearch.Length == 0) return null;
            
            foreach (var inv in inventoriesToSearch)
            {
                if (inv == null || inv.grids == null) continue;
                
                foreach (var grid in inv.grids)
                {
                    if (grid == null) continue;
                    
                    if (!inv.CanPlaceItemInGrid(itemData, grid)) continue;
                    
                    var settings = inv.GetGridSettings(grid);
                    if (settings != null && (settings.slotMode == SlotMode.Equipment || settings.slotMode == SlotMode.Consumable))
                    {
                        if (HasFreeSlot(grid, 1, 1))
                        {
                            DraumLogger.Info(this, $"[InventoryModule] Found suitable {settings.slotMode} inventory: {inv.name} (Grid: {grid.name})");
                            return inv;
                        }
                    }
                }
            }
            
            foreach (var inv in inventoriesToSearch)
            {
                if (inv == null || inv.grids == null) continue;
                
                foreach (var grid in inv.grids)
                {
                    if (grid == null) continue;
                    
                    if (!inv.CanPlaceItemInGrid(itemData, grid)) continue;
                    
                    var settings = inv.GetGridSettings(grid);
                    if (settings == null || settings.slotMode == SlotMode.Normal)
                    {
                        int width = itemData.size.width;
                        int height = itemData.size.height;
                        
                        if (HasFreeSlot(grid, width, height) || HasFreeSlot(grid, height, width))
                        {
                            DraumLogger.Info(this, $"[InventoryModule] Found suitable normal inventory: {inv.name} (Grid: {grid.name})");
                            return inv;
                        }
                    }
                }
            }
            
            DraumLogger.Warning(this, $"[InventoryModule] No suitable inventory found for item {itemData.name}");
            return null;
        }
        
        /// <summary>
        /// Проверяет есть ли свободный слот в Grid для предмета указанного размера
        /// </summary>
        private bool HasFreeSlot(InventoryGrid grid, int width, int height)
        {
            if (grid == null || grid.items == null) return false;
            
            if (width > grid.gridSize.x || height > grid.gridSize.y) return false;
            
            for (int y = 0; y <= grid.gridSize.y - height; y++)
            {
                for (int x = 0; x <= grid.gridSize.x - width; x++)
                {
                    bool isFree = true;
                    
                    for (int checkY = 0; checkY < height && isFree; checkY++)
                    {
                        for (int checkX = 0; checkX < width && isFree; checkX++)
                        {
                            int slotX = x + checkX;
                            int slotY = y + checkY;
                            
                            if (slotX >= 0 && slotX < grid.gridSize.x && 
                                slotY >= 0 && slotY < grid.gridSize.y)
                            {
                                if (grid.items[slotX, slotY] != null)
                                {
                                    isFree = false;
                                }
                            }
                            else
                            {
                                isFree = false;
                            }
                        }
                    }
                    
                    if (isFree) return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Добавляет предмет в первый подходящий инвентарь (проверяет все инвентари)
        /// </summary>
        public bool AddItemToAnyInventory(ItemData itemData, string preferredInventoryName = null)
        {
            if (itemData == null) return false;
            
            if (!string.IsNullOrEmpty(preferredInventoryName))
            {
                var preferredInventory = GetInventoryByName(preferredInventoryName);
                if (preferredInventory != null && preferredInventory.AddItem(itemData))
                {
                    Events.Publish(new ItemAddedEvent { ItemData = itemData });
                    DraumLogger.Info(this, $"[InventoryModule] AddItemToAnyInventory OK (preferred): {itemData.name} → {preferredInventory.name}");
                    return true;
                }
            }
            
            var suitableInventory = FindSuitableInventory(itemData);
            if (suitableInventory != null && suitableInventory.AddItem(itemData))
            {
                Events.Publish(new ItemAddedEvent { ItemData = itemData });
                DraumLogger.Info(this, $"[InventoryModule] AddItemToAnyInventory OK: {itemData.name} → {suitableInventory.name}");
                return true;
            }
            
            DraumLogger.Warning(this, $"[InventoryModule] AddItemToAnyInventory FAILED: {itemData?.name}");
            return false;
        }

        #endregion

        #region Update

        protected override void OnUpdate()
        {
        }

        #endregion

        #region Private Helpers

        private Canvas inventoryCanvas
        {
            get
            {
                if (backpackStateMachine != null && backpackStateMachine.inventoryCanvas != null)
                    return backpackStateMachine.inventoryCanvas;
                
                if (inventory != null)
                    return inventory.GetComponentInParent<Canvas>();
                
                return null;
            }
        }

        private void RefreshInventoriesSnapshot()
        {
            var allFound = Resources.FindObjectsOfTypeAll<global::Inventory>();
            var unique = new System.Collections.Generic.List<global::Inventory>();
            var seen = new System.Collections.Generic.HashSet<int>();

            if (allFound != null)
            {
                for (int i = 0; i < allFound.Length; i++)
                {
                    var inv = allFound[i];
                    if (inv == null) continue;
                    if (!inv.gameObject.scene.IsValid()) continue;

                    int id = inv.GetInstanceID();
                    if (!seen.Add(id)) continue;
                    unique.Add(inv);
                }
            }

            if (inventory != null)
            {
                int mainId = inventory.GetInstanceID();
                if (!seen.Contains(mainId))
                {
                    unique.Insert(0, inventory);
                }
            }

            allInventories = unique.Count > 0
                ? unique.ToArray()
                : (inventory != null ? new global::Inventory[] { inventory } : Array.Empty<global::Inventory>());
        }

        private static bool IsSameItem(ItemData a, ItemData b)
        {
            if (a == null || b == null) return false;
            return ReferenceEquals(a, b) || a == b;
        }

        private void SetCrosshairVisible(bool visible)
        {
            if (crosshairToToggle == null) return;
            if (crosshairToToggle.activeSelf == visible) return;
            crosshairToToggle.SetActive(visible);
            DraumLogger.Info(this, $"[InventoryModule] Crosshair {(visible ? "enabled" : "disabled")}.");
        }

        #endregion
    }
}



