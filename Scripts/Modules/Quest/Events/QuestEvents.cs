using DRAUM.Core;
using DRAUM.Modules.Quest.Data;
using DRAUM.Modules.Quest.Runtime;

namespace DRAUM.Modules.Quest.Events
{
    public class QuestAcceptRequestEvent : IEvent
    {
        public string QuestId { get; set; }
    }

    public class QuestDeclineRequestEvent : IEvent
    {
        public string QuestId { get; set; }
    }

    public class QuestCompleteRequestEvent : IEvent
    {
        public string QuestId { get; set; }
    }

    public class QuestStateChangedEvent : IEvent
    {
        public string QuestId { get; set; }
        public QuestProgressState NewState { get; set; }
    }

    public class QuestDialogOpenRequestEvent : IEvent
    {
        public string NpcId { get; set; }
        public QuestDefinition Quest { get; set; }
    }

    public class QuestDialogCloseRequestEvent : IEvent
    {
        public string NpcId { get; set; }
    }
}
