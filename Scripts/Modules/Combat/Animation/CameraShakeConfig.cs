using UnityEngine;

namespace DRAUM.Modules.Combat.Animation
{
    /// <summary>
    /// ScriptableObject конфигурация подёргиваний камеры при ударах.
    /// Содержит настройки для каждого типа удара (Left, Right, Up).
    /// </summary>
    [CreateAssetMenu(fileName = "NewCameraShakeConfig", menuName = "DRAUM/Combat/Camera Shake Config")]
    public class CameraShakeConfig : ScriptableObject
    {
        [Header("Attack Camera Shake Settings")]
        [Tooltip("Настройки подёргивания для удара слева")]
        public CameraShakeSettings leftAttackShake = new CameraShakeSettings
        {
            horizontalIntensity = 3f,
            verticalIntensity = 1f,
            duration = 0.2f,
            horizontalDirection = -1f,
            verticalDirection = 0f
        };

        [Tooltip("Настройки подёргивания для удара справа")]
        public CameraShakeSettings rightAttackShake = new CameraShakeSettings
        {
            horizontalIntensity = 3f,
            verticalIntensity = 1f,
            duration = 0.2f,
            horizontalDirection = 1f,
            verticalDirection = 0f
        };

        [Tooltip("Настройки подёргивания для удара сверху")]
        public CameraShakeSettings upAttackShake = new CameraShakeSettings
        {
            horizontalIntensity = 1f,
            verticalIntensity = 2f,
            duration = 0.25f,
            horizontalDirection = 0f,
            verticalDirection = -1f
        };

        [Header("Global Settings")]
        [Tooltip("Включить подёргивания камеры при ударах")]
        public bool enableCameraShake = true;

        [Tooltip("Множитель для всех подёргиваний (для быстрой настройки интенсивности)")]
        [Range(0f, 2f)]
        public float globalIntensityMultiplier = 1f;
    }
}

