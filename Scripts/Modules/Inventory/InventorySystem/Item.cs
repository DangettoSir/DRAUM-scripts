using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DRAUM.Core.Infrastructure.Logger;

[RequireComponent(typeof(RectTransform))]
public class Item : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>
    /// Данные предмета, на которые ссылается этот скрипт
    /// </summary>
    public ItemData data;


    /// <summary>
    /// UI-компонент Image, который показывает фон иконки предмета.
    /// </summary>
    public Image background;

    /// <summary>
    /// Контейнер для 3D модели (опционально, создаётся динамически)
    /// </summary>
    [Header("3D Model (Runtime)")]
    public GameObject model3DInstance;
    
    /// <summary>
    /// Материал для highlight при наведении на мир
    /// </summary>
    [Header("World Placement Settings")]
    [Tooltip("Материал для подсветки (можно null)")]
    public Material highlightMaterial;
    
    [Tooltip("Offset по Y при размещении в мир")]
    public float worldPlacementYOffset = 0.1f;
    
    private Material[] originalMaterials;
    private Vector3 originalPrefabScale;
    private Vector3 targetWorldScale;

    /// <summary>
    /// Целевая ротация предмета
    /// </summary>
    private Vector3 rotateTarget;

    /// <summary>
    /// Флаг: был ли предмет повернут.
    /// </summary>
    public bool isRotated;

    /// <summary>
    /// Индекс поворота, по которому предмет выбирает следующий угол.
    /// </summary>
    public int rotateIndex;

    /// <summary>
    /// Индексная позиция предмета относительно грида, в котором он находится.
    /// </summary>
    public Vector2Int indexPosition { get; set; }

    /// <summary>
    /// Ссылка на основной инвентарь, с которым взаимодействует скрипт.
    /// </summary>
    public Inventory inventory { get; set; }

    /// <summary>
    /// Ссылка на RectTransform предмета.
    /// </summary>
    public RectTransform rectTransform { get; set; }

    /// <summary>
    /// Грид, в котором сейчас находится предмет.
    /// </summary>
    public InventoryGrid inventoryGrid { get; set; }

    /// <summary>
    /// Корректированная высота/ширина с учётом поворота.
    /// </summary>
    public SizeInt correctedSize
    {
        get
        { return new(!isRotated ? data.size.width : data.size.height, !isRotated ? data.size.height : data.size.width); }
    }

    /// <summary>
    /// Вызывается в кадре, когда скрипт включён, и перед первым вызовом Update.
    /// </summary>
    private void Start()
    {
        if (data != null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            bool isWorldSpace = (canvas != null && canvas.renderMode == RenderMode.WorldSpace);
            
            if (background != null)
            {
                if (isWorldSpace)
                {
                    Color bgColor = background.color;
                    bgColor.a = 0f;
                    background.color = bgColor;
                }
                else
                {
                    background.color = data.backgroundColor;
                }
            }

            Setup3DModel();
        }
    }

    /// <summary>
    /// Создаёт 3D модель предмета (если назначена в ItemData)
    /// </summary>
    private void Setup3DModel()
    {
        if (data == null || data.model3D == null)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
        {
            return;
        }

        model3DInstance = Instantiate(data.model3D, rectTransform);
        
        model3DInstance.transform.localPosition = data.inventoryPosition;
        model3DInstance.transform.localRotation = Quaternion.Euler(data.inventoryRotation);
        
        DisablePhysicsAndScripts(model3DInstance);
        
        originalPrefabScale = data.model3D.transform.localScale;
        
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        Vector3 normalizedScale = Vector3.Scale(originalPrefabScale, data.inventoryScale);
        
        if (parentCanvas != null)
        {
            Vector3 canvasScale = parentCanvas.transform.localScale;
            normalizedScale = new Vector3(
                normalizedScale.x / canvasScale.x,
                normalizedScale.y / canvasScale.y,
                normalizedScale.z / canvasScale.z
            );
        }
        
        model3DInstance.transform.localScale = normalizedScale;
        
        targetWorldScale = model3DInstance.transform.lossyScale;

        SaveOriginalMaterials();

        DraumLogger.Info(this, $"[Item] 3D модель создана для {data.name}. Prefab scale: {originalPrefabScale}, Target world scale: {targetWorldScale}");
    }

    /// <summary>
    /// Рекурсивно отключает Collider'ы, скрипты, Rigidbody на объекте и детях
    /// </summary>
    private void DisablePhysicsAndScripts(GameObject obj)
    {
        if (obj == null) return;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        Collider[] colliders = obj.GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        MonoBehaviour[] scripts = obj.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            script.enabled = false;
        }

        foreach (Transform child in obj.transform)
        {
            DisablePhysicsAndScripts(child.gameObject);
        }
    }

    /// <summary>
    /// Сохраняет оригинальные материалы модели
    /// </summary>
    private void SaveOriginalMaterials()
    {
        if (model3DInstance == null) return;

        Renderer[] renderers = model3DInstance.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            originalMaterials = renderers[0].materials;
        }
    }

    /// <summary>
    /// Включает highlight материал (сигнализирует что можно положить в мир)
    /// </summary>
    public void EnableHighlight()
    {
        if (model3DInstance == null || highlightMaterial == null) return;

        Renderer[] renderers = model3DInstance.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material[] mats = new Material[renderer.materials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = highlightMaterial;
            }
            renderer.materials = mats;
        }
        
        if (background != null)
        {
            Color bgColor = background.color;
            bgColor.a = 0f;
            background.color = bgColor;
        }
    }

    /// <summary>
    /// Отключает highlight (возвращает оригинальные материалы)
    /// </summary>
    public void DisableHighlight()
    {
        if (model3DInstance == null || originalMaterials == null) return;

        Renderer[] renderers = model3DInstance.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            renderers[0].materials = originalMaterials;
        }
        
        Canvas canvas = GetComponentInParent<Canvas>();
        bool isWorldSpace = (canvas != null && canvas.renderMode == RenderMode.WorldSpace);
        
        if (isWorldSpace)
        {
            if (background != null)
            {
                Color bgColor = background.color;
                bgColor.a = 0f;
                background.color = bgColor;
            }
        }
        else
        {
            if (background != null)
            {
                Color bgColor = background.color;
                bgColor.a = 1f;
                background.color = bgColor;
            }
        }
    }

    /// <summary>
    /// Подготавливает предмет к размещению в мире
    /// </summary>
    public void PrepareForWorldPlacement()
    {
        if (model3DInstance == null) return;
        
        model3DInstance.transform.localScale = originalPrefabScale;
    }

    /// <summary>
    /// Возвращает предмет в инвентарь
    /// </summary>
    public void PrepareForInventory()
    {
        if (model3DInstance == null) return;
        
        model3DInstance.transform.localScale = originalPrefabScale;
    }
    
    /// <summary>
    /// Получить целевой мировой scale (для размещения в мире)
    /// </summary>
    public Vector3 GetTargetWorldScale()
    {
        return targetWorldScale;
    }
    
    
    /// <summary>
    /// Вызывается при наведении курсора на предмет
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        ItemTooltipController tooltip = FindFirstObjectByType<ItemTooltipController>();
        if (tooltip != null)
        {
            tooltip.ShowTooltip(this);
        }
    }
    
    /// <summary>
    /// Вызывается при уходе курсора с предмета
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltipController tooltip = FindFirstObjectByType<ItemTooltipController>();
        if (tooltip != null)
        {
            tooltip.HideTooltip();
        }
    }

    /// <summary>
    /// Удаляет 3D модель при уничтожении предмета
    /// </summary>
    private void OnDestroy()
    {
        if (model3DInstance != null)
        {
            Destroy(model3DInstance);
        }
    }

    /// <summary>
    /// Вызывается каждый кадр, если Behaviour включён.
    /// Вызывается после всех вызовов Update.
    /// </summary>
    private void LateUpdate()
    {
        UpdateRotateAnimation();
    }

    /// <summary>
    /// Поворачивает предмет на нужную игроку позицию.
    /// </summary>
    public void Rotate()
    {
        if (rotateIndex < 3)
        {
            rotateIndex++;
        }
        else if (rotateIndex >= 3)
        {
            rotateIndex = 0;
        }

        UpdateRotation();
    }

    /// <summary>
    /// Сбрасывает индекс поворота.
    /// </summary>
    public void ResetRotate()
    {
        rotateIndex = 0;
        UpdateRotation();
    }

    /// <summary>
    /// Обновляет целевую ротацию по индексу поворота.
    /// </summary>
    private void UpdateRotation()
    {
        switch (rotateIndex)
        {
            case 0:
                rotateTarget = new(0, 0, 0);
                isRotated = false;
                break;

            case 1:
                rotateTarget = new(0, 0, -90);
                isRotated = true;
                break;

            case 2:
                rotateTarget = new(0, 0, -180);
                isRotated = false;
                break;

            case 3:
                rotateTarget = new(0, 0, -270);
                isRotated = true;
                break;
        }
    }

    /// <summary>
    /// Обновляет анимацию поворота предмета.
    /// </summary>
    private void UpdateRotateAnimation()
    {
        Quaternion targetRotation = Quaternion.Euler(rotateTarget);

        if (rectTransform.localRotation != targetRotation)
        {
            rectTransform.localRotation = Quaternion.Slerp(
                rectTransform.localRotation,
                targetRotation,
                InventorySettings.rotationAnimationSpeed * Time.deltaTime
            );
        }
    }
}