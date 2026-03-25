using UnityEngine;

namespace DRAUM.Core
{
    /// <summary>
    /// Базовый класс для поведенческих модулей.
    /// Поведенческие модули реализуют игровую логику и поведение объектов.
    /// Типо как AIModule, CombatModule, SpawnModule
    /// </summary>
    public abstract class BehaviorModuleBase : GameModuleBase
    {
        /// <summary>
        /// Приоритет инициализации для поведенческих модулей (20-40)
        /// </summary>
        public override int InitializationPriority => 30;

        /// <summary>
        /// Флаги, кэн би паузед бро
        /// </summary>
        public override ModuleFlags Flags => ModuleFlags.StandardUpdate | ModuleFlags.CanBePaused;
    }
}
