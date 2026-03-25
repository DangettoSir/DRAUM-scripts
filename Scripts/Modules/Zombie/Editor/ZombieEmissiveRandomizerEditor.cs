#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ZombieEmissiveRandomizer))]
public class ZombieEmissiveRandomizerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        
        ZombieEmissiveRandomizer randomizer = (ZombieEmissiveRandomizer)target;
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Randomize Emissive", GUILayout.Height(30)))
        {
            randomizer.RandomizeEmissive();
        }
        
        if (GUILayout.Button("Reset", GUILayout.Height(30), GUILayout.Width(80)))
        {
            randomizer.ResetEmissive();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        Color currentColor = randomizer.GetCurrentEmissiveColor();
        EditorGUILayout.ColorField("Current Emissive", currentColor);
    }
}
#endif
