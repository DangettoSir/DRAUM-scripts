using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ZombieHitReaction : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Тег Enemy для проверки")]
    public string enemyTag = "Enemy";
    
    [Header("Hit Force Settings")]
    [Tooltip("Радиус рассеивания силы удара")]
    public float forceSpreadRadius = 2f;
    
    [Tooltip("Множитель силы в зависимости от расстояния (0=макс, 1=мин)")]
    public AnimationCurve distanceFalloff;
    
    [Tooltip("Минимальная сила для применения (процент от максимальной)")]
    public float minForceThreshold = 0.1f;
    
    [Tooltip("Время физической реакции на удар (секунды)")]
    public float physicsReactionDuration = 0.5f;
    
    [Header("References")]
    [Tooltip("Animator для временного отключения слоев")]
    public Animator zombieAnimator;
    
    [Tooltip("EnemyEntity для временного отключения")]
    public EnemyEntity enemyEntity;
    
    [Header("Animator Control")]
    [Tooltip("Отключать все слои аниматора при ударе")]
    public bool disableAllAnimatorLayers = false;
    
    [Tooltip("Индексы слоев аниматора для отключения (0 = Base Layer)")]
    public int[] animatorLayersToDisable = new int[] { 0 };
    
    [Header("Debug")]
    [HideInInspector] public bool showDebugLogs = false;
    public bool showHitGizmos = true;
    
    private Dictionary<Collider, Rigidbody> colliderRigidbodyMap = new Dictionary<Collider, Rigidbody>();
    private Dictionary<Collider, bool> colliderIsTriggerMap = new Dictionary<Collider, bool>();
    private Dictionary<Collider, bool> colliderIsEnemyTagMap = new Dictionary<Collider, bool>();
    
    private List<HitInfo> recentHits = new List<HitInfo>();
    private Dictionary<Collider, Vector3> weaponPreviousPositions = new Dictionary<Collider, Vector3>();
    private Dictionary<Collider, float> weaponPreviousTime = new Dictionary<Collider, float>();
    
    private class HitInfo
    {
        public Vector3 hitPoint;
        public float hitSpeed;
        public float timestamp;
        public float displayDuration = 2f;
    }
    
    private void Awake()
    {
        if (distanceFalloff == null || distanceFalloff.keys.Length == 0)
        {
            distanceFalloff = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
        }
        
        if (zombieAnimator == null)
            zombieAnimator = GetComponent<Animator>();
        
        if (enemyEntity == null)
            enemyEntity = GetComponent<EnemyEntity>();
        
        CollectAllCollidersAndRigidbodies();
        AnalyzeColliders();
    }
    
    private void CollectAllCollidersAndRigidbodies()
    {
        Collider[] allColliders = GetComponentsInChildren<Collider>();
        
        foreach (Collider col in allColliders)
        {
            Rigidbody rb = col.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = col.GetComponentInParent<Rigidbody>();
            }
            
            colliderRigidbodyMap[col] = rb;
            
            if (showDebugLogs)
            {
                Debug.Log($"[ZombieHitReaction] Коллизия: {col.name}, Rigidbody: {(rb != null ? rb.name : "null")}");
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[ZombieHitReaction] Собрано коллизий: {colliderRigidbodyMap.Count}");
        }
    }
    
    private void AnalyzeColliders()
    {
        foreach (var kvp in colliderRigidbodyMap)
        {
            Collider col = kvp.Key;
            
            colliderIsTriggerMap[col] = col.isTrigger;
            
            bool isEnemyTag = col.CompareTag(enemyTag);
            colliderIsEnemyTagMap[col] = isEnemyTag;
            
            if (showDebugLogs)
            {
                Debug.Log($"[ZombieHitReaction] {col.name}: isTrigger={col.isTrigger}, isEnemyTag={isEnemyTag}, Tag={col.tag}");
            }
        }
    }
    
    private void Update()
    {
        CleanupOldHits();
    }
    
    private void CleanupOldHits()
    {
        float currentTime = Time.time;
        recentHits.RemoveAll(hit => currentTime - hit.timestamp > hit.displayDuration);
    }
    
    public void RegisterHit(Vector3 hitPoint, float hitSpeed, float weaponMass, float baseForceMultiplier, float speedForceMultiplier)
    {
        HitInfo hitInfo = new HitInfo
        {
            hitPoint = hitPoint,
            hitSpeed = hitSpeed,
            timestamp = Time.time
        };
        
        recentHits.Add(hitInfo);
        
        if (showDebugLogs)
        {
            Debug.Log($"[ZombieHitReaction] RegisterHit: Точка: {hitPoint}, Скорость: {hitSpeed:F2}, Масса: {weaponMass}, Множители: base={baseForceMultiplier}, speed={speedForceMultiplier}");
        }
        
        ApplyHitForce(hitPoint, hitSpeed, weaponMass, baseForceMultiplier, speedForceMultiplier);
    }
    
    private void ApplyHitForce(Vector3 hitPoint, float hitSpeed, float weaponMass, float baseForceMultiplier, float speedForceMultiplier)
    {
        Collider weaponCollider = null;
        
        Collider[] nearbyColliders = Physics.OverlapSphere(hitPoint, forceSpreadRadius);
        
        foreach (var col in nearbyColliders)
        {
            if (col.CompareTag("Weapon"))
            {
                weaponCollider = col;
                break;
            }
        }
        
        float baseForce = (hitSpeed * speedForceMultiplier + weaponMass) * baseForceMultiplier;
        
        Vector3 hitDirection = Vector3.zero;
        if (weaponCollider != null)
        {
            hitDirection = (hitPoint - weaponCollider.transform.position).normalized;
        }
        else
        {
            hitDirection = (hitPoint - transform.position).normalized;
        }
        
        hitDirection.y = 0;
        if (hitDirection == Vector3.zero)
        {
            hitDirection = -transform.forward;
        }
        
        HashSet<Rigidbody> processedBodies = new HashSet<Rigidbody>();
        
        foreach (var kvp in colliderRigidbodyMap)
        {
            Collider col = kvp.Key;
            Rigidbody rb = kvp.Value;
            
            if (rb == null || processedBodies.Contains(rb))
            {
                continue;
            }
            
            float distance = Vector3.Distance(hitPoint, col.bounds.center);
            
            if (distance > forceSpreadRadius)
            {
                continue;
            }
            
            float normalizedDistance = distance / forceSpreadRadius;
            float forceMultiplier = distanceFalloff.Evaluate(normalizedDistance);
            
            if (forceMultiplier < minForceThreshold)
            {
                continue;
            }
            
            float force = baseForce * forceMultiplier;
            
            Vector3 forceDirection = (col.bounds.center - hitPoint).normalized;
            if (forceDirection == Vector3.zero)
            {
                forceDirection = hitDirection;
            }
            else
            {
                forceDirection = Vector3.Lerp(forceDirection, hitDirection, 0.5f).normalized;
            }
            
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.AddForce(forceDirection * force, ForceMode.Impulse);
            
            processedBodies.Add(rb);
            
            if (showDebugLogs)
            {
                Debug.Log($"[ZombieHitReaction] Применена сила {force:F2} к {col.name}, Расстояние: {distance:F2}, Множитель: {forceMultiplier:F2}");
            }
        }
        
        StartCoroutine(ResetPhysicsAfterDelay(processedBodies));
    }
    
    private IEnumerator ResetPhysicsAfterDelay(HashSet<Rigidbody> bodies)
    {
        bool animatorWasEnabled = zombieAnimator != null && zombieAnimator.enabled;
        bool enemyEntityWasEnabled = enemyEntity != null && enemyEntity.enabled;
        
        Dictionary<int, float> animatorLayerWeights = new Dictionary<int, float>();
        
        if (zombieAnimator != null)
        {
            if (disableAllAnimatorLayers)
            {
                zombieAnimator.enabled = false;
            }
            else
            {
                for (int i = 0; i < zombieAnimator.layerCount; i++)
                {
                    if (System.Array.IndexOf(animatorLayersToDisable, i) >= 0)
                    {
                        animatorLayerWeights[i] = zombieAnimator.GetLayerWeight(i);
                        zombieAnimator.SetLayerWeight(i, 0f);
                    }
                }
            }
        }
        
        if (enemyEntity != null)
        {
            enemyEntity.enabled = false;
        }
        
        yield return new WaitForSeconds(physicsReactionDuration);
        
        foreach (var rb in bodies)
        {
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
        
        if (zombieAnimator != null)
        {
            if (disableAllAnimatorLayers && animatorWasEnabled)
            {
                zombieAnimator.enabled = true;
            }
            else
            {
                foreach (var kvp in animatorLayerWeights)
                {
                    zombieAnimator.SetLayerWeight(kvp.Key, kvp.Value);
                }
            }
        }
        
        if (enemyEntity != null && enemyEntityWasEnabled)
        {
            enemyEntity.enabled = true;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[ZombieHitReaction] Физическая реакция завершена, компоненты восстановлены");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        ProcessHit(other, "OnTriggerEnter");
    }
    
    private void OnTriggerStay(Collider other)
    {
        ProcessHit(other, "OnTriggerStay");
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[ZombieHitReaction] OnTriggerExit: {other.name}");
        }
    }
    
    private void ProcessHit(Collider weaponCollider, string methodName)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[ZombieHitReaction] {methodName} вызван! Оружие: {weaponCollider.name}, Tag: {weaponCollider.tag}");
        }
        
        Collider hitCollider = null;
        
        foreach (var kvp in colliderRigidbodyMap)
        {
            Collider zombieCollider = kvp.Key;
            
            if (zombieCollider == weaponCollider)
            {
                continue;
            }
            
            if (!zombieCollider.bounds.Intersects(weaponCollider.bounds))
            {
                continue;
            }
            
            if (!colliderIsTriggerMap.ContainsKey(zombieCollider) || !colliderIsTriggerMap[zombieCollider])
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[ZombieHitReaction] Коллизия {zombieCollider.name} не является триггером, пропускаем");
                }
                continue;
            }
            
            if (!colliderIsEnemyTagMap.ContainsKey(zombieCollider) || !colliderIsEnemyTagMap[zombieCollider])
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[ZombieHitReaction] Коллизия {zombieCollider.name} не имеет тег {enemyTag}! Текущий тег: {zombieCollider.tag}");
                }
                continue;
            }
            
            hitCollider = zombieCollider;
            break;
        }
        
        if (hitCollider == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[ZombieHitReaction] Не найдена подходящая коллизия зомби для удара!");
            }
            return;
        }
        
        Vector3 hitPoint = weaponCollider.ClosestPoint(hitCollider.bounds.center);
        
        float hitSpeed = 0f;
        Rigidbody weaponRb = weaponCollider.GetComponent<Rigidbody>();
        if (weaponRb != null)
        {
            hitSpeed = weaponRb.linearVelocity.magnitude;
        }
        else
        {
            hitSpeed = CalculateWeaponSpeed(weaponCollider);
        }
        
        HitInfo hitInfo = new HitInfo
        {
            hitPoint = hitPoint,
            hitSpeed = hitSpeed,
            timestamp = Time.time
        };
        
        recentHits.Add(hitInfo);
        
        if (showDebugLogs)
        {
            Debug.Log($"[ZombieHitReaction] {methodName}: Удар в {hitCollider.name}, Точка: {hitPoint}, Скорость: {hitSpeed:F2}");
        }
    }
    
    private float CalculateWeaponSpeed(Collider weaponCollider)
    {
        if (!weaponPreviousPositions.ContainsKey(weaponCollider))
        {
            weaponPreviousPositions[weaponCollider] = weaponCollider.transform.position;
            weaponPreviousTime[weaponCollider] = Time.time;
            return 0f;
        }
        
        Vector3 currentPos = weaponCollider.transform.position;
        Vector3 previousPos = weaponPreviousPositions[weaponCollider];
        float deltaTime = Time.time - weaponPreviousTime[weaponCollider];
        
        if (deltaTime > 0.01f)
        {
            float speed = Vector3.Distance(currentPos, previousPos) / deltaTime;
            weaponPreviousPositions[weaponCollider] = currentPos;
            weaponPreviousTime[weaponCollider] = Time.time;
            return speed;
        }
        
        return 0f;
    }
    
    private void OnDrawGizmos()
    {
        if (!showHitGizmos) return;
        
        foreach (var hit in recentHits)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(hit.hitPoint, 0.1f);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.Label(hit.hitPoint + Vector3.up * 0.3f, $"Speed: {hit.hitSpeed:F2}");
            #endif
        }
    }
}
