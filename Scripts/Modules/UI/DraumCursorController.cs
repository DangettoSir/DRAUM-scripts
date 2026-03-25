using UnityEngine;
using DRAUM.Core.Infrastructure.Logger;
using DRAUM.Core;
using DRAUM.Modules.Player.Events;

namespace DRAUM.Modules.UI
{
    /// <summary>
    /// Контроллер анимаций курсора (DraumCursor).
    /// Синхронизирует анимации курсора с состоянием FPMAxe (WindUp) и интеракцией.
    /// </summary>
    public class DraumCursorController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Animator курсора (DraumCursor_AC)")]
        public Animator cursorAnimator;
        
        [Tooltip("FPMAxe для синхронизации WindUp состояний (автопоиск если не назначен)")]
        public FPMAxe fpAxe;
        
        [Tooltip("FirstPersonController для проверки интеракции (автопоиск если не назначен)")]
        public FirstPersonController playerController;
        
        // Настройки включения логов задаются через `LogSettings` (LogSettings фильтрует Console вывод).
        
        // Параметры Animator (имена должны совпадать с DraumCursor_AC)
        private static readonly int CursorLeft = Animator.StringToHash("IsLeft");
        private static readonly int CursorRight = Animator.StringToHash("IsRight");
        private static readonly int CursorUp = Animator.StringToHash("IsUp");
        private static readonly int CursorDown = Animator.StringToHash("IsDown");
        private static readonly int CursorInteract = Animator.StringToHash("IsInteract");
        
        // Кэш предыдущего состояния для логирования только при изменении
        private bool _lastLeft, _lastRight, _lastUp, _lastDown, _lastInteract;
        private bool _stateInitialized = false;
        private bool _useEventBusCues = false;
        private bool _combatCueSubscribed;
        
        private void Awake()
        {
            // Автопоиск компонентов если не назначены
            if (cursorAnimator == null)
            {
                cursorAnimator = GetComponent<Animator>();
                if (cursorAnimator == null)
                {
                    cursorAnimator = GetComponentInChildren<Animator>();
                }
            }
            
            if (fpAxe == null)
            {
                fpAxe = FindFirstObjectByType<FPMAxe>();
            }
            
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<FirstPersonController>();
            }
            
            if (cursorAnimator == null)
            {
                DraumLogger.Error(this, "[DraumCursorController] Cursor animator not found! Assign it in Inspector.");
            }
            
            if (fpAxe == null)
            {
                DraumLogger.Warning(this, "[DraumCursorController] FPMAxe not found! WindUp sync will not work.");
            }
            
            if (playerController == null)
            {
                DraumLogger.Warning(this, "[DraumCursorController] FirstPersonController not found! Interaction won't work.");
            }
        }
        
        private void Update()
        {
            if (cursorAnimator == null) return;
            TrySubscribeCombatCue();

            // Проверяем интеракцию (приоритет выше чем WindUp)
            bool isInteracting = CheckInteraction();

            if (isInteracting)
            {
                // Если есть интеракция - показываем только IsInteract, остальные false
                SetCursorState(false, false, false, false, true);
                return;
            }

            if (_useEventBusCues) return;

            // Проверяем WindUp состояния из FPMAxe
            bool windUpLeft = CheckWindUpLeft();
            bool windUpRight = CheckWindUpRight();
            bool windUpUp = CheckWindUpUp();

            // Если есть WindUp - показываем соответствующее направление
            if (windUpLeft || windUpRight || windUpUp)
            {
                SetCursorState(windUpLeft, windUpRight, windUpUp, false, false);
            }
            else
            {
                // Нет ни WindUp, ни интеракции - Idle (все false)
                SetCursorState(false, false, false, false, false);
            }
        }
        
        /// <summary>
        /// Проверяет есть ли интеракция (игрок смотрит на интерактивный объект)
        /// </summary>
        private bool CheckInteraction()
        {
            if (playerController == null) return false;
            
            return playerController.IsLookingAtInteractable();
        }
        
        /// <summary>
        /// Проверяет WindUpLeftReady из FPMAxe
        /// </summary>
        private bool CheckWindUpLeft()
        {
            if (fpAxe == null || fpAxe.animator == null) return false;
            
            return fpAxe.animator.GetBool("WindUpLeftReady");
        }
        
        /// <summary>
        /// Проверяет WindUpRightReady из FPMAxe
        /// </summary>
        private bool CheckWindUpRight()
        {
            if (fpAxe == null || fpAxe.animator == null) return false;
            
            return fpAxe.animator.GetBool("WindUpRightReady");
        }
        
        /// <summary>
        /// Проверяет WindUpUpReady из FPMAxe
        /// </summary>
        private bool CheckWindUpUp()
        {
            if (fpAxe == null || fpAxe.animator == null) return false;
            
            return fpAxe.animator.GetBool("WindUpUpReady");
        }
        
        /// <summary>
        /// Устанавливает состояние курсора (все параметры Animator)
        /// </summary>
        private void SetCursorState(bool left, bool right, bool up, bool down, bool interact)
        {
            if (cursorAnimator == null) return;
            
            // Проверяем изменилось ли состояние (для логирования только при изменении)
            bool stateChanged = !_stateInitialized || 
                _lastLeft != left || _lastRight != right || _lastUp != up || 
                _lastDown != down || _lastInteract != interact;
            
            cursorAnimator.SetBool(CursorLeft, left);
            cursorAnimator.SetBool(CursorRight, right);
            cursorAnimator.SetBool(CursorUp, up);
            cursorAnimator.SetBool(CursorDown, down);
            cursorAnimator.SetBool(CursorInteract, interact);
            
            // Логируем только при изменении состояния
            if (stateChanged)
            {
                DraumLogger.Info(this, $"[DraumCursorController] SetCursorState: Left={left}, Right={right}, Up={up}, Down={down}, Interact={interact}");
            }
            
            // Сохраняем текущее состояние
            _lastLeft = left;
            _lastRight = right;
            _lastUp = up;
            _lastDown = down;
            _lastInteract = interact;
            _stateInitialized = true;
        }

        private void OnEnable()
        {
            TrySubscribeCombatCue();
        }

        private void OnDisable()
        {
            TryUnsubscribeCombatCue();
        }

        private void TrySubscribeCombatCue()
        {
            if (_combatCueSubscribed) return;
            if (EventBus.Instance == null) return;
            EventBus.Instance.Subscribe<CombatAnimationCueEvent>(OnCombatAnimationCue);
            _combatCueSubscribed = true;
            DraumLogger.Info(this, "[DraumCursorController] Subscribed to CombatAnimationCueEvent");
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
        }

        private void OnCombatAnimationCue(CombatAnimationCueEvent evt)
        {
            if (evt == null || cursorAnimator == null || string.IsNullOrWhiteSpace(evt.CueKey)) return;
            if (!IsTargetMatch(evt.Target, "Cursor")) return;

            _useEventBusCues = true;
            string stateName = evt.CueKey.Trim();
            int fullPathHash = Animator.StringToHash($"Base Layer.{stateName}");
            if (!cursorAnimator.HasState(0, fullPathHash))
            {
                DraumLogger.Warning(this, $"[DraumCursorController] Cursor cue state not found: Base Layer.{stateName}");
                _useEventBusCues = false;
                return;
            }

            // Гарантируем, что интеракционный флаг не залипает при ручном проигрывании cursor state.
            cursorAnimator.SetBool(CursorInteract, false);
            cursorAnimator.Play(fullPathHash, 0, 0f);
            DraumLogger.Info(this, $"[DraumCursorController] Cursor cue state play: {stateName}");
            _useEventBusCues = false;
        }

        private static bool IsTargetMatch(string target, string channel)
        {
            if (string.IsNullOrWhiteSpace(target)) return true;
            return target.Equals("All", System.StringComparison.OrdinalIgnoreCase) ||
                   target.Equals(channel, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
