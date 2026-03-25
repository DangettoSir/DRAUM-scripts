using System;
using UnityEngine;

namespace DRAUM.Core.Infrastructure.Logger
{
    /// <summary>
    /// Точка входа для логов из вашего кода.
    /// </summary>
    public static class DraumLogger
    {
        public static LoggerModule Module { get; internal set; }

        public static void Info(object owner, object message, string details = null)
        {
            string msg = message != null ? message.ToString() : string.Empty;
            var line = Module?.LogFromCode(owner, LogLevel.Info, msg, details);
            var ctx = owner as UnityEngine.Object;
            if (Module == null || Module.IsConsoleEnabledForScript(owner))
            {
                if (ctx != null) Debug.Log(line ?? msg, ctx);
                else Debug.Log(line ?? msg);
            }
        }

        public static void Warning(object owner, object message, string details = null)
        {
            string msg = message != null ? message.ToString() : string.Empty;
            var line = Module?.LogFromCode(owner, LogLevel.Warning, msg, details);
            var ctx = owner as UnityEngine.Object;
            if (Module == null || Module.IsConsoleEnabledForScript(owner))
            {
                if (ctx != null) Debug.LogWarning(line ?? msg, ctx);
                else Debug.LogWarning(line ?? msg);
            }
        }

        public static void Error(object owner, object message, string details = null)
        {
            string msg = message != null ? message.ToString() : string.Empty;
            var line = Module?.LogFromCode(owner, LogLevel.Error, msg, details);
            var ctx = owner as UnityEngine.Object;
            if (Module == null || Module.IsConsoleEnabledForScript(owner))
            {
                if (ctx != null) Debug.LogError(line ?? msg, ctx);
                else Debug.LogError(line ?? msg);
            }
        }

        public static void Exception(object owner, object message, Exception ex)
        {
            string msg = message != null ? message.ToString() : string.Empty;
            string details = ex != null ? ex.ToString() : null;
            var line = Module?.LogFromCode(owner, LogLevel.Exception, msg, details);
            var ctx = owner as UnityEngine.Object;
            if (Module == null || Module.IsConsoleEnabledForScript(owner))
            {
                if (ex != null)
                {
                    if (ctx != null) Debug.LogException(ex, ctx);
                    else Debug.LogException(ex);
                }
                else
                {
                    if (ctx != null) Debug.LogError(line ?? msg, ctx);
                    else Debug.LogError(line ?? msg);
                }
            }
        }
    }
}

