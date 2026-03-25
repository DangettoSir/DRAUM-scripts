using System;
using System.Collections.Generic;
using UnityEngine;

namespace DRAUM.Core
{
    /// <summary>
    /// Базовая реализация сущности (Entity).
    /// </summary>
    public class Entity : MonoBehaviour, IEntity
    {
        #region Fields

        private static int _nextId = 1;
        private readonly Dictionary<Type, IComponent> _components = new Dictionary<Type, IComponent>();
        private bool _isActive = true;

        #endregion

        #region IEntity Implementation

        /// <summary>
        /// Уникальный идентификатор сущности
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Имя сущности (опционально, для отладки)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Активна ли сущность
        /// </summary>
        public bool IsActive => _isActive && gameObject.activeInHierarchy;

        /// <summary>
        /// Добавляет компонент к сущности
        /// </summary>
        public void AddComponent<T>(T component) where T : IComponent
        {
            if (component == null)
            {
                Debug.LogWarning($"[Entity] Попытка добавить null компонент к сущности {Id}");
                return;
            }

            Type componentType = typeof(T);
            
            if (_components.ContainsKey(componentType))
            {
                Debug.LogWarning($"[Entity] Компонент {componentType.Name} уже существует в сущности {Id}. Заменяем.");
            }

            component.Entity = this;
            _components[componentType] = component;
        }

        /// <summary>
        /// Получает компонент ECS по типу (не путать с Unity Component.GetComponent)
        /// </summary>
        public new T GetComponent<T>() where T : IComponent
        {
            Type componentType = typeof(T);
            
            if (_components.TryGetValue(componentType, out var component))
            {
                return (T)component;
            }

            return default(T);
        }

        /// <summary>
        /// Проверяет наличие компонента
        /// </summary>
        public bool HasComponent<T>() where T : IComponent
        {
            return _components.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Удаляет компонент
        /// </summary>
        public bool RemoveComponent<T>() where T : IComponent
        {
            Type componentType = typeof(T);
            
            if (_components.Remove(componentType))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Получает все компоненты сущности
        /// </summary>
        public IEnumerable<IComponent> GetAllComponents()
        {
            return _components.Values;
        }

        /// <summary>
        /// Активирует сущность
        /// </summary>
        public void Activate()
        {
            _isActive = true;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Деактивирует сущность
        /// </summary>
        public void Deactivate()
        {
            _isActive = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Уничтожает сущность
        /// </summary>
        public void Destroy()
        {
            _components.Clear();
            Destroy(gameObject);
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Id = _nextId++;
            Name = Name ?? gameObject.name;
        }

        private void OnDestroy()
        {
            _components.Clear();
        }

        #endregion
    }
}
