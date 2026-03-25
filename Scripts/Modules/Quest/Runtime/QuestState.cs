namespace DRAUM.Modules.Quest.Runtime
{
    public enum QuestProgressState
    {
        NotStarted = 0,
        Active = 1,
        Completed = 2,
        Declined = 3
    }

    [System.Serializable]
    public class QuestState
    {
        public string questId;
        public QuestProgressState progressState;
        public bool initialDialogCompleted;
    }
}
