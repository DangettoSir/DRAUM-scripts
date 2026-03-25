using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DependencyInjection; // https://github.com/adammyhre/Unity-Dependency-Injection-Lite
using UnityEngine;
using UnityEngine.AI;
using DRAUM.Core;
using DRAUM.Modules.Combat.Events;
using DRAUM.Modules.Combat;
using DRAUM.Core.Infrastructure.Logger;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AnimationController))]
[RequireComponent(typeof(EnemyEntity))]
public class GoapAgent : MonoBehaviour {
    [Header("Sensors")] 
    [SerializeField] Sensor eyesSensor;  // Eyes - поле обзора
    [SerializeField] Sensor chaseSensor; // Chase - максимальное расстояние преследования
    [SerializeField] Sensor attackSensor; // Combat - радиус атаки
    
    [Header("Known Locations")] 
    [SerializeField] Transform foodShack;
    
    NavMeshAgent navMeshAgent;
    AnimationController animations;
    Rigidbody rb;
    EnemyEntity enemyEntity;
    
    CountdownTimer statsTimer;
    
    GameObject target;
    Vector3 destination;
    
    private bool lastPlayerSeen = false;
    private bool lastInChaseRange = false;
    private bool lastInAttackRange = false;
    
    AgentGoal lastGoal;
    public AgentGoal currentGoal;
    public ActionPlan actionPlan;
    public AgentAction currentAction;
    
    public Dictionary<string, AgentBelief> beliefs;
    public HashSet<AgentAction> actions;
    public HashSet<AgentGoal> goals;
    
    [Inject] GoapFactory gFactory;
    IGoapPlanner gPlanner;
    
    public float CurrentHealth => enemyEntity != null ? enemyEntity.CurrentHealth : 0f;
    public float MaxHealth => enemyEntity != null ? enemyEntity.MaxHealth : 0f;
    public float HealthPercentage => enemyEntity != null ? enemyEntity.HealthPercentage : 0f;
    public bool IsAlive => enemyEntity != null && enemyEntity.IsAlive;
    public bool IsDead => enemyEntity != null && enemyEntity.IsDead;
    
    
    void Awake() {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animations = GetComponent<AnimationController>();
        rb = GetComponent<Rigidbody>();
        enemyEntity = GetComponent<EnemyEntity>();
        
        if (rb != null)
            rb.freezeRotation = true;
        
        if (enemyEntity == null) {
            DraumLogger.Error(this, $"[GoapAgent] EnemyEntity не найден на {gameObject.name}!");
        } else {
            enemyEntity.OnDeath += HandleEnemyDeath;
        }
        
        gPlanner = gFactory.CreatePlanner();
    }
    
    void OnDestroy() {
        if (enemyEntity != null) {
            enemyEntity.OnDeath -= HandleEnemyDeath;
        }
    }
    
    /// <summary>
    /// Обрабатывает смерть EnemyEntity - останавливает GOAP
    /// </summary>
    private void HandleEnemyDeath(EnemyEntity entity) {
        currentAction = null;
        currentGoal = null;
        actionPlan = null;
        
        if (navMeshAgent != null && navMeshAgent.enabled) {
            navMeshAgent.isStopped = true;
            navMeshAgent.ResetPath();
        }
    }
    

    void Start() {
        SetupTimers();
        SetupBeliefs();
        SetupActions();
        SetupGoals();
    }

    void SetupBeliefs() {
        beliefs = new Dictionary<string, AgentBelief>();
        BeliefFactory factory = new BeliefFactory(this, beliefs);
        
        factory.AddBelief("Nothing", () => false);
        
        factory.AddBelief("AgentIdle", () => !navMeshAgent.hasPath);
        factory.AddBelief("AgentMoving", () => navMeshAgent.hasPath);
        factory.AddBelief("AgentIsDead", () => IsDead);
        factory.AddBelief("AgentHealthLow", () => CurrentHealth < 30);
        factory.AddBelief("AgentHealthCritical", () => CurrentHealth < MaxHealth * 0.2f);
        factory.AddBelief("AgentIsHealthy", () => CurrentHealth >= 50);
        factory.AddLocationBelief("AgentAtFoodShack", 3f, foodShack);
        
        factory.AddSensorBelief("PlayerSeen", eyesSensor);
        factory.AddSensorBelief("PlayerInChaseRange", chaseSensor);
        factory.AddSensorBelief("PlayerInAttackRange", attackSensor);
        factory.AddBelief("AttackingPlayer", () => false); 
        factory.AddBelief("InCombat", () => (eyesSensor != null && eyesSensor.IsTargetInRange) || (chaseSensor != null && chaseSensor.IsTargetInRange) || (attackSensor != null && attackSensor.IsTargetInRange));
    }

    void SetupActions() {
        actions = new HashSet<AgentAction>();
        
        actions.Add(new AgentAction.Builder("Relax")
            .WithStrategy(new IdleStrategy(5))
            .AddEffect(beliefs["Nothing"])
            .Build());
        
        actions.Add(new AgentAction.Builder("Wander Around")
            .WithStrategy(new WanderStrategy(navMeshAgent, 25f))
            .AddEffect(beliefs["AgentMoving"])
            .Build());

        actions.Add(new AgentAction.Builder("MoveToEatingPosition")
            .WithStrategy(new MoveStrategy(navMeshAgent, () => foodShack.position))
            .AddEffect(beliefs["AgentAtFoodShack"])
            .Build());
        

        actions.Add(new AgentAction.Builder("QuickEat")
            .WithCost(0.5f)
            .WithStrategy(animations is FliffyAnimationController 
                ? new EatStrategy(animations, 2f)
                : new IdleStrategy(2f))
            .AddPrecondition(beliefs["AgentAtFoodShack"])
            .AddPrecondition(beliefs["InCombat"])
            .AddPrecondition(beliefs["AgentHealthCritical"])
            .AddEffect(beliefs["AgentIsHealthy"])
            .Build());
        
        actions.Add(new AgentAction.Builder("Eat")
            .WithStrategy(animations is FliffyAnimationController 
                ? new EatStrategy(animations, 5f)
                : new IdleStrategy(5f))
            .AddPrecondition(beliefs["AgentAtFoodShack"])
            .AddPrecondition(beliefs["AgentHealthLow"])
            .AddEffect(beliefs["AgentIsHealthy"])
            .Build());

        actions.Add(new AgentAction.Builder("ChasePlayer")
            .WithCost(1f) // Нормальная стоимость
            .WithStrategy(new ChaseStrategy(navMeshAgent, transform, () => chaseSensor.GetPredictedTargetPosition(0.5f), () => chaseSensor.TargetPosition))
            .AddPrecondition(beliefs["PlayerSeen"])
            .AddPrecondition(beliefs["PlayerInChaseRange"])
            .AddEffect(beliefs["PlayerInAttackRange"])
            .Build());

        actions.Add(new AgentAction.Builder("AttackPlayer")
            .WithStrategy(new AttackStrategy(animations, transform, () => attackSensor.TargetPosition, navMeshAgent))
            .AddPrecondition(beliefs["PlayerInAttackRange"])
            .AddEffect(beliefs["AttackingPlayer"])
            .Build());
    }

    void SetupGoals() {
        goals = new HashSet<AgentGoal>();
        
        goals.Add(new AgentGoal.Builder("Chill Out")
            .WithPriority(0.5f)
            .WithDesiredEffect(beliefs["Nothing"])
            .Build());
        
        goals.Add(new AgentGoal.Builder("Wander")
            .WithPriority(1.5f)
            .WithDesiredEffect(beliefs["AgentMoving"])
            .Build());
        
        goals.Add(new AgentGoal.Builder("KeepHealthUp")
            .WithPriority(2)
            .WithDesiredEffect(beliefs["AgentIsHealthy"])
            .Build());
        
        goals.Add(new AgentGoal.Builder("ChasePlayer")
            .WithPriority(2.5f)
            .WithDesiredEffect(beliefs["PlayerInAttackRange"])
            .Build());
        
        goals.Add(new AgentGoal.Builder("SeekAndDestroy")
            .WithPriority(3)
            .WithDesiredEffect(beliefs["AttackingPlayer"])
            .Build());
    }

    void SetupTimers() {
        statsTimer = new CountdownTimer(2f);
        statsTimer.OnTimerStop += () => {
            UpdateStats();
            statsTimer.Start();
        };
        statsTimer.Start();
    }

    void UpdateStats() {
        if (IsDead) return;
        
        bool atFoodShack = InRangeOf(foodShack.position, 3f);
        
        if (enemyEntity == null) return;
        
        if (atFoodShack) {
            enemyEntity.Heal(20);
        }
    }
    
    bool InRangeOf(Vector3 pos, float range) => Vector3.Distance(transform.position, pos) < range;
    
    /// <summary>
    /// Обновляет приоритеты целей в зависимости от состояния боя и здоровья
    /// </summary>
    void UpdateGoalPriorities() {
        bool inCombat = beliefs.ContainsKey("InCombat") && beliefs["InCombat"].Evaluate();
        bool playerSeen = beliefs.ContainsKey("PlayerSeen") && beliefs["PlayerSeen"].Evaluate();
        bool playerInChaseRange = beliefs.ContainsKey("PlayerInChaseRange") && beliefs["PlayerInChaseRange"].Evaluate();
        bool playerInAttackRange = beliefs.ContainsKey("PlayerInAttackRange") && beliefs["PlayerInAttackRange"].Evaluate();
        bool healthCritical = beliefs.ContainsKey("AgentHealthCritical") && beliefs["AgentHealthCritical"].Evaluate();
        bool healthLow = beliefs.ContainsKey("AgentHealthLow") && beliefs["AgentHealthLow"].Evaluate();
        
        if (playerSeen != lastPlayerSeen) {
            DraumLogger.Info(this, $"[GoapAgent] Игрок виден: {playerSeen}");
            lastPlayerSeen = playerSeen;
        }
        if (playerInChaseRange != lastInChaseRange) {
            DraumLogger.Info(this, $"[GoapAgent] Игрок в зоне преследования: {playerInChaseRange}");
            lastInChaseRange = playerInChaseRange;
        }
        if (playerInAttackRange != lastInAttackRange) {
            DraumLogger.Info(this, $"[GoapAgent] Игрок в зоне атаки: {playerInAttackRange}");
            lastInAttackRange = playerInAttackRange;
        }
        
        foreach (var goal in goals) {
            if (inCombat) {
                if (goal.Name == "SeekAndDestroy") {
                    goal.Priority = 5f;
                } else if (goal.Name == "ChasePlayer") {
                    goal.Priority = (playerSeen && !playerInAttackRange) ? 4.5f : 2.5f;
                } else if (goal.Name == "KeepHealthUp") {
                    goal.Priority = healthCritical ? 5f : (healthLow ? 4.5f : 4f);
                } else if (goal.Name == "Wander") {
                    goal.Priority = !playerSeen ? 2f : 1.5f;
                } else if (goal.Name == "Chill Out") {
                    goal.Priority = 0.5f; 
                }
            } else {
                if (goal.Name == "KeepHealthUp") {
                    goal.Priority = healthCritical ? 4f : (healthLow ? 3f : 2f);
                } else if (goal.Name == "SeekAndDestroy") {
                    goal.Priority = playerSeen ? 3f : 2f;
                } else if (goal.Name == "ChasePlayer") {
                    goal.Priority = (playerSeen && !playerInAttackRange) ? 2.8f : 2.5f;
                } else if (goal.Name == "Wander") {
                    goal.Priority = 1.5f;
                } else if (goal.Name == "Chill Out") {
                    goal.Priority = 0.5f;
                }
            }
        }
    }
    
    void OnEnable() {
        if (eyesSensor != null) eyesSensor.OnTargetChanged += HandleTargetChanged;
        if (chaseSensor != null) chaseSensor.OnTargetChanged += HandleTargetChanged;
    }
    void OnDisable() {
        if (eyesSensor != null) eyesSensor.OnTargetChanged -= HandleTargetChanged;
        if (chaseSensor != null) chaseSensor.OnTargetChanged -= HandleTargetChanged;
    }
    
    void HandleTargetChanged() {
        DraumLogger.Info(this, "Target changed, clearing current action and goal");
        currentAction = null;
        currentGoal = null;
    }

    void Update() {
    
        if (IsDead) {
            if (navMeshAgent != null && navMeshAgent.enabled)
                navMeshAgent.isStopped = true;
            return;
        }
        
        if (animations is FliffyAnimationController fliffyAnim && fliffyAnim.IsStunned) {
            if (navMeshAgent != null && navMeshAgent.enabled) {
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath();
            }
            
            if (currentAction != null) {
                currentAction.Stop();
                currentAction = null;
            }
            currentGoal = null;
            actionPlan = null;
            
            return;
        }
        
        statsTimer.Tick(Time.deltaTime);
        if (animations != null)
            animations.SetSpeed(navMeshAgent.velocity.magnitude);
        
        
        UpdateGoalPriorities();
        
        if (currentAction == null) {
            DraumLogger.Info(this, "Calculating any potential new plan");
            CalculatePlan();

            if (actionPlan != null && actionPlan.Actions.Count > 0) {
                navMeshAgent.ResetPath();

                currentGoal = actionPlan.AgentGoal;
                DraumLogger.Info(this, $"Goal: {currentGoal.Name} with {actionPlan.Actions.Count} actions in plan");
                currentAction = actionPlan.Actions.Pop();
                DraumLogger.Info(this, $"Popped action: {currentAction.Name}");
                // Verify all precondition effects are true
                if (currentAction.Preconditions.All(b => b.Evaluate())) {
                    currentAction.Start();
                } else {
                    DraumLogger.Info(this, "Preconditions not met, clearing current action and goal");
                    currentAction = null;
                    currentGoal = null;
                }
            }
        }

        if (actionPlan != null && currentAction != null) {
            currentAction.Update(Time.deltaTime);

            if (currentAction.Complete) {
                DraumLogger.Info(this, $"[GoapAgent] {currentAction.Name} complete! Время: {Time.time:F3}");
                
                if (currentAction.Name == "AttackPlayer" && animations is FliffyAnimationController fliffyAnimController) {
                    DraumLogger.Info(this, $"[GoapAgent] После завершения AttackPlayer: IsAttacking={fliffyAnimController.IsAttacking}, время: {Time.time:F3}");
                }
                
                currentAction.Stop();
                currentAction = null;

                if (actionPlan.Actions.Count == 0) {
                    DraumLogger.Info(this, $"[GoapAgent] Plan complete! Время: {Time.time:F3}");
                    lastGoal = currentGoal;
                    currentGoal = null;
                }
            }
        }
    }

    void CalculatePlan() {
        var priorityLevel = currentGoal?.Priority ?? 0;
        
        HashSet<AgentGoal> goalsToCheck = goals;
        
        if (currentGoal != null) {
            DraumLogger.Info(this, "Current goal exists, checking goals with higher priority");
            goalsToCheck = new HashSet<AgentGoal>(goals.Where(g => g.Priority > priorityLevel));
        }
        
        var potentialPlan = gPlanner.Plan(this, goalsToCheck, lastGoal);
        if (potentialPlan != null) {
            actionPlan = potentialPlan;
        }
    }
    
    /// <summary>
    /// Сбрасывает агента (очищает план и цель)
    /// </summary>
    public void Reset() {
        currentAction = null;
        currentGoal = null;
        actionPlan = null;
        
        if (navMeshAgent != null && navMeshAgent.enabled) {
            navMeshAgent.isStopped = false;
        }
        
        if (enemyEntity != null) {
            enemyEntity.Reset();
        }
    }
    
    /// <summary>
    /// Наносит урон агенту через EnemyEntity
    /// </summary>
    public float TakeDamage(float baseDamage, BodyPart bodyPart, Vector3 hitPoint) {
        if (enemyEntity == null) return 0f;

        float actualDamage = 0f;
        bool replied = false;

        // Маршрутизируем применение урона через CombatModule.
        // Если по какой-то причине подписчик не ответил — делаем fallback, чтобы не сломать здоровье/смерть.
        EnemyDamageRequestEvent evt = new EnemyDamageRequestEvent
        {
            TargetEnemy = enemyEntity,
            BaseDamage = baseDamage,
            BodyPart = bodyPart,
            HitPoint = hitPoint,
            Reply = d =>
            {
                actualDamage = d;
                replied = true;
            }
        };

        if (EventBus.Instance != null)
            EventBus.Instance.Publish(evt);

        if (!replied)
        {
            // Fallback, чтобы не потерять урон, но применение всё равно идёт через CombatModule.
            CombatModule combat = FindFirstObjectByType<CombatModule>();
            if (combat != null)
                actualDamage = combat.ApplyEnemyDamage(enemyEntity, baseDamage, bodyPart, hitPoint);
        }

        return actualDamage;
    }
    
    /// <summary>
    /// Наносит урон без учета части тела
    /// </summary>
    public float TakeDamage(float damage) {
        return TakeDamage(damage, BodyPart.Unknown, Vector3.zero);
    }
}