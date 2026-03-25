using UnityEngine;
using UnityEditor;

namespace DRAUM.CMS
{
    /// <summary>
    /// Главное меню CMS системы
    /// </summary>
    public static class CMSMenu
    {
        [MenuItem("DRAUM/CMS/CMS GUI", priority = 1)]
        public static void OpenCMSGUI()
        {
            CMS_GUI.ShowWindow();
        }
        
        [MenuItem("DRAUM/CMS/Open CMS GUI #x", priority = 2)]
        public static void OpenCMSGUIWithShortcut()
        {
            CMS_GUI window = EditorWindow.GetWindow<CMS_GUI>("CMS GUI");
            window.Focus();
        }
        
        [MenuItem("DRAUM/CMS/Create Category", priority = 5)]
        public static void CreateCategory()
        {
            CategoryCreator.ShowWindow();
        }
        
        [MenuItem("DRAUM/CMS/Quick Create/Art Subfolder", priority = 6)]
        public static void CreateArtSubfolder()
        {
            QuickFolderCreator.CreateArtSubfolder();
        }
        
        [MenuItem("DRAUM/CMS/Quick Create/Audio Subfolder", priority = 7)]
        public static void CreateAudioSubfolder()
        {
            QuickFolderCreator.CreateAudioSubfolder();
        }
        
        [MenuItem("DRAUM/CMS/Quick Create/Prefabs Subfolder", priority = 8)]
        public static void CreatePrefabsSubfolder()
        {
            QuickFolderCreator.CreatePrefabsSubfolder();
        }
        
        [MenuItem("DRAUM/CMS/Quick Create/Scenes Subfolder", priority = 9)]
        public static void CreateScenesSubfolder()
        {
            QuickFolderCreator.CreateScenesSubfolder();
        }
        
        [MenuItem("DRAUM/CMS/Organize Content", priority = 10)]
        public static void OrganizeContent()
        {
            ContentOrganizer.OrganizeAllContent();
        }
        
        [MenuItem("DRAUM/CMS/Organize Selected", priority = 11)]
        public static void OrganizeSelected()
        {
            ContentOrganizer.OrganizeSelectedAssets();
        }
        
        [MenuItem("DRAUM/CMS/Create Standard Structure", priority = 12)]
        public static void CreateStandardStructure()
        {
            ContentOrganizer.CreateStandardStructure();
        }
        
        [MenuItem("DRAUM/CMS/Clean Empty Folders", priority = 13)]
        public static void CleanEmptyFolders()
        {
            ContentOrganizer.CleanEmptyFolders();
        }
        
        [MenuItem("DRAUM/CMS/Rescan Database", priority = 20)]
        public static void RescanDatabase()
        {
            ContentDatabase.Instance.RescanAssets();
            Debug.Log("Content database rescanned successfully!");
        }
        
        [MenuItem("DRAUM/CMS/Clear Database", priority = 21)]
        public static void ClearDatabase()
        {
            if (EditorUtility.DisplayDialog("Clear Database", 
                "Are you sure you want to clear the content database?", 
                "Yes", "Cancel"))
            {
                ContentDatabase.Instance.ClearDatabase();
                Debug.Log("Content database cleared!");
            }
        }
        
        [MenuItem("DRAUM/CMS/About CMS", priority = 31)]
        public static void ShowAbout()
        {
            EditorUtility.DisplayDialog("DRAUM Content Management System", 
                "DRAUM CMS v1.0\n\n" +
                "A powerful content management system for Unity projects.\n\n" +
                "Features:\n" +
                "• Smart content organization\n" +
                "• Advanced search and filtering\n" +
                "• Automatic asset categorization\n" +
                "• Content browser with preview\n" +
                "• Batch operations\n\n" +
                "Created for DRAUM project", 
                "OK");
        }
    }
}
