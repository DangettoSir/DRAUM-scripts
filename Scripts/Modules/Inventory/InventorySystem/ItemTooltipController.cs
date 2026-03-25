using System.Collections;
using UnityEngine;
using TMPro;
using DRAUM.Core.Infrastructure.Logger;

/// <summary>
/// Управляет Tooltip'ом предметов в инвентаре
/// </summary>
public class ItemTooltipController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Tooltip Plane (UI элемент)")]
    public GameObject tooltipPlane;
    
    [Tooltip("Текст названия предмета")]
    public TextMeshProUGUI itemNameText;
    
    [Tooltip("Текст описания предмета")]
    public TextMeshProUGUI itemDescriptionText;
    
    [Header("Settings")]
    [Tooltip("Задержка перед появлением tooltip (секунды)")]
    [Range(0f, 2f)]
    public float showDelay = 0.2f;
    
    [Tooltip("Offset от курсора мыши")]
    public Vector2 cursorOffset = new Vector2(10f, -10f);
    
    [Tooltip("Использовать плавное появление")]
    public bool useFadeIn = true;
    
    [Tooltip("Скорость fade in")]
    [Range(1f, 20f)]
    public float fadeInSpeed = 10f;
    
    [Header("Debug")]
    [Tooltip("Показывать debug логи")]
    [HideInInspector] public bool showDebugLogs = false;
    
    private Item hoveredItem = null;
    private Coroutine showTooltipCoroutine = null;
    private CanvasGroup tooltipCanvasGroup;
    private RectTransform tooltipRectTransform;
    
    private void Awake()
    {
        if (tooltipPlane != null)
        {
            tooltipRectTransform = tooltipPlane.GetComponent<RectTransform>();
            
            tooltipCanvasGroup = tooltipPlane.GetComponent<CanvasGroup>();
            if (tooltipCanvasGroup == null)
            {
                tooltipCanvasGroup = tooltipPlane.AddComponent<CanvasGroup>();
            }
            
            tooltipPlane.SetActive(false);
        }
    }
    
    private void Update()
    {
        if (tooltipPlane != null && tooltipPlane.activeSelf)
        {
            UpdateTooltipPosition();
        }
    }
    
    /// <summary>
    /// Показать tooltip для предмета (вызывается при наведении)
    /// </summary>
    public void ShowTooltip(Item item)
    {
        if (item == null || item.data == null) return;
        
        if (hoveredItem == item) return;
        
        hoveredItem = item;
        
        if (showTooltipCoroutine != null)
        {
            StopCoroutine(showTooltipCoroutine);
        }
        
        showTooltipCoroutine = StartCoroutine(ShowTooltipWithDelay(item));
    }
    
    /// <summary>
    /// Скрыть tooltip (вызывается при уходе курсора с предмета)
    /// </summary>
    public void HideTooltip()
    {
        hoveredItem = null;
        
        if (showTooltipCoroutine != null)
        {
            StopCoroutine(showTooltipCoroutine);
            showTooltipCoroutine = null;
        }
        
        if (tooltipPlane != null)
        {
            tooltipPlane.SetActive(false);
        }
    }
    
    private IEnumerator ShowTooltipWithDelay(Item item)
    {
        yield return new WaitForSeconds(showDelay);
        
        if (hoveredItem != item) yield break;
        
        if (itemNameText != null)
        {
            itemNameText.text = item.data.name;
        }
        
        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = item.data.description;
        }
        
        if (tooltipPlane != null)
        {
            tooltipPlane.SetActive(true);
            
            if (useFadeIn && tooltipCanvasGroup != null)
            {
                tooltipCanvasGroup.alpha = 0f;
                float alpha = 0f;
                
                while (alpha < 1f)
                {
                    alpha += Time.deltaTime * fadeInSpeed;
                    tooltipCanvasGroup.alpha = Mathf.Clamp01(alpha);
                    yield return null;
                }
            }
            else if (tooltipCanvasGroup != null)
            {
                tooltipCanvasGroup.alpha = 1f;
            }
        }
        
        if (showDebugLogs) DraumLogger.Info(this, $"[ItemTooltip] Показан tooltip для: {item.data.name}");
    }
    
    private void UpdateTooltipPosition()
    {
        if (tooltipRectTransform == null) return;
        
        Vector2 position = Input.mousePosition + new Vector3(cursorOffset.x, cursorOffset.y, 0f);
        
        RectTransform canvasRect = tooltipRectTransform.parent as RectTransform;
        if (canvasRect != null)
        {
            Vector2 tooltipSize = tooltipRectTransform.sizeDelta;
            
            float maxX = canvasRect.rect.width - tooltipSize.x;
            position.x = Mathf.Clamp(position.x, 0f, maxX);
            
            float maxY = canvasRect.rect.height - tooltipSize.y;
            position.y = Mathf.Clamp(position.y, 0f, maxY);
        }
        
        tooltipRectTransform.position = position;
    }
}

