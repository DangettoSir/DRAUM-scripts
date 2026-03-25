using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace DRAUM.CMS
{
    /// <summary>
    /// Автоматическая система организации контента
    /// Сортирует ассеты по типам и категориям
    /// </summary>
    public static class ContentOrganizer
    {
        private static readonly Dictionary<string, string> AssetTypeFolders = new Dictionary<string, string>
        {
            { "Model", "Models" },
            { "Texture", "Textures" },
            { "Material", "Materials" },
            { "Prefab", "Prefabs" },
            { "Animation", "Animations" },
            { "Script", "Scripts" },
            { "Shader", "Shaders" },
            { "Scene", "Scenes" },
            { "Audio", "Audio" },
            { "Asset", "Assets" }
        };
        
        private static readonly Dictionary<string, string> CategoryFolders = new Dictionary<string, string>
        {
            { "Player", "Characters/Player" },
            { "NPCs", "Characters/NPCs" },
            { "Characters", "Characters" },
            { "Buildings", "Environment/Buildings" },
            { "Nature", "Environment/Nature" },
            { "Props", "Environment/Props" },
            { "Environment", "Environment" },
            { "Weapons", "Weapons" },
            { "UI", "UI" },
            { "Audio", "Audio" }
        };
        
        /// <summary>
        /// Организовать все ассеты в Content папке
        /// </summary>
        public static void OrganizeAllContent()
        {
            if (!EditorUtility.DisplayDialog("Organize Content", 
                "This will reorganize all content in the Content folder. Continue?", 
                "Yes", "Cancel"))
            {
                return;
            }
            
            try
            {
                EditorUtility.DisplayProgressBar("Organizing Content", "Scanning assets...", 0f);
                
                string contentPath = "Assets/_Project/Content";
                if (!Directory.Exists(contentPath))
                {
                    Debug.LogError("Content folder not found!");
                    return;
                }
                
                string[] allAssetGuids = AssetDatabase.FindAssets("", new[] { contentPath });
                int totalAssets = allAssetGuids.Length;
                int processedAssets = 0;
                
                List<string> movedAssets = new List<string>();
                
                foreach (string guid in allAssetGuids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    
                    if (Directory.Exists(assetPath) || assetPath.EndsWith(".meta"))
                        continue;
                    
                    EditorUtility.DisplayProgressBar("Organizing Content", 
                        $"Processing {Path.GetFileName(assetPath)}...", 
                        (float)processedAssets / totalAssets);
                    
                    string newPath = GetOrganizedPath(assetPath);
                    if (newPath != assetPath && !File.Exists(newPath))
                    {
                        string directory = Path.GetDirectoryName(newPath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        
                        string result = AssetDatabase.MoveAsset(assetPath, newPath);
                        if (string.IsNullOrEmpty(result))
                        {
                            movedAssets.Add($"{assetPath} -> {newPath}");
                        }
                    }
                    
                    processedAssets++;
                }
                
                AssetDatabase.Refresh();
                
                EditorUtility.ClearProgressBar();
                
                Debug.Log($"Content organization complete! Moved {movedAssets.Count} assets.");
                
                if (movedAssets.Count > 0)
                {
                    Debug.Log("Moved assets:\n" + string.Join("\n", movedAssets.Take(10)));
                    if (movedAssets.Count > 10)
                    {
                        Debug.Log($"... and {movedAssets.Count - 10} more");
                    }
                }
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"Error organizing content: {e.Message}");
            }
        }
        
        /// <summary>
        /// Организовать выбранные ассеты
        /// </summary>
        public static void OrganizeSelectedAssets()
        {
            if (Selection.objects.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select assets to organize.", "OK");
                return;
            }
            
            try
            {
                EditorUtility.DisplayProgressBar("Organizing Selected Assets", "Processing...", 0f);
                
                List<string> movedAssets = new List<string>();
                
                for (int i = 0; i < Selection.objects.Length; i++)
                {
                    UnityEngine.Object obj = Selection.objects[i];
                    string assetPath = AssetDatabase.GetAssetPath(obj);
                    
                    EditorUtility.DisplayProgressBar("Organizing Selected Assets", 
                        $"Processing {obj.name}...", 
                        (float)i / Selection.objects.Length);
                    
                    string newPath = GetOrganizedPath(assetPath);
                    if (newPath != assetPath && !File.Exists(newPath))
                    {
                        string directory = Path.GetDirectoryName(newPath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        
                        string result = AssetDatabase.MoveAsset(assetPath, newPath);
                        if (string.IsNullOrEmpty(result))
                        {
                            movedAssets.Add($"{assetPath} -> {newPath}");
                        }
                    }
                }
                
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();
                
                Debug.Log($"Organized {movedAssets.Count} selected assets.");
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"Error organizing selected assets: {e.Message}");
            }
        }
        
        /// <summary>
        /// Создать стандартную структуру папок
        /// </summary>
        public static void CreateStandardStructure()
        {
            try
            {
                string contentPath = "Assets/_Project/Content";
                
                CreateFolderStructure(contentPath, new[]
                {
                    "Characters/Player/Models",
                    "Characters/Player/Textures", 
                    "Characters/Player/Materials",
                    "Characters/Player/Animations",
                    "Characters/Player/Prefabs",
                    "Characters/NPCs/Models",
                    "Characters/NPCs/Textures",
                    "Characters/NPCs/Materials", 
                    "Characters/NPCs/Animations",
                    "Characters/NPCs/Prefabs",
                    "Environment/Buildings/Models",
                    "Environment/Buildings/Textures",
                    "Environment/Buildings/Materials",
                    "Environment/Buildings/Prefabs",
                    "Environment/Nature/Trees/Models",
                    "Environment/Nature/Trees/Textures",
                    "Environment/Nature/Trees/Materials",
                    "Environment/Nature/Rocks/Models",
                    "Environment/Nature/Rocks/Textures", 
                    "Environment/Nature/Rocks/Materials",
                    "Environment/Props/Models",
                    "Environment/Props/Textures",
                    "Environment/Props/Materials",
                    "Environment/Props/Prefabs",
                    "Weapons/Models",
                    "Weapons/Textures",
                    "Weapons/Materials",
                    "Weapons/Prefabs",
                    "UI/Sprites",
                    "UI/Materials",
                    "UI/Prefabs",
                    "Audio/Music",
                    "Audio/SFX",
                    "Audio/Voice"
                });
                
                AssetDatabase.Refresh();
                Debug.Log("Standard folder structure created successfully!");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error creating folder structure: {e.Message}");
            }
        }
        
        /// <summary>
        /// Получить организованный путь для ассета
        /// </summary>
        private static string GetOrganizedPath(string assetPath)
        {
            if (!assetPath.StartsWith("Assets/_Project/Content/"))
                return assetPath;
            
            string fileName = Path.GetFileName(assetPath);
            string fileExtension = Path.GetExtension(assetPath);
            string assetName = Path.GetFileNameWithoutExtension(assetPath);
            
            string assetType = GetAssetTypeFromExtension(fileExtension);
            string typeFolder = AssetTypeFolders.ContainsKey(assetType) ? AssetTypeFolders[assetType] : "Other";
            
            string category = GetCategoryFromPath(assetPath);
            string categoryFolder = CategoryFolders.ContainsKey(category) ? CategoryFolders[category] : "Other";
            
            string newPath = $"Assets/_Project/Content/{categoryFolder}/{typeFolder}/{fileName}";
            
            if (newPath == assetPath)
                return assetPath;
            
            return newPath;
        }
        
        /// <summary>
        /// Получить тип ассета из расширения
        /// </summary>
        private static string GetAssetTypeFromExtension(string extension)
        {
            switch (extension.ToLower())
            {
                case ".fbx":
                case ".obj":
                case ".blend":
                case ".dae":
                case ".3ds":
                case ".max":
                    return "Model";
                    
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".tga":
                case ".psd":
                case ".tiff":
                    return "Texture";
                    
                case ".mat":
                    return "Material";
                    
                case ".prefab":
                    return "Prefab";
                    
                case ".anim":
                    return "Animation";
                    
                case ".controller":
                    return "Animator";
                    
                case ".cs":
                    return "Script";
                    
                case ".shader":
                case ".shadergraph":
                    return "Shader";
                    
                case ".unity":
                    return "Scene";
                    
                case ".mp3":
                case ".wav":
                case ".ogg":
                case ".aiff":
                    return "Audio";
                    
                case ".asset":
                    return "Asset";
                    
                default:
                    return "Other";
            }
        }
        
        /// <summary>
        /// Получить категорию из пути
        /// </summary>
        private static string GetCategoryFromPath(string path)
        {
            string normalizedPath = path.Replace("\\", "/").ToLower();
            
            if (normalizedPath.Contains("/characters/player/"))
                return "Player";
            else if (normalizedPath.Contains("/characters/npc"))
                return "NPCs";
            else if (normalizedPath.Contains("/characters/"))
                return "Characters";
            else if (normalizedPath.Contains("/environment/buildings/"))
                return "Buildings";
            else if (normalizedPath.Contains("/environment/nature/"))
                return "Nature";
            else if (normalizedPath.Contains("/environment/props/"))
                return "Props";
            else if (normalizedPath.Contains("/environment/"))
                return "Environment";
            else if (normalizedPath.Contains("/weapons/"))
                return "Weapons";
            else if (normalizedPath.Contains("/ui/"))
                return "UI";
            else if (normalizedPath.Contains("/audio/"))
                return "Audio";
            
            return "Other";
        }
        
        /// <summary>
        /// Создать структуру папок
        /// </summary>
        private static void CreateFolderStructure(string basePath, string[] folders)
        {
            foreach (string folder in folders)
            {
                string fullPath = Path.Combine(basePath, folder);
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }
            }
        }
        
        /// <summary>
        /// Очистить пустые папки
        /// </summary>
        public static void CleanEmptyFolders()
        {
            try
            {
                string contentPath = "Assets/_Project/Content";
                CleanEmptyFoldersRecursive(contentPath);
                AssetDatabase.Refresh();
                Debug.Log("Empty folders cleaned successfully!");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error cleaning empty folders: {e.Message}");
            }
        }
        
        /// <summary>
        /// Рекурсивно очистить пустые папки
        /// </summary>
        private static void CleanEmptyFoldersRecursive(string path)
        {
            if (!Directory.Exists(path))
                return;
            
            string[] subdirectories = Directory.GetDirectories(path);
            foreach (string subdirectory in subdirectories)
            {
                CleanEmptyFoldersRecursive(subdirectory);
            }
            
            if (Directory.GetFiles(path).Length == 0 && Directory.GetDirectories(path).Length == 0)
            {
                Directory.Delete(path);
                string metaPath = path + ".meta";
                if (File.Exists(metaPath))
                {
                    File.Delete(metaPath);
                }
            }
        }
    }
}
