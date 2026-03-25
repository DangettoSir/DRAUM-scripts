using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EyeBlinkBridge))]
public class EyeBlinkBridgeEditor : Editor
{
    private SerializedProperty inventoryCameraController;
    private SerializedProperty showDebugLogs;
    
    private void OnEnable()
    {
        inventoryCameraController = serializedObject.FindProperty("inventoryCameraController");
        showDebugLogs = serializedObject.FindProperty("showDebugLogs");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Eye Blink Bridge", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Этот компонент должен быть на том же GameObject что и Volume Animator. Он перенаправляет Animation Events в InventoryCameraController.", MessageType.Info);
        
        EditorGUILayout.Space(5);
        EditorGUILayout.PropertyField(inventoryCameraController);
        EditorGUILayout.PropertyField(showDebugLogs);
        
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("Animation Events в анимации EyeBlink должны вызывать:\n• OnEyeBlinkClosed() - когда глаз полностью закрыт\n• OnEyeBlinkOpened() - когда глаз полностью открыт\n• TestAnimationEvent() - для тестирования", MessageType.Info);
        
        serializedObject.ApplyModifiedProperties();
    }
}