using UnityEngine;

namespace DRAUM.Modules.Combat.Animation
{
    /// <summary>
    /// Настройки подёргивания камеры для одного типа удара.
    /// Используется в CameraShakeConfig для настройки разных типов ударов.
    /// </summary>
    [System.Serializable]
    public class CameraShakeSettings
    {
        [Header("Shake Intensity")]
        [Tooltip("Интенсивность подёргивания по горизонтали (yaw)")]
        public float horizontalIntensity = 2f;

        [Tooltip("Интенсивность подёргивания по вертикали (pitch)")]
        public float verticalIntensity = 1f;

        [Header("Timing")]
        [Tooltip("Длительность подёргивания (секунды)")]
        public float duration = 0.2f;

        [Tooltip("Кривая затухания подёргивания (0 = начало, 1 = конец)")]
        public AnimationCurve falloffCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));

        [Header("Direction")]
        [Tooltip("Направление подёргивания по горизонтали (-1 = влево, 1 = вправо, 0 = случайное)")]
        [Range(-1f, 1f)]
        public float horizontalDirection = 0f;

        [Tooltip("Направление подёргивания по вертикали (-1 = вниз, 1 = вверх, 0 = случайное)")]
        [Range(-1f, 1f)]
        public float verticalDirection = 0f;

        [Header("Randomization")]
        [Tooltip("Случайное отклонение интенсивности (±%)")]
        [Range(0f, 50f)]
        public float intensityRandomness = 10f;

        [Tooltip("Случайное отклонение длительности (±%)")]
        [Range(0f, 30f)]
        public float durationRandomness = 5f;

        public CameraShakeSettings()
        {
            falloffCurve = new AnimationCurve(
                new Keyframe(0f, 1f, 0f, -2f),
                new Keyframe(1f, 0f, 0f, 0f)
            );
        }
    }
}

