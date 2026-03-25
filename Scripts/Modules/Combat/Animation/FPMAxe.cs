using System;
using System.Collections;
using UnityEngine;
using DRAUM.Modules.Combat.Animation;
using DRAUM.Modules.Combat.Animation.Contracts;
using DRAUM.Modules.Combat;
using DRAUM.Core;
using DRAUM.Core.Infrastructure.Logger;
using DRAUM.Modules.Player.Events;
using DRAUM.Modules.Player;

public class FPMAxe : MonoBehaviour, ICameraAnimationStateProvider
{
    public Animator animator;

    [Range(-1f, 1f)]
    public float mouseX;
    [Range(0f, 1f)]
    public float mouseY;
    public float mouseSensitivity = 0.01f;
    public float backwardMultiplier = 2f;
    public float threshold = 0.4f;
    public float edgeThreshold = 0.9f;

    [Header("Feint Settings")]
    public AnimationCurve feintCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float feintDuration = 0.3f;
    
    [Header("Inventory Binding")]
    [Tooltip("Ссылка на Inventory (можно оставить пустым для автопоиска)")]
    public Inventory inventory;
    
    [Tooltip("Гриды экипировки по порядку (кнопки 1,2,3). Каждый грид = один слот.")]
    public InventoryGrid[] equipmentGrids = new InventoryGrid[3];
    
    [Tooltip("Player Animator (для управления weight слоя Stick при equip/unequip)")]
    public Animator playerAnimator;
    
    [Tooltip("Имя слоя Stick в Player Animator Controller")]
    public string stickLayerName = "Stick";
    
    [Tooltip("Скорость изменения weight слоя Stick")]
    [Range(1f, 20f)]
    public float stickLayerTransitionSpeed = 5f;
    
    [Tooltip("GameObject оружия (wooden_dock_pole1) - скрывается/показывается при equip/unequip")]
    public GameObject weaponGameObject;
    
    [Tooltip("Длительность анимации Unequip (сек) - используется как fallback, если не удалось получить из Animator")]
    [HideInInspector]
    public float unequipAnimationDuration = 1.0f;
    
    [Header("Camera Shake")]
    [Tooltip("CameraShakeController для подёргиваний камеры при ударах (автопоиск если не назначен)")]
    public CameraShakeController cameraShakeController;

    [Header("Stamina")]
    [Tooltip("Сущность игрока (HP/стамина). Автопоиск если не назначен.")]
    public PlayerEntity playerEntity;
    [Tooltip("Конфиг расхода стамины по оружиям и направлениям удара.")]
    public WeaponStaminaConfig weaponStaminaConfig;

    [Header("Debug")]
    [HideInInspector] public bool showDebugLogs = false;

    private bool isFeinting = false;
    private float feintTimer = 0f;
    private float startMouseX = 0f;
    
    public enum WindUpState { None, Left, Right, Up }
    private WindUpState currentWindUp = WindUpState.None;
    private bool isAttacking = false;
    private WindUpState lastAttackDirection = WindUpState.None;
    
    private bool windUpLeftActive = false;
    private bool windUpRightActive = false;
    private bool windUpUpActive = false;
    
    private bool isBlocking = false;
    private bool isEquipped = false;
    private int currentEquipSlot = -1;
    private Item equippedItem = null;
    
    private int swingLeftHash;
    private int swingRightHash;
    private int swingUpHash;
    private int stickWeightHash;
    
    private void Awake()
    {
        swingLeftHash = Animator.StringToHash("SwingLeft");
        swingRightHash = Animator.StringToHash("SwingRight");
        swingUpHash = Animator.StringToHash("SwingUp");
        stickWeightHash = Animator.StringToHash("StickWeight");
        
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventory>();
        }

        if (cameraShakeController == null)
        {
            cameraShakeController = FindFirstObjectByType<CameraShakeController>();
        }

        if (playerEntity == null)
            playerEntity = FindFirstObjectByType<PlayerEntity>();

        if (playerAnimator != null && !string.IsNullOrEmpty(stickLayerName))
        {
            stickLayerIndex = playerAnimator.GetLayerIndex(stickLayerName);
            if (stickLayerIndex == -1)
            {
                DraumLogger.Warning(this, $"[FPMAxe] Слой '{stickLayerName}' не найден в Player Animator Controller! Проверь имя слоя.");
            }
        }
    }
    
    private void Start()
    {
        CacheUnequipDuration();
    }
    
    /// <summary>
    /// Кэширует длительность анимации Unequip для оптимизации
    /// </summary>
    private void CacheUnequipDuration()
    {
        cachedUnequipDuration = GetUnequipAnimationDuration();
        DraumLogger.Info(this, $"[FPMAxe] Кэширована длительность анимации Unequip: {cachedUnequipDuration:F3} сек");
    }

    void Update()
    {
        UpdateStickLayerWeight();

        if (!isEquipped) return;
        
        if (animator == null) return;

        bool isWindUpActive = windUpLeftActive || windUpRightActive || windUpUpActive;
        
        if (!isWindUpActive && !isAttacking && !isBlocking)
        {
            float inputX = Input.GetAxis("Mouse X");
            float inputY = Input.GetAxis("Mouse Y");

            if ((inputX < 0 && mouseX > threshold) || (inputX > 0 && mouseX < -threshold))
            {
                mouseX += inputX * mouseSensitivity * backwardMultiplier;
            }
            else
            {
                mouseX += inputX * mouseSensitivity;
            }

            mouseY += inputY * mouseSensitivity * (1f + mouseY * 2f);
            mouseX = Mathf.Clamp(mouseX, -1f, 1f);
            mouseY = Mathf.Clamp(mouseY, 0f, 1f);
        }
        else if (isWindUpActive && !isAttacking && !isBlocking)
        {
            if (windUpLeftActive)
            {
                if (mouseX > -edgeThreshold)
                    mouseX = -edgeThreshold;
                mouseX = Mathf.Clamp(mouseX, -1f, -edgeThreshold);
            }
            else if (windUpRightActive)
            {
                if (mouseX < edgeThreshold)
                    mouseX = edgeThreshold;
                mouseX = Mathf.Clamp(mouseX, edgeThreshold, 1f);
            }
            else if (windUpUpActive)
            {
                float inputY = Input.GetAxis("Mouse Y");
                if (inputY > 0)
                {
                    mouseY += inputY * mouseSensitivity * (1f + mouseY * 2f);
                }
                mouseY = Mathf.Clamp(mouseY, edgeThreshold, 1f);
            }
        }

        if (isFeinting)
        {
            feintTimer += Time.deltaTime;
            if (feintTimer >= feintDuration)
            {
                isFeinting = false;
            }
        }

        animator.SetFloat("MouseX", mouseX);
        animator.SetFloat("MouseY", mouseY);

        CheckWindUpStates();
        
        UpdateStickLayerWeight();

        if (Input.GetMouseButtonDown(0))
        {
            DraumLogger.Info(this, $"[FPMAxe] ЛКМ нажата! isAttacking={isAttacking}, isBlocking={isBlocking}, isEquipped={isEquipped}, currentWindUp={currentWindUp}, windUpUpActive={windUpUpActive}, windUpLeftActive={windUpLeftActive}, windUpRightActive={windUpRightActive}, mouseY={mouseY}, mouseX={mouseX}");
            DraumLogger.Info(this, $"[FPMAxe] Animator: WindUpUpReady={animator.GetBool("WindUpUpReady")}, WindUpLeftReady={animator.GetBool("WindUpLeftReady")}, WindUpRightReady={animator.GetBool("WindUpRightReady")}");
            
            if (!isAttacking && !isBlocking)
            {
                TriggerAttack();
            }
            else
                DraumLogger.Warning(this, $"[FPMAxe] Атака заблокирована! isAttacking={isAttacking}, isBlocking={isBlocking}");
        }

        bool shouldBlock = Input.GetMouseButton(1) && !isAttacking;
        if (shouldBlock && !isBlocking)
        {
            StartBlock();
        }
        else if (!shouldBlock && isBlocking)
        {
            StopBlock();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            bool isSwinging = animator.GetBool("SwingLeft") || animator.GetBool("SwingRight") || animator.GetBool("SwingUp");
            
            if (isAttacking || isSwinging)
            {
                DraumLogger.Info(this, $"[FPMAxe] Нажатие R заблокировано: идет анимация удара (isAttacking={isAttacking}, SwingLeft={animator.GetBool("SwingLeft")}, SwingRight={animator.GetBool("SwingRight")}, SwingUp={animator.GetBool("SwingUp")})");
                return;
            }
            
            StartFeint();
        }

    }

    private void CheckWindUpStates()
    {
        bool animatorWindUpUp = animator.GetBool("WindUpUpReady");
        bool animatorWindUpLeft = animator.GetBool("WindUpLeftReady");
        bool animatorWindUpRight = animator.GetBool("WindUpRightReady");
        
        if (!isAttacking)
        {
            if (animatorWindUpUp && !windUpUpActive)
            {
                DraumLogger.Info(this, $"[FPMAxe] CheckWindUpStates: Animator установил WindUpUpReady, синхронизирую windUpUpActive=true, currentWindUp=Up");
                windUpUpActive = true;
                currentWindUp = WindUpState.Up;
            }
            if (animatorWindUpLeft && !windUpLeftActive)
            {
                windUpLeftActive = true;
                currentWindUp = WindUpState.Left;
            }
            if (animatorWindUpRight && !windUpRightActive)
            {
                windUpRightActive = true;
                currentWindUp = WindUpState.Right;
            }
            
            if (mouseX <= -edgeThreshold && !windUpLeftActive)
            {
                windUpLeftActive = true;
                currentWindUp = WindUpState.Left;
                animator.SetBool("SwingRight", false);
                animator.SetBool("SwingUp", false);
            }
            else if (mouseX >= edgeThreshold && !windUpRightActive)
            {
                windUpRightActive = true;
                currentWindUp = WindUpState.Right;
                animator.SetBool("SwingLeft", false);
                animator.SetBool("SwingUp", false);
            }
            else if (mouseY >= edgeThreshold && !windUpUpActive)
            {
                windUpUpActive = true;
                currentWindUp = WindUpState.Up;
                animator.SetBool("SwingLeft", false);
                animator.SetBool("SwingRight", false);
            }
            
            if (windUpLeftActive && !animatorWindUpLeft && mouseX > -edgeThreshold + 0.05f)
            {
                windUpLeftActive = false;
                if (currentWindUp == WindUpState.Left)
                    currentWindUp = WindUpState.None;
            }
            else if (windUpRightActive && !animatorWindUpRight && mouseX < edgeThreshold - 0.05f)
            {
                windUpRightActive = false;
                if (currentWindUp == WindUpState.Right)
                    currentWindUp = WindUpState.None;
            }
            else if (windUpUpActive && !animatorWindUpUp && mouseY < edgeThreshold - 0.05f)
            {
                windUpUpActive = false;
                if (currentWindUp == WindUpState.Up)
                    currentWindUp = WindUpState.None;
            }
            
            if (windUpUpActive && currentWindUp != WindUpState.Up)
            {
                DraumLogger.Info(this, $"[FPMAxe] CheckWindUpStates: Исправляю currentWindUp с {currentWindUp} на Up (windUpUpActive=true)");
                currentWindUp = WindUpState.Up;
            }
            if (windUpLeftActive && currentWindUp != WindUpState.Left)
            {
                currentWindUp = WindUpState.Left;
            }
            if (windUpRightActive && currentWindUp != WindUpState.Right)
            {
                currentWindUp = WindUpState.Right;
            }
        }

        animator.SetBool("WindUpLeftReady", windUpLeftActive);
        animator.SetBool("WindUpRightReady", windUpRightActive);
        animator.SetBool("WindUpUpReady", windUpUpActive);
        
        if (mouseY >= 0.99f && windUpUpActive)
        {
            DraumLogger.Info(this, $"[FPMAxe] CheckWindUpStates: mouseY={mouseY}, windUpUpActive={windUpUpActive}, currentWindUp={currentWindUp}, animatorWindUpUp={animatorWindUpUp}");
        }
    }

    /// <summary>
    /// Переключает экипировку по индексу слота (0/1/2). Проверяет предмет в соответствующем Equipment Grid.
    /// </summary>
    public void ToggleEquipSlot(int slotIndex)
    {
        if (slotIndex < 0) return;
        
        if (isUnequipping)
        {
                DraumLogger.Warning(this, $"[FPMAxe] Equip slot {slotIndex}: Unequip уже в процессе, игнорируем нажатие. Подожди завершения анимации.");
            return;
        }
        
        InventoryGrid grid = (equipmentGrids != null && slotIndex < equipmentGrids.Length) ? equipmentGrids[slotIndex] : null;
        if (grid == null || grid.items == null)
        {
            DraumLogger.Info(this, $"[FPMAxe] Equip slot {slotIndex}: grid null или items null");
            return;
        }
        
        Item item = GetItemFromGrid(grid);
        
        if (item == null)
        {
            if (isEquipped && !isUnequipping)
            {
                DraumLogger.Info(this, $"[FPMAxe] Equip slot {slotIndex}: предмет отсутствует, снимаем оружие");
                StartUnequip();
            }
            else
                DraumLogger.Info(this, $"[FPMAxe] Equip slot {slotIndex}: предмет отсутствует, ничего не делаем (isEquipped={isEquipped}, isUnequipping={isUnequipping})");
            return;
        }
        
        if (isEquipped && currentEquipSlot == slotIndex && !isUnequipping)
        {
            DraumLogger.Info(this, $"[FPMAxe] Equip slot {slotIndex}: повторное нажатие, Unequip");
            StartUnequip();
            return;
        }
        
        if (isEquipped && currentEquipSlot != slotIndex && !isUnequipping)
        {
            DraumLogger.Info(this, $"[FPMAxe] Equip slot {slotIndex}: переключаем с {currentEquipSlot} на {slotIndex}");
            StartUnequip(() => EquipWithItem(item, slotIndex));
            return;
        }

        if (!isUnequipping)
        {
            EquipWithItem(item, slotIndex);
        }
    }
    
    /// <summary>
    /// Берет предмет из Equipment Grid (позиция 0,0 — гриды по одному слоту)
    /// </summary>
    private Item GetItemFromGrid(InventoryGrid grid)
    {
        if (grid == null || grid.items == null) return null;
        if (grid.items.GetLength(0) == 0 || grid.items.GetLength(1) == 0) return null;
        return grid.items[0, 0];
    }
    
    private InventoryGrid equippedItemGrid;
    private Vector2Int equippedItemPosition; 
    private Action unequipCompleteCallback; 
    private bool isUnequipping = false;
    private Coroutine unequipRoutine = null;
    private float cachedUnequipDuration = -1f;
    
    private int stickLayerIndex = -1; 
    private float targetStickWeight = 0f; 
    private float currentStickWeight = 0f;
    
    private void EquipWithItem(Item item, int slotIndex)
    {
        if (item == null || inventory == null)
        {
                DraumLogger.Warning(this, "[FPMAxe] EquipWithItem: item или inventory null!");
            return;
        }
        
        equippedItem = item;
        currentEquipSlot = slotIndex;
        equippedItemGrid = item.inventoryGrid;
        equippedItemPosition = item.indexPosition;
        
        inventory.ClearItemReferences(item);
        
        if (item.gameObject != null)
        {
            item.gameObject.SetActive(false);
        }
        
        isEquipped = true;
        
        if (weaponGameObject != null) weaponGameObject.SetActive(true);
        
        if (animator != null)
        {
            animator.gameObject.SetActive(true);
            animator.enabled = true;
        }
        
        if (playerAnimator != null && stickLayerIndex != -1)
        {

            currentStickWeight = 0.1f;
            playerAnimator.SetLayerWeight(stickLayerIndex, 0.1f);

            targetStickWeight = 1f;
        }
        

        if (animator != null)
        {
            animator.SetTrigger("Equip");
            TriggerFired?.Invoke("Equip");
        }
        

        OnEquipped();

        DraumLogger.Info(this, $"[FPMAxe] EquipWithItem: экипирован слот {slotIndex}, предмет {(item != null ? item.name : "NULL")}, убран из инвентаря");
    }
    
    private void StartUnequip(Action onComplete = null)
    {
        if (isUnequipping)
        {
            DraumLogger.Warning(this, "[FPMAxe] StartUnequip: Unequip уже в процессе, сохраняем новый callback");

            if (unequipCompleteCallback != null && onComplete != null)
            {
                Action oldCallback = unequipCompleteCallback;
                unequipCompleteCallback = () =>
                {
                    oldCallback?.Invoke();
                    onComplete?.Invoke();
                };
            }
            else if (onComplete != null)
            {
                unequipCompleteCallback = onComplete;
            }
            return;
        }
        
        if (!isEquipped)
        {
            DraumLogger.Warning(this, "[FPMAxe] StartUnequip: оружие не экипировано, ничего не делаем");
            onComplete?.Invoke();
            return;
        }
        
        if (cachedUnequipDuration < 0f)
        {
            CacheUnequipDuration();
        }
        
        unequipCompleteCallback = onComplete;
        isUnequipping = true;
        
        if (animator != null)
        {
            animator.SetTrigger("Unequip");
            TriggerFired?.Invoke("Unequip");
        }

        if (unequipRoutine != null)
        {
            StopCoroutine(unequipRoutine);
        }
        unequipRoutine = StartCoroutine(UnequipRoutine());
        
        DraumLogger.Info(this, $"[FPMAxe] StartUnequip: запущена анимация Unequip, ожидаем {cachedUnequipDuration:F3} сек для завершения");
    }
    
    /// <summary>
    /// Корутина для ожидания завершения анимации Unequip
    /// </summary>
    private IEnumerator UnequipRoutine()
    {
        yield return new WaitForSeconds(cachedUnequipDuration);
        
        FinishUnequip();
    }
    
    /// <summary>
    /// Завершает unequip: выключает руки, возвращает предмет в инвентарь, вызывает callback
    /// </summary>
    private void FinishUnequip()
    {
        if (!isUnequipping)
        {
            DraumLogger.Warning(this, "[FPMAxe] FinishUnequip: вызван, но isUnequipping = false - игнорируем");
            return;
        }
        
        DraumLogger.Info(this, "[FPMAxe] FinishUnequip: анимация Unequip завершена, выключаем руки и завершаем unequip");
        
        OnUnequipped();
        
        SetStickLayerWeight(0f);
        if (weaponGameObject != null) weaponGameObject.SetActive(false);
        
        if (animator != null)
        {
            animator.enabled = false;
            animator.gameObject.SetActive(false);
        }
        
        if (equippedItem != null && equippedItemGrid != null && inventory != null)
        {
            int slotX = equippedItemPosition.x;
            int slotY = equippedItemPosition.y;
            
            if (slotX >= 0 && slotX < equippedItemGrid.items.GetLength(0) &&
                slotY >= 0 && slotY < equippedItemGrid.items.GetLength(1))
            {
                equippedItemGrid.items[slotX, slotY] = equippedItem;
                equippedItem.indexPosition = equippedItemPosition;
                equippedItem.inventoryGrid = equippedItemGrid;
                
                if (equippedItem.gameObject != null)
                {
                    equippedItem.gameObject.SetActive(true);
                }
                
                DraumLogger.Info(this, $"[FPMAxe] FinishUnequip: предмет {(equippedItem != null ? equippedItem.name : "NULL")} возвращен в инвентарь на позицию ({slotX}, {slotY})");
            }
        }
        
        isEquipped = false;
        currentEquipSlot = -1;
        Item tempItem = equippedItem;
        equippedItem = null;
        equippedItemGrid = null;
        equippedItemPosition = Vector2Int.zero;
        isUnequipping = false;
        unequipRoutine = null;
        
        Action callback = unequipCompleteCallback;
        unequipCompleteCallback = null;
        callback?.Invoke();
    }
    
    
    private void TriggerAttack()
    {
        DraumLogger.Info(this, $"[FPMAxe] TriggerAttack вызван! currentWindUp={currentWindUp}, windUpUpActive={windUpUpActive}, windUpLeftActive={windUpLeftActive}, windUpRightActive={windUpRightActive}");
        
        if (currentWindUp == WindUpState.None)
        {
            DraumLogger.Info(this, "[FPMAxe] currentWindUp == None, пытаюсь восстановить из флагов...");
            if (windUpUpActive)
            {
                currentWindUp = WindUpState.Up;
                DraumLogger.Info(this, "[FPMAxe] Восстановлен currentWindUp = Up из windUpUpActive");
            }
            else if (windUpLeftActive)
            {
                currentWindUp = WindUpState.Left;
                DraumLogger.Info(this, "[FPMAxe] Восстановлен currentWindUp = Left из windUpLeftActive");
            }
            else if (windUpRightActive)
            {
                currentWindUp = WindUpState.Right;
                DraumLogger.Info(this, "[FPMAxe] Восстановлен currentWindUp = Right из windUpRightActive");
            }
            else
            {
                DraumLogger.Error(this, "[FPMAxe] Невозможно атаковать! Все флаги windUp неактивны!");
                return;
            }
        }

        string swingDirection = currentWindUp == WindUpState.Up ? "Up" : currentWindUp == WindUpState.Left ? "Left" : "Right";
        if (playerEntity != null && weaponStaminaConfig != null)
        {
            string weaponName = equippedItem != null && equippedItem.data != null ? equippedItem.data.name : "stick";
            float cost = weaponStaminaConfig.GetStaminaCost(weaponName, swingDirection) * playerEntity.EffectsStaminaCostMultiplier;
            if (!playerEntity.CanConsumeStamina(cost))
                return;
            playerEntity.ConsumeStamina(cost);
        }

        DraumLogger.Info(this, $"[FPMAxe] Атака запущена! currentWindUp={currentWindUp}");
        isAttacking = true;
        lastAttackDirection = currentWindUp;

        animator.SetBool("SwingLeft", false);
        animator.SetBool("SwingRight", false);
        animator.SetBool("SwingUp", false);

        swingDirection = "";
        switch (currentWindUp)
        {
            case WindUpState.Left:
                DraumLogger.Info(this, "[FPMAxe] Устанавливаю SwingLeft = true");
                animator.SetBool("SwingLeft", true);
                windUpLeftActive = false;
                swingDirection = "Left";
                break;
            case WindUpState.Right:
                DraumLogger.Info(this, "[FPMAxe] Устанавливаю SwingRight = true");
                animator.SetBool("SwingRight", true);
                windUpRightActive = false;
                swingDirection = "Right";
                break;
            case WindUpState.Up:
                DraumLogger.Info(this, "[FPMAxe] Устанавливаю SwingUp = true");
                animator.SetBool("SwingUp", true);
                windUpUpActive = false;
                mouseY = 0f;
                swingDirection = "Up";
                break;
        }
        
        if (EventBus.Instance != null)
        {
            string weaponName = equippedItem != null && equippedItem.data != null 
                ? equippedItem.data.name 
                : "DefaultWeapon";
            
            PlayerCombatHitEvent hitEvent = new PlayerCombatHitEvent
            {
                WeaponName = weaponName,
                MaterialName = "",
                HitPosition = transform.position,
                SwingDirection = swingDirection,
                IsImpact = false,
                HitCollider = null,
                HitSpeed = 0f,
                WeaponMass = 0f,
                BaseForceMultiplier = 0f,
                SpeedForceMultiplier = 0f
            };
            
            EventBus.Instance.Publish(hitEvent);
            
            DraumLogger.Info(this, $"[FPMAxe] Опубликовано событие начала атаки: Weapon={weaponName}, Direction={swingDirection}");
        }

        animator.SetBool("WindUpLeftReady", false);
        animator.SetBool("WindUpRightReady", false);
        animator.SetBool("WindUpUpReady", false);

        DraumLogger.Info(this, "[FPMAxe] Атака завершена, все WindUp флаги сброшены");
    }

    /// <summary>
    /// Вызывается из AnimationEvent (string) в клипах оружия.
    /// Примеры cue: SwingLeft, SwingRight, SwingUp, Impact, Recover.
    /// </summary>
    public void AnimationEventCombatCue(string cueKey)
    {
        if (string.IsNullOrWhiteSpace(cueKey)) return;
        if (EventBus.Instance == null) return;

        string target = "All";
        string normalizedCue = cueKey.Trim();
        int separatorIndex = normalizedCue.IndexOf(':');
        if (separatorIndex <= 0)
        {
            separatorIndex = normalizedCue.IndexOf('/');
        }

        if (separatorIndex > 0)
        {
            string prefix = normalizedCue.Substring(0, separatorIndex).Trim();
            string payload = normalizedCue.Substring(separatorIndex + 1).Trim();
            if (!string.IsNullOrEmpty(payload) &&
                (prefix.Equals("Camera", StringComparison.OrdinalIgnoreCase) ||
                 prefix.Equals("Audio", StringComparison.OrdinalIgnoreCase) ||
                 prefix.Equals("Cursor", StringComparison.OrdinalIgnoreCase) ||
                 prefix.Equals("All", StringComparison.OrdinalIgnoreCase)))
            {
                target = prefix;
                normalizedCue = payload;
            }
        }

        string direction = "";
        if (normalizedCue.Contains("Left", StringComparison.OrdinalIgnoreCase)) direction = "Left";
        else if (normalizedCue.Contains("Right", StringComparison.OrdinalIgnoreCase)) direction = "Right";
        else if (normalizedCue.Contains("Up", StringComparison.OrdinalIgnoreCase)) direction = "Up";
        else if (lastAttackDirection == WindUpState.Left) direction = "Left";
        else if (lastAttackDirection == WindUpState.Right) direction = "Right";
        else if (lastAttackDirection == WindUpState.Up) direction = "Up";

        string weaponName = equippedItem != null && equippedItem.data != null
            ? equippedItem.data.name
            : "DefaultWeapon";

        EventBus.Instance.Publish(new CombatAnimationCueEvent
        {
            Target = target,
            CueKey = normalizedCue,
            WeaponName = weaponName,
            SwingDirection = direction,
            Position = transform.position
        });

        DraumLogger.Info(this, $"[FPMAxe] AnimationEvent cue опубликован: Target={target}, Cue={normalizedCue}, Weapon={weaponName}, Direction={direction}");
    }

    public void OnAttackCompleted()
    {
        if (!isAttacking) return;

        isAttacking = false;

        animator.SetBool("SwingLeft", false);
        animator.SetBool("SwingRight", false);
        animator.SetBool("SwingUp", false);

        if (lastAttackDirection == WindUpState.Left)
        {
            windUpRightActive = true;
            animator.SetBool("WindUpRightReady", true);
            currentWindUp = WindUpState.Right;
            mouseX = 1f;
        }
        else if (lastAttackDirection == WindUpState.Right)
        {
            windUpLeftActive = true;
            animator.SetBool("WindUpLeftReady", true);
            currentWindUp = WindUpState.Left;
            mouseX = -1f;
        }
        else if (lastAttackDirection == WindUpState.Up)
        {
            currentWindUp = WindUpState.None;
            mouseY = 0f;
            windUpUpActive = false;
            animator.SetBool("WindUpUpReady", false);
        }
    }

    private void StartFeint()
    {
        if (isBlocking)
        {
            StopBlock();
            mouseX = 0f;
            mouseY = 0f;
            windUpLeftActive = false;
            windUpRightActive = false;
            windUpUpActive = false;
            currentWindUp = WindUpState.None;
            animator.SetBool("WindUpLeftReady", false);
            animator.SetBool("WindUpRightReady", false);
            animator.SetBool("WindUpUpReady", false);
            return;
        }

        if (currentWindUp == WindUpState.None) return;

        animator.SetBool("SwingLeft", false);
        animator.SetBool("SwingRight", false);
        animator.SetBool("SwingUp", false);

        if (isAttacking)
        {
            isAttacking = false;
            return;
        }

        isFeinting = true;
        feintTimer = 0f;
        startMouseX = mouseX;
        
        mouseX = 0f;
        mouseY = 0f;
        
        windUpLeftActive = false;
        windUpRightActive = false;
        windUpUpActive = false;
        
        animator.SetBool("WindUpLeftReady", false);
        animator.SetBool("WindUpRightReady", false);
        animator.SetBool("WindUpUpReady", false);
        
        currentWindUp = WindUpState.None;
    }

    private void StartBlock()
    {
        if (isAttacking) return;
        if (isBlocking) return;

        mouseX = 0f;
        mouseY = 0f;
        
        windUpLeftActive = false;
        windUpRightActive = false;
        windUpUpActive = false;
        currentWindUp = WindUpState.None;
        
        animator.SetBool("WindUpLeftReady", false);
        animator.SetBool("WindUpRightReady", false);
        animator.SetBool("WindUpUpReady", false);
        
        isBlocking = true;
        animator.SetTrigger("BlockStart");
        animator.SetBool("IsBlocking", true);
        TriggerFired?.Invoke("BlockStart");
    }

    private void StopBlock()
    {
        if (!isBlocking) return;

        isBlocking = false;
        animator.SetBool("IsBlocking", false);
    }

    /// <summary>
    /// Вызывается EquipmentSlot после завершения анимации Equip
    /// </summary>
    public void OnEquipped()
    {
        isEquipped = true;
        DraumLogger.Info(this, "[FPMAxe] OnEquipped вызван - оружие экипировано");
    }

    /// <summary>
    /// Вызывается EquipmentSlot перед/после анимации Unequip
    /// </summary>
    public void OnUnequipped()
    {
        isEquipped = false;
        isBlocking = false;
        isAttacking = false;
        currentWindUp = WindUpState.None;
        windUpLeftActive = false;
        windUpRightActive = false;
        windUpUpActive = false;
        
        if (animator != null)
        {
            animator.SetBool("IsBlocking", false);
            animator.SetBool("WindUpLeftReady", false);
            animator.SetBool("WindUpRightReady", false);
            animator.SetBool("WindUpUpReady", false);
            animator.SetBool("SwingLeft", false);
            animator.SetBool("SwingRight", false);
            animator.SetBool("SwingUp", false);
        }
        
        DraumLogger.Info(this, "[FPMAxe] OnUnequipped вызван - оружие снято");
    }

    public void ResetMouseX()
    {
        mouseX = 0f;
        mouseY = 0f;
        currentWindUp = WindUpState.None;
        isAttacking = false;
        isBlocking = false;
        windUpLeftActive = false;
        windUpRightActive = false;
        windUpUpActive = false;
        if (animator != null)
        {
            animator.SetFloat("MouseX", mouseX);
            animator.SetFloat("MouseY", mouseY);
            animator.SetBool("WindUpLeftReady", false);
            animator.SetBool("WindUpRightReady", false);
            animator.SetBool("WindUpUpReady", false);
            animator.SetBool("SwingLeft", false);
            animator.SetBool("SwingRight", false);
            animator.SetBool("SwingUp", false);
            animator.SetBool("IsBlocking", false);
        }
    }
    
    #region Public API for WeaponGoreDemo
    
    /// <summary>
    /// Проверяет, активна ли атака в данный момент
    /// </summary>
    public bool IsAttacking()
    {
        return isAttacking;
    }
    
    /// <summary>
    /// Проверяет, блокирует ли игрок в данный момент
    /// </summary>
    public bool IsBlocking()
    {
        return isBlocking;
    }
    
    /// <summary>
    /// Возвращает направление последней атаки
    /// </summary>
    public WindUpState GetLastAttackDirection()
    {
        return lastAttackDirection;
    }
    
    /// <summary>
    /// Проверяет, экипирован ли топор
    /// </summary>
    public bool IsEquipped()
    {
        return isEquipped;
    }
    
    /// <summary>
    /// Получает текущий слот экипировки (-1 если не экипирован)
    /// </summary>
    public int GetCurrentEquipSlot()
    {
        return currentEquipSlot;
    }
    
    /// <summary>
    /// Устанавливает целевой weight слоя Stick (0 или 1)
    /// </summary>
    private void SetStickLayerWeight(float targetWeight)
    {
        targetStickWeight = Mathf.Clamp01(targetWeight);
    }
    
    /// <summary>
    /// Обновляет weight слоя Stick (плавная интерполяция к целевому значению)
    /// </summary>
    private void UpdateStickLayerWeight()
    {
        if (playerAnimator == null || stickLayerIndex == -1)
        {
            return;
        }
        
        currentStickWeight = Mathf.MoveTowards(
            currentStickWeight,
            targetStickWeight,
            stickLayerTransitionSpeed * Time.deltaTime
        );
        
        playerAnimator.SetLayerWeight(stickLayerIndex, currentStickWeight);
        
        int stickWeightValue = (currentStickWeight >= 1f) ? 1 : 0;
        playerAnimator.SetInteger(stickWeightHash, stickWeightValue);
    }
    
    /// <summary>
    /// Вызывает unequip для открытия инвентаря (публичный метод)
    /// </summary>
    public void UnequipForInventory(Action onComplete = null)
    {
        if (!isEquipped)
        {
            onComplete?.Invoke();
            return;
        }

        DraumLogger.Info(this, "[FPMAxe] UnequipForInventory: запускаем unequip при открытии инвентаря");
        
        StartUnequip(onComplete);
    }
    
    /// <summary>
    /// Возвращает длительность анимации Unequip из Animator.
    /// Сначала пытается получить из текущего состояния, если анимация играет.
    /// Иначе ищет AnimationClip по имени в Animator Controller.
    /// </summary>
    public float GetUnequipAnimationDuration()
    {
        if (animator == null)
        {
            DraumLogger.Warning(this, "[FPMAxe] GetUnequipAnimationDuration: animator == null, возвращаем fallback значение");
            return unequipAnimationDuration;
        }
        
        if (animator.enabled)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Unequip") || stateInfo.fullPathHash == Animator.StringToHash("Base Layer.Unequip"))
            {
                float duration = stateInfo.length;
                DraumLogger.Info(this, $"[FPMAxe] GetUnequipAnimationDuration: получена из текущего состояния = {duration:F3} сек");
                return duration;
            }
            
            if (animator.IsInTransition(0))
            {
                AnimatorStateInfo nextStateInfo = animator.GetNextAnimatorStateInfo(0);
                if (nextStateInfo.IsName("Unequip") || nextStateInfo.fullPathHash == Animator.StringToHash("Base Layer.Unequip"))
                {
                    float duration = nextStateInfo.length;
                    DraumLogger.Info(this, $"[FPMAxe] GetUnequipAnimationDuration: получена из следующего состояния (переход) = {duration:F3} сек");
                    return duration;
                }
            }
        }
        
        if (animator.runtimeAnimatorController != null)
        {
            RuntimeAnimatorController ac = animator.runtimeAnimatorController;
            foreach (AnimationClip clip in ac.animationClips)
            {
                if (clip.name.Equals("UnEquip", System.StringComparison.OrdinalIgnoreCase) || 
                    clip.name.IndexOf("UnEquip", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    float duration = clip.length;
                    DraumLogger.Info(this, $"[FPMAxe] GetUnequipAnimationDuration: найдена анимация {clip.name}, длительность = {duration:F3} сек");
                    return duration;
                }
            }
        }
        
        DraumLogger.Warning(this, $"[FPMAxe] GetUnequipAnimationDuration: анимация Unequip не найдена в Animator Controller, возвращаем fallback значение = {unequipAnimationDuration:F3} сек");
        return unequipAnimationDuration;
    }

    #endregion

    #region ICameraAnimationStateProvider

    public event Action<string> TriggerFired;

    public bool TryGetFloat(string parameterName, out float value)
    {
        value = 0f;
        if (parameterName == "MouseX") { value = mouseX; return true; }
        if (parameterName == "MouseY") { value = mouseY; return true; }
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        
        int paramCount = animator.parameterCount;
        for (int i = 0; i < paramCount; i++)
        {
            if (i >= paramCount || animator.runtimeAnimatorController == null) break;
            
            try
            {
                var p = animator.GetParameter(i);
                if (p.type == AnimatorControllerParameterType.Float && p.name == parameterName)
                {
                    value = animator.GetFloat(p.nameHash);
                    return true;
                }
            }
            catch (System.IndexOutOfRangeException)
            {
                break;
            }
        }
        return false;
    }

    public bool TryGetBool(string parameterName, out bool value)
    {
        value = false;
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        
        int paramCount = animator.parameterCount;
        for (int i = 0; i < paramCount; i++)
        {
            if (i >= paramCount || animator.runtimeAnimatorController == null) break;
            
            try
            {
                var p = animator.GetParameter(i);
                if (p.type == AnimatorControllerParameterType.Bool && p.name == parameterName)
                {
                    value = animator.GetBool(p.nameHash);
                    return true;
                }
            }
            catch (System.IndexOutOfRangeException)
            {
                break;
            }
        }
        return false;
    }

    public bool TryGetInt(string parameterName, out int value)
    {
        value = 0;
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        
        int paramCount = animator.parameterCount;
        for (int i = 0; i < paramCount; i++)
        {
            if (i >= paramCount || animator.runtimeAnimatorController == null) break;
            
            try
            {
                var p = animator.GetParameter(i);
                if (p.type == AnimatorControllerParameterType.Int && p.name == parameterName)
                {
                    value = animator.GetInteger(p.nameHash);
                    return true;
                }
            }
            catch (System.IndexOutOfRangeException)
            {
                break;
            }
        }
        return false;
    }

    public string GetCurrentCameraAnimationLayerName()
    {
        if (equippedItem?.data == null) return "";
        return equippedItem.data.cameraAnimationLayerName ?? "";
    }

    #endregion
}
