using UnityEngine;

/// <summary>
/// ScriptableObject конфигурация для настройки движения персонажа
/// </summary>
[CreateAssetMenu(fileName = "New Movement Config", menuName = "FPS Controller/Movement Config", order = 1)]
public class MovementConfig : ScriptableObject
{
    [Header("Base Movement")]
    [Tooltip("Базовая скорость ходьбы")]
    public float walkSpeed = 5f;
    
    [Tooltip("Максимальная скорость бега")]
    public float sprintSpeed = 7f;
    
    [Tooltip("Максимальное изменение скорости")]
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
    
    [Tooltip("Кривая ускорения (опционально)")]
    public AnimationCurve accelerationCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
    [Tooltip("Кривая замедления (опционально)")]
    public AnimationCurve decelerationCurve = AnimationCurve.Linear(0, 1, 1, 0);
    
    [Header("Jump")]
    [Tooltip("Сила прыжка")]
    public float jumpPower = 5f;
    
    [Header("Crouch")]
    [Tooltip("Высота персонажа при присяде")]
    [Range(0.1f, 1f)]
    public float crouchHeight = 0.75f;
    
    [Tooltip("Множитель скорости при присяде")]
    [Range(0.1f, 1f)]
    public float crouchSpeedMultiplier = 0.5f;
    
    [Header("Sprint")]
    [Tooltip("FOV при беге")]
    public float sprintFOV = 80f;
    
    [Tooltip("Скорость изменения FOV при беге")]
    public float sprintFOVStepTime = 10f;
    
    [Tooltip("Использовать плавное изменение FOV при спринте")]
    public bool useSmoothSprintFOV = true;
    
    [Tooltip("Время изменения FOV при начале спринта (сек)")]
    [Range(0.1f, 3f)]
    public float sprintFOVTransitionTime = 0.5f;
    
    [Tooltip("Длительность спринта (если не безлимитный)")]
    public float sprintDuration = 5f;
    
    [Tooltip("Время восстановления спринта")]
    public float sprintCooldown = 0.5f;
    
    [Header("Sprint Acceleration")]
    [Tooltip("Использовать плавное ускорение спринта")]
    public bool useSprintAcceleration = true;
    
    [Tooltip("Время разгона от walkSpeed до sprintSpeed (сек)")]
    [Range(0.1f, 5f)]
    public float sprintAccelerationTime = 2f;
    
    [Tooltip("Время замедления от sprintSpeed до walkSpeed (сек)")]
    [Range(0.1f, 5f)]
    public float sprintDecelerationTime = 1f;
    
    [Tooltip("Кривая ускорения спринта (опционально, 0-1)")]
    public AnimationCurve sprintAccelerationCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
    [Tooltip("Кривая замедления спринта (опционально, 0-1)")]
    public AnimationCurve sprintDecelerationCurve = AnimationCurve.Linear(0, 1, 1, 0);
    
    [Header("Physics")]
    [Tooltip("Linear Damping когда игрок на земле")]
    [Range(0f, 10f)]
    public float groundedLinearDamping = 5f;
    
    [Tooltip("Linear Damping когда игрок в воздухе")]
    [Range(0f, 5f)]
    public float airborneLinearDamping = 0.1f;
    
    [Tooltip("Скорость изменения Linear Damping")]
    [Range(1f, 20f)]
    public float dampingChangeSpeed = 10f;
}

