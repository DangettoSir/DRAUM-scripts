using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace DRAUM.CMS
{
    /// <summary>
    /// Создатель категорий и подпапок для CMS
    /// </summary>
    public class CategoryCreator : EditorWindow
    {
        private string newCategoryName = "";
        private string newCategoryPath = "";
        private string selectedParentFolder = "Art";
        private bool createSubfolders = true;
        private bool addToCMS = true;
        
        private Vector2 scrollPosition;
        private string[] availableFolders = { "Art", "Audio", "Prefabs", "Scenes" };
        private int selectedFolderIndex = 0;
        
        public static void ShowWindow()
        {
            GetWindow<CategoryCreator>("Create Category");
        }
        
        private void OnEnable()
        {
            minSize = new Vector2(400, 500);
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Create New Category", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Category Name:", EditorStyles.label);
            newCategoryName = EditorGUILayout.TextField(newCategoryName);
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Parent Folder:", EditorStyles.label);
            selectedFolderIndex = EditorGUILayout.Popup(selectedFolderIndex, availableFolders);
            selectedParentFolder = availableFolders[selectedFolderIndex];
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Category Path:", EditorStyles.label);
            newCategoryPath = EditorGUILayout.TextField(newCategoryPath);
            
            if (string.IsNullOrEmpty(newCategoryPath) && !string.IsNullOrEmpty(newCategoryName))
            {
                newCategoryPath = newCategoryName;
            }
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Options:", EditorStyles.label);
            createSubfolders = EditorGUILayout.Toggle("Create Subfolders", createSubfolders);
            addToCMS = EditorGUILayout.Toggle("Add to CMS Categories", addToCMS);
            
            EditorGUILayout.Space(10);
            
            if (!string.IsNullOrEmpty(newCategoryName) && !string.IsNullOrEmpty(newCategoryPath))
            {
                EditorGUILayout.LabelField("Preview Structure:", EditorStyles.boldLabel);
                DrawStructurePreview();
            }
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = !string.IsNullOrEmpty(newCategoryName) && !string.IsNullOrEmpty(newCategoryPath);
            if (GUILayout.Button("Create Category", GUILayout.Height(30)))
            {
                CreateCategory();
            }
            GUI.enabled = true;
            
            if (GUILayout.Button("Cancel", GUILayout.Height(30)))
            {
                Close();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Quick Actions:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Art Subfolder"))
            {
                CreateQuickSubfolder("Art");
            }
            if (GUILayout.Button("Create Audio Subfolder"))
            {
                CreateQuickSubfolder("Audio");
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Prefabs Subfolder"))
            {
                CreateQuickSubfolder("Prefabs");
            }
            if (GUILayout.Button("Create Scenes Subfolder"))
            {
                CreateQuickSubfolder("Scenes");
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawStructurePreview()
        {
            EditorGUILayout.BeginVertical("box");
            
            string fullPath = $"Assets/_Project/{selectedParentFolder}/{newCategoryPath}";
            EditorGUILayout.LabelField($"📁 {fullPath}");
            
            if (createSubfolders)
            {
                string[] subfolders = GetDefaultSubfolders(selectedParentFolder);
                foreach (string subfolder in subfolders)
                {
                    EditorGUILayout.LabelField($"  📁 {subfolder}", EditorStyles.miniLabel);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private string[] GetDefaultSubfolders(string parentFolder)
        {
            switch (parentFolder)
            {
                case "Art":
                    return new string[] { "Models", "Textures", "Materials", "Animations", "Shaders" };
                case "Audio":
                    return new string[] { "Music", "SFX", "Voice", "Ambient" };
                case "Prefabs":
                    return new string[] { "Characters", "Environment", "UI", "Weapons" };
                case "Scenes":
                    return new string[] { "Main", "Levels", "Test", "UI" };
                default:
                    return new string[] { "Assets" };
            }
        }
        
        private void CreateCategory()
        {
            try
            {
                string fullPath = $"Assets/_Project/{selectedParentFolder}/{newCategoryPath}";
                
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }

                if (createSubfolders)
                {
                    string[] subfolders = GetDefaultSubfolders(selectedParentFolder);
                    foreach (string subfolder in subfolders)
                    {
                        string subfolderPath = Path.Combine(fullPath, subfolder);
                        if (!Directory.Exists(subfolderPath))
                        {
                            Directory.CreateDirectory(subfolderPath);
                        }
                    }
                }
                
                if (addToCMS)
                {
                    AddCategoryToCMS(newCategoryName, newCategoryPath, selectedParentFolder);
                }
                
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog("Success", 
                    $"Category '{newCategoryName}' created successfully!\nPath: {fullPath}", "OK");
                
                Close();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", 
                    $"Failed to create category: {e.Message}", "OK");
            }
        }
        
        private void CreateQuickSubfolder(string parentFolder)
        {
            string subfolderName = EditorInputDialog.Show("Create Subfolder", 
                $"Enter name for {parentFolder} subfolder:", "");
                
            if (!string.IsNullOrEmpty(subfolderName))
            {
                string fullPath = $"Assets/_Project/{parentFolder}/{subfolderName}";
                
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                    AssetDatabase.Refresh();
                    
                    EditorUtility.DisplayDialog("Success", 
                        $"Subfolder '{subfolderName}' created in {parentFolder}!", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Warning", 
                        $"Subfolder '{subfolderName}' already exists in {parentFolder}!", "OK");
                }
            }
        }
        
        private void AddCategoryToCMS(string categoryName, string categoryPath, string parentFolder)
        {
            ContentCategory category = new ContentCategory(categoryName, $"Category for {parentFolder}/{categoryPath}");

            ContentDatabase.Instance.AddCategory(category);
        }
    }
    
    /// <summary>
    /// Простой диалог ввода
    /// </summary>
    public class EditorInputDialog : EditorWindow
    {
        private string inputText = "";
        private string message = "";
        private Action<string> onOk;
        private Action onCancel;
        
        public static string Show(string title, string message, string defaultValue = "")
        {
            EditorInputDialog window = GetWindow<EditorInputDialog>(title);
            window.message = message;
            window.inputText = defaultValue;
            window.minSize = new Vector2(300, 100);
            window.maxSize = new Vector2(300, 100);
            return window.inputText;
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField(message);
            inputText = EditorGUILayout.TextField(inputText);
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("OK"))
            {
                Close();
            }
            if (GUILayout.Button("Cancel"))
            {
                inputText = "";
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
