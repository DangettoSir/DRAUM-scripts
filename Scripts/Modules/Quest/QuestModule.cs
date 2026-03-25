using System.Collections.Generic;
using DRAUM.Core;
using DRAUM.Core.Infrastructure.Logger;
using DRAUM.Modules.Quest.Data;
using DRAUM.Modules.Quest.Events;
using DRAUM.Modules.Quest.Runtime;
using UnityEngine;

namespace DRAUM.Modules.Quest
{
    public class QuestModule : BehaviorModuleBase
    {
        public override string ModuleName => "Quest";
        public override int InitializationPriority => 37;

        [Header("Quest Database")]
        public List<QuestDefinition> questDefinitions = new();

        private readonly Dictionary<string, QuestDefinition> _definitionsById = new();
        private readonly Dictionary<string, QuestState> _statesById = new();

        protected override void OnInitialize()
        {
            _definitionsById.Clear();
            foreach (var def in questDefinitions)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.questId)) continue;
                _definitionsById[def.questId] = def;
            }

            DraumLogger.Info(this, $"[QuestModule] Initialized. Definitions: {_definitionsById.Count}");

            Events.Subscribe<QuestAcceptRequestEvent>(OnQuestAcceptRequested);
            Events.Subscribe<QuestDeclineRequestEvent>(OnQuestDeclineRequested);
            Events.Subscribe<QuestCompleteRequestEvent>(OnQuestCompleteRequested);
        }

        protected override void OnShutdown()
        {
            if (EventBus.Instance != null)
            {
                Events.Unsubscribe<QuestAcceptRequestEvent>(OnQuestAcceptRequested);
                Events.Unsubscribe<QuestDeclineRequestEvent>(OnQuestDeclineRequested);
                Events.Unsubscribe<QuestCompleteRequestEvent>(OnQuestCompleteRequested);
            }

            _definitionsById.Clear();
            _statesById.Clear();
        }

        public QuestDefinition GetDefinition(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId)) return null;
            _definitionsById.TryGetValue(questId, out var def);
            return def;
        }

        public QuestState GetOrCreateState(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId)) return null;

            if (_statesById.TryGetValue(questId, out var existing))
                return existing;

            var state = new QuestState
            {
                questId = questId,
                progressState = QuestProgressState.NotStarted,
                initialDialogCompleted = false
            };

            _statesById[questId] = state;
            return state;
        }

        public IReadOnlyList<QuestDefinition> GetAllDefinitions()
        {
            return questDefinitions;
        }

        public IReadOnlyList<QuestDefinition> GetDefinitionsByState(QuestProgressState state)
        {
            var result = new List<QuestDefinition>();
            foreach (var def in questDefinitions)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.questId)) continue;
                var currentState = GetOrCreateState(def.questId);
                if (currentState != null && currentState.progressState == state)
                {
                    result.Add(def);
                }
            }
            return result;
        }

        public void SetQuestState(string questId, QuestProgressState state)
        {
            var questState = GetOrCreateState(questId);
            if (questState == null) return;
            questState.progressState = state;
            Events.Publish(new QuestStateChangedEvent
            {
                QuestId = questId,
                NewState = state
            });
            DraumLogger.Info(this, $"[QuestModule] State changed: {questId} -> {state}");
        }

        private void OnQuestAcceptRequested(QuestAcceptRequestEvent evt)
        {
            if (evt == null || string.IsNullOrWhiteSpace(evt.QuestId)) return;
            SetQuestState(evt.QuestId, QuestProgressState.Active);
        }

        private void OnQuestDeclineRequested(QuestDeclineRequestEvent evt)
        {
            if (evt == null || string.IsNullOrWhiteSpace(evt.QuestId)) return;
            SetQuestState(evt.QuestId, QuestProgressState.Declined);
        }

        private void OnQuestCompleteRequested(QuestCompleteRequestEvent evt)
        {
            if (evt == null || string.IsNullOrWhiteSpace(evt.QuestId)) return;
            SetQuestState(evt.QuestId, QuestProgressState.Completed);
        }
    }
}
