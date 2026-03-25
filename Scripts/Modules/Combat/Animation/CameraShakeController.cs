using System.Collections;
using UnityEngine;

namespace DRAUM.Modules.Combat.Animation
{
    /// <summary>
    /// Контроллер подёргиваний камеры при ударах.
    /// Управляет применением подёргиваний к камере FirstPersonController.
    /// </summary>
    public class CameraShakeController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("FirstPersonController для применения подёргиваний")]
        public FirstPersonController firstPersonController;

        [Tooltip("Конфигурация подёргиваний камеры")]
        public CameraShakeConfig shakeConfig;

        [Header("Debug")]
        [Tooltip("Показывать debug логи")]
        [HideInInspector] public bool showDebugLogs = false;

        private Coroutine _currentShakeCoroutine;
        private Vector2 _currentShakeOffset = Vector2.zero;

        /// <summary>
        /// Текущее смещение камеры от подёргиваний (x = yaw, y = pitch)
        /// </summary>
        public Vector2 CurrentShakeOffset => _currentShakeOffset;

        private void Awake()
        {
            if (firstPersonController == null)
            {
                firstPersonController = FindFirstObjectByType<FirstPersonController>();
            }

            if (firstPersonController == null)
            {
                Debug.LogWarning("[CameraShakeController] FirstPersonController не найден! Подёргивания камеры не будут работать.");
            }
        }

        /// <summary>
        /// Запускает подёргивание камеры для указанного типа удара
        /// </summary>
        public void TriggerAttackShake(FPMAxe.WindUpState attackType)
        {
            if (!shakeConfig || !shakeConfig.enableCameraShake || firstPersonController == null)
                return;

            CameraShakeSettings settings = null;

            switch (attackType)
            {
                case FPMAxe.WindUpState.Left:
                    settings = shakeConfig.leftAttackShake;
                    break;
                case FPMAxe.WindUpState.Right:
                    settings = shakeConfig.rightAttackShake;
                    break;
                case FPMAxe.WindUpState.Up:
                    settings = shakeConfig.upAttackShake;
                    break;
                default:
                    return;
            }

            if (settings == null) return;

            if (_currentShakeCoroutine != null)
            {
                StopCoroutine(_currentShakeCoroutine);
            }

            _currentShakeCoroutine = StartCoroutine(ShakeCameraCoroutine(settings));

            if (showDebugLogs)
            {
                Debug.Log($"[CameraShakeController] Запущено подёргивание для удара: {attackType}");
            }
        }


        private IEnumerator ShakeCameraCoroutine(CameraShakeSettings settings)
        {
            float intensityMultiplier = 1f + Random.Range(-settings.intensityRandomness / 100f, settings.intensityRandomness / 100f);
            float duration = settings.duration * (1f + Random.Range(-settings.durationRandomness / 100f, settings.durationRandomness / 100f));
            
            intensityMultiplier *= shakeConfig.globalIntensityMultiplier;

            float horizontalDir = settings.horizontalDirection;
            if (Mathf.Abs(horizontalDir) < 0.01f)
            {
                horizontalDir = Random.value < 0.5f ? -1f : 1f;
            }

            float verticalDir = settings.verticalDirection;
            if (Mathf.Abs(verticalDir) < 0.01f)
            {
                verticalDir = Random.value < 0.5f ? -1f : 1f;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float curveValue = settings.falloffCurve.Evaluate(progress);
                float horizontalShake = (Random.value * 2f - 1f) * settings.horizontalIntensity * intensityMultiplier * curveValue * horizontalDir;
                float verticalShake = (Random.value * 2f - 1f) * settings.verticalIntensity * intensityMultiplier * curveValue * verticalDir;

                _currentShakeOffset = new Vector2(horizontalShake, verticalShake);

                if (firstPersonController != null)
                {
                    firstPersonController.SetCameraShakeOffset(_currentShakeOffset);
                }

                yield return null;
            }

            float fadeOutTime = 0.05f;
            float fadeElapsed = 0f;
            Vector2 startOffset = _currentShakeOffset;

            while (fadeElapsed < fadeOutTime)
            {
                fadeElapsed += Time.deltaTime;
                float fadeProgress = fadeElapsed / fadeOutTime;
                _currentShakeOffset = Vector2.Lerp(startOffset, Vector2.zero, fadeProgress);

                if (firstPersonController != null)
                {
                    firstPersonController.SetCameraShakeOffset(_currentShakeOffset);
                }

                yield return null;
            }

            _currentShakeOffset = Vector2.zero;
            if (firstPersonController != null)
            {
                firstPersonController.SetCameraShakeOffset(Vector2.zero);
            }
            _currentShakeCoroutine = null;
        }

        /// <summary>
        /// Останавливает текущее подёргивание
        /// </summary>
    public void StopShake()
    {
        if (_currentShakeCoroutine != null)
        {
            StopCoroutine(_currentShakeCoroutine);
            _currentShakeCoroutine = null;
        }
        _currentShakeOffset = Vector2.zero;
        if (firstPersonController != null)
        {
            firstPersonController.SetCameraShakeOffset(Vector2.zero);
        }
    }
    }
}

