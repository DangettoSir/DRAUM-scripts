using UnityEngine;
using UnityEngine.Rendering;
using DRAUM.Core.Infrastructure.Logger;
using BeautifyEffect = Beautify.Universal.Beautify;

/// <summary>
/// Proxy для анимации Beautify через Animator.
/// </summary>
public class BeautifyBlinkAnimator : MonoBehaviour
{
    [Header("Volume Reference")]
    [Tooltip("Volume с Beautify override (автопоиск если пусто)")]
    public Volume targetVolume;
    
    [Header("Animatable Blink Properties")]
    [Tooltip("Прогресс моргания (0 = открыто, 1 = закрыто) - vignettingBlink")]
    [Range(0f, 1f)]
    public float blinkProgress = 0f;
    
    private BeautifyEffect beautify;
    
    [Header("Debug")]
    [Tooltip("Показывать логи в консоль")]
    [HideInInspector] public bool showDebugLogs = false;
    
    void Awake()
    {
        if (targetVolume == null)
        {
            targetVolume = GetComponent<Volume>();
        }
        
        if (targetVolume == null || targetVolume.profile == null)
        {
            DraumLogger.Error(this, "[BeautifyBlinkAnimator] Volume или Profile не найден!");
            return;
        }
        
        if (targetVolume.profile.TryGet<BeautifyEffect>(out beautify))
        {
            DraumLogger.Info(this, $"[BeautifyBlinkAnimator] Beautify найден! vignettingBlink.overrideState = {beautify.vignettingBlink.overrideState}");
            
        beautify.vignettingBlink.overrideState = true;
            
            DraumLogger.Info(this, $"[BeautifyBlinkAnimator] Override включен! Текущее значение vignettingBlink: {beautify.vignettingBlink.value}");
        }
        else
        {
            DraumLogger.Error(this, "[BeautifyBlinkAnimator] Beautify override НЕ найден в Volume Profile! Добавь 'Beautify' в Profile.");
        }
    }
    
    void Update()
    {
        UpdateBeautifyReference();
        
        if (beautify == null) return;
        
        SetParameterValue(beautify.vignettingBlink, blinkProgress);
        
    }
    
    /// <summary>
    /// Обновляет ссылку на Beautify компонент (для смены Volume Profile)
    /// </summary>
    void UpdateBeautifyReference()
    {
        if (targetVolume == null) return;
        
        if (targetVolume.profile != null)
        {
            BeautifyEffect newBeautify;
            if (targetVolume.profile.TryGet<BeautifyEffect>(out newBeautify))
            {
                if (beautify != newBeautify)
                {
                    beautify = newBeautify;
                    beautify.vignettingBlink.overrideState = true;
                    
                    if (showDebugLogs) DraumLogger.Info(this, "[BeautifyBlinkAnimator] Переключился на новый Beautify компонент!");
                }
            }
        }
    }
    
    void OnDisable()
    {
        blinkProgress = 0f;
        if (showDebugLogs) DraumLogger.Info(this, "[BeautifyBlinkAnimator] OnDisable - blinkProgress сброшен");
    }
    
    /// <summary>
    /// Устанавливает m_Value у VolumeParameter через Reflection
    /// </summary>
    void SetParameterValue<T>(VolumeParameter<T> parameter, T value)
    {
        if (parameter == null) return;
        
        if (!parameter.overrideState)
        {
            parameter.overrideState = true;
        }
        
        try
        {
            var field = typeof(VolumeParameter<T>).GetField("m_Value", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(parameter, value);
            }
            else
            {
                parameter.value = value;
            }
        }
        catch (System.Exception e)
        {
            parameter.value = value;
            
            if (showDebugLogs)
            {
                DraumLogger.Warning(this, $"[BeautifyBlink] Reflection FAILED: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Вызов из Animation Event
    /// </summary>
    public void OnBlinkClosed()
    {
        DraumLogger.Info(this, "[BeautifyBlinkAnimator] Blink CLOSED - можно переключать секцию!");
        
        var cameraController = FindFirstObjectByType<InventoryCameraController>();
        if (cameraController != null)
        {
        }
    }
    
    /// <summary>
    /// Принудительный сброс blinkProgress
    /// </summary>
    public void ResetBlinkProgress()
    {
        blinkProgress = 0f;
        if (showDebugLogs) DraumLogger.Info(this, "[BeautifyBlinkAnimator] blinkProgress принудительно сброшен в 0");
    }
    
    /// <summary>
    /// Принудительная установка blinkProgress
    /// </summary>
    public void SetBlinkProgress(float value)
    {
        blinkProgress = Mathf.Clamp01(value);
        if (showDebugLogs) DraumLogger.Info(this, $"[BeautifyBlinkAnimator] blinkProgress установлен в {blinkProgress}");
    }
}

