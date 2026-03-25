using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace DRAUM.CMS
{
    /// <summary>
    /// Мигратор для перемещения существующих ассетов в новую CMS структуру
    /// </summary>
    public static class ContentMigrator
    {
        /// <summary>
        /// Мигрировать все ассеты из старой структуры в новую CMS структуру
        /// </summary>
        public static void MigrateExistingAssets()
        {
            if (!EditorUtility.DisplayDialog("Migrate Assets", 
                "This will move all existing assets to the new CMS structure. Continue?", 
                "Yes", "Cancel"))
            {
                return;
            }
            
            try
            {
                EditorUtility.DisplayProgressBar("Migrating Assets", "Scanning existing assets...", 0f);
                
                CreateTargetStructure();
                
                int totalMoved = 0;
                
                totalMoved += MigrateCharacters();
                
                totalMoved += MigrateEnvironment();
                
                totalMoved += MigrateUI();
                
                totalMoved += MigrateAudio();
                
                totalMoved += MigrateScenes();
                
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();
                
                Debug.Log($"Migration complete! Moved {totalMoved} assets to CMS structure.");
                ContentDatabase.Instance.RescanAssets();
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"Migration failed: {e.Message}");
            }
        }
        
        private static void CreateTargetStructure()
        {
            string[] folders = {
                "Assets/_Project/Content/Characters/Player/Models",
                "Assets/_Project/Content/Characters/Player/Textures",
                "Assets/_Project/Content/Characters/Player/Materials",
                "Assets/_Project/Content/Characters/Player/Animations",
                "Assets/_Project/Content/Characters/Player/Prefabs",
                "Assets/_Project/Content/Characters/NPCs/Models",
                "Assets/_Project/Content/Characters/NPCs/Textures",
                "Assets/_Project/Content/Characters/NPCs/Materials",
                "Assets/_Project/Content/Characters/NPCs/Animations",
                "Assets/_Project/Content/Characters/NPCs/Prefabs",
                "Assets/_Project/Content/Environment/Buildings/Models",
                "Assets/_Project/Content/Environment/Buildings/Textures",
                "Assets/_Project/Content/Environment/Buildings/Materials",
                "Assets/_Project/Content/Environment/Buildings/Prefabs",
                "Assets/_Project/Content/Environment/Nature/Trees/Models",
                "Assets/_Project/Content/Environment/Nature/Trees/Textures",
                "Assets/_Project/Content/Environment/Nature/Trees/Materials",
                "Assets/_Project/Content/Environment/Nature/Rocks/Models",
                "Assets/_Project/Content/Environment/Nature/Rocks/Textures",
                "Assets/_Project/Content/Environment/Nature/Rocks/Materials",
                "Assets/_Project/Content/Environment/Props/Models",
                "Assets/_Project/Content/Environment/Props/Textures",
                "Assets/_Project/Content/Environment/Props/Materials",
                "Assets/_Project/Content/Environment/Props/Prefabs",
                "Assets/_Project/Content/Weapons/Models",
                "Assets/_Project/Content/Weapons/Textures",
                "Assets/_Project/Content/Weapons/Materials",
                "Assets/_Project/Content/Weapons/Prefabs",
                "Assets/_Project/Content/UI/Sprites",
                "Assets/_Project/Content/UI/Materials",
                "Assets/_Project/Content/UI/Prefabs",
                "Assets/_Project/Content/Audio/Music",
                "Assets/_Project/Content/Audio/SFX",
                "Assets/_Project/Content/Audio/Voice"
            };
            
            foreach (string folder in folders)
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }
        }
        
        private static int MigrateCharacters()
        {
            int moved = 0;
            
            string[] playerModels = AssetDatabase.FindAssets("t:Model", new[] { "Assets/_Project/Art/Models/Characters/Player" });
            foreach (string guid in playerModels)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileName(path);
                string newPath = $"Assets/_Project/Content/Characters/Player/Models/{fileName}";
                
                if (!File.Exists(newPath))
                {
                    AssetDatabase.MoveAsset(path, newPath);
                    moved++;
                }
            }
            
            string[] playerTextures = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project/Art/Textures/Characters/Player" });
            foreach (string guid in playerTextures)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileName(path);
                string newPath = $"Assets/_Project/Content/Characters/Player/Textures/{fileName}";
                
                if (!File.Exists(newPath))
                {
                    AssetDatabase.MoveAsset(path, newPath);
                    moved++;
                }
            }
            
            string[] playerMaterials = AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project/Art/Materials/Characters/Player" });
            foreach (string guid in playerMaterials)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileName(path);
                string newPath = $"Assets/_Project/Content/Characters/Player/Materials/{fileName}";
                
                if (!File.Exists(newPath))
                {
                    AssetDatabase.MoveAsset(path, newPath);
                    moved++;
                }
            }
            
            string[] playerAnimations = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/_Project/Art/Animations/Player" });
            foreach (string guid in playerAnimations)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileName(path);
                string newPath = $"Assets/_Project/Content/Characters/Player/Animations/{fileName}";
                
                if (!File.Exists(newPath))
                {
                    AssetDatabase.MoveAsset(path, newPath);
                    moved++;
                }
            }
            
            string[] playerPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/Player" });
            foreach (string guid in playerPrefabs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileName(path);
                string newPath = $"Assets/_Project/Content/Characters/Player/Prefabs/{fileName}";
                
                if (!File.Exists(newPath))
                {
                    AssetDatabase.MoveAsset(path, newPath);
                    moved++;
                }
            }
            
            return moved;
        }
        
        private static int MigrateEnvironment()
        {
            int moved = 0;
            
            string[] envModels = AssetDatabase.FindAssets("t:Model", new[] { "Assets/_Project/Art/Models/Environment" });
            foreach (string guid in envModels)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileName(path);
                
                string newPath;
                if (path.Contains("Buildings"))
                    newPath = $"Assets/_Project/Content/Environment/Buildings/Models/{fileName}";
                else if (path.Contains("Nature"))
                {
                    if (path.Contains("Trees"))
                        newPath = $"Assets/_Project/Content/Environment/Nature/Trees/Models/{fileName}";
                    else if (path.Contains("Rocks"))
                        newPath = $"Assets/_Project/Content/Environment/Nature/Rocks/Models/{fileName}";
                    else
                        newPath = $"Assets/_Project/Content/Environment/Props/Models/{fileName}";
                }
                else
                    newPath = $"Assets/_Project/Content/Environment/Props/Models/{fileName}";
                
                if (!File.Exists(newPath))
                {
                    AssetDatabase.MoveAsset(path, newPath);
                    moved++;
                }
            }
            
            string[] envTextures = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project/Art/Textures/Environment" });
            foreach (string guid in envTextures)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileName(path);
                
                string newPath;
                if (path.Contains("Buildings"))
                    newPath = $"Assets/_Project/Content/Environment/Buildings/Textures/{fileName}";
                else if (path.Contains("Nature"))
                {
                    if (path.Contains("Trees"))
                        newPath = $"Assets/_Project/Content/Environment/Nature/Trees/Textures/{fileName}";
                    else if (path.Contains("Rocks"))
                        newPath = $"Assets/_Project/Content/Environment/Nature/Rocks/Textures/{fileName}";
                    else
                        newPath = $"Assets/_Project/Content/Environment/Props/Textures/{fileName}";
                }
                else
                    newPath = $"Assets/_Project/Content/Environment/Props/Textures/{fileName}";
                
                if (!File.Exists(newPath))
                {
                    AssetDatabase.MoveAsset(path, newPath);
                    moved++;
                }
            }
            
            string[] envMaterials = AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project/Art/Materials/Environment" });
            foreach (string guid in envMaterials)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileName(path);
                
                string newPath;
                if (path.Contains("Buildings"))
                    newPath = $"Assets/_Project/Content/Environment/Buildings/Materials/{fileName}";
                else if (path.Contains("Nature"))
                {
                    if (path.Contains("Trees"))
                        newPath = $"Assets/_Project/Content/Environment/Nature/Trees/Materials/{fileName}";
                    else if (path.Contains("Rocks"))
                        newPath = $"Assets/_Project/Content/Environment/Nature/Rocks/Materials/{fileName}";
                    else
                        newPath = $"Assets/_Project/Content/Environment/Props/Materials/{fileName}";
                }
                else
                    newPath = $"Assets/_Project/Content/Environment/Props/Materials/{fileName}";
                
                if (!File.Exists(newPath))
                {
                    AssetDatabase.MoveAsset(path, newPath);
                    moved++;
                }
            }
            
            return moved;
        }
        
        private static int MigrateUI()
        {
            int moved = 0;
            
            string[] uiSprites = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project/UI/Sprites" });
            foreach (string guid in uiSprites)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileName(path);
                string newPath = $"Assets/_Project/Content/UI/Sprites/{fileName}";
                
                if (!File.Exists(newPath))
                {
                    AssetDatabase.MoveAsset(path, newPath);
                    moved++;
                }
            }
            
            string[] uiMaterials = AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project/UI/Sprites" });
            foreach (string guid in uiMaterials)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileName(path);
                string newPath = $"Assets/_Project/Content/UI/Materials/{fileName}";
                
                if (!File.Exists(newPath))
                {
                    AssetDatabase.MoveAsset(path, newPath);
                    moved++;
                }
            }
            
            return moved;
        }
        
        private static int MigrateAudio()
        {
            int moved = 0;
            
            string[] audioFiles = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Project/Audio" });
            foreach (string guid in audioFiles)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileName(path);
                string newPath = $"Assets/_Project/Content/Audio/Music/{fileName}";
                
                if (!File.Exists(newPath))
                {
                    AssetDatabase.MoveAsset(path, newPath);
                    moved++;
                }
            }
            
            return moved;
        }
        
        private static int MigrateScenes()
        {
            int moved = 0;
            
            string[] scenes = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Project/Scenes" });
            foreach (string guid in scenes)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ContentAsset asset = ContentAsset.CreateFromPath(path);
                if (asset != null)
                {
                    ContentDatabase.Instance.AddAsset(asset);
                    moved++;
                }
            }
            
            return moved;
        }
    }
}
