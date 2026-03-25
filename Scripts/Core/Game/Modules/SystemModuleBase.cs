using UnityEngine;

namespace DRAUM.Core
{
    /// <summary>
    /// Базовый класс для системных модулей.
    /// Системные модули - критически важные модули, которые должны инициализироваться первыми.
    /// InputModule, SaveModule, GameStateModule
    /// </summary>
    public abstract class SystemModuleBase : GameModuleBase
    {
        /// <summary>
        /// Приоритет инициализации для системных модулей (0-10)
        /// </summary>
        public override int InitializationPriority => 10;

        /// <summary>
        /// Эрли инит!!!!
        /// </summary>
        public override ModuleFlags Flags => ModuleFlags.StandardUpdate | ModuleFlags.Critical | ModuleFlags.RequiresEarlyInit;
    }
}
