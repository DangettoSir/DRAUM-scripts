using UnityEngine;

[ExecuteInEditMode]
public class ZombieBlendShapeRandomizer : MonoBehaviour
{
    [Header("BlendShape Settings")]
    [Tooltip("SkinnedMeshRenderer с blendshapes")]
    public SkinnedMeshRenderer zombieMesh;
    
    [Header("Debug")]
    [HideInInspector] public bool showDebugLogs = false;
    
    private Mesh mesh;
    private int blendShapeCount;
    private float[] targetBlendValues;
    
    private void Awake()
    {
        InitializeBlendShapes();
    }
    
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        InitializeBlendShapes();
    }
    
    private void InitializeBlendShapes()
    {
        if (zombieMesh == null)
        {
            zombieMesh = GetComponent<SkinnedMeshRenderer>();
        }
        
        if (zombieMesh == null)
        {
            Debug.LogError("[ZombieBlendShapeRandomizer] SkinnedMeshRenderer не найден!");
            enabled = false;
            return;
        }
        
        mesh = zombieMesh.sharedMesh;
        if (mesh == null)
        {
            Debug.LogError("[ZombieBlendShapeRandomizer] Mesh не найден!");
            enabled = false;
            return;
        }
        
        blendShapeCount = mesh.blendShapeCount;
        targetBlendValues = new float[blendShapeCount];
        
        if (showDebugLogs)
        {
            Debug.Log($"[ZombieBlendShapeRandomizer] Найдено {blendShapeCount} blendshapes:");
            for (int i = 0; i < blendShapeCount; i++)
            {
                Debug.Log($"  [{i}] {mesh.GetBlendShapeName(i)}");
            }
        }
        
        GoRandom();
    }
    
    private void Update()
    {
        UpdateBlendShapes();
    }
    
    public void GoRandom()
    {
        for (int i = 0; i < blendShapeCount; i++)
        {
            targetBlendValues[i] = Random.Range(0f, 1f);
        }
        
        if (showDebugLogs)
        {
            Debug.Log("[ZombieBlendShapeRandomizer] Blendshapes рандомизированы");
        }
    }
    
    private void UpdateBlendShapes()
    {
        for (int i = 0; i < blendShapeCount; i++)
        {
            zombieMesh.SetBlendShapeWeight(i, targetBlendValues[i] * 100f);
        }
    }
    
    public void SetBlendShapeValue(int index, float value)
    {
        if (index >= 0 && index < blendShapeCount)
        {
            targetBlendValues[index] = Mathf.Clamp01(value);
        }
    }
    
    public void SetBlendShapeValue(string name, float value)
    {
        for (int i = 0; i < blendShapeCount; i++)
        {
            if (mesh.GetBlendShapeName(i) == name)
            {
                targetBlendValues[i] = Mathf.Clamp01(value);
                return;
            }
        }
    }
    
    public float GetBlendShapeValue(int index)
    {
        if (index >= 0 && index < blendShapeCount)
        {
            return targetBlendValues[index];
        }
        return 0f;
    }
    
    public float GetBlendShapeValue(string name)
    {
        for (int i = 0; i < blendShapeCount; i++)
        {
            if (mesh.GetBlendShapeName(i) == name)
            {
                return targetBlendValues[i];
            }
        }
        return 0f;
    }
}
