using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DRAUM.Core
{
    /// <summary>
    /// Базовый класс системы обработки сущностей (Entity System).
    /// Система обрабатывает сущности с определенными компонентами.
    /// </summary>
    /// <typeparam name="TComponent">Тип компонента, который обрабатывает система</typeparam>
    public abstract class EntitySystem<TComponent> : MonoBehaviour, IEntitySystem
        where TComponent : IComponent
    {
        #region Fields

        /// <summary>
        /// Список сущностей, которые обрабатывает система
        /// </summary>
        protected readonly List<IEntity> Entities = new List<IEntity>();

        /// <summary>
        /// Менеджер сущностей
        /// </summary>
        protected EntityManager EntityManager { get; private set; }

        #endregion

        #region IEntitySystem Implementation

        /// <summary>
        /// Инициализация системы
        /// </summary>
        public virtual void Initialize()
        {
            EntityManager = EntityManager.Instance;
            
            if (EntityManager == null)
            {
                Debug.LogError($"[EntitySystem] EntityManager не найден! Система {GetType().Name} не может работать.");
                return;
            }
            EntityManager.OnEntityAdded += HandleEntityAdded;
            EntityManager.OnEntityRemoved += HandleEntityRemoved;
            EntityManager.OnEntityChanged += HandleEntityChanged;
            foreach (var entity in EntityManager.GetAllEntities())
            {
                if (ShouldProcessEntity(entity))
                {
                    Entities.Add(entity);
                    OnEntityAdded(entity);
                }
            }
        }

        /// <summary>
        /// Обновление системы каждый кадр
        /// </summary>
        public virtual void Update(float deltaTime)
        {
            if (!enabled || !gameObject.activeInHierarchy) return;

            for (int i = Entities.Count - 1; i >= 0; i--)
            {
                var entity = Entities[i];
                
                if (entity == null || !entity.IsActive)
                {
                    Entities.RemoveAt(i);
                    continue;
                }

                var component = entity.GetComponent<TComponent>();
                if (component != null && component.IsActive)
                {
                    ProcessEntity(entity, component, deltaTime);
                }
            }
        }

        /// <summary>
        /// Фиксированное обновление системы
        /// </summary>
        public virtual void FixedUpdate(float fixedDeltaTime)
        {
            if (!enabled || !gameObject.activeInHierarchy) return;

            for (int i = Entities.Count - 1; i >= 0; i--)
            {
                var entity = Entities[i];
                
                if (entity == null || !entity.IsActive)
                {
                    Entities.RemoveAt(i);
                    continue;
                }

                var component = entity.GetComponent<TComponent>();
                if (component != null && component.IsActive)
                {
                    ProcessEntityFixed(entity, component, fixedDeltaTime);
                }
            }
        }

        /// <summary>
        /// Unity Update - вызывает Update системы
        /// </summary>
        private void Update()
        {
            Update(Time.deltaTime);
        }

        /// <summary>
        /// Unity FixedUpdate - вызывает FixedUpdate системы
        /// </summary>
        private void FixedUpdate()
        {
            FixedUpdate(Time.fixedDeltaTime);
        }

        /// <summary>
        /// Вызывается при добавлении сущности в систему
        /// </summary>
        public virtual void OnEntityAdded(IEntity entity)
        {
        }

        /// <summary>
        /// Вызывается при удалении сущности из системы
        /// </summary>
        public virtual void OnEntityRemoved(IEntity entity)
        {
        }

        /// <summary>
        /// Вызывается при изменении компонентов сущности
        /// </summary>
        public virtual void OnEntityChanged(IEntity entity)
        {
    
            bool shouldProcess = ShouldProcessEntity(entity);
            bool isProcessing = Entities.Contains(entity);

            if (shouldProcess && !isProcessing)
            {
                Entities.Add(entity);
                OnEntityAdded(entity);
            }
            else if (!shouldProcess && isProcessing)
            {
                Entities.Remove(entity);
                OnEntityRemoved(entity);
            }
        }

        /// <summary>
        /// Выгрузка системы
        /// </summary>
        public virtual void Shutdown()
        {
            if (EntityManager != null)
            {
                EntityManager.OnEntityAdded -= HandleEntityAdded;
                EntityManager.OnEntityRemoved -= HandleEntityRemoved;
                EntityManager.OnEntityChanged -= HandleEntityChanged;
            }

            Entities.Clear();
        }

        #endregion

        #region Virtual Methods (Override in derived classes)

        /// <summary>
        /// Определяет, должна ли система обрабатывать данную сущность.
        /// По умолчанию проверяет наличие компонента TComponent.
        /// </summary>
        protected virtual bool ShouldProcessEntity(IEntity entity)
        {
            return entity != null && entity.IsActive && entity.HasComponent<TComponent>();
        }

        /// <summary>
        /// Обрабатывает сущность каждый кадр.
        /// </summary>
        /// <param name="entity">Сущность для обработки</param>
        /// <param name="component">Компонент сущности</param>
        /// <param name="deltaTime">Время с последнего кадра</param>
        protected virtual void ProcessEntity(IEntity entity, TComponent component, float deltaTime)
        {
        }

        /// <summary>
        /// Обрабатывает сущность при фиксированном обновлении.
        /// </summary>
        /// <param name="entity">Сущность для обработки</param>
        /// <param name="component">Компонент сущности</param>
        /// <param name="fixedDeltaTime">Фиксированное время</param>
        protected virtual void ProcessEntityFixed(IEntity entity, TComponent component, float fixedDeltaTime)
        {
          
        }

        #endregion

        #region Event Handlers

        private void HandleEntityAdded(IEntity entity)
        {
            if (ShouldProcessEntity(entity) && !Entities.Contains(entity))
            {
                Entities.Add(entity);
                OnEntityAdded(entity);
            }
        }

        private void HandleEntityRemoved(IEntity entity)
        {
            if (Entities.Remove(entity))
            {
                OnEntityRemoved(entity);
            }
        }

        private void HandleEntityChanged(IEntity entity)
        {
            OnEntityChanged(entity);
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            Shutdown();
        }

        #endregion
    }
}
