using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace DRAUM.CMS
{
    /// <summary>
    /// Центральная база данных для управления контентом
    /// Хранит метаданные всех ассетов для быстрого поиска и фильтрации
    /// </summary>
    [System.Serializable]
    public class ContentDatabase : ScriptableObject
    {
        [Header("Content Database")]
        [SerializeField] private List<ContentAsset> assets = new List<ContentAsset>();
        [SerializeField] private List<ContentCategory> categories = new List<ContentCategory>();
        [SerializeField] private List<ContentTag> tags = new List<ContentTag>();
        
        public static event Action<ContentAsset> OnAssetAdded;
        public static event Action<ContentAsset> OnAssetRemoved;
        public static event Action<ContentAsset> OnAssetUpdated;
        
        public IReadOnlyList<ContentAsset> Assets => assets;
        public IReadOnlyList<ContentCategory> Categories => categories;
        public IReadOnlyList<ContentTag> Tags => tags;
        
        private static ContentDatabase _instance;
        public static ContentDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindOrCreateDatabase();
                }
                return _instance;
            }
        }
        
        private static ContentDatabase FindOrCreateDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:ContentDatabase");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<ContentDatabase>(path);
            }
            
            ContentDatabase database = CreateInstance<ContentDatabase>();
            string databasePath = "Assets/_Project/Settings/ContentDatabase.asset";
            
            string directory = Path.GetDirectoryName(databasePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            AssetDatabase.CreateAsset(database, databasePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            return database;
        }
        
        /// <summary>
        /// Добавить ассет в базу данных
        /// </summary>
        public void AddAsset(ContentAsset asset)
        {
            if (assets.Any(a => a.GUID == asset.GUID))
            {
                UpdateAsset(asset);
                return;
            }
            
            assets.Add(asset);
            EditorUtility.SetDirty(this);
            OnAssetAdded?.Invoke(asset);
        }
        
        /// <summary>
        /// Удалить ассет из базы данных
        /// </summary>
        public void RemoveAsset(string guid)
        {
            var asset = assets.FirstOrDefault(a => a.GUID == guid);
            if (asset != null)
            {
                assets.Remove(asset);
                EditorUtility.SetDirty(this);
                OnAssetRemoved?.Invoke(asset);
            }
        }
        
        /// <summary>
        /// Обновить ассет в базе данных
        /// </summary>
        public void UpdateAsset(ContentAsset asset)
        {
            var existingAsset = assets.FirstOrDefault(a => a.GUID == asset.GUID);
            if (existingAsset != null)
            {
                int index = assets.IndexOf(existingAsset);
                assets[index] = asset;
                EditorUtility.SetDirty(this);
                OnAssetUpdated?.Invoke(asset);
            }
        }
        
        /// <summary>
        /// Получить ассет по GUID
        /// </summary>
        public ContentAsset GetAsset(string guid)
        {
            return assets.FirstOrDefault(a => a.GUID == guid);
        }
        
        /// <summary>
        /// Поиск ассетов по фильтрам
        /// </summary>
        public List<ContentAsset> SearchAssets(ContentSearchFilter filter)
        {
            var results = assets.AsEnumerable();
            
            if (filter.AssetTypes != null && filter.AssetTypes.Length > 0)
            {
                results = results.Where(a => filter.AssetTypes.Contains(a.AssetType));
            }
            
            if (!string.IsNullOrEmpty(filter.Category))
            {
                results = results.Where(a => a.Category == filter.Category);
            }
            
            if (filter.Tags != null && filter.Tags.Length > 0)
            {
                results = results.Where(a => filter.Tags.Any(tag => a.Tags.Contains(tag)));
            }
            
            if (!string.IsNullOrEmpty(filter.NameFilter))
            {
                results = results.Where(a => a.Name.ToLower().Contains(filter.NameFilter.ToLower()));
            }
            
            if (filter.DateFrom.HasValue)
            {
                results = results.Where(a => a.LastModified >= filter.DateFrom.Value);
            }
            
            if (filter.DateTo.HasValue)
            {
                results = results.Where(a => a.LastModified <= filter.DateTo.Value);
            }
            
            switch (filter.SortBy)
            {
                case ContentSortType.Name:
                    results = filter.SortAscending ? results.OrderBy(a => a.Name) : results.OrderByDescending(a => a.Name);
                    break;
                case ContentSortType.Date:
                    results = filter.SortAscending ? results.OrderBy(a => a.LastModified) : results.OrderByDescending(a => a.LastModified);
                    break;
                case ContentSortType.Size:
                    results = filter.SortAscending ? results.OrderBy(a => a.FileSize) : results.OrderByDescending(a => a.FileSize);
                    break;
            }
            
            return results.ToList();
        }
        
        /// <summary>
        /// Добавить категорию
        /// </summary>
        public void AddCategory(ContentCategory category)
        {
            if (!categories.Any(c => c.Name == category.Name))
            {
                categories.Add(category);
                EditorUtility.SetDirty(this);
            }
        }
        
        /// <summary>
        /// Добавить тег
        /// </summary>
        public void AddTag(ContentTag tag)
        {
            if (!tags.Any(t => t.Name == tag.Name))
            {
                tags.Add(tag);
                EditorUtility.SetDirty(this);
            }
        }
        
        /// <summary>
        /// Получить все уникальные категории из ассетов
        /// </summary>
        public List<string> GetUsedCategories()
        {
            return assets.Select(a => a.Category).Distinct().Where(c => !string.IsNullOrEmpty(c)).ToList();
        }
        
        /// <summary>
        /// Получить все уникальные теги из ассетов
        /// </summary>
        public List<string> GetUsedTags()
        {
            return assets.SelectMany(a => a.Tags).Distinct().Where(t => !string.IsNullOrEmpty(t)).ToList();
        }
        
        /// <summary>
        /// Очистить базу данных
        /// </summary>
        public void ClearDatabase()
        {
            assets.Clear();
            EditorUtility.SetDirty(this);
        }
        
        /// <summary>
        /// Удалить категорию
        /// </summary>
        public void RemoveCategory(ContentCategory category)
        {
            if (categories.Contains(category))
            {
                categories.Remove(category);
                EditorUtility.SetDirty(this);
            }
        }
        
        /// <summary>
        /// Пересканировать все ассеты
        /// </summary>
        public void RescanAssets()
        {
            ClearDatabase();
            
            string[] allAssets = AssetDatabase.FindAssets("", new[] { "Assets/_Project" });
            int projectCount = 0;
            
            foreach (string guid in allAssets)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".meta") && !Directory.Exists(path) && 
                    !path.Contains("/Scripts/") && !path.Contains("/Settings/") && 
                    !path.Contains("/Configs/") && !path.Contains("/Localization/"))
                {
                    ContentAsset asset = ContentAsset.CreateFromPath(path);
                    if (asset != null)
                    {
                        AddAsset(asset);
                        projectCount++;
                    }
                }
            }
            
            Debug.Log($"Content Database: Rescanned {projectCount} assets from _Project folder");
        }
    }
}
