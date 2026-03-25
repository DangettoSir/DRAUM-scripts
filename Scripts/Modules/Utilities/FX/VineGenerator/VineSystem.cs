using UnityEngine;

namespace DRAUM.Modules.Utilities.FX
{
    /// <summary>
    /// Главный компонент системы лозы - объединяет генерацию spline путей и рендеринг меша
    /// </summary>
    [RequireComponent(typeof(VineSplineGenerator), typeof(VineMeshGenerator))]
    public class VineSystem : MonoBehaviour
    {
        [Header("System Settings")]
        [Tooltip("Автоматически генерировать пути при старте")]
        public bool generateOnStart = true;
        
        [Tooltip("Обновлять меш при изменении путей")]
        public bool autoUpdateMesh = true;
        
        [Tooltip("Обновлять в реальном времени в редакторе")]
        public bool updateInEditor = true;
        
        private VineSplineGenerator _splineGenerator;
        private VineMeshGenerator _meshGenerator;
        
        private void Awake()
        {
            _splineGenerator = GetComponent<VineSplineGenerator>();
            _meshGenerator = GetComponent<VineMeshGenerator>();
            
            if (_meshGenerator.splineGenerator == null)
                _meshGenerator.splineGenerator = _splineGenerator;
        }
        
        private void Start()
        {
            if (generateOnStart)
            {
                GenerateVines();
            }
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (updateInEditor && Application.isPlaying && enabled)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null && Application.isPlaying)
                    {
                        GenerateVines();
                    }
                };
            }
        }
#endif
        
        /// <summary>
        /// Генерирует всю систему лозы
        /// </summary>
        public void GenerateVines()
        {
            if (_splineGenerator == null)
                _splineGenerator = GetComponent<VineSplineGenerator>();
            if (_meshGenerator == null)
                _meshGenerator = GetComponent<VineMeshGenerator>();
            
            _splineGenerator.GenerateVinePaths();
            
            if (autoUpdateMesh)
            {
                _meshGenerator.UpdateMesh();
            }
        }
        
        /// <summary>
        /// Обновляет только меш (если пути уже сгенерированы)
        /// </summary>
        public void UpdateMesh()
        {
            if (_meshGenerator == null)
                _meshGenerator = GetComponent<VineMeshGenerator>();
            
            _meshGenerator.UpdateMesh();
        }
        
        /// <summary>
        /// Регенерирует пути и меш
        /// </summary>
        public void Regenerate()
        {
            GenerateVines();
        }
    }
}
