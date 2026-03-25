using UnityEngine;

namespace DRAUM.Core
{
    /// <summary>
    /// Базовый класс для модулей игры с реализацией стандартной логики.
    /// </summary>
    public abstract class GameModuleBase : MonoBehaviour, IGameModule
    {
        #region IGameModule Implementation

        /// <summary>
        /// Уникальное имя модуля. По умолчанию используется имя класса.
        /// </summary>
        public virtual string ModuleName => GetType().Name;

        /// <summary>
        /// Приоритет инициализации. Меньше = раньше инициализируется.
        /// </summary>
        public virtual int InitializationPriority => 100;

        /// <summary>
        /// Флаги возможностей модуля (capabilities/tags).
        /// Определяет, как модуль обновляется и какие у него возможности.
        /// По умолчанию: Update + FixedUpdate
        /// </summary>
        public virtual ModuleFlags Flags => ModuleFlags.StandardUpdate;

        /// <summary>
        /// Флаг активности модуля
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Ссылка на экземпляр Game
        /// </summary>
        protected Game Game { get; private set; }

        /// <summary>
        /// EventBus для коммуникации между модулями.
        /// 
        /// Юзай EventBus для общения между модулями, НЕ Game напрямую!!!!!!
        /// Это предотвращает создание God Object.
        /// </summary>
        protected EventBus Events => EventBus.Instance;

        /// <summary>
        /// Инициализация модуля. Вызывается автоматически при старте игры.
        /// </summary>
        public virtual void Initialize(Game game)
        {
            Game = game;
            IsActive = true;
            OnInitialize();
        }

        /// <summary>
        /// Обновление модуля каждый кадр
        /// </summary>
        public virtual void Update()
        {
            if (IsActive && (Flags & ModuleFlags.Update) != 0)
            {
                OnUpdate();
            }
        }

        /// <summary>
        /// Фиксированное обновление модуля
        /// </summary>
        public virtual void FixedUpdate()
        {
            if (IsActive && (Flags & ModuleFlags.FixedUpdate) != 0)
            {
                OnFixedUpdate();
            }
        }

        /// <summary>
        /// Позднее обновление модуля (после всех Update)
        /// </summary>
        public virtual void LateUpdate()
        {
            if (IsActive && (Flags & ModuleFlags.LateUpdate) != 0)
            {
                OnLateUpdate();
            }
        }

        /// <summary>
        /// Выгрузка модуля
        /// </summary>
        public virtual void Shutdown()
        {
            OnShutdown();
            IsActive = false;
            Game = null;
        }

        /// <summary>
        /// Активация модуля
        /// </summary>
        public virtual void Activate()
        {
            IsActive = true;
            OnActivate();
        }

        /// <summary>
        /// Деактивация модуля
        /// </summary>
        public virtual void Deactivate()
        {
            IsActive = false;
            OnDeactivate();
        }

        #endregion

        #region Virtual Methods (Override in derived classes)

        /// <summary>
        /// Вызывается при инициализации модуля.
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// Вызывается каждый кадр, если модуль активен.
        /// </summary>
        protected virtual void OnUpdate() { }

        /// <summary>
        /// Вызывается при фиксированном обновлении, если модуль активен.
        /// </summary>
        protected virtual void OnFixedUpdate() { }

        /// <summary>
        /// Вызывается при позднем обновлении, если модуль активен.
        /// </summary>
        protected virtual void OnLateUpdate() { }

        /// <summary>
        /// Вызывается при выгрузке модуля.
        /// </summary>
        protected virtual void OnShutdown() { }

        /// <summary>
        /// Вызывается при активации модуля.
        /// </summary>
        protected virtual void OnActivate() { }

        /// <summary>
        /// Вызывается при деактивации модуля.
        /// </summary>
        protected virtual void OnDeactivate() { }

        #endregion
    }
}
