#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ZombieAnimationVariation))]
public class ZombieAnimationVariationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        
        ZombieAnimationVariation variation = (ZombieAnimationVariation)target;
        
        if (GUILayout.Button("Randomize Animation", GUILayout.Height(30)))
        {
            variation.ForceRandomize();
        }
    }
}
#endif


