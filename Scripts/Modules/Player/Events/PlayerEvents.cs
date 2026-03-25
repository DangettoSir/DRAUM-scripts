using DRAUM.Core;
using UnityEngine;

namespace DRAUM.Modules.Player.Events
{
    /// <summary>
    /// Событие изменения позиции игрока
    /// </summary>
    public class PlayerPositionChangedEvent : IEvent
    {
        public Vector3 Position { get; set; }
    }

    /// <summary>
    /// Событие изменения скорости игрока
    /// </summary>
    public class PlayerSpeedChangedEvent : IEvent
    {
        public float Speed { get; set; }
    }

    /// <summary>
    /// Событие включения/выключения боевого режима
    /// </summary>
    public class PlayerCombatModeChangedEvent : IEvent
    {
        public bool IsCombatModeActive { get; set; }
    }

    /// <summary>
    /// Событие блокировки/разблокировки игрока
    /// </summary>
    public class PlayerLockStateChangedEvent : IEvent
    {
        public bool IsLocked { get; set; }
        public bool MovementLocked { get; set; }
        public bool CameraLocked { get; set; }
    }

    /// <summary>
    /// Событие изменения состояния взаимодействия
    /// </summary>
    public class PlayerInteractionChangedEvent : IEvent
    {
        public bool IsLookingAtInteractable { get; set; }
        public string InteractionName { get; set; }
    }

    /// <summary>
    /// Событие шага игрока (для звуков шагов)
    /// </summary>
    public class PlayerFootstepEvent : IEvent
    {
        public string MaterialName { get; set; }
        public Vector3 Position { get; set; }
        public bool IsSprinting { get; set; } = false;
        public bool IsCrouched { get; set; } = false;
    }
    
    /// <summary>
    /// Событие интеракции игрока (подбор предметов и другие взаимодействия)
    /// </summary>
    public class PlayerInteractionEvent : IEvent
    {
        public InteractionType InteractionType { get; set; }
        public string ItemName { get; set; }
        public Vector3 Position { get; set; }
    }
    
    /// <summary>
    /// Событие удара оружием
    /// </summary>
    public class PlayerCombatHitEvent : IEvent
    {
        public string WeaponName { get; set; }
        public string MaterialName { get; set; }
        public Vector3 HitPosition { get; set; }
        public string SwingDirection { get; set; } // "Left", "Right", "Up"

        // Отличаем "начало атаки" от реального импакта (OnTriggerEnter).
        // FPMAxe публикует начало атаки, WeaponCollisionDetector публикует impact.
        public bool IsImpact { get; set; } = false;

        // Контекст импакта для расчёта урона.
        public Collider HitCollider { get; set; }
        public float HitSpeed { get; set; }
        public float WeaponMass { get; set; }
        public float BaseForceMultiplier { get; set; }
        public float SpeedForceMultiplier { get; set; }
    }

    /// <summary>
    /// Событие cue из AnimationEvent для боевых анимаций.
    /// Используется для тайминга звуков/шейков по клипу, а не по state toggle.
    /// </summary>
    public class CombatAnimationCueEvent : IEvent
    {
        // Поддерживаем маршрутизацию: Camera / Audio / Cursor / All.
        public string Target { get; set; } = "All";
        public string CueKey { get; set; }
        public string WeaponName { get; set; }
        public string SwingDirection { get; set; } // "Left", "Right", "Up"
        public Vector3 Position { get; set; }
    }
    
    /// <summary>
    /// Событие UI звука (инвентарь, клики и т.д.)
    /// </summary>
    public class UISoundEvent : IEvent
    {
        public UISoundType SoundType { get; set; }
        public string Context { get; set; } // Дополнительный контекст (например, имя секции)
    }
    
    /// <summary>
    /// Типы UI звуков
    /// </summary>
    public enum UISoundType
    {
        BackpackOpen,
        BackpackClose,
        SectionTransition,
        LeftClick,
        RightClick
    }

    /// <summary>
    /// Событие изменения стамины игрока
    /// </summary>
    public class PlayerStaminaChangedEvent : IEvent
    {
        public float CurrentStamina { get; set; }
        public float MaxStamina { get; set; }
    }

    /// <summary>
    /// Событие изменения здоровья игрока
    /// </summary>
    public class PlayerHealthChangedEvent : IEvent
    {
        public float CurrentHealth { get; set; }
        public float MaxHealth { get; set; }
    }
}