using UnityEngine;
using DRAUM.Core;
using DRAUM.Core.Infrastructure.Logger;
using DRAUM.Modules.Player.Events;

public class WeaponCollisionDetector : MonoBehaviour
{
    [Header("Weapon Settings")]
    [Tooltip("Масса оружия (для расчета силы удара)")]
    public float weaponMass = 1f;
    
    [Tooltip("Тег врага для обнаружения")]
    public string enemyTag = "Enemy";
    
    [Header("Hit Force Settings")]
    [Tooltip("Базовый множитель силы удара")]
    [Range(0.1f, 10f)]
    public float baseForceMultiplier = 1f;
    
    [Tooltip("Множитель силы от скорости оружия")]
    [Range(0f, 2f)]
    public float speedForceMultiplier = 0.5f;
    
    [Header("Speed Tracking")]
    [Tooltip("Интервал обновления скорости (секунды)")]
    public float speedUpdateInterval = 0.02f;
    
    [Header("Weapon Info")]
    [Tooltip("Имя оружия для звуков (если пусто, используется gameObject.name)")]
    public string weaponName;
    
    [Header("Debug")]
    [HideInInspector] public bool showDebugLogs = false;
    
    private Rigidbody rb;
    private Collider weaponCollider;
    private Vector3 previousPosition;
    private float previousTime;
    private float currentSpeed = 0f;
    private float averageSpeed = 0f;
    private int speedSamples = 0;
    
    public float CurrentSpeed => currentSpeed;
    public float AverageSpeed => averageSpeed;
    public float WeaponMass => weaponMass;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        weaponCollider = GetComponent<Collider>();
        
        if (weaponCollider == null)
        {
            weaponCollider = GetComponentInChildren<Collider>();
        }
        
        if (weaponCollider != null)
        {
            weaponCollider.isTrigger = true;
        }
        else
            DraumLogger.Warning(this, $"[WeaponCollisionDetector] Не найдена коллизия на оружии!");
        
        previousPosition = transform.position;
        previousTime = Time.time;
    }
    
    private void Update()
    {
        UpdateSpeed();
    }
    
    private void UpdateSpeed()
    {
        float currentTime = Time.time;
        float deltaTime = currentTime - previousTime;
        
        if (deltaTime >= speedUpdateInterval)
        {
            if (rb != null && rb.isKinematic == false)
            {
                currentSpeed = rb.linearVelocity.magnitude;
            }
            else
            {
                Vector3 currentPos = transform.position;
                float distance = Vector3.Distance(currentPos, previousPosition);
                currentSpeed = distance / deltaTime;
                previousPosition = currentPos;
            }
            
            previousTime = currentTime;
            
            averageSpeed = (averageSpeed * speedSamples + currentSpeed) / (speedSamples + 1);
            speedSamples++;
            
            if (speedSamples > 100)
            {
                speedSamples = 50;
                averageSpeed *= 0.5f;
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(enemyTag))
        {
            return;
        }
        
        Vector3 hitPoint = weaponCollider != null 
            ? weaponCollider.ClosestPoint(other.bounds.center)
            : transform.position;
        
        float hitSpeed = currentSpeed;
        
        DraumLogger.Info(this, $"[WeaponCollisionDetector] OnTriggerEnter: Удар в {other.name}, Точка: {hitPoint}, Скорость: {hitSpeed:F2}, Масса: {weaponMass}");
        
        ZombieHitReaction hitReaction = other.GetComponent<ZombieHitReaction>();
        if (hitReaction == null)
        {
            hitReaction = other.GetComponentInParent<ZombieHitReaction>();
        }
        
        if (hitReaction != null)
        {
            hitReaction.RegisterHit(hitPoint, hitSpeed, weaponMass, baseForceMultiplier, speedForceMultiplier);
        }
        
        // Импакт-анимация на зомби по направлению удара (FPMAxe) и «сзади»
        FliffyAnimationController fliffyAnim = other.GetComponent<FliffyAnimationController>();
        if (fliffyAnim == null)
        {
            fliffyAnim = other.GetComponentInParent<FliffyAnimationController>();
        }
        if (fliffyAnim != null)
        {
            FPMAxe fpmAxe = GetComponentInParent<FPMAxe>();
            if (fpmAxe != null)
            {
                var dir = fpmAxe.GetLastAttackDirection();
                bool swingLeft = dir == FPMAxe.WindUpState.Left;
                bool swingRight = dir == FPMAxe.WindUpState.Right;
                bool swingUp = dir == FPMAxe.WindUpState.Up;
                Transform zombieRoot = fliffyAnim.transform;
                Transform playerRoot = fpmAxe.transform;
                Vector3 toPlayer = (playerRoot.position - zombieRoot.position).normalized;
                bool playerIsBehind = Vector3.Dot(zombieRoot.forward, toPlayer) < 0f;
                DraumLogger.Info(this, $"[WeaponCollisionDetector] Impact: enemy={other.name}, dir={dir}, playerBehind={playerIsBehind}, dot={Vector3.Dot(zombieRoot.forward, toPlayer):F2}");
                fliffyAnim.PlayImpact(swingLeft, swingRight, swingUp, playerIsBehind);
            }
            else
                DraumLogger.Info(this, $"[WeaponCollisionDetector] Impact skip: FPMAxe not found in parent of {gameObject.name}");
        }
        else
            DraumLogger.Info(this, $"[WeaponCollisionDetector] Impact skip: FliffyAnimationController not found on {other.name}");
        

        if (EventBus.Instance != null)
        {
            string materialName = other.sharedMaterial?.name ?? "Default";
            string weapon = !string.IsNullOrEmpty(weaponName) ? weaponName : gameObject.name;
            
            PlayerCombatHitEvent hitEvent = new PlayerCombatHitEvent
            {
                WeaponName = weapon,
                MaterialName = materialName,
                HitPosition = hitPoint,
                SwingDirection = "",
                IsImpact = true,
                HitCollider = other,
                HitSpeed = hitSpeed,
                WeaponMass = weaponMass,
                BaseForceMultiplier = baseForceMultiplier,
                SpeedForceMultiplier = speedForceMultiplier
            };
            
            EventBus.Instance.Publish(hitEvent);
            DraumLogger.Info(this, $"[WeaponCollisionDetector] Опубликовано событие удара: Weapon={weapon}, Material={materialName}");
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(enemyTag))
            DraumLogger.Info(this, $"[WeaponCollisionDetector] OnTriggerStay: {other.name}");
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(enemyTag))
            DraumLogger.Info(this, $"[WeaponCollisionDetector] OnTriggerExit: {other.name}");
    }
}

