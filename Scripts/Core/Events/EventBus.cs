using System;
using System.Collections.Generic;
using UnityEngine;

namespace DRAUM.Core
{
    /// <summary>
    /// Централизованная система событий для коммуникации между модулями.
    /// EventBus для передачи событий между модулями.
    /// </summary>
    public class EventBus : MonoBehaviour
    {
        #region Singleton

        private static EventBus _instance;

        public static EventBus Instance
        {
            get
            {
                if (_instance != null) return _instance;
                if (!Application.isPlaying) return null;
                _instance = FindFirstObjectByType<EventBus>();
                return _instance;
            }
        }

        #endregion

        #region Fields

        private readonly Dictionary<Type, List<object>> _subscribers = new Dictionary<Type, List<object>>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[EventBus] Попытка создать второй экземпляр EventBus. Уничтожаем дубликат.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
            Clear();
        }

        private void OnApplicationQuit()
        {
            _instance = null;
            Clear();
        }

        #endregion

        #region Subscribe/Unsubscribe

        /// <summary>
        /// Подписывается на события типа T
        /// </summary>
        /// <typeparam name="T">Тип события</typeparam>
        /// <param name="handler">Обработчик события</param>
        public void Subscribe<T>(Action<T> handler) where T : class
        {
            if (handler == null) return;
            Type eventType = typeof(T);
            if (!_subscribers.ContainsKey(eventType))
                _subscribers[eventType] = new List<object>();
            _subscribers[eventType].Add(handler);
        }

        /// <summary>
        /// Отписывается от событий типа T
        /// </summary>
        public void Unsubscribe<T>(Action<T> handler) where T : class
        {
            if (handler == null) return;
            Type eventType = typeof(T);
            if (!_subscribers.TryGetValue(eventType, out var handlers)) return;
            handlers.Remove(handler);
            if (handlers.Count == 0)
                _subscribers.Remove(eventType);
        }

        #endregion

        #region Publish

        /// <summary>
        /// Публикует событие типа T
        /// </summary>
        /// <typeparam name="T">Тип события</typeparam>
        /// <param name="eventData">Данные события</param>
        public void Publish<T>(T eventData) where T : class
        {
            if (eventData == null) return;
            Type eventType = typeof(T);
            if (!_subscribers.TryGetValue(eventType, out var handlers) || handlers.Count == 0)
                return;
            var list = new List<object>(handlers);
            foreach (var handler in list)
            {
                if (handler == null) continue;
                try
                {
                    ((Action<T>)handler)(eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] Ошибка при обработке события {eventType.Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Публикует событие без данных
        /// </summary>
        /// <typeparam name="T">Тип события</typeparam>
        public void Publish<T>() where T : class, new()
        {
            Publish(new T());
        }

        #endregion

        #region Clear

        /// <summary>
        /// Очищает все подписки
        /// </summary>
        public void Clear()
        {
            _subscribers.Clear();
        }

        #endregion
    }
}
