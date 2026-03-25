using System;
using UnityEngine;

namespace DRAUM.Modules.Combat.Events
{
    /// <summary>
    /// Универсальный запрос на применение урона к врагу.
    /// CombatModule должен обработать это событие и при необходимости вызвать Reply(actualDamage).
    /// </summary>
    public class EnemyDamageRequestEvent
    {
        public EnemyEntity TargetEnemy { get; set; }

        // "Базовый" урон (до body-part множителей EnemyEntity).
        public float BaseDamage { get; set; }

        public BodyPart BodyPart { get; set; } = BodyPart.Unknown;

        public Vector3 HitPoint { get; set; } = Vector3.zero;

        // Синхронный ответ: actualDamage уже с учётом body-part множителей EnemyEntity.
        public Action<float> Reply { get; set; }
    }
}

