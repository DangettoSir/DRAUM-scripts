using UnityEngine;
using UnityEngine.EventSystems;
using DRAUM.Core.Infrastructure.Logger;

[RequireComponent(typeof(RectTransform))]
public class InventoryGrid : MonoBehaviour, IPointerEnterHandler
{
    /// <summary>
    /// Размер слота в 1x1 (для расчётов сетки)
    /// </summary>
    [Header("Grid Config")]
    public Vector2Int gridSize = new(5, 5);

    /// <summary>
    /// Ссылка на основной `RectTransform` грида.
    /// </summary>
    public RectTransform rectTransform;

    /// <summary>
    /// Приоритет грида. Меньшее значение = выше приоритет при добавлении предметов.
    /// Центральный инвентарь должен иметь priority = 0.
    /// </summary>
    [Header("Priority")]
    [Tooltip("Lower value = higher priority. Central inventory should be 0")]
    public int priority = 1;
    
    /// <summary>
    /// Настройки подсветки слотов
    /// </summary>
    [Header("Slot Highlighting")]
    [Tooltip("Включить подсветку слотов при перемещении предметов")]
    public bool enableSlotHighlighting = true;
    
    [Tooltip("Цвет подсветки слота")]
    public Color highlightColor = Color.white;
    
    [Tooltip("Прозрачность подсветки")]
    [Range(0f, 1f)]
    public float highlightAlpha = 0.3f;
    
    /// <summary>
    /// Визуальные индикаторы подсветки слотов
    /// </summary>
    private GameObject[] slotHighlighters;
    
    /// <summary>
    /// Prefab для подсветки слота
    /// </summary>
    [Tooltip("Prefab для подсветки слота (Image с белым цветом)")]
    public GameObject slotHighlighterPrefab;

    /// <summary>
    /// Массив предметов.
    /// </summary>
    public Item[,] items { get; set; }

    /// <summary>
    /// Ссылка на основной инвентарь.
    /// </summary>
    public Inventory inventory { get; private set; }

    private void Awake()
    {
        if (rectTransform != null)
        {
            inventory = UnityEngine.Object.FindFirstObjectByType<Inventory>();
            InitializeGrid();
        }
        else
        {
            DraumLogger.Error(this, "(InventoryGrid) RectTransform not found!");
        }
    }

    /// <summary>
    /// Инициализирует матрицу предметов и размеры грида.
    /// </summary>
    private void InitializeGrid()
    {
        items = new Item[gridSize.x, gridSize.y];

        Vector2 size =
            new(
                gridSize.x * InventorySettings.slotSize.x,
                gridSize.y * InventorySettings.slotSize.y
            );
        rectTransform.sizeDelta = size;
    }

    /// <summary>
    /// Делает этот грид главным под курсором мыши.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        inventory.gridOnMouse = this;
    }
    
    /// <summary>
    /// Показывает иконку предмета под курсором для указанного предмета
    /// </summary>
    public void ShowItemPreviewUnderCursor(Vector2Int slotPosition, Vector2Int itemSize, ItemData itemData)
    {
        if (!enableSlotHighlighting) return;
        
        if (slotPosition.x < 0 || slotPosition.y < 0 || 
            slotPosition.x + itemSize.x > gridSize.x || 
            slotPosition.y + itemSize.y > gridSize.y)
        {
            ClearSlotHighlighting();
            return;
        }
        
        CreateItemPreview(slotPosition, itemSize, itemData);
    }
    
    /// <summary>
    /// Создаёт превью иконки предмета
    /// </summary>
    private void CreateItemPreview(Vector2Int slotPosition, Vector2Int itemSize, ItemData itemData)
    {
        ClearSlotHighlighting();
        
        if (itemData == null) return;
        
        GameObject previewObject = new GameObject("ItemPreview");
        previewObject.transform.SetParent(rectTransform);
        
        RectTransform previewRect = previewObject.AddComponent<RectTransform>();
        
        Vector2 previewSize = new Vector2(
            itemSize.x * InventorySettings.slotSize.x,
            itemSize.y * InventorySettings.slotSize.y
        );
        previewRect.sizeDelta = previewSize;
        
        Vector2 previewPosition = new Vector2(
            slotPosition.x * InventorySettings.slotSize.x + previewSize.x / 2,
            -(slotPosition.y * InventorySettings.slotSize.y + previewSize.y / 2)
        );
        previewRect.anchoredPosition = previewPosition;
        
        slotHighlighters = new GameObject[] { previewObject };
    }
    
    
    /// <summary>
    /// Убирает подсветку всех слотов
    /// </summary>
    public void ClearSlotHighlighting()
    {
        if (!enableSlotHighlighting) return;
        
        if (slotHighlighters != null)
        {
            foreach (GameObject highlighter in slotHighlighters)
            {
                if (highlighter != null)
                {
                    DestroyImmediate(highlighter);
                }
            }
            slotHighlighters = null;
        }
    }
    
    [Header("Slot Highlighting (Backup)")]
    [Tooltip("Включить подсветку слотов при перемещении предметов")]
    public bool enableSlotHighlightingBackup = true;
    
    [Tooltip("Цвет подсветки слота")]
    public Color highlightColorBackup = Color.white;
    
    [Tooltip("Прозрачность подсветки")]
    [Range(0f, 1f)]
    public float highlightAlphaBackup = 0.3f;
    
    [Tooltip("Prefab для подсветки слота (Image с белым цветом)")]
    public GameObject slotHighlighterPrefabBackup;
}