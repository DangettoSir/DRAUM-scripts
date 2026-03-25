using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DRAUM.Core.Infrastructure.Logger;
using DRAUM.Modules.Player;
using DRAUM.Modules.Player.Effects;
using DRAUM.Modules.Combat.Animation;

/// <summary>
/// Управляет контекстным меню для предметов в инвентаре (ПКМ)
/// Показывает опции: Выкинуть, Употребить, Экипировать, Прочитать
/// </summary>
public class InventoryContextMenuController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ContextMenuPlane GameObject (UI панель с кнопками)")]
    public GameObject contextMenuPlane;
    
    [Tooltip("Контейнер для кнопок (обычно VerticalLayoutGroup внутри ContextMenuPlane)")]
    public Transform buttonsContainer;
    
    [Tooltip("Prefab кнопки для опций меню")]
    public GameObject buttonPrefab;
    
    [Header("Category Settings")]
    [Tooltip("Категории предметов которые можно экипировать (например, TwoHandedWeapon)")]
    public ItemCategory[] equippableCategories = new ItemCategory[0];
    
    [Tooltip("Категории предметов которые можно читать (например, Book, Note)")]
    public ItemCategory[] readableCategories = new ItemCategory[0];
    
    [Tooltip("Категория Consumable для расходников")]
    public ItemCategory consumableCategory;
    
    [Header("Button Settings")]
    [Tooltip("Размер кнопки (ширина x высота)")]
    public Vector2 buttonSize = new Vector2(1000f, 400f);
    
    [Tooltip("Отступ между кнопками")]
    public float buttonSpacing = 10f;
    
    [Tooltip("Использовать плавное появление")]
    public bool useFadeIn = true;
    
    [Tooltip("Скорость fade in")]
    [Range(1f, 20f)]
    public float fadeInSpeed = 10f;
    
    [Header("Debug")]
    [Tooltip("Показывать debug логи")]
    [HideInInspector] public bool showDebugLogs = false;
    
    private Inventory inventory;
    private FPMAxe fpAxe;
    private PlayerEntity playerEntity;
    private ConsumableAnimationHandler consumableAnimationHandler;
    private CanvasGroup menuCanvasGroup;
    private RectTransform menuRectTransform;
    
    private Item currentItem = null;
    private List<GameObject> currentButtons = new List<GameObject>();
    
    private void Awake()
    {
        if (inventory == null)
        {
            inventory = GetComponent<Inventory>();
            if (inventory == null)
            {
                inventory = FindFirstObjectByType<Inventory>();
            }
        }
        
        if (fpAxe == null)
            fpAxe = FindFirstObjectByType<FPMAxe>();
        if (playerEntity == null)
            playerEntity = FindFirstObjectByType<PlayerEntity>();
        if (consumableAnimationHandler == null)
            consumableAnimationHandler = FindFirstObjectByType<ConsumableAnimationHandler>();
        
        if (contextMenuPlane != null)
        {
            menuRectTransform = contextMenuPlane.GetComponent<RectTransform>();
            
            menuCanvasGroup = contextMenuPlane.GetComponent<CanvasGroup>();
            if (menuCanvasGroup == null)
            {
                menuCanvasGroup = contextMenuPlane.AddComponent<CanvasGroup>();
            }
            
            contextMenuPlane.SetActive(false);
        }
        
        if (buttonsContainer == null && contextMenuPlane != null)
        {
            buttonsContainer = contextMenuPlane.transform.Find("ButtonsContainer");
            if (buttonsContainer == null)
            {
                VerticalLayoutGroup layoutGroup = contextMenuPlane.GetComponentInChildren<VerticalLayoutGroup>();
                if (layoutGroup != null)
                {
                    buttonsContainer = layoutGroup.transform;
                }
                else
                {
                    buttonsContainer = contextMenuPlane.transform;
                    
                    VerticalLayoutGroup newLayout = contextMenuPlane.GetComponent<VerticalLayoutGroup>();
                    if (newLayout == null)
                    {
                        newLayout = contextMenuPlane.AddComponent<VerticalLayoutGroup>();
                        newLayout.spacing = buttonSpacing;
                        newLayout.childAlignment = TextAnchor.UpperCenter;
                        newLayout.childControlWidth = true;
                        newLayout.childControlHeight = false;
                        newLayout.childForceExpandWidth = true;
                        newLayout.childForceExpandHeight = false;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Показать контекстное меню для предмета
    /// </summary>
    public void ShowContextMenu(Item item)
    {
        if (item == null || item.data == null)
        {
            if (showDebugLogs) DraumLogger.Warning(this, "[InventoryContextMenu] ShowContextMenu: item или data null!");
            return;
        }
        
        if (contextMenuPlane == null)
        {
            if (showDebugLogs) DraumLogger.Warning(this, "[InventoryContextMenu] ContextMenuPlane не назначен!");
            return;
        }
        
        currentItem = item;
        
        ClearButtons();
        
        CreateMenuButtons(item);
        
        contextMenuPlane.SetActive(true);
        
        if (useFadeIn && menuCanvasGroup != null)
        {
            StartCoroutine(FadeInMenu());
        }
        else if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 1f;
        }
        
        if (showDebugLogs)
        {
            DraumLogger.Info(this, $"[InventoryContextMenu] Показано меню для: {item.data.name}");
        }
    }
    
    /// <summary>
    /// Скрыть контекстное меню
    /// </summary>
    public void HideContextMenu()
    {
        if (contextMenuPlane != null)
        {
            contextMenuPlane.SetActive(false);
        }
        
        ClearButtons();
        currentItem = null;
        
        if (showDebugLogs)
        {
            DraumLogger.Info(this, "[InventoryContextMenu] Меню скрыто");
        }
    }
    
    /// <summary>
    /// Создаёт кнопки меню в зависимости от свойств предмета
    /// </summary>
    private void CreateMenuButtons(Item item)
    {
        if (buttonsContainer == null)
        {
            if (showDebugLogs) DraumLogger.Warning(this, "[InventoryContextMenu] buttonsContainer не назначен!");
            return;
        }
        
        ItemData itemData = item.data;
        
        CreateButton("Выкинуть", () => OnDropItem(item));
        
        bool isConsumable = consumableCategory != null && itemData.HasCategory(consumableCategory);
        bool isEquippable = HasAnyCategory(itemData, equippableCategories);
        bool isReadable = HasAnyCategory(itemData, readableCategories);
        
        if (isConsumable)
        {
            CreateButton("Употребить", () => OnConsumeItem(item));
        }
        
        if (isEquippable)
        {
            CreateButton("Экипировать", () => OnEquipItem(item));
        }
        
        if (isReadable)
        {
            CreateButton("Прочитать", () => OnReadItem(item));
        }
    }
    
    /// <summary>
    /// Создаёт кнопку в меню
    /// </summary>
    private void CreateButton(string text, System.Action onClick)
    {
        GameObject buttonObj;
        
        if (buttonPrefab != null)
        {
            buttonObj = Instantiate(buttonPrefab, buttonsContainer, false);
            
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                buttonRect.sizeDelta = buttonSize;
                buttonRect.localScale = Vector3.one;
            }
        }
        else
        {
            buttonObj = new GameObject($"Button_{text}");
            buttonObj.transform.SetParent(buttonsContainer, false);
            
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = buttonSize;
            buttonRect.localScale = Vector3.one;
            buttonRect.anchoredPosition = Vector2.zero;
            buttonRect.anchorMin = new Vector2(0.5f, 1f);
            buttonRect.anchorMax = new Vector2(0.5f, 1f);
            buttonRect.pivot = new Vector2(0.5f, 1f);
            
            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            
            Button button = buttonObj.AddComponent<Button>();
            
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontSize = 100;
            textComponent.raycastTarget = false;
            
            RectTransform textRect = textComponent.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
        }
        
        Button buttonComponent = buttonObj.GetComponent<Button>();
        if (buttonComponent != null)
        {
            buttonComponent.onClick.RemoveAllListeners();
            
            buttonComponent.onClick.AddListener(() =>
            {
                if (showDebugLogs)
                {
                    DraumLogger.Info(this, $"[InventoryContextMenu] Нажата кнопка: {text}");
                }
                
                onClick?.Invoke();
                HideContextMenu();
            });
            
            buttonComponent.interactable = true;
            
            var colors = buttonComponent.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            colors.pressedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            colors.selectedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            colors.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            buttonComponent.colors = colors;
        }
        
        Image buttonImage = buttonObj.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.raycastTarget = true;
        }
        else
        {
            buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            buttonImage.raycastTarget = true;
        }
        
        TextMeshProUGUI textMesh = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (textMesh != null)
        {
            textMesh.text = text;
        }
        
        Canvas canvas = buttonObj.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            UnityEngine.UI.GraphicRaycaster raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            
            if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
                if (showDebugLogs)
                {
                    DraumLogger.Warning(this, $"[InventoryContextMenu] Canvas {canvas.name} не имеет Event Camera! Назначен Camera.main");
                }
            }
        }
        
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (showDebugLogs)
            {
                DraumLogger.Warning(this, "[InventoryContextMenu] EventSystem не найден! Создан автоматически.");
            }
        }
        
        RectTransform finalRect = buttonObj.GetComponent<RectTransform>();
        if (finalRect != null)
        {
            finalRect.sizeDelta = buttonSize;
            if (showDebugLogs)
            {
                DraumLogger.Info(this, $"[InventoryContextMenu] Создана кнопка '{text}' с размером: {finalRect.sizeDelta}");
            }
        }
        
        currentButtons.Add(buttonObj);
    }
    
    /// <summary>
    /// Очищает все кнопки меню
    /// </summary>
    private void ClearButtons()
    {
        foreach (GameObject button in currentButtons)
        {
            if (button != null)
            {
                Destroy(button);
            }
        }
        currentButtons.Clear();
    }
    
    
    /// <summary>
    /// Fade in анимация для меню
    /// </summary>
    private System.Collections.IEnumerator FadeInMenu()
    {
        if (menuCanvasGroup == null) yield break;
        
        menuCanvasGroup.alpha = 0f;
        float alpha = 0f;
        
        while (alpha < 1f)
        {
            alpha += Time.deltaTime * fadeInSpeed;
            menuCanvasGroup.alpha = Mathf.Clamp01(alpha);
            yield return null;
        }
    }
    
    /// <summary>
    /// Проверяет есть ли у предмета любая из указанных категорий
    /// </summary>
    private bool HasAnyCategory(ItemData itemData, ItemCategory[] categories)
    {
        if (itemData == null || categories == null || categories.Length == 0) return false;
        
        foreach (ItemCategory category in categories)
        {
            if (category != null && itemData.HasCategory(category))
            {
                return true;
            }
        }
        
        return false;
    }
    
    #region Menu Actions
    
    /// <summary>
    /// Выкинуть предмет из инвентаря
    /// </summary>
    private void OnDropItem(Item item)
    {
        if (item == null || inventory == null) return;
        
        InventoryController controller = FindFirstObjectByType<InventoryController>();
        if (controller != null)
        {
            var method = typeof(InventoryController).GetMethod("PlaceItemInWorld", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (method != null)
            {
                try
                {
                    method.Invoke(controller, new object[] { item });
                    
                    if (showDebugLogs)
                    {
                        DraumLogger.Info(this, $"[InventoryContextMenu] Предмет {item.data.name} выкинут в мир");
                    }
                }
                catch (System.Exception e)
                {
                    DraumLogger.Error(this, $"[InventoryContextMenu] Ошибка при выкидывании предмета: {e.Message}");
                    inventory.RemoveItem(item);
                }
            }
            else
            {
                inventory.RemoveItem(item);
                
                if (showDebugLogs)
                {
                    DraumLogger.Info(this, $"[InventoryContextMenu] Предмет {item.data.name} удалён из инвентаря (PlaceItemInWorld не найден)");
                }
            }
        }
        else
        {
            inventory.RemoveItem(item);
            
            if (showDebugLogs)
            {
                DraumLogger.Info(this, $"[InventoryContextMenu] Предмет {item.data.name} удалён из инвентаря (InventoryController не найден)");
            }
        }
        
        if (DRAUM.Core.EventBus.Instance != null)
        {
            DRAUM.Core.EventBus.Instance.Publish(new DRAUM.Modules.Player.Events.UISoundEvent 
            { 
                SoundType = DRAUM.Modules.Player.Events.UISoundType.RightClick,
                Context = "Inventory_Drop"
            });
        }
    }
    
    /// <summary>
    /// Употребить предмет (Consumable). Если есть ConsumableAnimationHandler и consumablePrefab — запускает анимацию.
    /// Иначе применяет эффект сразу и удаляет предмет (старое поведение для совместимости).
    /// </summary>
    private void OnConsumeItem(Item item)
    {
        if (item == null || item.data == null) return;

        if (consumableAnimationHandler != null && item.data.consumablePrefab != null)
        {
            consumableAnimationHandler.StartConsume(item);
            if (showDebugLogs)
                DraumLogger.Info(this, $"[InventoryContextMenu] Запущена анимация потребления для {item.data.name}");
        }
        else
        {
            if (playerEntity != null && item.data.consumableEffect != null)
            {
                PlayerEffect effect = item.data.consumableEffect.CreateEffect();
                playerEntity.AddEffect(effect);
                if (showDebugLogs)
                    DraumLogger.Info(this, $"[InventoryContextMenu] Наложен эффект {effect.DisplayName} на {effect.RemainingTime:F0} сек от предмета {item.data.name}");
            }
            else if (showDebugLogs)
            {
                DraumLogger.Info(this, $"[InventoryContextMenu] Употреблён предмет: {item.data.name}" + (item.data.consumableEffect == null ? " (без эффекта)" : ""));
            }

            if (inventory != null)
                inventory.RemoveItem(item);
        }

        if (DRAUM.Core.EventBus.Instance != null)
        {
            DRAUM.Core.EventBus.Instance.Publish(new DRAUM.Modules.Player.Events.UISoundEvent
            {
                SoundType = DRAUM.Modules.Player.Events.UISoundType.RightClick,
                Context = "Inventory_Consume"
            });
        }
    }
    
    /// <summary>
    /// Экипировать предмет
    /// </summary>
    private void OnEquipItem(Item item)
    {
        if (item == null || item.data == null) return;
        
        if (inventory != null)
        {
            InventoryGrid[] equipmentGrids = inventory.GetEquipmentGrids();
            
            for (int i = 0; i < equipmentGrids.Length && i < 3; i++)
            {
                InventoryGrid grid = equipmentGrids[i];
                if (grid == null) continue;
                
                if (!inventory.CanPlaceItemInGrid(item.data, grid))
                {
                    continue;
                }
                
                if (grid.items != null && grid.items[0, 0] == null)
                {
                    inventory.MoveItemToGrid(item, grid, Vector2Int.zero);
                    
                    if (fpAxe != null)
                    {
                        fpAxe.ToggleEquipSlot(i);
                    }
                    
                    if (showDebugLogs)
                    {
                        DraumLogger.Info(this, $"[InventoryContextMenu] Предмет {item.data.name} экипирован в слот {i}");
                    }
                    
                    if (DRAUM.Core.EventBus.Instance != null)
                    {
                        DRAUM.Core.EventBus.Instance.Publish(new DRAUM.Modules.Player.Events.UISoundEvent 
                        { 
                            SoundType = DRAUM.Modules.Player.Events.UISoundType.RightClick,
                            Context = "Inventory_Equip"
                        });
                    }
                    
                    return;
                }
            }
            
            DraumLogger.Warning(this, $"[InventoryContextMenu] Нет свободных Equipment слотов для {item.data.name} или предмет не проходит фильтр категорий");
        }
    }
    
    /// <summary>
    /// Прочитать предмет (Readable)
    /// </summary>
    private void OnReadItem(Item item)
    {
        if (item == null || item.data == null) return;
        
        DraumLogger.Info(this, $"[InventoryContextMenu] Прочитан предмет: {item.data.name}");
        DraumLogger.Info(this, $"[InventoryContextMenu] Описание: {item.data.description}");
        
        if (DRAUM.Core.EventBus.Instance != null)
        {
            DRAUM.Core.EventBus.Instance.Publish(new DRAUM.Modules.Player.Events.UISoundEvent 
            { 
                SoundType = DRAUM.Modules.Player.Events.UISoundType.RightClick,
                Context = "Inventory_Read"
            });
        }
    }
    
    #endregion
    
    private void Update()
    {
        if (contextMenuPlane != null && contextMenuPlane.activeSelf)
        {
            if (Input.GetMouseButtonDown(1))
            {
                if (!IsPointerOverMenu() && !IsPointerOverButton())
                {
                    HideContextMenu();
                }
            }
            else if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverMenu() || IsPointerOverButton())
                {
                    return;
                }
                HideContextMenu();
            }
        }
    }
    
    /// <summary>
    /// Проверяет находится ли курсор над кнопками меню
    /// </summary>
    private bool IsPointerOverButton()
    {
        if (currentButtons == null || currentButtons.Count == 0) return false;
        
        Canvas canvas = contextMenuPlane != null ? contextMenuPlane.GetComponentInParent<Canvas>() : null;
        if (canvas == null) return false;
        
        Camera cam = canvas.renderMode == RenderMode.WorldSpace ? (canvas.worldCamera ?? Camera.main) : null;
        
        foreach (var button in currentButtons)
        {
            if (button == null) continue;
            
            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect == null) continue;
            
            Vector2 mousePos = Input.mousePosition;
            
            if (canvas.renderMode == RenderMode.WorldSpace && cam != null)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, cam))
                {
                    return true;
                }
            }
            else
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Проверяет находится ли курсор над меню (для WorldSpace и ScreenSpace)
    /// </summary>
    private bool IsPointerOverMenu()
    {
        if (contextMenuPlane == null) return false;
        
        Canvas canvas = contextMenuPlane.GetComponentInParent<Canvas>();
        if (canvas == null) return false;
        
        RectTransform rect = contextMenuPlane.GetComponent<RectTransform>();
        if (rect == null) return false;
        
        Vector2 mousePos = Input.mousePosition;
        
        if (canvas.renderMode == RenderMode.WorldSpace)
        {
            Camera cam = canvas.worldCamera ?? Camera.main;
            if (cam != null)
            {
                return RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, cam);
            }
        }
        else
        {
            return RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos);
        }
        
        return false;
    }
}
