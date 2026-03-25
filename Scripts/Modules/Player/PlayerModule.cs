using DRAUM.Core;
using DRAUM.Core.Infrastructure.Logger;
using DRAUM.Modules.Player.Events;
using DRAUM.Modules.Inventory.Events;
using UnityEngine;

namespace DRAUM.Modules.Player
{
    public class PlayerModule : ManagerModuleBase
    {
        [Header("Player References")]
        [Tooltip("FirstPersonController компонент игрока")]
        public FirstPersonController playerController;

        [Tooltip("Автоматически найти FirstPersonController в сцене, если не назначен")]
        public bool autoFindController = true;

        [Header("Footstep Detection")]
        [Tooltip("Слой, по которому определяем поверхность для звука шагов")]
        public LayerMask groundLayerMask = -1;

        [Tooltip("Расстояние между шагами при ходьбе (обычно 1.0–1.6)")]
        public float footstepDistance = 1.2f;
        
        [Tooltip("Множитель расстояния между шагами при беге (меньше = чаще шаги)")]
        [Range(0.3f, 1f)]
        public float sprintFootstepMultiplier = 0.6f;
        
        [Tooltip("Множитель расстояния между шагами при ползании (больше = реже шаги)")]
        [Range(1f, 3f)]
        public float crouchFootstepMultiplier = 1.8f;

        [Tooltip("Минимальная скорость, при которой играются звуки шагов")]
        public float minSpeedForFootsteps = 1.0f;

        [Tooltip("Минимальный интервал между звуками шагов (в секундах)")]
        public float minTimeBetweenFootsteps = 0.35f;
        
        [Tooltip("Множитель минимального интервала между шагами при беге (меньше = чаще).")]
        [Range(0.3f, 1f)]
        public float sprintMinTimeMultiplier = 0.65f;

        public override string ModuleName => "Player";
        public override int InitializationPriority => 15;
        public override ModuleFlags Flags => ModuleFlags.StandardUpdate | ModuleFlags.CanBePaused;

        #region Private Fields

        private Vector3 lastPosition;
        private float lastSpeed;
        private bool lastCombatModeState;
        private bool lastLockState;
        private bool lastInteractionState;
        private string lastInteractionName;

        private float accumulatedFootstepDistance = 0f;
        private Vector3 lastFootstepPosition;
        private float lastFootstepTime;

        #endregion

        #region Properties

        public bool IsControllerEnabled => playerController != null && playerController.enabled;

        public bool CanMove
        {
            get => playerController != null && playerController.playerCanMove;
            set
            {
                if (playerController != null)
                    playerController.playerCanMove = value;
            }
        }

        public bool CanLook
        {
            get => playerController != null && playerController.cameraCanMove;
            set
            {
                if (playerController != null)
                    playerController.cameraCanMove = value;
            }
        }

        public float CurrentSpeed
        {
            get
            {
                if (playerController != null && playerController.GetComponent<Rigidbody>() != null)
                {
                    var rb = playerController.GetComponent<Rigidbody>();
                    return new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;
                }
                return 0f;
            }
        }

        public bool IsCombatModeActive => playerController != null && playerController.IsCombatModeActive();

        public bool IsLookingAtInteractable => playerController != null && playerController.IsLookingAtInteractable();

        public string CurrentInteractionName => playerController != null ? playerController.GetCurrentInteractionName() : "";

        public Camera PlayerCamera => playerController != null ? playerController.playerCamera : null;
        
        /// <summary>
        /// Проверяет спринтует ли игрок (через FirstPersonController)
        /// </summary>
        public bool IsSprinting()
        {
            return playerController != null && playerController.IsSprinting;
        }
        
        /// <summary>
        /// Проверяет ползет ли игрок (через FirstPersonController)
        /// </summary>
        public bool IsCrouched()
        {
            return playerController != null && playerController.IsCrouched;
        }

        #endregion

        #region Initialization

        protected override void OnInitialize()
        {
            if (playerController == null && autoFindController)
            {
                playerController = FindFirstObjectByType<FirstPersonController>();
            }

            if (playerController == null)
            {
                DraumLogger.Error(this, "[PlayerModule] FirstPersonController not found! Module cannot work.");
                return;
            }

            lastPosition = GetPosition();
            lastSpeed = CurrentSpeed;
            lastCombatModeState = IsCombatModeActive;
            lastLockState = !CanMove || !CanLook;
            lastInteractionState = IsLookingAtInteractable;
            lastInteractionName = CurrentInteractionName;

            lastFootstepPosition = GetPosition();
            lastFootstepTime = -10f;

            DraumLogger.Info(this, "[PlayerModule] Player module initialized.");
            SubscribeToEvents();
        }

        protected override void OnShutdown()
        {
            UnsubscribeFromEvents();
            playerController = null;
            DraumLogger.Info(this, "[PlayerModule] Player module unloaded.");
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeToEvents()
        {
            Events.Subscribe<InventoryOpenedEvent>(OnInventoryOpened);
            Events.Subscribe<InventoryClosedEvent>(OnInventoryClosed);
            Events.Subscribe<PlayerLockStateChangedEvent>(OnPlayerLockStateChangedRequested);
        }

        private void UnsubscribeFromEvents()
        {
            if (EventBus.Instance == null) return;
            Events.Unsubscribe<InventoryOpenedEvent>(OnInventoryOpened);
            Events.Unsubscribe<InventoryClosedEvent>(OnInventoryClosed);
            Events.Unsubscribe<PlayerLockStateChangedEvent>(OnPlayerLockStateChangedRequested);
        }

        private void OnInventoryOpened(InventoryOpenedEvent evt)
        {
            LockPlayer(true, true);
        }

        private void OnInventoryClosed(InventoryClosedEvent evt)
        {
            UnlockPlayer();
        }

        private void OnPlayerLockStateChangedRequested(PlayerLockStateChangedEvent evt)
        {
            if (evt == null || playerController == null) return;

            // Применяем внешний lock-запрос (диалоги/меню и т.д.) к реальному контроллеру.
            LockPlayer(evt.MovementLocked, evt.CameraLocked);
            playerController.lockCursor = !evt.IsLocked;

            DraumLogger.Info(this,
                $"[PlayerModule] External lock applied. IsLocked={evt.IsLocked}, MovementLocked={evt.MovementLocked}, CameraLocked={evt.CameraLocked}");
        }

        #endregion

        #region Public API

        public void SetControllerEnabled(bool enabled)
        {
            if (playerController != null)
            {
                playerController.enabled = enabled;
            }
        }

        public void LockPlayer(bool lockMovement = true, bool lockCamera = true)
        {
            CanMove = !lockMovement;
            CanLook = !lockCamera;
        }

        public void UnlockPlayer()
        {
            CanMove = true;
            CanLook = true;
        }

        public void EnableCombatMode()
        {
            if (playerController != null)
            {
                playerController.EnableCombatMode();
            }
        }

        public void DisableCombatMode()
        {
            if (playerController != null)
            {
                playerController.DisableCombatMode();
            }
        }

        public void EnableMovement()
        {
            if (playerController != null)
            {
                playerController.EnableMovement();
            }
        }

        public void SetPosition(Vector3 position)
        {
            if (playerController != null)
            {
                playerController.transform.position = position;
            }
        }

        public Vector3 GetPosition()
        {
            return playerController != null ? playerController.transform.position : Vector3.zero;
        }

        public Vector3 GetForward()
        {
            return playerController != null && PlayerCamera != null
                ? PlayerCamera.transform.forward
                : Vector3.forward;
        }

        public void RequestOpenInventory()
        {
            Events.Publish<InventoryOpenRequestEvent>();
        }

        public void RequestCloseInventory()
        {
            Events.Publish<InventoryCloseRequestEvent>();
        }

        public void ToggleInventory()
        {
            Events.Publish<InventoryOpenRequestEvent>();
        }

        #endregion

        #region Update

        protected override void OnUpdate()
        {
            if (playerController == null) return;

            CheckAndPublishPositionChanged();
            CheckAndPublishSpeedChanged();
            CheckAndPublishCombatModeChanged();
            CheckAndPublishLockStateChanged();
            CheckAndPublishInteractionChanged();
            CheckAndPublishFootstep();
        }

        private void CheckAndPublishPositionChanged()
        {
            Vector3 currentPosition = GetPosition();
            if (Vector3.Distance(currentPosition, lastPosition) > 0.1f)
            {
                lastPosition = currentPosition;
                Events.Publish(new PlayerPositionChangedEvent { Position = currentPosition });
            }
        }

        private void CheckAndPublishSpeedChanged()
        {
            float currentSpeed = CurrentSpeed;
            if (Mathf.Abs(currentSpeed - lastSpeed) > 0.1f)
            {
                lastSpeed = currentSpeed;
                Events.Publish(new PlayerSpeedChangedEvent { Speed = currentSpeed });
            }
        }

        private void CheckAndPublishCombatModeChanged()
        {
            bool currentCombatMode = IsCombatModeActive;
            if (currentCombatMode != lastCombatModeState)
            {
                lastCombatModeState = currentCombatMode;
                Events.Publish(new PlayerCombatModeChangedEvent { IsCombatModeActive = currentCombatMode });
            }
        }

        private void CheckAndPublishLockStateChanged()
        {
            bool currentLockState = !CanMove || !CanLook;
            if (currentLockState != lastLockState)
            {
                lastLockState = currentLockState;
                Events.Publish(new PlayerLockStateChangedEvent
                {
                    IsLocked = currentLockState,
                    MovementLocked = !CanMove,
                    CameraLocked = !CanLook
                });
            }
        }

        private void CheckAndPublishInteractionChanged()
        {
            bool currentInteraction = IsLookingAtInteractable;
            string currentName = CurrentInteractionName;

            if (currentInteraction != lastInteractionState || currentName != lastInteractionName)
            {
                lastInteractionState = currentInteraction;
                lastInteractionName = currentName;
                Events.Publish(new PlayerInteractionChangedEvent
                {
                    IsLookingAtInteractable = currentInteraction,
                    InteractionName = currentName
                });
            }
        }

        private void CheckAndPublishFootstep()
        {
            if (!CanMove || CurrentSpeed < minSpeedForFootsteps)
                return;

            bool isSprinting = playerController != null && IsSprinting();
            bool isCrouched = playerController != null && IsCrouched();

            // Проверяем минимальный интервал по времени
            float currentMinTimeBetweenFootsteps = minTimeBetweenFootsteps;
            if (isSprinting)
            {
                currentMinTimeBetweenFootsteps *= sprintMinTimeMultiplier;
            }
            if (Time.time - lastFootstepTime < currentMinTimeBetweenFootsteps)
                return;

            float distanceThisFrame = Vector3.Distance(GetPosition(), lastFootstepPosition);
            accumulatedFootstepDistance += distanceThisFrame;

            // Вычисляем текущее расстояние между шагами в зависимости от состояния
            float currentFootstepDistance = footstepDistance;
            
            if (isSprinting)
            {
                currentFootstepDistance *= sprintFootstepMultiplier; // Бег - чаще шаги
            }
            else if (isCrouched)
            {
                currentFootstepDistance *= crouchFootstepMultiplier; // Ползание - реже шаги
            }
            
            if (accumulatedFootstepDistance >= currentFootstepDistance)
            {
                Vector3 rayStart = GetPosition() + Vector3.down * 0.5f + Vector3.up * 0.1f;

                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 1.5f, groundLayerMask))
                {
                    string materialName = hit.collider.sharedMaterial?.name ?? "Default";
                    
                    Events.Publish(new PlayerFootstepEvent 
                    { 
                        MaterialName = materialName, 
                        Position = hit.point,
                        IsSprinting = isSprinting,
                        IsCrouched = isCrouched
                    });

                    lastFootstepTime = Time.time;
                    accumulatedFootstepDistance = 0f;
                    lastFootstepPosition = GetPosition();
                }
                else
                {
                    accumulatedFootstepDistance = 0f;
                    lastFootstepPosition = GetPosition();
                }
            }
        }

        #endregion

        #region Activation/Deactivation

        protected override void OnActivate()
        {
            SetControllerEnabled(true);
            UnlockPlayer();
        }

        protected override void OnDeactivate()
        {
            LockPlayer(true, true);
            SetControllerEnabled(false);
        }

        #endregion
    }
}