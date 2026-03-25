using System;
using System.Text;

namespace DRAUM.Core.Infrastructure.Logger
{
    /// <summary>
    /// Форматирование строк для логов в файлы/консоль.
    /// Тело сообщения может быть любым текстом, но шаблон строки фиксирован.
    /// </summary>
    public static class LogFormat
    {
        /// <summary>
        /// Формирует одну строку лога по структуре LogEvent.
        /// </summary>
        public static string FormatLine(in LogEvent evt)
        {
            var sb = new StringBuilder(256);
            sb.Append(evt.TimestampUtc.ToString("yyyy-MM-dd HH:mm:ss.fff'Z'"));
            sb.Append(" [").Append(evt.PriorityIndex).Append("] - [").Append(evt.Category).Append("] ");

            sb.Append(evt.Level switch
            {
                LogLevel.Info => "Info",
                LogLevel.Warning => "Warning",
                LogLevel.Error => "Error",
                LogLevel.Exception => "Exception",
                _ => "Info"
            });
            sb.Append(": ");
            sb.Append(evt.Message);

            if (!string.IsNullOrEmpty(evt.Details))
            {
                sb.Append(" | ");
                sb.Append(evt.Details);
            }

            return sb.ToString();
        }
    }
}

