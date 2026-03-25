#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ZombieBlendShapeRandomizer))]
public class ZombieBlendShapeRandomizerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        
        ZombieBlendShapeRandomizer randomizer = (ZombieBlendShapeRandomizer)target;
        
        if (GUILayout.Button("Go Random", GUILayout.Height(30)))
        {
            randomizer.GoRandom();
        }
    }
}
#endif


