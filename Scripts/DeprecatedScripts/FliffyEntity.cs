using UnityEngine;

/// <summary>
/// Пример конкретного врага - Fliffy
/// </summary>
public class FliffyEntity : EnemyEntity
{
    protected override void Awake()
    {
     
        maxHealth = 50f;
        
        base.Awake();
        
        if (showDebugLogs)
        {
            Debug.Log($"[FliffyEntity] Fliffy инициализирован с {maxHealth} HP");
        }
    }
}

