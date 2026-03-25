using UnityEngine;
using DRAUM.Core.Infrastructure.Logger;

/// <summary>
/// Мост для Animation Events Eye Blink эффекта
/// Должен быть на том же GameObject что и Volume Animator
/// </summary>
public class EyeBlinkBridge : MonoBehaviour
{
    [Header("Eye Blink Bridge")]
    [Tooltip("Ссылка на InventoryCameraController для перенаправления вызовов")]
    public InventoryCameraController inventoryCameraController;
    
    [Tooltip("Показывать debug логи")]
    [HideInInspector] public bool showDebugLogs = false;
    
    /// <summary>
    /// Тестовый метод для проверки Animation Events
    /// </summary>
    public void TestAnimationEvent()
    {
        DraumLogger.Info(this, "[EyeBlinkBridge] Тестовый Animation Event работает!");
    }
    
    /// <summary>
    /// Вызывается Animation Event когда глаз полностью закрыт
    /// </summary>
    public void OnEyeBlinkClosed()
    {
        if (showDebugLogs)
        {
            DraumLogger.Info(this, "[EyeBlinkBridge] OnEyeBlinkClosed() вызван!");
        }
        
        if (inventoryCameraController != null)
        {
            inventoryCameraController.OnEyeBlinkClosed();
        }
        else if (showDebugLogs)
        {
            DraumLogger.Warning(this, "[EyeBlinkBridge] InventoryCameraController не назначен!");
        }
    }
    
    /// <summary>
    /// Вызывается Animation Event когда глаз полностью открыт (конец анимации)
    /// </summary>
    public void OnEyeBlinkOpened()
    {
        if (showDebugLogs)
        {
            DraumLogger.Info(this, "[EyeBlinkBridge] OnEyeBlinkOpened() вызван!");
        }
        
        if (inventoryCameraController != null)
        {
            inventoryCameraController.OnEyeBlinkOpened();
        }
        else if (showDebugLogs)
        {
            DraumLogger.Warning(this, "[EyeBlinkBridge] InventoryCameraController не назначен!");
        }
    }
}
