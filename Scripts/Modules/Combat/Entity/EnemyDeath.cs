using UnityEngine;
using System.Collections;

using FIMSpace.FProceduralAnimation;

/// <summary>
/// Скрипт для переключения на ragdoll при смерти врага.
/// Работает аналогично Demo_Ragd_OptimalDeathClicker - отключает анимацию и включает ragdoll с импульсом.
/// </summary>
[RequireComponent(typeof(EnemyEntity))]
public class EnemyDeath : MonoBehaviour
{
    [Header("Ragdoll Settings")]
    [Tooltip("Сила импульса при смерти")]
    public float deathImpact = 4f;
    
    [Tooltip("Направление импульса (если null, используется направление от камеры)")]
    public Vector3? customImpactDirection = null;
    
    [Header("Debug")]
    [Tooltip("Показывать debug логи")]
    [HideInInspector] public bool showDebugLogs = false;
    
    private EnemyEntity enemyEntity;
    private bool deathProcessed = false;
    
    private void Awake()
    {
        enemyEntity = GetComponent<EnemyEntity>();
        if (enemyEntity == null)
        {
            Debug.LogError($"[EnemyDeath] EnemyEntity not found on {gameObject.name}!");
            enabled = false;
            return;
        }
        
        enemyEntity.OnDeath += HandleDeath;
    }
    
    private void OnDestroy()
    {
        if (enemyEntity != null)
        {
            enemyEntity.OnDeath -= HandleDeath;
        }
    }
    
    /// <summary>
    /// Обрабатывает смерть врага - переключает на ragdoll
    /// </summary>
    private void HandleDeath(EnemyEntity entity)
    {
        if (deathProcessed)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[EnemyDeath] Death already processed for {gameObject.name}");
            }
            return;
        }
        
        deathProcessed = true;
        
        if (showDebugLogs)
        {
            Debug.Log($"[EnemyDeath] Processing death for {gameObject.name}");
        }
        
        var ragdollAnimator = FindRagdollAnimator2();
        if (ragdollAnimator != null)
        {
            StartCoroutine(ApplyDeathRagdoll(ragdollAnimator));
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[EnemyDeath] RagdollAnimator2 not found on {gameObject.name} or children!");
            }
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
                    Debug.Log($"[EnemyDeath] Disabled Animator on {gameObject.name}");
                }
            }
            
            var legsAnimator = GetComponent<FIMSpace.FProceduralAnimation.LegsAnimator>();
            if (legsAnimator == null)
            {
                legsAnimator = GetComponentInChildren<FIMSpace.FProceduralAnimation.LegsAnimator>();
            }
            if (legsAnimator == null)
            {
                legsAnimator = GetComponentInParent<FIMSpace.FProceduralAnimation.LegsAnimator>();
            }
            
            if (legsAnimator != null)
            {
                legsAnimator.enabled = false;
                
                if (showDebugLogs)
                {
                    Debug.Log($"[EnemyDeath] Disabled LegsAnimator on {gameObject.name}");
                }
            }
            
            rag.enabled = true;
            
            if (rag.Handler != null)
            {
                rag.Handler.AnimatingMode = RagdollHandler.EAnimatingMode.Falling;
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"[EnemyDeath] Enabled Ragdoll on {gameObject.name}, set mode to Falling");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnemyDeath] Error setting up ragdoll: {e.Message}\n{e.StackTrace}");
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
            Debug.LogError($"[EnemyDeath] Error applying impulse: {e.Message}\n{e.StackTrace}");
        }
    }
    
    /// <summary>
    /// Применяет импульс ко всем костям ragdoll (прямой доступ)
    /// </summary>
    private void ApplyImpulseToAllBonesDirect(RagdollAnimator2 rag)
    {
        if (rag.Handler == null || rag.Handler.Chains == null) return;
        
        Vector3 impactDirection;
        if (customImpactDirection.HasValue)
        {
            impactDirection = customImpactDirection.Value.normalized * deathImpact;
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
            Debug.Log($"[EnemyDeath] Applied impulse {deathImpact} to all bones on {gameObject.name}");
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
                                Debug.Log($"[EnemyDeath] Disabled Animator on {gameObject.name}");
                            }
                        }
                    }
                }
            }
            
            var legsAnimator = GetComponent<FIMSpace.FProceduralAnimation.LegsAnimator>();
            if (legsAnimator == null)
            {
                legsAnimator = GetComponentInChildren<FIMSpace.FProceduralAnimation.LegsAnimator>();
            }
            if (legsAnimator == null)
            {
                legsAnimator = GetComponentInParent<FIMSpace.FProceduralAnimation.LegsAnimator>();
            }
            
            if (legsAnimator != null)
            {
                legsAnimator.enabled = false;
                
                if (showDebugLogs)
                {
                    Debug.Log($"[EnemyDeath] Disabled LegsAnimator on {gameObject.name}");
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
                            var fallingEnum = System.Enum.Parse(animatingModeProp.PropertyType, "Falling");
                            animatingModeProp.SetValue(handler, fallingEnum);
                            
                            if (showDebugLogs)
                            {
                                Debug.Log($"[EnemyDeath] Set Ragdoll Animation Mode to Falling on {gameObject.name}");
                            }
                        }
                    }
                }
                
                if (showDebugLogs)
                {
                    Debug.Log($"[EnemyDeath] Enabled Ragdoll on {gameObject.name}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnemyDeath] Error setting up ragdoll (reflection): {e.Message}\n{e.StackTrace}");
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
            Debug.LogError($"[EnemyDeath] Error applying impulse (reflection): {e.Message}\n{e.StackTrace}");
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
            if (customImpactDirection.HasValue)
            {
                impactDirection = customImpactDirection.Value.normalized * deathImpact;
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
                Debug.Log($"[EnemyDeath] Applied impulse {deathImpact} to all bones on {gameObject.name}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnemyDeath] Error applying impulse (reflection): {e.Message}\n{e.StackTrace}");
        }
    }
}

