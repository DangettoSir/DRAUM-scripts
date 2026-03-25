using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace DRAUM.CMS
{
    /// <summary>
    /// Главное окно CMS GUI для управления контентом
    /// </summary>
    public class CMS_GUI : EditorWindow
    {
        private ContentDatabase database;
        private ContentSearchFilter currentFilter = new ContentSearchFilter();
        private List<ContentAsset> filteredAssets = new List<ContentAsset>();
        
        // UI Settings
        private Vector2 scrollPosition;
        private Vector2 filterScrollPosition;
        private bool showFilters = true;
        private bool showPreview = true;
        private ContentViewMode viewMode = ContentViewMode.Grid;
        private float gridItemSize = 100f;
        
        // Пагинация
        private int currentPage = 0;
        private int itemsPerPage = 20;
        private int totalPages = 0;
        
        // Вкладки
        private int selectedTab = 0;
        private string[] tabNames = { "Content Browser", "Content Tree", "Statistics" };
        
        // Search
        private string searchQuery = "";
        private string lastSearchQuery = "";
        
        // Selection
        private ContentAsset selectedAsset;
        private List<ContentAsset> selectedAssets = new List<ContentAsset>();
        
        // Layout
        private Rect toolbarRect;
        private Rect filterPanelRect;
        private Rect contentAreaRect;
        private Rect previewPanelRect;
        
        // Resizable panels
        private float filterPanelWidth = 200f;
        private float previewPanelWidth = 200f;
        private bool isResizingFilter = false;
        private bool isResizingPreview = false;
        
        // Drag & Drop
        private ContentAsset draggedAsset;
        private bool isDragging = false;
        
        // Shortcuts
        private bool focusSearchField = false;
        
        public static void ShowWindow()
        {
            CMS_GUI window = GetWindow<CMS_GUI>("CMS GUI");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            database = ContentDatabase.Instance;
            RefreshAssets();
            
       
            ContentDatabase.OnAssetAdded += OnAssetAdded;
            ContentDatabase.OnAssetRemoved += OnAssetRemoved;
            ContentDatabase.OnAssetUpdated += OnAssetUpdated;
        }
        
        private void OnDisable()
        {
            
            ContentDatabase.OnAssetAdded -= OnAssetAdded;
            ContentDatabase.OnAssetRemoved -= OnAssetRemoved;
            ContentDatabase.OnAssetUpdated -= OnAssetUpdated;
        }
        
        private void OnGUI()
        {
            if (database == null)
            {
                database = ContentDatabase.Instance;
                RefreshAssets();
            }
            
          
            HandleShortcuts();
            
            DrawToolbar();
            DrawTabs();
            
            EditorGUILayout.BeginHorizontal();
            
            if (showFilters)
            {
                DrawFilterPanel();
            }
            
            DrawContentArea();
            
            if (showPreview)
            {
                DrawPreviewPanel();
            }
            
            EditorGUILayout.EndHorizontal();
            
         
            if (selectedTab == 0)
            {
                DrawSearchAndPagination();
            }
            
            HandleEvents();
        }
        
        private void DrawToolbar()
        {
            toolbarRect = new Rect(0, 0, position.width, 30);
            GUILayout.BeginArea(toolbarRect);
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            
     
            if (GUILayout.Button("🔄", EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                RefreshAssets();
            }
            
            if (GUILayout.Button("🔍", EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                database.RescanAssets();
                RefreshAssets();
            }
            
            GUILayout.FlexibleSpace();
            
      
            string viewModeText = viewMode == ContentViewMode.Grid ? "⊞" : "☰";
            if (GUILayout.Button(viewModeText, EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                viewMode = viewMode == ContentViewMode.Grid ? ContentViewMode.List : ContentViewMode.Grid;
            }
            
            if (GUILayout.Button(showFilters ? "🔽" : "🔼", EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                showFilters = !showFilters;
            }
            
            if (GUILayout.Button(showPreview ? "👁️" : "👁️‍🗨️", EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                showPreview = !showPreview;
            }
            
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
        
        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            for (int i = 0; i < tabNames.Length; i++)
            {
                GUI.backgroundColor = selectedTab == i ? Color.white : Color.gray;
                if (GUILayout.Button(tabNames[i], EditorStyles.toolbarButton))
                {
                    selectedTab = i;
                    Repaint();
                }
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawFilterPanel()
        {
            if (!showFilters) return;
            
            filterPanelRect = new Rect(0, 30, filterPanelWidth, position.height - 30);
            GUILayout.BeginArea(filterPanelRect, EditorStyles.helpBox);
            GUILayout.Label("Filters", EditorStyles.boldLabel);
            
            filterScrollPosition = GUILayout.BeginScrollView(filterScrollPosition);
            
       
            GUILayout.Label("Asset Types:", EditorStyles.label);
            string[] assetTypes = { "Model", "Texture", "Material", "Prefab", "Animation", "Script", "Shader", "Scene", "Audio", "Asset" };
            foreach (string type in assetTypes)
            {
                bool isSelected = currentFilter.AssetTypes != null && currentFilter.AssetTypes.Contains(type);
                bool newSelected = GUILayout.Toggle(isSelected, type);
                if (newSelected != isSelected)
                {
                    if (newSelected)
                    {
                        if (currentFilter.AssetTypes == null) currentFilter.AssetTypes = new string[0];
                        currentFilter.AssetTypes = currentFilter.AssetTypes.Concat(new[] { type }).ToArray();
                    }
                    else
                    {
                        currentFilter.AssetTypes = currentFilter.AssetTypes.Where(t => t != type).ToArray();
                    }
                    RefreshAssets();
                }
            }
            
            GUILayout.Space(10);
            
      
            GUILayout.Label("Categories:", EditorStyles.label);
            List<string> categories = database.GetUsedCategories();
            foreach (string category in categories)
            {
                bool isSelected = currentFilter.Category == category;
                bool newSelected = GUILayout.Toggle(isSelected, category);
                if (newSelected != isSelected)
                {
                    currentFilter.Category = newSelected ? category : "";
                    RefreshAssets();
                }
            }
            
            GUILayout.Space(10);
            
     
            GUILayout.Label("Tags:", EditorStyles.label);
            List<string> tags = database.GetUsedTags();
            foreach (string tag in tags)
            {
                bool isSelected = currentFilter.Tags != null && currentFilter.Tags.Contains(tag);
                bool newSelected = GUILayout.Toggle(isSelected, tag);
                if (newSelected != isSelected)
                {
                    if (newSelected)
                    {
                        if (currentFilter.Tags == null) currentFilter.Tags = new string[0];
                        currentFilter.Tags = currentFilter.Tags.Concat(new[] { tag }).ToArray();
                    }
                    else
                    {
                        currentFilter.Tags = currentFilter.Tags.Where(t => t != tag).ToArray();
                    }
                    RefreshAssets();
                }
            }
            
            GUILayout.Space(10);
            
        
            GUILayout.Label("Additional Filters:", EditorStyles.label);
            currentFilter.ShowFavoritesOnly = GUILayout.Toggle(currentFilter.ShowFavoritesOnly, "Favorites Only");
            currentFilter.ShowHidden = GUILayout.Toggle(currentFilter.ShowHidden, "Show Hidden");
            
            GUILayout.Space(10);
            
        
            GUILayout.Label("Quick Filters:", EditorStyles.label);
            if (GUILayout.Button("Recent (7 days)"))
            {
                currentFilter = ContentSearchFilter.Recent(7);
                RefreshAssets();
            }
            
            if (GUILayout.Button("Large Files"))
            {
                currentFilter = ContentSearchFilter.LargeFiles(10);
                RefreshAssets();
            }
            
            if (GUILayout.Button("Clear Filters"))
            {
                currentFilter.Clear();
                RefreshAssets();
            }
            
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            
         
            DrawResizeHandle(filterPanelRect, ref filterPanelWidth, ref isResizingFilter, true);
        }
        
        private void DrawContentArea()
        {
            float filterWidth = showFilters ? filterPanelWidth : 0;
            float previewWidth = showPreview ? previewPanelWidth : 0;
            
      
            float leftMargin = 10f;
            float rightMargin = 10f;
            
            contentAreaRect = new Rect(filterWidth + leftMargin, 60, position.width - filterWidth - previewWidth - leftMargin - rightMargin, position.height - 90);
            
            GUILayout.BeginArea(contentAreaRect);
            
            if (selectedTab == 0)
            {
         
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Found {filteredAssets.Count} assets", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label($"Selected: {selectedAssets.Count}");
                GUILayout.EndHorizontal();
                
           
                if (filteredAssets.Count > 0)
                {
                    GUILayout.BeginHorizontal();
                    var assetTypes = filteredAssets.GroupBy(a => a.AssetType).ToDictionary(g => g.Key, g => g.Count());
                    foreach (var kvp in assetTypes.Take(5))
                    {
                        GUILayout.Label($"{kvp.Key}: {kvp.Value}", EditorStyles.miniLabel);
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"Total Size: {GetFormattedTotalSize()}", EditorStyles.miniLabel);
                    GUILayout.EndHorizontal();
                }
                
             
                if (viewMode == ContentViewMode.Grid)
                {
                    DrawGridView();
                }
                else
                {
                    DrawListView();
                }
            }
            else if (selectedTab == 1)
            {
                DrawContentTree();
            }
            else if (selectedTab == 2)
            {
                DrawStatistics();
            }
            
            GUILayout.EndArea();
        }
        
        private void DrawContentTree()
        {
            EditorGUILayout.LabelField("Content Tree", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
         
            DrawFolderTree("Assets/_Project", 0);
            
            EditorGUILayout.Space(10);
            
            
            EditorGUILayout.LabelField("Quick Actions:", EditorStyles.label);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Art Subfolder"))
            {
                QuickFolderCreator.CreateArtSubfolder();
            }
            if (GUILayout.Button("Create Audio Subfolder"))
            {
                QuickFolderCreator.CreateAudioSubfolder();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Prefabs Subfolder"))
            {
                QuickFolderCreator.CreatePrefabsSubfolder();
            }
            if (GUILayout.Button("Create Scenes Subfolder"))
            {
                QuickFolderCreator.CreateScenesSubfolder();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
        
            if (GUILayout.Button("Create New Category"))
            {
                CategoryCreator.ShowWindow();
            }
            
            EditorGUILayout.Space(10);
            
            
            EditorGUILayout.LabelField("Organization:", EditorStyles.label);
            
            if (GUILayout.Button("Organize Content"))
            {
                ContentOrganizer.OrganizeAllContent();
            }
            
            if (GUILayout.Button("Clean Empty Folders"))
            {
                ContentOrganizer.CleanEmptyFolders();
            }
        }
        
        private void DrawStatistics()
        {
            EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
          
            EditorGUILayout.LabelField($"Total Assets: {filteredAssets.Count}");
            EditorGUILayout.LabelField($"Selected Assets: {selectedAssets.Count}");
            
            EditorGUILayout.Space(10);
            
         
            EditorGUILayout.LabelField("By Asset Type:", EditorStyles.boldLabel);
            var typeStats = filteredAssets.GroupBy(a => a.AssetType).OrderByDescending(g => g.Count());
            foreach (var group in typeStats)
            {
                EditorGUILayout.LabelField($"{group.Key}: {group.Count()}");
            }
            
            EditorGUILayout.Space(10);
            
        
            EditorGUILayout.LabelField("By Category:", EditorStyles.boldLabel);
            var categoryStats = filteredAssets.GroupBy(a => a.Category).OrderByDescending(g => g.Count());
            foreach (var group in categoryStats)
            {
                EditorGUILayout.LabelField($"{group.Key}: {group.Count()}");
            }
        }
        
        private void DrawSearchAndPagination()
        {
            EditorGUILayout.BeginHorizontal();
            
          
            GUILayout.Label("🔍", GUILayout.Width(20));
            
           
            if (focusSearchField)
            {
                GUI.FocusControl("SearchField");
                focusSearchField = false;
            }
            
            GUI.SetNextControlName("SearchField");
            string newSearchQuery = GUILayout.TextField(searchQuery, EditorStyles.toolbarTextField, GUILayout.Width(200));
            if (newSearchQuery != searchQuery)
            {
                searchQuery = newSearchQuery;
                if (searchQuery != lastSearchQuery)
                {
                    lastSearchQuery = searchQuery;
                    currentFilter.NameFilter = searchQuery;
                    RefreshAssets();
                }
            }
            
            GUILayout.FlexibleSpace();
            
            
            if (totalPages > 1)
            {
               
                GUI.enabled = currentPage > 0;
                if (GUILayout.Button("◀", EditorStyles.miniButton, GUILayout.Width(30)))
                {
                    currentPage--;
                }
                GUI.enabled = true;
                
              
                int startPage = Mathf.Max(0, currentPage - 2);
                int endPage = Mathf.Min(totalPages - 1, currentPage + 2);
                
                for (int i = startPage; i <= endPage; i++)
                {
                    GUI.backgroundColor = i == currentPage ? Color.white : Color.gray;
                    if (GUILayout.Button((i + 1).ToString(), EditorStyles.miniButton, GUILayout.Width(30)))
                    {
                        currentPage = i;
                    }
                }
                GUI.backgroundColor = Color.white;
                
                
                GUI.enabled = currentPage < totalPages - 1;
                if (GUILayout.Button("▶", EditorStyles.miniButton, GUILayout.Width(30)))
                {
                    currentPage++;
                }
                GUI.enabled = true;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawGridView()
        {
            
            float availableWidth = contentAreaRect.width - 20;
            int itemsPerRow = Mathf.FloorToInt(availableWidth / (gridItemSize + 10));
            if (itemsPerRow < 1) itemsPerRow = 1;
            
            int startIndex = currentPage * itemsPerPage;
            int endIndex = Mathf.Min(startIndex + itemsPerPage, filteredAssets.Count);
            
            
            GUILayout.BeginVertical();
            
            for (int i = startIndex; i < endIndex; i++)
            {
                GUILayout.BeginHorizontal();
                
                
                for (int j = 0; j < itemsPerRow && (i + j) < endIndex; j++)
                {
                    ContentAsset asset = filteredAssets[i + j];
                    DrawGridItem(asset);
                    
                    
                    if (j < itemsPerRow - 1 && (i + j + 1) < endIndex)
                    {
                        GUILayout.Space(10);
                    }
                }
                
                GUILayout.EndHorizontal();
                
                
                if (i + itemsPerRow < endIndex)
                {
                    GUILayout.Space(10);
                }
                
                
                i += itemsPerRow - 1;
            }
            
            GUILayout.EndVertical();
        }
        
        private void DrawGridItem(ContentAsset asset)
        {
            GUILayout.BeginVertical(GUILayout.Width(gridItemSize), GUILayout.Height(gridItemSize + 40));
            
            
            Rect previewRect = GUILayoutUtility.GetRect(gridItemSize, gridItemSize);
            bool isSelected = selectedAssets.Contains(asset);
            
            if (isSelected)
            {
                EditorGUI.DrawRect(previewRect, Color.blue);
            }
            
            if (asset.Preview != null)
            {
                GUI.DrawTexture(previewRect, asset.Preview, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUI.DrawRect(previewRect, Color.gray);
                GUI.Label(previewRect, asset.AssetType, EditorStyles.centeredGreyMiniLabel);
            }
            
            
            HandleDragAndDrop(previewRect, asset);
            
           
            if (Event.current.type == EventType.MouseDown && previewRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.clickCount == 2)
                {
                    
                    OpenAssetInProject(asset);
                    Event.current.Use();
                }
                else if (Event.current.control)
                {
                    
                    if (isSelected)
                        selectedAssets.Remove(asset);
                    else
                        selectedAssets.Add(asset);
                }
                else
                {
                    
                    selectedAssets.Clear();
                    selectedAssets.Add(asset);
                }
                
                selectedAsset = asset;
                Repaint();
                Event.current.Use();
            }
            
            
            GUILayout.Label(asset.Name, EditorStyles.miniLabel, GUILayout.Height(20));
            
            
            GUILayout.Label(asset.GetFormattedFileSize(), EditorStyles.miniLabel);
            
            GUILayout.EndVertical();
        }
        
        private void DrawListView()
        {
            int startIndex = currentPage * itemsPerPage;
            int endIndex = Mathf.Min(startIndex + itemsPerPage, filteredAssets.Count);
            
            for (int i = startIndex; i < endIndex; i++)
            {
                DrawListItem(filteredAssets[i]);
                
                
                if (i < endIndex - 1)
                {
                    GUILayout.Space(5);
                }
            }
        }
        
        private void DrawListItem(ContentAsset asset)
        {
            GUILayout.BeginHorizontal();
            
            bool isSelected = selectedAssets.Contains(asset);
            bool newSelected = GUILayout.Toggle(isSelected, "", GUILayout.Width(20));
            if (newSelected != isSelected)
            {
                if (newSelected)
                    selectedAssets.Add(asset);
                else
                    selectedAssets.Remove(asset);
            }
            
            
            if (asset.Preview != null)
            {
                GUILayout.Label(asset.Preview, GUILayout.Width(32), GUILayout.Height(32));
            }
            else
            {
                GUILayout.Box("", GUILayout.Width(32), GUILayout.Height(32));
            }
            
            
            GUILayout.BeginVertical();
            GUILayout.Label(asset.Name, EditorStyles.boldLabel);
            GUILayout.Label($"{asset.AssetType} | {asset.Category} | {asset.GetFormattedFileSize()}", EditorStyles.miniLabel);
            if (!string.IsNullOrEmpty(asset.Description))
            {
                GUILayout.Label(asset.Description, EditorStyles.miniLabel);
            }
            GUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            
            
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asset.Path);
            }
            
            if (GUILayout.Button("Open", GUILayout.Width(60)))
            {
                AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asset.Path));
            }
            
            GUILayout.EndHorizontal();
        }
        
        private void DrawPreviewPanel()
        {
            if (!showPreview) return;
            
            previewPanelRect = new Rect(position.width - previewPanelWidth, 30, previewPanelWidth, position.height - 30);
            GUILayout.BeginArea(previewPanelRect, EditorStyles.helpBox);
            
            GUILayout.Label("Preview", EditorStyles.boldLabel);
            
            if (selectedAsset != null)
            {
                GUILayout.Label($"Name: {selectedAsset.Name}");
                GUILayout.Label($"Type: {selectedAsset.AssetType}");
                GUILayout.Label($"Category: {selectedAsset.Category}");
                GUILayout.Label($"Size: {selectedAsset.GetFormattedFileSize()}");
                GUILayout.Label($"Modified: {selectedAsset.LastModified:yyyy-MM-dd HH:mm}");
                
                if (!string.IsNullOrEmpty(selectedAsset.Description))
                {
                    GUILayout.Space(10);
                    GUILayout.Label("Description:", EditorStyles.boldLabel);
                    GUILayout.Label(selectedAsset.Description, EditorStyles.wordWrappedLabel);
                }
                
                if (selectedAsset.Tags.Count > 0)
                {
                    GUILayout.Space(10);
                    GUILayout.Label("Tags:", EditorStyles.boldLabel);
                    foreach (string tag in selectedAsset.Tags)
                    {
                        GUILayout.Label($"• {tag}", EditorStyles.miniLabel);
                    }
                }
            }
            else
            {
                GUILayout.Label("Select an asset to preview");
            }
            
            GUILayout.EndArea();
            
            
            DrawResizeHandle(previewPanelRect, ref previewPanelWidth, ref isResizingPreview, false);
        }
        
        private void HandleEvents()
        {
            Event e = Event.current;
            
            if (e.type == EventType.MouseDown)
            {
                if (e.button == 0)
                {
                    
                    HandleAssetClick(e.mousePosition);
                }
            }
            
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Delete && selectedAssets.Count > 0)
                {
                   
                    DeleteSelectedAssets();
                }
                
                if (e.keyCode == KeyCode.A && e.control)
                {
                    
                    selectedAssets.Clear();
                    selectedAssets.AddRange(filteredAssets);
                }
            }
        }
        
        private void HandleAssetClick(Vector2 mousePosition)
        {
            
        }
        
        private void DeleteSelectedAssets()
        {
            if (EditorUtility.DisplayDialog("Delete Assets", 
                $"Are you sure you want to delete {selectedAssets.Count} assets?", 
                "Delete", "Cancel"))
            {
                foreach (ContentAsset asset in selectedAssets)
                {
                    if (File.Exists(asset.Path))
                    {
                        AssetDatabase.DeleteAsset(asset.Path);
                        database.RemoveAsset(asset.GUID);
                    }
                }
                
                selectedAssets.Clear();
                RefreshAssets();
            }
        }
        
        private void RefreshAssets()
        {
            filteredAssets = database.SearchAssets(currentFilter);
            
            
            totalPages = Mathf.CeilToInt((float)filteredAssets.Count / itemsPerPage);
            if (currentPage >= totalPages)
                currentPage = Mathf.Max(0, totalPages - 1);
        }
        
        private void OnAssetAdded(ContentAsset asset)
        {
            RefreshAssets();
            Repaint();
        }
        
        private void OnAssetRemoved(ContentAsset asset)
        {
            selectedAssets.Remove(asset);
            if (selectedAsset == asset)
                selectedAsset = null;
            
            RefreshAssets();
            Repaint();
        }
        
        private void OnAssetUpdated(ContentAsset asset)
        {
            RefreshAssets();
            Repaint();
        }
        
        private string GetFormattedTotalSize()
        {
            long totalSize = filteredAssets.Sum(a => a.FileSize);
            
            if (totalSize < 1024)
                return $"{totalSize} B";
            else if (totalSize < 1024 * 1024)
                return $"{totalSize / 1024.0:F1} KB";
            else if (totalSize < 1024 * 1024 * 1024)
                return $"{totalSize / (1024.0 * 1024.0):F1} MB";
            else
                return $"{totalSize / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }
        
        private void DrawFolderTree(string folderPath, int indentLevel)
        {
            if (!Directory.Exists(folderPath)) return;
            
            string folderName = Path.GetFileName(folderPath);
            if (string.IsNullOrEmpty(folderName)) folderName = "_Project";
            
            
            string indent = new string(' ', indentLevel * 4);
            string icon = Directory.GetDirectories(folderPath).Length > 0 ? "📁" : "📄";
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{indent}{icon} {folderName}", EditorStyles.label);
            GUILayout.FlexibleSpace();
            
            
            if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                CreateSubfolderInPath(folderPath);
            }
            
            
            if (folderPath != "Assets/_Project" && GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                DeleteFolderWithConfirmation(folderPath);
            }
            EditorGUILayout.EndHorizontal();
            
            
            string[] subfolders = Directory.GetDirectories(folderPath);
            foreach (string subfolder in subfolders)
            {
                DrawFolderTree(subfolder, indentLevel + 1);
            }
        }
        
        private void CreateSubfolderInPath(string parentPath)
        {
            string newFolderName = EditorInputDialog.Show("Create Subfolder", "Enter folder name:", "NewFolder");
            if (!string.IsNullOrEmpty(newFolderName))
            {
                string newPath = Path.Combine(parentPath, newFolderName);
                if (!Directory.Exists(newPath))
                {
                    Directory.CreateDirectory(newPath);
                    AssetDatabase.Refresh();
                    Debug.Log($"Created folder: {newPath}");
                }
            }
        }
        
        private void DeleteFolderWithConfirmation(string folderPath)
        {
            string folderName = Path.GetFileName(folderPath);
            if (EditorUtility.DisplayDialog("Delete Folder", 
                $"Are you sure you want to delete the folder '{folderName}'?\n\nThis will permanently delete the folder and all its contents!", 
                "Delete", "Cancel"))
            {
                try
                {
                    
                    AssetDatabase.DeleteAsset(folderPath);
                    AssetDatabase.Refresh();
                    Debug.Log($"Deleted folder: {folderPath}");
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("Error", $"Failed to delete folder: {e.Message}", "OK");
                }
            }
        }
        
        private void OpenAssetInProject(ContentAsset asset)
        {
            if (string.IsNullOrEmpty(asset.Path)) return;
            
            
            UnityEngine.Object assetObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asset.Path);
            if (assetObject != null)
            {
                EditorGUIUtility.PingObject(assetObject);
                Selection.activeObject = assetObject;
            }
        }
        
        private void HandleDragAndDrop(Rect rect, ContentAsset asset)
        {
            Event e = Event.current;
            
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (rect.Contains(e.mousePosition) && e.button == 0)
                    {
                        draggedAsset = asset;
                        isDragging = true;
                    }
                    break;
                    
                case EventType.MouseDrag:
                    if (isDragging && draggedAsset == asset)
                    {
                        
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.SetGenericData("CMS_Asset", asset);
                        
                        
                        UnityEngine.Object assetObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asset.Path);
                        if (assetObject != null)
                        {
                            DragAndDrop.objectReferences = new UnityEngine.Object[] { assetObject };
                            DragAndDrop.StartDrag(asset.Name);
                        }
                        
                        e.Use();
                    }
                    break;
                    
                case EventType.MouseUp:
                    if (isDragging && draggedAsset == asset)
                    {
                        isDragging = false;
                        draggedAsset = null;
                    }
                    break;
            }
        }
        
        private void HandleShortcuts()
        {
            Event e = Event.current;
            
            if (e.type == EventType.KeyDown)
            {
                
                if (e.control && e.keyCode == KeyCode.S)
                {
                    focusSearchField = true;
                    e.Use();
                }
            }
        }
        
        private void DrawResizeHandle(Rect panelRect, ref float panelWidth, ref bool isResizing, bool isLeft)
        {
            Rect handleRect = isLeft ? 
                new Rect(panelRect.x + panelRect.width - 5, panelRect.y, 10, panelRect.height) :
                new Rect(panelRect.x - 5, panelRect.y, 10, panelRect.height);
            
            EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeHorizontal);
            
            if (Event.current.type == EventType.MouseDown && handleRect.Contains(Event.current.mousePosition))
            {
                isResizing = true;
                Event.current.Use();
            }
            
            if (isResizing && Event.current.type == EventType.MouseDrag)
            {
                float delta = Event.current.delta.x;
                if (isLeft)
                    panelWidth += delta;
                else
                    panelWidth -= delta;
                
                panelWidth = Mathf.Clamp(panelWidth, 150f, position.width * 0.5f);
                Repaint();
                Event.current.Use();
            }
            
            if (Event.current.type == EventType.MouseUp)
            {
                isResizing = false;
            }
            
            
            EditorGUI.DrawRect(handleRect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }
    }
    
    /// <summary>
    /// Режимы просмотра контента
    /// </summary>
    public enum ContentViewMode
    {
        Grid,
        List
    }
    
}
