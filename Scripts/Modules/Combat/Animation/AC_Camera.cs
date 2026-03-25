using System.Collections.Generic;
using DRAUM.Core;
using DRAUM.Modules.Player.Events;
using UnityEngine;
using DRAUM.Modules.Combat.Animation.Contracts;

namespace DRAUM.Modules.Combat.Animation
{
    /// <summary>
    /// Управляет Animator камеры через контракт ICameraAnimationStateProvider/>.
    /// Единственная точка записи в cameraAnimator — всё состояние приходит из провайдера (FPMAxe и т.д.).
    /// Параметры Animator камеры (как у FPHands): Float — MouseX, MouseY; Bool — SwingLeft, SwingRight, SwingUp, WindUpLeftReady, WindUpRightReady, WindUpUpReady, IsBlocking; Trigger — Equip, Unequip, BlockStart.
    /// </summary>
    public class AC_Camera : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Animator на камере (или на объекте камеры)")]
        public Animator cameraAnimator;
        [Tooltip("Провайдер состояния (MonoBehaviour с интерфейсом ICameraAnimationStateProvider), например FPMAxe")]
        public MonoBehaviour stateProvider;

        [Header("Options")]
        [Tooltip("Если задано — синхронизируются только эти параметры. Пусто = все параметры, которые есть у камеры и у провайдера.")]
        public string[] parameterFilter = new string[0];

        [Header("Weapon Layers")]
        [Tooltip("Имена слоёв Animator камеры под оружие (например Stick). Провайдер возвращает текущий слой из ItemData.cameraAnimationLayerName — включается weight 1, остальные 0. Пусто = слои не трогаем.")]
        public string[] weaponLayerNames = new string[0];
        [Tooltip("Скорость перехода weight слоёв оружия")]
        [Range(0.1f, 20f)]
        public float weaponLayerBlendSpeed = 8f;

        [Header("Consumable Layer")]
        [Tooltip("Имя слоя Consumable в Animator камеры (например Consumable). Управляется через ConsumableAnimationHandler — weight 1 во время потребления, 0 иначе.")]
        public string consumableLayerName = "Consumable";
        [Tooltip("ConsumableAnimationHandler для проверки состояния потребления (автопоиск если не назначен)")]
        public ConsumableAnimationHandler consumableAnimationHandler;
        [Tooltip("Скорость перехода weight слоя Consumable")]
        [Range(0.1f, 20f)]
        public float consumableLayerBlendSpeed = 8f;

        [Header("Brutality Layer")]
        [Tooltip("Имя слоя Brutality в Animator камеры (например Brutality). Управляется через BrutalityController — weight 1 во время добивания, 0 иначе.")]
        public string brutalityLayerName = "Brutality";
        [Tooltip("BrutalityController для проверки состояния добивания (автопоиск если не назначен)")]
        public BrutalityController brutalityController;
        [Tooltip("Имя анимационного состояния BRUTAL-CAMERA на слое Brutality")]
        public string brutalCameraStateName = "BRUTAL-CAMERA";
        [Tooltip("Скорость перехода weight слоя Brutality")]
        [Range(0.1f, 20f)]
        public float brutalityLayerBlendSpeed = 8f;

        [Header("Debug")]
        [HideInInspector] public bool showDebugLogs = false;

        private ICameraAnimationStateProvider _provider;
        private HashSet<string> _paramNames = new HashSet<string>();
        private HashSet<string> _filterSet = new HashSet<string>();
        private Dictionary<string, int> _weaponLayerIndices = new Dictionary<string, int>();
        private Dictionary<string, float> _weaponLayerTargetWeight = new Dictionary<string, float>();
        private int _consumableLayerIndex = -1;
        private float _consumableLayerTargetWeight = 0f;
        private float _consumableLayerCurrentWeight = 0f;
        private bool _isCameraConsuming = false;
        private int _brutalityLayerIndex = -1;
        private float _brutalityLayerTargetWeight = 0f;
        private float _brutalityLayerCurrentWeight = 0f;
        private bool _isCameraBrutalizing = false;
        private bool _initialized;
        private bool _combatCueSubscribed;

        private void Awake()
        {
            if (cameraAnimator == null) cameraAnimator = GetComponent<Animator>();
            if (cameraAnimator == null) cameraAnimator = GetComponentInChildren<Animator>();
            if (consumableAnimationHandler == null) consumableAnimationHandler = FindFirstObjectByType<ConsumableAnimationHandler>();
            if (brutalityController == null) brutalityController = FindFirstObjectByType<BrutalityController>();
            ResolveProvider();
            BuildParameterCache();
        }

        private void Start()
        {
            if (_paramNames.Count == 0 && cameraAnimator != null && cameraAnimator.runtimeAnimatorController != null)
                BuildParameterCache();
        }

        private void OnEnable()
        {
            ResolveProvider();
            if (_provider != null)
                _provider.TriggerFired += OnTriggerFired;
            TrySubscribeCombatCue();
        }

        private void OnDisable()
        {
            if (_provider != null)
                _provider.TriggerFired -= OnTriggerFired;
            TryUnsubscribeCombatCue();
        }

        private void ResolveProvider()
        {
            _provider = stateProvider != null ? stateProvider as ICameraAnimationStateProvider : null;
            if (_provider == null && stateProvider != null)
                _provider = stateProvider.GetComponent<ICameraAnimationStateProvider>();
            if (_provider == null && showDebugLogs)
                Debug.LogWarning("[AC_Camera] ICameraAnimationStateProvider не найден. Назначь stateProvider (например FPMAxe).");
        }

        private void BuildParameterCache()
        {
            _paramNames.Clear();
            if (cameraAnimator != null && cameraAnimator.runtimeAnimatorController != null)
            {
                foreach (var p in cameraAnimator.parameters)
                    _paramNames.Add(p.name);
            }

            _filterSet.Clear();
            if (parameterFilter != null && parameterFilter.Length > 0)
            {
                foreach (var name in parameterFilter)
                    if (!string.IsNullOrEmpty(name)) _filterSet.Add(name.Trim());
            }

            _weaponLayerIndices.Clear();
            if (cameraAnimator != null && weaponLayerNames != null)
            {
                for (int i = 0; i < cameraAnimator.layerCount; i++)
                {
                    string layerName = cameraAnimator.GetLayerName(i);
                    foreach (var wn in weaponLayerNames)
                    {
                        if (string.IsNullOrEmpty(wn)) continue;
                        if (layerName == wn.Trim())
                        {
                            _weaponLayerIndices[wn.Trim()] = i;
                            if (!_weaponLayerTargetWeight.ContainsKey(wn.Trim()))
                                _weaponLayerTargetWeight[wn.Trim()] = 0f;
                            break;
                        }
                    }
                }
            }

            _consumableLayerIndex = -1;
            if (cameraAnimator != null && !string.IsNullOrEmpty(consumableLayerName))
            {
                _consumableLayerIndex = cameraAnimator.GetLayerIndex(consumableLayerName);
                if (_consumableLayerIndex == -1 && showDebugLogs)
                    Debug.LogWarning($"[AC_Camera] Слой Consumable '{consumableLayerName}' не найден в Animator камеры!");
            }

            _brutalityLayerIndex = -1;
            if (cameraAnimator != null && !string.IsNullOrEmpty(brutalityLayerName))
            {
                _brutalityLayerIndex = cameraAnimator.GetLayerIndex(brutalityLayerName);
                if (_brutalityLayerIndex == -1 && showDebugLogs)
                    Debug.LogWarning($"[AC_Camera] Слой Brutality '{brutalityLayerName}' не найден в Animator камеры!");
            }

            _initialized = cameraAnimator != null && _provider != null;
        }

        private void LateUpdate()
        {
            TrySubscribeCombatCue();

            if (!_initialized || _provider == null || cameraAnimator == null || cameraAnimator.runtimeAnimatorController == null)
                return;

            int paramCount = cameraAnimator.parameterCount;
            
            for (int i = 0; i < paramCount; i++)
            {
                if (cameraAnimator == null || cameraAnimator.runtimeAnimatorController == null || i >= cameraAnimator.parameterCount)
                    break;
                
                try
                {
                    var p = cameraAnimator.GetParameter(i);
                    if (p.type == AnimatorControllerParameterType.Trigger) continue;
                    if (_filterSet.Count > 0 && !_filterSet.Contains(p.name)) continue;

                    switch (p.type)
                    {
                        case AnimatorControllerParameterType.Float:
                            if (_provider.TryGetFloat(p.name, out float f))
                                cameraAnimator.SetFloat(p.nameHash, f);
                            break;
                        case AnimatorControllerParameterType.Int:
                            if (_provider.TryGetInt(p.name, out int n))
                                cameraAnimator.SetInteger(p.nameHash, n);
                            break;
                        case AnimatorControllerParameterType.Bool:
                            if (_provider.TryGetBool(p.name, out bool b))
                                cameraAnimator.SetBool(p.nameHash, b);
                            break;
                    }
                }
                catch (System.IndexOutOfRangeException)
                {
                    if (showDebugLogs)
                        Debug.LogWarning("[AC_Camera] Animator изменился во время LateUpdate, прерываем синхронизацию параметров");
                    break;
                }
            }

            UpdateWeaponLayers();
            UpdateConsumableLayer();
            UpdateBrutalityLayer();
        }

        private void TrySubscribeCombatCue()
        {
            if (_combatCueSubscribed) return;
            if (EventBus.Instance == null) return;
            EventBus.Instance.Subscribe<CombatAnimationCueEvent>(OnCombatAnimationCue);
            _combatCueSubscribed = true;
            if (showDebugLogs) Debug.Log("[AC_Camera] Subscribed to CombatAnimationCueEvent");
        }

        private void TryUnsubscribeCombatCue()
        {
            if (!_combatCueSubscribed) return;
            if (EventBus.Instance == null)
            {
                _combatCueSubscribed = false;
                return;
            }

            EventBus.Instance.Unsubscribe<CombatAnimationCueEvent>(OnCombatAnimationCue);
            _combatCueSubscribed = false;
            if (showDebugLogs) Debug.Log("[AC_Camera] Unsubscribed from CombatAnimationCueEvent");
        }

        private void UpdateWeaponLayers()
        {
            if (cameraAnimator == null || weaponLayerNames == null || weaponLayerNames.Length == 0) return;
            string currentLayer = _provider?.GetCurrentCameraAnimationLayerName()?.Trim() ?? "";
            float dt = Time.deltaTime;
            float blend = Mathf.Clamp01(weaponLayerBlendSpeed * dt);

            foreach (var kv in _weaponLayerIndices)
            {
                string name = kv.Key;
                int layerIndex = kv.Value;
                float target = (name == currentLayer && !string.IsNullOrEmpty(currentLayer)) ? 1f : 0f;
                if (!_weaponLayerTargetWeight.ContainsKey(name)) _weaponLayerTargetWeight[name] = 0f;
                _weaponLayerTargetWeight[name] = Mathf.MoveTowards(_weaponLayerTargetWeight[name], target, blend);
                if (Mathf.Abs(_weaponLayerTargetWeight[name] - target) < 0.001f) _weaponLayerTargetWeight[name] = target;
                cameraAnimator.SetLayerWeight(layerIndex, _weaponLayerTargetWeight[name]);
            }
        }

        private void UpdateConsumableLayer()
        {
            if (cameraAnimator == null || _consumableLayerIndex == -1 || consumableAnimationHandler == null) return;
            
            bool isConsuming = consumableAnimationHandler.IsConsuming();

            if (isConsuming && !_isCameraConsuming)
            {
                _isCameraConsuming = true;
                _consumableLayerTargetWeight = 1f;

                string stateName = consumableAnimationHandler.CurrentAnimationStateName;
                if (!string.IsNullOrEmpty(stateName))
                {
                    cameraAnimator.CrossFadeInFixedTime(stateName, 0f, _consumableLayerIndex, 0f);
                    if (showDebugLogs) Debug.Log($"[AC_Camera] Запущена анимация Consumable: {stateName}");
                }
            }
            else if (!isConsuming && _isCameraConsuming)
            {
                _isCameraConsuming = false;
                _consumableLayerTargetWeight = 0f;
            }

            float blend = consumableLayerBlendSpeed * Time.deltaTime;
            _consumableLayerCurrentWeight = Mathf.MoveTowards(_consumableLayerCurrentWeight, _consumableLayerTargetWeight, blend);
            if (Mathf.Abs(_consumableLayerCurrentWeight - _consumableLayerTargetWeight) < 0.001f)
            {
                _consumableLayerCurrentWeight = _consumableLayerTargetWeight;
            }
            cameraAnimator.SetLayerWeight(_consumableLayerIndex, _consumableLayerCurrentWeight);
        }

        private void UpdateBrutalityLayer()
        {
            if (cameraAnimator == null || _brutalityLayerIndex == -1 || brutalityController == null) return;
            
            bool isBrutalizing = brutalityController.IsExecutingBrutality;

            if (isBrutalizing && !_isCameraBrutalizing)
            {
                _isCameraBrutalizing = true;
                _brutalityLayerTargetWeight = 1f;
                if (!string.IsNullOrEmpty(brutalCameraStateName))
                {
                    cameraAnimator.CrossFadeInFixedTime(brutalCameraStateName, 0.1f, _brutalityLayerIndex, 0f);
                    if (showDebugLogs) Debug.Log($"[AC_Camera] Запущена анимация Brutality: {brutalCameraStateName}");
                }
            }
            else if (!isBrutalizing && _isCameraBrutalizing)
            {
                _isCameraBrutalizing = false;
                _brutalityLayerTargetWeight = 0f;
            }

            float blend = brutalityLayerBlendSpeed * Time.deltaTime;
            _brutalityLayerCurrentWeight = Mathf.MoveTowards(_brutalityLayerCurrentWeight, _brutalityLayerTargetWeight, blend);
            if (Mathf.Abs(_brutalityLayerCurrentWeight - _brutalityLayerTargetWeight) < 0.001f)
            {
                _brutalityLayerCurrentWeight = _brutalityLayerTargetWeight;
            }
            cameraAnimator.SetLayerWeight(_brutalityLayerIndex, _brutalityLayerCurrentWeight);
        }

        private void OnTriggerFired(string triggerName)
        {
            if (cameraAnimator == null || string.IsNullOrEmpty(triggerName)) return;
            if (_paramNames.Count == 0) BuildParameterCache();
            int hash = Animator.StringToHash(triggerName);
            for (int i = 0; i < cameraAnimator.parameterCount; i++)
            {
                if (cameraAnimator.GetParameter(i).nameHash == hash)
                {
                    cameraAnimator.SetTrigger(hash);
                    if (showDebugLogs) Debug.Log($"[AC_Camera] Trigger: {triggerName}");
                    return;
                }
            }
            if (showDebugLogs) Debug.LogWarning($"[AC_Camera] Триггер '{triggerName}' не найден в Animator камеры.");
        }

        private void OnCombatAnimationCue(CombatAnimationCueEvent evt)
        {
            if (evt == null || cameraAnimator == null || string.IsNullOrWhiteSpace(evt.CueKey)) return;
            if (!IsTargetMatch(evt.Target, "Camera")) return;
            string cue = evt.CueKey.Trim();
            if (TryPlayCueState(cue)) return;
            TriggerIfExists(cue);
        }

        private static bool IsTargetMatch(string target, string channel)
        {
            if (string.IsNullOrWhiteSpace(target)) return true;
            return target.Equals("All", System.StringComparison.OrdinalIgnoreCase) ||
                   target.Equals(channel, System.StringComparison.OrdinalIgnoreCase);
        }

        private bool TryPlayCueState(string stateName)
        {
            if (cameraAnimator == null || string.IsNullOrEmpty(stateName)) return false;

            string currentWeaponLayer = _provider?.GetCurrentCameraAnimationLayerName()?.Trim() ?? "";

            if (!string.IsNullOrEmpty(currentWeaponLayer) &&
                _weaponLayerIndices.TryGetValue(currentWeaponLayer, out int weaponLayerIndex))
            {
                int fullPathHash = Animator.StringToHash($"{currentWeaponLayer}.{stateName}");
                if (cameraAnimator.HasState(weaponLayerIndex, fullPathHash))
                {
                    // Без fade: мгновенно запускаем state с начала.
                    cameraAnimator.Play(fullPathHash, weaponLayerIndex, 0f);
                    if (showDebugLogs) Debug.Log($"[AC_Camera] Combat cue state play: {stateName} (weapon layer: {currentWeaponLayer})");
                    return true;
                }
            }

            for (int i = 0; i < cameraAnimator.layerCount; i++)
            {
                string layerName = cameraAnimator.GetLayerName(i);
                int fullPathHash = Animator.StringToHash($"{layerName}.{stateName}");
                if (!cameraAnimator.HasState(i, fullPathHash)) continue;
                cameraAnimator.Play(fullPathHash, i, 0f);
                if (showDebugLogs) Debug.Log($"[AC_Camera] Combat cue state play: {stateName} (layer index: {i})");
                return true;
            }

            return false;
        }

        private void TriggerIfExists(string triggerName)
        {
            if (cameraAnimator == null || string.IsNullOrEmpty(triggerName)) return;
            int hash = Animator.StringToHash(triggerName);
            for (int i = 0; i < cameraAnimator.parameterCount; i++)
            {
                var p = cameraAnimator.GetParameter(i);
                if (p.type == AnimatorControllerParameterType.Trigger && p.nameHash == hash)
                {
                    cameraAnimator.SetTrigger(hash);
                    if (showDebugLogs) Debug.Log($"[AC_Camera] Combat cue trigger: {triggerName}");
                    return;
                }
            }
            if (showDebugLogs) Debug.LogWarning($"[AC_Camera] Combat cue trigger '{triggerName}' не найден в Animator камеры.");
        }

        /// <summary> Пересобрать кэш параметров (если сменили Runtime Animator Controller в рантайме). </summary>
        public void RebuildParameterCache()
        {
            BuildParameterCache();
        }
    }
}
