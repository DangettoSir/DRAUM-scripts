using System;
using System.Collections.Generic;
using UnityEngine;

namespace DRAUM.CMS
{
    /// <summary>
    /// Фильтр для поиска контента
    /// </summary>
    [System.Serializable]
    public class ContentSearchFilter
    {
        [Header("Basic Filters")]
        public string NameFilter = "";
        public string[] AssetTypes = new string[0];
        public string Category = "";
        public string[] Tags = new string[0];
        
        [Header("Date Filters")]
        public DateTime? DateFrom;
        public DateTime? DateTo;
        
        [Header("Size Filters")]
        public long MinSize = 0;
        public long MaxSize = long.MaxValue;
        
        [Header("Sorting")]
        public ContentSortType SortBy = ContentSortType.Name;
        public bool SortAscending = true;
        
        [Header("Display Options")]
        public bool ShowHidden = false;
        public bool ShowFavoritesOnly = false;
        public int MaxResults = 1000;
        
        public ContentSearchFilter()
        {
            NameFilter = "";
            AssetTypes = new string[0];
            Category = "";
            Tags = new string[0];
            DateFrom = null;
            DateTo = null;
            MinSize = 0;
            MaxSize = long.MaxValue;
            SortBy = ContentSortType.Name;
            SortAscending = true;
            ShowHidden = false;
            ShowFavoritesOnly = false;
            MaxResults = 1000;
        }
        
        /// <summary>
        /// Создать фильтр для поиска по типу ассета
        /// </summary>
        public static ContentSearchFilter ByAssetType(string assetType)
        {
            return new ContentSearchFilter
            {
                AssetTypes = new[] { assetType }
            };
        }
        
        /// <summary>
        /// Создать фильтр для поиска по категории
        /// </summary>
        public static ContentSearchFilter ByCategory(string category)
        {
            return new ContentSearchFilter
            {
                Category = category
            };
        }
        
        /// <summary>
        /// Создать фильтр для поиска по тегам
        /// </summary>
        public static ContentSearchFilter ByTags(params string[] tags)
        {
            return new ContentSearchFilter
            {
                Tags = tags
            };
        }
        
        /// <summary>
        /// Создать фильтр для поиска по имени
        /// </summary>
        public static ContentSearchFilter ByName(string name)
        {
            return new ContentSearchFilter
            {
                NameFilter = name
            };
        }
        
        /// <summary>
        /// Создать фильтр для избранных ассетов
        /// </summary>
        public static ContentSearchFilter Favorites()
        {
            return new ContentSearchFilter
            {
                ShowFavoritesOnly = true
            };
        }
        
        /// <summary>
        /// Создать фильтр для недавно измененных ассетов
        /// </summary>
        public static ContentSearchFilter Recent(int days = 7)
        {
            return new ContentSearchFilter
            {
                DateFrom = DateTime.Now.AddDays(-days),
                SortBy = ContentSortType.Date,
                SortAscending = false
            };
        }
        
        /// <summary>
        /// Создать фильтр для больших файлов
        /// </summary>
        public static ContentSearchFilter LargeFiles(long minSizeMB = 10)
        {
            return new ContentSearchFilter
            {
                MinSize = minSizeMB * 1024 * 1024,
                SortBy = ContentSortType.Size,
                SortAscending = false
            };
        }
        
        /// <summary>
        /// Очистить фильтр
        /// </summary>
        public void Clear()
        {
            NameFilter = "";
            AssetTypes = new string[0];
            Category = "";
            Tags = new string[0];
            DateFrom = null;
            DateTo = null;
            MinSize = 0;
            MaxSize = long.MaxValue;
            SortBy = ContentSortType.Name;
            SortAscending = true;
            ShowHidden = false;
            ShowFavoritesOnly = false;
            MaxResults = 1000;
        }
        
        /// <summary>
        /// Проверить, пустой ли фильтр
        /// </summary>
        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(NameFilter) &&
                   (AssetTypes == null || AssetTypes.Length == 0) &&
                   string.IsNullOrEmpty(Category) &&
                   (Tags == null || Tags.Length == 0) &&
                   !DateFrom.HasValue &&
                   !DateTo.HasValue &&
                   MinSize == 0 &&
                   MaxSize == long.MaxValue &&
                   !ShowFavoritesOnly;
        }
        
        /// <summary>
        /// Клонировать фильтр
        /// </summary>
        public ContentSearchFilter Clone()
        {
            return new ContentSearchFilter
            {
                NameFilter = NameFilter,
                AssetTypes = AssetTypes != null ? (string[])AssetTypes.Clone() : new string[0],
                Category = Category,
                Tags = Tags != null ? (string[])Tags.Clone() : new string[0],
                DateFrom = DateFrom,
                DateTo = DateTo,
                MinSize = MinSize,
                MaxSize = MaxSize,
                SortBy = SortBy,
                SortAscending = SortAscending,
                ShowHidden = ShowHidden,
                ShowFavoritesOnly = ShowFavoritesOnly,
                MaxResults = MaxResults
            };
        }
    }
    
    /// <summary>
    /// Типы сортировки контента
    /// </summary>
    public enum ContentSortType
    {
        Name,
        Date,
        Size,
        Type,
        Category
    }
}
