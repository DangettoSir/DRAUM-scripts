using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using DRAUM.Modules.Player;
using DRAUM.Core;
using DRAUM.Modules.Combat.Events;
using FIMSpace.FProceduralAnimation;

namespace DRAUM.Modules.Combat.Animation
{
    /// <summary>
    /// Контроллер добивания (Brutality) - управляет анимациями добивания врагов.
    /// Блокирует камеру и движение игрока, воспроизводит анимации на FPHands и враге.
    /// </summary>
    public class BrutalityController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Animator на FPHands (для слоя Brutality)")]
        public Animator fpHandsAnimator;
        
        [Tooltip("PlayerModule для блокировки движения и камеры")]
        public PlayerModule playerModule;
        
        [Tooltip("Имя слоя Brutality в Animator Controller FPHands")]
        public string brutalityLayerName = "Brutality";
        
        [Tooltip("Имя анимационного состояния BRUTAL-ARMS на слое Brutality")]
        public string brutalArmsStateName = "BRUTAL-ARMS";
        
        [Header("Debug")]
    private bool showDebugLogs = false;
        
        private int _brutalityLayerIndex = -1;
        private bool _isExecutingBrutality = false;
        private EnemyEntity _targetEnemy = null;
        private Animator _enemyAnimator = null;
        
        /// <summary>
        /// Проверяет, выполняется ли сейчас добивание
        /// </summary>
        public bool IsExecutingBrutality => _isExecutingBrutality;
        
        private void Awake()
        {
            if (fpHandsAnimator == null)
            {
                fpHandsAnimator = GetComponent<Animator>();
                if (fpHandsAnimator == null)
                    fpHandsAnimator = GetComponentInChildren<Animator>();
            }
            
            if (playerModule == null)
            {
                playerModule = FindFirstObjectByType<PlayerModule>();
            }
            
            if (fpHandsAnimator != null && !string.IsNullOrEmpty(brutalityLayerName))
            {
                _brutalityLayerIndex = fpHandsAnimator.GetLayerIndex(brutalityLayerName);
                if (_brutalityLayerIndex == -1 && showDebugLogs)
                    Debug.LogWarning($"[BrutalityController] Слой '{brutalityLayerName}' не найден в FPHands Animator!");
            }
        }
        
        /// <summary>
        /// Запускает добивание врага
        /// </summary>
        /// <param name="enemy">Враг для добивания (должен иметь EnemyEntity и Animator)</param>
        public void ExecuteBrutality(EnemyEntity enemy)
        {
            if (_isExecutingBrutality)
            {
                if (showDebugLogs) Debug.LogWarning("[BrutalityController] Уже выполняется добивание, игнорируем");
                return;
            }
            
            if (enemy == null)
            {
                if (showDebugLogs) Debug.LogError("[BrutalityController] EnemyEntity null!");
                return;
            }
            
            if (fpHandsAnimator == null || _brutalityLayerIndex == -1)
            {
                if (showDebugLogs) Debug.LogError("[BrutalityController] FPHands Animator или слой Brutality не настроены!");
                return;
            }
            
            _enemyAnimator = enemy.GetComponent<Animator>();
            if (_enemyAnimator == null)
            {
                _enemyAnimator = enemy.GetComponentInChildren<Animator>();
            }
            
            if (_enemyAnimator == null)
            {
                if (showDebugLogs) Debug.LogError("[BrutalityController] Animator не найден на враге!");
                return;
            }
            
            _targetEnemy = enemy;
            _isExecutingBrutality = true;
            
            StartCoroutine(ExecuteBrutalitySequence());
        }
        
        /// <summary>
        /// Корутина для выполнения добивания:
        /// 1. Блокируем камеру и движение
        /// 2. Включаем слой Brutality и запускаем анимацию BRUTAL-ARMS на FPHands
        /// 3. Одновременно запускаем анимацию BRUTAL-ZOMBIE на враге
        /// 4. Ждём завершения анимации
        /// 5. Разблокируем камеру и движение
        /// </summary>
        private IEnumerator ExecuteBrutalitySequence()
        {
            if (showDebugLogs) Debug.Log("[BrutalityController] Начало добивания");
            
            if (playerModule != null)
            {
                playerModule.LockPlayer(lockMovement: true, lockCamera: true);
                if (showDebugLogs) Debug.Log("[BrutalityController] Движение и камера заблокированы");
            }
            
            fpHandsAnimator.SetLayerWeight(_brutalityLayerIndex, 1f);
            fpHandsAnimator.CrossFadeInFixedTime(brutalArmsStateName, 0.1f, _brutalityLayerIndex);
            
            if (showDebugLogs) Debug.Log($"[BrutalityController] Запущена анимация {brutalArmsStateName} на FPHands");
            
            if (_enemyAnimator != null)
            {
                _enemyAnimator.CrossFadeInFixedTime("BRUTAL-ZOMBIE", 0.1f);
                if (showDebugLogs) Debug.Log("[BrutalityController] Запущена анимация BRUTAL-ZOMBIE на враге");
            }
            
            float animationLength = GetAnimationLength(fpHandsAnimator, brutalArmsStateName, _brutalityLayerIndex);
            if (animationLength <= 0f)
            {
                animationLength = 3f;
            }
            
            yield return new WaitForSeconds(animationLength);
            
            if (playerModule != null)
            {
                playerModule.UnlockPlayer();
                if (showDebugLogs) Debug.Log("[BrutalityController] Движение и камера разблокированы");
            }
            
            fpHandsAnimator.SetLayerWeight(_brutalityLayerIndex, 0f);
            
            if (_targetEnemy != null && _targetEnemy.IsAlive)
            {
                // Урон добивания маршрутизируем через CombatModule (единый damage pipeline).
                float killDamage = _targetEnemy.CurrentHealth + 100f;
                EventBus.Instance?.Publish(new EnemyDamageRequestEvent
                {
                    TargetEnemy = _targetEnemy,
                    BaseDamage = killDamage,
                    BodyPart = BodyPart.Unknown,
                    HitPoint = _targetEnemy.transform.position,
                    Reply = null
                });
                if (showDebugLogs) Debug.Log("[BrutalityController] Враг убит после добивания");
            }
            
            if (_targetEnemy != null)
            {
                DisableEnemyComponents(_targetEnemy.gameObject);
            }
            
            _isExecutingBrutality = false;
            _targetEnemy = null;
            _enemyAnimator = null;
            
            if (showDebugLogs) Debug.Log("[BrutalityController] Добивание завершено");
        }
        
        /// <summary>
        /// Отключает все компоненты врага кроме RagdollAnimator после добивания
        /// </summary>
        private void DisableEnemyComponents(GameObject enemyObject)
        {
            if (enemyObject == null) return;
            
            var skinGoreRenderer = enemyObject.GetComponent<SkinGoreRenderer>();
            if (skinGoreRenderer == null) skinGoreRenderer = enemyObject.GetComponentInChildren<SkinGoreRenderer>();
            if (skinGoreRenderer != null)
            {
                skinGoreRenderer.enabled = false;
                if (showDebugLogs) Debug.Log($"[BrutalityController] Отключен SkinGoreRenderer на {enemyObject.name}");
            }
            
            
            var enemyEntity = enemyObject.GetComponent<EnemyEntity>();
            if (enemyEntity == null) enemyEntity = enemyObject.GetComponentInChildren<EnemyEntity>();
            if (enemyEntity != null)
            {
                enemyEntity.enabled = false;
                if (showDebugLogs) Debug.Log($"[BrutalityController] Отключен EnemyEntity на {enemyObject.name}");
            }
            
            var legsAnimator = enemyObject.GetComponent<LegsAnimator>();
            if (legsAnimator == null) legsAnimator = enemyObject.GetComponentInChildren<LegsAnimator>();
            if (legsAnimator != null)
            {
                legsAnimator.enabled = false;
                if (showDebugLogs) Debug.Log($"[BrutalityController] Отключен LegsAnimator на {enemyObject.name}");
            }
            
            var navMeshAgent = enemyObject.GetComponent<NavMeshAgent>();
            if (navMeshAgent == null) navMeshAgent = enemyObject.GetComponentInChildren<NavMeshAgent>();
            if (navMeshAgent != null)
            {
                navMeshAgent.enabled = false;
                if (showDebugLogs) Debug.Log($"[BrutalityController] Отключен NavMeshAgent на {enemyObject.name}");
            }
            
            var goapAgent = enemyObject.GetComponent<GoapAgent>();
            if (goapAgent == null) goapAgent = enemyObject.GetComponentInChildren<GoapAgent>();
            if (goapAgent != null)
            {
                goapAgent.enabled = false;
                if (showDebugLogs) Debug.Log($"[BrutalityController] Отключен GoapAgent на {enemyObject.name}");
            }
            
            var fliffyAnimationController = enemyObject.GetComponent<FliffyAnimationController>();
            if (fliffyAnimationController == null) fliffyAnimationController = enemyObject.GetComponentInChildren<FliffyAnimationController>();
            if (fliffyAnimationController != null)
            {
                fliffyAnimationController.enabled = false;
                if (showDebugLogs) Debug.Log($"[BrutalityController] Отключен FliffyAnimationController на {enemyObject.name}");
            }
            
            if (showDebugLogs) Debug.Log($"[BrutalityController] Компоненты врага отключены (кроме RagdollAnimator)");
        }
        
        /// <summary>
        /// Получает длину анимации по имени состояния и слою
        /// </summary>
        private float GetAnimationLength(Animator animator, string stateName, int layerIndex)
        {
            if (animator == null || string.IsNullOrEmpty(stateName)) return 0f;
            
            int stateHash = Animator.StringToHash(stateName);
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
            
            if (stateInfo.shortNameHash == stateHash)
            {
                return stateInfo.length;
            }
            
            if (animator.runtimeAnimatorController != null)
            {
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                {
                    if (clip.name == stateName)
                    {
                        return clip.length;
                    }
                }
            }
            
            return 0f;
        }
    }
}
