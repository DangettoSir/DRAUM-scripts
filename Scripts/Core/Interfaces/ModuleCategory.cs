namespace DRAUM.Core
{
    /// <summary>
    /// Категории модулей игры для классификации и организации.
    /// </summary>
    [System.Obsolete("ModuleFlags вместо ModuleCategory")]
    public enum ModuleCategory
    {
        /// <summary>
        /// Системные модули
        /// </summary>
        System = 0,

        /// <summary>
        /// Менеджеры
        /// </summary>
        Manager = 1,

        /// <summary>
        /// Сервисы
        /// </summary>
        Service = 2,

        /// <summary>
        /// Поведенческие модули
        /// </summary>
        Behavior = 3,

        /// <summary>
        /// Декоративные модули
        /// </summary>
        Decorative = 4
    }
}
