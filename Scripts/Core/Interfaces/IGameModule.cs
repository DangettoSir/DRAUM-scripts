using UnityEngine;

namespace DRAUM.Core
{
    /// <summary>
    /// Базовый интерфейс для всех модулей игры.
    /// Каждый модуль должен реализовывать этот интерфейс для интеграции в систему Game.
    /// </summary>
    public interface IGameModule
    {
        /// <summary>
        /// Уникальное имя модуля для идентификации в системе
        /// </summary>
        string ModuleName { get; }

        /// <summary>
        /// Приоритет инициализации модуля (меньше = раньше)
        /// </summary>
        int InitializationPriority { get; }

        /// <summary>
        /// Вызывается при инициализации модуля. Здесь должна происходить основная настройка модуля.
        /// </summary>
        /// <param name="game">Ссылка на экземпляр Game</param>
        void Initialize(Game game);

        /// <summary>
        /// Вызывается каждый кадр, если модуль активен
        /// </summary>
        void Update();

        /// <summary>
        /// Вызывается при фиксированном обновлении (FixedUpdate)
        /// </summary>
        void FixedUpdate();

        /// <summary>
        /// Вызывается при завершении работы модуля (при выгрузке сцены или завершении игры)
        /// </summary>
        void Shutdown();

        /// <summary>
        /// Проверяет, активен ли модуль в данный момент
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Активирует модуль
        /// </summary>
        void Activate();

        /// <summary>
        /// Деактивирует модуль
        /// </summary>
        void Deactivate();
    }
}
