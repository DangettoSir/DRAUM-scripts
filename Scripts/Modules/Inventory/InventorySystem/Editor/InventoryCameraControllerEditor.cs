using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(InventoryCameraController))]
public class InventoryCameraControllerEditor : Editor
{
    private SerializedProperty playerCamera;
    private SerializedProperty cameraPivot;
    private SerializedProperty backpackStateMachine;
    private SerializedProperty inputActions;
    
    private SerializedProperty enableFreeLook;
    private SerializedProperty freeLookSensitivity;
    
    private SerializedProperty sectionTransitionSpeed;
    private SerializedProperty gridFadeSpeed;
    private SerializedProperty fovTransitionSpeed;
    private SerializedProperty doubleClickTime;
    
    private SerializedProperty mainSectionPosition;
    private SerializedProperty mainSectionRotation;
    private SerializedProperty mainSectionFOV;
    private SerializedProperty customMainSectionFOV;
    private SerializedProperty useCustomMainFOV;
    private SerializedProperty mainSectionGrids;
    private SerializedProperty mainMaxLookAngleX;
    private SerializedProperty mainMaxLookAngleY;
    
    private SerializedProperty closedSectionPosition;
    private SerializedProperty closedSectionRotation;
    private SerializedProperty closedSectionFOV;
    private SerializedProperty customClosedSectionFOV;
    private SerializedProperty useCustomClosedFOV;
    private SerializedProperty closedSectionGrids;
    private SerializedProperty closedMaxLookAngleX;
    private SerializedProperty closedMaxLookAngleY;
    
    private SerializedProperty leftPocketPosition;
    private SerializedProperty leftPocketRotation;
    private SerializedProperty leftPocketFOV;
    private SerializedProperty customLeftPocketFOV;
    private SerializedProperty useCustomLeftFOV;
    private SerializedProperty leftPocketGrids;
    private SerializedProperty leftPocketGridOffset;
    private SerializedProperty leftMaxLookAngleX;
    private SerializedProperty leftMaxLookAngleY;
    
    private SerializedProperty rightPocketPosition;
    private SerializedProperty rightPocketRotation;
    private SerializedProperty rightPocketFOV;
    private SerializedProperty customRightPocketFOV;
    private SerializedProperty useCustomRightFOV;
    private SerializedProperty rightPocketGrids;
    private SerializedProperty rightPocketGridOffset;
    private SerializedProperty rightMaxLookAngleX;
    private SerializedProperty rightMaxLookAngleY;
    
    private SerializedProperty specialSectionPosition;
    private SerializedProperty specialSectionRotation;
    private SerializedProperty specialSectionFOV;
    private SerializedProperty customSpecialSectionFOV;
    private SerializedProperty useCustomSpecialFOV;
    private SerializedProperty specialSectionGrids;
    private SerializedProperty specialSectionGridOffset;
    private SerializedProperty specialMaxLookAngleX;
    private SerializedProperty specialMaxLookAngleY;
    private SerializedProperty specialVolumeProfile;
    
    private SerializedProperty volumeAnimator;
    private SerializedProperty blinkTriggerName;
    private SerializedProperty useEyeBlinkForSpecial;
    
    private SerializedProperty showDebugLogs;
    
    private bool showMainSection = true;
    private bool showClosedSection = true;
    private bool showLeftSection = true;
    private bool showRightSection = true;
    private bool showSpecialSection = true;
    
    private void OnEnable()
    {
        playerCamera = serializedObject.FindProperty("playerCamera");
        cameraPivot = serializedObject.FindProperty("cameraPivot");
        backpackStateMachine = serializedObject.FindProperty("backpackStateMachine");
        inputActions = serializedObject.FindProperty("inputActions");
        
        enableFreeLook = serializedObject.FindProperty("enableFreeLook");
        freeLookSensitivity = serializedObject.FindProperty("freeLookSensitivity");
        
        sectionTransitionSpeed = serializedObject.FindProperty("sectionTransitionSpeed");
        gridFadeSpeed = serializedObject.FindProperty("gridFadeSpeed");
        fovTransitionSpeed = serializedObject.FindProperty("fovTransitionSpeed");
        doubleClickTime = serializedObject.FindProperty("doubleClickTime");
        
        mainSectionPosition = serializedObject.FindProperty("mainSectionPosition");
        mainSectionRotation = serializedObject.FindProperty("mainSectionRotation");
        mainSectionFOV = serializedObject.FindProperty("mainSectionFOV");
        customMainSectionFOV = serializedObject.FindProperty("customMainSectionFOV");
        useCustomMainFOV = serializedObject.FindProperty("useCustomMainFOV");
        mainSectionGrids = serializedObject.FindProperty("mainSectionGrids");
        mainMaxLookAngleX = serializedObject.FindProperty("mainMaxLookAngleX");
        mainMaxLookAngleY = serializedObject.FindProperty("mainMaxLookAngleY");
        
        closedSectionPosition = serializedObject.FindProperty("closedSectionPosition");
        closedSectionRotation = serializedObject.FindProperty("closedSectionRotation");
        closedSectionFOV = serializedObject.FindProperty("closedSectionFOV");
        customClosedSectionFOV = serializedObject.FindProperty("customClosedSectionFOV");
        useCustomClosedFOV = serializedObject.FindProperty("useCustomClosedFOV");
        closedSectionGrids = serializedObject.FindProperty("closedSectionGrids");
        closedMaxLookAngleX = serializedObject.FindProperty("closedMaxLookAngleX");
        closedMaxLookAngleY = serializedObject.FindProperty("closedMaxLookAngleY");
        
        leftPocketPosition = serializedObject.FindProperty("leftPocketPosition");
        leftPocketRotation = serializedObject.FindProperty("leftPocketRotation");
        leftPocketFOV = serializedObject.FindProperty("leftPocketFOV");
        customLeftPocketFOV = serializedObject.FindProperty("customLeftPocketFOV");
        useCustomLeftFOV = serializedObject.FindProperty("useCustomLeftFOV");
        leftPocketGrids = serializedObject.FindProperty("leftPocketGrids");
        leftPocketGridOffset = serializedObject.FindProperty("leftPocketGridOffset");
        leftMaxLookAngleX = serializedObject.FindProperty("leftMaxLookAngleX");
        leftMaxLookAngleY = serializedObject.FindProperty("leftMaxLookAngleY");
        
        rightPocketPosition = serializedObject.FindProperty("rightPocketPosition");
        rightPocketRotation = serializedObject.FindProperty("rightPocketRotation");
        rightPocketFOV = serializedObject.FindProperty("rightPocketFOV");
        customRightPocketFOV = serializedObject.FindProperty("customRightPocketFOV");
        useCustomRightFOV = serializedObject.FindProperty("useCustomRightFOV");
        rightPocketGrids = serializedObject.FindProperty("rightPocketGrids");
        rightPocketGridOffset = serializedObject.FindProperty("rightPocketGridOffset");
        rightMaxLookAngleX = serializedObject.FindProperty("rightMaxLookAngleX");
        rightMaxLookAngleY = serializedObject.FindProperty("rightMaxLookAngleY");
        
        specialSectionPosition = serializedObject.FindProperty("specialSectionPosition");
        specialSectionRotation = serializedObject.FindProperty("specialSectionRotation");
        specialSectionFOV = serializedObject.FindProperty("specialSectionFOV");
        customSpecialSectionFOV = serializedObject.FindProperty("customSpecialSectionFOV");
        useCustomSpecialFOV = serializedObject.FindProperty("useCustomSpecialFOV");
        specialSectionGrids = serializedObject.FindProperty("specialSectionGrids");
        specialSectionGridOffset = serializedObject.FindProperty("specialSectionGridOffset");
        specialMaxLookAngleX = serializedObject.FindProperty("specialMaxLookAngleX");
        specialMaxLookAngleY = serializedObject.FindProperty("specialMaxLookAngleY");
        specialVolumeProfile = serializedObject.FindProperty("specialVolumeProfile");
        
        volumeAnimator = serializedObject.FindProperty("volumeAnimator");
        blinkTriggerName = serializedObject.FindProperty("blinkTriggerName");
        useEyeBlinkForSpecial = serializedObject.FindProperty("useEyeBlinkForSpecial");
        
        showDebugLogs = serializedObject.FindProperty("showDebugLogs");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Inventory Camera Controller", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Управляет камерой в инвентаре: позиция, поворот, Free Look (ALT), переключение секций (WASD).", MessageType.Info);
        EditorGUILayout.Space(5);
        
        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(playerCamera);
        EditorGUILayout.PropertyField(cameraPivot);
        EditorGUILayout.PropertyField(backpackStateMachine);
        EditorGUILayout.PropertyField(inputActions);
        EditorGUILayout.Space(10);
        
        EditorGUILayout.LabelField("Camera Look Settings (ALT)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enableFreeLook);
        EditorGUILayout.PropertyField(freeLookSensitivity);
        EditorGUILayout.Space(10);
        
        EditorGUILayout.LabelField("Section Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(sectionTransitionSpeed);
        EditorGUILayout.PropertyField(gridFadeSpeed);
        EditorGUILayout.PropertyField(fovTransitionSpeed);
        EditorGUILayout.PropertyField(doubleClickTime);
        
        
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox("Двойное нажатие A/D пропускает центр (Left ↔ Right напрямую). Grid'ы плавно появляются/исчезают через CanvasGroup.alpha.", MessageType.Info);
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Eye Blink Effect", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(useEyeBlinkForSpecial);
        EditorGUILayout.PropertyField(volumeAnimator);
        EditorGUILayout.PropertyField(blinkTriggerName);
        EditorGUILayout.HelpBox("Eye Blink Effect: использует анимацию Volume для плавного перехода в Special секцию. Volume Animator должен содержать триггер 'IsBlink' и Animation Events 'OnEyeBlinkClosed' и 'OnEyeBlinkOpened'.", MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        showMainSection = EditorGUILayout.Foldout(showMainSection, "Main Section (Center, Backpack Open)", true, EditorStyles.foldoutHeader);
        if (showMainSection)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(mainSectionPosition);
            EditorGUILayout.PropertyField(mainSectionRotation);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("FOV Settings", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(mainSectionFOV);
            EditorGUILayout.PropertyField(useCustomMainFOV);
            if (useCustomMainFOV.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(customMainSectionFOV);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(mainSectionGrids);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Free Look Limits (ALT)", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(mainMaxLookAngleX);
            EditorGUILayout.PropertyField(mainMaxLookAngleY);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space(5);
        
        showClosedSection = EditorGUILayout.Foldout(showClosedSection, "Closed Section (W - Backpack Closed)", true, EditorStyles.foldoutHeader);
        if (showClosedSection)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox("W закрывает рюкзак (Open blendshape = 0), но инвентарь остаётся открытым. Камера перемещается в эту позицию.", MessageType.Info);
            EditorGUILayout.PropertyField(closedSectionPosition);
            EditorGUILayout.PropertyField(closedSectionRotation);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("FOV Settings", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(closedSectionFOV);
            EditorGUILayout.PropertyField(useCustomClosedFOV);
            if (useCustomClosedFOV.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(customClosedSectionFOV);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(closedSectionGrids);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Free Look Limits (ALT)", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(closedMaxLookAngleX);
            EditorGUILayout.PropertyField(closedMaxLookAngleY);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space(5);
        
        showLeftSection = EditorGUILayout.Foldout(showLeftSection, "Left Pocket Section", true, EditorStyles.foldoutHeader);
        if (showLeftSection)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(leftPocketPosition);
            EditorGUILayout.PropertyField(leftPocketRotation);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("FOV Settings", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(leftPocketFOV);
            EditorGUILayout.PropertyField(useCustomLeftFOV);
            if (useCustomLeftFOV.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(customLeftPocketFOV);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(leftPocketGrids);
            EditorGUILayout.PropertyField(leftPocketGridOffset);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Free Look Limits (ALT)", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(leftMaxLookAngleX);
            EditorGUILayout.PropertyField(leftMaxLookAngleY);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space(5);
        
        showRightSection = EditorGUILayout.Foldout(showRightSection, "Right Pocket Section", true, EditorStyles.foldoutHeader);
        if (showRightSection)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(rightPocketPosition);
            EditorGUILayout.PropertyField(rightPocketRotation);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("FOV Settings", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(rightPocketFOV);
            EditorGUILayout.PropertyField(useCustomRightFOV);
            if (useCustomRightFOV.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(customRightPocketFOV);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(rightPocketGrids);
            EditorGUILayout.PropertyField(rightPocketGridOffset);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Free Look Limits (ALT)", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(rightMaxLookAngleX);
            EditorGUILayout.PropertyField(rightMaxLookAngleY);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space(5);
        
        showSpecialSection = EditorGUILayout.Foldout(showSpecialSection, "Special Section (S Button)", true, EditorStyles.foldoutHeader);
        if (showSpecialSection)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(specialSectionPosition);
            EditorGUILayout.PropertyField(specialSectionRotation);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("FOV Settings", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(specialSectionFOV);
            EditorGUILayout.PropertyField(useCustomSpecialFOV);
            if (useCustomSpecialFOV.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(customSpecialSectionFOV);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(specialSectionGrids);
            EditorGUILayout.PropertyField(specialSectionGridOffset);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Free Look Limits (ALT)", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(specialMaxLookAngleX);
            EditorGUILayout.PropertyField(specialMaxLookAngleY);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Visual Effects", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(specialVolumeProfile);
            EditorGUILayout.HelpBox("Volume Profile применяется когда игрок переходит в Special секцию (S). Например, можно добавить Vignette, Chromatic Aberration, или изменить цветокоррекцию.", MessageType.Info);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space(10);
        
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(showDebugLogs);
        
        if (Application.isPlaying)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Runtime Info", EditorStyles.boldLabel);
            
            InventoryCameraController controller = (InventoryCameraController)target;
            
            EditorGUILayout.LabelField($"Current Section: {controller.currentSection}");
            EditorGUILayout.LabelField($"Target Section: {controller.targetSection}");
            
            if (controller.playerCamera != null)
            {
                EditorGUILayout.LabelField($"Camera Position: {controller.playerCamera.transform.localPosition}");
                EditorGUILayout.LabelField($"Camera Rotation: {controller.playerCamera.transform.localEulerAngles}");
                EditorGUILayout.LabelField($"Camera FOV: {controller.playerCamera.fieldOfView:F1}°");
            }
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
