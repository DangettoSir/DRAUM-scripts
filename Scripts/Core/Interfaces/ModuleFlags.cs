using System;

namespace DRAUM.Core
{
    /// <summary>
    /// Флаги возможностей модуля (capabilities/tags).
    /// </summary>
    [Flags]
    public enum ModuleFlags
    {
        /// <summary>
        /// Нет флагов
        /// </summary>
        None = 0,

        /// <summary>
        /// Модуль обновляется в Update()
        /// </summary>
        Update = 1 << 0,

        /// <summary>
        /// Модуль обновляется в FixedUpdate()
        /// </summary>
        FixedUpdate = 1 << 1,

        /// <summary>
        /// Модуль обновляется в LateUpdate()
        /// </summary>
        LateUpdate = 1 << 2,

        /// <summary>
        /// Модуль работает только на сервере (для сетевых игр)
        /// </summary>
        ServerOnly = 1 << 3,

        /// <summary>
        /// Модуль работает только на клиенте (для сетевых игр)
        /// </summary>
        ClientOnly = 1 << 4,

        /// <summary>
        /// Модуль требует инициализации перед другими модулями
        /// </summary>
        RequiresEarlyInit = 1 << 5,

        /// <summary>
        /// Модуль может быть приостановлен (Pause)
        /// </summary>
        CanBePaused = 1 << 6,

        /// <summary>
        /// Модуль критически важен для работы игры
        /// </summary>
        Critical = 1 << 7,

        /// <summary>
        /// Комбинация: Update + FixedUpdate (стандартное обновление)
        /// </summary>
        StandardUpdate = Update | FixedUpdate,

        /// <summary>
        /// Комбинация: Update + LateUpdate (полное обновление)
        /// </summary>
        FullUpdate = Update | FixedUpdate | LateUpdate
    }
}
