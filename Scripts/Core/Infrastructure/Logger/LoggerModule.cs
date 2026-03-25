using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using UnityEngine;
using DRAUM.Core;
using DRAUM.Core.Infrastructure.Logger;
using UDebug = UnityEngine.Debug;


namespace DRAUM.Core.Infrastructure.Logger
{
    /// <summary>
    /// Модуль системного логирования: пишет логи в файлы и разносит их по модулям.
    /// </summary>
    public sealed class LoggerModule : DRAUM.Core.SystemModuleBase
    {
        public override int InitializationPriority => -100;

        private const string RunRootFolderName = "Logs";
        private string _runFolderPath;
        private FileLogSink _sink;
        private LogSettings _logSettings;
        private ILogHandler _originalUnityLogHandler;
        private bool _registeredForConsoleFilter;
        private static int _consoleFilterRefCount;

        private readonly Dictionary<string, (int priorityIndex, string fileName)> _moduleKeyToRoute = new Dictionary<string, (int, string)>(16);
        private int _nextDynamicPriorityIndex = 50;

        private void Awake()
        {
            EnsureRunFolderAndSink();

            Application.logMessageReceived += HandleUnityLogMessage;

            DraumLogger.Module = this;

            _logSettings = GetComponent<LogSettings>() ?? FindFirstObjectByType<LogSettings>();
            _logSettings?.SyncDictionaries();

            TryInstallConsoleFilter();
        }

        private void TryInstallConsoleFilter()
        {  
            if (_consoleFilterRefCount <= 0)
            {
                _originalUnityLogHandler = UDebug.unityLogger.logHandler;
                UDebug.unityLogger.logHandler = new FilteredUnityLogHandler(_originalUnityLogHandler, this);
            }

            _consoleFilterRefCount++;
            _registeredForConsoleFilter = true;
        }

        private sealed class FilteredUnityLogHandler : ILogHandler
        {
            private readonly ILogHandler _inner;
            private readonly LoggerModule _module;

            public FilteredUnityLogHandler(ILogHandler inner, LoggerModule module)
            {
                _inner = inner;
                _module = module;
            }

            public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
            {
                if (context == null) return;
                if (!_module.IsConsoleEnabledForScript(context)) return;
                _inner.LogFormat(logType, context, format, args);
            }

            public void LogException(Exception exception, UnityEngine.Object context)
            {
                if (context == null) return;
                if (!_module.IsConsoleEnabledForScript(context)) return;
                _inner.LogException(exception, context);
            }
        }

        protected override void OnInitialize()
        {
            BuildModulePriorityAndRoutingMap();
            _nextDynamicPriorityIndex = GetMaxKnownPriorityIndex() + 1;
        }

        private int GetMaxKnownPriorityIndex()
        {
            int max = 1;
            foreach (var route in _moduleKeyToRoute.Values)
                max = Mathf.Max(max, route.priorityIndex);
            return max;
        }

        private void EnsureRunFolderAndSink()
        {
            if (_sink != null) return;

            string runStamp = DateTime.Now.ToString("dd_MM_yyyy-HH_mm_ss_fff");
            runStamp = runStamp.Replace('.', '_');
            string rootPath;
            if (Application.isEditor)
            {
                rootPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            }
            else
            {
                rootPath = Application.persistentDataPath;
            }

            string logsRoot = Path.Combine(rootPath, RunRootFolderName);
            _runFolderPath = Path.Combine(logsRoot, runStamp);

            _sink = new FileLogSink(_runFolderPath);

            var evt = new LogEvent(DateTime.UtcNow, LogLevel.Info, 1, "Game", "Core Logger", "LoggerModule run started", null);
            _sink.WriteAll(evt);
        }

        private void BuildModulePriorityAndRoutingMap()
        {
            try
            {
                if (Game == null)
                    return;

                var modulesField = typeof(Game).GetField("_modules", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                if (modulesField == null)
                    return;

                var modulesObj = modulesField.GetValue(Game) as IDictionary<string, IGameModule>;
                if (modulesObj == null)
                    return;

                var ordered = new List<IGameModule>(modulesObj.Values);
                ordered.Sort((a, b) => a.InitializationPriority.CompareTo(b.InitializationPriority));

                _moduleKeyToRoute.Clear();
                _moduleKeyToRoute["Game"] = (1, "Game.log");

                for (int i = 0; i < ordered.Count; i++)
                {
                    var m = ordered[i];
                    if (m == null) continue;

                    string moduleKey = m.ModuleName;
                    string fileName = moduleKey + "Module.log";
                    int rank = i + 2;

                    _moduleKeyToRoute[moduleKey] = (rank, fileName);
                }
            }
            catch (Exception)
            {
            }
        }

        private void HandleUnityLogMessage(string condition, string stackTrace, LogType logType)
        {
            if (!string.IsNullOrEmpty(stackTrace))
            {
                var normalized = stackTrace.Replace('\\', '/');
                if (normalized.IndexOf("DraumLogger.cs", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    normalized.IndexOf("DraumLogger", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return;
                }
            }

            string message = condition ?? string.Empty;
            string details = null;
            if (logType == LogType.Error || logType == LogType.Exception || logType == LogType.Assert)
                details = ExtractShortDetails(stackTrace);

            var (level, _, moduleKey) = InferFromUnityLog(logType, stackTrace);
            if (string.IsNullOrEmpty(moduleKey))
                moduleKey = "Game";

            EnsureRouteForModuleKey(moduleKey);

            var route = _moduleKeyToRoute[moduleKey];
            int priority = route.priorityIndex;
            string fileName = route.fileName;

            string category = fileName;
            if (category.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                category = category.Substring(0, category.Length - 4);

            var evt = new LogEvent(DateTime.UtcNow, level, priority, category, moduleKey, message, details);
            _sink.Write(in evt, fileName);
            _sink.WriteAll(in evt);
        }

        private (LogLevel level, string category, string moduleKey) InferFromUnityLog(LogType logType, string stackTrace)
        {
            LogLevel level = LogLevel.Info;
            switch (logType)
            {
                case LogType.Warning:
                    level = LogLevel.Warning;
                    break;
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    level = (logType == LogType.Exception) ? LogLevel.Exception : LogLevel.Error;
                    break;
                default:
                    level = LogLevel.Info;
                    break;
            }

            string category = "Unknown";
            string moduleKey = "Game";
            if (!string.IsNullOrEmpty(stackTrace))
            {
                var normalized = stackTrace.Replace('\\', '/');
                int atPos = normalized.LastIndexOf(" (at ", StringComparison.OrdinalIgnoreCase);
                if (atPos >= 0)
                {
                    var beforeAt = normalized.Substring(0, atPos).TrimEnd();
                    int lineStart = beforeAt.LastIndexOf('\n');
                    if (lineStart >= 0)
                        beforeAt = beforeAt.Substring(lineStart + 1);
                    beforeAt = beforeAt.Trim();

                    int colonIdx = beforeAt.IndexOf(':');
                    if (colonIdx > 0)
                        category = beforeAt.Substring(0, colonIdx).Trim();
                }
                else
                {
                    var colonIdx = stackTrace.IndexOf(':');
                    if (colonIdx > 0)
                        category = stackTrace.Substring(0, colonIdx).Trim();
                }
                const string marker = "/Scripts/Modules/";
                int markerIdx = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (markerIdx >= 0)
                {
                    int start = markerIdx + marker.Length;
                    var rest = normalized.Substring(start);
                    int slash = rest.IndexOf('/');
                    if (slash > 0)
                    {
                        moduleKey = rest.Substring(0, slash);
                    }
                }
            }

            if (string.Equals(moduleKey, "Core", StringComparison.OrdinalIgnoreCase))
            {
                var normalized = stackTrace.Replace('\\', '/');
                if (normalized.IndexOf("/Scripts/Core/Game/", StringComparison.OrdinalIgnoreCase) >= 0)
                    moduleKey = "Game";
            }

            return (level, category, moduleKey);
        }

        private void EnsureRouteForModuleKey(string moduleKey)
        {
            if (string.IsNullOrEmpty(moduleKey))
                moduleKey = "Game";

            if (_moduleKeyToRoute.ContainsKey(moduleKey))
                return;

            int priority = _nextDynamicPriorityIndex++;
            string fileName = moduleKey + "Module.log";
            _moduleKeyToRoute[moduleKey] = (priority, fileName);
        }

        private string ExtractShortDetails(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
                return null;

            var normalized = stackTrace.Replace('\\', '/');
            int atPos = normalized.LastIndexOf(" (at ", StringComparison.OrdinalIgnoreCase);
            if (atPos >= 0)
                return normalized.Substring(atPos + 4).Trim();
            return null;
        }

        internal string LogFromCode(object owner, LogLevel level, string message, string details)
        {
            if (owner == null) owner = this;
            if (_sink == null) EnsureRunFolderAndSink();

            var type = owner as Type ?? owner.GetType();

            string moduleKey = InferModuleKeyFromType(type);

            int priority = 0;
            string fileName = "Core.log";
            string category = type.Name;
            EnsureRouteForModuleKey(moduleKey);
            if (_moduleKeyToRoute.TryGetValue(moduleKey, out var route))
            {
                priority = route.priorityIndex;
                fileName = route.fileName;
                category = fileName;
            }

            var evt = new LogEvent(DateTime.UtcNow, level, priority, category, moduleKey, message, details);
            var line = LogFormat.FormatLine(in evt);
            _sink.Write(in evt, fileName);
            _sink.WriteAll(in evt);

            return line;
        }

        internal bool IsConsoleEnabledForScript(object owner)
        {
            if (_logSettings == null)
                return false;

            var type = owner as Type ?? owner?.GetType();
            if (type == null)
                return false;

            return _logSettings.IsEnabled(type);
        }

        private string InferModuleKeyFromType(Type type)
        {
            if (type == null) return "Core";

            if (type == typeof(DRAUM.Core.Game) || type.IsSubclassOf(typeof(DRAUM.Core.Game)))
                return "Game";

            var ns = type.Namespace ?? string.Empty;
            var parts = ns.Split('.');

            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == "Modules" && i + 1 < parts.Length)
                    return parts[i + 1];
            }

            if (type.Name.EndsWith("Module", StringComparison.Ordinal))
                return type.Name.Substring(0, type.Name.Length - "Module".Length);

            // Важный fallback:
            // многие ваши скрипты (например FPMAxe/FirstPersonController) лежат в Assets/_Project/Scripts/Modules/...
            // но НЕ имеют namespace -> type.Namespace пустой -> сейчас они уходят в CoreModule.log.
            // Поэтому, если namespace не помог — пытаемся распарсить путь из stack trace.
            var fromStack = InferModuleKeyFromCallStack();
            return string.IsNullOrEmpty(fromStack) ? "Core" : fromStack;
        }

        private string InferModuleKeyFromCallStack()
        {
            try
            {
                // Стек иногда содержит file path/line. Нам нужен кусок вида:
                // .../Assets/_Project/Scripts/Modules/<ModuleName>/...
                var st = new StackTrace(2, true);
                var stack = st.ToString().Replace('\\', '/');

                const string marker = "/Scripts/Modules/";
                int markerIdx = stack.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (markerIdx < 0)
                    return null;

                int start = markerIdx + marker.Length;
                var rest = stack.Substring(start);
                int slash = rest.IndexOf('/');
                if (slash <= 0)
                    return null;

                return rest.Substring(0, slash);
            }
            catch
            {
                return null;
            }
        }

        protected override void OnShutdown()
        {
            Application.logMessageReceived -= HandleUnityLogMessage;
            DraumLogger.Module = null;
            _sink?.Dispose();
            _sink = null;

            if (_registeredForConsoleFilter)
            {
                _consoleFilterRefCount = Mathf.Max(0, _consoleFilterRefCount - 1);


                if (_consoleFilterRefCount == 0 && _originalUnityLogHandler != null)
                    UDebug.unityLogger.logHandler = _originalUnityLogHandler;
            }

            _registeredForConsoleFilter = false;
            _originalUnityLogHandler = null;
        }
    }
}

