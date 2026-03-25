using UnityEditor;
using UnityEngine;
using DRAUM.Core.Infrastructure.Logger;

[CustomEditor(typeof(LogSettings))]
public class LogSettingsEditor : Editor
{
    private string _search = "";
    private Vector2 _scroll;

    public override void OnInspectorGUI()
    {
        var settings = (LogSettings)target;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "LogSettings управляет выводом в Unity Console по имени класса-источника логов. " +
            "Файлы пишутся всегда. Кнопка Auto Discover добавляет в список классы, которые используют DraumLogger.",
            MessageType.Info);

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Auto Discover Loggable Scripts", GUILayout.Height(28)))
        {
            settings.AutoDiscoverLoggableScripts();
            EditorUtility.SetDirty(settings);
        }
        if (GUILayout.Button("Clear", GUILayout.Height(28)))
        {
            settings.scriptToggles?.Clear();
            settings.enabledLogs?.Clear();
            settings.SyncDictionaries();
            EditorUtility.SetDirty(settings);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        _search = EditorGUILayout.TextField("Search", _search);

        var list = settings.scriptToggles;
        if (list == null) list = new System.Collections.Generic.List<ScriptLogToggle>();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        for (int i = 0; i < list.Count; i++)
        {
            var t = list[i];
            if (t == null) continue;

            if (!string.IsNullOrEmpty(_search) && (t.scriptName == null || !t.scriptName.ToLowerInvariant().Contains(_search.ToLowerInvariant())))
                continue;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(t.scriptName ?? "(null)", GUILayout.MinWidth(200));
            t.enabled = EditorGUILayout.Toggle(t.enabled, GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();

            list[i] = t;
        }
        EditorGUILayout.EndScrollView();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(settings);
            settings.SyncDictionaries();
        }
    }
}

