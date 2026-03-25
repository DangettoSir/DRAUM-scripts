using System.Collections.Generic;

namespace DRAUM.Core
{
    /// <summary>
    /// Интерфейс сущности (Entity) в системе ECS.
    /// </summary>
    public interface IEntity
    {
        /// <summary>
        /// Уникальный идентификатор сущности
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Имя сущности (опционально, для отладки)
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Активна ли сущность
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Добавляет компонент к сущности
        /// </summary>
        /// <typeparam name="T">Тип компонента</typeparam>
        /// <param name="component">Компонент для добавления</param>
        void AddComponent<T>(T component) where T : IComponent;

        /// <summary>
        /// Получает компонент по типу
        /// </summary>
        /// <typeparam name="T">Тип компонента</typeparam>
        /// <returns>Компонент или null, если не найден</returns>
        T GetComponent<T>() where T : IComponent;

        /// <summary>
        /// Проверяет наличие компонента
        /// </summary>
        /// <typeparam name="T">Тип компонента</typeparam>
        /// <returns>True, если компонент присутствует</returns>
        bool HasComponent<T>() where T : IComponent;

        /// <summary>
        /// Удаляет компонент
        /// </summary>
        /// <typeparam name="T">Тип компонента</typeparam>
        /// <returns>True, если компонент был удален</returns>
        bool RemoveComponent<T>() where T : IComponent;

        /// <summary>
        /// Получает все компоненты сущности
        /// </summary>
        /// <returns>Список всех компонентов</returns>
        IEnumerable<IComponent> GetAllComponents();

        /// <summary>
        /// Активирует сущность
        /// </summary>
        void Activate();

        /// <summary>
        /// Деактивирует сущность
        /// </summary>
        void Deactivate();

        /// <summary>
        /// Уничтожает сущность
        /// </summary>
        void Destroy();
    }
}
