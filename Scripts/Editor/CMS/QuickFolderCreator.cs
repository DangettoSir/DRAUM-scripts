using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace DRAUM.CMS
{
    /// <summary>
    /// Быстрый создатель папок для CMS
    /// </summary>
    public static class QuickFolderCreator
    {
        public static void CreateArtSubfolder()
        {
            CreateQuickSubfolder("Art");
        }
        
        public static void CreateAudioSubfolder()
        {
            CreateQuickSubfolder("Audio");
        }
        
        public static void CreatePrefabsSubfolder()
        {
            CreateQuickSubfolder("Prefabs");
        }
        
        public static void CreateScenesSubfolder()
        {
            CreateQuickSubfolder("Scenes");
        }
        
        public static void CreateCustomFolder()
        {
            CategoryCreator.ShowWindow();
        }
        
        private static void CreateQuickSubfolder(string parentFolder)
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
                        
                    // Автоматически обновляем CMS
                    ContentDatabase.Instance.RescanAssets();
                }
                else
                {
                    EditorUtility.DisplayDialog("Warning", 
                        $"Subfolder '{subfolderName}' already exists in {parentFolder}!", "OK");
                }
            }
        }
        
        /// <summary>
        /// Создать стандартную структуру для новой категории
        /// </summary>
        public static void CreateStandardStructure(string parentFolder, string categoryName)
        {
            string basePath = $"Assets/_Project/{parentFolder}/{categoryName}";
            
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            
            // Создаем подпапки в зависимости от родительской папки
            string[] subfolders = GetDefaultSubfolders(parentFolder);
            foreach (string subfolder in subfolders)
            {
                string subfolderPath = Path.Combine(basePath, subfolder);
                if (!Directory.Exists(subfolderPath))
                {
                    Directory.CreateDirectory(subfolderPath);
                }
            }
            
            AssetDatabase.Refresh();
        }
        
        private static string[] GetDefaultSubfolders(string parentFolder)
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
    }
}
