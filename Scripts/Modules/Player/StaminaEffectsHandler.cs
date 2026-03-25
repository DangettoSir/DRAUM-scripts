using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DRAUM.Core;
using DRAUM.Modules.Player.Events;

namespace DRAUM.Modules.Player
{
    /// <summary>
    /// Применяет эффекты стамины: виньетка (Volume) и множитель скорости аниматора.
    /// Также применяет тинт от активных эффектов игрока (ColorAdjustments colorFilter).
    /// </summary>
    public class StaminaEffectsHandler : MonoBehaviour
    {
        [Header("References")]
        public PlayerEntity playerEntity;
        public Volume volume;
        public Animator attackAnimator;

        [Header("Vignette")]
        public bool controlVignette = true;

        [Header("Effect Tint (Volume)")]
        [Tooltip("Применять ли тинт от активных эффектов (Color Adjustments > Color Filter). Добавь override в Volume Profile.")]
        public bool controlEffectTint = true;

        private Vignette _vignette;
        private ColorAdjustments _colorAdjustments;
        private float _cachedVignetteIntensity = -1f;
        private Color _cachedEffectTint = Color.white;
        private float _cachedEffectTintIntensity = -1f;

        private void Awake()
        {
            if (playerEntity == null) playerEntity = FindFirstObjectByType<PlayerEntity>();
            if (volume != null && volume.profile != null)
            {
                volume.profile.TryGet(out _vignette);
                volume.profile.TryGet(out _colorAdjustments);
            }
        }

        private void OnEnable()
        {
            if (EventBus.Instance != null)
                EventBus.Instance.Subscribe<PlayerStaminaChangedEvent>(OnStaminaChanged);
        }

        private void OnDisable()
        {
            if (EventBus.Instance != null)
                EventBus.Instance.Unsubscribe<PlayerStaminaChangedEvent>(OnStaminaChanged);
        }

        private void OnStaminaChanged(PlayerStaminaChangedEvent evt)
        {
            ApplyEffects();
        }

        private void Update()
        {
            if (playerEntity == null) return;
            ApplyEffects();
        }

        private void ApplyEffects()
        {
            if (playerEntity == null) return;

            if (controlVignette && _vignette != null)
            {
                float target = playerEntity.VignetteIntensity;
                if (Mathf.Abs(_cachedVignetteIntensity - target) > 0.001f)
                {
                    _vignette.intensity.Override(target);
                    _cachedVignetteIntensity = target;
                }
            }

            if (attackAnimator != null)
            {
                float speed = playerEntity.AnimatorSpeedMultiplier * playerEntity.EffectsAttackSpeedMultiplier;
                if (Mathf.Abs(attackAnimator.speed - speed) > 0.001f)
                    attackAnimator.speed = speed;
            }

            if (controlEffectTint && _colorAdjustments != null && playerEntity.TryGetEffectVolumeTint(out Color tintColor, out float tintIntensity))
            {
                Color targetFilter = Color.Lerp(Color.white, tintColor, tintIntensity);
                if (_cachedEffectTint != targetFilter || Mathf.Abs(_cachedEffectTintIntensity - tintIntensity) > 0.001f)
                {
                    _colorAdjustments.colorFilter.Override(targetFilter);
                    _cachedEffectTint = targetFilter;
                    _cachedEffectTintIntensity = tintIntensity;
                }
            }
            else if (controlEffectTint && _colorAdjustments != null && _cachedEffectTintIntensity > 0f)
            {
                _colorAdjustments.colorFilter.Override(Color.white);
                _cachedEffectTint = Color.white;
                _cachedEffectTintIntensity = 0f;
            }
        }
    }
}
