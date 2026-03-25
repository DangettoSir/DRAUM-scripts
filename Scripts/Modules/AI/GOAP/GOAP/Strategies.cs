using System;
using UnityEngine;
using UnityEngine.AI;

// TODO Перенести Strategies, Beliefs, Actions и Goals в Scriptable Objects и создать для них Node Editor
public interface IActionStrategy {
    bool CanPerform { get; }
    bool Complete { get; }
    
    void Start() {
        // пипиапа
    }
    
    void Update(float deltaTime) {
    }
    
    void Stop() {
    }
}

public class AttackStrategy : IActionStrategy {
    public bool CanPerform => true;
    public bool Complete { get; private set; }
    
    CountdownTimer timer;
    readonly AnimationController animations;
    readonly Transform agentTransform;
    readonly NavMeshAgent navMeshAgent;
    readonly Func<Vector3> targetPositionGetter;
    
    private bool isRotating = false;
    private bool rotationComplete = false;
    private bool attackStarted = false;
    private const float rotationSpeed = 5f;
    private const float rotationThreshold = 5f;

    public AttackStrategy(AnimationController animations, Transform agentTransform, Func<Vector3> targetPositionGetter, NavMeshAgent navMeshAgent = null) {
        this.animations = animations;
        this.agentTransform = agentTransform;
        this.targetPositionGetter = targetPositionGetter;
        this.navMeshAgent = navMeshAgent;
        
        float defaultLength = 1.5f;
        
        if (!(animations is FliffyAnimationController) && animations.attackClip != 0) {
            defaultLength = animations.GetAnimationLength(animations.attackClip);
        }
        
        if (!(animations is FliffyAnimationController)) {
            timer = new CountdownTimer(defaultLength);
            timer.OnTimerStart += () => Complete = false;
            timer.OnTimerStop += () => Complete = true;
        }
    }
    
    public void Start() {
        Debug.Log($"[AttackStrategy] Start() вызван! Время: {Time.time:F3}");
        Complete = false;
        isRotating = true;
        rotationComplete = false;
        attackStarted = false;
        
        if (navMeshAgent != null) {
            navMeshAgent.updateRotation = false;
            Debug.Log($"[AttackStrategy] NavMeshAgent.updateRotation = false, время: {Time.time:F3}");
        }
    }
    
    public void Update(float deltaTime) {
        if (isRotating && !rotationComplete) {
            Vector3 targetPos = targetPositionGetter();
            if (targetPos != Vector3.zero) {
                Vector3 direction = (targetPos - agentTransform.position).normalized;
                direction.y = 0;
                
                if (direction != Vector3.zero) {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    agentTransform.rotation = Quaternion.Slerp(agentTransform.rotation, targetRotation, rotationSpeed * deltaTime);
                    
                    float angle = Quaternion.Angle(agentTransform.rotation, targetRotation);
                    if (angle <= rotationThreshold) {
                        rotationComplete = true;
                        isRotating = false;
                    }
                }
            } else {
                rotationComplete = true;
                isRotating = false;
            }
        } else if (rotationComplete && !attackStarted) {
            if (animations is FliffyAnimationController fliffyAnim && fliffyAnim.IsStunned) {
                Debug.Log($"[AttackStrategy] Моб оглушен, не могу атаковать! Время: {Time.time:F3}");
                Complete = true;
                return;
            }
            
            Debug.Log($"[AttackStrategy] Поворот завершен, начинаем атаку! Время: {Time.time:F3}");
            attackStarted = true;
            animations.Attack();
            
            if (!(animations is FliffyAnimationController)) {
                timer.Start();
                Debug.Log($"[AttackStrategy] Запущен таймер для не-Fliffy контроллера, время: {Time.time:F3}");
            } else {
                Debug.Log($"[AttackStrategy] FliffyAnimationController - ждем AnimationEvent, время: {Time.time:F3}");
            }
        } else if (attackStarted) {
            if (animations is FliffyAnimationController fliffyAnim) {
                bool isAttackingNow = fliffyAnim.IsAttacking;
                if (!isAttackingNow && !Complete) {
                    Debug.Log($"[AttackStrategy] Атака завершена! IsAttacking={isAttackingNow}, Complete: false -> true, время: {Time.time:F3}");
                    Complete = true;
                } else if (isAttackingNow) {
                    if ((Time.frameCount % 30) == 0) {
                        Debug.Log($"[AttackStrategy] Атака в процессе... IsAttacking={isAttackingNow}, Complete={Complete}, время: {Time.time:F3}");
                    }
                }
            } else {
                timer.Tick(deltaTime);
            }
        }
    }
    
    public void Stop() {
        if (navMeshAgent != null) {
            navMeshAgent.updateRotation = true;
        }
    }
}

public class MoveStrategy : IActionStrategy {
    readonly NavMeshAgent agent;
    readonly Func<Vector3> destination;
    readonly float normalSpeed;
    float accelerationTimer = 0f;
    const float accelerationDuration = 0.5f;
    const float maxSpeedMultiplier = 4f;
    
    public bool CanPerform => !Complete;
    public bool Complete => agent.remainingDistance <= 2f && !agent.pathPending;
    
    public MoveStrategy(NavMeshAgent agent, Func<Vector3> destination) {
        this.agent = agent;
        this.destination = destination;
        this.normalSpeed = agent.speed;
    }
    
    public void Start() {
        agent.SetDestination(destination());
        accelerationTimer = 0f;
        agent.speed = normalSpeed * maxSpeedMultiplier;
    }
    
    public void Update(float deltaTime) {
        if (accelerationTimer < accelerationDuration) {
            accelerationTimer += deltaTime;

            float t = accelerationTimer / accelerationDuration;
            agent.speed = Mathf.Lerp(normalSpeed * maxSpeedMultiplier, normalSpeed, t);
        } else {
            agent.speed = normalSpeed;
        }
    }
    
    public void Stop() {
        agent.ResetPath();
        agent.speed = normalSpeed;
    }
}

public class ChaseStrategy : IActionStrategy {
    readonly NavMeshAgent agent;
    readonly Transform agentTransform;
    readonly Func<Vector3> predictedDestination;
    readonly Func<Vector3> currentTargetPosition;
    readonly float normalSpeed;
    float accelerationTimer = 0f;
    const float accelerationDuration = 0.5f;
    const float maxSpeedMultiplier = 4f;
    const float rotationSpeed = 5f;
    const float rotationThreshold = 10f;
    
    public bool CanPerform => !Complete;
    public bool Complete => agent.remainingDistance <= 2f && !agent.pathPending;
    
    public ChaseStrategy(NavMeshAgent agent, Transform agentTransform, Func<Vector3> predictedDestination, Func<Vector3> currentTargetPosition) {
        this.agent = agent;
        this.agentTransform = agentTransform;
        this.predictedDestination = predictedDestination;
        this.currentTargetPosition = currentTargetPosition;
        this.normalSpeed = agent.speed;
    }
    
    public void Start() {
        agent.updateRotation = false;
        accelerationTimer = 0f;
        agent.speed = normalSpeed * maxSpeedMultiplier;
    }
    
    public void Update(float deltaTime) {
        Vector3 targetPos = currentTargetPosition();
        Vector3 predictedPos = predictedDestination();
        
        Vector3 destination = predictedPos != Vector3.zero ? predictedPos : targetPos;
        
        if (destination != Vector3.zero) {
            Vector3 direction = (destination - agentTransform.position).normalized;
            direction.y = 0;
            
            if (direction != Vector3.zero) {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                agentTransform.rotation = Quaternion.Slerp(agentTransform.rotation, targetRotation, rotationSpeed * deltaTime);
                
                float angle = Quaternion.Angle(agentTransform.rotation, targetRotation);
                if (angle <= rotationThreshold) {
                    agent.SetDestination(destination);
                }
            }
        }
        
        if (accelerationTimer < accelerationDuration) {
            accelerationTimer += deltaTime;
            float t = accelerationTimer / accelerationDuration;
            agent.speed = Mathf.Lerp(normalSpeed * maxSpeedMultiplier, normalSpeed, t);
        } else {
            agent.speed = normalSpeed;
        }
    }
    
    public void Stop() {
        agent.updateRotation = true;
        agent.ResetPath();
        agent.speed = normalSpeed;
    }
}

public class WanderStrategy : IActionStrategy {
    readonly NavMeshAgent agent;
    readonly float wanderRadius;
    readonly float normalSpeed;
    float accelerationTimer = 0f;
    const float accelerationDuration = 0.5f;
    const float maxSpeedMultiplier = 4f;
    
    public bool CanPerform => !Complete;
    public bool Complete => agent.remainingDistance <= 2f && !agent.pathPending;
    
    public WanderStrategy(NavMeshAgent agent, float wanderRadius) {
        this.agent = agent;
        this.wanderRadius = wanderRadius;
        this.normalSpeed = agent.speed;
    }

    public void Start() {
        for (int i = 0; i < 5; i++) {
            Vector3 randomDirection = (UnityEngine.Random.insideUnitSphere * wanderRadius).With(y: 0);
            NavMeshHit hit;

            if (NavMesh.SamplePosition(agent.transform.position + randomDirection, out hit, wanderRadius, 1)) {
                agent.SetDestination(hit.position);
                accelerationTimer = 0f;
                agent.speed = normalSpeed * maxSpeedMultiplier;
                return;
            }
        }
    }
    
    public void Update(float deltaTime) {
        if (accelerationTimer < accelerationDuration) {
            accelerationTimer += deltaTime;
            float t = accelerationTimer / accelerationDuration;
            agent.speed = Mathf.Lerp(normalSpeed * maxSpeedMultiplier, normalSpeed, t);
        } else {
            agent.speed = normalSpeed;
        }
    }
    
    public void Stop() {
        agent.speed = normalSpeed;
    }
}

public class RetreatStrategy : IActionStrategy {
    readonly NavMeshAgent agent;
    readonly Func<Vector3> retreatDestination;
    readonly float normalSpeed;
    float accelerationTimer = 0f;
    const float accelerationDuration = 0.5f;
    const float maxSpeedMultiplier = 4f;
    
    public bool CanPerform => !Complete;
    public bool Complete => agent.remainingDistance <= 2f && !agent.pathPending;
    
    public RetreatStrategy(NavMeshAgent agent, Func<Vector3> retreatDestination) {
        this.agent = agent;
        this.retreatDestination = retreatDestination;
        this.normalSpeed = agent.speed;
    }
    
    public void Start() {
        Vector3 dest = retreatDestination();
        NavMeshHit hit;
        if (NavMesh.SamplePosition(dest, out hit, 5f, 1)) {
            agent.SetDestination(hit.position);
        }
        accelerationTimer = 0f;
        agent.speed = normalSpeed * maxSpeedMultiplier;
    }
    
    public void Stop() {
        agent.ResetPath();
        agent.speed = normalSpeed;
    }
    
    public void Update(float deltaTime) {
        Vector3 dest = retreatDestination();
        if (Vector3.Distance(agent.destination, dest) > 2f) {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(dest, out hit, 5f, 1)) {
                agent.SetDestination(hit.position);
            }
        }
        
        if (accelerationTimer < accelerationDuration) {
            accelerationTimer += deltaTime;
            float t = accelerationTimer / accelerationDuration;
            agent.speed = Mathf.Lerp(normalSpeed * maxSpeedMultiplier, normalSpeed, t);
        } else {
            agent.speed = normalSpeed;
        }
    }
}

public class IdleStrategy : IActionStrategy {
    public bool CanPerform => true;
    public bool Complete { get; private set; }
    
    readonly CountdownTimer timer;

    public IdleStrategy(float duration) {
        timer = new CountdownTimer(duration);
        timer.OnTimerStart += () => Complete = false;
        timer.OnTimerStop += () => Complete = true;
    }
    
    public void Start() => timer.Start();
    public void Update(float deltaTime) => timer.Tick(deltaTime);
}

public class EatStrategy : IActionStrategy {
    public bool CanPerform => true;
    public bool Complete { get; private set; }
    
    readonly CountdownTimer timer;
    readonly FliffyAnimationController fliffyAnimations;
    readonly AnimationController animations;
    readonly float totalDuration;
    
    public EatStrategy(AnimationController animations, float eatDuration) {
        this.animations = animations;
        this.fliffyAnimations = animations as FliffyAnimationController;
        
        float calculatedDuration = eatDuration;
        if (fliffyAnimations != null) {
            float eatStartLength = fliffyAnimations.GetAnimationLength(Animator.StringToHash("Eat-Start"));
            float eatEndLength = fliffyAnimations.GetAnimationLength(Animator.StringToHash("Eat-End"));
            
            if (eatStartLength > 0) calculatedDuration += eatStartLength;
            if (eatEndLength > 0) calculatedDuration += eatEndLength;
        }
        
        totalDuration = calculatedDuration;
        timer = new CountdownTimer(totalDuration);
        timer.OnTimerStart += () => Complete = false;
        timer.OnTimerStop += () => Complete = true;
    }
    
    public void Start() {
        timer.Start();
        
        if (fliffyAnimations != null) {
            float eatStartLength = fliffyAnimations.GetAnimationLength(Animator.StringToHash("Eat-Start"));
            float eatEndLength = fliffyAnimations.GetAnimationLength(Animator.StringToHash("Eat-End"));
            float eatDuration = totalDuration - eatStartLength - eatEndLength;
            
            fliffyAnimations.StartEating(Mathf.Max(0f, eatDuration));
        }
    }
    
    public void Update(float deltaTime) => timer.Tick(deltaTime);
}