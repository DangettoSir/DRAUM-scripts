using UnityEngine;

namespace DRAUM.Modules.Player.Effects
{
    /// <summary>
    /// Данные эффекта для расходника (ScriptableObject). Создай ассет и повесь на ItemData.consumableEffect.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerEffectData", menuName = "DRAUM/Player/Player Effect Data")]
    public class PlayerEffectData : ScriptableObject
    {
        public string effectId = "AttackSpeed";
        public string displayName = "Скорость атаки";
        [Tooltip("Длительность в секундах. 0 = бесконечно (до снятия).")]
        public float duration = 30f;

        [Header("Attack Speed Effect")]
        [Tooltip("Множитель скорости атаки (1.5 = на 50% быстрее).")]
        public float attackSpeedMultiplier = 1.5f;
        [Tooltip("Множитель расхода стамины за удар (0.3 = уходит на 70% медленнее).")]
        [Range(0f, 1f)]
        public float staminaCostMultiplier = 0.3f;

        [Header("Volume (визуал на игроке)")]
        [Tooltip("Цвет тинта в постобработке (например лёгкий зелёный для баффа).")]
        public Color volumeTintColor = new Color(0.4f, 0.9f, 0.35f, 1f);
        [Range(0f, 1f)]
        public float volumeTintIntensity = 0.15f;

        /// <summary>
        /// Создаёт рантайм-эффект по данным ассета.
        /// </summary>
        public PlayerEffect CreateEffect()
        {
            if (string.IsNullOrEmpty(effectId)) effectId = "AttackSpeed";
            var effect = new AttackSpeedEffect(duration, attackSpeedMultiplier, staminaCostMultiplier, volumeTintColor, volumeTintIntensity);
            effect.DisplayName = string.IsNullOrEmpty(displayName) ? effectId : displayName;
            return effect;
        }
    }
}
