using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DRAUM.Core
{
    /// <summary>
    /// Менеджер сущностей (Entity Manager).
    /// Управляет всеми сущностями в игре и координирует работу систем.
    /// Реализует паттерн Singleton.
    /// </summary>
    public class EntityManager : MonoBehaviour
    {
        #region Singleton

        private static EntityManager _instance;

        /// <summary>
        /// Единственный экземпляр EntityManager
        /// </summary>
        public static EntityManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<EntityManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("EntityManager");
                        _instance = go.AddComponent<EntityManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Событие добавления сущности
        /// </summary>
        public event Action<IEntity> OnEntityAdded;

        /// <summary>
        /// Событие удаления сущности
        /// </summary>
        public event Action<IEntity> OnEntityRemoved;

        /// <summary>
        /// Событие изменения сущности (добавление/удаление компонентов)
        /// </summary>
        public event Action<IEntity> OnEntityChanged;

        #endregion

        #region Fields

        private readonly Dictionary<int, IEntity> _entities = new Dictionary<int, IEntity>();
        private readonly List<IEntitySystem> _systems = new List<IEntitySystem>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[EntityManager] Попытка создать второй экземпляр EntityManager. Уничтожаем дубликат.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        // Важно!!!! Запоминаем ребяточки
        // EntitySystem сам вызывает Update/FixedUpdate через Unity
        // EntityManager только регистрирует системы, но не обновляет их

        private void OnDestroy()
        {
            foreach (var system in _systems)
            {
                system?.Shutdown();
            }
            _systems.Clear();
            _entities.Clear();
        }

        #endregion

        #region Entity Management

        /// <summary>
        /// Регистрирует сущность в менеджере
        /// </summary>
        /// <param name="entity">Сущность для регистрации</param>
        public void RegisterEntity(IEntity entity)
        {
            if (entity == null)
            {
                Debug.LogWarning("[EntityManager] Попытка зарегистрировать null сущность!");
                return;
            }

            if (_entities.ContainsKey(entity.Id))
            {
                Debug.LogWarning($"[EntityManager] Сущность с ID {entity.Id} уже зарегистрирована!");
                return;
            }

            _entities[entity.Id] = entity;
            OnEntityAdded?.Invoke(entity);
        }

        /// <summary>
        /// Удаляет сущность из менеджера
        /// </summary>
        /// <param name="entity">Сущность для удаления</param>
        public void UnregisterEntity(IEntity entity)
        {
            if (entity == null) return;

            if (_entities.Remove(entity.Id))
            {
                OnEntityRemoved?.Invoke(entity);
            }
        }

        /// <summary>
        /// Получает сущность по ID
        /// </summary>
        /// <param name="id">ID сущности</param>
        /// <returns>Сущность или null</returns>
        public IEntity GetEntity(int id)
        {
            _entities.TryGetValue(id, out var entity);
            return entity;
        }

        /// <summary>
        /// Получает все сущности
        /// </summary>
        /// <returns>Список всех сущностей</returns>
        public IEnumerable<IEntity> GetAllEntities()
        {
            return _entities.Values;
        }

        /// <summary>
        /// Получает сущности с определенным компонентом
        /// </summary>
        /// <typeparam name="T">Тип компонента</typeparam>
        /// <returns>Список сущностей с компонентом</returns>
        public IEnumerable<IEntity> GetEntitiesWithComponent<T>() where T : IComponent
        {
            return _entities.Values.Where(e => e.HasComponent<T>());
        }

        /// <summary>
        /// Уведомляет о изменении сущности (добавление/удаление компонентов)
        /// </summary>
        /// <param name="entity">Измененная сущность</param>
        public void NotifyEntityChanged(IEntity entity)
        {
            if (entity != null && _entities.ContainsKey(entity.Id))
            {
                OnEntityChanged?.Invoke(entity);
            }
        }

        #endregion

        #region System Management

        /// <summary>
        /// Регистрирует систему обработки сущностей
        /// </summary>
        /// <param name="system">Система для регистрации</param>
        public void RegisterSystem(IEntitySystem system)
        {
            if (system == null)
            {
                Debug.LogWarning("[EntityManager] Попытка зарегистрировать null систему!");
                return;
            }

            if (_systems.Contains(system))
            {
                Debug.LogWarning($"[EntityManager] Система {system.GetType().Name} уже зарегистрирована!");
                return;
            }

            _systems.Add(system);
            system.Initialize();
        }

        /// <summary>
        /// Удаляет систему обработки сущностей
        /// </summary>
        /// <param name="system">Система для удаления</param>
        public void UnregisterSystem(IEntitySystem system)
        {
            if (system == null) return;

            if (_systems.Remove(system))
            {
                system.Shutdown();
            }
        }

        #endregion
    }
}
