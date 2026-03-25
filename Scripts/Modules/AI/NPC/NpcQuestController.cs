using System.Collections.Generic;
using DRAUM.Core;
using DRAUM.Core.Infrastructure.Logger;
using DRAUM.Modules.Quest;
using DRAUM.Modules.Quest.Data;
using DRAUM.Modules.Quest.Events;
using DRAUM.Modules.Quest.Runtime;
using UnityEngine;
using UnityEngine.Events;

namespace DRAUM.Modules.AI.NPC
{
    public class NpcQuestController : MonoBehaviour
    {
        [Header("NPC Identity")]
        public string npcId;

        [Header("Interaction")]
        [Tooltip("Автоматически привязывает InteractableObject для prompt/интеракта через FirstPersonController.")]
        public bool autoConfigureInteractable = true;
        [Tooltip("Текст в InteractionPanel.")]
        public string talkPrompt = "Поговорить";

        [Header("Quest Chain")]
        public List<QuestDefinition> quests = new();
        public int activeQuestIndex;

        private QuestModule _questModule;
        private InteractableObject _interactable;

        private void Awake()
        {
            _questModule = FindFirstObjectByType<QuestModule>();
            if (_questModule == null)
            {
                DraumLogger.Warning(this, "[NpcQuestController] QuestModule not found in scene.");
            }

            TryConfigureInteractable();
        }

        private void OnEnable()
        {
            TryConfigureInteractable();
        }

        private void Reset()
        {
            TryConfigureInteractable();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying) return;
            TryConfigureInteractable(allowAddComponent: false);
        }

        public QuestDefinition GetCurrentQuest()
        {
            if (activeQuestIndex < 0 || activeQuestIndex >= quests.Count) return null;
            return quests[activeQuestIndex];
        }

        public QuestProgressState GetCurrentQuestState()
        {
            var current = GetCurrentQuest();
            if (current == null || _questModule == null) return QuestProgressState.NotStarted;
            return _questModule.GetOrCreateState(current.questId).progressState;
        }

        public void AcceptCurrentQuest()
        {
            var current = GetCurrentQuest();
            if (current == null || _questModule == null) return;
            EventBus.Instance?.Publish(new QuestAcceptRequestEvent { QuestId = current.questId });
            DraumLogger.Info(this, $"[NpcQuestController] Quest accepted: {current.questId}");
        }

        public void DeclineCurrentQuest()
        {
            var current = GetCurrentQuest();
            if (current == null || _questModule == null) return;
            EventBus.Instance?.Publish(new QuestDeclineRequestEvent { QuestId = current.questId });
            DraumLogger.Info(this, $"[NpcQuestController] Quest declined: {current.questId}");
        }

        public void CompleteCurrentQuestAndAdvance()
        {
            var current = GetCurrentQuest();
            if (current == null || _questModule == null) return;
            EventBus.Instance?.Publish(new QuestCompleteRequestEvent { QuestId = current.questId });
            activeQuestIndex++;
            DraumLogger.Info(this, $"[NpcQuestController] Quest completed: {current.questId}");
        }

        public void OpenDialog()
        {
            var current = GetCurrentQuest();
            if (current == null)
            {
                DraumLogger.Warning(this, $"[NpcQuestController] OpenDialog skipped: current quest is null. npcId={npcId}");
                return;
            }

            if (EventBus.Instance == null)
            {
                DraumLogger.Warning(this, $"[NpcQuestController] OpenDialog skipped: EventBus is null. npcId={npcId}");
                return;
            }

            EventBus.Instance.Publish(new QuestDialogOpenRequestEvent
            {
                NpcId = npcId,
                Quest = current
            });
            DraumLogger.Info(this, $"[NpcQuestController] OpenDialog published. npcId={npcId}, questId={current.questId}");
        }

        public void CloseDialog()
        {
            EventBus.Instance?.Publish(new QuestDialogCloseRequestEvent
            {
                NpcId = npcId
            });
        }

        private void TryConfigureInteractable(bool allowAddComponent = true)
        {
            if (!autoConfigureInteractable) return;

            _interactable = GetComponent<InteractableObject>();
            if (_interactable == null && allowAddComponent)
            {
                _interactable = gameObject.AddComponent<InteractableObject>();
            }
            if (_interactable == null) return;

            if (_interactable.onCustomInteract == null)
            {
                _interactable.onCustomInteract = new UnityEvent();
            }

            _interactable.interactionType = InteractionType.Custom;
            _interactable.interactionName = string.IsNullOrWhiteSpace(talkPrompt) ? "Поговорить" : talkPrompt;
            _interactable.onCustomInteract.RemoveListener(OpenDialog);
            _interactable.onCustomInteract.AddListener(OpenDialog);
            DraumLogger.Info(this, $"[NpcQuestController] Interactable configured. npcId={npcId}, prompt={_interactable.interactionName}");
        }
    }
}
