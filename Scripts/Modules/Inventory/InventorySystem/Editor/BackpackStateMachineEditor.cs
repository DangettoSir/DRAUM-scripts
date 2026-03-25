using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BackpackStateMachine))]
public class BackpackStateMachineEditor : Editor
{
    private SerializedProperty backpackAnimator;
    private SerializedProperty backpackMesh;
    private SerializedProperty inventoryCanvas;
    
    private SerializedProperty openBlendshapeName;
    private SerializedProperty leftPocketBlendshapeName;
    private SerializedProperty rightPocketBlendshapeName;
    
    private SerializedProperty blendshapeSpeed;
    
    private SerializedProperty blendshapeStartDelay;
    private SerializedProperty blendshapeCloseSpeed;
    private SerializedProperty blendshapeOpenSpeed;
    private SerializedProperty inventoryCameraController;
    
    
    private SerializedProperty canvasShowThreshold;
    
    private SerializedProperty mainSectionOpenValue;
    private SerializedProperty leftPocketOpenValue;
    private SerializedProperty leftPocketValue;
    private SerializedProperty rightPocketOpenValue;
    private SerializedProperty rightPocketValue;
    
    private SerializedProperty showDebugLogs;
    
    private void OnEnable()
    {
        backpackAnimator = serializedObject.FindProperty("backpackAnimator");
        backpackMesh = serializedObject.FindProperty("backpackMesh");
        inventoryCanvas = serializedObject.FindProperty("inventoryCanvas");
        
        openBlendshapeName = serializedObject.FindProperty("openBlendshapeName");
        leftPocketBlendshapeName = serializedObject.FindProperty("leftPocketBlendshapeName");
        rightPocketBlendshapeName = serializedObject.FindProperty("rightPocketBlendshapeName");
        
        blendshapeSpeed = serializedObject.FindProperty("blendshapeSpeed");
        
        blendshapeStartDelay = serializedObject.FindProperty("blendshapeStartDelay");
        blendshapeCloseSpeed = serializedObject.FindProperty("blendshapeCloseSpeed");
        blendshapeOpenSpeed = serializedObject.FindProperty("blendshapeOpenSpeed");
        inventoryCameraController = serializedObject.FindProperty("inventoryCameraController");
        
        
        canvasShowThreshold = serializedObject.FindProperty("canvasShowThreshold");
        
        mainSectionOpenValue = serializedObject.FindProperty("mainSectionOpenValue");
        leftPocketOpenValue = serializedObject.FindProperty("leftPocketOpenValue");
        leftPocketValue = serializedObject.FindProperty("leftPocketValue");
        rightPocketOpenValue = serializedObject.FindProperty("rightPocketOpenValue");
        rightPocketValue = serializedObject.FindProperty("rightPocketValue");
        
        showDebugLogs = serializedObject.FindProperty("showDebugLogs");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Backpack State Machine", EditorStyles.boldLabel);
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Basic Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(backpackAnimator);
        EditorGUILayout.PropertyField(backpackMesh);
        EditorGUILayout.PropertyField(inventoryCanvas);
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Blendshape Names", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(openBlendshapeName);
        EditorGUILayout.PropertyField(leftPocketBlendshapeName);
        EditorGUILayout.PropertyField(rightPocketBlendshapeName);
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Blendshape Animation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(blendshapeSpeed);
        EditorGUILayout.PropertyField(canvasShowThreshold);
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Section Transition Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(blendshapeStartDelay);
        EditorGUILayout.PropertyField(blendshapeCloseSpeed);
        EditorGUILayout.PropertyField(blendshapeOpenSpeed);
        EditorGUILayout.PropertyField(inventoryCameraController);
        EditorGUILayout.HelpBox("Blendshape Start Delay: задержка перед началом blendshape'ов после поворота камеры. Blendshape Close Speed: скорость закрытия всех blendshape'ов до 0. Blendshape Open Speed: скорость открытия нужных blendshape'ов до целевых значений.", MessageType.Info);
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Section Blendshape Values", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(mainSectionOpenValue);
        EditorGUILayout.PropertyField(leftPocketOpenValue);
        EditorGUILayout.PropertyField(leftPocketValue);
        EditorGUILayout.PropertyField(rightPocketOpenValue);
        EditorGUILayout.PropertyField(rightPocketValue);
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(showDebugLogs);
        
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("Двухфазный переход: сначала все blendshape'ы закрываются до 0, потом нужные открываются до целевых значений. Grid'ы появляются только при высоких значениях blendshape'ов.", MessageType.Info);
        
        serializedObject.ApplyModifiedProperties();
    }
}