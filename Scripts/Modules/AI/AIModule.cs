using DRAUM.Core;
using DRAUM.Core.Infrastructure.Logger;
using UnityEngine;
using UnityServiceLocator;
using DependencyInjection;

namespace DRAUM.Modules.AI
{
    /// <summary>
    /// Модуль управления AI на основе GOAP системы.
    /// Управляет инициализацией GoapFactory и следит за GoapAgent'ами в сцене.
    /// </summary>
    public class AIModule : BehaviorModuleBase
    {
        [Header("GOAP Settings")]
        [Tooltip("Ссылка на GoapFactory. Если не назначена, будет создана автоматически.")]
        public GoapFactory goapFactory;

        [Tooltip("Автоматически создавать GoapFactory если он отсутствует в сцене")]
        public bool autoCreateGoapFactory = true;

        // Логи включаются/выключаются через `LogSettings` (Console-воронка).

        /// <summary>
        /// Имя модуля
        /// </summary>
        public override string ModuleName => "AI";

        /// <summary>
        /// Приоритет инициализации - AI инициализируется после игрока и инвентаря
        /// </summary>
        public override int InitializationPriority => 30;

        #region Private Fields

        private GoapAgent[] _goapAgents;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Awake вызывается очень рано, до инициализации модуля.
        /// Проверяем наличие GoapFactory.
        /// GoapFactory должен существовать в сцене заранее или создаваться через prefab.
        /// </summary>
        protected virtual void Awake()
        {

            if (goapFactory == null)
            {
                goapFactory = FindFirstObjectByType<GoapFactory>();
            }

            if (goapFactory == null && autoCreateGoapFactory)
            {
                GameObject factoryObject = new GameObject("GoapFactory");
                goapFactory = factoryObject.AddComponent<GoapFactory>();
                DraumLogger.Warning(this, "[AIModule] GoapFactory created automatically, but Injector already ran. " +
                               "Рекомендуется добавить GoapFactory в сцену заранее или через prefab.");
            }
        }

        #endregion

        #region Initialization

        protected override void OnInitialize()
        {
            ValidateGoapFactory();
            RefreshGoapAgents();

            DraumLogger.Info(this, "[AIModule] AI (GOAP) module initialized.");
            
            DraumLogger.Info(this, $"[AIModule] Found GoapAgent(s) in scene: {_goapAgents?.Length ?? 0}");
        }

        protected override void OnShutdown()
        {
            _goapAgents = null;
            DraumLogger.Info(this, "[AIModule] AI module unloaded.");
        }

        #endregion

        #region Update

        protected override void OnUpdate()
        {
            // GoapAgent'ы обновляются самостоятельно через MonoBehaviour.Update
            // Здесь можно добавить дополнительную логику мониторинга или оптимизации
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Проверяет что GoapFactory корректно инициализирован для DI системы
        /// </summary>
        private void ValidateGoapFactory()
        {
            if (goapFactory == null)
            {
                goapFactory = FindFirstObjectByType<GoapFactory>();
            }

            if (goapFactory != null)
            {
                if (ServiceLocator.Global.TryGet<GoapFactory>(out var registeredFactory))
                {
                    DraumLogger.Info(this, "[AIModule] GoapFactory successfully registered in ServiceLocator.");
                }
                else
                {
                    DraumLogger.Warning(this, "[AIModule] GoapFactory not found in ServiceLocator. This is normal; it is used through DependencyInjection.");
                }
            }
            else
            {
                DraumLogger.Error(this, "[AIModule] GoapFactory not found! GOAP agents won't work. Add GoapFactory to the scene or enable autoCreateGoapFactory.");
            }
        }

        /// <summary>
        /// Обновляет список GoapAgent'ов в сцене
        /// </summary>
        private void RefreshGoapAgents()
        {
            _goapAgents = FindObjectsByType<GoapAgent>(FindObjectsSortMode.None);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Получает все GoapAgent'ы в сцене
        /// </summary>
        public GoapAgent[] GetAllGoapAgents()
        {
            if (_goapAgents == null)
            {
                RefreshGoapAgents();
            }
            return _goapAgents;
        }

        /// <summary>
        /// Получает количество активных GoapAgent'ов в сцене
        /// </summary>
        public int GetActiveAgentCount()
        {
            if (_goapAgents == null)
            {
                RefreshGoapAgents();
            }

            if (_goapAgents == null) return 0;

            int count = 0;
            foreach (var agent in _goapAgents)
            {
                if (agent != null && agent.enabled && agent.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }
            return count;
        }

        #endregion
    }
}

