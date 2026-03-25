using UnityEngine;

namespace DRAUM.Core
{
    /// <summary>
    /// Базовый класс для модулей-менеджеров.
    /// Менеджеры управляют основными системами игры и игровыми объектами.
    /// примерчики PlayerModule, InventoryModule, EquipmentModule
    /// </summary>
    public abstract class ManagerModuleBase : GameModuleBase
    {
        /// <summary>
        /// Приоритет инициализации для менеджеров (10-30)
        /// </summary>
        public override int InitializationPriority => 20;


        public override ModuleFlags Flags => ModuleFlags.StandardUpdate | ModuleFlags.CanBePaused;
    }
}
