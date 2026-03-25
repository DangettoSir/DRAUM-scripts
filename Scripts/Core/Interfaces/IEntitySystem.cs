namespace DRAUM.Core
{
    /// <summary>
    /// Интерфейс системы обработки сущностей (Entity System) в ECS.
    /// </summary>
    public interface IEntitySystem
    {
        /// <summary>
        /// Инициализация системы
        /// </summary>
        void Initialize();

        /// <summary>
        /// Обновление системы каждый кадр
        /// </summary>
        /// <param name="deltaTime">Время с последнего кадра</param>
        void Update(float deltaTime);

        /// <summary>
        /// Фиксированное обновление системы
        /// </summary>
        /// <param name="fixedDeltaTime">Фиксированное время</param>
        void FixedUpdate(float fixedDeltaTime);

        /// <summary>
        /// Вызывается при добавлении сущности в систему
        /// </summary>
        /// <param name="entity">Добавленная сущность</param>
        void OnEntityAdded(IEntity entity);

        /// <summary>
        /// Вызывается при удалении сущности из системы
        /// </summary>
        /// <param name="entity">Удаленная сущность</param>
        void OnEntityRemoved(IEntity entity);

        /// <summary>
        /// Вызывается при изменении компонентов сущности
        /// </summary>
        /// <param name="entity">Измененная сущность</param>
        void OnEntityChanged(IEntity entity);

        /// <summary>
        /// Выгрузка системы
        /// </summary>
        void Shutdown();
    }
}
