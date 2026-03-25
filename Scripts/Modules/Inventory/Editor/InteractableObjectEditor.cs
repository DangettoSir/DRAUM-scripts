#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(InteractableObject))]
public class InteractableObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        InteractableObject interactable = (InteractableObject)target;

        serializedObject.Update();

        EditorGUILayout.LabelField("Interaction Settings", EditorStyles.boldLabel);
        interactable.interactionType = (InteractionType)EditorGUILayout.EnumPopup("Interaction Type", interactable.interactionType);
        interactable.interactionName = EditorGUILayout.TextField("Interaction Name", interactable.interactionName);

        EditorGUILayout.Space();

        switch (interactable.interactionType)
        {
            case InteractionType.Pickable:
                ShowPickableSettings(interactable);
                break;

            case InteractionType.Button:
                ShowButtonSettings(interactable);
                break;

            case InteractionType.Lever:
                ShowLeverSettings(interactable);
                break;

            case InteractionType.Door:
                ShowDoorSettings(interactable);
                break;

            case InteractionType.Container:
                ShowContainerSettings(interactable);
                break;

            case InteractionType.Custom:
                ShowCustomSettings(interactable);
                break;
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(interactable);
            serializedObject.ApplyModifiedProperties();
        }
    }

    private void ShowPickableSettings(InteractableObject obj)
    {
        EditorGUILayout.LabelField("Pickable Settings", EditorStyles.boldLabel);
        
        obj.itemData = (ItemData)EditorGUILayout.ObjectField("Item Data", obj.itemData, typeof(ItemData), false);
        obj.inventorySystem = (Inventory)EditorGUILayout.ObjectField("Inventory System", obj.inventorySystem, typeof(Inventory), true);
        
        EditorGUILayout.HelpBox("Leave Inventory System empty to auto-find in scene", MessageType.Info);
    }

    private void ShowButtonSettings(InteractableObject obj)
    {
        EditorGUILayout.LabelField("Button Settings", EditorStyles.boldLabel);
        
        obj.canPressMultipleTimes = EditorGUILayout.Toggle("Can Press Multiple Times", obj.canPressMultipleTimes);
        obj.buttonCooldown = EditorGUILayout.Slider("Cooldown (seconds)", obj.buttonCooldown, 0f, 10f);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
        SerializedProperty onButtonPressed = serializedObject.FindProperty("onButtonPressed");
        EditorGUILayout.PropertyField(onButtonPressed);
    }

    private void ShowLeverSettings(InteractableObject obj)
    {
        EditorGUILayout.LabelField("Lever Settings", EditorStyles.boldLabel);
        
        obj.isLeverActivated = EditorGUILayout.Toggle("Initial State (Activated)", obj.isLeverActivated);
        obj.leverTransform = (Transform)EditorGUILayout.ObjectField("Lever Transform", obj.leverTransform, typeof(Transform), true);
        
        if (obj.leverTransform != null)
        {
            obj.leverActivatedRotation = EditorGUILayout.Vector3Field("Activated Rotation", obj.leverActivatedRotation);
            obj.leverDeactivatedRotation = EditorGUILayout.Vector3Field("Deactivated Rotation", obj.leverDeactivatedRotation);
            obj.leverRotationSpeed = EditorGUILayout.Slider("Rotation Speed", obj.leverRotationSpeed, 0.1f, 20f);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
        SerializedProperty onLeverActivated = serializedObject.FindProperty("onLeverActivated");
        SerializedProperty onLeverDeactivated = serializedObject.FindProperty("onLeverDeactivated");
        EditorGUILayout.PropertyField(onLeverActivated);
        EditorGUILayout.PropertyField(onLeverDeactivated);
    }

    private void ShowDoorSettings(InteractableObject obj)
    {
        EditorGUILayout.LabelField("Door Settings", EditorStyles.boldLabel);
        
        obj.isDoorOpen = EditorGUILayout.Toggle("Initial State (Open)", obj.isDoorOpen);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
        SerializedProperty onDoorOpened = serializedObject.FindProperty("onDoorOpened");
        SerializedProperty onDoorClosed = serializedObject.FindProperty("onDoorClosed");
        EditorGUILayout.PropertyField(onDoorOpened);
        EditorGUILayout.PropertyField(onDoorClosed);
        
        EditorGUILayout.HelpBox("Use UnityEvents to animate door movement", MessageType.Info);
    }

    private void ShowContainerSettings(InteractableObject obj)
    {
        EditorGUILayout.LabelField("Container Settings", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Container functionality coming soon!\nUse Custom type with UnityEvent for now.", MessageType.Warning);
    }

    private void ShowCustomSettings(InteractableObject obj)
    {
        EditorGUILayout.LabelField("Custom Settings", EditorStyles.boldLabel);
        
        SerializedProperty onCustomInteract = serializedObject.FindProperty("onCustomInteract");
        EditorGUILayout.PropertyField(onCustomInteract);
        
        EditorGUILayout.HelpBox("Use UnityEvent to define custom interaction behavior", MessageType.Info);
    }
}
#endif



