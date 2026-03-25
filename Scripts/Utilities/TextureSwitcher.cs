using UnityEngine;
using System.Collections;

public class TextureSwitcher : MonoBehaviour
{
    [Header("Material Settings")]
    [Tooltip("Материал для смены текстуры")]
    public Material targetMaterial;
    
    private string texturePropertyName = "_BaseMap";
    
    [Header("Texture Settings")]
    [Tooltip("Оригинальная текстура (сохраняется)")]
    public Texture2D originalTexture;
    
    [Tooltip("Текстура на которую меняем")]
    public Texture2D switchTexture;
    
    [Header("Timing Settings")]
    [Tooltip("Интервал между сменами (секунды)")]
    [Range(1f, 60f)]
    public float switchInterval = 5f;
    
    [Tooltip("Длительность показа switch текстуры (секунды)")]
    [Range(0.1f, 10f)]
    public float switchDuration = 1f;
    
    [Header("Control")]
    [Tooltip("Автоматически запускать при старте")]
    public bool autoStart = true;
    
    [Tooltip("Показывать debug логи")]
    [HideInInspector] public bool showDebugLogs = false;
    
    private bool isSwitching = false;
    private bool isRunning = false;
    private Coroutine switchCoroutine;
    
    private void Start()
    {
        if (targetMaterial == null)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                targetMaterial = renderer.material;
                if (showDebugLogs) Debug.Log($"[TextureSwitcher] Материал найден автоматически: {targetMaterial.name}");
            }
        }
        
        if (targetMaterial != null)
        {
            if (targetMaterial.HasProperty("_BaseMap"))
            {
                texturePropertyName = "_BaseMap";
            }
            else if (targetMaterial.HasProperty("_MainTex"))
            {
                texturePropertyName = "_MainTex";
            }
            else if (targetMaterial.HasProperty("Texture2D_4450AB74"))
            {
                texturePropertyName = "Texture2D_4450AB74";
            }
            
            if (showDebugLogs) Debug.Log($"[TextureSwitcher] Используется property: {texturePropertyName}");
        }
        
        if (originalTexture == null && targetMaterial != null)
        {
            originalTexture = targetMaterial.GetTexture(texturePropertyName) as Texture2D;
            if (showDebugLogs) Debug.Log($"[TextureSwitcher] Оригинальная текстура сохранена: {(originalTexture != null ? originalTexture.name : "NULL")}");
        }
        
        if (autoStart)
        {
            StartSwitching();
        }
    }
    
    /// <summary>
    /// Запустить смену текстур
    /// </summary>
    public void StartSwitching()
    {
        if (isRunning)
        {
            if (showDebugLogs) Debug.LogWarning("[TextureSwitcher] Уже запущен!");
            return;
        }
        
        if (targetMaterial == null)
        {
            Debug.LogError("[TextureSwitcher] Target Material не назначен!");
            return;
        }
        
        if (switchTexture == null)
        {
            Debug.LogError("[TextureSwitcher] Switch Texture не назначена!");
            return;
        }
        
        isRunning = true;
        switchCoroutine = StartCoroutine(SwitchTextureLoop());
        
        if (showDebugLogs) Debug.Log("[TextureSwitcher] Смена текстур запущена");
    }
    
    /// <summary>
    /// Остановить смену текстур
    /// </summary>
    public void StopSwitching()
    {
        if (!isRunning)
        {
            if (showDebugLogs) Debug.LogWarning("[TextureSwitcher] Не запущен!");
            return;
        }
        
        isRunning = false;
        
        if (switchCoroutine != null)
        {
            StopCoroutine(switchCoroutine);
            switchCoroutine = null;
        }
        
        if (targetMaterial != null && originalTexture != null)
        {
            targetMaterial.SetTexture(texturePropertyName, originalTexture);
        }
        
        if (showDebugLogs) Debug.Log("[TextureSwitcher] Смена текстур остановлена");
    }
    
    /// <summary>
    /// Сменить текстуру вручную
    /// </summary>
    public void SwitchTextureNow()
    {
        if (targetMaterial == null || switchTexture == null) return;
        
        StartCoroutine(SingleTextureSwitch());
    }
    
    private IEnumerator SwitchTextureLoop()
    {
        while (isRunning)
        {
            yield return new WaitForSeconds(switchInterval);
            
            if (!isRunning) break;
            
            yield return StartCoroutine(SingleTextureSwitch());
        }
    }
    
    private IEnumerator SingleTextureSwitch()
    {
        if (isSwitching) yield break;
        
        isSwitching = true;
        
        if (showDebugLogs) Debug.Log($"[TextureSwitcher] Меняем текстуру на {switchTexture.name}");
        
        if (targetMaterial != null)
        {
            targetMaterial.SetTexture(texturePropertyName, switchTexture);
        }
        
        yield return new WaitForSeconds(switchDuration);
        
        if (targetMaterial != null && originalTexture != null)
        {
            targetMaterial.SetTexture(texturePropertyName, originalTexture);
            if (showDebugLogs) Debug.Log($"[TextureSwitcher] Возвращаем оригинальную текстуру {originalTexture.name}");
        }
        
        isSwitching = false;
    }
    
    /// <summary>
    /// Установить новую switch текстуру
    /// </summary>
    public void SetSwitchTexture(Texture2D newTexture)
    {
        switchTexture = newTexture;
        if (showDebugLogs) Debug.Log($"[TextureSwitcher] Switch текстура изменена на: {(newTexture != null ? newTexture.name : "NULL")}");
    }
    
    /// <summary>
    /// Установить интервал смены
    /// </summary>
    public void SetSwitchInterval(float newInterval)
    {
        switchInterval = Mathf.Max(0.1f, newInterval);
        if (showDebugLogs) Debug.Log($"[TextureSwitcher] Интервал изменён на: {switchInterval} сек");
    }
    
    /// <summary>
    /// Установить длительность показа
    /// </summary>
    public void SetSwitchDuration(float newDuration)
    {
        switchDuration = Mathf.Max(0.1f, newDuration);
        if (showDebugLogs) Debug.Log($"[TextureSwitcher] Длительность изменена на: {switchDuration} сек");
    }
    
    private void OnDestroy()
    {
        StopSwitching();
    }
    
    private void OnDisable()
    {
        StopSwitching();
    }
    
    [Header("Debug Info")]
    [SerializeField, Tooltip("Текущее состояние")]
    private string currentState = "Stopped";
    
    private void Update()
    {
        if (isRunning)
        {
            currentState = isSwitching ? "Switching" : "Waiting";
        }
        else
        {
            currentState = "Stopped";
        }
    }
}
