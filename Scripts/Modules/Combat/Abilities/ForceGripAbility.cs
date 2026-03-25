using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FIMSpace.FProceduralAnimation;
using DRAUM.Core;
using DRAUM.Modules.Combat.Events;
using DRAUM.Modules.Combat.Animation;

namespace DRAUM.Modules.Combat.Abilities
{
    /// <summary>
    /// Способность "Force Grip":
    /// - По команде находит ближайший RagdollHandler перед игроком (поиск по ragdoll, но без включения ragdoll режима).
    /// - Включает DissolveVFX (дочерний объект зомби) и останавливает Animator на родителе (зомби).
    /// - Запускает анимацию атаки игрока.
    /// - Через 1 секунду выключает SkinnedMeshRenderer у дочернего объекта зомби.
    /// - Через 3 секунды удаляет зомби (родителя со скриптами).
    /// - Автоматически отключается после завершения действия (предотвращает спам).
    /// - Параллельно крутит анимацию FPHands: слой Items = 1, триггер "A Mulet".
    /// </summary>
    public class ForceGripAbility : MonoBehaviour
    {
        
        [Header("Ragdoll / RA2")]
        [Tooltip("Слои, по которым ищем RagdollAnimator2BoneIndicator (враги с RA2)")]
        public LayerMask hittableLayerMask;

        [Tooltip("Максимальная дистанция до цели")]
        public float maxDistance = 10f;

        [Header("FPHands / Анимация")]
        [Tooltip("Animator на FPHands")]
        public Animator fpHandsAnimator;


        [Header("VFX")]
        [Tooltip("Имя дочернего объекта DissolveVFX у зомби (если не указано, будет искаться автоматически)")]
        public string dissolveVFXChildName = "DissolveVFX";

        [Header("Ссылки")]
        [Tooltip("Камера игрока (если не назначена, возьмём Camera.main)")]
        public Transform cameraTransform;

        [Tooltip("Трансформ игрока (нужен для расчёта направления и дистанции)")]
        public Transform playerRoot;

        [Header("Отладка")]
        [HideInInspector] public bool showDebugLogs = false;

        [Header("Настройки подъёма врага")]
        [Tooltip("Высота подъёма врага вверх (в единицах Unity)")]
        [Range(0f, 20f)]
        public float liftHeight = 5f;

        [Tooltip("Время плавного подъёма врага (секунды)")]
        [Range(0.1f, 10f)]
        public float liftDuration = 2f;

        [Tooltip("Имя анимационного состояния Gripped (например, Gripped)")]
        public string grippedAnimationStateName = "Gripped";

        [Header("Настройки таймингов")]
        [Tooltip("Задержка перед включением DissolveVFX после остановки Animator (секунды)")]
        [Range(0f, 5f)]
        public float delayBeforeEnableDissolveVFX = 1f;

        [Tooltip("Задержка перед выключением SkinnedMeshRenderer после включения DissolveVFX (секунды)")]
        [Range(0f, 10f)]
        public float delayBeforeDisableSkinnedMeshRenderer = 1f;

        [Tooltip("Задержка перед удалением зомби после выключения SkinnedMeshRenderer (секунды)")]
        [Range(0f, 10f)]
        public float delayBeforeDestroyEnemy = 3f;


        private bool _modeActive = false;
        private bool _holding = false;
        private bool _isExecuting = false;

        private RagdollHandler _gripped = null;
        private GameObject _dissolveVFXObject = null;
        private Transform _enemyParentTransform = null;

        private ConsumableAnimationHandler _consumableAnimationHandler;
        
        private int _grippedAnimationHash = -1;

        private readonly Collider[] _surround = new Collider[64];

        private readonly List<Collider> _toIgnore = new List<Collider>();
        private readonly List<RagdollHandler> _detectedRagdolls = new List<RagdollHandler>();

        private void Awake()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (playerRoot == null)
                playerRoot = transform;

            _consumableAnimationHandler = FindFirstObjectByType<ConsumableAnimationHandler>();
            if (_consumableAnimationHandler == null && showDebugLogs)
                Debug.LogWarning("[ForceGripAbility] ConsumableAnimationHandler не найден!");
                
            if (!string.IsNullOrEmpty(grippedAnimationStateName))
            {
                _grippedAnimationHash = Animator.StringToHash(grippedAnimationStateName);
                if (showDebugLogs) Debug.Log($"[ForceGripAbility] Хэш анимации Gripped: {_grippedAnimationHash}");
            }

            var myCols = GetComponentsInChildren<Collider>();
            foreach (var col in myCols)
                if (!_toIgnore.Contains(col)) _toIgnore.Add(col);
        }
        
        private void OnEnable()
        {
            EventBus.Instance.Subscribe<DRAUM.Modules.Combat.Events.ActivateAbilityEvent>(OnActivateAbilityEvent);
        }

        private void OnDisable()
        {
            if (EventBus.Instance != null)
                EventBus.Instance.Unsubscribe<DRAUM.Modules.Combat.Events.ActivateAbilityEvent>(OnActivateAbilityEvent);
        }

        private void Update()
        {
            bool lmbDown = Input.GetMouseButtonDown(0);
            if (lmbDown)
            {
                if (showDebugLogs)
                    Debug.Log($"[ForceGripAbility] ЛКМ нажата. _modeActive={_modeActive}, _isExecuting={_isExecuting}, _holding={_holding}");
                else if (!_modeActive && !_isExecuting)
                    Debug.Log("[ForceGripAbility] ЛКМ нажата, но режим не активен (_modeActive=false). Используй амулет (Consumable), чтобы активировать.");
            }

            if (lmbDown && _modeActive && !_isExecuting)
                OnPrimaryPressed();

        }

        /// <summary>
        /// Включает/выключает режим способности (по X снаружи).
        /// </summary>
        public void SetModeActive(bool active)
        {
            if (showDebugLogs) Debug.Log($"[ForceGripAbility] SetModeActive({active})");

            _modeActive = active;

            if (!_modeActive)
            {
                if (_holding)
                {
                    _holding = false;
                    if (_gripped != null) _gripped.User_OverrideMusclesPower = null;
                    _gripped = null;
                }
                _isExecuting = false;
            }
        }

        [Tooltip("Имя анимационного состояния атаки (например Attack-Amulet)")]
        public string attackAnimationStateName = "Attack-Amulet";

        /// <summary>
        /// Внешний вызов с ЛКМ (PrimaryAttack).
        /// Один клик: захватывает цель, переводит в ragdoll, запускает анимацию атаки и уничтожает врага.
        /// </summary>
        public void OnPrimaryPressed()
        {
            if (showDebugLogs) Debug.Log("[ForceGripAbility] OnPrimaryPressed вызван");

            if (!_modeActive)
            {
                Debug.Log("[ForceGripAbility] Выход: способность не активна (_modeActive=false). Активируй амулет из инвентаря.");
                return;
            }
            if (_isExecuting)
            {
                if (showDebugLogs) Debug.Log("[ForceGripAbility] Выход: уже выполняется (_isExecuting=true)");
                return;
            }

            if (_holding)
            {
                if (showDebugLogs) Debug.Log("[ForceGripAbility] Выход: уже держим цель (_holding=true)");
                return;
            }

            if (TryStartGrip())
            {
                if (showDebugLogs) Debug.Log("[ForceGripAbility] TryStartGrip успешен, запускаем ExecuteForceGripSequence");
                StartCoroutine(ExecuteForceGripSequence());
            }
            else
            {
                Debug.Log("[ForceGripAbility] TryStartGrip: цель не найдена. Проверь: камера назначена, враг на слое hittableLayerMask, у врага есть RagdollAnimator2BoneIndicator.");
            }
        }

        /// <summary>
        /// Пытаемся найти подходящий RagdollHandler перед игроком и начать Force Grip.
        /// </summary>
        /// <returns>true если захват успешен, false если нет</returns>
        private bool TryStartGrip()
        {
            if (cameraTransform == null)
            {
                Debug.LogWarning("[ForceGripAbility] Нет cameraTransform, Force Grip невозможен. Назначь камеру в инспекторе.");
                return false;
            }

            var best = FindBestRagdollInFront();
            if (best == null)
            {
                if (showDebugLogs) Debug.Log("[ForceGripAbility] Подходящий Ragdoll не найден (FindBestRagdollInFront вернул null).");
                return false;
            }

            _gripped = best;


            _holding = true;
            _isExecuting = true; 

            if (showDebugLogs) Debug.Log("[ForceGripAbility] Захвачен RagdollHandler (Force Grip активен, ragdoll режим отключен)");
            return true;
        }

        /// <summary>
        /// Корутина для выполнения полной последовательности Force Grip:
        /// 1. Захват врага (без ragdoll режима)
        /// 2. Плавный подъём врага вверх на liftHeight за liftDuration секунд
        /// 3. Параллельно включаем анимацию Gripped на враге
        /// 4. Остановка Animator на родителе (зомби)
        /// 5. Через delayBeforeEnableDissolveVFX → включение DissolveVFX
        /// 6. Через delayBeforeDisableSkinnedMeshRenderer → выключение SkinnedMeshRenderer И запуск анимации атаки игрока одновременно
        /// 7. Через delayBeforeDestroyEnemy → удаление зомби
        /// Все задержки настраиваются в инспекторе.
        /// </summary>
        private IEnumerator ExecuteForceGripSequence()
        {
            if (_gripped == null) yield break;

            _enemyParentTransform = GetParentTransform();
            if (_enemyParentTransform == null)
            {
                if (showDebugLogs) Debug.LogError("[ForceGripAbility] Не удалось найти родителя, прерываем выполнение");
                yield break;
            }

            Animator enemyAnimator = _enemyParentTransform.GetComponent<Animator>();
            if (enemyAnimator == null)
            {
                enemyAnimator = _enemyParentTransform.GetComponentInChildren<Animator>();
            }

            Vector3 startPosition = _enemyParentTransform.position;
            Vector3 targetPosition = startPosition + Vector3.up * liftHeight;
            float elapsedTime = 0f;

            if (enemyAnimator != null && _grippedAnimationHash != -1)
            {
                enemyAnimator.CrossFadeInFixedTime(_grippedAnimationHash, 0.1f);
                if (showDebugLogs) Debug.Log($"[ForceGripAbility] Запущена анимация Gripped на враге");
            }
            else if (enemyAnimator != null && !string.IsNullOrEmpty(grippedAnimationStateName))
            {
                enemyAnimator.CrossFadeInFixedTime(grippedAnimationStateName, 0.1f);
                if (showDebugLogs) Debug.Log($"[ForceGripAbility] Запущена анимация Gripped на враге (через строку)");
            }

            while (elapsedTime < liftDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / liftDuration;
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                _enemyParentTransform.position = Vector3.Lerp(startPosition, targetPosition, smoothT);
                yield return null;
            }

            _enemyParentTransform.position = targetPosition;
            if (showDebugLogs) Debug.Log($"[ForceGripAbility] Враг поднят на высоту {liftHeight} за {liftDuration} сек");

            StopParentAnimator(_enemyParentTransform);
            if (showDebugLogs) Debug.Log("[ForceGripAbility] Animator остановлен после подъёма");

            yield return new WaitForSeconds(delayBeforeEnableDissolveVFX);
            
            EnableDissolveVFX(_enemyParentTransform);
            if (showDebugLogs) Debug.Log($"[ForceGripAbility] DissolveVFX включен (через {delayBeforeEnableDissolveVFX} сек после остановки Animator)");

            yield return new WaitForSeconds(delayBeforeDisableSkinnedMeshRenderer);
            
            DisableSkinnedMeshRenderer(_enemyParentTransform);
            if (showDebugLogs) Debug.Log($"[ForceGripAbility] SkinnedMeshRenderer выключен (через {delayBeforeDisableSkinnedMeshRenderer} сек после включения DissolveVFX)");

            if (fpHandsAnimator != null && !string.IsNullOrEmpty(attackAnimationStateName))
            {
                fpHandsAnimator.CrossFadeInFixedTime(attackAnimationStateName, 0.1f);
                if (showDebugLogs) Debug.Log($"[ForceGripAbility] Запущена анимация атаки одновременно с выключением SkinnedMeshRenderer");
            }

            yield return new WaitForSeconds(delayBeforeDestroyEnemy);
            
            DestroyEnemy(_enemyParentTransform);
            if (showDebugLogs) Debug.Log($"[ForceGripAbility] Зомби удалён (через {delayBeforeDestroyEnemy} сек после выключения SkinnedMeshRenderer)");
        }

        /// <summary>
        /// Получает родительский Transform (зомби со скриптами) из RagdollHandler.
        /// </summary>
        private Transform GetParentTransform()
        {
            if (_gripped == null) return null;

            GameObject ragdollRoot = _gripped.Mecanim.gameObject;
            Transform parentTransform = null;
            string foundMethod = "";


            Transform baseTransform = _gripped.GetBaseTransform();
            if (baseTransform != null)
            {
                parentTransform = baseTransform;
                foundMethod = "GetBaseTransform";
            }

            if (parentTransform == null)
            {
                FIMSpace.FProceduralAnimation.RagdollAnimator2 ragdollAnimator2 = ragdollRoot.GetComponent<FIMSpace.FProceduralAnimation.RagdollAnimator2>();
                if (ragdollAnimator2 == null && ragdollRoot.transform.parent != null)
                {
                    ragdollAnimator2 = ragdollRoot.transform.parent.GetComponent<FIMSpace.FProceduralAnimation.RagdollAnimator2>();
                }

                if (ragdollAnimator2 != null)
                {
                    parentTransform = ragdollAnimator2.transform;
                    foundMethod = "RagdollAnimator2";
                }
            }

            if (parentTransform == null && ragdollRoot.transform.parent != null)
            {
                parentTransform = ragdollRoot.transform.parent;
                foundMethod = "transform.parent";
            }

            if (parentTransform == null)
            {
                Transform current = ragdollRoot.transform;
                int depth = 0;
                while (current != null && depth < 10)
                {
                    FIMSpace.FProceduralAnimation.RagdollAnimator2 ra2 = current.GetComponent<FIMSpace.FProceduralAnimation.RagdollAnimator2>();
                    if (ra2 != null)
                    {
                        parentTransform = current;
                        foundMethod = "RagdollAnimator2 в иерархии";
                        break;
                    }
                    current = current.parent;
                    depth++;
                }
            }

            if (parentTransform != null && showDebugLogs)
            {
                Debug.Log($"[ForceGripAbility] Найден родитель: {parentTransform.name} (метод: {foundMethod})");
            }

            return parentTransform;
        }

        /// <summary>
        /// Включает DissolveVFX, который должен быть дочерним объектом зомби (родителя).
        /// Настраивает SkinnedMeshToMesh для передачи меша в VFX.
        /// </summary>
        private void EnableDissolveVFX(Transform parentTransform)
        {
            if (parentTransform == null)
            {
                if (showDebugLogs) Debug.LogWarning("[ForceGripAbility] parentTransform null, не можем включить DissolveVFX");
                return;
            }

            _dissolveVFXObject = null;

            if (!string.IsNullOrEmpty(dissolveVFXChildName))
            {
                Transform child = parentTransform.Find(dissolveVFXChildName);
                if (child != null)
                {
                    _dissolveVFXObject = child.gameObject;
                    if (showDebugLogs) Debug.Log($"[ForceGripAbility] Найден DissolveVFX по имени '{dissolveVFXChildName}': {_dissolveVFXObject.name}");
                }
            }

            if (_dissolveVFXObject == null)
            {
                UnityEngine.VFX.VisualEffect[] vfxComponents = parentTransform.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>(true);
                if (vfxComponents.Length > 0)
                {
                    _dissolveVFXObject = vfxComponents[0].transform.root == parentTransform 
                        ? vfxComponents[0].gameObject 
                        : vfxComponents[0].transform.parent.gameObject;
                    
                    if (showDebugLogs) Debug.Log($"[ForceGripAbility] Найден DissolveVFX через поиск VisualEffect: {_dissolveVFXObject.name}");
                }
            }

            if (_dissolveVFXObject == null)
            {
                foreach (Transform child in parentTransform)
                {
                    if (child.name.Contains("Dissolve") || child.name.Contains("VFX"))
                    {
                        _dissolveVFXObject = child.gameObject;
                        if (showDebugLogs) Debug.Log($"[ForceGripAbility] Найден DissolveVFX по имени (содержит 'Dissolve' или 'VFX'): {_dissolveVFXObject.name}");
                        break;
                    }
                }
            }

            if (_dissolveVFXObject == null)
            {
                if (showDebugLogs) Debug.LogError($"[ForceGripAbility] DissolveVFX не найден как дочерний объект {parentTransform.name}!");
                return;
            }


            _dissolveVFXObject.SetActive(true);
            if (showDebugLogs) Debug.Log($"[ForceGripAbility] DissolveVFX включен: {_dissolveVFXObject.name}");


            SkinnedMeshRenderer skinnedMeshRenderer = parentTransform.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer == null)
            {
                if (showDebugLogs) Debug.LogWarning("[ForceGripAbility] SkinnedMeshRenderer не найден на зомби");
                return;
            }


            SkinnedMeshToMesh skinnedMeshToMesh = parentTransform.GetComponent<SkinnedMeshToMesh>();
            if (skinnedMeshToMesh == null)
            {
                skinnedMeshToMesh = parentTransform.gameObject.AddComponent<SkinnedMeshToMesh>();
            }


            UnityEngine.VFX.VisualEffect vfxGraph = _dissolveVFXObject.GetComponentInChildren<UnityEngine.VFX.VisualEffect>();
            if (vfxGraph == null)
            {
                if (showDebugLogs) Debug.LogWarning("[ForceGripAbility] VisualEffect не найден в дочерних объектах DissolveVFX");
                return;
            }


            skinnedMeshToMesh.skinnedMesh = skinnedMeshRenderer;
            skinnedMeshToMesh.VFXGraph = vfxGraph;
            skinnedMeshToMesh.refreshRate = 0.05f;

            if (showDebugLogs) Debug.Log($"[ForceGripAbility] Настроен SkinnedMeshToMesh: skinnedMesh={skinnedMeshRenderer.name}, VFXGraph={vfxGraph.name}");
        }

        /// <summary>
        /// Останавливает Animator на родителе (зомби).
        /// </summary>
        private void StopParentAnimator(Transform parentTransform)
        {
            if (parentTransform == null) return;

            Animator animator = parentTransform.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
                if (showDebugLogs) Debug.Log($"[ForceGripAbility] Animator остановлен на {parentTransform.name}");
            }
            else
            {
                if (showDebugLogs) Debug.LogWarning($"[ForceGripAbility] Animator не найден на {parentTransform.name}");
            }
        }

        /// <summary>
        /// Выключает SkinnedMeshRenderer у дочернего объекта зомби.
        /// </summary>
        private void DisableSkinnedMeshRenderer(Transform parentTransform)
        {
            if (parentTransform == null) return;

            SkinnedMeshRenderer skinnedMeshRenderer = parentTransform.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                skinnedMeshRenderer.enabled = false;
                if (showDebugLogs) Debug.Log($"[ForceGripAbility] SkinnedMeshRenderer выключен: {skinnedMeshRenderer.name}");
            }
            else
            {
                if (showDebugLogs) Debug.LogWarning($"[ForceGripAbility] SkinnedMeshRenderer не найден на {parentTransform.name}");
            }
        }

        /// <summary>
        /// Удаляет зомби (родителя со скриптами).
        /// </summary>
        private void DestroyEnemy(Transform parentTransform)
        {
            if (parentTransform == null) return;

            if (showDebugLogs) Debug.Log($"[ForceGripAbility] Удаляем зомби: {parentTransform.name}");
            Destroy(parentTransform.gameObject);
        }
        

        /// <summary>
        /// Вызывается из Animation Event в конце анимации атаки.
        /// Сбрасывает состояние способности. Зомби удаляется в корутине через 3 секунды.
        /// </summary>
        public void OnAttackAnimationComplete()
        {

            _holding = false;
            _isExecuting = false;

            if (_consumableAnimationHandler != null)
            {
                _consumableAnimationHandler.ResetConsumableLayer();
                if (showDebugLogs) Debug.Log("[ForceGripAbility] Слой Consumable сброшен через ConsumableAnimationHandler");
            }
            EventBus.Instance.Publish(new AbilityActionCompleteEvent { AbilityName = "ForceGrip" });

            EventBus.Instance.Publish(new ActivateAbilityEvent 
            { 
                AbilityName = "ForceGrip", 
                Activate = false 
            });

            if (showDebugLogs) Debug.Log("[ForceGripAbility] Способность ForceGrip отключена после завершения анимации. Зомби будет удалён через 3 секунды из корутины.");
        }



        public void OnPrimaryReleased()
        {
        }

        /// <summary>
        /// Находит RagdollHandler цели, на которую смотрит перекрестье камеры (центр экрана).
        /// Использует Raycast от камеры по направлению взгляда.
        /// </summary>
        private RagdollHandler FindBestRagdollInFront()
        {
            if (cameraTransform == null) return null;

            Vector3 origin = cameraTransform.position;
            Vector3 direction = cameraTransform.forward;

            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, maxDistance, hittableLayerMask))
            {
                if (_toIgnore.Contains(hit.collider))
                {
                    if (showDebugLogs) Debug.Log("[ForceGripAbility] Raycast попал в коллайдер игрока, игнорируем");
                    return null;
                }

                var indicator = hit.collider.GetComponent<RagdollAnimator2BoneIndicator>();
                if (indicator == null)
                    indicator = hit.collider.GetComponentInChildren<RagdollAnimator2BoneIndicator>();
                if (indicator == null)
                    indicator = hit.collider.GetComponentInParent<RagdollAnimator2BoneIndicator>();

                if (indicator == null)
                {
                    Debug.Log($"[ForceGripAbility] Raycast попал в '{hit.collider.name}' (слой: {LayerMask.LayerToName(hit.collider.gameObject.layer)}), но RagdollAnimator2BoneIndicator не найден на объекте/родителях.");
                    return null;
                }

                var handler = indicator.ParentHandler;
                if (handler == null)
                {
                    Debug.Log("[ForceGripAbility] RagdollAnimator2BoneIndicator найден, но ParentHandler == null. Назначь ParentHandler в инспекторе индикатора.");
                    return null;
                }

                if (showDebugLogs) Debug.Log($"[ForceGripAbility] Найдена цель через Raycast: {hit.collider.name}, расстояние: {hit.distance:F2}");
                return handler;
            }

            if (showDebugLogs) Debug.Log($"[ForceGripAbility] Raycast не попал ни в один объект (дистанция {maxDistance}, слои: {hittableLayerMask.value}). Проверь hittableLayerMask и что враг перед камерой.");
            return null;
        }

        private void OnActivateAbilityEvent(DRAUM.Modules.Combat.Events.ActivateAbilityEvent evt)
        {
            if (showDebugLogs) Debug.Log($"[ForceGripAbility] Получено событие ActivateAbilityEvent: AbilityName={evt.AbilityName}, Activate={evt.Activate}");
            
            if (evt.AbilityName == "ForceGrip")
            {
                if (showDebugLogs) Debug.Log($"[ForceGripAbility] Событие для ForceGrip! Вызываем SetModeActive({evt.Activate})");
                SetModeActive(evt.Activate);
            }
            else
            {
                if (showDebugLogs) Debug.Log($"[ForceGripAbility] Событие не для ForceGrip (получено: {evt.AbilityName}), игнорируем");
            }
        }
    }
}

