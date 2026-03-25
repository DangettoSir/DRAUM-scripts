using UnityEngine;
using System.Reflection;
using DRAUM.Modules.Player;
using DRAUM.Modules.Inventory;
using DRAUM.Modules.AI;
using DRAUM.Modules.Audio;
using DRAUM.Modules.Combat;
using DRAUM.Modules.Quest;
using DRAUM.Modules.Quest.UI;
using DRAUM.Core.Infrastructure.Logger;

namespace DRAUM.Core
{
    /// <summary>
    /// Конкретная реализация Game для проекта DRAUM.
    /// Вся логика должна быть в модулях!!!!!!!!!
    /// </summary>
    public class DraumGame : Game
    {
        [Header("Module References")]
        [Tooltip("Ссылки на модули игры. Назначьте их в инспекторе Unity.")]
        public PlayerModule playerModule;

        public InventoryModule inventoryModule;

        public AIModule aiModule;

        public AudioModule audioModule;

    [Header("Core Infrastructure")]
    [Tooltip("Модуль логгера (пишет ложки в logs/<timestamp>/...)")]
    public LoggerModule loggerModule;

        [Header("Combat")]
        [Tooltip("Связка боёвки для Core-архитектуры (оркестровка боевых компонент)")]
        public CombatModule combatModule;

        [Header("Quest")]
        [Tooltip("Квестовый модуль (ScriptableObject-конфиги + runtime states)")]
        public QuestModule questModule;

        [Tooltip("UI-модуль диалогов квестов (через EventBus + lock state)")]
        public QuestDialogModule questDialogModule;

        [Tooltip("UI-трекер квестов (active/completed списки)")]
        public QuestTrackerUIModule questTrackerUIModule;

        // Примеры других модулей
        // public CombatModule combatModule;
        // public UIModule uiModule;
        // public AudioModule audioModule;

        protected override void Awake()
        {
            base.Awake();
            if (EventBus.Instance == null && GetComponent<EventBus>() == null)
                gameObject.AddComponent<EventBus>();

        if (loggerModule == null)
            loggerModule = GetComponent<LoggerModule>() ?? gameObject.AddComponent<LoggerModule>();
        }

        /// <summary>
        /// Регистрация всех модулей игры.
        /// Автоматически регистрирует все поля типа GameModuleBase (или наследников).
        /// </summary>
        protected override void RegisterModules()
        {
            DraumLogger.Info(this, "[DraumGame] Registering modules...");

            var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (typeof(GameModuleBase).IsAssignableFrom(field.FieldType))
                {
                    var module = field.GetValue(this) as GameModuleBase;
                    if (module != null)
                    {
                        RegisterModule(module);
                    }
                    else
                    {
                        DraumLogger.Warning(this, $"[DraumGame] {field.Name} is not assigned in the inspector!");
                    }
                }
            }

            DraumLogger.Info(this, "[DraumGame] Module registration completed.");
        }

        /// <summary>
        /// Дополнительная инициализация после регистрации модулей.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            OnGameInitialized();
        }

        /// <summary>
        /// Вызывается после полной инициализации игры и всех модулей.
        /// </summary>
        protected virtual void OnGameInitialized()
        {
            DraumLogger.Info(this, "[DraumGame] DRAUM game is fully initialized and ready.");
        }
    }
}