namespace DRAUM.Core
{
    /// <summary>
    /// Базовый класс компонента (Component).
    /// Компонент содержит только данные, без логики уауауау
    /// </summary>
    public abstract class Component : IComponent
    {
        /// <summary>
        /// Сущность, которой принадлежит компонент
        /// </summary>
        public IEntity Entity { get; set; }

        /// <summary>
        /// Активен ли компонент
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Конструктор компонента
        /// </summary>
        protected Component()
        {
            IsActive = true;
        }
    }
}
