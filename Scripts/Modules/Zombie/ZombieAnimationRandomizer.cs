using UnityEngine;

public class ZombieAnimationRandomizer : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Animator зомби")]
    public Animator zombieAnimator;
    
    [Tooltip("Название параметра для выбора Idle анимации")]
    public string idleAnimationParameter = "IdleAnimation";
    
    [Tooltip("Количество Idle анимаций")]
    public int idleAnimationCount = 2;
    
    [Tooltip("Интервал смены анимации (секунды)")]
    public float animationChangeInterval = 5f;
    
    [Tooltip("Случайное отклонение интервала")]
    public float animationDeviation = 2f;
    
    [Header("Debug")]
    [HideInInspector] public bool showDebugLogs = false;
    
    private int idleParameterHash;
    private float animationTimer;
    private float nextAnimationTime;
    private int currentIdleAnimation = 0;
    
    private void Awake()
    {
        if (zombieAnimator == null)
            zombieAnimator = GetComponent<Animator>();
        
        if (zombieAnimator != null)
        {
            idleParameterHash = Animator.StringToHash(idleAnimationParameter);
        }
        
        SetNextAnimationTime();
    }
    
    private void Update()
    {
        animationTimer += Time.deltaTime;
        
        if (animationTimer >= nextAnimationTime)
        {
            RandomizeIdleAnimation();
            SetNextAnimationTime();
            animationTimer = 0f;
        }
    }
    
    private void RandomizeIdleAnimation()
    {
        int newAnimation = Random.Range(0, idleAnimationCount);
        
        while (newAnimation == currentIdleAnimation && idleAnimationCount > 1)
        {
            newAnimation = Random.Range(0, idleAnimationCount);
        }
        
        currentIdleAnimation = newAnimation;
        
        if (zombieAnimator != null)
        {
            zombieAnimator.SetInteger(idleParameterHash, currentIdleAnimation);
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[ZombieAnimationRandomizer] Смена Idle анимации на: {currentIdleAnimation}");
        }
    }
    
    private void SetNextAnimationTime()
    {
        nextAnimationTime = animationChangeInterval + Random.Range(-animationDeviation, animationDeviation);
    }
    
    public void ForceRandomize()
    {
        RandomizeIdleAnimation();
        SetNextAnimationTime();
        animationTimer = 0f;
    }
    
    public int GetCurrentIdleAnimation()
    {
        return currentIdleAnimation;
    }
}


