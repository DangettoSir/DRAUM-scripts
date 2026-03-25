using UnityEngine;

namespace DRAUM.Modules.Player.Effects
{
    /// <summary>
    /// Базовый класс эффекта на игроке. Живёт на PlayerEntity, тикает по времени, может влиять на стамину/атаку/Volume.
    /// </summary>
    public abstract class PlayerEffect
    {
        /// <summary> Уникальный id для стека (один тип эффекта — один экземпляр по умолчанию, можно расширить). </summary>
        public string Id { get; protected set; }
        /// <summary> Отображаемое имя (для UI). </summary>
        public string DisplayName { get; set; }
        /// <summary> Длительность в секундах (0 = бесконечно до снятия). </summary>
        public float Duration { get; protected set; }
        /// <summary> Оставшееся время в секундах. </summary>
        public float RemainingTime { get; set; }
        /// <summary> Множитель скорости атаки (1 = без изменений, &gt;1 = быстрее). </summary>
        public virtual float AttackSpeedMultiplier => 1f;
        /// <summary> Множитель расхода стамины за удар (1 = без изменений, &lt;1 = тратится меньше). </summary>
        public virtual float StaminaCostMultiplier => 1f;
        /// <summary> Цвет тинта для Volume (ColorAdjustments). </summary>
        public virtual Color VolumeTintColor => Color.clear;
        /// <summary> Интенсивность тинта 0..1 (0 = не применять). </summary>
        public virtual float VolumeTintIntensity => 0f;

        public bool IsExpired => Duration > 0f && RemainingTime <= 0f;

        protected PlayerEffect(string id, string displayName, float duration)
        {
            Id = id;
            DisplayName = displayName;
            Duration = duration;
            RemainingTime = duration;
        }

        /// <summary> Вызывается при наложении эффекта. </summary>
        public virtual void OnApply(PlayerEntity entity) { }

        /// <summary> Вызывается при снятии/истечении. </summary>
        public virtual void OnExpire(PlayerEntity entity) { }

        /// <summary> Тик каждый кадр. По умолчанию только уменьшает RemainingTime. </summary>
        public virtual void Tick(float deltaTime)
        {
            if (Duration > 0f)
                RemainingTime -= deltaTime;
        }
    }
}
