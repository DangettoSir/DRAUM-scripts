using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace DRAUM.CMS
{
    /// <summary>
    /// Модель данных для ассета в CMS
    /// Содержит всю метаинформацию для поиска и фильтрации
    /// </summary>
    [System.Serializable]
    public class ContentAsset
    {
        [Header("Basic Info")]
        public string GUID;
        public string Name;
        public string Path;
        public string AssetType;
        public long FileSize;
        public DateTime LastModified;
        
        [Header("Organization")]
        public string Category;
        public List<string> Tags = new List<string>();
        public string Description;
        
        [Header("Metadata")]
        public string Author;
        public string Version;
        public int UsageCount;
        public bool IsFavorite;
        public bool IsHidden;
        
        [Header("Technical")]
        public string ImportSettings;
        public string Dependencies;
        public string References;
        
        [Header("Preview")]
        public Texture2D Preview;
        public string PreviewPath;
        
        public ContentAsset()
        {
            GUID = System.Guid.NewGuid().ToString();
            LastModified = DateTime.Now;
            Tags = new List<string>();
        }
        
        /// <summary>
        /// Создать превью для ассета
        /// </summary>
        public void CreatePreview()
        {
            if (string.IsNullOrEmpty(Path)) return;
            
            UnityEngine.Object assetObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Path);
            if (assetObject == null) return;
            
            switch (AssetType.ToLower())
            {
                case "texture":
                    Preview = assetObject as Texture2D;
                    break;
                case "material":
                    Preview = AssetPreview.GetAssetPreview(assetObject);
                    break;
                case "model":
                case "prefab":
                    Preview = AssetPreview.GetAssetPreview(assetObject);
                    if (Preview == null)
                        Preview = AssetPreview.GetMiniThumbnail(assetObject);
                    break;
                case "animation":
                    Preview = AssetPreview.GetMiniThumbnail(assetObject);
                    break;
                case "scene":
                    Preview = AssetPreview.GetMiniThumbnail(assetObject);
                    break;
                case "audio":
                    Preview = AssetPreview.GetMiniThumbnail(assetObject);
                    break;
                case "shader":
                    Preview = AssetPreview.GetMiniThumbnail(assetObject);
                    break;
                default:
                    Preview = AssetPreview.GetMiniThumbnail(assetObject);
                    break;
            }
            
            if (Preview == null)
            {
                Preview = CreateTypeIcon();
            }
        }
        
        /// <summary>
        /// Создать иконку по типу ассета
        /// </summary>
        private Texture2D CreateTypeIcon()
        {
            Texture2D icon = new Texture2D(64, 64);
            Color[] pixels = new Color[64 * 64];
            
            Color iconColor = GetTypeColor();
            
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float centerX = 32f;
                    float centerY = 32f;
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    
                    if (distance < 30f)
                    {
                        pixels[y * 64 + x] = iconColor;
                    }
                    else if (distance < 32f)
                    {
                        pixels[y * 64 + x] = Color.Lerp(iconColor, Color.black, 0.5f);
                    }
                    else
                    {
                        pixels[y * 64 + x] = Color.clear;
                    }
                }
            }
            
            icon.SetPixels(pixels);
            icon.Apply();
            
            return icon;
        }
        
        /// <summary>
        /// Получить цвет иконки по типу ассета
        /// </summary>
        private Color GetTypeColor()
        {
            switch (AssetType.ToLower())
            {
                case "texture":
                    return new Color(0.2f, 0.8f, 0.2f, 1f); // Зеленый
                case "material":
                    return new Color(0.8f, 0.2f, 0.8f, 1f); // Фиолетовый
                case "model":
                    return new Color(0.2f, 0.2f, 0.8f, 1f); // Синий
                case "prefab":
                    return new Color(0.8f, 0.8f, 0.2f, 1f); // Желтый
                case "animation":
                    return new Color(0.8f, 0.4f, 0.2f, 1f); // Оранжевый
                case "scene":
                    return new Color(0.2f, 0.8f, 0.8f, 1f); // Голубой
                case "audio":
                    return new Color(0.8f, 0.2f, 0.2f, 1f); // Красный
                case "shader":
                    return new Color(0.4f, 0.4f, 0.4f, 1f); // Серый
                default:
                    return new Color(0.5f, 0.5f, 0.5f, 1f); // Серый по умолчанию
            }
        }
        
        /// <summary>
        /// Создать ContentAsset из пути к файлу
        /// </summary>
        public static ContentAsset CreateFromPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath))
                return null;
                
            ContentAsset asset = new ContentAsset();
            
            asset.GUID = AssetDatabase.AssetPathToGUID(assetPath);
            asset.Name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            asset.Path = assetPath;
            asset.AssetType = GetAssetType(assetPath);
            asset.FileSize = new FileInfo(assetPath).Length;
            asset.LastModified = File.GetLastWriteTime(assetPath);
            
            asset.Category = GetCategoryFromPath(assetPath);
            
            asset.Tags = GetAutoTags(assetPath, asset.AssetType);
            
            asset.CreatePreview();
            
            return asset;
        }
        
        /// <summary>
        /// Получить тип ассета из расширения файла
        /// </summary>
        private static string GetAssetType(string path)
        {
            string extension = System.IO.Path.GetExtension(path).ToLower();
            
            switch (extension)
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
        /// Получить категорию из пути к файлу
        /// </summary>
        private static string GetCategoryFromPath(string path)
        {
            string normalizedPath = path.Replace("\\", "/").ToLower();
            
            if (normalizedPath.Contains("/art/"))
            {
                if (normalizedPath.Contains("/models/"))
                    return "Models";
                else if (normalizedPath.Contains("/textures/"))
                    return "Textures";
                else if (normalizedPath.Contains("/materials/"))
                    return "Materials";
                else if (normalizedPath.Contains("/animations/"))
                    return "Animations";
                else if (normalizedPath.Contains("/shaders/"))
                    return "Shaders";
                else if (normalizedPath.Contains("/terrain/"))
                    return "Terrain";
                else if (normalizedPath.Contains("/ui/"))
                    return "UI";
                else
                    return "Art";
            }
            else if (normalizedPath.Contains("/audio/"))
            {
                return "Audio";
            }
            else if (normalizedPath.Contains("/prefabs/"))
            {
                return "Prefabs";
            }
            else if (normalizedPath.Contains("/scenes/"))
            {
                if (normalizedPath.Contains("/core/"))
                    return "Core Scenes";
                else if (normalizedPath.Contains("/levels/"))
                    return "Level Scenes";
                else if (normalizedPath.Contains("/test/"))
                    return "Test Scenes";
                else
                    return "Scenes";
            }
            
            return "Other";
        }
        
        /// <summary>
        /// Получить автоматические теги на основе пути и типа
        /// </summary>
        private static List<string> GetAutoTags(string path, string assetType)
        {
            List<string> tags = new List<string>();
            
            tags.Add(assetType.ToLower());
            
            string normalizedPath = path.Replace("\\", "/").ToLower();
            
            if (normalizedPath.Contains("player"))
                tags.Add("player");
            if (normalizedPath.Contains("npc"))
                tags.Add("npc");
            if (normalizedPath.Contains("building"))
                tags.Add("building");
            if (normalizedPath.Contains("tree"))
                tags.Add("tree");
            if (normalizedPath.Contains("rock"))
                tags.Add("rock");
            if (normalizedPath.Contains("weapon"))
                tags.Add("weapon");
            if (normalizedPath.Contains("ui"))
                tags.Add("ui");
            if (normalizedPath.Contains("audio"))
                tags.Add("audio");
            
            long fileSize = new FileInfo(path).Length;
            if (fileSize > 10 * 1024 * 1024)
                tags.Add("large");
            else if (fileSize < 100 * 1024)
                tags.Add("small");
            
            return tags;
        }
        
        /// <summary>
        /// Обновить информацию об ассете
        /// </summary>
        public void Refresh()
        {
            if (File.Exists(Path))
            {
                FileSize = new FileInfo(Path).Length;
                LastModified = File.GetLastWriteTime(Path);
                Preview = AssetPreview.GetAssetPreview(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Path));
            }
        }
        
        /// <summary>
        /// Проверить, существует ли ассет
        /// </summary>
        public bool Exists()
        {
            return File.Exists(Path);
        }
        
        /// <summary>
        /// Получить размер файла в читаемом формате
        /// </summary>
        public string GetFormattedFileSize()
        {
            if (FileSize < 1024)
                return $"{FileSize} B";
            else if (FileSize < 1024 * 1024)
                return $"{FileSize / 1024.0:F1} KB";
            else if (FileSize < 1024 * 1024 * 1024)
                return $"{FileSize / (1024.0 * 1024.0):F1} MB";
            else
                return $"{FileSize / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }
        
        /// <summary>
        /// Получить относительный путь от Content папки
        /// </summary>
        public string GetRelativePath()
        {
            if (Path.Contains("Assets/_Project/Content/"))
            {
                return Path.Substring(Path.IndexOf("Assets/_Project/Content/") + "Assets/_Project/Content/".Length);
            }
            return Path;
        }
        
        /// <summary>
        /// Добавить тег
        /// </summary>
        public void AddTag(string tag)
        {
            if (!Tags.Contains(tag))
            {
                Tags.Add(tag);
            }
        }
        
        /// <summary>
        /// Удалить тег
        /// </summary>
        public void RemoveTag(string tag)
        {
            Tags.Remove(tag);
        }
        
        /// <summary>
        /// Проверить, содержит ли ассет тег
        /// </summary>
        public bool HasTag(string tag)
        {
            return Tags.Contains(tag);
        }
    }
}
