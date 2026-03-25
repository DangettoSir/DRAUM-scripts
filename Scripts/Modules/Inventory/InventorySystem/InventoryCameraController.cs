using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using DRAUM.Core;
using DRAUM.Core.Infrastructure.Logger;
using DRAUM.Modules.Player.Events;

/// <summary>
/// Управляет ТОЛЬКО камерой в инвентаре (position + rotation)
/// Blendshapes управляются через BackpackStateMachine
/// </summary> 
[DefaultExecutionOrder(100)]
public class InventoryCameraController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Камера игрока")]
    public Camera playerCamera;
    
    [Tooltip("Camera Pivot из FirstPersonController. Если задан — при управлении инвентарём обнуляется, чтобы поворот камеры совпадал с секциями. Автопоиск из FPC если пусто.")]
    public Transform cameraPivot;
    
    [Tooltip("BackpackStateMachine для управления blendshapes")]
    public BackpackStateMachine backpackStateMachine;
    
    [Tooltip("Input Action Asset")]
    public InputActionAsset inputActions;
    
    [Header("Camera Look Settings (ALT)")]
    [Tooltip("Разрешить свободный взгляд (ALT)")]
    public bool enableFreeLook = true;
    
    [Tooltip("Чувствительность мыши при свободном взгляде")]
    [Range(0.1f, 5f)]
    public float freeLookSensitivity = 1f;
    
    [Header("Section Settings")]
    [Tooltip("Скорость перехода между секциями (камера, blendshapes)")]
    [Range(1f, 10f)]
    public float sectionTransitionSpeed = 3f;
    
    [Tooltip("Скорость появления/исчезновения Grid'ов (alpha fade)")]
    [Range(1f, 100f)]
    public float gridFadeSpeed = 5f;
    
    [Tooltip("Скорость изменения FOV при переходах между секциями")]
    [Range(1f, 20f)]
    public float fovTransitionSpeed = 10f;
    
    [Tooltip("Время двойного нажатия для пропуска центра (сек)")]
    [Range(0.1f, 1f)]
    public float doubleClickTime = 0.3f;
    
    
    [Header("Main Section (Center, Backpack Open)")]
    [Tooltip("Позиция камеры в основной секции (Local)")]
    public Vector3 mainSectionPosition = Vector3.zero;
    
    [Tooltip("Поворот камеры в основной секции (Euler angles)")]
    public Vector3 mainSectionRotation = Vector3.zero;
    
    [Tooltip("Приближение камеры в основной секции (FOV)")]
    [Range(30f, 120f)]
    public float mainSectionFOV = 60f;
    
    [Tooltip("Кастомный FOV для основной секции (если отличается от дефолтного)")]
    [Range(30f, 120f)]
    public float customMainSectionFOV = 60f;
    
    [Tooltip("Использовать кастомный FOV для основной секции")]
    public bool useCustomMainFOV = false;
    
    [Tooltip("Grid'ы которые активны в основной секции")]
    public InventoryGrid[] mainSectionGrids;
    
    [Header("Closed Section (W - Backpack Closed)")]
    [Tooltip("Позиция камеры когда рюкзак закрыт (Local)")]
    public Vector3 closedSectionPosition = new Vector3(0, -0.5f, -0.5f);
    
    [Tooltip("Поворот камеры когда рюкзак закрыт (Euler angles)")]
    public Vector3 closedSectionRotation = new Vector3(20, 0, 0);
    
    [Tooltip("Приближение камеры когда рюкзак закрыт (FOV)")]
    [Range(30f, 120f)]
    public float closedSectionFOV = 50f;
    
    [Tooltip("Кастомный FOV для закрытой секции (если отличается от дефолтного)")]
    [Range(30f, 120f)]
    public float customClosedSectionFOV = 50f;
    
    [Tooltip("Использовать кастомный FOV для закрытой секции")]
    public bool useCustomClosedFOV = false;
    
    [Tooltip("Grid'ы которые активны когда рюкзак закрыт (обычно пустые)")]
    public InventoryGrid[] closedSectionGrids;
    
    [Tooltip("Максимальный угол Free Look X (влево/вправо)")]
    [Range(0f, 180f)]
    public float closedMaxLookAngleX = 30f;
    
    [Tooltip("Максимальный угол Free Look Y (вверх/вниз)")]
    [Range(0f, 90f)]
    public float closedMaxLookAngleY = 20f;
    
    [Tooltip("Максимальный угол Free Look X (влево/вправо)")]
    [Range(0f, 180f)]
    public float mainMaxLookAngleX = 45f;
    
    [Tooltip("Максимальный угол Free Look Y (вверх/вниз)")]
    [Range(0f, 90f)]
    public float mainMaxLookAngleY = 30f;
    
    [Header("Left Pocket Section")]
    [Tooltip("Позиция камеры в левом кармане (Local)")]
    public Vector3 leftPocketPosition = new Vector3(-1f, 0f, 0f);
    
    [Tooltip("Поворот камеры в левом кармане (Euler angles)")]
    public Vector3 leftPocketRotation = new Vector3(0, -45f, 0);
    
    [Tooltip("Приближение камеры в левом кармане (FOV)")]
    [Range(30f, 120f)]
    public float leftPocketFOV = 55f;
    
    [Tooltip("Кастомный FOV для левого кармана (если отличается от дефолтного)")]
    [Range(30f, 120f)]
    public float customLeftPocketFOV = 55f;
    
    [Tooltip("Использовать кастомный FOV для левого кармана")]
    public bool useCustomLeftFOV = false;
    
    [Tooltip("Grid'ы которые активны в левом кармане")]
    public InventoryGrid[] leftPocketGrids;
    
    [Tooltip("Offset для Grid'ов (Local Position)")]
    public Vector3 leftPocketGridOffset = Vector3.zero;
    
    [Tooltip("Максимальный угол Free Look X (влево/вправо)")]
    [Range(0f, 180f)]
    public float leftMaxLookAngleX = 30f;
    
    [Tooltip("Максимальный угол Free Look Y (вверх/вниз)")]
    [Range(0f, 90f)]
    public float leftMaxLookAngleY = 20f;
    
    [Header("Right Pocket Section")]
    [Tooltip("Позиция камеры в правом кармане (Local)")]
    public Vector3 rightPocketPosition = new Vector3(1f, 0f, 0f);
    
    [Tooltip("Поворот камеры в правом кармане (Euler angles)")]
    public Vector3 rightPocketRotation = new Vector3(0, 45f, 0);
    
    [Tooltip("Приближение камеры в правом кармане (FOV)")]
    [Range(30f, 120f)]
    public float rightPocketFOV = 55f;
    
    [Tooltip("Кастомный FOV для правого кармана (если отличается от дефолтного)")]
    [Range(30f, 120f)]
    public float customRightPocketFOV = 55f;
    
    [Tooltip("Использовать кастомный FOV для правого кармана")]
    public bool useCustomRightFOV = false;
    
    [Tooltip("Grid'ы которые активны в правом кармане")]
    public InventoryGrid[] rightPocketGrids;
    
    [Tooltip("Offset для Grid'ов (Local Position)")]
    public Vector3 rightPocketGridOffset = Vector3.zero;
    
    [Tooltip("Максимальный угол Free Look X (влево/вправо)")]
    [Range(0f, 180f)]
    public float rightMaxLookAngleX = 30f;
    
    [Tooltip("Максимальный угол Free Look Y (вверх/вниз)")]
    [Range(0f, 90f)]
    public float rightMaxLookAngleY = 20f;
    
    [Header("Special Section (S Button)")]
    [Tooltip("Позиция камеры в специальной секции (Local)")]
    public Vector3 specialSectionPosition = new Vector3(0, 2f, 0);
    
    [Tooltip("Поворот камеры в специальной секции (Euler angles)")]
    public Vector3 specialSectionRotation = new Vector3(90, 0, 0);
    
    [Tooltip("Приближение камеры в специальной секции (FOV)")]
    [Range(30f, 120f)]
    public float specialSectionFOV = 40f;
    
    [Tooltip("Кастомный FOV для специальной секции (если отличается от дефолтного)")]
    [Range(30f, 120f)]
    public float customSpecialSectionFOV = 40f;
    
    [Tooltip("Использовать кастомный FOV для специальной секции")]
    public bool useCustomSpecialFOV = false;
    
    [Tooltip("Grid'ы которые активны в специальной секции")]
    public InventoryGrid[] specialSectionGrids;
    
    [Tooltip("Offset для Grid'ов (Local Position)")]
    public Vector3 specialSectionGridOffset = Vector3.zero;
    
    [Tooltip("Максимальный угол Free Look X (влево/вправо)")]
    [Range(0f, 180f)]
    public float specialMaxLookAngleX = 45f;
    
    [Tooltip("Максимальный угол Free Look Y (вверх/вниз)")]
    [Range(0f, 90f)]
    public float specialMaxLookAngleY = 30f;
    
    [Tooltip("Volume Profile для специальной секции (опционально)")]
    public UnityEngine.Rendering.VolumeProfile specialVolumeProfile;
    
    
    [Header("Debug")]
    [Tooltip("Показывать debug логи")]
    [HideInInspector] public bool showDebugLogs = false;
    
    [Header("Eye Blink Effect")]
    [Tooltip("Animator для Volume с Eye Blink эффектом")]
    public Animator volumeAnimator;
    
    [Tooltip("Название триггера для запуска Eye Blink (по умолчанию 'IsBlink')")]
    public string blinkTriggerName = "IsBlink";
    
    [Tooltip("Использовать Eye Blink эффект при переходе в Special секцию")]
    public bool useEyeBlinkForSpecial = true;
    
    private List<InventoryGrid> allGrids = new List<InventoryGrid>();
    
    public enum InventorySection
    {
        Main,
        Closed,
        LeftPocket,
        RightPocket,
        Special
    }
    
    [HideInInspector] public InventorySection currentSection = InventorySection.Closed;
    [HideInInspector] public InventorySection targetSection = InventorySection.Closed;
    

    private InputActionMap inventoryMap;
    private InputAction lookAroundAction;
    private InputAction switchSidesAction;
    

    private Vector3 originalCameraPosition;
    private Vector3 originalCameraRotation;
    private Vector3 currentFreeLookRotation;
    private bool isFreeLooking = false;
    private float currentFOV;
    private float targetFOV;
    

    private float transitionProgress = 1f;
    private bool isClosingBlendshapeForSpecial = false;
    private bool isTransitioning = false;
    
    public bool isEyeBlinking = false;
    private bool isTransitioningToSpecial = false;
    private bool isTransitioningFromSpecial = false;
    private int blinkTriggerHash;
    
    private float inputCooldown = 0f;
    private const float INPUT_COOLDOWN_TIME = 0.3f;

    private float lastAPress = -999f;
    private float lastDPress = -999f;
    
    private Dictionary<InventoryGrid, Vector3> originalGridPositions = new Dictionary<InventoryGrid, Vector3>();
    
    private Dictionary<InventoryGrid, CanvasGroup> gridCanvasGroups = new Dictionary<InventoryGrid, CanvasGroup>();
    
    private Dictionary<InventoryGrid, float> gridTargetAlpha = new Dictionary<InventoryGrid, float>();
    
    private UnityEngine.Rendering.Volume globalVolume;
    private UnityEngine.Rendering.VolumeProfile originalVolumeProfile;
    private Inventory cachedInventory;
    private float nextInventoryLookupTime = 0f;
    
    private bool isActive = false;
    
    private void Awake()
    {
        isActive = false;
        
        if (inputActions != null)
        {
            inventoryMap = inputActions.FindActionMap("Inventory");
            if (inventoryMap != null)
            {
                lookAroundAction = inventoryMap.FindAction("LookAround");
                switchSidesAction = inventoryMap.FindAction("SwitchSides");
            }
        }
        
        InitializeGridCanvasGroups();
        
        FindGlobalVolume();
        cachedInventory = UnityEngine.Object.FindFirstObjectByType<Inventory>();
        
        blinkTriggerHash = Animator.StringToHash(blinkTriggerName);
        
        if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] Awake: Input Actions инициализированы");
    }
    
    /// <summary>
    /// Находит Global Volume в сцене для применения эффектов
    /// </summary>
    private void FindGlobalVolume()
    {
        globalVolume = UnityEngine.Object.FindFirstObjectByType<UnityEngine.Rendering.Volume>();
        
        if (globalVolume != null)
        {
            originalVolumeProfile = globalVolume.profile;
            if (showDebugLogs) DraumLogger.Info(this, $"[InventoryCameraController] Global Volume найден: {globalVolume.name}");
        }
        else
        {
            if (showDebugLogs) DraumLogger.Warning(this, "[InventoryCameraController] Global Volume не найден! Эффекты для Special секции не будут работать.");
        }
    }
    
    private void EnsureGlobalVolume()
    {
        if (globalVolume != null) return;
        
        // 1) Пытаемся найти активный Volume (быстрый путь)
        globalVolume = UnityEngine.Object.FindFirstObjectByType<UnityEngine.Rendering.Volume>();
        
        // 2) Если не найдено — ищем в т.ч. неактивные в загруженных сценах (типичный build-кейс)
        if (globalVolume == null)
        {
            var allVolumes = Resources.FindObjectsOfTypeAll<UnityEngine.Rendering.Volume>();
            if (allVolumes != null && allVolumes.Length > 0)
            {
                for (int i = 0; i < allVolumes.Length; i++)
                {
                    var v = allVolumes[i];
                    if (v == null || !v.gameObject.scene.IsValid()) continue;
                    if (v.isGlobal)
                    {
                        globalVolume = v;
                        break;
                    }
                }
                
                if (globalVolume == null)
                {
                    for (int i = 0; i < allVolumes.Length; i++)
                    {
                        var v = allVolumes[i];
                        if (v == null || !v.gameObject.scene.IsValid()) continue;
                        globalVolume = v;
                        break;
                    }
                }
            }
        }
        
        if (globalVolume != null && originalVolumeProfile == null)
        {
            originalVolumeProfile = globalVolume.profile;
            DraumLogger.Info(this, $"[InventoryCameraController] Global Volume (runtime) найден: {globalVolume.name}");
        }
    }
    
    
    private void Start()
    {
        InitializeAllGrids();
        
        if (!isActive)
        {
            enabled = false;
            if (showDebugLogs)
            {
                DraumLogger.Info(this, "[InventoryCameraController] Start: компонент ВЫКЛЮЧЕН (не был активирован)");
            }
        }
        else
        {
            if (showDebugLogs)
            {
                DraumLogger.Info(this, "[InventoryCameraController] Start: компонент УЖЕ АКТИВИРОВАН, не трогаем");
            }
        }
    }
    
    private void InitializeAllGrids()
    {
        allGrids.Clear();
        
        allGrids.AddRange(mainSectionGrids);
        allGrids.AddRange(leftPocketGrids);
        allGrids.AddRange(rightPocketGrids);
        allGrids.AddRange(closedSectionGrids);
        allGrids.AddRange(specialSectionGrids);
        
        if (showDebugLogs) DraumLogger.Info(this, $"[InventoryCameraController] Инициализировано {allGrids.Count} Grid'ов");
    }
    
    private void InitializeGridCanvasGroups()
    {
        InventoryGrid[][] allGridArrays = { mainSectionGrids, leftPocketGrids, rightPocketGrids };
        
        foreach (InventoryGrid[] grids in allGridArrays)
        {
            if (grids != null)
            {
                foreach (InventoryGrid grid in grids)
                {
                    if (grid != null && !gridCanvasGroups.ContainsKey(grid))
                    {

                        CanvasGroup canvasGroup = grid.GetComponent<CanvasGroup>();
                        if (canvasGroup == null)
                        {
                            canvasGroup = grid.gameObject.AddComponent<CanvasGroup>();
                        }
                        
                        gridCanvasGroups[grid] = canvasGroup;
                        gridTargetAlpha[grid] = 0f;
                        canvasGroup.alpha = 0f;
                        
                        if (showDebugLogs) DraumLogger.Info(this, $"[InventoryCameraController] CanvasGroup добавлен для Grid '{grid.name}'");
                    }
                }
            }
        }
    }
    
    private float lastInventoryCameraLogTime = 0f;
    private const float INVENTORY_CAMERA_LOG_INTERVAL = 2f;
    
    private void Update()
    {

        if (!isActive || !enabled)
        {

            if (showDebugLogs && Time.time - lastInventoryCameraLogTime > INVENTORY_CAMERA_LOG_INTERVAL)
            {
                lastInventoryCameraLogTime = Time.time;
                DraumLogger.Info(this, $"[InventoryCamera] ℹ️ Компонент не активен: isActive={isActive}, enabled={enabled}");
            }
            return;
        }
        
        if (showDebugLogs && Time.time - lastInventoryCameraLogTime > INVENTORY_CAMERA_LOG_INTERVAL)
        {
            lastInventoryCameraLogTime = Time.time;
            DraumLogger.Info(this, $"[InventoryCamera] ✅ Компонент активен: isActive={isActive}, enabled={enabled}, currentSection={currentSection}, transitionProgress={transitionProgress:F2}");
        }
        
        if (inputCooldown > 0f)
        {
            inputCooldown -= Time.deltaTime;
        }
        
        if (isClosingBlendshapeForSpecial)
        {
            HandleSpecialBlendshapeClosing();
            return; 
        }
        
        HandleFreeLook();
        
        HandleSectionSwitching();
        
        UpdateCamera();
        
        UpdateGridFade();
    }
    
    private void UpdateGridFade()
    {
        if (cachedInventory == null && Time.time >= nextInventoryLookupTime)
        {
            cachedInventory = UnityEngine.Object.FindFirstObjectByType<Inventory>();
            nextInventoryLookupTime = Time.time + 1f; // не спамим дорогим поиском каждый кадр
        }
        
        foreach (var kvp in gridCanvasGroups)
        {
            InventoryGrid grid = kvp.Key;
            CanvasGroup canvasGroup = kvp.Value;
            
            if (grid == null || canvasGroup == null) continue;
            
            float targetAlpha = gridTargetAlpha.ContainsKey(grid) ? gridTargetAlpha[grid] : 0f;
            
            float fadeSpeed = gridFadeSpeed;
            
            if (cachedInventory != null)
            {
                fadeSpeed = cachedInventory.GetGridFadeSpeed(grid, gridFadeSpeed);
            }
            
            if (Mathf.Abs(canvasGroup.alpha - targetAlpha) > 0.01f)
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
            }
            else
            {
                canvasGroup.alpha = targetAlpha;
            }
        }
    }
    
    /// <summary>
    /// Активировать контроллер (вызывается при открытии инвентаря)
    /// </summary>
    public void Activate()
    {
        if (isActive)
        {
            if (showDebugLogs) DraumLogger.Warning(this, "[InventoryCameraController] Уже активирован!");
            return;
        }
        
        isActive = true;
        enabled = true;
        if (cachedInventory == null)
        {
            cachedInventory = UnityEngine.Object.FindFirstObjectByType<Inventory>();
        }
        
        currentSection = InventorySection.Closed;
        targetSection = InventorySection.Closed;
        transitionProgress = 1f;
        inputCooldown = 0f;
        
        if (cameraPivot == null)
        {
            var fpc = FindFirstObjectByType<FirstPersonController>();
            if (fpc != null)
                cameraPivot = fpc.cameraPivot;
        }

        Transform targetTransform = cameraPivot != null ? cameraPivot : (playerCamera != null ? playerCamera.transform : null);
        if (targetTransform != null)
        {
            originalCameraPosition = targetTransform.localPosition;
            originalCameraRotation = targetTransform.localEulerAngles;
            if (playerCamera != null)
            {
                currentFOV = playerCamera.fieldOfView;
                targetFOV = GetSectionFOV(InventorySection.Closed);
            }
            
            if (showDebugLogs)
            {
                DraumLogger.Info(this, $"[InventoryCameraController] Сохранено положение {(cameraPivot != null ? "cameraPivot" : "playerCamera")}: pos={originalCameraPosition}, rot={originalCameraRotation}, FOV={currentFOV}");
            }
        }
        
        SetActiveGridsWithOffset(closedSectionGrids, Vector3.zero);
        
        if (showDebugLogs)
        {
            DraumLogger.Info(this, $"[InventoryCameraController] Активирован (Main Section)");
            DraumLogger.Info(this, $"  - inventoryMap: {(inventoryMap != null ? "OK" : "NULL")}");
            DraumLogger.Info(this, $"  - lookAroundAction: {(lookAroundAction != null ? "OK" : "NULL")}");
            DraumLogger.Info(this, $"  - switchSidesAction: {(switchSidesAction != null ? "OK" : "NULL")}");
            
            if (inventoryMap != null)
            {
                DraumLogger.Info(this, $"  - inventoryMap.enabled: {inventoryMap.enabled}");
            }
        }
    }
    
    /// <summary>
    /// Деактивировать контроллер (вызывается при закрытии инвентаря)
    /// </summary>
    public void Deactivate()
    {
        if (!isActive)
        {
            if (showDebugLogs) DraumLogger.Warning(this, "[InventoryCameraController] Уже деактивирован!");
            return;
        }
        
        if (showDebugLogs)
        {
            DraumLogger.Info(this, $"[InventoryCamera] 🔴 ДЕАКТИВАЦИЯ: isActive={isActive} -> false, enabled={enabled} -> false");
        }
        
        isActive = false;
        enabled = false;
        
        isFreeLooking = false;
        currentFreeLookRotation = Vector3.zero;
        inputCooldown = 0f;

        DeactivateAllGrids();

        Transform targetTransform = cameraPivot != null ? cameraPivot : (playerCamera != null ? playerCamera.transform : null);
        if (targetTransform != null)
        {
            targetTransform.localPosition = originalCameraPosition;
            targetTransform.localEulerAngles = originalCameraRotation;
        }
        
        if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] Деактивирован, Grid'ы fade out");
    }
    
    private void HandleFreeLook()
    {
        if (!isActive || !enableFreeLook || lookAroundAction == null) return;
        
        if (currentSection == InventorySection.Special) return;

        float lookValue = lookAroundAction.ReadValue<float>();
        bool isLookingAround = lookValue > 0.5f;
        
        if (showDebugLogs && lookValue > 0f)
        {
            DraumLogger.Info(this, $"[InventoryCameraController] LookAround value: {lookValue}");
        }
        
        if (isLookingAround)
        {
            if (!isFreeLooking)
            {
                isFreeLooking = true;
                if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] Free look ENABLED");
            }
            
            Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            mouseDelta *= freeLookSensitivity;
            
            currentFreeLookRotation.y += mouseDelta.x;
            currentFreeLookRotation.x -= mouseDelta.y;

            float maxX = GetCurrentMaxLookAngleX();
            float maxY = GetCurrentMaxLookAngleY();
            
            currentFreeLookRotation.x = Mathf.Clamp(currentFreeLookRotation.x, -maxY, maxY);
            currentFreeLookRotation.y = Mathf.Clamp(currentFreeLookRotation.y, -maxX, maxX);
        }
        else
        {
            if (isFreeLooking)
            {
                isFreeLooking = false;
                if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] Free look DISABLED");
            }
            
            currentFreeLookRotation = Vector3.Lerp(currentFreeLookRotation, Vector3.zero, Time.deltaTime * sectionTransitionSpeed);
        }
    }
    
    private void HandleSectionSwitching()
    {
        if (!isActive)
        {
            if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] HandleSectionSwitching: NOT ACTIVE");
            return;
        }
        
        if (switchSidesAction == null)
        {
            if (showDebugLogs) DraumLogger.Error(this, "[InventoryCameraController] HandleSectionSwitching: switchSidesAction is NULL!");
            return;
        }
        
        if (inputCooldown > 0f)
        {
            return;
        }
        
        Vector2 input = switchSidesAction.ReadValue<Vector2>();

        if (input.y > 0.5f)
        {
            if (currentSection == InventorySection.Special)
            {
                if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] W pressed: ЗАБЛОКИРОВАНО - из Special можно выйти только через S");
                return;
            }
            
            if (currentSection != InventorySection.Closed && targetSection != InventorySection.Closed)
            {
                targetSection = InventorySection.Closed;
                transitionProgress = 0f;
                inputCooldown = INPUT_COOLDOWN_TIME;
                
                if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] W pressed: переход в Closed (закрытый рюкзак)");
            }
            else if (currentSection == InventorySection.Closed && targetSection == InventorySection.Closed)
            {
                StartEyeBlinkToSpecial();
                inputCooldown = INPUT_COOLDOWN_TIME;
                
                if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] W pressed: запускаем Eye Blink для перехода из Closed в Special");
            }
            return;
        }
        
        if (input.y < -0.5f)
        {
            if (currentSection == InventorySection.Main && targetSection == InventorySection.Main)
            {
                if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] S pressed: ЗАБЛОКИРОВАНО - из Main нельзя в Special, только из Closed");
                inputCooldown = INPUT_COOLDOWN_TIME;
                return;
            }
            else if (currentSection == InventorySection.Special && targetSection == InventorySection.Special)
            {
                StartEyeBlinkFromSpecial();
                inputCooldown = INPUT_COOLDOWN_TIME;
                
                if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] S pressed: запускаем Eye Blink для возврата из Special в Closed");
            }
            else if (currentSection == InventorySection.Closed && targetSection == InventorySection.Closed)
            {
                targetSection = InventorySection.Main;
                transitionProgress = 0f;
                inputCooldown = INPUT_COOLDOWN_TIME;
                
                if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] S pressed: переход из Closed в Main (Center)");
            }
            return;
        }
        
        if (input.x < -0.5f)
        {
            if (currentSection == InventorySection.Special)
            {
                if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] A pressed: ЗАБЛОКИРОВАНО - из Special можно выйти только через S");
                return;
            }
            
            float currentTime = Time.time;
            bool isDoubleClick = (currentTime - lastAPress) < doubleClickTime;

            if (isDoubleClick && currentSection == InventorySection.RightPocket && targetSection == InventorySection.RightPocket)
            {
                targetSection = InventorySection.LeftPocket;
                transitionProgress = 0f;
                inputCooldown = INPUT_COOLDOWN_TIME;
                lastAPress = currentTime;
                
                if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] DOUBLE CLICK A: Switching from RIGHT to LEFT (skip center)");
            }

            else             if (currentSection == InventorySection.Main && targetSection == InventorySection.Main)
            {
                targetSection = InventorySection.LeftPocket;
                transitionProgress = 0f;
                inputCooldown = INPUT_COOLDOWN_TIME;
                lastAPress = currentTime;
                
                if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] Switching to LEFT pocket");
            }

            else if (currentSection == InventorySection.RightPocket && targetSection == InventorySection.RightPocket)
            {
                targetSection = InventorySection.Main;
                transitionProgress = 0f;
                inputCooldown = INPUT_COOLDOWN_TIME;
                lastAPress = currentTime;
                
                if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] Switching from RIGHT to MAIN");
            }

            else if (currentSection == InventorySection.Closed && targetSection == InventorySection.Closed)
            {
                targetSection = InventorySection.LeftPocket;
                transitionProgress = 0f;
                inputCooldown = INPUT_COOLDOWN_TIME;
                lastAPress = currentTime;
                
                if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] A pressed: переход из Closed в Left Pocket");
            }
            else
            {
                lastAPress = currentTime;
            }
        }

        if (input.x > 0.5f)
        {

            if (currentSection == InventorySection.Special)
            {
                if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] D pressed: ЗАБЛОКИРОВАНО - из Special можно выйти только через S");
                return;
            }
            
            float currentTime = Time.time;
            bool isDoubleClick = (currentTime - lastDPress) < doubleClickTime;

            if (isDoubleClick && currentSection == InventorySection.LeftPocket && targetSection == InventorySection.LeftPocket)
            {
                targetSection = InventorySection.RightPocket;
                transitionProgress = 0f;
                inputCooldown = INPUT_COOLDOWN_TIME;
                lastDPress = currentTime;
                
                if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] DOUBLE CLICK D: Switching from LEFT to RIGHT (skip center)");
            }

            else if (currentSection == InventorySection.LeftPocket && targetSection == InventorySection.LeftPocket)
            {
                targetSection = InventorySection.Main;
                transitionProgress = 0f;
                inputCooldown = INPUT_COOLDOWN_TIME;
                lastDPress = currentTime;
                
                if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] Switching from LEFT to MAIN");
            }

            else             if (currentSection == InventorySection.Main && targetSection == InventorySection.Main)
            {
                targetSection = InventorySection.RightPocket;
                transitionProgress = 0f;
                inputCooldown = INPUT_COOLDOWN_TIME;
                lastDPress = currentTime;
                
                if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] Switching to RIGHT pocket");
            }

            else if (currentSection == InventorySection.Closed && targetSection == InventorySection.Closed)
            {
                targetSection = InventorySection.RightPocket;
                transitionProgress = 0f;
                inputCooldown = INPUT_COOLDOWN_TIME;
                lastDPress = currentTime;
                
                if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] D pressed: переход из Closed в Right Pocket");
            }
            else
            {
                lastDPress = currentTime;
            }
        }
    }
    
    /// <summary>
    /// Обновляет позицию и rotation камеры для инвентаря (В UPDATE !!!!)
    /// </summary>
    private void UpdateCamera()
    {
        if (playerCamera == null)
        {
            if (showDebugLogs) DraumLogger.Warning(this, "[InventoryCamera] UpdateCamera: playerCamera == null!");
            return;
        }
        
        if (!isActive || !enabled)
        {
            return;
        }
        
        if (transitionProgress < 1f)
        {
            if (!isTransitioning)
            {
                isTransitioning = true;
                DisableAllGrids();
                if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] Начался переход - Grid'ы отключены");
            }
            
            transitionProgress += Time.deltaTime * sectionTransitionSpeed;
            transitionProgress = Mathf.Clamp01(transitionProgress);
            
            if (transitionProgress >= 1f)
            {
                currentSection = targetSection;
                isTransitioning = false;
                
                if (backpackStateMachine != null)
                {
                    backpackStateMachine.StartSectionTransition(currentSection);
                }
                
                switch (currentSection)
                {
                    case InventorySection.Main:
                        SetActiveGridsWithOffset(mainSectionGrids, Vector3.zero);
                        ApplyVolumeProfile(null);
                        break;
                    case InventorySection.Closed:
                        SetActiveGridsWithOffset(closedSectionGrids, Vector3.zero);
                        ApplyVolumeProfile(null);
                        break;
                    case InventorySection.LeftPocket:
                        SetActiveGridsWithOffset(leftPocketGrids, leftPocketGridOffset);
                        ApplyVolumeProfile(null);
                        break;
                    case InventorySection.RightPocket:
                        SetActiveGridsWithOffset(rightPocketGrids, rightPocketGridOffset);
                        ApplyVolumeProfile(null);
                        break;
                    case InventorySection.Special:
                        SetActiveGridsWithOffset(specialSectionGrids, specialSectionGridOffset);
                        ApplyVolumeProfile(specialVolumeProfile);
                        break;
                }
                
                if (showDebugLogs) DraumLogger.Info(this, $"[InventoryCameraController] Transition complete: {currentSection}");
            }
        }
        
        if (backpackStateMachine != null)
        {
            backpackStateMachine.UpdateSectionTransition();
            backpackStateMachine.UpdateSmoothBlendshapeTransition();
        }

        Vector3 currentPos = GetSectionPosition(currentSection);
        Vector3 targetPos = GetSectionPosition(targetSection);
        
        Vector3 currentRot = GetSectionRotation(currentSection);
        Vector3 targetRot = GetSectionRotation(targetSection);
        
        Vector3 finalPosition = Vector3.Lerp(currentPos, targetPos, transitionProgress);
        Vector3 finalRotation = Vector3.Lerp(currentRot, targetRot, transitionProgress);
        
        finalRotation += currentFreeLookRotation;

        if (currentSection == InventorySection.Special)
        {
            Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            mouseDelta *= freeLookSensitivity;
            
            currentFreeLookRotation.y += mouseDelta.x;
            currentFreeLookRotation.x -= mouseDelta.y;
            
            currentFreeLookRotation.x = Mathf.Clamp(currentFreeLookRotation.x, -GetCurrentMaxLookAngleY(), GetCurrentMaxLookAngleY());
            currentFreeLookRotation.y = Mathf.Clamp(currentFreeLookRotation.y, -GetCurrentMaxLookAngleX(), GetCurrentMaxLookAngleX());
            
            finalRotation += currentFreeLookRotation;
        }
        
        Transform targetTransform = cameraPivot != null ? cameraPivot : (playerCamera != null ? playerCamera.transform : null);
        if (targetTransform != null)
        {
            targetTransform.localPosition = finalPosition;
            targetTransform.localEulerAngles = finalRotation;
        }
        
        if (showDebugLogs && Time.time - lastInventoryCameraLogTime > INVENTORY_CAMERA_LOG_INTERVAL)
        {
            lastInventoryCameraLogTime = Time.time;
            DraumLogger.Info(this, $"[InventoryCamera] 📹 Обновление камеры: pos={finalPosition}, rot={finalRotation}, section={currentSection}, transition={transitionProgress:F2}");
        }
        
        float newTargetFOV = GetSectionFOV(currentSection);
        if (Mathf.Abs(newTargetFOV - targetFOV) > 0.1f)
        {
            targetFOV = newTargetFOV;
            if (showDebugLogs) DraumLogger.Info(this, $"[InventoryCameraController] FOV target changed to {targetFOV} for section {currentSection}");
        }
        
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * fovTransitionSpeed);
        playerCamera.fieldOfView = currentFOV;
    }
    
    private Vector3 GetSectionPosition(InventorySection section)
    {
        switch (section)
        {
            case InventorySection.Main: return mainSectionPosition;
            case InventorySection.Closed: return closedSectionPosition;
            case InventorySection.LeftPocket: return leftPocketPosition;
            case InventorySection.RightPocket: return rightPocketPosition;
            case InventorySection.Special: return specialSectionPosition;
            default: return Vector3.zero;
        }
    }
    
    private Vector3 GetSectionRotation(InventorySection section)
    {
        switch (section)
        {
            case InventorySection.Main: return mainSectionRotation;
            case InventorySection.Closed: return closedSectionRotation;
            case InventorySection.LeftPocket: return leftPocketRotation;
            case InventorySection.RightPocket: return rightPocketRotation;
            case InventorySection.Special: return specialSectionRotation;
            default: return Vector3.zero;
        }
    }
    
    /// <summary>
    /// Получает FOV для указанной секции (с учетом кастомных значений)
    /// </summary>
    private float GetSectionFOV(InventorySection section)
    {
        switch (section)
        {
            case InventorySection.Main: 
                return useCustomMainFOV ? customMainSectionFOV : mainSectionFOV;
            case InventorySection.Closed: 
                return useCustomClosedFOV ? customClosedSectionFOV : closedSectionFOV;
            case InventorySection.LeftPocket: 
                return useCustomLeftFOV ? customLeftPocketFOV : leftPocketFOV;
            case InventorySection.RightPocket: 
                return useCustomRightFOV ? customRightPocketFOV : rightPocketFOV;
            case InventorySection.Special: 
                return useCustomSpecialFOV ? customSpecialSectionFOV : specialSectionFOV;
            default: return 60f;
        }
    }
    
    private void SetActiveGrids(InventoryGrid[] grids)
    {
        DeactivateAllGrids();
        
        if (grids != null)
        {
            foreach (InventoryGrid grid in grids)
            {
                if (grid != null)
                {
                    grid.gameObject.SetActive(true);
                }
            }
        }
    }
    
    private void DeactivateAllGrids()
    {
        if (mainSectionGrids != null)
        {
            foreach (InventoryGrid grid in mainSectionGrids)
            {
                if (grid != null && gridTargetAlpha.ContainsKey(grid))
                {
                    gridTargetAlpha[grid] = 0f;
                }
            }
        }
        
        if (leftPocketGrids != null)
        {
            foreach (InventoryGrid grid in leftPocketGrids)
            {
                if (grid != null && gridTargetAlpha.ContainsKey(grid))
                {
                    gridTargetAlpha[grid] = 0f;
                }
            }
        }
        
        if (rightPocketGrids != null)
        {
            foreach (InventoryGrid grid in rightPocketGrids)
            {
                if (grid != null && gridTargetAlpha.ContainsKey(grid))
                {
                    gridTargetAlpha[grid] = 0f;
                }
            }
        }
        
        if (specialSectionGrids != null)
        {
            foreach (InventoryGrid grid in specialSectionGrids)
            {
                if (grid != null && gridTargetAlpha.ContainsKey(grid))
                {
                    gridTargetAlpha[grid] = 0f;
                }
            }
        }
        
        if (closedSectionGrids != null)
        {
            foreach (InventoryGrid grid in closedSectionGrids)
            {
                if (grid != null && gridTargetAlpha.ContainsKey(grid))
                {
                    gridTargetAlpha[grid] = 0f;
                }
            }
        }
    }
    
    private void DisableAllGrids()
    {
        InventoryGrid[][] allGridArrays = { mainSectionGrids, closedSectionGrids, leftPocketGrids, rightPocketGrids, specialSectionGrids };
        
        foreach (InventoryGrid[] grids in allGridArrays)
        {
            if (grids != null)
            {
                foreach (InventoryGrid grid in grids)
                {
                    if (grid != null)
                    {
                        grid.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
    
    private void SetActiveGridsWithOffset(InventoryGrid[] grids, Vector3 offset)
    {
        DeactivateAllGrids();
        
        if (grids != null)
        {
            foreach (InventoryGrid grid in grids)
            {
                if (grid != null)
                {
                    grid.gameObject.SetActive(true);
                    
                    RectTransform rectTransform = grid.GetComponent<RectTransform>();
                    
                    if (rectTransform != null)
                    {
                        if (!originalGridPositions.ContainsKey(grid))
                        {
                            originalGridPositions[grid] = rectTransform.anchoredPosition3D;
                        }
                        
                        rectTransform.anchoredPosition3D = originalGridPositions[grid] + offset;
                        
                        if (showDebugLogs && offset != Vector3.zero)
                        {
                            DraumLogger.Info(this, $"[InventoryCameraController] Grid '{grid.name}' RectTransform offset applied: {offset} → {rectTransform.anchoredPosition3D}");
                        }
                    }
                    else
                    {
                        if (!originalGridPositions.ContainsKey(grid))
                        {
                            originalGridPositions[grid] = grid.transform.localPosition;
                        }
                        
                        grid.transform.localPosition = originalGridPositions[grid] + offset;
                        
                        if (showDebugLogs && offset != Vector3.zero)
                        {
                            DraumLogger.Info(this, $"[InventoryCameraController] Grid '{grid.name}' Transform offset applied: {offset}");
                        }
                    }
                    
                    if (!grid.gameObject.activeSelf)
                    {
                        grid.gameObject.SetActive(true);
                    }
                    
                    if (gridTargetAlpha.ContainsKey(grid))
                    {
                        gridTargetAlpha[grid] = 1f;
                    }
                }
            }
        }
    }
    
    private float GetCurrentMaxLookAngleX()
    {
        switch (currentSection)
        {
            case InventorySection.Main: return mainMaxLookAngleX;
            case InventorySection.Closed: return closedMaxLookAngleX;
            case InventorySection.LeftPocket: return leftMaxLookAngleX;
            case InventorySection.RightPocket: return rightMaxLookAngleX;
            case InventorySection.Special: return specialMaxLookAngleX;
            default: return mainMaxLookAngleX;
        }
    }
    
    private float GetCurrentMaxLookAngleY()
    {
        switch (currentSection)
        {
            case InventorySection.Main: return mainMaxLookAngleY;
            case InventorySection.Closed: return closedMaxLookAngleY;
            case InventorySection.LeftPocket: return leftMaxLookAngleY;
            case InventorySection.RightPocket: return rightMaxLookAngleY;
            case InventorySection.Special: return specialMaxLookAngleY;
            default: return mainMaxLookAngleY;
        }
    }
    
    
    private Vector3 GetSectionBlendshapeValues(InventorySection section)
    {
        if (backpackStateMachine == null) return Vector3.zero;
        
        switch (section)
        {
            case InventorySection.Main:
                return new Vector3(backpackStateMachine.mainSectionOpenValue, 0f, 0f);
                
            case InventorySection.Closed:
                return new Vector3(0f, 0f, 0f);
                
            case InventorySection.LeftPocket:
                return new Vector3(backpackStateMachine.leftPocketOpenValue, backpackStateMachine.leftPocketValue, 0f);
                
            case InventorySection.RightPocket:
                return new Vector3(backpackStateMachine.rightPocketOpenValue, 0f, backpackStateMachine.rightPocketValue);
                
            case InventorySection.Special:
                return new Vector3(0f, 0f, 0f);
                
            default:
                return Vector3.zero;
        }
    }
    
    /// <summary>
    /// Применяет Volume Profile
    /// </summary>
    private void ApplyVolumeProfile(UnityEngine.Rendering.VolumeProfile profile)
    {
        EnsureGlobalVolume();
        if (globalVolume == null)
        {
            if (showDebugLogs) DraumLogger.Warning(this, "[InventoryCameraController] Global Volume is NULL! Cannot apply profile.");
            return;
        }
        
        if (profile != null)
        {
            globalVolume.profile = profile;
            if (showDebugLogs) DraumLogger.Info(this, $"[InventoryCameraController] Volume Profile применен: {profile.name}");
        }
        else
        {
            if (originalVolumeProfile != null)
            {
                globalVolume.profile = originalVolumeProfile;
                if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] Volume Profile сброшен на оригинальный");
            }
        }
    }
    
    
    /// <summary>
    /// Обработка закрытия Open blendshape для перехода в Special секцию
    /// </summary>
    private void HandleSpecialBlendshapeClosing()
    {
        if (backpackStateMachine == null || backpackStateMachine.openBlendshapeIndex == -1)
        {
            isClosingBlendshapeForSpecial = false;
            currentSection = InventorySection.Special;
            transitionProgress = 1f;
            return;
        }
        
        float currentValue = backpackStateMachine.backpackMesh.GetBlendShapeWeight(backpackStateMachine.openBlendshapeIndex);
        float targetValue = 0f;
        
        currentValue = Mathf.MoveTowards(currentValue, targetValue, backpackStateMachine.blendshapeSpeed * Time.deltaTime);
        backpackStateMachine.backpackMesh.SetBlendShapeWeight(backpackStateMachine.openBlendshapeIndex, currentValue);

        if (currentValue <= 0.1f)
        {
            isClosingBlendshapeForSpecial = false;
            currentSection = InventorySection.Special;
            transitionProgress = 1f;
            
            SetActiveGridsWithOffset(specialSectionGrids, specialSectionGridOffset);
            
            ApplyVolumeProfile(specialVolumeProfile);
            
            if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] Open blendshape закрыт, переключились в Special");
        }
    }
    
    /// <summary>
    /// Обновляет Grid'ы для секции с учётом значений blendshape'ов
    /// </summary>
    public void UpdateGridsForSection(InventorySection section, Vector3 blendshapeValues)
    {
        switch (section)
        {
            case InventorySection.Main:
                SetActiveGridsWithOffset(mainSectionGrids, Vector3.zero);
                break;
            case InventorySection.LeftPocket:
                SetActiveGridsWithOffset(leftPocketGrids, leftPocketGridOffset);
                break;
            case InventorySection.RightPocket:
                SetActiveGridsWithOffset(rightPocketGrids, rightPocketGridOffset);
                break;
            case InventorySection.Closed:
                SetActiveGridsWithOffset(closedSectionGrids, Vector3.zero);
                break;
            case InventorySection.Special:
                SetActiveGridsWithOffset(specialSectionGrids, specialSectionGridOffset);
                break;
        }
        
        if (showDebugLogs) DraumLogger.Info(this, $"[InventoryCameraController] UpdateGridsForSection: {section} - Grid'ы активированы");
    }
    
    /// <summary>
    /// Вычисляет прозрачность Grid'а на основе значений blendshape'ов
    /// </summary>
    private float CalculateGridAlpha(InventorySection section, Vector3 blendshapeValues)
    {
        float openValue = blendshapeValues.x;
        float leftValue = blendshapeValues.y;
        float rightValue = blendshapeValues.z;
        
        switch (section)
        {
            case InventorySection.Main:
                return Mathf.Clamp01(openValue / 100f);
            case InventorySection.LeftPocket:
                return Mathf.Clamp01(leftValue / 100f);
            case InventorySection.RightPocket:
                return Mathf.Clamp01(rightValue / 100f);
            case InventorySection.Closed:
                return 1f;
            case InventorySection.Special:
                return 1f;
            default:
                return 0f;
        }
    }
    
    /// <summary>
    /// Скрывает все Grid'ы
    /// </summary>
    public void HideAllGrids()
    {
        foreach (InventoryGrid grid in allGrids)
        {
            if (grid != null && grid.gameObject.activeInHierarchy)
            {
                CanvasGroup canvasGroup = grid.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }
                grid.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Завершает переход в Special секцию (вызывается из Eye Blink Animation Event)
    /// </summary>
    public void CompleteSpecialTransition()
    {
        if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] Eye Blink: глаз закрыт, меняем камеру и Volume");
        
        targetSection = InventorySection.Special;
        transitionProgress = 0f;

        Transform targetTransform = cameraPivot != null ? cameraPivot : (playerCamera != null ? playerCamera.transform : null);
        if (targetTransform != null)
        {
            targetTransform.localPosition = originalCameraPosition + specialSectionPosition;
            targetTransform.localEulerAngles = originalCameraRotation + specialSectionRotation;
        }
        
        targetFOV = GetSectionFOV(InventorySection.Special);
        
        SetActiveGridsWithOffset(specialSectionGrids, specialSectionGridOffset);
        
        ApplyVolumeProfile(specialVolumeProfile);
        
        isClosingBlendshapeForSpecial = false;
        
        currentSection = InventorySection.Special;
    }
    
    /// <summary>
    /// Завершает возврат из Special секции (вызывается из Eye Blink Animation Event)
    /// </summary>
    public void CompleteSpecialReturn()
    {
        if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] Eye Blink: глаз закрыт, меняем камеру и Volume");
        
        targetSection = InventorySection.Closed;
        transitionProgress = 0f;

        Transform targetTransform = cameraPivot != null ? cameraPivot : (playerCamera != null ? playerCamera.transform : null);
        if (targetTransform != null)
        {
            targetTransform.localPosition = originalCameraPosition + closedSectionPosition;
            targetTransform.localEulerAngles = originalCameraRotation + closedSectionRotation;
        }
        
        targetFOV = GetSectionFOV(InventorySection.Closed);
        
        SetActiveGridsWithOffset(closedSectionGrids, Vector3.zero);
        
        ApplyVolumeProfile(null);
        
        currentSection = InventorySection.Closed;
    }
    
    /// <summary>
    /// Публичный метод для перехода в секцию (используется из FirstPersonController)
    /// </summary>
    public void TransitionToSection(InventorySection newSection)
    {
        if (!isActive)
        {
            if (showDebugLogs) DraumLogger.Warning(this, "[InventoryCameraController] TransitionToSection: NOT ACTIVE");
            return;
        }
        
        if (inputCooldown > 0f)
        {
            if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] TransitionToSection: input cooldown active");
            return;
        }
        
        targetSection = newSection;
        transitionProgress = 0f;
        inputCooldown = 0.2f;
        
        if (EventBus.Instance != null && currentSection != newSection)
        {
            EventBus.Instance.Publish(new UISoundEvent 
            { 
                SoundType = UISoundType.SectionTransition,
                Context = $"{currentSection}->{newSection}"
            });
        }
        
        if (newSection == InventorySection.Closed)
        {
            StartCoroutine(HandleTabTransitionToClosed());
        }
        
        if (showDebugLogs) DraumLogger.Info(this, $"[InventoryCameraController] TransitionToSection: переход в {newSection}");
    }
    
    /// <summary>
    /// Обрабатывает TAB переход в Closed секцию (включить grid'ы на 0.1 сек)
    /// </summary>
    private System.Collections.IEnumerator HandleTabTransitionToClosed()
    {
        SetActiveGridsWithOffset(closedSectionGrids, Vector3.zero);
        
        if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] TAB переход: grid'ы включены на 0.1 сек");
        
        yield return new WaitForSeconds(0.1f);
        
        DisableAllGrids();
        
        if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] TAB переход: grid'ы выключены");
    }
    
    /// <summary>
    /// Запускает Eye Blink эффект для перехода в Special секцию
    /// </summary>
    public void StartEyeBlinkToSpecial()
    {
        if (!useEyeBlinkForSpecial || volumeAnimator == null) return;

        var beautifyBlinkAnimator = FindFirstObjectByType<BeautifyBlinkAnimator>();
        if (beautifyBlinkAnimator != null)
        {
            beautifyBlinkAnimator.ResetBlinkProgress();
            if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] BeautifyBlinkAnimator сброшен перед запуском");
        }
        
        isEyeBlinking = true;
        isTransitioningToSpecial = true;
        isTransitioningFromSpecial = false;
        
        volumeAnimator.SetTrigger(blinkTriggerHash);
        
        if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] Запущен Eye Blink эффект для перехода в Special");
    }
    
    /// <summary>
    /// Запускает Eye Blink эффект для перехода из Special секции в Closed
    /// </summary>
    public void StartEyeBlinkFromSpecial()
    {
        if (!useEyeBlinkForSpecial || volumeAnimator == null) return;

        var beautifyBlinkAnimator = FindFirstObjectByType<BeautifyBlinkAnimator>();
        if (beautifyBlinkAnimator != null)
        {
            beautifyBlinkAnimator.ResetBlinkProgress();
            if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] BeautifyBlinkAnimator сброшен перед запуском");
        }
        
        isEyeBlinking = true;
        isTransitioningToSpecial = false;
        isTransitioningFromSpecial = true;
        
        volumeAnimator.SetTrigger(blinkTriggerHash);
        
        if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] Запущен Eye Blink эффект для перехода из Special в Closed");
    }
    
    /// <summary>
    /// Вызывается Animation Event когда глаз полностью закрыт
    /// </summary>
    public void OnEyeBlinkClosed()
    {
        if (!isEyeBlinking) return;
        
        if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] Eye Blink: глаз полностью закрыт, выполняем переход");
        
        if (isTransitioningToSpecial)
        {
            CompleteSpecialTransition();
        }
        else if (isTransitioningFromSpecial)
        {
            CompleteSpecialReturn();
        }
    }
    
    /// <summary>
    /// Вызывается Animation Event когда глаз полностью открыт (конец анимации)
    /// </summary>
    public void OnEyeBlinkOpened()
    {
        if (!isEyeBlinking) return;
        
        var beautifyBlinkAnimator = FindFirstObjectByType<BeautifyBlinkAnimator>();
        if (beautifyBlinkAnimator != null)
        {
            beautifyBlinkAnimator.ResetBlinkProgress();
            if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] BeautifyBlinkAnimator сброшен");
        }
        
        isEyeBlinking = false;
        isTransitioningToSpecial = false;
        isTransitioningFromSpecial = false;
        
        if (showDebugLogs) DraumLogger.Info(this, "[InventoryCameraController] Eye Blink эффект завершён - глаз открыт");
    }
}
