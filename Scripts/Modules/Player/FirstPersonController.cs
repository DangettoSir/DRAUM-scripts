// CHANGE LOG
// 
// CHANGES || version VERSION
//
// "Enable/Disable Headbob, Changed look rotations - should result in reduced camera jitters" || version 1.0.1

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DRAUM.Modules.Combat.Abilities;
using DRAUM.Modules.Combat;
using DRAUM.Core.Infrastructure.Logger;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
    using UnityEditor;
    using System.Net;
#endif

[DefaultExecutionOrder(-100)] // Выполняется РАНЬШЕ других скриптов управления камерой
public class FirstPersonController : MonoBehaviour
{
    private Rigidbody rb;
    
    #region Input System
    [Header("Input System")]
    public InputActionAsset inputActions;
    
    private InputActionMap gameplayMap;
    private InputActionMap combatMap;
    private InputActionMap inventoryMap;
    
    // Gameplay actions
    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction crouchAction;
    private InputAction interactAction;
    private InputAction jumpAction;
    private InputAction inventoryAction; // TAB из Gameplay map (открыть)
    
    // Inventory actions
    private InputAction closeInventoryAction; // TAB из Inventory map (переход в Closed)
    private InputAction exitInventoryAction; // ESC из Inventory map (полноПе закрытие)в
    
    // Combat actions
    private InputAction primaryAttackAction;
    private InputAction secondaryAttackAction;
    private InputAction forceModeAction;
    
    // Equipment actions
    private InputAction equipFirstAction;
    private InputAction equipSecondAction;
    private InputAction equipThirdAction;
    
    // Combat mode state
    private bool isInCombatMode = false;
    #endregion

    #region Camera Movement Variables

    public Camera playerCamera;
    [Tooltip("Если задан — pitch применяется сюда, а не к камере. Камера (с Animator) — дочерний объект; анимации камеры тогда не перезаписываются. Пусто = pitch на playerCamera (как раньше).")]
    public Transform cameraPivot;

    public float fov = 60f;
    public bool invertCamera = false;
    public bool cameraCanMove = true;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 50f;

    // Crosshair
    public bool lockCursor = true;
    public bool crosshair = true;
    public GameObject crosshairPrefab;
    [HideInInspector] public GameObject crosshairInstance;

    public Color crosshairColor = Color.white;

    // Internal Variables
    private float yaw = 0.0f;
    private float pitch = 0.0f;

    // Camera Shake Support
    private Vector2 cameraShakeOffset = Vector2.zero;

    /// <summary>
    /// Устанавливает смещение камеры от подёргиваний (для внешних скриптов).
    /// Используется CameraShakeController для применения подёргиваний при ударах.
    /// </summary>
    /// <param name="offset">Смещение камеры (x = yaw, y = pitch)</param>
    public void SetCameraShakeOffset(Vector2 offset)
    {
        cameraShakeOffset = offset;
    }

    #region Camera Zoom Variables

    public bool enableZoom = true;
    public bool holdToZoom = false;
    public float zoomFOV = 30f;
    public float zoomStepTime = 5f;

    // Internal Variables
    private bool isZoomed = false;
    private InputAction zoomAction;

    #endregion
    #endregion

    #region Movement Variables

    public bool playerCanMove = true;
    
    [Header("Movement Config (Optional)")]
    [Tooltip("ScriptableObject конфиг движения (опционально, иначе используй параметры ниже)")]
    public MovementConfig movementConfig;
    
    [Header("Movement Settings (если нет MovementConfig)")]
    public float walkSpeed = 5f;
    public float maxVelocityChange = 10f;
    
    [Header("Acceleration / Deceleration")]
    [Tooltip("Использовать плавное ускорение/замедление")]
    public bool useAcceleration = true;
    
    [Tooltip("Время разгона до максимальной скорости (секунды)")]
    [Range(0.1f, 5f)]
    public float accelerationTime = 0.5f;
    
    [Tooltip("Время торможения до полной остановки (секунды)")]
    [Range(0.1f, 5f)]
    public float decelerationTime = 0.3f;

    // Internal Variables
    private bool isWalking = false;
    private float currentSpeedMultiplier = 0f; // 0 = стоит, 1 = полная скорость
    private float targetSpeedMultiplier = 0f;

    #region Sprint

    public bool enableSprint = true;
    public bool unlimitedSprint = false;
    public float sprintSpeed = 7f;
    public float sprintDuration = 5f;
    public float sprintCooldown = .5f;
    
    [Header("Sprint FOV")]
    [Tooltip("FOV при беге")]
    public float sprintFOV = 80f;
    [Tooltip("Скорость изменения FOV (старый метод)")]
    public float sprintFOVStepTime = 10f;
    [Tooltip("Использовать плавное изменение FOV")]
    public bool useSmoothSprintFOV = true;
    [Tooltip("Время изменения FOV (сек)")]
    [Range(0.1f, 3f)]
    public float sprintFOVTransitionTime = 0.5f;
    
    [Header("Sprint Acceleration")]
    [Tooltip("Использовать плавное ускорение спринта")]
    public bool useSprintAcceleration = true;
    [Tooltip("Время разгона от walkSpeed до sprintSpeed (сек)")]
    [Range(0.1f, 5f)]
    public float sprintAccelerationTime = 2f;
    [Tooltip("Время замедления от sprintSpeed до walkSpeed (сек)")]
    [Range(0.1f, 5f)]
    public float sprintDecelerationTime = 1f;

    [Header("Sprint Bar")]
    public bool useSprintBar = true;
    public bool hideBarWhenFull = true;
    public Image sprintBarBG;
    public Image sprintBar;
    public float sprintBarWidthPercent = .3f;
    public float sprintBarHeightPercent = .015f;

    // Internal Variables
    private CanvasGroup sprintBarCG;
    private bool isSprinting = false;
    private float sprintRemaining;
    private float sprintBarWidth;
    private float sprintBarHeight;
    private bool isSprintCooldown = false;
    private float sprintCooldownReset;
    
    // FOV transition
    private float currentFOVMultiplier = 0f; // 0 = normal FOV, 1 = sprint FOV
    private float targetFOVMultiplier = 0f;
    
    // Sprint acceleration
    private float currentSprintMultiplier = 0f; // 0 = walkSpeed, 1 = sprintSpeed
    private float targetSprintMultiplier = 0f;

    #endregion

    #region Jump

    public bool enableJump = true;
    public float jumpPower = 5f;

    // Internal Variables
    private bool isGrounded = false;

    #endregion

    #region Physics
    
    [Header("Physics (Ground Damping)")]
    [Tooltip("Linear Damping когда на земле")]
    [Range(0f, 10f)]
    public float groundedLinearDamping = 5f;
    
    [Tooltip("Linear Damping в воздухе")]
    [Range(0f, 5f)]
    public float airborneLinearDamping = 0.1f;
    
    [Tooltip("Скорость изменения damping")]
    [Range(1f, 20f)]
    public float dampingChangeSpeed = 10f;

    #endregion

    #region Crouch

    public bool enableCrouch = true;
    public bool holdToCrouch = true;
    public float crouchHeight = .75f;
    public float speedReduction = .5f;

    // Internal Variables
    private bool isCrouched = false;
    private Vector3 originalScale;
    
    /// <summary>
    /// Проверяет спринтует ли игрок в данный момент
    /// </summary>
    public bool IsSprinting => isSprinting;
    
    /// <summary>
    /// Проверяет ползет ли игрок в данный момент
    /// </summary>
    public bool IsCrouched => isCrouched;

    #endregion
    #endregion

    #region Head Bob

    public bool enableHeadBob = true;
    public Transform joint;
    public float bobSpeed = 10f;
    public Vector3 bobAmount = new Vector3(.15f, .05f, 0f);

    // Internal Variables
    private Vector3 jointOriginalPos;
    private float timer = 0;

    #endregion
    
    #region Unarmed Layer Animation
    
    [Header("Unarmed Layer Animation")]
    [Tooltip("Animator игрока (для управления weight слоя PickUp)")]
    public Animator playerAnimator;
    
    [Tooltip("Имя слоя PickUp в Animator Controller")]
    public string pickupLayerName = "PickUp";
    
    [Tooltip("Скорость изменения weight слоя PickUp")]
    [Range(1f, 20f)]
    public float pickupLayerTransitionSpeed = 5f;
    
    [Tooltip("GameObject оружия (wooden_dock_pole1) - скрывается/показывается при equip/unequip")]
    public GameObject weaponGameObject;
    
    // Internal Variables
    private int pickupLayerIndex = -1; // Индекс слоя PickUp
    private float targetPickupWeight = 0f; // Целевой weight (0 или 1)
    private float currentPickupWeight = 0f; // Текущий weight (плавно интерполируется)
    
    #endregion

		#region InventorySystem
		public bool enableInventory = true;
	
	[Header("Backpack State Machine (Recommended)")]
	[Tooltip("BackpackStateMachine для управления рюкзаком (анимации, blendshapes, Canvas)")]
	public BackpackStateMachine backpackStateMachine;
	
	[Tooltip("InventoryCameraController для управления камерой в инвентаре (автопоиск если не назначен)")]
	public InventoryCameraController inventoryCameraController;
	
	[Header("Legacy: Manual Canvas Control")]
	[Tooltip("Canvas инвентаря (если не используешь BackpackStateMachine)")]
		public Canvas inventoryCanvas;
	
	private bool isClosingInventory = false;
	private bool isTabClosing = false; // Флаг для TAB закрытия
	private bool isTabTransition = false; // Флаг что это TAB переход (не обычный WASD)
	
	// Сохранение состояния экипировки перед открытием инвентаря
	private bool wasEquippedBeforeInventory = false; // Было ли оружие экипировано перед открытием инвентаря
	private int equipSlotBeforeInventory = -1; // Какой слот был экипирован перед открытием инвентаря (-1 если не экипирован)
		#endregion

		#region CombatSystem

        public bool enableCombat = true;
        
		[Header("Equipment System")]
		[Tooltip("Скрипт оружия FPMAxe, который отвечает за удары/экип/аниматор")]
		public FPMAxe fpAxe;

		#endregion
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // ===== INPUT SYSTEM SETUP =====
        if (inputActions != null)
        {
            gameplayMap = inputActions.FindActionMap("Gameplay");
            combatMap = inputActions.FindActionMap("Combat");
            inventoryMap = inputActions.FindActionMap("Inventory");
            
            // Gameplay actions (всегда активны вне инвентаря)
            moveAction = gameplayMap.FindAction("Move");
            sprintAction = gameplayMap.FindAction("Sprint");
            crouchAction = gameplayMap.FindAction("Crouch");
            interactAction = gameplayMap.FindAction("Interact");
            jumpAction = gameplayMap.FindAction("Jump");
            zoomAction = gameplayMap.FindAction("Zoom");
            inventoryAction = inputActions.FindAction("Inventory") ?? gameplayMap.FindAction("Inventory");
            
            // Inventory actions
            if (inventoryMap != null)
            {
                closeInventoryAction = inventoryMap.FindAction("Close");
                exitInventoryAction = inventoryMap.FindAction("Exit");
                DraumLogger.Info(this, $"[FPC] Inventory map найдена! closeInventoryAction = {(closeInventoryAction != null ? "OK" : "NULL")}, exitInventoryAction = {(exitInventoryAction != null ? "OK" : "NULL")}");
            }
            else
            {
                DraumLogger.Error(this, "[FPC] Inventory map НЕ НАЙДЕНА в inputActions!");
            }
            
            // Combat actions (активируются при взятии оружия)
            if (combatMap != null)
            {
                primaryAttackAction = combatMap.FindAction("PrimaryAttack");
                secondaryAttackAction = combatMap.FindAction("SecondaryAttack");
                DraumLogger.Info(this, $"[FPC] Combat map найдена! PrimaryAttack = {(primaryAttackAction != null ? "OK" : "NULL")}, SecondaryAttack = {(secondaryAttackAction != null ? "OK" : "NULL")}");
            }
            else
            {
                DraumLogger.Warning(this, "[FPC] Combat map НЕ НАЙДЕНА в inputActions!");
            }
            if (gameplayMap != null)
            {
                equipFirstAction = gameplayMap.FindAction("Equip_First");
                equipSecondAction = gameplayMap.FindAction("Equip_Second");
                equipThirdAction = gameplayMap.FindAction("Equip_Third");
                DraumLogger.Info(this, $"[FPC] Equipment actions найдены! First = {(equipFirstAction != null ? "OK" : "NULL")}, Second = {(equipSecondAction != null ? "OK" : "NULL")}, Third = {(equipThirdAction != null ? "OK" : "NULL")}");
            }
            
            DraumLogger.Info(this, "[FPC] Input System инициализирован");
        }
        else
        {
            DraumLogger.Error(this, "[FPC] InputActionAsset НЕ НАЗНАЧЕН! Назначь Player_IC в Inspector!");
        }
        
        // АВТОПОИСК InventoryCameraController если не назначен
        if (inventoryCameraController == null)
        {
            inventoryCameraController = GetComponentInChildren<InventoryCameraController>();
            if (inventoryCameraController != null)
            {
                DraumLogger.Info(this, "[FPC] InventoryCameraController найден автоматически в children");
            }
            else
            {
                DraumLogger.Warning(this, "[FPC] InventoryCameraController НЕ НАЙДЕН! WASD/ALT в инвентаре не будут работать.");
            }
        }

        // Crosshair Image не используется, вместо этого создаём экземпляр Prefab
        if (crosshair && crosshairPrefab != null && crosshairInstance == null)
        {
            crosshairInstance = Instantiate(crosshairPrefab, playerCamera != null ? playerCamera.transform : transform);
            crosshairInstance.SetActive(true);
        }

        // Set internal variables
        playerCamera.fieldOfView = fov;
        originalScale = transform.localScale;
        if (joint != null)
        {
            jointOriginalPos = joint.localPosition;
        }

        if (!unlimitedSprint)
        {
            sprintRemaining = sprintDuration;
            sprintCooldownReset = sprintCooldown;
        }
        
        // Инициализация PickUp layer
        if (playerAnimator != null && !string.IsNullOrEmpty(pickupLayerName))
        {
            pickupLayerIndex = playerAnimator.GetLayerIndex(pickupLayerName);
            if (pickupLayerIndex == -1)
            {
                    DraumLogger.Warning(this, $"[FPC] Слой '{pickupLayerName}' не найден в Animator Controller! Проверь имя слоя.");
            }
        }
        else if (playerAnimator == null)
        {
                DraumLogger.Warning(this, "[FPC] playerAnimator не назначен! Назначь Animator в Inspector для работы PickUp layer.");
        }
    }
    
    private void OnEnable()
    {
        if (gameplayMap != null)
        {
            gameplayMap.Enable();
            DraumLogger.Info(this, "[FPC] Gameplay map ENABLED");
        }
        
        // Combat map НЕ включаем по умолчанию (только при взятии оружия)
    }
    
    private void OnDisable()
    {
        if (gameplayMap != null)
        {
            gameplayMap.Disable();
        }
        if (combatMap != null)
        {
            combatMap.Disable();
        }
        if (inventoryMap != null)
        {
            inventoryMap.Disable();
        }
    }

    void Start()
    {
        if (showCameraDebugLogs)
        {
            Debug.Log($"[FPC Camera] 🎬 Start(): lockCursor={lockCursor}, cameraCanMove={cameraCanMove}, playerCanMove={playerCanMove}, playerCamera={playerCamera != null}");
        }
        
        // Инициализируем yaw и pitch из текущей rotation
        if (transform != null)
        {
            yaw = transform.localEulerAngles.y;
        }
        if (playerCamera != null || cameraPivot != null)
        {
            Transform pitchSource = cameraPivot != null ? cameraPivot : (playerCamera != null ? playerCamera.transform : null);
            if (pitchSource != null)
            {
                pitch = pitchSource.localEulerAngles.x;
                if (pitch > 180f) pitch -= 360f;
            }
        }
        
        if(lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            if (showCameraDebugLogs)
            {
                Debug.Log($"[FPC Camera] 🔒 Блокируем курсор при старте: Cursor.lockState={Cursor.lockState}");
            }
        }

        // Управление кастомным курсором
        if (crosshair && crosshairInstance != null)
        {
            crosshairInstance.SetActive(true);
        }
        else if (crosshairInstance != null)
        {
            crosshairInstance.SetActive(false);
        }

        #region Sprint Bar

        sprintBarCG = GetComponentInChildren<CanvasGroup>();

        if(useSprintBar)
        {
            sprintBarBG.gameObject.SetActive(true);
            sprintBar.gameObject.SetActive(true);

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            sprintBarWidth = screenWidth * sprintBarWidthPercent;
            sprintBarHeight = screenHeight * sprintBarHeightPercent;

            sprintBarBG.rectTransform.sizeDelta = new Vector3(sprintBarWidth, sprintBarHeight, 0f);
            sprintBar.rectTransform.sizeDelta = new Vector3(sprintBarWidth - 2, sprintBarHeight - 2, 0f);

            if(hideBarWhenFull)
            {
                sprintBarCG.alpha = 0;
            }
        }
        else
        {
            sprintBarBG.gameObject.SetActive(false);
            sprintBar.gameObject.SetActive(false);
        }

        #endregion
    }

    [Header("Debug - Camera Control")]
    [Tooltip("Показывать детальные логи управления камерой (для диагностики)")]
    public bool showCameraDebugLogs = false;
    private float lastCameraLogTime = 0f;
    private const float CAMERA_LOG_INTERVAL = 0.5f;
    
    // Camera input values (обновляются в Update, применяются в LateUpdate для плавности)
    private float mouseXInput = 0f;
    private float mouseYInput = 0f;
    
    private void Update()
    {
        #region Camera Input Processing

        // Проверяем и восстанавливаем блокировку курсора если нужно
        if (lockCursor && Cursor.lockState != CursorLockMode.Locked && cameraCanMove && playerCanMove)
        {
            if (showCameraDebugLogs)
            {
                Debug.Log($"[FPC Camera] ⚠️ Восстанавливаем блокировку курсора! lockCursor={lockCursor}, Cursor.lockState={Cursor.lockState}, cameraCanMove={cameraCanMove}, playerCanMove={playerCanMove}");
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Обрабатываем ввод мыши (сохраняем для применения в LateUpdate)
        if(cameraCanMove && Cursor.lockState == CursorLockMode.Locked)
        {
            mouseXInput = Input.GetAxis("Mouse X");
            mouseYInput = Input.GetAxis("Mouse Y");
            
            // Логируем только когда есть движение мыши
            if (showCameraDebugLogs && (Mathf.Abs(mouseXInput) > 0.001f || Mathf.Abs(mouseYInput) > 0.001f))
            {
                Debug.Log($"[FPC Camera] ✅ Ввод мыши: mouseX={mouseXInput:F3}, mouseY={mouseYInput:F3}");
            }
        }
        else
        {
            mouseXInput = 0f;
            mouseYInput = 0f;
            
            // Логируем состояние
            if (showCameraDebugLogs && Time.time - lastCameraLogTime > CAMERA_LOG_INTERVAL)
            {
                lastCameraLogTime = Time.time;
                Debug.LogWarning($"[FPC Camera] ❌ Камера заблокирована: cameraCanMove={cameraCanMove}, Cursor.lockState={Cursor.lockState}, playerCanMove={playerCanMove}");
            }
        }

        // Периодическое логирование состояния
        if (showCameraDebugLogs && Time.time - lastCameraLogTime > CAMERA_LOG_INTERVAL)
        {
            lastCameraLogTime = Time.time;
            bool hasMouseInput = Mathf.Abs(mouseXInput) > 0.001f || Mathf.Abs(mouseYInput) > 0.001f;
            Debug.Log($"[FPC Camera] 📊 Состояние: cameraCanMove={cameraCanMove}, playerCanMove={playerCanMove}, lockCursor={lockCursor}, Cursor.lockState={Cursor.lockState}, mouseInput={hasMouseInput}");
        }

        #region Camera Zoom

        if (enableZoom && zoomAction != null)
        {
            // Changes isZoomed when key is pressed
            // Behavior for toogle zoom
            if(zoomAction.WasPressedThisFrame() && !holdToZoom && !isSprinting)
            {
                if (!isZoomed)
                {
                    isZoomed = true;
                }
                else
                {
                    isZoomed = false;
                }
            }

            // Changes isZoomed when key is pressed
            // Behavior for hold to zoom
            if(holdToZoom && !isSprinting)
            {
                if(zoomAction.WasPressedThisFrame())
                {
                    isZoomed = true;
                }
                else if(zoomAction.WasReleasedThisFrame())
                {
                    isZoomed = false;
                }
            }

            // Проверяем, не открыт ли инвентарь - если открыт, не меняем FOV (управляет InventoryCameraController)
            bool isInventoryOpen = false;
            if (backpackStateMachine != null)
            {
                Canvas canvas = backpackStateMachine.inventoryCanvas;
                isInventoryOpen = canvas != null && canvas.gameObject.activeSelf;
            }
            else if (inventoryCanvas != null)
            {
                isInventoryOpen = inventoryCanvas.gameObject.activeSelf;
            }
            
            // НЕ меняем FOV если инвентарь открыт (InventoryCameraController управляет FOV)
            if (!isInventoryOpen)
            {
                // Lerps camera.fieldOfView to allow for a smooth transistion
                if(isZoomed)
                {
                    playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, zoomFOV, zoomStepTime * Time.deltaTime);
                }
                else if(!isZoomed && !isSprinting)
                {
                    playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, fov, zoomStepTime * Time.deltaTime);
                }
            }
        }

        #endregion
        #endregion

        #region Sprint

        if(enableSprint)
        {
            // Проверяем, не открыт ли инвентарь - если открыт, не меняем FOV (управляет InventoryCameraController)
            bool isInventoryOpen = false;
            if (backpackStateMachine != null)
            {
                Canvas canvas = backpackStateMachine.inventoryCanvas;
                isInventoryOpen = canvas != null && canvas.gameObject.activeSelf;
            }
            else if (inventoryCanvas != null)
            {
                isInventoryOpen = inventoryCanvas.gameObject.activeSelf;
            }
            
            // Получаем параметры из конфига или напрямую
            float currentSprintFOV = movementConfig != null ? movementConfig.sprintFOV : sprintFOV;
            bool useSmoothFOV = movementConfig != null ? movementConfig.useSmoothSprintFOV : useSmoothSprintFOV;
            float fovTransitionTime = movementConfig != null ? movementConfig.sprintFOVTransitionTime : sprintFOVTransitionTime;
            
            // НЕ меняем FOV если инвентарь открыт (InventoryCameraController управляет FOV)
            if (!isInventoryOpen)
            {
                if(isSprinting)
                {
                    isZoomed = false;
                    
                    // ПЛАВНОЕ ИЗМЕНЕНИЕ FOV при спринте
                    if (useSmoothFOV)
                    {
                        // Целевой multiplier = 1 (sprint FOV)
                        targetFOVMultiplier = 1f;
                        
                        // Плавно интерполируем
                        float transitionRate = 1f / fovTransitionTime;
                        currentFOVMultiplier = Mathf.MoveTowards(currentFOVMultiplier, targetFOVMultiplier, transitionRate * Time.deltaTime);
                        
                        // Применяем FOV
                        playerCamera.fieldOfView = Mathf.Lerp(fov, currentSprintFOV, currentFOVMultiplier);
                    }
                    else
                    {
                        // Старый метод (резкое изменение)
                        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, currentSprintFOV, sprintFOVStepTime * Time.deltaTime);
                    }
                }
                else
                {
                    // НЕ СПРИНТИМ - возвращаем FOV к normal
                    if (useSmoothFOV)
                    {
                        // Целевой multiplier = 0 (normal FOV)
                        targetFOVMultiplier = 0f;
                        
                        // Плавно интерполируем
                        float transitionRate = 1f / fovTransitionTime;
                        currentFOVMultiplier = Mathf.MoveTowards(currentFOVMultiplier, targetFOVMultiplier, transitionRate * Time.deltaTime);
                        
                        // Применяем FOV
                        playerCamera.fieldOfView = Mathf.Lerp(fov, currentSprintFOV, currentFOVMultiplier);
                    }
                    else
                    {
                        // Старый метод
                        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, fov, sprintFOVStepTime * Time.deltaTime);
                    }
                }
            }
            
            // Обработка спринта (независимо от FOV)
            if(isSprinting)
            {
                // Drain sprint remaining while sprinting
                if(!unlimitedSprint)
                {
                    sprintRemaining -= 1 * Time.deltaTime;
                    if (sprintRemaining <= 0)
                    {
                        isSprinting = false;
                        isSprintCooldown = true;
                    }
                }
            }
            else
            {
                // Regain sprint while not sprinting
                sprintRemaining = Mathf.Clamp(sprintRemaining += 1 * Time.deltaTime, 0, sprintDuration);
            }

            // Handles sprint cooldown 
            // When sprint remaining == 0 stops sprint ability until hitting cooldown
            if(isSprintCooldown)
            {
                sprintCooldown -= 1 * Time.deltaTime;
                if (sprintCooldown <= 0)
                {
                    isSprintCooldown = false;
                }
            }
            else
            {
                sprintCooldown = sprintCooldownReset;
            }

            // Handles sprintBar 
            if(useSprintBar && !unlimitedSprint)
            {
                float sprintRemainingPercent = sprintRemaining / sprintDuration;
                sprintBar.transform.localScale = new Vector3(sprintRemainingPercent, 1f, 1f);
            }
        }

        #endregion

        #region Jump

        // Gets input and calls jump method
        if(enableJump && jumpAction != null && jumpAction.WasPressedThisFrame() && isGrounded)
        {
            Jump();
        }

        #endregion

        #region Crouch

        if (enableCrouch && crouchAction != null)
        {
            if(crouchAction.WasPressedThisFrame() && !holdToCrouch)
            {
                Crouch();
            }
            
            if(crouchAction.WasPressedThisFrame() && holdToCrouch)
            {
                isCrouched = false;
                Crouch();
            }
            else if(crouchAction.WasReleasedThisFrame() && holdToCrouch)
            {
                isCrouched = true;
                Crouch();
            }
        }

        #endregion
				#region Interaction
				HandleInteraction();
				#endregion
				
				#region InventorySystem
				InventorySystem();
				#endregion
				
				#region PickUp Layer Animation
				UpdatePickupLayerWeight();
				#endregion
				
				#region EquipmentSystem
				HandleEquipmentInput();
				#endregion
				
        CheckGround();

        if(enableHeadBob)
        {
            HeadBob();
        }
    }
    
    /// <summary>
    /// Управление камерой в LateUpdate для более плавного движения
    /// Выполняется ПОСЛЕ всех Update(), что исключает конфликты с другими скриптами
    /// </summary>
    private     void LateUpdate()
    {
        #region Camera Rotation Apply
        
        // Применяем rotation камеры в LateUpdate для плавности (после всех Update)
        if (cameraCanMove && Cursor.lockState == CursorLockMode.Locked && playerCamera != null)
        {
            // Обновляем yaw и pitch на основе сохраненного ввода мыши из Update()
            yaw += mouseXInput * mouseSensitivity;

            if (!invertCamera)
            {
                pitch -= mouseSensitivity * mouseYInput;
            }
            else
            {
                pitch += mouseSensitivity * mouseYInput;
            }

            // Применяем подёргивания камеры (если есть)
            float finalYaw = yaw + cameraShakeOffset.x;
            float finalPitch = pitch + cameraShakeOffset.y;

            // Clamp pitch between lookAngle (учитываем подёргивания)
            finalPitch = Mathf.Clamp(finalPitch, -maxLookAngle, maxLookAngle);
            
            // Pitch: если есть cameraPivot — крутим его (камера с Animator остаётся дочерней и анимации работают). Иначе — крутим саму камеру.
            if (cameraPivot != null)
                cameraPivot.localEulerAngles = new Vector3(finalPitch, 0, 0);
            else
                playerCamera.transform.localEulerAngles = new Vector3(finalPitch, 0, 0);
            
            // ПРИМЕЧАНИЕ: Поворот тела игрока (yaw) применяется в FixedUpdate через rb.MoveRotation
            // для корректной работы с физикой
        }
        
        #endregion
    }

    void FixedUpdate()
    {
        #region Player Rotation (Rigidbody)
        
        // Применяем rotation тела игрока через Rigidbody в FixedUpdate (для корректной работы с физикой)
        if (cameraCanMove && Cursor.lockState == CursorLockMode.Locked && rb != null)
        {
            // Используем rb.MoveRotation для плавного поворота через физику
            // yaw обновляется в LateUpdate, здесь мы только применяем его
            Quaternion targetRotation = Quaternion.Euler(0, yaw, 0);
            rb.MoveRotation(targetRotation);
        }
        else if (cameraCanMove && Cursor.lockState == CursorLockMode.Locked && rb == null)
        {
            // Fallback если нет Rigidbody - используем transform напрямую (с учетом подёргиваний)
            float finalYaw = yaw + cameraShakeOffset.x;
            transform.localEulerAngles = new Vector3(0, finalYaw, 0);
        }
        
        #endregion
        
        #region Movement

        if (playerCanMove)
        {
            // Calculate how fast we should be moving (INPUT SYSTEM)
            Vector2 moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            Vector3 targetVelocity = new Vector3(moveInput.x, 0, moveInput.y);
            
            // Определяем есть ли движение
            bool isMoving = targetVelocity.magnitude > 0.01f;

            // Checks if player is walking and isGrounded
            // Will allow head bob
            if (isMoving && isGrounded)
            {
                isWalking = true;
            }
            else
            {
                isWalking = false;
            }

            // Получаем параметры из конфига или напрямую
            float currentWalkSpeed = movementConfig != null ? movementConfig.walkSpeed : walkSpeed;
            float currentSprintSpeed = movementConfig != null ? movementConfig.sprintSpeed : sprintSpeed;
            float currentMaxVelocityChange = movementConfig != null ? movementConfig.maxVelocityChange : maxVelocityChange;
            bool useAccel = movementConfig != null ? movementConfig.useAcceleration : useAcceleration;
            float accelTime = movementConfig != null ? movementConfig.accelerationTime : accelerationTime;
            float decelTime = movementConfig != null ? movementConfig.decelerationTime : decelerationTime;

            // All movement calculations shile sprint is active (INPUT SYSTEM)
            bool sprintPressed = sprintAction != null && sprintAction.IsPressed();
            bool isSprinting = enableSprint && sprintPressed && sprintRemaining > 0f && !isSprintCooldown;
            
            // SPRINT ACCELERATION / DECELERATION
            bool useSprintAccel = movementConfig != null ? movementConfig.useSprintAcceleration : useSprintAcceleration;
            float sprintAccelTime = movementConfig != null ? movementConfig.sprintAccelerationTime : sprintAccelerationTime;
            float sprintDecelTime = movementConfig != null ? movementConfig.sprintDecelerationTime : sprintDecelerationTime;
            
            if (useSprintAccel)
            {
                // Целевой multiplier: 1 если спринтим, 0 если нет
                targetSprintMultiplier = isSprinting ? 1f : 0f;
                
                // Плавно интерполируем
                if (currentSprintMultiplier < targetSprintMultiplier)
                {
                    // Ускорение спринта (walkSpeed → sprintSpeed)
                    float sprintAccelRate = 1f / sprintAccelTime;
                    currentSprintMultiplier = Mathf.MoveTowards(currentSprintMultiplier, targetSprintMultiplier, sprintAccelRate * Time.fixedDeltaTime);
                    
                    // Применяем кривую если есть
                    if (movementConfig != null && movementConfig.sprintAccelerationCurve != null)
                    {
                        currentSprintMultiplier = movementConfig.sprintAccelerationCurve.Evaluate(currentSprintMultiplier);
                    }
                }
                else if (currentSprintMultiplier > targetSprintMultiplier)
                {
                    // Замедление спринта (sprintSpeed → walkSpeed)
                    float sprintDecelRate = 1f / sprintDecelTime;
                    currentSprintMultiplier = Mathf.MoveTowards(currentSprintMultiplier, targetSprintMultiplier, sprintDecelRate * Time.fixedDeltaTime);
                    
                    // Применяем кривую если есть
                    if (movementConfig != null && movementConfig.sprintDecelerationCurve != null)
                    {
                        currentSprintMultiplier = movementConfig.sprintDecelerationCurve.Evaluate(currentSprintMultiplier);
                    }
                }
            }
            else
            {
                // Без плавного ускорения спринта (мгновенное переключение)
                currentSprintMultiplier = isSprinting ? 1f : 0f;
            }
            
            // Выбираем целевую скорость с учётом sprint acceleration
            float targetSpeed = Mathf.Lerp(currentWalkSpeed, currentSprintSpeed, currentSprintMultiplier);
            
            // ПЛАВНОЕ УСКОРЕНИЕ / ЗАМЕДЛЕНИЕ
            if (useAccel)
            {
                // Определяем целевой multiplier (0 или 1)
                targetSpeedMultiplier = isMoving ? 1f : 0f;
                
                // Плавно интерполируем
                if (currentSpeedMultiplier < targetSpeedMultiplier)
                {
                    // Ускорение
                    float accelRate = 1f / accelTime;
                    currentSpeedMultiplier = Mathf.MoveTowards(currentSpeedMultiplier, targetSpeedMultiplier, accelRate * Time.fixedDeltaTime);
                    
                    // Применяем кривую если есть в конфиге
                    if (movementConfig != null && movementConfig.accelerationCurve != null)
                    {
                        currentSpeedMultiplier = movementConfig.accelerationCurve.Evaluate(currentSpeedMultiplier);
                    }
                }
                else if (currentSpeedMultiplier > targetSpeedMultiplier)
                {
                    // Замедление
                    float decelRate = 1f / decelTime;
                    currentSpeedMultiplier = Mathf.MoveTowards(currentSpeedMultiplier, targetSpeedMultiplier, decelRate * Time.fixedDeltaTime);
                    
                    // Применяем кривую если есть в конфиге
                    if (movementConfig != null && movementConfig.decelerationCurve != null)
                    {
                        currentSpeedMultiplier = movementConfig.decelerationCurve.Evaluate(currentSpeedMultiplier);
                    }
                }
                
                // Применяем multiplier к скорости
                targetSpeed *= currentSpeedMultiplier;
            }
            
            if (isSprinting)
            {
                targetVelocity = transform.TransformDirection(targetVelocity) * targetSpeed;

                // Apply a force that attempts to reach our target velocity
                Vector3 velocity = rb.linearVelocity;
                Vector3 velocityChange = (targetVelocity - velocity);
                velocityChange.x = Mathf.Clamp(velocityChange.x, -currentMaxVelocityChange, currentMaxVelocityChange);
                velocityChange.z = Mathf.Clamp(velocityChange.z, -currentMaxVelocityChange, currentMaxVelocityChange);
                velocityChange.y = 0;

                // Player is only moving when valocity change != 0
                // Makes sure fov change only happens during movement
                if (velocityChange.x != 0 || velocityChange.z != 0)
                {
                    this.isSprinting = true;

                    if (isCrouched)
                    {
                        Crouch();
                    }

                    if (hideBarWhenFull && !unlimitedSprint)
                    {
                        sprintBarCG.alpha += 5 * Time.deltaTime;
                    }
                }

                rb.AddForce(velocityChange, ForceMode.VelocityChange);
            }
            // All movement calculations while walking
            else
            {
                this.isSprinting = false;

                if (hideBarWhenFull && sprintRemaining == sprintDuration)
                {
                    sprintBarCG.alpha -= 3 * Time.deltaTime;
                }

                targetVelocity = transform.TransformDirection(targetVelocity) * targetSpeed;

                // Apply a force that attempts to reach our target velocity
                Vector3 velocity = rb.linearVelocity;
                Vector3 velocityChange = (targetVelocity - velocity);
                velocityChange.x = Mathf.Clamp(velocityChange.x, -currentMaxVelocityChange, currentMaxVelocityChange);
                velocityChange.z = Mathf.Clamp(velocityChange.z, -currentMaxVelocityChange, currentMaxVelocityChange);
                velocityChange.y = 0;

                rb.AddForce(velocityChange, ForceMode.VelocityChange);
            }
        }
        
        // PHYSICS: Linear Damping в зависимости от grounded
        UpdateLinearDamping();

        #endregion
    }
    
    /// <summary>
    /// Обновляет Linear Damping в зависимости от grounded состояния
    /// </summary>
    private void UpdateLinearDamping()
    {
        // Получаем параметры из конфига или напрямую
        float targetGroundedDamping = movementConfig != null ? movementConfig.groundedLinearDamping : groundedLinearDamping;
        float targetAirborneDamping = movementConfig != null ? movementConfig.airborneLinearDamping : airborneLinearDamping;
        float changeSpeed = movementConfig != null ? movementConfig.dampingChangeSpeed : dampingChangeSpeed;
        
        // Целевое значение damping
        float targetDamping = isGrounded ? targetGroundedDamping : targetAirborneDamping;
        
        // Плавно интерполируем
        rb.linearDamping = Mathf.Lerp(rb.linearDamping, targetDamping, changeSpeed * Time.fixedDeltaTime);
    }

    // Sets isGrounded based on a raycast sent straigth down from the player object
    private void CheckGround()
    {
        Vector3 origin = new Vector3(transform.position.x, transform.position.y - (transform.localScale.y * .5f), transform.position.z);
        Vector3 direction = transform.TransformDirection(Vector3.down);
        float distance = .75f;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, distance))
        {
            Debug.DrawRay(origin, direction * distance, Color.red);
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    private void Jump()
    {
        // Adds force to the player rigidbody to jump
        if (isGrounded)
        {
            rb.AddForce(0f, jumpPower, 0f, ForceMode.Impulse);
            isGrounded = false;
        }

        // When crouched and using toggle system, will uncrouch for a jump
        if(isCrouched && !holdToCrouch)
        {
            Crouch();
        }
    }

    private void Crouch()
    {
        // Stands player up to full height
        // Brings walkSpeed back up to original speed
        if(isCrouched)
        {
            transform.localScale = new Vector3(originalScale.x, originalScale.y, originalScale.z);
            walkSpeed /= speedReduction;

            isCrouched = false;
        }
        // Crouches player down to set height
        // Reduces walkSpeed
        else
        {
            transform.localScale = new Vector3(originalScale.x, crouchHeight, originalScale.z);
            walkSpeed *= speedReduction;

            isCrouched = true;
        }
    }

    private void HeadBob()
    {
        if(isWalking)
        {
            // Calculates HeadBob speed during sprint
            if(isSprinting)
            {
                timer += Time.deltaTime * (bobSpeed + sprintSpeed);
            }
            // Calculates HeadBob speed during crouched movement
            else if (isCrouched)
            {
                timer += Time.deltaTime * (bobSpeed * speedReduction);
            }
            // Calculates HeadBob speed during walking
            else
            {
                timer += Time.deltaTime * bobSpeed;
            }
            // Applies HeadBob movement
            joint.localPosition = new Vector3(jointOriginalPos.x + Mathf.Sin(timer) * bobAmount.x, jointOriginalPos.y + Mathf.Sin(timer) * bobAmount.y, jointOriginalPos.z + Mathf.Sin(timer) * bobAmount.z);
        }
        else
        {
            // Resets when play stops moving
            timer = 0;
            joint.localPosition = new Vector3(Mathf.Lerp(joint.localPosition.x, jointOriginalPos.x, Time.deltaTime * bobSpeed), Mathf.Lerp(joint.localPosition.y, jointOriginalPos.y, Time.deltaTime * bobSpeed), Mathf.Lerp(joint.localPosition.z, jointOriginalPos.z, Time.deltaTime * bobSpeed));
        }
    }

		private void InventorySystem()
		{
			if (!enableInventory || isClosingInventory)
			{
					return;
			}
			
			// Определяем открыт ли инвентарь
			bool isInventoryOpen = false;
			
			if (backpackStateMachine != null)
			{
					// Используем BackpackStateMachine
					Canvas canvas = backpackStateMachine.inventoryCanvas;
					isInventoryOpen = canvas != null && canvas.gameObject.activeSelf;
			}
			else if (inventoryCanvas != null)
			{
					// Legacy: используем прямой Canvas
					isInventoryOpen = inventoryCanvas.gameObject.activeSelf;
			}
			else
			{
					Debug.LogError("[FPC] Ни BackpackStateMachine, ни inventoryCanvas не назначены!");
					return;
			}
			
		// ОТКРЫТЬ инвентарь (TAB из Gameplay map)
		if (!isInventoryOpen && inventoryAction != null && inventoryAction.WasPressedThisFrame())
		{
				// ОСТАНАВЛИВАЕМ ДВИЖЕНИЕ ИГРОКА (убираем headbob)
				if (rb != null)
				{
						rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0); // Сохраняем только Y (гравитация)
				}
				isWalking = false; // Останавливаем headbob
				
				// Сохраняем состояние экипировки перед открытием инвентаря
				if (fpAxe != null)
				{
					wasEquippedBeforeInventory = fpAxe.IsEquipped();
					equipSlotBeforeInventory = wasEquippedBeforeInventory ? fpAxe.GetCurrentEquipSlot() : -1;
					Debug.Log($"[FPC] Открытие инвентаря: сохранено состояние экипировки - было экипировано={wasEquippedBeforeInventory}, слот={equipSlotBeforeInventory}");
				}
				else
				{
					wasEquippedBeforeInventory = false;
					equipSlotBeforeInventory = -1;
				}
				
				// Проверяем, экипировано ли оружие - если да, сначала проигрываем Unequip анимацию
				if (fpAxe != null && fpAxe.IsEquipped())
				{
						StartCoroutine(OpenInventoryAfterUnequip());
				}
				else
				{
						// Оружие не экипировано - открываем инвентарь сразу
						OpenInventoryDirectly();
				}
		}
		// ЗАКРЫТЬ инвентарь (TAB из Inventory map)
		else if (isInventoryOpen)
		{
				// Проверяем состояние closeInventoryAction
				if (closeInventoryAction == null)
				{
						Debug.LogError("[FPC] closeInventoryAction == NULL! Проверь Inventory map!");
						return;
				}
				
				if (!inventoryMap.enabled)
				{
						Debug.LogError("[FPC] Inventory map НЕ АКТИВНА! Инвентарь открыт но map выключена!");
						return;
				}
				
				// TAB - закрытие инвентаря с правильной логикой
				if (closeInventoryAction.WasPressedThisFrame() && !isTabClosing)
				{
						// Устанавливаем флаги TAB закрытия
						isTabClosing = true;
						isTabTransition = true; // Это TAB переход, не обычный WASD
						
						if (inventoryCameraController != null)
						{
								// Если НЕ в Closed секции - сначала переходим в Closed, потом закрываем
								if (inventoryCameraController.currentSection != InventoryCameraController.InventorySection.Closed)
								{
										// Если в Special секции, используем Blink эффект
										if (inventoryCameraController.currentSection == InventoryCameraController.InventorySection.Special)
										{
												inventoryCameraController.StartEyeBlinkFromSpecial();
						}
						else
						{
												// Для других секций используем обычный переход
												inventoryCameraController.TransitionToSection(InventoryCameraController.InventorySection.Closed);
										}
								}
								else
								{
										// Уже в Closed секции - сразу закрываем инвентарь
										isTabTransition = false; // Сбрасываем флаг
										if (backpackStateMachine != null)
										{
												StartCoroutine(CloseInventoryWithBackpackStateMachine());
										}
										else
										{
												StartCoroutine(CloseInventoryWithAnimation());
										}
								}
						}
						else
						{
								Debug.LogWarning("[FPC] inventoryCameraController == null! TAB не работает");
								isTabClosing = false; // Сбрасываем флаг если ошибка
								isTabTransition = false;
						}
				}
				
				// ESC - полное закрытие инвентаря
				if (exitInventoryAction != null && exitInventoryAction.WasPressedThisFrame())
				{
						Debug.Log($"[FPC] ESC нажат! Полное закрытие инвентаря");
						
						// ИСПОЛЬЗУЕМ BackpackStateMachine (если есть)
						if (backpackStateMachine != null)
						{
								StartCoroutine(CloseInventoryWithBackpackStateMachine());
						}
						else
						{
								// Legacy: закрываем напрямую
								StartCoroutine(CloseInventoryWithAnimation());
						}
				}
		}
	}
	
	/// <summary>
	/// Открывает инвентарь после завершения анимации Unequip (если оружие экипировано)
	/// </summary>
	private IEnumerator OpenInventoryAfterUnequip()
	{
		if (fpAxe == null)
		{
			OpenInventoryDirectly();
			yield break;
		}
		
		// Флаг для отслеживания завершения unequip
		bool unequipFinished = false;
		
		// Запускаем unequip анимацию с callback для открытия инвентаря
		fpAxe.UnequipForInventory(() =>
		{
			unequipFinished = true;
			Debug.Log("[FPC] Unequip callback вызван - анимация завершена, открываем инвентарь");
		});
		
		Debug.Log("[FPC] Ожидаем завершения Unequip анимации (через корутину с длительностью) перед открытием инвентаря");
		
		// Ждем пока callback не будет вызван (после завершения анимации через корутину)
		while (!unequipFinished)
		{
			yield return null;
		}
		
		// Небольшая задержка для завершения всех операций
		yield return new WaitForSeconds(0.1f);
		
		// Теперь открываем инвентарь
		OpenInventoryDirectly();
	}
	
	/// <summary>
	/// Открывает инвентарь напрямую (основная логика открытия)
	/// </summary>
	private void OpenInventoryDirectly()
	{
		// ПЛАВНЫЙ ПЕРЕХОД КАМЕРЫ К CENTER ПРИ ОТКРЫТИИ
		StartCoroutine(SmoothCameraToCenter());
		
		// ИСПОЛЬЗУЕМ BackpackStateMachine (если есть)
		if (backpackStateMachine != null)
		{
			backpackStateMachine.OpenBackpack();
		}
		else if (inventoryCanvas != null)
		{
			// Legacy: просто включаем Canvas
			inventoryCanvas.gameObject.SetActive(true);
		}
		
		// АКТИВИРУЕМ InventoryCameraController
		if (inventoryCameraController != null)
		{
			inventoryCameraController.Activate();
			Debug.Log("[FPC] InventoryCameraController.Activate() вызван");
		}
		else
		{
			Debug.LogWarning("[FPC] InventoryCameraController НЕ НАЗНАЧЕН! WASD/ALT не будут работать в инвентаре.");
		}
		
		// ИНВЕНТАРЬ ОТКРЫТ
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		playerCanMove = false; // БЛОКИРУЕМ ДВИЖЕНИЕ WASD
		cameraCanMove = false; // БЛОКИРУЕМ КАМЕРУ
		
		// ПЕРЕКЛЮЧАЕМ на Inventory Action Map
		SwitchToInventoryMap();
		
		Debug.Log("[FPC] Инвентарь ОТКРЫТ");
	}
	
	/// <summary>
	/// Плавный переход камеры к Closed позиции при открытии инвентаря
	/// </summary>
	private IEnumerator SmoothCameraToCenter()
	{
		if (inventoryCameraController == null) yield break;
		
		// Получаем Closed позицию (первая секция)
		Vector3 closedPosition = inventoryCameraController.closedSectionPosition;
		Vector3 closedRotation = inventoryCameraController.closedSectionRotation;
		
		// Сохраняем текущую позицию камеры
		Vector3 startPosition = playerCamera.transform.localPosition;
		Vector3 startRotation = playerCamera.transform.localEulerAngles;
		
		// ФАЗА 1: Сначала сбрасываем rotation к 0 (0.5 секунды)
		float resetDuration = 0.5f;
		float resetTimer = 0f;
		Vector3 zeroRotation = Vector3.zero;
		
		while (resetTimer < resetDuration)
		{
			resetTimer += Time.deltaTime;
			float progress = resetTimer / resetDuration;
			
			// Плавно сбрасываем rotation к 0, используя правильную интерполяцию углов
			Vector3 resetRot = Vector3.Lerp(startRotation, zeroRotation, progress);
			
			// Нормализуем углы для правильной интерполяции
			resetRot.x = Mathf.LerpAngle(startRotation.x, zeroRotation.x, progress);
			resetRot.y = Mathf.LerpAngle(startRotation.y, zeroRotation.y, progress);
			resetRot.z = Mathf.LerpAngle(startRotation.z, zeroRotation.z, progress);
			
			// Применяем только rotation (позицию не трогаем)
			playerCamera.transform.localEulerAngles = resetRot;
			
			yield return null;
		}
		
		// ФАЗА 2: Теперь переходим к Closed позиции (0.5 секунды)
		float moveDuration = 0.5f;
		float moveTimer = 0f;
		Vector3 currentPos = startPosition;
		Vector3 currentRot = zeroRotation;
		
		while (moveTimer < moveDuration)
		{
			moveTimer += Time.deltaTime;
			float progress = moveTimer / moveDuration;
			
			// Плавная интерполяция к Closed позиции
			currentPos = Vector3.Lerp(startPosition, closedPosition, progress);
			
			// Правильная интерполяция углов
			currentRot.x = Mathf.LerpAngle(zeroRotation.x, closedRotation.x, progress);
			currentRot.y = Mathf.LerpAngle(zeroRotation.y, closedRotation.y, progress);
			currentRot.z = Mathf.LerpAngle(zeroRotation.z, closedRotation.z, progress);
			
			// Применяем к камере
			playerCamera.transform.localPosition = currentPos;
			playerCamera.transform.localEulerAngles = currentRot;
			
			yield return null;
		}
		
		// Устанавливаем точную позицию
		playerCamera.transform.localPosition = closedPosition;
		playerCamera.transform.localEulerAngles = closedRotation;
		
		Debug.Log("[FPC] Камера плавно переведена к Closed позиции (rotation сброшен)");
	}
	
	/// <summary>
	/// Закрывает инвентарь через BackpackStateMachine (рекомендуемый метод)
	/// </summary>
	private IEnumerator CloseInventoryWithBackpackStateMachine()
	{
			isClosingInventory = true;
			
		// СНАЧАЛА переходим в Closed секцию из любой секции
		if (inventoryCameraController != null)
		{
			Debug.Log($"[FPC] Начинаем закрытие. Текущая секция: {inventoryCameraController.currentSection}");
			
			// Если мы НЕ в Closed секции, переходим туда
			if (inventoryCameraController.currentSection != InventoryCameraController.InventorySection.Closed)
			{
				Debug.Log($"[FPC] Закрытие из {inventoryCameraController.currentSection} секции - сначала переход в Closed");
				
				// Если в Special секции, используем Blink эффект
				if (inventoryCameraController.currentSection == InventoryCameraController.InventorySection.Special)
				{
					Debug.Log("[FPC] Special секция - используем Blink эффект");
					inventoryCameraController.StartEyeBlinkFromSpecial();
					yield return new WaitUntil(() => !inventoryCameraController.isEyeBlinking);
					Debug.Log("[FPC] Blink эффект завершён");
				}
				else
				{
					// Для других секций используем плавный переход как при WASD
					Debug.Log($"[FPC] Плавный переход из {inventoryCameraController.currentSection} в Closed (как при W)");
					
					// Устанавливаем цель - Closed секция (как при нажатии W)
					inventoryCameraController.targetSection = InventoryCameraController.InventorySection.Closed;
					
					// Ждём завершения плавного перехода (камера + blendshapes + grids)
					float transitionTimeout = 5f; // Таймаут на случай зависания
					float transitionTimer = 0f;
					
					while (inventoryCameraController.currentSection != InventoryCameraController.InventorySection.Closed && transitionTimer < transitionTimeout)
					{
						transitionTimer += Time.deltaTime;
						yield return null;
					}
					
					if (transitionTimer >= transitionTimeout)
					{
						Debug.LogWarning("[FPC] Таймаут перехода в Closed! Принудительно завершаем");
						// Принудительно завершаем если зависло
						inventoryCameraController.currentSection = InventoryCameraController.InventorySection.Closed;
					}
					
					Debug.Log($"[FPC] Плавный переход в Closed завершён за {transitionTimer:F2} сек");
				}
				
				Debug.Log("[FPC] Переход в Closed секцию завершён, продолжаем закрытие");
			}
			else
			{
				Debug.Log("[FPC] Уже в Closed секции, продолжаем закрытие");
			}
		}
		else
		{
			Debug.LogWarning("[FPC] inventoryCameraController == null! Пропускаем переход в Closed");
		}
			
			// ДЕАКТИВИРУЕМ InventoryCameraController
			if (inventoryCameraController != null)
			{
					inventoryCameraController.Deactivate();
			}
			
			// ОТМЕНЯЕМ перемещение предмета если он был выбран
			Canvas canvas = backpackStateMachine.inventoryCanvas;
			if (canvas != null)
			{
					Inventory inventory = canvas.GetComponentInChildren<Inventory>();
					if (inventory != null && inventory.selectedItem != null)
					{
							Debug.Log("[FPC] Инвентарь закрыт с выбранным предметом - отменяем перемещение");
							inventory.CancelItemMove();
					}
			}
			
		// Запускаем закрытие через BackpackStateMachine
		backpackStateMachine.CloseBackpack();
		
		// Ждём завершения закрытия (blendshape + TakeOn анимация)
		float timeout = 10f; // Таймаут на случай зависания
		float timer = 0f;
		
		while (!backpackStateMachine.IsClosingComplete())
		{
				timer += Time.deltaTime;
				if (timer > timeout)
				{
						Debug.LogError($"[FPC] TIMEOUT! BackpackStateMachine застрял в состоянии: {backpackStateMachine.GetCurrentState()}");
						break;
				}
				yield return null;
		}
		
		Debug.Log($"[FPC] BackpackStateMachine закрытие завершено за {timer:F2} сек");
		
		// ОТКЛЮЧАЕМ Canvas после завершения анимации
		if (canvas != null)
		{
			canvas.gameObject.SetActive(false);
			Debug.Log("[FPC] InventoryCanvas отключён после закрытия");
		}
			
		// ИНВЕНТАРЬ ЗАКРЫТ
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		playerCanMove = true;
		cameraCanMove = true;
		
		if (showCameraDebugLogs)
		{
			Debug.Log($"[FPC Camera] ✅ ИНВЕНТАРЬ ЗАКРЫТ - разблокируем камеру: cameraCanMove={cameraCanMove}, playerCanMove={playerCanMove}, Cursor.lockState={Cursor.lockState}");
		}
			
			// ПЕРЕКЛЮЧАЕМ обратно на Gameplay Action Map
			SwitchToGameplayMap();
			
			isClosingInventory = false;
			isTabClosing = false; // Сбрасываем флаг TAB закрытия
			isTabTransition = false; // Сбрасываем флаг TAB перехода
			
			// Восстанавливаем экипировку если она была перед открытием инвентаря
			if (fpAxe != null && wasEquippedBeforeInventory && equipSlotBeforeInventory >= 0)
			{
				Debug.Log($"[FPC] Восстанавливаем экипировку: слот {equipSlotBeforeInventory}");
				// Небольшая задержка перед экипировкой для завершения всех операций закрытия
				StartCoroutine(RestoreEquipAfterDelay(equipSlotBeforeInventory));
			}
			else
			{
				// Сбрасываем сохраненное состояние
				wasEquippedBeforeInventory = false;
				equipSlotBeforeInventory = -1;
			}
			
			Debug.Log("[FPC] Инвентарь ЗАКРЫТ");
	}
	
	/// <summary>
	/// Закрывает инвентарь БЕЗ анимации (LEGACY: без BackpackStateMachine)
	/// </summary>
	private IEnumerator CloseInventoryWithAnimation()
	{
			isClosingInventory = true;
			
			// ОТМЕНЯЕМ перемещение предмета если он был выбран
			Canvas canvas = backpackStateMachine.inventoryCanvas;
			if (canvas != null)
			{
				Inventory inventory = canvas.GetComponentInChildren<Inventory>();
				if (inventory != null && inventory.selectedItem != null)
				{
					Debug.Log("[FPC] Инвентарь закрыт с выбранным предметом - отменяем перемещение");
					inventory.CancelItemMove();
				}
			}
			
			// Небольшая задержка для плавности
			yield return null;
			
			// ТЕПЕРЬ скрываем инвентарь
			if (canvas != null)
			{
				canvas.gameObject.SetActive(false);
			}
			
			// ИНВЕНТАРЬ ЗАКРЫТ
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
			playerCanMove = true;
			cameraCanMove = true;
			
			// ПЕРЕКЛЮЧАЕМ обратно на Gameplay Action Map
			SwitchToGameplayMap();
			
			Debug.Log("[FPC] Инвентарь ЗАКРЫТ (legacy mode)");
			
			isClosingInventory = false;
			isTabClosing = false; // Сбрасываем флаг TAB закрытия
			isTabTransition = false; // Сбрасываем флаг TAB перехода
			
			// Восстанавливаем экипировку если она была перед открытием инвентаря
			if (fpAxe != null && wasEquippedBeforeInventory && equipSlotBeforeInventory >= 0)
			{
				Debug.Log($"[FPC] Восстанавливаем экипировку: слот {equipSlotBeforeInventory}");
				StartCoroutine(RestoreEquipAfterDelay(equipSlotBeforeInventory));
			}
			else
			{
				// Сбрасываем сохраненное состояние
				wasEquippedBeforeInventory = false;
				equipSlotBeforeInventory = -1;
			}
	}
	
	/// <summary>
	/// Переключить на Gameplay Action Map
	/// </summary>
	private void SwitchToGameplayMap()
	{
			if (inventoryMap != null && inventoryMap.enabled)
			{
					inventoryMap.Disable();
			}
			if (gameplayMap != null)
			{
					gameplayMap.Enable();
					Debug.Log("[FPC] Переключено на Gameplay map");
			}
			
			// Восстанавливаем Combat map если был в боевом режиме
			if (isInCombatMode && combatMap != null)
			{
					combatMap.Enable();
			}
	}
	
	/// <summary>
	/// Переключить на Inventory Action Map
	/// </summary>
	private void SwitchToInventoryMap()
	{
			if (gameplayMap != null && gameplayMap.enabled)
			{
					gameplayMap.Disable();
			}
			if (combatMap != null && combatMap.enabled)
			{
					combatMap.Disable();
			}
			if (inventoryMap != null)
			{
					inventoryMap.Enable();
					Debug.Log("[FPC] Переключено на Inventory map");
			}
	}
	
	/// <summary>
	/// Включить Combat Mode (взял оружие)
	/// </summary>
	public void EnableCombatMode()
	{
			if (combatMap != null)
			{
					combatMap.Enable();
					isInCombatMode = true;
					
					// Отключаем Zoom (ПКМ теперь для прицеливания)
					if (zoomAction != null)
					{
							// Можно опционально отключить zoom
							// zoomAction.Disable();
					}
					
					Debug.Log("[FPC] Combat Mode ENABLED");
			}
	}
	
	/// <summary>
	/// Разблокировать движение и камеру (вызывается из BackpackStateMachine через Animation Event)
	/// </summary>
	public void EnableMovement()
	{
			playerCanMove = true;
			cameraCanMove = true;
			Debug.Log("[FPC] Движение и камера разблокированы");
	}
	
	/// <summary>
	/// Выключить Combat Mode (убрал оружие)
	/// </summary>
	public void DisableCombatMode()
	{
			if (combatMap != null && combatMap.enabled)
			{
					combatMap.Disable();
					isInCombatMode = false;
					
					// Включаем обратно Zoom
					if (zoomAction != null)
					{
							// zoomAction.Enable();
					}
					
					Debug.Log("[FPC] Combat Mode DISABLED");
			}
	}
	
	/// <summary>
	/// Инициализирует TAB после завершения blendshape перехода (вызывается из BackpackStateMachine)
	/// </summary>
	public void InitializeTabAfterTransition()
	{
		// Закрываем инвентарь ТОЛЬКО если это был TAB переход
		if (isTabTransition)
		{
			isTabTransition = false; // Сбрасываем флаг
			
			// Прямо закрываем инвентарь БЕЗ дополнительных переходов
			// (переход в Closed уже произошёл через Blink или обычный переход)
			if (backpackStateMachine != null)
			{
				StartCoroutine(CloseInventoryDirectly());
			}
			else
			{
				// Legacy: закрываем напрямую
				StartCoroutine(CloseInventoryWithAnimation());
			}
		}
	}
	
	/// <summary>
	/// Восстанавливает экипировку после закрытия инвентаря (если она была перед открытием)
	/// </summary>
	private IEnumerator RestoreEquipAfterDelay(int slotIndex)
	{
		// Небольшая задержка для завершения всех операций закрытия инвентаря
		yield return new WaitForSeconds(0.2f);
		
		if (fpAxe != null && !fpAxe.IsEquipped())
		{
			Debug.Log($"[FPC] Восстанавливаем экипировку слота {slotIndex}");
			fpAxe.ToggleEquipSlot(slotIndex);
			
			// Сбрасываем сохраненное состояние после восстановления
			wasEquippedBeforeInventory = false;
			equipSlotBeforeInventory = -1;
		}
		else
		{
			// Если уже экипировано что-то другое, не восстанавливаем
			Debug.Log("[FPC] Оружие уже экипировано или fpAxe == null, пропускаем восстановление");
			wasEquippedBeforeInventory = false;
			equipSlotBeforeInventory = -1;
		}
	}
	
	/// <summary>
	/// Закрывает инвентарь напрямую БЕЗ дополнительных переходов (для TAB после Blink)
	/// </summary>
	private IEnumerator CloseInventoryDirectly()
	{
		isClosingInventory = true;
		
		// ДЕАКТИВИРУЕМ InventoryCameraController
		if (inventoryCameraController != null)
		{
			inventoryCameraController.Deactivate();
		}
		
		// ОТМЕНЯЕМ перемещение предмета если он был выбран
		Canvas canvas = backpackStateMachine.inventoryCanvas;
		if (canvas != null)
		{
			Inventory inventory = canvas.GetComponentInChildren<Inventory>();
			if (inventory != null && inventory.selectedItem != null)
			{
				Debug.Log("[FPC] Инвентарь закрыт с выбранным предметом - отменяем перемещение");
				inventory.CancelItemMove();
			}
		}
		
		// Запускаем закрытие через BackpackStateMachine
		backpackStateMachine.CloseBackpack();
		
		// Ждём завершения закрытия (blendshape + TakeOn анимация)
		float timeout = 10f; // Таймаут на случай зависания
		float timer = 0f;
		
		while (!backpackStateMachine.IsClosingComplete())
		{
			timer += Time.deltaTime;
			if (timer > timeout)
			{
				Debug.LogError($"[FPC] TIMEOUT! BackpackStateMachine застрял в состоянии: {backpackStateMachine.GetCurrentState()}");
				break;
			}
			yield return null;
		}
		
		Debug.Log($"[FPC] BackpackStateMachine закрытие завершено за {timer:F2} сек");
		
		// ОТКЛЮЧАЕМ Canvas после завершения анимации
		if (canvas != null)
		{
			canvas.gameObject.SetActive(false);
			Debug.Log("[FPC] InventoryCanvas отключён после закрытия");
		}
			
		// ИНВЕНТАРЬ ЗАКРЫТ
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		playerCanMove = true;
		cameraCanMove = true;
		
		if (showCameraDebugLogs)
		{
			Debug.Log($"[FPC Camera] ✅ ИНВЕНТАРЬ ЗАКРЫТ - разблокируем камеру: cameraCanMove={cameraCanMove}, playerCanMove={playerCanMove}, Cursor.lockState={Cursor.lockState}");
		}
			
		// ПЕРЕКЛЮЧАЕМ обратно на Gameplay Action Map
		SwitchToGameplayMap();
		
		isClosingInventory = false;
		isTabClosing = false; // Сбрасываем флаг TAB закрытия
		isTabTransition = false; // Сбрасываем флаг TAB перехода
		
		// Восстанавливаем экипировку если она была перед открытием инвентаря
		if (fpAxe != null && wasEquippedBeforeInventory && equipSlotBeforeInventory >= 0)
		{
			Debug.Log($"[FPC] Восстанавливаем экипировку: слот {equipSlotBeforeInventory}");
			StartCoroutine(RestoreEquipAfterDelay(equipSlotBeforeInventory));
		}
		else
		{
			// Сбрасываем сохраненное состояние
			wasEquippedBeforeInventory = false;
			equipSlotBeforeInventory = -1;
		}
		
		Debug.Log("[FPC] Инвентарь ЗАКРЫТ (прямое закрытие)");
	}
	
		#region Interaction System
		
		[Header("Interaction")]
		public float interactionDistance = 5f;
		[Tooltip("Радиус взаимодействия для добивания (Brutality). Обычно меньше чем обычный радиус взаимодействия.")]
		public float brutalityInteractionDistance = 2f;
		[Tooltip("Layer mask для объектов взаимодействия. Должен включать слои: Interactable (для предметов), Enemy и Ragdoll (для добивания). Настраивается в инспекторе.")]
		public LayerMask interactionLayer = -1; // Everything layer для интеракции (настраивается в инспекторе)
		
		[Header("Debug")]
		[Tooltip("Показывать логи взаимодействия (для отладки)")]
		public bool showInteractionDebugLogs = false;
		
		private InteractableObject currentInteractable = null;
		
		/// <summary>
		/// Проверяет смотрит ли игрок на интерактивный объект (для UI)
		/// </summary>
		public bool IsLookingAtInteractable()
		{
			return currentInteractable != null;
		}
		
		/// <summary>
		/// Получает название текущего интерактивного объекта (для UI)
		/// </summary>
		public string GetCurrentInteractionName()
		{
			if (currentInteractable != null)
			{
				return currentInteractable.GetInteractionName();
			}
			return "";
		}
	
    private void HandleEquipmentInput()
    {
        if (fpAxe == null) return;
        
        // Обработка кнопки 1 (Equip_First)
        if (equipFirstAction != null && equipFirstAction.WasPressedThisFrame())
        {
            fpAxe.ToggleEquipSlot(0);
        }
        
        // Обработка кнопки 2 (Equip_Second)
        if (equipSecondAction != null && equipSecondAction.WasPressedThisFrame())
        {
            fpAxe.ToggleEquipSlot(1);
        }
        
        // Обработка кнопки 3 (Equip_Third)
        if (equipThirdAction != null && equipThirdAction.WasPressedThisFrame())
        {
            fpAxe.ToggleEquipSlot(2);
        }
    }
		
	private void HandleInteraction()
	{
		// Проверяем что currentInteractable ещё существует
		if (currentInteractable != null && currentInteractable.gameObject == null)
		{
			// Объект был уничтожен (например подобран) → обнуляем
			currentInteractable = null;
		}
		
		// Raycast от камеры (центр экрана)
		Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
		RaycastHit hit;
		
		// Определяем максимальную дистанцию для raycast (зависит от типа взаимодействия)
		// Сначала делаем raycast с обычной дистанцией для всех объектов
		float maxDistance = interactionDistance;
		bool hitFound = Physics.Raycast(ray, out hit, maxDistance, interactionLayer);
		
		if (showInteractionDebugLogs && hitFound)
		{
			Debug.Log($"[FirstPersonController] Raycast попал в: {hit.collider.name} (слой: {LayerMask.LayerToName(hit.collider.gameObject.layer)}, расстояние: {hit.distance:F2})");
		}
		
		// Если нашли объект, проверяем его тип и применяем соответствующую дистанцию
		if (hitFound)
		{
			// Ищем InteractableObject на объекте с коллайдером и его родителях (для ragdoll)
			InteractableObject interactable = hit.collider.GetComponent<InteractableObject>();
			if (interactable == null)
			{
				interactable = hit.collider.GetComponentInParent<InteractableObject>();
			}
			
			if (showInteractionDebugLogs)
			{
				Debug.Log($"[FirstPersonController] InteractableObject через GetComponent: {(interactable != null ? interactable.name : "null")}");
			}
			
			// Также проверяем BrutalityInteractable для поиска InteractableObject
			// Это важно для случаев когда raycast попадает в дочерние объекты (например EyesSensor, ChaseSensor)
			if (interactable == null)
			{
				BrutalityInteractable brutalityInteractable = null;
				
				// Получаем корневой объект
				Transform root = hit.collider.transform.root;
				
				if (showInteractionDebugLogs)
				{
					Debug.Log($"[FirstPersonController] Ищем BrutalityInteractable для {hit.collider.name}, корень: {root.name}");
				}
				
				// Ищем на корневом объекте напрямую
				brutalityInteractable = root.GetComponent<BrutalityInteractable>();
				
				// Если не найден на корне, ищем через EnemyEntity
				if (brutalityInteractable == null)
				{
					EnemyEntity enemyEntity = hit.collider.GetComponent<EnemyEntity>();
					if (enemyEntity == null)
					{
						enemyEntity = hit.collider.GetComponentInParent<EnemyEntity>();
					}
					if (enemyEntity != null)
					{
						brutalityInteractable = enemyEntity.GetComponent<BrutalityInteractable>();
					}
				}
				
				// Если всё ещё не найден, ищем через все BrutalityInteractable в сцене и проверяем корень
				if (brutalityInteractable == null)
				{
					BrutalityInteractable[] allBrutality = FindObjectsByType<BrutalityInteractable>(FindObjectsSortMode.None);
					if (showInteractionDebugLogs)
					{
						Debug.Log($"[FirstPersonController] Поиск через FindObjectsOfType: найдено {allBrutality.Length} BrutalityInteractable, ищем корень: {root.name}");
					}
					
					foreach (var b in allBrutality)
					{
						Transform bRoot = b.transform.root;
						
						// Сравниваем по имени корня (так как transform.root может возвращать разные ссылки)
						// Также проверяем что объект с коллайдером находится в иерархии того же корня
						bool rootsMatch = bRoot.name == root.name;
						bool isInSameHierarchy = hit.collider.transform.IsChildOf(bRoot) || b.transform.IsChildOf(root);
						
						if (showInteractionDebugLogs)
						{
							Debug.Log($"[FirstPersonController] Проверяем {b.name}: корень = {bRoot.name}, имя совпадает? {rootsMatch}, в одной иерархии? {isInSameHierarchy}");
						}
						
						if (rootsMatch || isInSameHierarchy)
						{
							brutalityInteractable = b;
							if (showInteractionDebugLogs)
							{
								Debug.Log($"[FirstPersonController] ✓ Найден BrutalityInteractable через поиск по корню: {b.name} (корень: {bRoot.name})");
							}
							break;
						}
					}
				}
				
				if (showInteractionDebugLogs && brutalityInteractable != null)
				{
					Debug.Log($"[FirstPersonController] Найден BrutalityInteractable: {brutalityInteractable.name} (на объекте: {brutalityInteractable.gameObject.name}, корень: {brutalityInteractable.transform.root.name})");
				}
				
				if (brutalityInteractable != null)
				{
					// Получаем InteractableObject через публичное свойство BrutalityInteractable
					interactable = brutalityInteractable.InteractableObject;
					
					if (showInteractionDebugLogs)
					{
						Debug.Log($"[FirstPersonController] InteractableObject через BrutalityInteractable.InteractableObject: {(interactable != null ? interactable.name : "null")}");
					}
					
					// Если не найден через свойство, ищем вручную
					if (interactable == null)
					{
						interactable = brutalityInteractable.GetComponent<InteractableObject>();
					}
					if (interactable == null)
					{
						interactable = brutalityInteractable.GetComponentInParent<InteractableObject>();
					}
					
					// Если всё ещё не найден, ищем на корневом объекте врага
					if (interactable == null)
					{
						interactable = brutalityInteractable.transform.root.GetComponent<InteractableObject>();
					}
					
					if (showInteractionDebugLogs)
					{
						Debug.Log($"[FirstPersonController] InteractableObject через BrutalityInteractable (после поиска): {(interactable != null ? interactable.name : "null")}");
					}
				}
				else if (showInteractionDebugLogs)
				{
					Debug.LogWarning($"[FirstPersonController] BrutalityInteractable не найден для {hit.collider.name}! Корень: {root.name}");
				}
			}
			
			if (interactable != null)
			{
				if (showInteractionDebugLogs)
				{
					Debug.Log($"[FirstPersonController] Найден InteractableObject: {interactable.name}, Type: {interactable.interactionType}, Name: '{interactable.interactionName}', Available: {interactable.IsInteractionAvailable()}");
				}
				// Если это Brutality взаимодействие - проверяем специальный радиус
				if (interactable.interactionType == InteractionType.Brutality)
				{
					// Проверяем расстояние для добивания
					if (hit.distance > brutalityInteractionDistance)
					{
						// Слишком далеко для добивания
						currentInteractable = null;
						Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.yellow);
						return;
					}
				}
				
				// Проверяем доступность взаимодействия
				if (!interactable.IsInteractionAvailable())
				{
					// Взаимодействие недоступно
					currentInteractable = null;
					Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.yellow);
					return;
				}
				
				// ДОПОЛНИТЕЛЬНАЯ ПРОВЕРКА: нет ли препятствий между игроком и объектом
				Vector3 directionToObject = (hit.point - playerCamera.transform.position).normalized;
				float distanceToObject = Vector3.Distance(playerCamera.transform.position, hit.point);
				
				// Raycast от камеры к объекту для проверки препятствий
				if (Physics.Raycast(playerCamera.transform.position, directionToObject, out RaycastHit obstacleHit, distanceToObject))
				{
					// Если попали в препятствие, которое НЕ является нашим объектом
					if (obstacleHit.collider != hit.collider)
					{
						// Препятствие между игроком и объектом - блокируем взаимодействие
						currentInteractable = null;
						Debug.DrawRay(playerCamera.transform.position, directionToObject * distanceToObject, Color.red);
						return;
					}
				}
				
				currentInteractable = interactable;
				
				if (showInteractionDebugLogs)
				{
					Debug.Log($"[FirstPersonController] currentInteractable установлен: {currentInteractable.name}, Type: {currentInteractable.interactionType}, Name: '{currentInteractable.interactionName}'");
				}
				
				// Показываем UI (если есть InteractionUIController в сцене)
				// UI обновляется автоматически через InteractionUIController
				
				// Обработка взаимодействия (INPUT SYSTEM)
				if (interactAction != null && interactAction.WasPressedThisFrame())
				{
					// Запоминаем тип перед взаимодействием
					InteractionType type = interactable.interactionType;
					string itemName = null;
					
					// Получаем имя предмета для звуков
					if (type == InteractionType.Pickable && interactable.itemData != null)
					{
						itemName = interactable.itemData.name;
					}
					
					interactable.TryInteract();
					
					// Публикуем событие для звуков интеракции
					if (DRAUM.Core.EventBus.Instance != null)
					{
						DRAUM.Modules.Player.Events.PlayerInteractionEvent interactionEvent = 
							new DRAUM.Modules.Player.Events.PlayerInteractionEvent
							{
								InteractionType = type,
								ItemName = itemName,
								Position = hit.point
							};
						DRAUM.Core.EventBus.Instance.Publish(interactionEvent);
					}
					
					// Если это Pickable → обнуляем сразу (объект будет уничтожен)
					if (type == InteractionType.Pickable)
					{
						currentInteractable = null;
					}
				}
				
				// Debug ray (зелёный если видим объект без препятствий)
				Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green);
				Debug.DrawRay(playerCamera.transform.position, directionToObject * distanceToObject, Color.blue);
			}
			else
			{
				currentInteractable = null;
				Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.yellow);
			}
		}
		else
		{
			currentInteractable = null;
			Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.red);
		}
	}
	
	/// <summary>
	/// Обновляет weight слоя PickUp в зависимости от того, смотрит ли игрок на Pickable предмет
	/// </summary>
	private void UpdatePickupLayerWeight()
	{
		if (playerAnimator == null || pickupLayerIndex == -1)
		{
			return;
		}
		
		// Определяем целевой weight: 1 если смотрим на Pickable предмет, 0 если нет
		bool isLookingAtPickable = currentInteractable != null && 
		                            currentInteractable.interactionType == InteractionType.Pickable;
		
		targetPickupWeight = isLookingAtPickable ? 1f : 0f;
		
		// Плавно интерполируем текущий weight к целевому
		currentPickupWeight = Mathf.MoveTowards(
			currentPickupWeight,
			targetPickupWeight,
			pickupLayerTransitionSpeed * Time.deltaTime
		);
		
		// Применяем weight к слою
		playerAnimator.SetLayerWeight(pickupLayerIndex, currentPickupWeight);
	}
		
		#endregion
		
		#region Gizmos
		
		/// <summary>
		/// Рисует сферу взаимодействия в Scene View
		/// </summary>
		private void OnDrawGizmos()
		{
			if (interactionDistance > 0)
			{
				// Рисуем сферу взаимодействия
				Gizmos.color = Color.yellow;
				Gizmos.DrawWireSphere(transform.position, interactionDistance);
				
				// Рисуем направление взгляда
				if (playerCamera != null)
				{
					Gizmos.color = Color.green;
					Vector3 lookDirection = playerCamera.transform.forward * interactionDistance;
					Gizmos.DrawRay(playerCamera.transform.position, lookDirection);
				}
			}
		}
		
		#endregion
		
		private void CombatSystem()
		{
				if (!enableCombat || !isInCombatMode) return;
				
				// ЛКМ - основная атака
				if (primaryAttackAction != null && primaryAttackAction.WasPressedThisFrame())
				{
						Debug.Log("[FPC] PRIMARY ATTACK (ЛКМ)");
						// TODO: Твоя логика атаки
				}
				
				// ПКМ - вторичная атака / прицеливание
				if (secondaryAttackAction != null && secondaryAttackAction.IsPressed())
				{
						Debug.Log("[FPC] SECONDARY ATTACK / AIM (ПКМ held)");
						// TODO: Твоя логика прицеливания
				}
		}
		
		#region Combat Mode Public API
		
		/// <summary>
		/// Проверить активен ли Combat Mode
		/// </summary>
		public bool IsCombatModeActive()
		{
				return isInCombatMode;
		}
		
		#endregion
}



// Custom Editor
#if UNITY_EDITOR
    [CustomEditor(typeof(FirstPersonController)), InitializeOnLoadAttribute]
    public class FirstPersonControllerEditor : Editor
    {
    FirstPersonController fpc;
    SerializedObject SerFPC;

    private void OnEnable()
    {
        fpc = (FirstPersonController)target;
        SerFPC = new SerializedObject(fpc);
    }

    public override void OnInspectorGUI()
    {
        SerFPC.Update();

        EditorGUILayout.Space();
        GUILayout.Label("Modular First Person Controller", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 16 });
        GUILayout.Label("By Jess Case", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Normal, fontSize = 12 });
        GUILayout.Label("version 1.0.1 + Input System", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Normal, fontSize = 12 });
        EditorGUILayout.Space();

        #region Input System Setup
        
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Input System Setup", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();
        
        fpc.inputActions = (InputActionAsset)EditorGUILayout.ObjectField(new GUIContent("Input Actions", "Input Action Asset (Player_IC)"), fpc.inputActions, typeof(InputActionAsset), false);
        
        if (fpc.inputActions == null)
        {
            EditorGUILayout.HelpBox("⚠️ InputActionAsset не назначен! Назначь Player_IC.inputactions", MessageType.Warning);
        }
        
        EditorGUILayout.Space();
        
        #endregion

        #region Camera Setup

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Camera Setup", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();

        fpc.playerCamera = (Camera)EditorGUILayout.ObjectField(new GUIContent("Camera", "Camera attached to the controller."), fpc.playerCamera, typeof(Camera), true);
        fpc.cameraPivot = (Transform)EditorGUILayout.ObjectField(new GUIContent("Camera Pivot", "Если задан — pitch применяется сюда, камера (с Animator) остаётся дочерней и анимации не перезаписываются. Пусто = pitch на камеру."), fpc.cameraPivot, typeof(Transform), true);
        fpc.fov = EditorGUILayout.Slider(new GUIContent("Field of View", "The camera's view angle. Changes the player camera directly."), fpc.fov, fpc.zoomFOV, 179f);
        fpc.cameraCanMove = EditorGUILayout.ToggleLeft(new GUIContent("Enable Camera Rotation", "Determines if the camera is allowed to move."), fpc.cameraCanMove);
        
        EditorGUILayout.Space();
        fpc.showCameraDebugLogs = EditorGUILayout.ToggleLeft(new GUIContent("Show Camera Debug Logs", "Включить детальные логи управления камерой (для диагностики проблем)"), fpc.showCameraDebugLogs);

        GUI.enabled = fpc.cameraCanMove;
        fpc.invertCamera = EditorGUILayout.ToggleLeft(new GUIContent("Invert Camera Rotation", "Inverts the up and down movement of the camera."), fpc.invertCamera);
        fpc.mouseSensitivity = EditorGUILayout.Slider(new GUIContent("Look Sensitivity", "Determines how sensitive the mouse movement is."), fpc.mouseSensitivity, .1f, 10f);
        fpc.maxLookAngle = EditorGUILayout.Slider(new GUIContent("Max Look Angle", "Determines the max and min angle the player camera is able to look."), fpc.maxLookAngle, 40, 90);
        GUI.enabled = true;

        fpc.lockCursor = EditorGUILayout.ToggleLeft(new GUIContent("Lock and Hide Cursor", "Turns off the cursor visibility and locks it to the middle of the screen."), fpc.lockCursor);

        fpc.crosshair = EditorGUILayout.ToggleLeft(new GUIContent("Auto Crosshair", "Determines if the basic crosshair will be turned on, and sets is to the center of the screen."), fpc.crosshair);

        // Only displays crosshair options if crosshair is enabled
        if(fpc.crosshair) 
        { 
            EditorGUI.indentLevel++; 
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Crosshair Prefab", "GameObject Prefab для кроссхейра"));
            fpc.crosshairPrefab = (GameObject)EditorGUILayout.ObjectField(fpc.crosshairPrefab, typeof(GameObject), false);
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--; 
        }

        EditorGUILayout.Space();

        #region Camera Zoom Setup

        GUILayout.Label("Zoom", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));

        fpc.enableZoom = EditorGUILayout.ToggleLeft(new GUIContent("Enable Zoom", "Determines if the player is able to zoom in while playing."), fpc.enableZoom);

        GUI.enabled = fpc.enableZoom;
        fpc.holdToZoom = EditorGUILayout.ToggleLeft(new GUIContent("Hold to Zoom", "Requires the player to hold the zoom key instead if pressing to zoom and unzoom."), fpc.holdToZoom);
        EditorGUILayout.LabelField("Zoom Key", "Настраивается в Input Actions (Zoom action)");
        fpc.zoomFOV = EditorGUILayout.Slider(new GUIContent("Zoom FOV", "Determines the field of view the camera zooms to."), fpc.zoomFOV, .1f, fpc.fov);
        fpc.zoomStepTime = EditorGUILayout.Slider(new GUIContent("Step Time", "Determines how fast the FOV transitions while zooming in."), fpc.zoomStepTime, .1f, 10f);
        GUI.enabled = true;

        #endregion

        #endregion

        #region Movement Setup

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Movement Setup", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();

        fpc.playerCanMove = EditorGUILayout.ToggleLeft(new GUIContent("Enable Player Movement", "Determines if the player is allowed to move."), fpc.playerCanMove);
        
        EditorGUILayout.Space();
        fpc.movementConfig = (MovementConfig)EditorGUILayout.ObjectField(new GUIContent("Movement Config (Optional)", "ScriptableObject конфиг движения"), fpc.movementConfig, typeof(MovementConfig), false);
        
        if (fpc.movementConfig == null)
        {
            EditorGUILayout.HelpBox("💡 Используй MovementConfig для быстрой смены настроек движения или настраивай параметры ниже", MessageType.Info);

        GUI.enabled = fpc.playerCanMove;
            fpc.walkSpeed = EditorGUILayout.Slider(new GUIContent("Walk Speed", "Determines how fast the player will move while walking."), fpc.walkSpeed, .1f, 20f);
            fpc.maxVelocityChange = EditorGUILayout.Slider(new GUIContent("Max Velocity Change", "Maximum velocity change"), fpc.maxVelocityChange, .1f, 20f);
            
            EditorGUILayout.Space();
            GUILayout.Label("Acceleration / Deceleration", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 12 });
            fpc.useAcceleration = EditorGUILayout.ToggleLeft(new GUIContent("Use Acceleration", "Плавное ускорение/замедление"), fpc.useAcceleration);
            
            if (fpc.useAcceleration)
            {
                EditorGUI.indentLevel++;
                fpc.accelerationTime = EditorGUILayout.Slider(new GUIContent("Acceleration Time", "Время разгона (сек)"), fpc.accelerationTime, 0.1f, 5f);
                fpc.decelerationTime = EditorGUILayout.Slider(new GUIContent("Deceleration Time", "Время торможения (сек)"), fpc.decelerationTime, 0.1f, 5f);
                EditorGUI.indentLevel--;
            }
            
        GUI.enabled = true;
        }
        else
        {
            EditorGUILayout.HelpBox("✅ Используется MovementConfig. Настрой параметры в ScriptableObject.", MessageType.Info);
        }

        EditorGUILayout.Space();

        #region Sprint

        GUILayout.Label("Sprint", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));

        fpc.enableSprint = EditorGUILayout.ToggleLeft(new GUIContent("Enable Sprint", "Determines if the player is allowed to sprint."), fpc.enableSprint);

        GUI.enabled = fpc.enableSprint;
        fpc.unlimitedSprint = EditorGUILayout.ToggleLeft(new GUIContent("Unlimited Sprint", "Determines if 'Sprint Duration' is enabled. Turning this on will allow for unlimited sprint."), fpc.unlimitedSprint);
        EditorGUILayout.LabelField("Sprint Key", "Настраивается в Input Actions (Sprint action)");
        fpc.sprintSpeed = EditorGUILayout.Slider(new GUIContent("Sprint Speed", "Determines how fast the player will move while sprinting."), fpc.sprintSpeed, fpc.walkSpeed, 20f);

        //GUI.enabled = !fpc.unlimitedSprint;
        fpc.sprintDuration = EditorGUILayout.Slider(new GUIContent("Sprint Duration", "Determines how long the player can sprint while unlimited sprint is disabled."), fpc.sprintDuration, 1f, 20f);
        fpc.sprintCooldown = EditorGUILayout.Slider(new GUIContent("Sprint Cooldown", "Determines how long the recovery time is when the player runs out of sprint."), fpc.sprintCooldown, .1f, fpc.sprintDuration);
        //GUI.enabled = true;

        fpc.sprintFOV = EditorGUILayout.Slider(new GUIContent("Sprint FOV", "Determines the field of view the camera changes to while sprinting."), fpc.sprintFOV, fpc.fov, 179f);
        fpc.sprintFOVStepTime = EditorGUILayout.Slider(new GUIContent("Step Time", "Determines how fast the FOV transitions while sprinting."), fpc.sprintFOVStepTime, .1f, 20f);

        fpc.useSprintBar = EditorGUILayout.ToggleLeft(new GUIContent("Use Sprint Bar", "Determines if the default sprint bar will appear on screen."), fpc.useSprintBar);

        // Only displays sprint bar options if sprint bar is enabled
        if(fpc.useSprintBar)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            fpc.hideBarWhenFull = EditorGUILayout.ToggleLeft(new GUIContent("Hide Full Bar", "Hides the sprint bar when sprint duration is full, and fades the bar in when sprinting. Disabling this will leave the bar on screen at all times when the sprint bar is enabled."), fpc.hideBarWhenFull);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Bar BG", "Object to be used as sprint bar background."));
            fpc.sprintBarBG = (Image)EditorGUILayout.ObjectField(fpc.sprintBarBG, typeof(Image), true);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Bar", "Object to be used as sprint bar foreground."));
            fpc.sprintBar = (Image)EditorGUILayout.ObjectField(fpc.sprintBar, typeof(Image), true);
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            fpc.sprintBarWidthPercent = EditorGUILayout.Slider(new GUIContent("Bar Width", "Determines the width of the sprint bar."), fpc.sprintBarWidthPercent, .1f, .5f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            fpc.sprintBarHeightPercent = EditorGUILayout.Slider(new GUIContent("Bar Height", "Determines the height of the sprint bar."), fpc.sprintBarHeightPercent, .001f, .025f);
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }
        GUI.enabled = true;

        EditorGUILayout.Space();

        #endregion

        #region Jump

        GUILayout.Label("Jump", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));

        fpc.enableJump = EditorGUILayout.ToggleLeft(new GUIContent("Enable Jump", "Determines if the player is allowed to jump."), fpc.enableJump);

        GUI.enabled = fpc.enableJump;
        EditorGUILayout.LabelField("Jump Key", "Настраивается в Input Actions (Jump action)");
        fpc.jumpPower = EditorGUILayout.Slider(new GUIContent("Jump Power", "Determines how high the player will jump."), fpc.jumpPower, .1f, 20f);
        GUI.enabled = true;

        EditorGUILayout.Space();

        #endregion

        #region Crouch

        GUILayout.Label("Crouch", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));

        fpc.enableCrouch = EditorGUILayout.ToggleLeft(new GUIContent("Enable Crouch", "Determines if the player is allowed to crouch."), fpc.enableCrouch);

        GUI.enabled = fpc.enableCrouch;
        fpc.holdToCrouch = EditorGUILayout.ToggleLeft(new GUIContent("Hold To Crouch", "Requires the player to hold the crouch key instead if pressing to crouch and uncrouch."), fpc.holdToCrouch);
        EditorGUILayout.LabelField("Crouch Key", "Настраивается в Input Actions (Crouch action)");
        fpc.crouchHeight = EditorGUILayout.Slider(new GUIContent("Crouch Height", "Determines the y scale of the player object when crouched."), fpc.crouchHeight, .1f, 1);
        fpc.speedReduction = EditorGUILayout.Slider(new GUIContent("Speed Reduction", "Determines the percent 'Walk Speed' is reduced by. 1 being no reduction, and .5 being half."), fpc.speedReduction, .1f, 1);
        GUI.enabled = true;

        #endregion

        #endregion

        #region Head Bob

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Head Bob Setup", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();

        fpc.enableHeadBob = EditorGUILayout.ToggleLeft(new GUIContent("Enable Head Bob", "Determines if the camera will bob while the player is walking."), fpc.enableHeadBob);
        

        GUI.enabled = fpc.enableHeadBob;
        fpc.joint = (Transform)EditorGUILayout.ObjectField(new GUIContent("Camera Joint", "Joint object position is moved while head bob is active."), fpc.joint, typeof(Transform), true);
        fpc.bobSpeed = EditorGUILayout.Slider(new GUIContent("Speed", "Determines how often a bob rotation is completed."), fpc.bobSpeed, 1, 20);
        fpc.bobAmount = EditorGUILayout.Vector3Field(new GUIContent("Bob Amount", "Determines the amount the joint moves in both directions on every axes."), fpc.bobAmount);
        GUI.enabled = true;

        #endregion

        #region Pickup Layer Animation

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("PickUp Layer Animation", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();

        fpc.playerAnimator = (Animator)EditorGUILayout.ObjectField(new GUIContent("Player Animator", "Animator игрока (для управления weight слоя PickUp)"), fpc.playerAnimator, typeof(Animator), true);
        fpc.pickupLayerName = EditorGUILayout.TextField(new GUIContent("PickUp Layer Name", "Имя слоя PickUp в Animator Controller"), fpc.pickupLayerName);
        fpc.pickupLayerTransitionSpeed = EditorGUILayout.Slider(new GUIContent("Transition Speed", "Скорость изменения weight слоя PickUp"), fpc.pickupLayerTransitionSpeed, 1f, 20f);
        
        EditorGUILayout.HelpBox("Weight слоя Pickup плавно становится 1 когда игрок наводится на Pickable предмет, и возвращается к 0 когда не смотрит на него.", MessageType.Info);

        #endregion

        #region Interaction System

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Interaction System", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();

        fpc.interactionDistance = EditorGUILayout.Slider(new GUIContent("Interaction Distance", "Determines how far the player can interact with objects."), fpc.interactionDistance, 1f, 20f);
        
        fpc.brutalityInteractionDistance = EditorGUILayout.Slider(new GUIContent("Brutality Interaction Distance", "Радиус взаимодействия для добивания (Brutality). Обычно меньше чем обычный радиус взаимодействия."), fpc.brutalityInteractionDistance, 0.5f, 10f);
        
        // Проверка что радиус добивания не больше обычного радиуса
        if (fpc.brutalityInteractionDistance > fpc.interactionDistance)
        {
            EditorGUILayout.HelpBox("⚠️ Радиус добивания больше обычного радиуса взаимодействия. Рекомендуется сделать его меньше.", MessageType.Warning);
        }
        
        // LayerMask для интеракции (можно выбрать несколько слоёв)
        string[] layerNames = new string[32];
        for (int i = 0; i < 32; i++)
        {
            layerNames[i] = LayerMask.LayerToName(i);
            if (string.IsNullOrEmpty(layerNames[i]))
                layerNames[i] = "Layer " + i;
        }
        fpc.interactionLayer = EditorGUILayout.MaskField(new GUIContent("Interaction Layer", "Layer mask for interactable objects."), fpc.interactionLayer, layerNames);
        
        EditorGUILayout.Space();
        fpc.showInteractionDebugLogs = EditorGUILayout.ToggleLeft(new GUIContent("Show Interaction Debug Logs", "Включить детальные логи взаимодействия (для диагностики проблем с Brutality и другими взаимодействиями)"), fpc.showInteractionDebugLogs);
        
        // Показываем информацию о сферах взаимодействия
        if (fpc.interactionDistance > 0)
        {
            EditorGUILayout.HelpBox($"Сфера взаимодействия: радиус {fpc.interactionDistance:F1}\nСфера добивания: радиус {fpc.brutalityInteractionDistance:F1}", MessageType.Info);
        }

        #endregion

				#region InventorySystem
				  // Новый раздел для инвентаря
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Inventory Setup", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();

        fpc.enableInventory = EditorGUILayout.ToggleLeft(new GUIContent("Enable Inventory", "Determines if the inventory can be opened."), fpc.enableInventory);
        EditorGUILayout.LabelField("Inventory Key", "Настраивается в Input Actions (Inventory action)");
        
        EditorGUILayout.Space();
        GUILayout.Label("Backpack State Machine (Recommended)", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 12 }, GUILayout.ExpandWidth(true));
        fpc.backpackStateMachine = (BackpackStateMachine)EditorGUILayout.ObjectField(new GUIContent("Backpack State Machine", "BackpackStateMachine для управления рюкзаком (анимации, blendshapes, Canvas)"), fpc.backpackStateMachine, typeof(BackpackStateMachine), true);
        
        fpc.inventoryCameraController = (InventoryCameraController)EditorGUILayout.ObjectField(new GUIContent("Inventory Camera Controller", "InventoryCameraController для управления камерой в инвентаре (автопоиск если не назначен)"), fpc.inventoryCameraController, typeof(InventoryCameraController), true);
        
        EditorGUILayout.Space();
        GUILayout.Label("Legacy: Manual Canvas Control", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 12 }, GUILayout.ExpandWidth(true));
        fpc.inventoryCanvas = (Canvas)EditorGUILayout.ObjectField(new GUIContent("Inventory Canvas (Legacy)", "Canvas инвентаря (если не используешь BackpackStateMachine)"), fpc.inventoryCanvas, typeof(Canvas), true);

        EditorGUILayout.Space();
				#endregion
				#region CombatSystem
				EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Combat Setup", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));
				EditorGUILayout.Space();
        fpc.enableCombat = EditorGUILayout.ToggleLeft(new GUIContent("Enable Combat", "Determines if the combat system can be enabled."), fpc.enableCombat);
        EditorGUILayout.LabelField("Attack Key", "Настраивается в Input Actions (Attack action)");
        
        EditorGUILayout.Space();
        GUILayout.Label("Equipment System", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 12 }, GUILayout.ExpandWidth(true));
        fpc.fpAxe = (FPMAxe)EditorGUILayout.ObjectField(new GUIContent("FP Axe", "Скрипт оружия FPMAxe, который отвечает за удары/экип/аниматор"), fpc.fpAxe, typeof(FPMAxe), true);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("X (Input Action 'ForceMode' в Gameplay map) включает режим Force Grip на указанный таймер. В режиме ЛКМ хватает/бросает ближайший рагдолл перед камерой, FPHands играет анимацию 'A Mulet' на слое Items.", MessageType.Info);
				#endregion
        //Sets any changes from the prefab
        if(GUI.changed)
        {
            EditorUtility.SetDirty(fpc);
            Undo.RecordObject(fpc, "FPC Change");
            SerFPC.ApplyModifiedProperties();
        }
    }

}

#endif