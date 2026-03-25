#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DRAUM.Modules.Utilities.FX
{
    [CustomEditor(typeof(VineGenerator))]
    [CanEditMultipleObjects]
    public class VineGeneratorEditor : Editor
    {
        private SerializedProperty _centerPoint;
        private SerializedProperty _searchRadius;
        private SerializedProperty _vineCount;
        private SerializedProperty _vineLength;
        private SerializedProperty _verticalLength;
        private SerializedProperty _pointSpacing;
        private SerializedProperty _meshLayerMask;
        private SerializedProperty _growthSpeed;
        private SerializedProperty _vineThickness;
        private SerializedProperty _randomVariation;
        private SerializedProperty _wrapSmoothness;
        private SerializedProperty _surfacePrediction;
        private SerializedProperty _stickDistance;
        private SerializedProperty _raycastCount;
        private SerializedProperty _surfaceOffset;
        private SerializedProperty _radialSegments;
        private SerializedProperty _lengthSegments;
        private SerializedProperty _thicknessVariation;
        private SerializedProperty _animateGrowth;
        private SerializedProperty _growthProgress;
        private SerializedProperty _generateOnStart;
        private SerializedProperty _meshOffset;
        private SerializedProperty _useLocalSpace;
        
        private void OnEnable()
        {
            _centerPoint = serializedObject.FindProperty("centerPoint");
            _searchRadius = serializedObject.FindProperty("searchRadius");
            _vineCount = serializedObject.FindProperty("vineCount");
            _vineLength = serializedObject.FindProperty("vineLength");
            _verticalLength = serializedObject.FindProperty("verticalLength");
            _pointSpacing = serializedObject.FindProperty("pointSpacing");
            _meshLayerMask = serializedObject.FindProperty("meshLayerMask");
            _growthSpeed = serializedObject.FindProperty("growthSpeed");
            _vineThickness = serializedObject.FindProperty("vineThickness");
            _randomVariation = serializedObject.FindProperty("randomVariation");
            _wrapSmoothness = serializedObject.FindProperty("wrapSmoothness");
            _surfacePrediction = serializedObject.FindProperty("surfacePrediction");
            _stickDistance = serializedObject.FindProperty("stickDistance");
            _raycastCount = serializedObject.FindProperty("raycastCount");
            _surfaceOffset = serializedObject.FindProperty("surfaceOffset");
            _radialSegments = serializedObject.FindProperty("radialSegments");
            _lengthSegments = serializedObject.FindProperty("lengthSegments");
            _thicknessVariation = serializedObject.FindProperty("thicknessVariation");
            _animateGrowth = serializedObject.FindProperty("animateGrowth");
            _growthProgress = serializedObject.FindProperty("growthProgress");
            _generateOnStart = serializedObject.FindProperty("generateOnStart");
            _meshOffset = serializedObject.FindProperty("meshOffset");
            _useLocalSpace = serializedObject.FindProperty("useLocalSpace");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            VineGenerator generator = (VineGenerator)target;
            
            // Заголовок и информация
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Vine Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Генератор лозы с spline путями. Работает в редакторе и в игре.", MessageType.Info);
            
            if (targets.Length > 1)
            {
                EditorGUILayout.HelpBox($"Выбрано объектов: {targets.Length}. Кнопки применятся ко всем.", MessageType.Info);
            }
            
            EditorGUILayout.Space(10);
            
            // КНОПКИ УПРАВЛЕНИЯ
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Управление", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Generate", GUILayout.Height(35)))
            {
                foreach (VineGenerator gen in targets)
                {
                    if (gen != null)
                    {
                        gen.GenerateVines();
                    }
                }
                SceneView.RepaintAll();
            }
            
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Regenerate", GUILayout.Height(35)))
            {
                foreach (VineGenerator gen in targets)
                {
                    if (gen != null)
                    {
                        gen.GenerateVines();
                    }
                }
                SceneView.RepaintAll();
            }
            
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Generate Paths Only", GUILayout.Height(35)))
            {
                foreach (VineGenerator gen in targets)
                {
                    if (gen != null)
                    {
                        gen.GenerateVinePaths();
                    }
                }
                SceneView.RepaintAll();
            }
            
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Generate Mesh Only", GUILayout.Height(35)))
            {
                foreach (VineGenerator gen in targets)
                {
                    if (gen != null)
                    {
                        gen.GenerateMesh();
                    }
                }
                SceneView.RepaintAll();
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // НАСТРОЙКИ ГЕНЕРАЦИИ
            EditorGUILayout.LabelField("Generation Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_centerPoint);
            EditorGUILayout.PropertyField(_searchRadius);
            EditorGUILayout.PropertyField(_vineCount);
            EditorGUILayout.PropertyField(_vineLength);
            EditorGUILayout.PropertyField(_verticalLength);
            EditorGUILayout.PropertyField(_pointSpacing);
            EditorGUILayout.PropertyField(_meshLayerMask);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Vine Behavior", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_growthSpeed);
            EditorGUILayout.PropertyField(_vineThickness);
            EditorGUILayout.PropertyField(_randomVariation);
            EditorGUILayout.PropertyField(_wrapSmoothness);
            EditorGUILayout.PropertyField(_surfacePrediction);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mesh Detection", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_stickDistance);
            EditorGUILayout.PropertyField(_raycastCount);
            EditorGUILayout.PropertyField(_surfaceOffset);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mesh Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_radialSegments);
            EditorGUILayout.PropertyField(_lengthSegments);
            EditorGUILayout.PropertyField(_thicknessVariation);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Growth Animation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_animateGrowth);
            if (_animateGrowth.boolValue)
            {
                EditorGUILayout.PropertyField(_growthProgress);
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("System Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_generateOnStart);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mesh Positioning", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_meshOffset);
            EditorGUILayout.PropertyField(_useLocalSpace);
            
            serializedObject.ApplyModifiedProperties();
            
            // Статистика
            if (Application.isPlaying == false && generator.GetVinePaths() != null && generator.GetVinePaths().Count > 0)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Статистика", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Путей сгенерировано: {generator.GetVinePaths().Count}");
                EditorGUILayout.EndVertical();
            }
        }
    }
}
#endif
