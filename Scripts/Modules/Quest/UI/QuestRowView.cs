using TMPro;
using UnityEngine;

namespace DRAUM.Modules.Quest.UI
{
    public class QuestRowView : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI questGiverText;
        public TextMeshProUGUI rewardsText;
        public TextMeshProUGUI trackingText;
        public GameObject completedMark;
        public GameObject activeMark;

        public void Bind(
            string title,
            string description,
            string questGiver,
            string rewards,
            string trackingStatus,
            bool isActive,
            bool isCompleted)
        {
            if (titleText != null) titleText.text = title;
            if (descriptionText != null) descriptionText.text = description;
            if (questGiverText != null) questGiverText.text = questGiver;
            if (rewardsText != null) rewardsText.text = rewards;
            if (trackingText != null) trackingText.text = trackingStatus;
            if (activeMark != null) activeMark.SetActive(isActive);
            if (completedMark != null) completedMark.SetActive(isCompleted);
        }
    }
}
