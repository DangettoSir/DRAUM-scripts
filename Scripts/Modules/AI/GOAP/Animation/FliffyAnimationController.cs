using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FliffyAnimationController : AnimationController {
    [Header("Block Hit Particles")]
    [Tooltip("Префаб партиклов для спавна при попадании атаки в блок игрока")]
    public GameObject blockHitParticlesPrefab;
    
    [Tooltip("Смещение позиции спавна партиклов по XYZ (относительно позиции оружия/игрока)")]
    public Vector3 blockHitParticleOffset = new Vector3(0f, 0f, 0f);
    
    [Tooltip("Время жизни партиклов (сек). Если 0 - удаляется автоматически через ParticleSystem.main.duration")]
    public float blockHitParticlesLifetime = 0f;
    
    private const int LEFT_ATTACK = 0;
    private const int UP_ATTACK = 1;
    private const int RIGHT_ATTACK = 2;
    
    private int attackIntHash;
    private int isAttackingHash;
    private int isEatingHash;
    private int isStunnedHash;
    
    private Queue<int> attackHistory = new Queue<int>();
    private const int maxHistorySize = 10;
    
    private bool isEating = false;
    private bool isAttacking = false;
    private bool isStunned = false;
    
    /// <summary>
    /// Проверяет, атакует ли сейчас агент
    /// </summary>
    public bool IsAttacking => isAttacking;
    
    /// <summary>
    /// Проверяет, оглушен ли сейчас агент
    /// </summary>
    public bool IsStunned => isStunned;
    
    /// <summary>
    /// Вызывается через AnimationEvent в конце атаки
    /// Важно добавить AnimationEvent в конце каждой атаки (Left-Attack, Up-Attack, Right-Attack) в Animator Controller
    /// </summary>
    public void OnAttackEnd() {
        Debug.Log($"[FliffyAnimationController] OnAttackEnd вызван! isAttacking до: {isAttacking}, время: {Time.time:F3}");
        
        if (animator != null && animator.isActiveAndEnabled) {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"[FliffyAnimationController] Текущее состояние аниматора: {stateInfo.fullPathHash}, normalizedTime: {stateInfo.normalizedTime:F3}");
            
            animator.SetBool(isAttackingHash, false);
            animator.SetInteger(attackIntHash, -1);
        }
        
        bool wasAttacking = isAttacking;
        isAttacking = false;
        Debug.Log($"[FliffyAnimationController] Атака завершена через AnimationEvent! isAttacking: {wasAttacking} -> {isAttacking}, время: {Time.time:F3}");
    }
    
    /// <summary>
    /// Вызывается через AnimationEvent AddDamageToPlayer в момент нанесения урона
    /// Проверяет, блокирует ли игрок - если да, оглушает моба, если нет - наносит урон игроку
    /// </summary>
    public void AddDamageToPlayer() {
        Debug.Log($"[FliffyAnimationController] AddDamageToPlayer вызван! Время: {Time.time:F3}");
        
        if (!isAttacking) {
            Debug.LogWarning($"[FliffyAnimationController] AddDamageToPlayer вызван, но моб не атакует! isAttacking={isAttacking}");
            return;
        }
        
        if (isStunned) {
            Debug.LogWarning($"[FliffyAnimationController] AddDamageToPlayer вызван, но моб уже оглушен!");
            return;
        }
        

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) {
            Debug.LogWarning($"[FliffyAnimationController] Игрок не найден по тегу 'Player'!");
            return;
        }
        
        FPMAxe fpmAxe = player.GetComponent<FPMAxe>();
        if (fpmAxe == null) {
            fpmAxe = player.GetComponentInChildren<FPMAxe>();
        }
        if (fpmAxe == null) {
            fpmAxe = player.GetComponentInParent<FPMAxe>();
        }
        
        if (fpmAxe == null) {
            Debug.LogWarning($"[FliffyAnimationController] FPMAxe не найден на игроке!");
            return;
        }
        
        bool playerIsBlocking = fpmAxe.IsBlocking();
        Debug.Log($"[FliffyAnimationController] Игрок блокирует: {playerIsBlocking}, время: {Time.time:F3}");
        
        if (playerIsBlocking) {
            Debug.Log($"[FliffyAnimationController] Игрок заблокировал атаку! Оглушаем моба, время: {Time.time:F3}");
            
            SpawnBlockHitParticles(player.transform.position);
            
            StartStun();
        } else {
            Debug.Log($"[FliffyAnimationController] Игрок НЕ блокирует! Наносим урон игроку, время: {Time.time:F3}");
            // TODO: Здесь нужно добавить логику нанесения урона игроку
        }
    }
    
    /// <summary>
    /// Включает состояние оглушения (Stun)
    /// </summary>
    public void StartStun() {
        if (isStunned) {
            Debug.LogWarning($"[FliffyAnimationController] Уже оглушен! Время: {Time.time:F3}");
            return;
        }
        
        Debug.Log($"[FliffyAnimationController] StartStun вызван! Время: {Time.time:F3}");
        
        isStunned = true;

        if (isAttacking) {
            animator.SetBool(isAttackingHash, false);
            animator.SetInteger(attackIntHash, -1);
            isAttacking = false;
        }
        

        if (animator != null && animator.isActiveAndEnabled) {
            animator.SetBool(isStunnedHash, true);
            Debug.Log($"[FliffyAnimationController] IsStunned установлен в true, переход в Stun состояние, время: {Time.time:F3}");
        }
    }
    
    /// <summary>
    /// Вызывается через AnimationEvent в конце Stun анимации
    /// Добавь AnimationEvent в конце Stun состояния в Animator Controller
    /// </summary>
    public void OnStunEnd() {
        Debug.Log($"[FliffyAnimationController] OnStunEnd вызван! isStunned до: {isStunned}, время: {Time.time:F3}");
        
        if (animator != null && animator.isActiveAndEnabled) {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"[FliffyAnimationController] Текущее состояние аниматора: {stateInfo.fullPathHash}, normalizedTime: {stateInfo.normalizedTime:F3}");
            
            animator.SetBool(isStunnedHash, false);
            
            float idleLength = GetAnimationLength(locomotionClip);
            if (idleLength > 0) {
                animator.CrossFade(locomotionClip, 0.1f);
            } else {
                animator.Play("Idle1", 0, 0f);
            }
        }
        
        bool wasStunned = isStunned;
        isStunned = false;
        Debug.Log($"[FliffyAnimationController] Оглушение завершено! isStunned: {wasStunned} -> {isStunned}, время: {Time.time:F3}");
    }
    
    protected override void SetLocomotionClip() {
        locomotionClip = Animator.StringToHash("Idle1");
    }
    
    protected override void SetAttackClip() {
        attackClip = Animator.StringToHash("Right-Attack");
    }
    
    protected override void SetSpeedHash() {
        speedHash = Animator.StringToHash("Speed");
        
        attackIntHash = Animator.StringToHash("AttackInt");
        isAttackingHash = Animator.StringToHash("IsAttacking");
        isEatingHash = Animator.StringToHash("IsEating");
        isStunnedHash = Animator.StringToHash("IsStunned");
    }
    
    /// <summary>
    /// Проигрывает рандомную атаку с избежанием повторений
    /// </summary>
    public override void Attack() {
        Debug.Log($"[FliffyAnimationController] Attack() вызван! isAttacking до: {isAttacking}, время: {Time.time:F3}");
        
        int selectedAttack = GetRandomAttack();
        string attackName = GetAttackStateName(selectedAttack);
        Debug.Log($"[FliffyAnimationController] Выбрана атака: {attackName} (int={selectedAttack}), время: {Time.time:F3}");
        
        attackHistory.Enqueue(selectedAttack);
        if (attackHistory.Count > maxHistorySize) {
            attackHistory.Dequeue();
        }
        
        PlayAttackState(selectedAttack);
    }
    
    /// <summary>
    /// Выбирает рандомную атаку, избегая повторений из истории
    /// </summary>
    private int GetRandomAttack() {
        List<int> availableAttacks = new List<int> { LEFT_ATTACK, UP_ATTACK, RIGHT_ATTACK };
        
        if (attackHistory.Count >= maxHistorySize) {
            HashSet<int> recentAttacks = new HashSet<int>(attackHistory);
            
            if (recentAttacks.Count >= availableAttacks.Count) {
                attackHistory.Clear();
            } else {
                availableAttacks.RemoveAll(attack => recentAttacks.Contains(attack));
                
                if (availableAttacks.Count == 0) {
                    availableAttacks = new List<int> { LEFT_ATTACK, UP_ATTACK, RIGHT_ATTACK };
                }
            }
        }
        
        return availableAttacks[Random.Range(0, availableAttacks.Count)];
    }
    
    /// <summary>
    /// Проигрывает атаку через параметр AttackInt (переходы настроены в AC)
    /// AnimationEvent в конце анимации вызовет OnAttackEnd()
    /// </summary>
    private void PlayAttackState(int attackInt) {
        if (animator == null || !animator.isActiveAndEnabled) {
            Debug.LogWarning($"[FliffyAnimationController] Animator недоступен! attackInt={attackInt}");
            return;
        }
        
        if (isStunned) {
            Debug.LogWarning($"[FliffyAnimationController] Оглушен! Не могу атаковать {attackInt}, время: {Time.time:F3}");
            return; 
        }
        
        if (isAttacking) {
            Debug.LogWarning($"[FliffyAnimationController] Уже атакуем! Пропускаем атаку {attackInt}, время: {Time.time:F3}");
            return;
        }
        
        string stateName = GetAttackStateName(attackInt);
        Debug.Log($"[FliffyAnimationController] PlayAttackState: {stateName}, время: {Time.time:F3}");
        
        bool wasAttacking = isAttacking;
        isAttacking = true;
        Debug.Log($"[FliffyAnimationController] isAttacking изменен: {wasAttacking} -> {isAttacking}, время: {Time.time:F3}");
        
        animator.SetInteger(attackIntHash, attackInt);
        animator.SetBool(isAttackingHash, true);
        Debug.Log($"[FliffyAnimationController] Параметры установлены: AttackInt={attackInt}, IsAttacking=true, время: {Time.time:F3}");
        
    }
    
    /// <summary>
    /// Получает имя состояния атаки по значению AttackInt
    /// </summary>
    private string GetAttackStateName(int attackInt) {
        switch (attackInt) {
            case LEFT_ATTACK: return "Left-Attack";
            case UP_ATTACK: return "Up-Attack";
            case RIGHT_ATTACK: return "Right-Attack";
            default: return "Right-Attack";
        }
    }
    
    /// <summary>
    /// Проигрывает импакт-анимацию от удара игрока (справа/слева/сверху).
    /// Вызывается из WeaponCollisionDetector при попадании по зомби.
    /// </summary>
    /// <param name="swingLeft">Удар слева</param>
    /// <param name="swingRight">Удар справа</param>
    /// <param name="swingUp">Удар сверху</param>
    /// <param name="playerIsBehind">Игрок бьёт сзади — использовать Mirror-анимации для L/R</param>
    public void PlayImpact(bool swingLeft, bool swingRight, bool swingUp, bool playerIsBehind)
    {
        Debug.Log($"[FliffyAnimationController] PlayImpact called: L={swingLeft} R={swingRight} U={swingUp} playerBehind={playerIsBehind} | obj={gameObject.name}");
        if (animator == null)
        {
            Debug.LogWarning($"[FliffyAnimationController] PlayImpact: animator is null, skip");
            return;
        }
        if (!animator.isActiveAndEnabled)
        {
            Debug.LogWarning($"[FliffyAnimationController] PlayImpact: animator not active/enabled, skip");
            return;
        }
        
        string stateName = null;
        if (swingUp)
        {
            stateName = "IMPACT-UP";
        }
        else if (swingLeft)
        {
            stateName = playerIsBehind ? "IMPACT-L-Mirror" : "IMPACT-L";
        }
        else if (swingRight)
        {
            stateName = playerIsBehind ? "IMPACT-R-Mirror" : "IMPACT-R";
        }
        
        if (string.IsNullOrEmpty(stateName))
        {
            Debug.LogWarning($"[FliffyAnimationController] PlayImpact: неизвестное направление L={swingLeft} R={swingRight} U={swingUp}, stateName не выбран");
            return;
        }
        
        animator.CrossFadeInFixedTime(stateName, 0.1f, 0, 0f);
        Debug.Log($"[FliffyAnimationController] Impact playing: state={stateName} | obj={gameObject.name}");
    }
    
    /// <summary>
    /// Получает длину анимации состояния через временный переход
    /// </summary>
    private float GetStateAnimationLength(string stateName) {
        if (animator == null || animator.runtimeAnimatorController == null) return -1f;
        
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        string currentStateName = currentState.IsName("Idle1") ? "Idle1" : "";
        
        int stateHash = Animator.StringToHash(stateName);
        animator.Play(stateHash, 0, 0f);
        animator.Update(0.01f);
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float length = -1f;
        
        if (stateInfo.IsName(stateName)) {
            length = stateInfo.length;
        }
        
        if (!string.IsNullOrEmpty(currentStateName)) {
            animator.Play(Animator.StringToHash(currentStateName), 0, currentState.normalizedTime);
        } else {
            animator.Play(0, 0, 0f);
        }
        
        return length;
    }
    
    
    /// <summary>
    /// Начинает процесс еды: проигрывает Eat-Start, затем Eat, затем Eat-End
    /// </summary>
    public void StartEating(float eatDuration) {
        if (!isEating) {
            isEating = true;
            StartCoroutine(EatingSequence(eatDuration));
        }
    }
    
    private IEnumerator EatingSequence(float eatDuration) {
        const float k_crossfadeDuration = 0.1f;
        
        animator.SetBool(isEatingHash, true);
        
        string eatStartState = "Eat-Start";
        float eatStartLength = GetStateAnimationLength(eatStartState);
        if (eatStartLength > 0) {
            animator.CrossFade(eatStartState, k_crossfadeDuration);
            
            float waitTime = 0f;
            while (waitTime < 0.2f) {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName(eatStartState)) {
                    break;
                }
                waitTime += Time.deltaTime;
                yield return null;
            }
            
            float elapsed = 0f;
            while (elapsed < eatStartLength) {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (!stateInfo.IsName(eatStartState) && elapsed > 0.1f) {
                    break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        string eatState = "Eat";
        float eatLength = GetStateAnimationLength(eatState);
        if (eatLength > 0) {
            animator.CrossFade(eatState, k_crossfadeDuration);
            
            float waitTime = 0f;
            while (waitTime < 0.2f) {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName(eatState)) {
                    break;
                }
                waitTime += Time.deltaTime;
                yield return null;
            }
            
            float elapsed = 0f;
            float lastLoopTime = 0f;
            
            while (elapsed < eatDuration) {
                elapsed += Time.deltaTime;
                
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                
                if (elapsed - lastLoopTime >= eatLength || (!stateInfo.IsName(eatState) && elapsed - lastLoopTime > 0.1f)) {
                    animator.CrossFade(eatState, k_crossfadeDuration);
                    lastLoopTime = elapsed;
                    
                    waitTime = 0f;
                    while (waitTime < 0.1f) {
                        stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                        if (stateInfo.IsName(eatState)) {
                            break;
                        }
                        waitTime += Time.deltaTime;
                        yield return null;
                    }
                }
                
                yield return null;
            }
        } else {
            yield return new WaitForSeconds(eatDuration);
        }
        
        string eatEndState = "Eat-End";
        float eatEndLength = GetStateAnimationLength(eatEndState);
        if (eatEndLength > 0) {
            animator.CrossFade(eatEndState, k_crossfadeDuration);
            
            float waitTime = 0f;
            while (waitTime < 0.2f) {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName(eatEndState)) {
                    break;
                }
                waitTime += Time.deltaTime;
                yield return null;
            }
            
            float elapsed = 0f;
            while (elapsed < eatEndLength) {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (!stateInfo.IsName(eatEndState) && elapsed > 0.1f) {
                    break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        animator.SetBool(isEatingHash, false);
        
        float idleLength = GetAnimationLength(locomotionClip);
        if (idleLength > 0) {
            animator.CrossFade(locomotionClip, k_crossfadeDuration);
        } else {
            animator.Play("Idle1", 0, 0f);
        }
        
        isEating = false;
    }
    
    /// <summary>
    /// Спавнит партиклы при попадании атаки в блок игрока
    /// </summary>
    private void SpawnBlockHitParticles(Vector3 playerPosition)
    {
        if (blockHitParticlesPrefab == null) return;
        
        Vector3 enemyPosition = transform.position;
        Vector3 directionToPlayer = (playerPosition - enemyPosition).normalized;
        
        GameObject player = GameObject.FindWithTag("Player");
        Transform weaponTransform = null;
        
        if (player != null)
        {
            Transform fpHands = player.transform.Find("FPHands");
            if (fpHands == null)
            {
                fpHands = FindDeepChild(player.transform, "FPHands");
            }
            
            if (fpHands != null)
            {
                weaponTransform = fpHands;
            }
            else
            {
                FPMAxe fpmAxe = player.GetComponent<FPMAxe>();
                if (fpmAxe == null)
                {
                    fpmAxe = player.GetComponentInChildren<FPMAxe>();
                }
                if (fpmAxe != null && fpmAxe.transform != null)
                {
                    weaponTransform = fpmAxe.transform;
                }
            }
        }
        
        Vector3 basePosition;
        Vector3 spawnDirection;
        
        if (weaponTransform != null)
        {
            basePosition = weaponTransform.position;
            spawnDirection = -directionToPlayer;
        }
        else
        {
            basePosition = playerPosition;
            spawnDirection = -directionToPlayer;
        }
        
        Vector3 spawnPosition = basePosition;
        if (weaponTransform != null)
        {
            spawnPosition = weaponTransform.position + weaponTransform.right * blockHitParticleOffset.x +
                          weaponTransform.up * blockHitParticleOffset.y +
                          weaponTransform.forward * blockHitParticleOffset.z;
        }
        else
        {
            spawnPosition = basePosition + Vector3.right * blockHitParticleOffset.x +
                          Vector3.up * blockHitParticleOffset.y +
                          Vector3.forward * blockHitParticleOffset.z;
        }
        
        GameObject particles = Instantiate(blockHitParticlesPrefab, spawnPosition, Quaternion.LookRotation(spawnDirection));
        
        StartCoroutine(DestroyParticlesAfterLifetime(particles));
        
        Debug.Log($"[FliffyAnimationController] Спавнены партиклы блока в позиции {spawnPosition}");
    }
    
    /// <summary>
    /// Удаляет партиклы после завершения их воспроизведения
    /// </summary>
    private IEnumerator DestroyParticlesAfterLifetime(GameObject particles)
    {
        if (particles == null) yield break;
        
        ParticleSystem ps = particles.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            ps = particles.GetComponentInChildren<ParticleSystem>();
        }
        
        float lifetime = blockHitParticlesLifetime;
        
        if (ps != null)
        {
            if (lifetime <= 0f)
            {
                lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
            }
        }
        else
        {
            if (lifetime <= 0f)
            {
                lifetime = 5f;
            }
        }
        
        yield return new WaitForSeconds(lifetime);
        
        if (particles != null)
        {
            Destroy(particles);
            Debug.Log($"[FliffyAnimationController] Партиклы блока удалены после {lifetime} сек");
        }
    }
    
    /// <summary>
    /// Рекурсивный поиск дочернего объекта по имени
    /// </summary>
    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child;
            }
            Transform found = FindDeepChild(child, name);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }
}
