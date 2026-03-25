using UnityEngine;
using UnityEditor;
using DRAUM.Core.Infrastructure.Logger;

[CustomEditor(typeof(Inventory))]
public class InventoryEditor : Editor
{
    private SerializedProperty itemsData;
    private SerializedProperty itemPrefab;
    private SerializedProperty gridSettings;
    
    private void OnEnable()
    {
        itemsData = serializedObject.FindProperty("itemsData");
        itemPrefab = serializedObject.FindProperty("itemPrefab");
        gridSettings = serializedObject.FindProperty("gridSettings");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Inventory System", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(itemsData);
        EditorGUILayout.PropertyField(itemPrefab);
        EditorGUILayout.Space(10);
        
        EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Auto-Setup Grids");
        
        if (GUILayout.Button("Collect All Grids from Children", GUILayout.Height(25)))
        {
            CollectGridsFromChildren();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.HelpBox(
            "Нажми 'Collect All Grids from Children' чтобы автоматически найти все InventoryGrid в children и создать для них настройки.\n\n" +
            "Существующие настройки будут сохранены, новые Grid'ы будут добавлены с дефолтными значениями.",
            MessageType.Info
        );
        
        EditorGUILayout.Space(5);
        EditorGUILayout.PropertyField(gridSettings, true);
        
        if (Application.isPlaying)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Runtime Info", EditorStyles.boldLabel);
            
            Inventory inventory = (Inventory)target;
            
            if (inventory.grids != null)
            {
                EditorGUILayout.LabelField($"Total Grids: {inventory.grids.Length}");
                
                for (int i = 0; i < inventory.grids.Length; i++)
                {
                    InventoryGrid grid = inventory.grids[i];
                    if (grid != null)
                    {
                        EditorGUILayout.LabelField($"  [{i}] {grid.name} (Priority: {grid.priority})");
                    }
                }
            }
            
            if (inventory.selectedItem != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Selected Item: {inventory.selectedItem.name}");
            }
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void CollectGridsFromChildren()
    {
        Inventory inventory = (Inventory)target;
        
        InventoryGrid[] foundGrids = inventory.GetComponentsInChildren<InventoryGrid>(true);
        
        if (foundGrids == null || foundGrids.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "No Grids Found",
                "Не найдено ни одного InventoryGrid в children этого объекта.",
                "OK"
            );
            return;
        }
        
        System.Collections.Generic.Dictionary<InventoryGrid, InventoryGridSettings> existingSettings = 
            new System.Collections.Generic.Dictionary<InventoryGrid, InventoryGridSettings>();
        
        if (inventory.gridSettings != null)
        {
            foreach (InventoryGridSettings settings in inventory.gridSettings)
            {
                if (settings != null && settings.grid != null)
                {
                    existingSettings[settings.grid] = settings;
                }
            }
        }
        
        System.Collections.Generic.List<InventoryGridSettings> newSettingsList = 
            new System.Collections.Generic.List<InventoryGridSettings>();
        
        int addedCount = 0;
        int preservedCount = 0;
        
        foreach (InventoryGrid grid in foundGrids)
        {
            if (grid == null) continue;
            
            if (existingSettings.ContainsKey(grid))
            {
                newSettingsList.Add(existingSettings[grid]);
                preservedCount++;
            }
            else
            {
                InventoryGridSettings newSettings = new InventoryGridSettings
                {
                    grid = grid,
                    customFadeSpeed = 5f,
                    useCustomSpeed = false,
                    filterMode = CategoryFilterMode.AllowAll,
                    filterCategories = new ItemCategory[0]
                };
                
                newSettingsList.Add(newSettings);
                addedCount++;
            }
        }
        
        Undo.RecordObject(inventory, "Collect Grids from Children");
        inventory.gridSettings = newSettingsList.ToArray();
        EditorUtility.SetDirty(inventory);

        string message = $"Готово!\n\n" +
                        $"Найдено Grid'ов: {foundGrids.Length}\n" +
                        $"Сохранено существующих: {preservedCount}\n" +
                        $"Добавлено новых: {addedCount}\n\n" +
                        $"Настройки созданы с дефолтными значениями. Настрой их в Inspector.";
        
        EditorUtility.DisplayDialog("Grids Collected", message, "OK");
        
        DraumLogger.Info(this, $"[InventoryEditor] Собрано {foundGrids.Length} Grid'ов: {preservedCount} сохранено, {addedCount} добавлено");
    }
}

