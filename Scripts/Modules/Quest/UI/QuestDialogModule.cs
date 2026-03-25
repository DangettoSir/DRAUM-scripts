using DRAUM.Core;
using DRAUM.Core.Infrastructure.Logger;
using DRAUM.Modules.Inventory;
using DRAUM.Modules.Player.Events;
using DRAUM.Modules.Quest;
using DRAUM.Modules.Quest.Data;
using DRAUM.Modules.Quest.Events;
using DRAUM.Modules.Quest.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DRAUM.Modules.Quest.UI
{
    public class QuestDialogModule : ServiceModuleBase
    {
        public override string ModuleName => "QuestDialog";
        public override int InitializationPriority => 46;

        [Header("UI")]
        public Canvas dialogCanvas;
        public TextMeshProUGUI dialogText;
        public Button option1Button;
        public TextMeshProUGUI option1Text;
        public Button option2Button;
        public TextMeshProUGUI option2Text;
        [Tooltip("Объект crosshair/cursor, который надо скрывать на время диалога.")]
        public GameObject crosshairToToggle;
        
        [Header("Dialog Audio")]
        public AudioSource dialogAudioSource;
        [Range(0f, 1f)] public float dialogVoiceVolume = 1f;

        [Header("State")]
        public bool dialogOpen;
        public string currentNpcId;
        public QuestDefinition currentQuest;
        private FirstPersonController _playerController;
        private QuestModule _questModule;
        private InventoryModule _inventoryModule;

        protected override void OnInitialize()
        {
            if (dialogCanvas != null) dialogCanvas.gameObject.SetActive(false);
            dialogOpen = false;
            _playerController = FindFirstObjectByType<FirstPersonController>();
            _questModule = FindFirstObjectByType<QuestModule>();
            _inventoryModule = FindFirstObjectByType<InventoryModule>();
            EnsureDialogAudioSource();

            Events.Subscribe<QuestDialogOpenRequestEvent>(OnOpenRequested);
            Events.Subscribe<QuestDialogCloseRequestEvent>(OnCloseRequested);
            DraumLogger.Info(this, "[QuestDialogModule] Initialized.");
        }

        protected override void OnShutdown()
        {
            if (EventBus.Instance != null)
            {
                Events.Unsubscribe<QuestDialogOpenRequestEvent>(OnOpenRequested);
                Events.Unsubscribe<QuestDialogCloseRequestEvent>(OnCloseRequested);
            }
        }

        private void OnOpenRequested(QuestDialogOpenRequestEvent evt)
        {
            if (dialogCanvas == null) return;
            dialogCanvas.gameObject.SetActive(true);
            dialogOpen = true;
            currentNpcId = evt?.NpcId;
            currentQuest = evt?.Quest;
            SetCrosshairVisible(false);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ApplyHardPlayerLock(true);

            Events.Publish(new PlayerLockStateChangedEvent
            {
                IsLocked = true,
                MovementLocked = true,
                CameraLocked = true
            });

            if (dialogText != null && evt?.Quest != null)
            {
                ConfigureDialogByQuestState(evt.Quest);
            }

            DraumLogger.Info(this, $"[QuestDialogModule] Dialog opened. NPC={evt?.NpcId}");
        }

        private void OnCloseRequested(QuestDialogCloseRequestEvent evt)
        {
            if (dialogCanvas != null) dialogCanvas.gameObject.SetActive(false);
            dialogOpen = false;
            currentNpcId = null;
            currentQuest = null;

            if (_inventoryModule == null) _inventoryModule = FindFirstObjectByType<InventoryModule>();
            if (_inventoryModule == null || !_inventoryModule.IsOpen)
            {
                SetCrosshairVisible(true);
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            ApplyHardPlayerLock(false);

            Events.Publish(new PlayerLockStateChangedEvent
            {
                IsLocked = false,
                MovementLocked = false,
                CameraLocked = false
            });

            DraumLogger.Info(this, $"[QuestDialogModule] Dialog closed. NPC={evt?.NpcId}");
        }

        private void ConfigureDialogByQuestState(QuestDefinition quest)
        {
            if (quest == null)
            {
                ConfigureButtonsForCloseOnly("Закрыть");
                if (dialogText != null) dialogText.text = "...";
                return;
            }

            QuestProgressState state = QuestProgressState.NotStarted;
            if (_questModule != null)
            {
                state = _questModule.GetOrCreateState(quest.questId).progressState;
            }

            DraumLogger.Info(this, $"[QuestDialogModule] ConfigureDialog questId={quest.questId} state={state}");

            switch (state)
            {
                case QuestProgressState.NotStarted:
                    ConfigureButtonsForAcceptDecline(quest);
                    if (dialogText != null)
                    {
                        dialogText.text = quest.initialDialog != null && quest.initialDialog.Count > 0
                            ? quest.initialDialog[0]
                            : quest.questDescription;
                    }
                    PlayQuestClip(GetInitialDialogClip(quest));
                    break;

                case QuestProgressState.Active:
                {
                    bool readyForTurnIn = HasAllRequirements(quest);
                    LogQuestRequirementSnapshot(quest, readyForTurnIn);
                    if (readyForTurnIn)
                    {
                        if (dialogText != null)
                            dialogText.text = "Принес? Отлично, давай сюда.";
                        ConfigureButtonsForTurnIn("Сдать квест", "Потом");
                    }
                    else
                    {
                        if (dialogText != null)
                            dialogText.text = string.IsNullOrWhiteSpace(quest.comebackInProgress)
                                ? "Квест в процессе. Принеси предмет."
                                : quest.comebackInProgress;
                        ConfigureButtonsForCloseOnly("Ок");
                    }
                    PlayQuestClip(GetInitialDialogClip(quest));
                    break;
                }

                case QuestProgressState.Completed:
                    if (dialogText != null)
                        dialogText.text = string.IsNullOrWhiteSpace(quest.comebackCompleted)
                            ? "Йоу, квест уже выполнен."
                            : quest.comebackCompleted;
                    ConfigureButtonsForCloseOnly("Закрыть");
                    PlayQuestClip(GetInitialDialogClip(quest));
                    break;

                case QuestProgressState.Declined:
                    ConfigureButtonsForAcceptDecline(quest);
                    if (dialogText != null)
                        dialogText.text = string.IsNullOrWhiteSpace(quest.comebackAfterDecline)
                            ? "Передумал?"
                            : quest.comebackAfterDecline;
                    PlayQuestClip(quest.declineAnswerClip);
                    break;
            }
        }

        private void ConfigureButtonsForAcceptDecline(QuestDefinition quest)
        {
            if (option1Button != null) option1Button.gameObject.SetActive(true);
            if (option2Button != null) option2Button.gameObject.SetActive(true);
            if (option1Button != null)
            {
                option1Button.onClick.RemoveAllListeners();
                option1Button.onClick.AddListener(OnAcceptClicked);
            }
            if (option2Button != null)
            {
                option2Button.onClick.RemoveAllListeners();
                option2Button.onClick.AddListener(OnDeclineClicked);
            }

            if (option1Text != null)
            {
                option1Text.text = quest != null && !string.IsNullOrWhiteSpace(quest.acceptOption)
                    ? quest.acceptOption
                    : "Accept";
            }

            if (option2Text != null)
            {
                option2Text.text = quest != null && !string.IsNullOrWhiteSpace(quest.declineOption)
                    ? quest.declineOption
                    : "Decline";
            }
        }

        private void ConfigureButtonsForCloseOnly(string closeCaption)
        {
            if (option1Button != null)
            {
                option1Button.gameObject.SetActive(true);
                option1Button.onClick.RemoveAllListeners();
                option1Button.onClick.AddListener(() => Events.Publish(new QuestDialogCloseRequestEvent { NpcId = currentNpcId }));
            }
            if (option1Text != null) option1Text.text = closeCaption;

            if (option2Button != null) option2Button.gameObject.SetActive(false);
        }

        private void ConfigureButtonsForLeaveOnly(string leaveCaption)
        {
            if (option1Button != null) option1Button.gameObject.SetActive(false);

            if (option2Button != null)
            {
                option2Button.gameObject.SetActive(true);
                option2Button.onClick.RemoveAllListeners();
                option2Button.onClick.AddListener(() => Events.Publish(new QuestDialogCloseRequestEvent { NpcId = currentNpcId }));
            }

            if (option2Text != null) option2Text.text = leaveCaption;
        }

        private void ConfigureButtonsForTurnIn(string turnInCaption, string closeCaption)
        {
            if (option1Button != null) option1Button.gameObject.SetActive(true);
            if (option2Button != null) option2Button.gameObject.SetActive(true);

            if (option1Button != null)
            {
                option1Button.onClick.RemoveAllListeners();
                option1Button.onClick.AddListener(OnTurnInClicked);
            }
            if (option2Button != null)
            {
                option2Button.onClick.RemoveAllListeners();
                option2Button.onClick.AddListener(() => Events.Publish(new QuestDialogCloseRequestEvent { NpcId = currentNpcId }));
            }

            if (option1Text != null) option1Text.text = turnInCaption;
            if (option2Text != null) option2Text.text = closeCaption;
        }

        private void OnAcceptClicked()
        {
            if (currentQuest != null)
            {
                Events.Publish(new QuestAcceptRequestEvent { QuestId = currentQuest.questId });
                if (dialogText != null)
                {
                    dialogText.text = string.IsNullOrWhiteSpace(currentQuest.acceptAnswer)
                        ? "АХАХХАХА, давай выполняй."
                        : currentQuest.acceptAnswer;
                }
                PlayQuestClip(currentQuest.acceptAnswerClip);
            }

            ConfigureButtonsForLeaveOnly("Уйти");
        }

        private void OnDeclineClicked()
        {
            if (currentQuest != null)
            {
                Events.Publish(new QuestDeclineRequestEvent { QuestId = currentQuest.questId });
                if (dialogText != null)
                {
                    dialogText.text = string.IsNullOrWhiteSpace(currentQuest.declineAnswer)
                        ? "Ладно, как знаешь."
                        : currentQuest.declineAnswer;
                }
                PlayQuestClip(currentQuest.declineAnswerClip);
            }

            ConfigureButtonsForLeaveOnly("Уйти");
        }

        private void OnTurnInClicked()
        {
            if (currentQuest == null) return;
            if (!HasAllRequirements(currentQuest))
            {
                if (dialogText != null) dialogText.text = "Не хватает предметов для сдачи.";
                return;
            }

            if (!ConsumeRequirements(currentQuest))
            {
                if (dialogText != null) dialogText.text = "Не удалось снять предметы из инвентаря.";
                return;
            }
            if (!GrantRewards(currentQuest))
            {
                if (dialogText != null) dialogText.text = "Награда не выдана: проверь место в инвентаре.";
                return;
            }

            Events.Publish(new QuestCompleteRequestEvent { QuestId = currentQuest.questId });
            if (dialogText != null)
            {
                dialogText.text = string.IsNullOrWhiteSpace(currentQuest.finalWords)
                    ? "Йоу йоу, квест закрыт."
                    : currentQuest.finalWords;
            }
            PlayQuestClip(GetInitialDialogClip(currentQuest));
            ConfigureButtonsForCloseOnly("Закрыть");
        }

        private bool HasAllRequirements(QuestDefinition quest)
        {
            if (quest == null || quest.itemRequirements == null || quest.itemRequirements.Count == 0) return true;
            if (_inventoryModule == null) _inventoryModule = FindFirstObjectByType<InventoryModule>();
            if (_inventoryModule == null) return false;

            foreach (var req in quest.itemRequirements)
            {
                if (req == null || req.itemData == null || req.amount <= 0) continue;
                int count = CountItems(req.itemData);
                if (count < req.amount) return false;
            }

            return true;
        }

        private void LogQuestRequirementSnapshot(QuestDefinition quest, bool readyForTurnIn)
        {
            if (quest == null) return;
            if (_inventoryModule == null) _inventoryModule = FindFirstObjectByType<InventoryModule>();
            DraumLogger.Info(this, $"[QuestDialogModule] Active quest requirements: readyForTurnIn={readyForTurnIn}, inventoryModule={(_inventoryModule != null ? "OK" : "NULL")}");
            if (quest.itemRequirements == null || quest.itemRequirements.Count == 0)
            {
                DraumLogger.Info(this, "[QuestDialogModule] itemRequirements: (none)");
                return;
            }
            foreach (var req in quest.itemRequirements)
            {
                if (req == null || req.itemData == null) continue;
                int have = _inventoryModule != null ? _inventoryModule.CountItems(req.itemData) : 0;
                DraumLogger.Info(this, $"[QuestDialogModule]   req: {req.itemData.name} need={req.amount} have={have}");
            }
        }

        private int CountItems(ItemData itemData)
        {
            if (itemData == null || _inventoryModule == null) return 0;
            return _inventoryModule.CountItems(itemData);
        }

        private bool ConsumeRequirements(QuestDefinition quest)
        {
            if (quest == null || quest.itemRequirements == null || quest.itemRequirements.Count == 0) return true;
            if (_inventoryModule == null) _inventoryModule = FindFirstObjectByType<InventoryModule>();
            if (_inventoryModule == null) return false;

            foreach (var req in quest.itemRequirements)
            {
                if (req == null || req.itemData == null || req.amount <= 0) continue;

                for (int i = 0; i < req.amount; i++)
                {
                    if (!_inventoryModule.TryConsumeItems(req.itemData, 1))
                    {
                        DraumLogger.Warning(this, $"[QuestDialogModule] Requirement item not consumed: {req.itemData.name}");
                        return false;
                    }
                }
            }

            return true;
        }
        
        private bool GrantRewards(QuestDefinition quest)
        {
            if (quest == null) return true;
            if (_inventoryModule == null) _inventoryModule = FindFirstObjectByType<InventoryModule>();
            if (_inventoryModule == null)
            {
                DraumLogger.Warning(this, "[QuestDialogModule] GrantRewards skipped: InventoryModule is null.");
                return false;
            }

            if (quest.rewardItem1 != null && !_inventoryModule.AddItemToAnyInventory(quest.rewardItem1))
            {
                DraumLogger.Warning(this, $"[QuestDialogModule] Reward not added: {quest.rewardItem1.name}");
                return false;
            }

            if (quest.rewardItem2 != null && !_inventoryModule.AddItemToAnyInventory(quest.rewardItem2))
            {
                DraumLogger.Warning(this, $"[QuestDialogModule] Reward not added: {quest.rewardItem2.name}");
                return false;
            }

            return true;
        }

        private void EnsureDialogAudioSource()
        {
            if (dialogAudioSource != null) return;
            dialogAudioSource = GetComponent<AudioSource>();
            if (dialogAudioSource == null)
            {
                dialogAudioSource = gameObject.AddComponent<AudioSource>();
            }
            dialogAudioSource.playOnAwake = false;
            dialogAudioSource.spatialBlend = 0f;
            dialogAudioSource.loop = false;
            dialogAudioSource.volume = dialogVoiceVolume;
        }

        private AudioClip GetInitialDialogClip(QuestDefinition quest)
        {
            if (quest == null || quest.initialDialogClips == null || quest.initialDialogClips.Count == 0) return null;
            return quest.initialDialogClips[0];
        }

        private void PlayQuestClip(AudioClip clip)
        {
            if (clip == null) return;
            EnsureDialogAudioSource();
            if (dialogAudioSource == null) return;

            dialogAudioSource.Stop();
            dialogAudioSource.volume = dialogVoiceVolume;
            dialogAudioSource.clip = clip;
            dialogAudioSource.Play();
            DraumLogger.Info(this, $"[QuestDialogModule] Quest clip played: {clip.name}");
        }

        private void ApplyHardPlayerLock(bool locked)
        {
            if (_playerController == null)
            {
                _playerController = FindFirstObjectByType<FirstPersonController>();
                if (_playerController == null) return;
            }

            _playerController.playerCanMove = !locked;
            _playerController.cameraCanMove = !locked;
            _playerController.lockCursor = !locked;
            DraumLogger.Info(this, $"[QuestDialogModule] Hard lock applied: {locked}");
        }

        private void SetCrosshairVisible(bool visible)
        {
            if (crosshairToToggle == null) return;
            if (crosshairToToggle.activeSelf == visible) return;
            crosshairToToggle.SetActive(visible);
            DraumLogger.Info(this, $"[QuestDialogModule] Crosshair {(visible ? "enabled" : "disabled")}.");
        }
    }
}
