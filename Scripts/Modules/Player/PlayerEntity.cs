using System.Collections.Generic;
using UnityEngine;
using DRAUM.Core;
using DRAUM.Modules.Player.Events;
using DRAUM.Modules.Player.Effects;

namespace DRAUM.Modules.Player
{
    /// <summary>
    /// Сущность игрока: HP, стамина, регенерация, активные эффекты. Публикует события при изменении.
    /// </summary>
    public class PlayerEntity : MonoBehaviour
    {
        [Header("Health")]
        public float maxHealth = 100f;
        public float currentHealth = 100f;

        [Header("Stamina")]
        public float maxStamina = 100f;
        public float currentStamina = 100f;
        public float staminaRegenPerSecond = 15f;
        public float staminaRegenDelayAfterUse = 0.5f;

        [Header("Stamina Effects")]
        [Range(0f, 1f)]
        public float lowStaminaThreshold = 0.25f;
        [Range(0f, 1f)]
        public float animatorSpeedWhenLowStamina = 0.5f;
        [Range(0f, 1f)]
        public float animatorSpeedWhenZeroStamina = 0.25f;
        [Range(0f, 1f)]
        public float vignetteIntensityMax = 0.5f;

        private float _lastStaminaUseTime;
        private readonly List<PlayerEffect> _activeEffects = new List<PlayerEffect>();

        /// <summary> Нормализованная стамина (0..1). </summary>
        public float StaminaNormalized => maxStamina > 0 ? Mathf.Clamp01(currentStamina / maxStamina) : 0f;

        /// <summary> Нормализованное здоровье (0..1). </summary>
        public float HealthNormalized => maxHealth > 0 ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

        /// <summary> Множитель скорости анимаций атаки в зависимости от стамины. </summary>
        public float AnimatorSpeedMultiplier
        {
            get
            {
                if (currentStamina <= 0f) return animatorSpeedWhenZeroStamina;
                if (StaminaNormalized < lowStaminaThreshold) return animatorSpeedWhenLowStamina;
                return 1f;
            }
        }

        /// <summary> Интенсивность виньетки (0 = нет, 1 = макс). Увеличивается при падении стамины. </summary>
        public float VignetteIntensity => (1f - StaminaNormalized) * vignetteIntensityMax;

        /// <summary> Множитель скорости атаки от эффектов (произведение всех активных). </summary>
        public float EffectsAttackSpeedMultiplier
        {
            get
            {
                float m = 1f;
                for (int i = 0; i < _activeEffects.Count; i++)
                    m *= _activeEffects[i].AttackSpeedMultiplier;
                return m;
            }
        }

        /// <summary> Множитель расхода стамины за удар от эффектов (произведение всех активных). </summary>
        public float EffectsStaminaCostMultiplier
        {
            get
            {
                float m = 1f;
                for (int i = 0; i < _activeEffects.Count; i++)
                    m *= _activeEffects[i].StaminaCostMultiplier;
                return m;
            }
        }

        /// <summary> Текущие активные эффекты (readonly). </summary>
        public IReadOnlyList<PlayerEffect> GetActiveEffects() => _activeEffects;

        /// <summary> Есть ли хотя бы один эффект с тинтом для Volume. </summary>
        public bool TryGetEffectVolumeTint(out Color color, out float intensity)
        {
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                var e = _activeEffects[i];
                if (e.VolumeTintIntensity > 0f)
                {
                    color = e.VolumeTintColor;
                    intensity = e.VolumeTintIntensity;
                    return true;
                }
            }
            color = Color.clear;
            intensity = 0f;
            return false;
        }

        /// <summary> Накладывает эффект на игрока. Если эффект с таким Id уже есть — продлевает/перезаписывает по необходимости. </summary>
        public void AddEffect(PlayerEffect effect)
        {
            if (effect == null) return;
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                if (_activeEffects[i].Id == effect.Id)
                {
                    _activeEffects[i].OnExpire(this);
                    _activeEffects.RemoveAt(i);
                    break;
                }
            }
            _activeEffects.Add(effect);
            effect.OnApply(this);
        }

        /// <summary> Снимает эффект по Id. </summary>
        public bool RemoveEffect(string effectId)
        {
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                if (_activeEffects[i].Id == effectId)
                {
                    _activeEffects[i].OnExpire(this);
                    _activeEffects.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Тик эффектов, снятие истёкших
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                _activeEffects[i].Tick(dt);
                if (_activeEffects[i].IsExpired)
                {
                    _activeEffects[i].OnExpire(this);
                    _activeEffects.RemoveAt(i);
                }
            }

            if (staminaRegenPerSecond <= 0f || currentStamina >= maxStamina) return;
            if (Time.time - _lastStaminaUseTime < staminaRegenDelayAfterUse) return;

            float add = staminaRegenPerSecond * dt;
            float before = currentStamina;
            currentStamina = Mathf.Min(currentStamina + add, maxStamina);
            if (currentStamina != before && EventBus.Instance != null)
                EventBus.Instance.Publish(new PlayerStaminaChangedEvent { CurrentStamina = currentStamina, MaxStamina = maxStamina });
        }

        /// <summary> Можно ли потратить указанное количество стамины. </summary>
        public bool CanConsumeStamina(float amount)
        {
            return amount >= 0 && currentStamina >= amount;
        }

        /// <summary> Тратит стамину. Возвращает true, если хватило. </summary>
        public bool ConsumeStamina(float amount)
        {
            if (amount <= 0) return true;
            if (currentStamina < amount) return false;

            currentStamina = Mathf.Max(0f, currentStamina - amount);
            _lastStaminaUseTime = Time.time;

            if (EventBus.Instance != null)
                EventBus.Instance.Publish(new PlayerStaminaChangedEvent { CurrentStamina = currentStamina, MaxStamina = maxStamina });
            return true;
        }

        /// <summary> Наносит урон по здоровью. </summary>
        public void TakeDamage(float damage)
        {
            if (damage <= 0) return;
            currentHealth = Mathf.Max(0f, currentHealth - damage);
            if (EventBus.Instance != null)
                EventBus.Instance.Publish(new PlayerHealthChangedEvent { CurrentHealth = currentHealth, MaxHealth = maxHealth });
        }

        /// <summary> Восстанавливает здоровье/стамину (для отладки или аптечек). </summary>
        public void HealHealth(float amount) { currentHealth = Mathf.Min(maxHealth, currentHealth + amount); }
        public void RestoreStamina(float amount) { currentStamina = Mathf.Min(maxStamina, currentStamina + amount); }
    }
}
