using System;
using System.Collections.Generic;
using UnityEngine;

namespace DRAUM.Modules.Quest.Data
{
    [CreateAssetMenu(fileName = "QuestDefinition", menuName = "DRAUM/Quest/Quest Definition")]
    public class QuestDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string questId;
        public string questName;
        public string questGiverId;
        [TextArea(2, 6)] public string questDescription;

        [Header("Dialog")]
        [TextArea(2, 8)] public List<string> initialDialog = new();
        public List<AudioClip> initialDialogClips = new();
        [TextArea(2, 6)] public string acceptOption = "[Accept]";
        [TextArea(2, 6)] public string acceptAnswer;
        public AudioClip acceptAnswerClip;
        [TextArea(2, 6)] public string declineOption = "[Decline]";
        [TextArea(2, 6)] public string declineAnswer;
        public AudioClip declineAnswerClip;
        [TextArea(2, 6)] public string comebackAfterDecline;
        [TextArea(2, 6)] public string comebackInProgress;
        [TextArea(2, 6)] public string comebackCompleted;
        [TextArea(2, 6)] public string finalWords;

        [Header("Rewards")]
        public int coinReward;
        public ItemData rewardItem1;
        public ItemData rewardItem2;

        [Header("Requirements")]
        public List<QuestItemRequirement> itemRequirements = new();
    }

    [Serializable]
    public class QuestItemRequirement
    {
        public ItemData itemData;
        public int amount = 1;
    }
}
