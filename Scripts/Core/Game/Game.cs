using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DRAUM.Core.Infrastructure.Logger;

namespace DRAUM.Core
{
    /// <summary>
    /// Абстрактный базовый класс для центрального менеджера игры.
    /// Game = ДИРИЖЁР | Модули = ОРКЕСТР
    /// Game - это точка сборки и управления жизненным циклом игры, а НЕ контейнер всей логики. ЗАПОМНИТЕ ДЕТИ ЗАПОМНИТЕ
    /// </summary>
    public abstract class Game : MonoBehaviour
    {
        #region Singleton Pattern

        private static Game _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Единственный экземпляр Game в сцене
        /// </summary>
        public static Game Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = FindFirstObjectByType<Game>();
                            if (_instance == null)
                            {
                                Debug.LogError("[Game] Game instance not found in the scene!");
                            }
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// Реестр всех зарегистрированных модулей
        /// </summary>
        protected readonly Dictionary<string, IGameModule> _modules = new Dictionary<string, IGameModule>();

        /// <summary>
        /// Список модулей, отсортированный по приоритету инициализации
        /// </summary>
        protected List<IGameModule> _initializedModules = new List<IGameModule>();

        /// <summary>
        /// Флаг, указывающий, инициализирована ли игра
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Флаг, указывающий, запущена ли игра
        /// </summary>
        public bool IsRunning { get; private set; }

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                DraumLogger.Warning(this, "[Game] Attempted to create a second Game instance. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void Start()
        {
            InitializeGame();
        }

        protected virtual void Update()
        {
            if (!IsRunning) return;

            foreach (var module in _initializedModules)
            {
                if (module.IsActive)
                {
                    module.Update();
                }
            }
        }

        protected virtual void FixedUpdate()
        {
            if (!IsRunning) return;

            foreach (var module in _initializedModules)
            {
                if (module.IsActive)
                {
                    module.FixedUpdate();
                }
            }
        }

        protected virtual void LateUpdate()
        {
            if (!IsRunning) return;
            foreach (var module in _initializedModules)
            {
                if (module.IsActive && module is GameModuleBase baseModule)
                {
                    baseModule.LateUpdate();
                }
            }
        }

        protected virtual void OnDestroy()
        {
            ShutdownGame();
        }

        protected virtual void OnApplicationQuit()
        {
            ShutdownGame();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализация игры. Вызывается автоматически в Start().
        /// </summary>
        protected virtual void InitializeGame()
        {
            if (IsInitialized)
            {
                DraumLogger.Warning(this, "[Game] Game is already initialized!");
                return;
            }

            DraumLogger.Info(this, "[Game] Starting game initialization...");

            RegisterModules();

            InitializeModules();

            IsInitialized = true;
            IsRunning = true;

            DraumLogger.Info(this, $"[Game] Game initialized successfully. Registered modules: {_modules.Count}");
        }

        /// <summary>
        /// Регистрация всех модулей игры.
        /// Переопределите этот метод в наследниках для регистрации ваших модулей.
        /// </summary>
        protected abstract void RegisterModules();

        /// <summary>
        /// Инициализация всех зарегистрированных модулей в порядке приоритета
        /// </summary>
        protected virtual void InitializeModules()
        {
            var sortedModules = _modules.Values
                .OrderBy(m => m.InitializationPriority)
                .ToList();

            foreach (var module in sortedModules)
            {
                try
                {
                    DraumLogger.Info(this, $"[Game] Initializing module: {module.ModuleName} (priority: {module.InitializationPriority})");
                    module.Initialize(this);
                    _initializedModules.Add(module);
                    DraumLogger.Info(this, $"[Game] Module {module.ModuleName} initialized successfully.");
                }
                catch (Exception ex)
                {
                    DraumLogger.Error(this, $"[Game] Error while initializing module {module.ModuleName}: {ex.Message}");
                }
            }
        }

        #endregion

        #region Module Management

        /// <summary>
        /// Регистрирует модуль в системе
        /// </summary>
        /// <param name="module">Модуль для регистрации</param>
        /// <exception cref="ArgumentException">Если модуль с таким именем уже зарегистрирован</exception>
        public virtual void RegisterModule(IGameModule module)
        {
            if (module == null)
            {
                DraumLogger.Error(this, "[Game] Attempted to register a null module!");
                return;
            }

            if (_modules.ContainsKey(module.ModuleName))
            {
                DraumLogger.Error(this, $"[Game] Module with name '{module.ModuleName}' is already registered!");
                return;
            }

            _modules[module.ModuleName] = module;
            DraumLogger.Info(this, $"[Game] Module '{module.ModuleName}' registered.");
        }

        /// <summary>
        /// Получает модуль по имени
        /// </summary>
        /// <typeparam name="T">Тип модуля</typeparam>
        /// <param name="moduleName">Имя модуля</param>
        /// <returns>Модуль или null, если не найден</returns>
        public virtual T GetModule<T>(string moduleName) where T : class, IGameModule
        {
            if (_modules.TryGetValue(moduleName, out var module))
            {
                return module as T;
            }

            DraumLogger.Warning(this, $"[Game] Module '{moduleName}' not found!");
            return null;
        }

        /// <summary>
        /// Получает модуль по типу (если имя модуля совпадает с именем типа)
        /// </summary>
        /// <typeparam name="T">Тип модуля</typeparam>
        /// <returns>Модуль или null, если не найден</returns>
        public virtual T GetModule<T>() where T : class, IGameModule
        {
            return GetModule<T>(typeof(T).Name);
        }

        /// <summary>
        /// Проверяет, зарегистрирован ли модуль
        /// </summary>
        /// <param name="moduleName">Имя модуля</param>
        /// <returns>True, если модуль зарегистрирован</returns>
        public virtual bool HasModule(string moduleName)
        {
            return _modules.ContainsKey(moduleName);
        }

        /// <summary>
        /// Активирует модуль
        /// </summary>
        /// <param name="moduleName">Имя модуля</param>
        public virtual void ActivateModule(string moduleName)
        {
            if (_modules.TryGetValue(moduleName, out var module))
            {
                module.Activate();
            }
            else
            {
                DraumLogger.Warning(this, $"[Game] Module '{moduleName}' not found for activation!");
            }
        }

        /// <summary>
        /// Деактивирует модуль
        /// </summary>
        /// <param name="moduleName">Имя модуля</param>
        public virtual void DeactivateModule(string moduleName)
        {
            if (_modules.TryGetValue(moduleName, out var module))
            {
                module.Deactivate();
            }
            else
            {
                DraumLogger.Warning(this, $"[Game] Module '{moduleName}' not found for deactivation!");
            }
        }

        #endregion

        #region Game Control

        /// <summary>
        /// Приостанавливает игру (устанавливает Time.timeScale = 0)
        /// </summary>
        public virtual void Pause()
        {
            if (!IsRunning) return;

            Time.timeScale = 0f;
            DraumLogger.Info(this, "[Game] Game paused.");
        }

        /// <summary>
        /// Возобновляет игру (устанавливает Time.timeScale = 1)
        /// </summary>
        public virtual void Resume()
        {
            if (!IsRunning) return;

            Time.timeScale = 1f;
            DraumLogger.Info(this, "[Game] Game resumed.");
        }

        /// <summary>
        /// Завершает работу игры и выгружает все модули
        /// </summary>
        protected virtual void ShutdownGame()
        {
            if (!IsInitialized) return;

            DraumLogger.Info(this, "[Game] Shutting down game...");

            IsRunning = false;

            for (int i = _initializedModules.Count - 1; i >= 0; i--)
            {
                try
                {
                    _initializedModules[i].Shutdown();
                }
                catch (Exception ex)
                {
                    DraumLogger.Error(this, $"[Game] Error while unloading module {_initializedModules[i].ModuleName}: {ex.Message}");
                }
            }

            _modules.Clear();
            _initializedModules.Clear();
            IsInitialized = false;

            DraumLogger.Info(this, "[Game] Game shutdown complete.");
        }

        #endregion
    }
}
