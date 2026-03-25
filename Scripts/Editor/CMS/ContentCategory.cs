using System;
using UnityEngine;

namespace DRAUM.CMS
{
    /// <summary>
    /// Модель категории контента
    /// </summary>
    [System.Serializable]
    public class ContentCategory
    {
        [Header("Category Info")]
        public string Name;
        public string Description;
        public Color Color = Color.white;
        public Texture2D Icon;
        
        [Header("Settings")]
        public bool IsDefault;
        public bool IsHidden;
        public int SortOrder;
        
        [Header("Statistics")]
        public int AssetCount;
        public long TotalSize;
        
        public ContentCategory()
        {
            Name = "New Category";
            Description = "";
            Color = Color.white;
            IsDefault = false;
            IsHidden = false;
            SortOrder = 0;
            AssetCount = 0;
            TotalSize = 0;
        }
        
        public ContentCategory(string name, string description = "", Color? color = null)
        {
            Name = name;
            Description = description;
            Color = color ?? Color.white;
            IsDefault = false;
            IsHidden = false;
            SortOrder = 0;
            AssetCount = 0;
            TotalSize = 0;
        }
    }
}
