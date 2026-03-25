using UnityEngine;

namespace DRAUM.Core
{
    /// <summary>
    /// Базовый класс для декоративных модулей.
    /// Декоративные модули отвечают за визуальные эффекты и постобработку.
    /// Наподобие ParticleModule, PostProcessingModule, WeatherModule
    /// </summary>
    public abstract class DecorativeModuleBase : GameModuleBase
    {
        /// <summary>
        /// Приоритет инициализации для декоративных модулей (50+)
        /// </summary>
        public override int InitializationPriority => 50;

        /// <summary>
        /// Полное обновление с LateUpdate для визуальных эффектов
        /// </summary>
        public override ModuleFlags Flags => ModuleFlags.FullUpdate | ModuleFlags.CanBePaused;
    }
}
