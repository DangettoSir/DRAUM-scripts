namespace DRAUM.Core
{
    /// <summary>
    /// Интерфейс компонента (Component) в системе ECS.
    /// </summary>
    public interface IComponent
    {
        /// <summary>
        /// Сущность, которой принадлежит компонент
        /// </summary>
        IEntity Entity { get; set; }

        /// <summary>
        /// Активен ли компонент
        /// </summary>
        bool IsActive { get; set; }
    }
}
