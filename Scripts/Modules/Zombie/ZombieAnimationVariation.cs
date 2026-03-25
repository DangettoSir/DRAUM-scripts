using UnityEngine;

[ExecuteInEditMode]
public class ZombieAnimationVariation : MonoBehaviour
{
    [Header("Animation Variation")]
    [Tooltip("Animator зомби")]
    public Animator zombieAnimator;
    
    [Tooltip("Скорость вариации анимации")]
    public float animationSpeedVariation = 0.2f;
    
    [Tooltip("Смещение времени анимации")]
    public float timeOffsetVariation = 0.5f;
    
    [Tooltip("Интервал смены вариации (секунды)")]
    public float variationChangeInterval = 3f;
    
    [Tooltip("Случайное отклонение интервала")]
    public float variationDeviation = 1f;
    
    [Header("Debug")]
    [HideInInspector] public bool showDebugLogs = false;
    
    private float variationTimer;
    private float nextVariationTime;
    private float currentSpeedMultiplier = 1f;
    private float currentTimeOffset = 0f;
    
    private void Awake()
    {
        if (zombieAnimator == null)
            zombieAnimator = GetComponent<Animator>();
        
        SetNextVariationTime();
        RandomizeAnimation();
    }
    
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        if (zombieAnimator == null)
            zombieAnimator = GetComponent<Animator>();
    }
    
    private void Update()
    {
        variationTimer += Time.deltaTime;
        
        if (variationTimer >= nextVariationTime)
        {
            RandomizeAnimation();
            SetNextVariationTime();
            variationTimer = 0f;
        }
        
        ApplyAnimationVariation();
    }
    
    private void RandomizeAnimation()
    {
        currentSpeedMultiplier = 1f + Random.Range(-animationSpeedVariation, animationSpeedVariation);
        currentTimeOffset = Random.Range(-timeOffsetVariation, timeOffsetVariation);
        
        if (zombieAnimator != null)
        {
            zombieAnimator.speed = currentSpeedMultiplier;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[ZombieAnimationVariation] Speed: {currentSpeedMultiplier:F2}, TimeOffset: {currentTimeOffset:F2}");
        }
    }
    
    private void ApplyAnimationVariation()
    {
        if (zombieAnimator != null)
        {
            zombieAnimator.speed = currentSpeedMultiplier;
            
            AnimatorStateInfo stateInfo = zombieAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.normalizedTime > 0f)
            {
                zombieAnimator.Play(stateInfo.shortNameHash, 0, (stateInfo.normalizedTime + currentTimeOffset) % 1f);
            }
        }
    }
    
    private void SetNextVariationTime()
    {
        nextVariationTime = variationChangeInterval + Random.Range(-variationDeviation, variationDeviation);
    }
    
    public void ForceRandomize()
    {
        RandomizeAnimation();
        SetNextVariationTime();
        variationTimer = 0f;
    }
}


