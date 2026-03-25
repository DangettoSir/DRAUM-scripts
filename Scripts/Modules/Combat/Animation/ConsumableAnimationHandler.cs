using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DRAUM.Modules.Player;
using DRAUM.Modules.Player.Effects;
using DRAUM.Modules.Inventory;
using DRAUM.Core;
using DRAUM.Modules.Combat.Events;
using DRAUM.Modules.Combat.Abilities;

namespace DRAUM.Modules.Combat.Animation
{
    /// <summary>
    /// Управляет анимацией потребления расходников (яблоко и т.д.) на FPHands.
    /// Спавнит префаб в jointApple, управляет слоем Consumable, обрабатывает Animation Events.
    /// </summary>
    public class ConsumableAnimationHandler : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Animator на FPHands (слой Consumable должен быть настроен)")]
        public Animator fpHandsAnimator;
        
        [Tooltip("Transform для спавна префаба расходника (например jointApple)")]
        public Transform consumableJoint;
        
        [Tooltip("Имя слоя Consumable в Animator Controller")]
        public string consumableLayerName = "Consumable";
        
        [Header("Weapon Oil Settings")]
        [Tooltip("Transform для объекта stick (путь: JointStick -> JointStick2 -> stick). Если не назначен, будет найден автоматически.")]
        public Transform stickTransform;
        
        [Header("Animation Settings")]
        [Tooltip("Скорость перехода weight слоя Consumable (0→1 при старте, 1→0 при завершении)")]
        [Range(1f, 20f)]
        public float layerBlendSpeed = 8f;

        [Header("Debug")]
        [HideInInspector] public bool showDebugLogs = false;

        private int _consumableLayerIndex = -1;
        private float _targetLayerWeight = 0f;
        private float _currentLayerWeight = 0f;
        private GameObject _spawnedConsumable;
        private bool _isConsuming = false;
        private Item _currentConsumableItem;
        private PlayerEntity _playerEntity;
        private GameObject _stickObject; // Объект stick для WeaponOil
        private List<(Renderer r, Material shared)> _savedStickMaterials;
        private Coroutine _revertStickMaterialCoroutine;
        private const float WeaponOilDurationSeconds = 60f;

        public string CurrentAnimationStateName { get; private set; }

        /// <summary> Проверяет, идёт ли сейчас потребление (для AC_Camera). </summary>
        public bool IsConsuming() => _isConsuming;

        /// <summary> true, пока на палке действует масло (1 минута после употребления WeaponOil1). </summary>
        public bool IsWeaponOilActive { get; private set; }
        
        /// <summary>
        /// Публичный метод для сброса слоя Consumable извне (например, из ForceGripAbility).
        /// Переключает на EmptyState и устанавливает weight в 0.
        /// </summary>
        public void ResetConsumableLayer()
        {
            if (fpHandsAnimator == null || _consumableLayerIndex < 0) return;
            
            if (showDebugLogs) Debug.Log("[ConsumableAnimationHandler] ResetConsumableLayer вызван извне");
            
            string emptyStateName = "EmptyState";
            if (!string.IsNullOrEmpty(emptyStateName))
            {
                fpHandsAnimator.CrossFadeInFixedTime(emptyStateName, 0.1f, _consumableLayerIndex);
                if (showDebugLogs) Debug.Log($"[ConsumableAnimationHandler] Переключен слой Consumable на {emptyStateName}");
            }
            
            _targetLayerWeight = 0f;
            
            _isConsuming = false;
            
            _currentConsumableItem = null;
            if (_spawnedConsumable != null)
            {
                Destroy(_spawnedConsumable);
                _spawnedConsumable = null;
            }
        }

        private void OnEnable()
        {
            EventBus.Instance.Subscribe<AbilityActionCompleteEvent>(OnAbilityActionComplete);
            EventBus.Instance.Subscribe<ActivateAbilityEvent>(OnActivateAbilityEvent);
        }

        private void OnDisable()
        {
            if (EventBus.Instance == null) return;
            EventBus.Instance.Unsubscribe<AbilityActionCompleteEvent>(OnAbilityActionComplete);
            EventBus.Instance.Unsubscribe<ActivateAbilityEvent>(OnActivateAbilityEvent);
        }

        private void Awake()
        {
            if (fpHandsAnimator == null) fpHandsAnimator = GetComponent<Animator>();
            if (fpHandsAnimator == null) fpHandsAnimator = GetComponentInChildren<Animator>();
            if (_playerEntity == null) _playerEntity = FindFirstObjectByType<PlayerEntity>();

            if (fpHandsAnimator != null && !string.IsNullOrEmpty(consumableLayerName))
            {
                _consumableLayerIndex = fpHandsAnimator.GetLayerIndex(consumableLayerName);
                if (_consumableLayerIndex == -1 && showDebugLogs)
                    Debug.LogWarning($"[ConsumableAnimationHandler] Слой '{consumableLayerName}' не найден в Animator!");
            }
            
            if (stickTransform == null)
            {
                FindStickObject();
            }
            else
            {
                _stickObject = stickTransform.gameObject;
            }
        }
        
        /// <summary>
        /// Находит объект stick по пути JointStick -> JointStick2 -> stick
        /// </summary>
        private void FindStickObject()
        {
            Transform fpHandsTransform = fpHandsAnimator != null ? fpHandsAnimator.transform : transform;
            
            Transform jointStick = fpHandsTransform.Find("JointStick");
            
            if (jointStick == null)
            {
                jointStick = FindChildRecursive(fpHandsTransform, "JointStick");
            }
            
            if (jointStick != null)
            {
                Transform jointStick2 = jointStick.Find("JointStick2");
                if (jointStick2 != null)
                {
                    Transform stick = jointStick2.Find("stick");
                    if (stick != null)
                    {
                        _stickObject = stick.gameObject;
                        stickTransform = stick;
                        if (showDebugLogs)
                            Debug.Log($"[ConsumableAnimationHandler] Найден объект stick: {_stickObject.name}");
                    }
                    else if (showDebugLogs)
                    {
                        Debug.LogWarning("[ConsumableAnimationHandler] Не найден объект 'stick' в JointStick2");
                    }
                }
                else if (showDebugLogs)
                {
                    Debug.LogWarning("[ConsumableAnimationHandler] Не найден объект 'JointStick2' в JointStick");
                }
            }
            else if (showDebugLogs)
            {
                Debug.LogWarning("[ConsumableAnimationHandler] Не найден объект 'JointStick' в FPHands");
            }
        }
        
        /// <summary>
        /// Рекурсивно ищет дочерний объект по имени
        /// </summary>
        private Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    return child;
                }
                
                Transform found = FindChildRecursive(child, name);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private void Update()
        {
            if (_consumableLayerIndex == -1) return;

            _currentLayerWeight = Mathf.MoveTowards(_currentLayerWeight, _targetLayerWeight, layerBlendSpeed * Time.deltaTime);
            fpHandsAnimator.SetLayerWeight(_consumableLayerIndex, _currentLayerWeight);
        }

        /// <summary>
        /// Запускает анимацию потребления предмета. Закрывает инвентарь, спавнит префаб, включает слой, ставит триггер.
        /// </summary>
        public void StartConsume(Item item)
        {
            if (item == null || item.data == null)
            {
                if (showDebugLogs) Debug.LogWarning("[ConsumableAnimationHandler] StartConsume: item или data null!");
                return;
            }

            if (_isConsuming)
            {
                if (showDebugLogs) Debug.LogWarning("[ConsumableAnimationHandler] Уже идёт потребление, игнорируем.");
                return;
            }

            if (fpHandsAnimator == null || _consumableLayerIndex == -1)
            {
                if (showDebugLogs) Debug.LogWarning("[ConsumableAnimationHandler] Animator или слой Consumable не настроены!");
                return;
            }

            _currentConsumableItem = item;

            if (_currentConsumableItem != null)
            {
                global::Inventory inventory = _currentConsumableItem.inventoryGrid?.inventory;
                if (inventory != null)
                {
                    inventory.RemoveItem(_currentConsumableItem);
                    if (showDebugLogs) Debug.Log($"[ConsumableAnimationHandler] Предмет {_currentConsumableItem.data.name} удалён из инвентаря (при StartConsume)");
                }
            }

            CloseInventory();

            if (item.data.consumablePrefab != null && consumableJoint != null)
            {
                _spawnedConsumable = Instantiate(item.data.consumablePrefab, consumableJoint);
                _spawnedConsumable.transform.localPosition = item.data.consumablePrefab.transform.localPosition;
                _spawnedConsumable.transform.localRotation = item.data.consumablePrefab.transform.localRotation;
                _spawnedConsumable.transform.localScale = item.data.consumablePrefab.transform.localScale;
                
                string originalName = _spawnedConsumable.name;
                if (_spawnedConsumable.name.EndsWith("(Clone)"))
                {
                    _spawnedConsumable.name = originalName.Replace("(Clone)", "");
                    if (showDebugLogs) Debug.Log($"[ConsumableAnimationHandler] Переименован клон: {originalName} → {_spawnedConsumable.name} (для работы blendshapes в Animator)");
                }
                
                SkinnedMeshRenderer skinnedMesh = _spawnedConsumable.GetComponentInChildren<SkinnedMeshRenderer>();
                if (skinnedMesh != null && showDebugLogs)
                {
                    Debug.Log($"[ConsumableAnimationHandler] Найден SkinnedMeshRenderer на {skinnedMesh.gameObject.name}, sharedMesh: {skinnedMesh.sharedMesh?.name ?? "NULL"}");
                }
                
                if (showDebugLogs) Debug.Log($"[ConsumableAnimationHandler] Спавнен префаб {item.data.consumablePrefab.name} в {consumableJoint.name} со scale {_spawnedConsumable.transform.localScale}");
            }

            _isConsuming = true;
            _targetLayerWeight = 1f;
            _currentLayerWeight = 1f;

            string stateName = item.data.consumableAnimationStateName;
            if (string.IsNullOrEmpty(stateName))
            {
                stateName = "IsConsuming";
            }
            CurrentAnimationStateName = stateName;

            fpHandsAnimator.SetLayerWeight(_consumableLayerIndex, 1f);
            
            fpHandsAnimator.CrossFadeInFixedTime(stateName, 0f, _consumableLayerIndex, 0f);

            if (showDebugLogs) Debug.Log($"[ConsumableAnimationHandler] Запущена анимация потребления: {item.data.name} (State: {stateName}), weight слоя установлен в 1");
            
            if (item.data.name == "WeaponOil1" && stateName == "FLASK-ANIM")
            {
                EnableStick();
            }
        }

        /// <summary>
        /// Вызывается из Animation Event в анимации потребления — добавляет эффект на игрока.
        /// </summary>
        public void OnConsumeAnimationEvent()
        {
            if (_currentConsumableItem?.data == null) return;

            if (!string.IsNullOrEmpty(_currentConsumableItem.data.activatedAbilityEventName))
            {
                var abilityEvent = new DRAUM.Modules.Combat.Events.ActivateAbilityEvent
                {
                    AbilityName = _currentConsumableItem.data.activatedAbilityEventName,
                    Activate = true
                };

                if (EventBus.Instance != null)
                {
                    EventBus.Instance.Publish(abilityEvent);
                    if (showDebugLogs) Debug.Log($"[ConsumableAnimationHandler] Опубликовано событие активации способности: {_currentConsumableItem.data.activatedAbilityEventName} (Activate=true)");
                }
                else if (showDebugLogs)
                    Debug.LogWarning("[ConsumableAnimationHandler] EventBus.Instance == null, событие активации способности не отправлено. Убедись, что EventBus в сцене.");

                if (_currentConsumableItem.data.activatedAbilityEventName == "ForceGrip")
                {
                    var forceGrip = FindFirstObjectByType<ForceGripAbility>();
                    if (forceGrip != null)
                    {
                        forceGrip.SetModeActive(true);
                        if (showDebugLogs) Debug.Log("[ConsumableAnimationHandler] ForceGrip активирован напрямую (SetModeActive(true)).");
                    }
                }
            }
            else
            {
                if (showDebugLogs) Debug.LogWarning($"[ConsumableAnimationHandler] activatedAbilityEventName пустой для предмета {_currentConsumableItem.data.name}");
            }

            if (_playerEntity != null && _currentConsumableItem.data.consumableEffect != null)
            {
                PlayerEffect effect = _currentConsumableItem.data.consumableEffect.CreateEffect();
                _playerEntity.AddEffect(effect);
                if (showDebugLogs)
                    Debug.Log($"[ConsumableAnimationHandler] Эффект {effect.DisplayName} добавлен на {effect.RemainingTime:F0} сек");
            }
        }

        /// <summary>
        /// Вызывается из Animation Event в конце анимации — завершает потребление, убирает префаб, сбрасывает слой.
        /// </summary>
        public void OnConsumeAnimationComplete()
        {
            if (_currentConsumableItem?.data?.name == "WeaponOil1" && CurrentAnimationStateName == "FLASK-ANIM")
            {
                ApplyWeaponOilMaterial(_currentConsumableItem.data);
                DisableStick();
            }
            

            if (!_isConsuming) return;

            _isConsuming = false;
            _targetLayerWeight = 0f;

            if (_spawnedConsumable != null)
            {
                Destroy(_spawnedConsumable);
                _spawnedConsumable = null;
                if (showDebugLogs) Debug.Log("[ConsumableAnimationHandler] Префаб удалён");
            }

            if (_currentConsumableItem != null)
            {
                global::Inventory inventory = _currentConsumableItem.inventoryGrid?.inventory;
                if (inventory != null)
                {
                    inventory.RemoveItem(_currentConsumableItem);
                    if (showDebugLogs) Debug.Log($"[ConsumableAnimationHandler] Предмет {_currentConsumableItem.data.name} удалён из инвентаря");
                }
            }

            _currentConsumableItem = null;

            if (showDebugLogs) Debug.Log("[ConsumableAnimationHandler] Потребление завершено");
        }

        private void OnAbilityActionComplete(AbilityActionCompleteEvent evt)
        {
            if (!_isConsuming || _currentConsumableItem == null) return;

            if (evt.AbilityName == _currentConsumableItem.data.activatedAbilityEventName)
            {
                if (showDebugLogs) Debug.Log($"[ConsumableAnimationHandler] Получено событие завершения способности: {evt.AbilityName}. Завершаем потребление после атаки.");
                
                ResetConsumableLayerAfterAttack();
            }
        }
        
        /// <summary>
        /// Сбрасывает слой Consumable после завершения атаки (Attack-Amulet).
        /// Вызывается после завершения способности, когда анимация Attack-Amulet закончилась.
        /// </summary>
        private void ResetConsumableLayerAfterAttack()
        {
            if (fpHandsAnimator == null || _consumableLayerIndex < 0) return;
            
            if (showDebugLogs) Debug.Log("[ConsumableAnimationHandler] Сбрасываем слой Consumable после завершения атаки.");
            
            string emptyStateName = "EmptyState";
            if (!string.IsNullOrEmpty(emptyStateName))
            {
                fpHandsAnimator.CrossFadeInFixedTime(emptyStateName, 0.1f, _consumableLayerIndex);
                if (showDebugLogs) Debug.Log($"[ConsumableAnimationHandler] Переключен слой Consumable на {emptyStateName}");
            }
            
            _targetLayerWeight = 0f;
            
            _isConsuming = false;

            _currentConsumableItem = null;
            
            if (_spawnedConsumable != null)
            {
                Destroy(_spawnedConsumable);
                _spawnedConsumable = null;
                if (showDebugLogs) Debug.Log("[ConsumableAnimationHandler] Префаб удалён после завершения атаки");
            }
        }

        /// <summary>
        /// Обрабатывает события активации/деактивации способностей.
        /// Если способность ForceGrip деактивируется, сбрасываем состояние Consumable слоя.
        /// </summary>
        private void OnActivateAbilityEvent(ActivateAbilityEvent evt)
        {
            if (evt.AbilityName == "ForceGrip" && !evt.Activate && _isConsuming)
            {
                if (showDebugLogs) Debug.Log("[ConsumableAnimationHandler] ForceGrip деактивирован, сбрасываем состояние Consumable слоя.");
                ResetConsumableLayer();
            }
        }

        private void CloseInventory()
        {
            var backpackStateMachine = FindFirstObjectByType<BackpackStateMachine>();
            
            if (backpackStateMachine != null && backpackStateMachine.inventoryCanvas != null)
            {
                Canvas canvas = backpackStateMachine.inventoryCanvas;
                global::Inventory inventory = canvas.GetComponentInChildren<global::Inventory>();
                if (inventory != null && inventory.selectedItem != null)
                {
                    inventory.CancelItemMove();
                    if (showDebugLogs) Debug.Log("[ConsumableAnimationHandler] Отменено перемещение выбранного предмета");
                }
            }

            var inventoryCameraController = FindFirstObjectByType<InventoryCameraController>();
            if (inventoryCameraController != null)
            {
                inventoryCameraController.Deactivate();
                if (showDebugLogs) Debug.Log("[ConsumableAnimationHandler] InventoryCameraController деактивирован");
            }

            if (backpackStateMachine != null)
            {
                backpackStateMachine.CloseBackpack();
                if (backpackStateMachine.inventoryCanvas != null && backpackStateMachine.inventoryCanvas.gameObject.activeSelf)
                {
                    backpackStateMachine.inventoryCanvas.gameObject.SetActive(false);
                    if (showDebugLogs) Debug.Log("[ConsumableAnimationHandler] Canvas принудительно скрыт");
                }
                if (showDebugLogs) Debug.Log("[ConsumableAnimationHandler] Инвентарь закрыт через BackpackStateMachine");
            }
            else
            {
                var inventoryModule = FindFirstObjectByType<InventoryModule>();
                if (inventoryModule != null)
                {
                    inventoryModule.CloseInventory();
                    if (showDebugLogs) Debug.Log("[ConsumableAnimationHandler] Инвентарь закрыт через InventoryModule");
                }
            }
        }
        
        /// <summary>
        /// Включает объект stick (для WeaponOil1)
        /// </summary>
        private void EnableStick()
        {
            if (_stickObject == null)
            {
                if (showDebugLogs) Debug.LogWarning("[ConsumableAnimationHandler] Объект stick не найден, не можем включить");
                return;
            }
            
            _stickObject.SetActive(true);
            
            if (showDebugLogs) Debug.Log("[ConsumableAnimationHandler] Включен stick для WeaponOil1");
        }
        
        /// <summary>
        /// Выключает объект stick (для WeaponOil1)
        /// </summary>
        private void DisableStick()
        {
            if (_stickObject == null)
            {
                if (showDebugLogs) Debug.LogWarning("[ConsumableAnimationHandler] Объект stick не найден, не можем выключить");
                return;
            }
            
            _stickObject.SetActive(false);
            
            if (showDebugLogs) Debug.Log("[ConsumableAnimationHandler] Выключен stick для WeaponOil1");
        }

        /// <summary>
        /// Применяет материал из ItemData (weaponOilMaterial) ко всем рендерерам на stick на 1 минуту, затем возвращает обычный материал.
        /// </summary>
        private void ApplyWeaponOilMaterial(ItemData data)
        {
            if (data == null || data.weaponOilMaterial == null || _stickObject == null) return;
            Renderer[] renderers = _stickObject.GetComponentsInChildren<Renderer>(true);
            _savedStickMaterials = new List<(Renderer, Material)>();
            foreach (Renderer r in renderers)
            {
                _savedStickMaterials.Add((r, r.sharedMaterial));
                r.material = data.weaponOilMaterial;
                if (showDebugLogs) Debug.Log($"[ConsumableAnimationHandler] На stick применён материал {data.weaponOilMaterial.name} (рендерер: {r.gameObject.name})");
            }
            IsWeaponOilActive = true;
            if (_revertStickMaterialCoroutine != null) StopCoroutine(_revertStickMaterialCoroutine);
            _revertStickMaterialCoroutine = StartCoroutine(RevertStickMaterialAfterDelay(WeaponOilDurationSeconds));
        }

        private IEnumerator RevertStickMaterialAfterDelay(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _revertStickMaterialCoroutine = null;
            IsWeaponOilActive = false;
            if (_savedStickMaterials != null)
            {
                foreach (var (r, shared) in _savedStickMaterials)
                {
                    if (r != null) r.sharedMaterial = shared;
                }
                _savedStickMaterials = null;
                if (showDebugLogs) Debug.Log("[ConsumableAnimationHandler] Материал палки возвращён к обычному (истекла 1 мин масла).");
            }
        }
    }
}
