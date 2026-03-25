using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DRAUM.Core.Infrastructure.Logger
{
    [Serializable]
    /// <summary>
    /// Тоггл, который включает/выключает вывод в Unity Console для конкретного класса-источника логов.
    /// </summary>
    public class ScriptLogToggle
    {
        public string scriptName;
        public bool enabled;
    }

    /// <summary>
    /// Настройки, какие классы-источники печатают логи в Unity Console (по имени класса, см. scriptToggles).
    /// Логи через DraumLogger всегда пишутся в файлы `Logs/timestamp/` (и all.log) через LoggerModule;
    /// тогглы здесь только отключают шум в Console, не отключая запись в файл.
    /// </summary>
    public class LogSettings : MonoBehaviour
    {
        public List<ScriptLogToggle> scriptToggles = new List<ScriptLogToggle>();

        public Dictionary<Type, bool> enabledLogs = new Dictionary<Type, bool>();

        private Dictionary<string, bool> _lookup;

        private void Awake()
        {
            if (scriptToggles == null || scriptToggles.Count == 0)
                AutoDiscoverLoggableScripts_EditorOnly();
            BuildLookup();
            BuildTypeDictionary();
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (scriptToggles == null || scriptToggles.Count == 0)
                AutoDiscoverLoggableScripts_EditorOnly();
            BuildLookup();
            BuildTypeDictionary();
        }
        #endif

        [ContextMenu("Auto Discover Loggable Scripts")]
        public void AutoDiscoverLoggableScripts()
        {
            AutoDiscoverLoggableScripts_EditorOnly();
            BuildLookup();
            BuildTypeDictionary();
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            foreach (var s in scriptToggles)
            {
                if (string.IsNullOrEmpty(s.scriptName))
                    continue;
                _lookup[s.scriptName] = s.enabled;
            }
        }

        public void SyncDictionaries()
        {
            BuildLookup();
            BuildTypeDictionary();
        }

        public bool IsEnabled(string scriptName)
        {
            if (string.IsNullOrEmpty(scriptName))
                return false;

            return _lookup != null && _lookup.TryGetValue(scriptName, out var enabled) && enabled;
        }

        public bool IsEnabled(Type scriptType)
        {
            if (scriptType == null) return false;
            if (enabledLogs == null || enabledLogs.Count == 0) return false;
            return enabledLogs.TryGetValue(scriptType, out var enabled) && enabled;
        }

        private void BuildTypeDictionary()
        {
            enabledLogs = new Dictionary<Type, bool>();
            var typeByName = BuildTypeNameLookup();

            foreach (var s in scriptToggles)
            {
                if (s == null) continue;
                if (string.IsNullOrEmpty(s.scriptName)) continue;

                if (!typeByName.TryGetValue(s.scriptName, out var t)) continue;
                enabledLogs[t] = s.enabled;
            }
        }

        private Dictionary<string, Type> BuildTypeNameLookup()
        {
            var map = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var asm in assemblies)
            {
                Type[] typesInAsm;
                try { typesInAsm = asm.GetTypes(); }
                catch { continue; }

                foreach (var t in typesInAsm)
                {
                    if (t == null) continue;
                    if (string.IsNullOrEmpty(t.Name)) continue;
                    if (!map.ContainsKey(t.Name))
                        map[t.Name] = t;
                }
            }

            return map;
        }

        private void AutoDiscoverLoggableScripts_EditorOnly()
        {
        #if UNITY_EDITOR
            var prevEnabled = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (scriptToggles != null)
            {
                foreach (var s in scriptToggles)
                {
                    if (s == null) continue;
                    if (string.IsNullOrEmpty(s.scriptName)) continue;
                    prevEnabled[s.scriptName] = s.enabled;
                }
            }

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var scriptsRoot = Path.Combine(projectRoot, "Assets", "_Project", "Scripts");
            if (!Directory.Exists(scriptsRoot))
                return;

            var csFiles = Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories);
            var foundNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var classRegex = new Regex(@"\bclass\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);

            foreach (var file in csFiles)
            {
                string text;
                try { text = File.ReadAllText(file); }
                catch { continue; }

                if (string.IsNullOrEmpty(text)) continue;
                if (!text.Contains("DraumLogger.")) continue;

                var classMatches = classRegex.Matches(text);
                if (classMatches == null || classMatches.Count == 0) continue;

                var classStarts = new List<(string name, int startIndex)>(classMatches.Count);
                foreach (Match m in classMatches)
                {
                    if (!m.Success) continue;
                    var className = m.Groups[1].Value;
                    if (string.IsNullOrEmpty(className)) continue;
                    classStarts.Add((className, m.Index));
                }

                classStarts.Sort((a, b) => a.startIndex.CompareTo(b.startIndex));
                for (int i = 0; i < classStarts.Count; i++)
                {
                    var cur = classStarts[i];
                    int end = (i + 1 < classStarts.Count) ? classStarts[i + 1].startIndex : text.Length;
                    if (end <= cur.startIndex) continue;

                    int idx = text.IndexOf("DraumLogger.", cur.startIndex, end - cur.startIndex, StringComparison.Ordinal);
                    if (idx >= 0)
                        foundNames.Add(cur.name);
                }
            }

            var newList = new List<ScriptLogToggle>();
            foreach (var name in foundNames.OrderBy(x => x))
            {
                prevEnabled.TryGetValue(name, out var enabled);
                newList.Add(new ScriptLogToggle { scriptName = name, enabled = enabled });
            }

            scriptToggles = newList;
        #endif
        }
    }
}

