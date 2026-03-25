using UnityEngine;

namespace DRAUM.Core
{
    /// <summary>
    /// Базовый класс для сервисных модулей.
    /// Сервисы предоставляют услуги другим модулям и обычно не содержат игровую логику.
    /// AudioModule, UIModule, EffectsModule
    /// </summary>
    public abstract class ServiceModuleBase : GameModuleBase
    {
        /// <summary>
        /// Приоритет инициализации для сервисов (30-50)
        /// </summary>
        public override int InitializationPriority => 40;

        /// <summary>
        /// Кэн би паузед фулл апдейт
        /// </summary>
        public override ModuleFlags Flags => ModuleFlags.FullUpdate | ModuleFlags.CanBePaused;
    }
}
