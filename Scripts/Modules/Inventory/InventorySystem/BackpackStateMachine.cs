using System.Collections;
using UnityEngine;
using DRAUM.Core;
using DRAUM.Core.Infrastructure.Logger;
using DRAUM.Modules.Player.Events;

/// <summary>
/// Управляет состоянием рюкзака: анимации, blendshapes, показ/скрытие Canvas
/// </summary>
public class BackpackStateMachine : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Animator рюкзака")]
    public Animator backpackAnimator;
    
    [Tooltip("SkinnedMeshRenderer модели рюкзака с blendshapes")]
    public SkinnedMeshRenderer backpackMesh;
    
    [Tooltip("Canvas инвентаря для показа/скрытия")]
    public Canvas inventoryCanvas;
    
    [Header("Animation Parameters")]
    [Tooltip("Название boolean параметра для открытия")]
    public string openParameterName = "Openning";
    
    [Tooltip("Название boolean параметра для закрытия")]
    public string closeParameterName = "Closing";
    
    [Tooltip("Название состояния TakeOff (снятие рюкзака)")]
    public string takeOffStateName = "TakeOff";
    
    [Tooltip("Название состояния TakeOn (надевание рюкзака)")]
    public string takeOnStateName = "TakeOn";
    
    [Tooltip("Название состояния открытия (после TakeOff)")]
    public string openStateName = "InventoryOpen";
    
    [Tooltip("Название состояния закрытия (после TakeOn)")]
    public string closeStateName = "InventoryClosed";
    
    [Header("Blendshape Settings")]
    [Tooltip("Название blendshape для открытия рюкзака (0 = закрыт, 100 = открыт)")]
    public string openBlendshapeName = "Open";
    
    [Tooltip("Название blendshape для левого кармана")]
    public string leftPocketBlendshapeName = "L_PACKET";
    
    [Tooltip("Название blendshape для правого кармана")]
    public string rightPocketBlendshapeName = "R_PACKET";
    
    [Tooltip("Скорость изменения blendshape (0-100 за секунду)")]
    [Range(10f, 500f)]
    public float blendshapeSpeed = 100f;
    
    [Header("Section Transition Settings")]
    [Tooltip("Задержка перед началом blendshape'ов после поворота камеры (сек)")]
    [Range(0f, 2f)]
    public float blendshapeStartDelay = 0.3f;
    
    [Tooltip("Скорость закрытия blendshape'ов (1 = быстро, 0.5 = медленно)")]
    [Range(0.1f, 10f)]
    public float blendshapeCloseSpeed = 2f;
    
    [Tooltip("Скорость открытия blendshape'ов (1 = быстро, 0.5 = медленно)")]
    [Range(0.1f, 10f)]
    public float blendshapeOpenSpeed = 1.5f;
    
    [Header("Grid Management")]
    [Tooltip("Ссылка на InventoryCameraController для управления Grid'ами")]
    public InventoryCameraController inventoryCameraController;
    
    [Tooltip("Значение blendshape при котором показывать Canvas (0-100)")]
    [Range(0f, 100f)]
    public float canvasShowThreshold = 50f;
    
    [Header("Section Blendshape Values")]
    [Tooltip("Open blendshape в основной секции")]
    [Range(0f, 100f)]
    public float mainSectionOpenValue = 100f;
    
    [Tooltip("Open blendshape в левом кармане")]
    [Range(0f, 100f)]
    public float leftPocketOpenValue = 30f;
    
    [Tooltip("L_PACKET blendshape в левом кармане")]
    [Range(0f, 100f)]
    public float leftPocketValue = 100f;
    
    [Tooltip("Open blendshape в правом кармане")]
    [Range(0f, 100f)]
    public float rightPocketOpenValue = 30f;
    
    [Tooltip("R_PACKET blendshape в правом кармане")]
    [Range(0f, 100f)]
    public float rightPocketValue = 100f;
    
    [Header("Debug")]
    [Tooltip("Показывать debug логи")]
    [HideInInspector] public bool showDebugLogs = false;
    
    private int openParameterHash;
    private int closeParameterHash;
    [HideInInspector] public int openBlendshapeIndex = -1;
    [HideInInspector] public int leftPocketBlendshapeIndex = -1;
    [HideInInspector] public int rightPocketBlendshapeIndex = -1;
    private float currentBlendshapeValue = 0f;
    private float targetBlendshapeValue = 0f;
    
    
    private enum BackpackState
    {
        Closed,
        TakingOff,
        OpeningBlendshape,
        Open,
        ClosingBlendshape,
        TakingOn
    }
    
    private BackpackState currentState = BackpackState.Closed;
    private bool canvasShown = false;
    private float stateTimer = 0f;
    
    private bool isWaitingForBlendshapeStart = false;
    private float blendshapeStartTimer = 0f;
    private InventoryCameraController.InventorySection targetSection = InventoryCameraController.InventorySection.Main;
    
    private bool isTransitioningBlendshapes = false;
    private float blendshapeTransitionProgress = 0f;
    private Vector3 currentBlendshapeValues = Vector3.zero;
    private Vector3 targetBlendshapeValues = Vector3.zero;
    
    private bool isClosingBlendshapes = false;
    private bool isOpeningBlendshapes = false;
    private Vector3 startBlendshapeValues = Vector3.zero;
    
    
    private void Awake()
    {
        openParameterHash = Animator.StringToHash(openParameterName);
        closeParameterHash = Animator.StringToHash(closeParameterName);
        
        if (backpackAnimator == null)
        {
            DraumLogger.Error(this, "[BackpackStateMachine] Animator не назначен!");
        }
        
        if (backpackMesh == null)
        {
            DraumLogger.Warning(this, "[BackpackStateMachine] SkinnedMeshRenderer не назначен! Blendshapes не будут работать.");
        }
        else
        {
            Mesh mesh = backpackMesh.sharedMesh;
            openBlendshapeIndex = -1;
            leftPocketBlendshapeIndex = -1;
            rightPocketBlendshapeIndex = -1;
            
            if (showDebugLogs)
            {
                DraumLogger.Info(this, $"[BackpackStateMachine] Ищем blendshapes:");
                DraumLogger.Info(this, $"  - Open: '{openBlendshapeName}'");
                DraumLogger.Info(this, $"  - Left Pocket: '{leftPocketBlendshapeName}'");
                DraumLogger.Info(this, $"  - Right Pocket: '{rightPocketBlendshapeName}'");
                DraumLogger.Info(this, $"[BackpackStateMachine] Доступные blendshapes на меше:");
            }
            
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                string blendShapeName = mesh.GetBlendShapeName(i);
                
                if (showDebugLogs)
                {
                    DraumLogger.Info(this, $"  [{i}] '{blendShapeName}'");
                }
                
                if (!string.IsNullOrEmpty(openBlendshapeName) && 
                    string.Equals(blendShapeName, openBlendshapeName, System.StringComparison.OrdinalIgnoreCase))
                {
                    openBlendshapeIndex = i;
                }
                
                if (!string.IsNullOrEmpty(leftPocketBlendshapeName) && 
                    string.Equals(blendShapeName, leftPocketBlendshapeName, System.StringComparison.OrdinalIgnoreCase))
                {
                    leftPocketBlendshapeIndex = i;
                }
                
                if (!string.IsNullOrEmpty(rightPocketBlendshapeName) && 
                    string.Equals(blendShapeName, rightPocketBlendshapeName, System.StringComparison.OrdinalIgnoreCase))
                {
                    rightPocketBlendshapeIndex = i;
                }
            }
            
            if (openBlendshapeIndex == -1)
            {
                DraumLogger.Error(this, $"[BackpackStateMachine] Blendshape '{openBlendshapeName}' не найден! Проверь название в Inspector.");
            }
            if (leftPocketBlendshapeIndex == -1 && !string.IsNullOrEmpty(leftPocketBlendshapeName))
            {
                DraumLogger.Warning(this, $"[BackpackStateMachine] Blendshape '{leftPocketBlendshapeName}' не найден! Проверь название в Inspector.");
            }
            if (rightPocketBlendshapeIndex == -1 && !string.IsNullOrEmpty(rightPocketBlendshapeName))
            {
                DraumLogger.Warning(this, $"[BackpackStateMachine] Blendshape '{rightPocketBlendshapeName}' не найден! Проверь название в Inspector.");
            }
            
            if (openBlendshapeIndex != -1)
            {
                currentBlendshapeValue = backpackMesh.GetBlendShapeWeight(openBlendshapeIndex);
            }
            
            if (showDebugLogs)
            {
                DraumLogger.Info(this, $"[BackpackStateMachine] Результат поиска blendshapes:");
                DraumLogger.Info(this, $"  - Open: index={openBlendshapeIndex} ('{openBlendshapeName}')");
                DraumLogger.Info(this, $"  - L_PACKET: index={leftPocketBlendshapeIndex} ('{leftPocketBlendshapeName}')");
                DraumLogger.Info(this, $"  - R_PACKET: index={rightPocketBlendshapeIndex} ('{rightPocketBlendshapeName}')");
            }
        }
        
        if (inventoryCanvas == null)
        {
            DraumLogger.Error(this, "[BackpackStateMachine] Inventory Canvas не назначен!");
        }
        else
        {
            inventoryCanvas.gameObject.SetActive(false);
            canvasShown = false;
        }
    }
    
    private void Update()
    {
        switch (currentState)
        {
            case BackpackState.TakingOff:
                UpdateTakeOffState();
                break;
                
            case BackpackState.OpeningBlendshape:
                UpdateOpeningBlendshapeState();
                break;
                
            case BackpackState.ClosingBlendshape:
                UpdateClosingBlendshapeState();
                break;
                
            case BackpackState.TakingOn:
                UpdateTakeOnState();
                break;
        }
    }
    
    /// <summary>
    /// Открыть рюкзак (вызывается из FirstPersonController)
    /// </summary>
    public void OpenBackpack()
    {
        if (showDebugLogs) DraumLogger.Info(this, "[BackpackStateMachine] Начинаем открытие: TakeOff анимация");
        
        if (inventoryCanvas != null)
        {
            inventoryCanvas.gameObject.SetActive(true);
            canvasShown = true;
            if (showDebugLogs) DraumLogger.Info(this, "[BackpackStateMachine] Canvas включен при открытии");
        }
        
        if (backpackMesh != null && openBlendshapeIndex != -1)
        {
            currentBlendshapeValue = 0f;
            targetBlendshapeValue = 0f;
            backpackMesh.SetBlendShapeWeight(openBlendshapeIndex, 0f);
            
            if (showDebugLogs) DraumLogger.Info(this, "[BackpackStateMachine] Blendshape сброшен в 0 перед открытием");
        }
        
        if (backpackAnimator != null)
        {
            backpackAnimator.SetBool(openParameterHash, true);
            backpackAnimator.SetBool(closeParameterHash, false);
        }
        
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Publish(new UISoundEvent 
            { 
                SoundType = UISoundType.BackpackOpen,
                Context = "Backpack"
            });
        }
        
        currentState = BackpackState.TakingOff;
        canvasShown = false;
    }
    
    /// <summary>
    /// Закрыть рюкзак (вызывается из FirstPersonController)
    /// </summary>
    public void CloseBackpack()
    {
        if (showDebugLogs) DraumLogger.Info(this, "[BackpackStateMachine] Начинаем закрытие: blendshape 100→0");
        
        if (inventoryCanvas != null && canvasShown)
        {
            inventoryCanvas.gameObject.SetActive(false);
            canvasShown = false;
            if (showDebugLogs) DraumLogger.Info(this, "[BackpackStateMachine] Canvas скрыт");
        }
        
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Publish(new UISoundEvent 
            { 
                SoundType = UISoundType.BackpackClose,
                Context = "Backpack"
            });
        }
        
        currentState = BackpackState.ClosingBlendshape;
        targetBlendshapeValue = 0f;
    }
    
    /// <summary>
    /// State: Ждём завершения TakeOff анимации
    /// </summary>
    private void UpdateTakeOffState()
    {
        if (backpackAnimator == null) return;
        
        AnimatorStateInfo stateInfo = backpackAnimator.GetCurrentAnimatorStateInfo(0);
        
        if (stateInfo.IsName(takeOffStateName) && stateInfo.normalizedTime >= 1.0f)
        {
            if (showDebugLogs) DraumLogger.Info(this, "[BackpackStateMachine] TakeOff завершён, начинаем открывать blendshape");
            currentState = BackpackState.OpeningBlendshape;
            
            if (inventoryCameraController != null)
            {
                SetSectionBlendshapes(inventoryCameraController.currentSection);
                if (showDebugLogs) DraumLogger.Info(this, $"[BackpackStateMachine] Blendshapes установлены для секции: {inventoryCameraController.currentSection}");
            }
            else
            {
                targetBlendshapeValue = mainSectionOpenValue;
                if (showDebugLogs) DraumLogger.Info(this, "[BackpackStateMachine] InventoryCameraController не найден, используем Main секцию");
            }
        }
    }
    
    /// <summary>
    /// State: Открываем blendshape 0→100
    /// </summary>
    private void UpdateOpeningBlendshapeState()
    {
        if (backpackMesh == null || openBlendshapeIndex == -1) return;
        
        currentBlendshapeValue = Mathf.MoveTowards(
            currentBlendshapeValue,
            targetBlendshapeValue,
            blendshapeSpeed * Time.deltaTime
        );
        
        backpackMesh.SetBlendShapeWeight(openBlendshapeIndex, currentBlendshapeValue);
        
        if (!canvasShown && currentBlendshapeValue >= canvasShowThreshold)
        {
            if (inventoryCanvas != null)
            {
                inventoryCanvas.gameObject.SetActive(true);
                canvasShown = true;
                if (showDebugLogs) DraumLogger.Info(this, $"[BackpackStateMachine] Canvas показан (blendshape: {currentBlendshapeValue:F1})");
            }
        }
        
        if (Mathf.Approximately(currentBlendshapeValue, targetBlendshapeValue))
        {
            currentState = BackpackState.Open;
            if (showDebugLogs) DraumLogger.Info(this, "[BackpackStateMachine] Открытие завершено");
        }
    }
    
    /// <summary>
    /// State: Закрываем blendshape 100→0
    /// </summary>
    private void UpdateClosingBlendshapeState()
    {
        if (backpackMesh == null || openBlendshapeIndex == -1) return;
        
        currentBlendshapeValue = Mathf.MoveTowards(
            currentBlendshapeValue,
            targetBlendshapeValue,
            blendshapeSpeed * Time.deltaTime
        );
        
        backpackMesh.SetBlendShapeWeight(openBlendshapeIndex, currentBlendshapeValue);
        
        if (Mathf.Approximately(currentBlendshapeValue, targetBlendshapeValue))
        {
            if (showDebugLogs) DraumLogger.Info(this, "[BackpackStateMachine] Blendshape закрыт, запускаем TakeOn анимацию");
            
            if (backpackAnimator != null)
            {
                backpackAnimator.SetBool(openParameterHash, false);
                backpackAnimator.SetBool(closeParameterHash, true);
            }
            
            currentState = BackpackState.TakingOn;
            stateTimer = 0f;
        }
    }
    
    /// <summary>
    /// State: Ждём завершения TakeOn анимации
    /// </summary>
    private void UpdateTakeOnState()
    {
        stateTimer += Time.deltaTime;
        
        if (backpackAnimator == null)
        {
            DraumLogger.Warning(this, "[BackpackStateMachine] Animator == null в TakingOn! Переходим в Closed.");
            currentState = BackpackState.Closed;
            stateTimer = 0f;
            return;
        }
        
        AnimatorStateInfo stateInfo = backpackAnimator.GetCurrentAnimatorStateInfo(0);
        

        if (stateTimer > 5f)
        {
            DraumLogger.Warning(this, $"[BackpackStateMachine] TIMEOUT в TakingOn! Анимация не завершилась за 5 сек. Принудительно переходим в Closed. IsName = {stateInfo.IsName(takeOnStateName)}, normalizedTime = {stateInfo.normalizedTime:F2}");
            
            if (backpackAnimator != null)
            {
                backpackAnimator.SetBool(closeParameterHash, false);
            }
            
            currentState = BackpackState.Closed;
            stateTimer = 0f;
            return;
        }
        
        if (stateInfo.IsName(takeOnStateName) && stateInfo.normalizedTime >= 1.0f)
        {
            if (showDebugLogs) DraumLogger.Info(this, "[BackpackStateMachine] TakeOn завершён, закрытие завершено");
            
            if (backpackAnimator != null)
            {
                backpackAnimator.SetBool(closeParameterHash, false);
            }
            
            currentState = BackpackState.Closed;
            stateTimer = 0f;
        }
    }
    
    /// <summary>
    /// Проверяет завершена ли анимация закрытия
    /// </summary>
    public bool IsClosingComplete()
    {
        return currentState == BackpackState.Closed;
    }
    
    /// <summary>
    /// Проверяет завершена ли анимация открытия
    /// </summary>
    public bool IsOpeningComplete()
    {
        return currentState == BackpackState.Open;
    }
    
    /// <summary>
    /// Получить текущее состояние (для debug)
    /// </summary>
    public string GetCurrentState()
    {
        return currentState.ToString();
    }
    
    /// <summary>
    /// Вызывается через Animation Event в конце TakeOn анимации
    /// Переключает Input Map на Gameplay и скрывает курсор
    /// </summary>
    public void SwitchToGameplay()
    {
        if (showDebugLogs) DraumLogger.Info(this, "[BackpackStateMachine] Animation Event: SwitchToGameplay вызван");
        
        FirstPersonController fpc = FindFirstObjectByType<FirstPersonController>();
        if (fpc != null)
        {
            fpc.SendMessage("SwitchToGameplayMap", SendMessageOptions.DontRequireReceiver);
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            fpc.SendMessage("EnableMovement", SendMessageOptions.DontRequireReceiver);
            
            if (showDebugLogs) DraumLogger.Info(this, "[BackpackStateMachine] Переключено на Gameplay, курсор скрыт, движение разблокировано");
        }
        else
        {
            DraumLogger.Error(this, "[BackpackStateMachine] FirstPersonController не найден!");
        }
        
        currentState = BackpackState.Closed;
        stateTimer = 0f;
        
        if (backpackAnimator != null)
        {
            backpackAnimator.SetBool(closeParameterHash, false);
        }
    }
    
    /// <summary>
    /// Установить blendshapes для секции (вызывается из InventoryCameraController)
    /// </summary>
    public void SetSectionBlendshapes(InventoryCameraController.InventorySection section)
    {
        if (backpackMesh == null) return;
        
        float openValue = 0f;
        float leftValue = 0f;
        float rightValue = 0f;
        
        switch (section)
        {
            case InventoryCameraController.InventorySection.Main:
                openValue = mainSectionOpenValue;
                leftValue = 0f;
                rightValue = 0f;
                break;
                
            case InventoryCameraController.InventorySection.Closed:
                openValue = 0f;
                leftValue = 0f;
                rightValue = 0f;
                break;
                
            case InventoryCameraController.InventorySection.LeftPocket:
                openValue = leftPocketOpenValue;
                leftValue = leftPocketValue;
                rightValue = 0f;
                break;
                
            case InventoryCameraController.InventorySection.RightPocket:
                openValue = rightPocketOpenValue;
                leftValue = 0f;
                rightValue = rightPocketValue;
                break;
        }
        
        targetBlendshapeValue = openValue;
        
        if (openBlendshapeIndex != -1)
        {
            backpackMesh.SetBlendShapeWeight(openBlendshapeIndex, openValue);
        }
        
        if (leftPocketBlendshapeIndex != -1)
        {
            backpackMesh.SetBlendShapeWeight(leftPocketBlendshapeIndex, leftValue);
        }
        
        if (rightPocketBlendshapeIndex != -1)
        {
            backpackMesh.SetBlendShapeWeight(rightPocketBlendshapeIndex, rightValue);
        }
        
        if (showDebugLogs)
        {
            DraumLogger.Info(this, $"[BackpackStateMachine] Blendshapes установлены для {section}: Open={openValue}, L_PACKET={leftValue}, R_PACKET={rightValue}");
        }
    }
    
    /// <summary>
    /// Начинает плавный переход к новой секции с задержкой
    /// </summary>
    public void StartSectionTransition(InventoryCameraController.InventorySection newSection)
    {
        targetSection = newSection;
        isWaitingForBlendshapeStart = true;
        blendshapeStartTimer = 0f;
        
        if (showDebugLogs) DraumLogger.Info(this, $"[BackpackStateMachine] Начинаем переход к {newSection} с задержкой {blendshapeStartDelay:F1}с");
    }
    
    /// <summary>
    /// Обновляет плавный переход между секциями
    /// </summary>
    public void UpdateSectionTransition()
    {
        if (!isWaitingForBlendshapeStart) return;
        
        blendshapeStartTimer += Time.deltaTime;
        
        if (blendshapeStartTimer >= blendshapeStartDelay)
        {
            isWaitingForBlendshapeStart = false;
            StartSmoothBlendshapeTransition(targetSection);
            
            if (showDebugLogs) DraumLogger.Info(this, $"[BackpackStateMachine] Задержка завершена, начинаем плавный переход к {targetSection}");
        }
    }
    
    /// <summary>
    /// Обновляет плавный переход blendshape'ов (вызывается из Update)
    /// </summary>
    public void UpdateSmoothBlendshapeTransition()
    {
        if (!isTransitioningBlendshapes || backpackMesh == null) return;
        
        Vector3 lerpedValues = Vector3.zero;
        float currentSpeed = 0f;
        
        if (isClosingBlendshapes)
        {
            currentSpeed = blendshapeCloseSpeed;
            blendshapeTransitionProgress += Time.deltaTime * currentSpeed;
            blendshapeTransitionProgress = Mathf.Clamp01(blendshapeTransitionProgress);
            
            lerpedValues = Vector3.Lerp(startBlendshapeValues, Vector3.zero, blendshapeTransitionProgress);
            
            if (blendshapeTransitionProgress >= 1f)
            {
                isClosingBlendshapes = false;
                isOpeningBlendshapes = true;
                blendshapeTransitionProgress = 0f;
                
                if (showDebugLogs) DraumLogger.Info(this, $"[BackpackStateMachine] Фаза закрытия завершена, начинаем открытие");
            }
        }
        else if (isOpeningBlendshapes)
        {
            currentSpeed = blendshapeOpenSpeed;
            blendshapeTransitionProgress += Time.deltaTime * currentSpeed;
            blendshapeTransitionProgress = Mathf.Clamp01(blendshapeTransitionProgress);
            
            lerpedValues = Vector3.Lerp(Vector3.zero, targetBlendshapeValues, blendshapeTransitionProgress);
            
            if (blendshapeTransitionProgress >= 1f)
            {
                isOpeningBlendshapes = false;
                isTransitioningBlendshapes = false;
                
                if (showDebugLogs) DraumLogger.Info(this, $"[BackpackStateMachine] Двухфазный переход завершён");
                
                InitializeTabAfterBlendshapeTransition();
            }
        }
        
        if (openBlendshapeIndex != -1)
            backpackMesh.SetBlendShapeWeight(openBlendshapeIndex, lerpedValues.x);
            
        if (leftPocketBlendshapeIndex != -1)
            backpackMesh.SetBlendShapeWeight(leftPocketBlendshapeIndex, lerpedValues.y);
            
        if (rightPocketBlendshapeIndex != -1)
            backpackMesh.SetBlendShapeWeight(rightPocketBlendshapeIndex, lerpedValues.z);

        if (isOpeningBlendshapes)
        {
            if (lerpedValues.x >= 95f || lerpedValues.y >= 95f || lerpedValues.z >= 95f)
            {
                UpdateGridsWithBlendshapes(lerpedValues);
            }
        }
        else if (isClosingBlendshapes)
        {
            if (inventoryCameraController != null)
            {
                inventoryCameraController.HideAllGrids();
            }
        }
    }
    
    /// <summary>
    /// Начинает плавный переход blendshape'ов к целевой секции (двухфазный: закрыть → открыть)
    /// </summary>
    private void StartSmoothBlendshapeTransition(InventoryCameraController.InventorySection targetSection)
    {
        if (backpackMesh == null) return;
        
        startBlendshapeValues.x = (openBlendshapeIndex != -1) ? backpackMesh.GetBlendShapeWeight(openBlendshapeIndex) : 0f;
        startBlendshapeValues.y = (leftPocketBlendshapeIndex != -1) ? backpackMesh.GetBlendShapeWeight(leftPocketBlendshapeIndex) : 0f;
        startBlendshapeValues.z = (rightPocketBlendshapeIndex != -1) ? backpackMesh.GetBlendShapeWeight(rightPocketBlendshapeIndex) : 0f;
        
        targetBlendshapeValues = GetSectionBlendshapeValues(targetSection);
        
        isClosingBlendshapes = true;
        isOpeningBlendshapes = false;
        isTransitioningBlendshapes = true;
        blendshapeTransitionProgress = 0f;
        
        if (showDebugLogs) DraumLogger.Info(this, $"[BackpackStateMachine] Начинаем двухфазный переход: {startBlendshapeValues} → закрыть → {targetBlendshapeValues}");
    }
    
    
    /// <summary>
    /// Инициализирует TAB после завершения blendshape перехода
    /// </summary>
    private void InitializeTabAfterBlendshapeTransition()
    {
        FirstPersonController fpc = FindFirstObjectByType<FirstPersonController>();
        if (fpc != null)
        {
            var isTabTransitionField = typeof(FirstPersonController).GetField("isTabTransition", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (isTabTransitionField != null)
            {
                bool isTabTransition = (bool)isTabTransitionField.GetValue(fpc);
                
                if (isTabTransition)
                {
                    if (showDebugLogs) DraumLogger.Info(this, "[BackpackStateMachine] TAB переход обнаружен - инициализируем закрытие");
                    
                    fpc.SendMessage("InitializeTabAfterTransition", SendMessageOptions.DontRequireReceiver);
                }
                else
                {
                    if (showDebugLogs) DraumLogger.Info(this, "[BackpackStateMachine] Обычный WASD переход - TAB не инициализируем");
                }
            }
            else
            {
                DraumLogger.Warning(this, "[BackpackStateMachine] Поле isTabTransition не найдено в FirstPersonController!");
            }
        }
        else
        {
            DraumLogger.Warning(this, "[BackpackStateMachine] FirstPersonController не найден!");
        }
    }
    
    /// <summary>
    /// Получает значения blendshape'ов для секции
    /// </summary>
    private Vector3 GetSectionBlendshapeValues(InventoryCameraController.InventorySection section)
    {
        float openValue = 0f;
        float leftValue = 0f;
        float rightValue = 0f;
        
        switch (section)
        {
            case InventoryCameraController.InventorySection.Main:
                openValue = mainSectionOpenValue;
                leftValue = 0f;
                rightValue = 0f;
                break;
                
            case InventoryCameraController.InventorySection.LeftPocket:
                openValue = leftPocketOpenValue;
                leftValue = leftPocketValue;
                rightValue = 0f;
                break;
                
            case InventoryCameraController.InventorySection.RightPocket:
                openValue = rightPocketOpenValue;
                leftValue = 0f;
                rightValue = rightPocketValue;
                break;
                
            case InventoryCameraController.InventorySection.Closed:
                openValue = 0f;
                leftValue = 0f;
                rightValue = 0f;
                break;
                
            case InventoryCameraController.InventorySection.Special:
                openValue = 0f;
                leftValue = 0f;
                rightValue = 0f;
                break;
        }
        
        return new Vector3(openValue, leftValue, rightValue);
    }
    
    
    /// <summary>
    /// Управляет Grid'ами в зависимости от значений blendshape'ов
    /// </summary>
    private void UpdateGridsWithBlendshapes(Vector3 blendshapeValues)
    {
        if (inventoryCameraController == null) return;
        
        InventoryCameraController.InventorySection activeSection = DetermineActiveSection(blendshapeValues);
        
        inventoryCameraController.UpdateGridsForSection(activeSection, blendshapeValues);
    }
    
    /// <summary>
    /// Определяет активную секцию на основе значений blendshape'ов
    /// </summary>
    private InventoryCameraController.InventorySection DetermineActiveSection(Vector3 blendshapeValues)
    {
        float openValue = blendshapeValues.x;
        float leftValue = blendshapeValues.y;
        float rightValue = blendshapeValues.z;
        
        if (leftValue > 50f)
        {
            return InventoryCameraController.InventorySection.LeftPocket;
        }
        else if (rightValue > 50f)
        {
            return InventoryCameraController.InventorySection.RightPocket;
        }
        else if (openValue > 50f)
        {
            return InventoryCameraController.InventorySection.Main;
        }

        else
        {
            return InventoryCameraController.InventorySection.Closed;
        }
    }
}


