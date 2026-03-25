using UnityEngine;

namespace DRAUM.Modules.Player.Effects
{
    /// <summary>
    /// Эффект от яблока: атаки быстрее, стамина от ударов тратится на 70% медленнее (множитель 0.3).
    /// </summary>
    public class AttackSpeedEffect : PlayerEffect
    {
        private readonly float _attackSpeedMultiplier;
        private readonly float _staminaCostMultiplier;
        private readonly Color _volumeTintColor;
        private readonly float _volumeTintIntensity;

        public override float AttackSpeedMultiplier => _attackSpeedMultiplier;
        public override float StaminaCostMultiplier => _staminaCostMultiplier;
        public override Color VolumeTintColor => _volumeTintColor;
        public override float VolumeTintIntensity => _volumeTintIntensity;

        public AttackSpeedEffect(float duration, float attackSpeedMultiplier = 1.5f, float staminaCostMultiplier = 0.3f,
            Color? volumeTintColor = null, float volumeTintIntensity = 0.15f)
            : base("AttackSpeed", "Скорость атаки", duration)
        {
            _attackSpeedMultiplier = Mathf.Max(0.1f, attackSpeedMultiplier);
            _staminaCostMultiplier = Mathf.Clamp01(staminaCostMultiplier);
            _volumeTintColor = volumeTintColor ?? new Color(0.4f, 0.9f, 0.35f, 1f); // лёгкий зелёный
            _volumeTintIntensity = Mathf.Clamp01(volumeTintIntensity);
        }
    }
}
