using UnityEngine;

[ExecuteInEditMode]
public class ZombieEmissiveRandomizer : MonoBehaviour
{
    [Header("Emissive Settings")]
    [Tooltip("Renderer с материалом")]
    public Renderer zombieRenderer;
    
    [Tooltip("Индекс материала (если несколько)")]
    public int materialIndex = 0;
    
    [Tooltip("Название параметра emissive цвета")]
    public string emissiveColorProperty = "_EmissionColor";
    
    [Tooltip("Минимальная яркость emissive")]
    [Range(0f, 5f)]
    public float minEmissiveIntensity = 0f;
    
    [Tooltip("Максимальная яркость emissive")]
    [Range(0f, 5f)]
    public float maxEmissiveIntensity = 2f;
    
    [Header("Color Variation")]
    [Tooltip("Минимальный оттенок (HSV)")]
    [Range(0f, 1f)]
    public float minHue = 0f; // Оранжевый
    
    [Tooltip("Максимальный оттенок (HSV)")]
    [Range(0f, 1f)]
    public float maxHue = 0.3f; // Бледно-зеленый
    
    [Tooltip("Насыщенность")]
    [Range(0f, 1f)]
    public float saturation = 0.8f;
    
    [Header("Debug")]
    [HideInInspector] public bool showDebugLogs = false;
    
    private Material originalMaterial;
    private Material instanceMaterial;
    private Color originalEmissionColor;
    
    private void Awake()
    {
        if (zombieRenderer == null)
            zombieRenderer = GetComponent<Renderer>();
        
        if (zombieRenderer == null)
            zombieRenderer = GetComponentInChildren<Renderer>();
        
        InitializeEmissive();
    }
    
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        if (zombieRenderer == null)
            zombieRenderer = GetComponent<Renderer>();
        
        if (zombieRenderer == null)
            zombieRenderer = GetComponentInChildren<Renderer>();
    }
    
    private void OnDestroy()
    {
        if (originalMaterial != null && instanceMaterial != null)
        {
            DestroyImmediate(instanceMaterial);
        }
    }
    
    private void InitializeEmissive()
    {
        if (zombieRenderer == null)
        {
            Debug.LogError("[ZombieEmissiveRandomizer] Renderer не найден! Убедись что у объекта есть Renderer компонент.");
            return;
        }
        
        if (zombieRenderer.sharedMaterials == null || zombieRenderer.sharedMaterials.Length == 0)
        {
            Debug.LogError("[ZombieEmissiveRandomizer] Материалы не найдены на Renderer!");
            return;
        }
        
        if (materialIndex >= zombieRenderer.sharedMaterials.Length)
        {
            materialIndex = 0;
        }
        
        originalMaterial = zombieRenderer.sharedMaterials[materialIndex];
        
        if (originalMaterial != null && originalMaterial.HasProperty(emissiveColorProperty))
        {
            originalEmissionColor = originalMaterial.GetColor(emissiveColorProperty);
            
            instanceMaterial = new Material(originalMaterial);
            Material[] materials = zombieRenderer.sharedMaterials;
            materials[materialIndex] = instanceMaterial;
            zombieRenderer.materials = materials;
            
            RandomizeEmissive();
            
            if (showDebugLogs)
            {
                Debug.Log($"[ZombieEmissiveRandomizer] Emissive инициализирован для материала {materialIndex}");
            }
        }
        else
        {
            Debug.LogWarning($"[ZombieEmissiveRandomizer] Материал не содержит параметр {emissiveColorProperty} или материал null");
        }
    }
    
    public void RandomizeEmissive()
    {
        if (instanceMaterial == null) return;
        
        float intensity = Random.Range(minEmissiveIntensity, maxEmissiveIntensity);
        float hue = Random.Range(minHue, maxHue);
        float value = intensity;
        
        Color emissiveColor = Color.HSVToRGB(hue, saturation, value);
        
        instanceMaterial.SetColor(emissiveColorProperty, emissiveColor);
        
        if (showDebugLogs)
        {
            Debug.Log($"[ZombieEmissiveRandomizer] Emissive рандомизирован: {emissiveColor}, Hue: {hue:F2}, Intensity: {intensity:F2}");
        }
    }
    
    public void ResetEmissive()
    {
        if (instanceMaterial == null) return;
        
        instanceMaterial.SetColor(emissiveColorProperty, originalEmissionColor);
        
        if (showDebugLogs)
        {
            Debug.Log("[ZombieEmissiveRandomizer] Emissive сброшен к оригинальному цвету");
        }
    }
    
    public Color GetCurrentEmissiveColor()
    {
        if (instanceMaterial == null) return Color.black;
        return instanceMaterial.GetColor(emissiveColorProperty);
    }
}
