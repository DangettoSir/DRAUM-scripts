using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FIMSpace.FProceduralAnimation;

/// <summary>
/// Базовый класс для всех врагов с системой ХП и урона по частям тела
/// </summary>
public class EnemyEntity : MonoBehaviour
{
    [Header("Настройки ХП")]
    [Tooltip("Максимальное здоровье врага")]
    public float maxHealth = 100f;
    
    [Tooltip("Текущее здоровье врага")]
    [SerializeField]
    private float currentHealth;
    
    [Header("Настройки урона по частям")]
    [Tooltip("Использовать фиксированный урон вместо множителей")]
    public bool useFixedDamage = false;
    
    [Header("Множители урона (если useFixedDamage = false)")]
    [Tooltip("Множитель урона по голове")]
    public float headDamageMultiplier = 2.0f;
    [Tooltip("Множитель урона по шее")]
    public float neckDamageMultiplier = 1.5f;
    [Tooltip("Множитель урона по туловищу")]
    public float torsoDamageMultiplier = 1.0f;
    [Tooltip("Множитель урона по тазу")]
    public float pelvisDamageMultiplier = 0.8f;
    [Tooltip("Множитель урона по левой руке")]
    public float leftArmDamageMultiplier = 0.6f;
    [Tooltip("Множитель урона по правой руке")]
    public float rightArmDamageMultiplier = 0.6f;
    [Tooltip("Множитель урона по левой ноге")]
    public float leftLegDamageMultiplier = 0.7f;
    [Tooltip("Множитель урона по правой ноге")]
    public float rightLegDamageMultiplier = 0.7f;
    [Tooltip("Множитель урона по левой кисти")]
    public float leftHandDamageMultiplier = 0.4f;
    [Tooltip("Множитель урона по правой кисти")]
    public float rightHandDamageMultiplier = 0.4f;
    [Tooltip("Множитель урона по левой стопе")]
    public float leftFootDamageMultiplier = 0.3f;
    [Tooltip("Множитель урона по правой стопе")]
    public float rightFootDamageMultiplier = 0.3f;
    
    [Header("Фиксированный урон (если useFixedDamage = true)")]
    [Tooltip("Урон по голове")]
    public float headFixedDamage = 25f;
    [Tooltip("Урон по шее")]
    public float neckFixedDamage = 20f;
    [Tooltip("Урон по туловищу")]
    public float torsoFixedDamage = 15f;
    [Tooltip("Урон по тазу")]
    public float pelvisFixedDamage = 12f;
    [Tooltip("Урон по левой руке")]
    public float leftArmFixedDamage = 10f;
    [Tooltip("Урон по правой руке")]
    public float rightArmFixedDamage = 10f;
    [Tooltip("Урон по левой ноге")]
    public float leftLegFixedDamage = 8f;
    [Tooltip("Урон по правой ноге")]
    public float rightLegFixedDamage = 8f;
    [Tooltip("Урон по левой кисти")]
    public float leftHandFixedDamage = 5f;
    [Tooltip("Урон по правой кисти")]
    public float rightHandFixedDamage = 5f;
    [Tooltip("Урон по левой стопе")]
    public float leftFootFixedDamage = 4f;
    [Tooltip("Урон по правой стопе")]
    public float rightFootFixedDamage = 4f;
    
    // Внутренние словари для быстрого доступа
    private Dictionary<BodyPart, float> bodyPartDamageMultipliers;
    private Dictionary<BodyPart, float> bodyPartFixedDamage;
    
    [Header("Death Settings")]
    [Tooltip("Уничтожать GameObject после смерти")]
    public bool destroyOnDeath = false;
    
    [Tooltip("Задержка перед уничтожением (секунды)")]
    public float destroyDelay = 5f;
    
    [Tooltip("Сила импульса при смерти (для ragdoll)")]
    public float deathImpact = 4f;
    
    [Tooltip("Направление импульса (если zero, используется направление от камеры)")]
    public Vector3 customImpactDirection = Vector3.zero;
    
    [Header("Debug")]
    [Tooltip("Показывать debug логи")]
    [HideInInspector] public bool showDebugLogs = false;
    
    private bool deathProcessed = false;
    
    // События
    public System.Action<EnemyEntity, float> OnHealthChanged;
    public System.Action<EnemyEntity, BodyPart, float> OnDamageTaken;
    public System.Action<EnemyEntity> OnDeath;
    
    // Свойства
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public bool IsAlive => currentHealth > 0f;
    public bool IsDead => currentHealth <= 0f;
    
    protected virtual void Awake()
    {

        currentHealth = maxHealth;
        

        InitializeDamageDictionaries();
    }
    
    private void InitializeDamageDictionaries()
    {
        bodyPartDamageMultipliers = new Dictionary<BodyPart, float>
        {
            { BodyPart.Head, headDamageMultiplier },
            { BodyPart.Neck, neckDamageMultiplier },
            { BodyPart.Torso, torsoDamageMultiplier },
            { BodyPart.Pelvis, pelvisDamageMultiplier },
            { BodyPart.LeftArm, leftArmDamageMultiplier },
            { BodyPart.RightArm, rightArmDamageMultiplier },
            { BodyPart.LeftLeg, leftLegDamageMultiplier },
            { BodyPart.RightLeg, rightLegDamageMultiplier },
            { BodyPart.LeftHand, leftHandDamageMultiplier },
            { BodyPart.RightHand, rightHandDamageMultiplier },
            { BodyPart.LeftFoot, leftFootDamageMultiplier },
            { BodyPart.RightFoot, rightFootDamageMultiplier },
            { BodyPart.Unknown, 1.0f } // Неизвестная часть - базовый множитель
        };
        
        // Инициализируем фиксированный урон
        bodyPartFixedDamage = new Dictionary<BodyPart, float>
        {
            { BodyPart.Head, headFixedDamage },
            { BodyPart.Neck, neckFixedDamage },
            { BodyPart.Torso, torsoFixedDamage },
            { BodyPart.Pelvis, pelvisFixedDamage },
            { BodyPart.LeftArm, leftArmFixedDamage },
            { BodyPart.RightArm, rightArmFixedDamage },
            { BodyPart.LeftLeg, leftLegFixedDamage },
            { BodyPart.RightLeg, rightLegFixedDamage },
            { BodyPart.LeftHand, leftHandFixedDamage },
            { BodyPart.RightHand, rightHandFixedDamage },
            { BodyPart.LeftFoot, leftFootFixedDamage },
            { BodyPart.RightFoot, rightFootFixedDamage },
            { BodyPart.Unknown, 10f } 
        };
    }
    
    /// <summary>
    /// Наносит урон врагу в зависимости от части тела
    /// </summary>
    /// <param name="baseDamage">Базовый урон</param>
    /// <param name="bodyPart">Часть тела, в которую попали</param>
    /// <param name="hitPoint">Точка попадания</param>
    /// <returns>Реальный нанесенный урон</returns>
    public float TakeDamage(float baseDamage, BodyPart bodyPart, Vector3 hitPoint)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[EnemyEntity] TakeDamage CALLED on {gameObject.name}: " +
                     $"baseDamage={baseDamage}, bodyPart={bodyPart}, hitPoint={hitPoint}, " +
                     $"currentHealth={currentHealth}, isDead={IsDead}");
        }
        
        if (IsDead)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[EnemyEntity] {gameObject.name} уже мертв, урон игнорируется");
            }
            return 0f;
        }
        

        float finalDamage;
        
        if (useFixedDamage)
        {
  
            if (bodyPartFixedDamage.TryGetValue(bodyPart, out float fixedDmg))
            {
                finalDamage = fixedDmg;
            }
            else
            {
                finalDamage = bodyPartFixedDamage[BodyPart.Unknown];
            }
        }
        else
        {

            if (bodyPartDamageMultipliers.TryGetValue(bodyPart, out float multiplier))
            {
                finalDamage = baseDamage * multiplier;
            }
            else
            {
                finalDamage = baseDamage * bodyPartDamageMultipliers[BodyPart.Unknown];
            }
        }
        

        float oldHealth = currentHealth;
        currentHealth = Mathf.Max(0f, currentHealth - finalDamage);
        

        if (showDebugLogs)
        {
            Debug.Log($"[EnemyEntity] {gameObject.name} получил {finalDamage:F1} урона по {bodyPart} " +
                     $"(базовый: {baseDamage:F1}, ХП: {oldHealth:F1} -> {currentHealth:F1})");
        }
        

        OnDamageTaken?.Invoke(this, bodyPart, finalDamage);
        OnHealthChanged?.Invoke(this, currentHealth);

        if (currentHealth <= 0f && oldHealth > 0f)
        {
            Die();
        }
        
        return finalDamage;
    }
    
    /// <summary>
    /// Наносит урон без учета части тела
    /// </summary>
    public float TakeDamage(float damage)
    {
        return TakeDamage(damage, BodyPart.Unknown, Vector3.zero);
    }
    
    /// <summary>
    /// Обрабатывает смерть врага
    /// </summary>
    private void Die()
    {
        if (IsDead && deathProcessed) return;
        
        if (showDebugLogs)
        {
            Debug.Log($"[EnemyEntity] {gameObject.name} УБИТ! (ХП: {currentHealth:F1}/{maxHealth:F1})");
        }

        OnDeath?.Invoke(this);
        

        StartCoroutine(ProcessDeath());
    }
    
    /// <summary>
    /// Обрабатывает процесс смерти (ragdoll и уничтожение)
    /// </summary>
    private IEnumerator ProcessDeath()
    {
        if (deathProcessed) yield break;
        deathProcessed = true;
        

        var ragdollAnimator = FindRagdollAnimator2();
        if (ragdollAnimator != null)
        {
            yield return ApplyDeathRagdoll(ragdollAnimator);
        }
        

        if (destroyOnDeath)
        {
            yield return new WaitForSeconds(destroyDelay);
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Ищет RagdollAnimator2 на объекте или в иерархии
    /// </summary>
    private Component FindRagdollAnimator2()
    {

        RagdollAnimator2 rag = GetComponent<RagdollAnimator2>();
        if (rag != null) return rag;
        
        rag = GetComponentInChildren<RagdollAnimator2>();
        if (rag != null) return rag;
        
        rag = GetComponentInParent<RagdollAnimator2>();
        if (rag != null) return rag;

        var components = GetComponents<Component>();
        foreach (var comp in components)
        {
            if (comp.GetType().Name == "RagdollAnimator2")
            {
                return comp;
            }
        }
        
        components = GetComponentsInChildren<Component>();
        foreach (var comp in components)
        {
            if (comp.GetType().Name == "RagdollAnimator2")
            {
                return comp;
            }
        }
        
        components = GetComponentsInParent<Component>();
        foreach (var comp in components)
        {
            if (comp.GetType().Name == "RagdollAnimator2")
            {
                return comp;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Применяет ragdoll с задержкой и импульсом
    /// </summary>
    private IEnumerator ApplyDeathRagdoll(Component ragdollAnimator)
    {
        if (ragdollAnimator == null) yield break;
        
        if (ragdollAnimator is RagdollAnimator2 rag)
        {
            yield return ApplyDeathRagdollDirect(rag);
            yield break;
        }
        

        yield return ApplyDeathRagdollReflection(ragdollAnimator);
    }
    
    /// <summary>
    /// Применяет ragdoll напрямую (если тип доступен)
    /// </summary>
    private IEnumerator ApplyDeathRagdollDirect(RagdollAnimator2 rag)
    {

        try
        {
            if (rag.Handler != null && rag.Handler.Mecanim != null)
            {
                rag.Handler.Mecanim.enabled = false;
                
                if (showDebugLogs)
                {
                    Debug.Log($"[EnemyEntity] Disabled Animator on {gameObject.name}");
                }
            }
            

            var legsAnimator = GetComponent<LegsAnimator>();
            if (legsAnimator == null)
            {
                legsAnimator = GetComponentInChildren<LegsAnimator>();
            }
            if (legsAnimator == null)
            {
                legsAnimator = GetComponentInParent<LegsAnimator>();
            }
            
            if (legsAnimator != null)
            {
                legsAnimator.enabled = false;
                
                if (showDebugLogs)
                {
                    Debug.Log($"[EnemyEntity] Disabled LegsAnimator on {gameObject.name}");
                }
            }
            
   
            rag.enabled = true;
            
  
            if (rag.Handler != null)
            {
                rag.Handler.AnimatingMode = RagdollHandler.EAnimatingMode.Falling;
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"[EnemyEntity] Enabled Ragdoll on {gameObject.name}, set mode to Falling");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnemyEntity] Error setting up ragdoll: {e.Message}\n{e.StackTrace}");
            yield break;
        }
        

        yield return null;
        yield return new WaitForFixedUpdate();

        try
        {
            ApplyImpulseToAllBonesDirect(rag);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnemyEntity] Error applying impulse: {e.Message}\n{e.StackTrace}");
        }
    }
    
    /// <summary>
    /// Применяет импульс ко всем костям ragdoll (прямой доступ)
    /// </summary>
    private void ApplyImpulseToAllBonesDirect(RagdollAnimator2 rag)
    {
        if (rag.Handler == null || rag.Handler.Chains == null) return;
        

        Vector3 impactDirection;
        if (customImpactDirection != Vector3.zero)
        {
            impactDirection = customImpactDirection.normalized * deathImpact;
        }
        else
        {

            if (Camera.main != null)
            {
                impactDirection = Camera.main.transform.forward * deathImpact;
            }
            else
            {
                impactDirection = Vector3.forward * deathImpact;
            }
        }
        

        foreach (var chain in rag.Handler.Chains)
        {
            foreach (var bone in chain.BoneSetups)
            {
                Rigidbody rig = bone.SourceBone.GetComponent<Rigidbody>();
                if (rig == null) continue;
                
                RagdollHandlerUtilities.ApplyLimbImpact(rig, impactDirection, ForceMode.Force);
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[EnemyEntity] Applied impulse {deathImpact} to all bones on {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Применяет ragdoll через рефлексию (если тип недоступен напрямую)
    /// </summary>
    private IEnumerator ApplyDeathRagdollReflection(Component ragdollAnimator)
    {
        var ragType = ragdollAnimator.GetType();
        
        try
        {
            var handlerProp = ragType.GetProperty("Handler");
            if (handlerProp != null)
            {
                var handler = handlerProp.GetValue(ragdollAnimator);
                if (handler != null)
                {
                    var mecanimProp = handler.GetType().GetProperty("Mecanim");
                    if (mecanimProp != null)
                    {
                        var mecanim = mecanimProp.GetValue(handler) as Animator;
                        if (mecanim != null)
                        {
                            mecanim.enabled = false;
                            
                            if (showDebugLogs)
                            {
                                Debug.Log($"[EnemyEntity] Disabled Animator on {gameObject.name}");
                            }
                        }
                    }
                }
            }
            
            var legsAnimator = GetComponent<LegsAnimator>();
            if (legsAnimator == null)
            {
                legsAnimator = GetComponentInChildren<LegsAnimator>();
            }
            if (legsAnimator == null)
            {
                legsAnimator = GetComponentInParent<LegsAnimator>();
            }
            
            if (legsAnimator != null)
            {
                legsAnimator.enabled = false;
                
                if (showDebugLogs)
                {
                    Debug.Log($"[EnemyEntity] Disabled LegsAnimator on {gameObject.name}");
                }
            }
            
            var enabledProp = ragType.GetProperty("enabled");
            if (enabledProp != null)
            {
                enabledProp.SetValue(ragdollAnimator, true);
                
                if (handlerProp != null)
                {
                    var handler = handlerProp.GetValue(ragdollAnimator);
                    if (handler != null)
                    {
                        var animatingModeProp = handler.GetType().GetProperty("AnimatingMode");
                        if (animatingModeProp != null)
                        {
                            // Получаем enum Falling
                            var fallingEnum = System.Enum.Parse(animatingModeProp.PropertyType, "Falling");
                            animatingModeProp.SetValue(handler, fallingEnum);
                            
                            if (showDebugLogs)
                            {
                                Debug.Log($"[EnemyEntity] Set Ragdoll Animation Mode to Falling on {gameObject.name}");
                            }
                        }
                    }
                }
                
                if (showDebugLogs)
                {
                    Debug.Log($"[EnemyEntity] Enabled Ragdoll on {gameObject.name}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnemyEntity] Error setting up ragdoll (reflection): {e.Message}\n{e.StackTrace}");
            yield break;
        }
        
        yield return null;
        yield return new WaitForFixedUpdate();
        
        try
        {
            ApplyImpulseToAllBonesReflection(ragdollAnimator);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnemyEntity] Error applying impulse (reflection): {e.Message}\n{e.StackTrace}");
        }
    }
    
    /// <summary>
    /// Применяет импульс ко всем костям ragdoll (через рефлексию)
    /// </summary>
    private void ApplyImpulseToAllBonesReflection(Component ragdollAnimator)
    {
        try
        {
            var ragType = ragdollAnimator.GetType();
            var handlerProp = ragType.GetProperty("Handler");
            
            if (handlerProp == null) return;
            
            var handler = handlerProp.GetValue(ragdollAnimator);
            if (handler == null) return;
            
            var chainsProp = handler.GetType().GetProperty("Chains");
            if (chainsProp == null) return;
            
            var chains = chainsProp.GetValue(handler);
            if (chains == null) return;

            Vector3 impactDirection;
            if (customImpactDirection != Vector3.zero)
            {
                impactDirection = customImpactDirection.normalized * deathImpact;
            }
            else
            {

                if (Camera.main != null)
                {
                    impactDirection = Camera.main.transform.forward * deathImpact;
                }
                else
                {
                    impactDirection = Vector3.forward * deathImpact;
                }
            }
            
            var utilitiesType = System.Type.GetType("FIMSpace.FProceduralAnimation.RagdollHandlerUtilities");
            System.Reflection.MethodInfo applyMethod = null;
            if (utilitiesType != null)
            {
                applyMethod = utilitiesType.GetMethod("ApplyLimbImpact", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                    null,
                    new System.Type[] { typeof(Rigidbody), typeof(Vector3), typeof(ForceMode) },
                    null);
            }
            
            var enumerable = chains as System.Collections.IEnumerable;
            if (enumerable != null)
            {
                foreach (var chain in enumerable)
                {
                    if (chain == null) continue;
                    
                    var boneSetupsProp = chain.GetType().GetProperty("BoneSetups");
                    if (boneSetupsProp == null) continue;
                    
                    var boneSetups = boneSetupsProp.GetValue(chain);
                    if (boneSetups == null) continue;
                    
                    var boneSetupsEnumerable = boneSetups as System.Collections.IEnumerable;
                    if (boneSetupsEnumerable != null)
                    {
                        foreach (var bone in boneSetupsEnumerable)
                        {
                            if (bone == null) continue;
                            
                            var sourceBoneProp = bone.GetType().GetProperty("SourceBone");
                            if (sourceBoneProp == null) continue;
                            
                            var sourceBone = sourceBoneProp.GetValue(bone) as Transform;
                            if (sourceBone == null) continue;
                            
                            Rigidbody rig = sourceBone.GetComponent<Rigidbody>();
                            if (rig == null) continue;
                            
                            if (applyMethod != null)
                            {
                                applyMethod.Invoke(null, new object[] { rig, impactDirection, ForceMode.Force });
                            }
                            else
                            {
                                rig.AddForce(impactDirection, ForceMode.Force);
                            }
                        }
                    }
                }
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"[EnemyEntity] Applied impulse {deathImpact} to all bones on {gameObject.name}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnemyEntity] Error applying impulse (reflection): {e.Message}\n{e.StackTrace}");
        }
    }
    
    /// <summary>
    /// Восстанавливает здоровье
    /// </summary>
    public void Heal(float amount)
    {
        if (IsDead) return;
        
        float oldHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        
        if (showDebugLogs && currentHealth != oldHealth)
        {
            Debug.Log($"[EnemyEntity] {gameObject.name} восстановил {amount:F1} ХП ({oldHealth:F1} -> {currentHealth:F1})");
        }
        
        OnHealthChanged?.Invoke(this, currentHealth);
    }
    
    /// <summary>
    /// Устанавливает здоровье
    /// </summary>
    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0f, maxHealth);
        OnHealthChanged?.Invoke(this, currentHealth);
    }
    
    /// <summary>
    /// Полностью восстанавливает здоровье
    /// </summary>
    public void FullHeal()
    {
        SetHealth(maxHealth);
    }
    
    /// <summary>
    /// Сбрасывает врага (полное здоровье, жив)
    /// </summary>
    public void Reset()
    {
        currentHealth = maxHealth;
        deathProcessed = false;
        OnHealthChanged?.Invoke(this, currentHealth);
    }
}

