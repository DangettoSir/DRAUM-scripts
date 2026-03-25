using System.Collections.Generic;
using DRAUM.Core;
using DRAUM.Core.Infrastructure.Logger;
using DRAUM.Modules.Inventory;
using DRAUM.Modules.Quest.Data;
using DRAUM.Modules.Quest.Events;
using DRAUM.Modules.Quest.Runtime;
using UnityEngine;

namespace DRAUM.Modules.Quest.UI
{
    public class QuestTrackerUIModule : ServiceModuleBase
    {
        public override string ModuleName => "QuestTrackerUI";
        public override int InitializationPriority => 47;

        [Header("References")]
        public QuestModule questModule;
        public InventoryModule inventoryModule;

        [Header("Containers")]
        public Transform activeContainer;
        public Transform completedContainer;

        [Header("Prefabs")]
        public QuestRowView rowPrefab;

        [Header("Auto Refresh")]
        public bool rebuildOnInit = true;
        public bool clearEmptyContainersOnInit = true;

        private readonly List<QuestRowView> _spawnedRows = new List<QuestRowView>();

        protected override void OnInitialize()
        {
            if (questModule == null)
            {
                questModule = FindFirstObjectByType<QuestModule>();
            }
            if (inventoryModule == null)
            {
                inventoryModule = FindFirstObjectByType<InventoryModule>();
            }

            if (questModule == null)
            {
                DraumLogger.Warning(this, "[QuestTrackerUIModule] QuestModule not found.");
                return;
            }

            Events.Subscribe<QuestStateChangedEvent>(OnQuestStateChanged);

            if (rebuildOnInit)
            {
                Rebuild();
            }

            DraumLogger.Info(this, "[QuestTrackerUIModule] Initialized.");
        }

        protected override void OnShutdown()
        {
            if (EventBus.Instance != null)
            {
                Events.Unsubscribe<QuestStateChangedEvent>(OnQuestStateChanged);
            }

            ClearRows();
        }

        private void OnQuestStateChanged(QuestStateChangedEvent evt)
        {
            Rebuild();
        }

        [ContextMenu("Rebuild Quest Tracker")]
        public void Rebuild()
        {
            if (questModule == null || rowPrefab == null)
            {
                DraumLogger.Warning(this, "[QuestTrackerUIModule] Cannot rebuild: missing questModule or rowPrefab.");
                return;
            }

            ClearRows();

            var active = questModule.GetDefinitionsByState(QuestProgressState.Active);
            var completed = questModule.GetDefinitionsByState(QuestProgressState.Completed);

            SpawnRows(active, activeContainer, true, false);
            SpawnRows(completed, completedContainer, false, true);

            DraumLogger.Info(this, $"[QuestTrackerUIModule] Rebuilt. Active={active.Count}, Completed={completed.Count}");
        }

        private void SpawnRows(IReadOnlyList<QuestDefinition> defs, Transform container, bool isActive, bool isCompleted)
        {
            if (defs == null || container == null) return;

            foreach (var def in defs)
            {
                if (def == null) continue;
                var row = Instantiate(rowPrefab, container);
                string giver = string.IsNullOrWhiteSpace(def.questGiverId) ? "Unknown" : def.questGiverId;
                string rewards = BuildRewardsText(def);
                string tracking = BuildTrackingStatus(def, isActive, isCompleted);
                row.Bind(def.questName, def.questDescription, giver, rewards, tracking, isActive, isCompleted);
                _spawnedRows.Add(row);
            }
        }

        private string BuildRewardsText(QuestDefinition def)
        {
            if (def == null) return "-";
            var parts = new List<string>();
            if (def.coinReward > 0) parts.Add($"{def.coinReward} coins");
            if (def.rewardItem1 != null) parts.Add(def.rewardItem1.name);
            if (def.rewardItem2 != null) parts.Add(def.rewardItem2.name);
            return parts.Count == 0 ? "-" : string.Join(", ", parts);
        }

        private string BuildTrackingStatus(QuestDefinition def, bool isActive, bool isCompleted)
        {
            if (isCompleted) return "Completed";
            if (!isActive) return "Not tracked";
            if (def != null && HasAllRequirements(def)) return "Ready to turn in";
            return "In progress";
        }

        private bool HasAllRequirements(QuestDefinition quest)
        {
            if (quest == null || quest.itemRequirements == null || quest.itemRequirements.Count == 0) return true;
            if (inventoryModule == null) return false;

            foreach (var req in quest.itemRequirements)
            {
                if (req == null || req.itemData == null || req.amount <= 0) continue;
                if (CountItems(req.itemData) < req.amount) return false;
            }

            return true;
        }

        private int CountItems(ItemData itemData)
        {
            if (itemData == null || inventoryModule == null) return 0;
            return inventoryModule.CountItems(itemData);
        }

        private void ClearRows()
        {
            foreach (var row in _spawnedRows)
            {
                if (row != null) Destroy(row.gameObject);
            }
            _spawnedRows.Clear();

            if (!clearEmptyContainersOnInit) return;
            DestroyChildren(activeContainer);
            DestroyChildren(completedContainer);
        }

        private static void DestroyChildren(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child != null) Destroy(child.gameObject);
            }
        }
    }
}
