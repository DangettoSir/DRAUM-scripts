using System;

namespace DRAUM.Core.Infrastructure.Logger
{
    /// <summary>
    /// Метаданные одного события лога.
    /// </summary>
    public readonly struct LogEvent
    {
        public readonly DateTime TimestampUtc;
        public readonly LogLevel Level;
        public readonly int PriorityIndex;
        public readonly string Category;
        public readonly string ModuleKey;
        public readonly string Message;
        public readonly string Details;

        public LogEvent(DateTime timestampUtc, LogLevel level, int priorityIndex, string category, string moduleKey, string message, string details)
        {
            TimestampUtc = timestampUtc;
            Level = level;
            PriorityIndex = priorityIndex;
            Category = category ?? string.Empty;
            ModuleKey = moduleKey ?? string.Empty;
            Message = message ?? string.Empty;
            Details = details ?? string.Empty;
        }
    }
}

