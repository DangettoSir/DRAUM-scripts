#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DRAUM.Modules.Utilities.FX
{
    [CustomEditor(typeof(VineSystem))]
    [CanEditMultipleObjects]
    public class VineSystemEditor : Editor
    {
        private SerializedProperty _generateOnStart;
        private SerializedProperty _autoUpdateMesh;
        private SerializedProperty _updateInEditor;
        
        private void OnEnable()
        {
            _generateOnStart = serializedObject.FindProperty("generateOnStart");
            _autoUpdateMesh = serializedObject.FindProperty("autoUpdateMesh");
            _updateInEditor = serializedObject.FindProperty("updateInEditor");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(_generateOnStart);
            EditorGUILayout.PropertyField(_autoUpdateMesh);
            EditorGUILayout.PropertyField(_updateInEditor);
            
            serializedObject.ApplyModifiedProperties();
            
            EditorGUILayout.Space(10);
            
            // Проверяем, выбрано ли несколько объектов
            if (targets.Length > 1)
            {
                EditorGUILayout.HelpBox("Multi-object editing: Buttons will affect all selected objects.", MessageType.Info);
            }
            
            EditorGUI.BeginDisabledGroup(targets.Length == 0);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Regenerate", GUILayout.Height(30)))
            {
                foreach (VineSystem vineSystem in targets)
                {
                    if (vineSystem != null)
                    {
                        vineSystem.Regenerate();
                    }
                }
                SceneView.RepaintAll();
            }
            
            if (GUILayout.Button("Update Mesh", GUILayout.Height(30)))
            {
                foreach (VineSystem vineSystem in targets)
                {
                    if (vineSystem != null)
                    {
                        vineSystem.UpdateMesh();
                    }
                }
                SceneView.RepaintAll();
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
        }
    }
    
    [CustomEditor(typeof(VineSplineGenerator))]
    [CanEditMultipleObjects]
    public class VineSplineGeneratorEditor : Editor
    {
        private SerializedProperty _centerPoint;
        private SerializedProperty _searchRadius;
        private SerializedProperty _vineCount;
        private SerializedProperty _vineLength;
        private SerializedProperty _pointSpacing;
        private SerializedProperty _meshLayerMask;
        private SerializedProperty _growthSpeed;
        private SerializedProperty _vineThickness;
        private SerializedProperty _randomVariation;
        private SerializedProperty _stickDistance;
        private SerializedProperty _raycastCount;
        
        private void OnEnable()
        {
            _centerPoint = serializedObject.FindProperty("centerPoint");
            _searchRadius = serializedObject.FindProperty("searchRadius");
            _vineCount = serializedObject.FindProperty("vineCount");
            _vineLength = serializedObject.FindProperty("vineLength");
            _pointSpacing = serializedObject.FindProperty("pointSpacing");
            _meshLayerMask = serializedObject.FindProperty("meshLayerMask");
            _growthSpeed = serializedObject.FindProperty("growthSpeed");
            _vineThickness = serializedObject.FindProperty("vineThickness");
            _randomVariation = serializedObject.FindProperty("randomVariation");
            _stickDistance = serializedObject.FindProperty("stickDistance");
            _raycastCount = serializedObject.FindProperty("raycastCount");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.LabelField("Generation Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_centerPoint);
            EditorGUILayout.PropertyField(_searchRadius);
            EditorGUILayout.PropertyField(_vineCount);
            EditorGUILayout.PropertyField(_vineLength);
            EditorGUILayout.PropertyField(_pointSpacing);
            EditorGUILayout.PropertyField(_meshLayerMask);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Vine Behavior", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_growthSpeed);
            EditorGUILayout.PropertyField(_vineThickness);
            EditorGUILayout.PropertyField(_randomVariation);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mesh Detection", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_stickDistance);
            EditorGUILayout.PropertyField(_raycastCount);
            
            serializedObject.ApplyModifiedProperties();
            
            EditorGUILayout.Space(10);
            
            // Проверяем, выбрано ли несколько объектов
            if (targets.Length > 1)
            {
                EditorGUILayout.HelpBox("Multi-object editing: Button will affect all selected objects.", MessageType.Info);
            }
            
            EditorGUI.BeginDisabledGroup(targets.Length == 0);
            if (GUILayout.Button("Generate Paths", GUILayout.Height(30)))
            {
                foreach (VineSplineGenerator generator in targets)
                {
                    if (generator != null)
                    {
                        generator.GenerateVinePaths();
                    }
                }
                SceneView.RepaintAll();
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
#endif
