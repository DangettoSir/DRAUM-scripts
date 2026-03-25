using UnityEngine;

namespace DRAUM.Modules.Utilities.FX
{
    /// <summary>
    /// Компонент для проекции декаля лозы на поверхности вокруг.
    /// Устанавливает проекционные матрицы в материал для корректной проекции декаля.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class VineDecalProjector : MonoBehaviour
    {
        [Header("Projection Settings")]
        [Tooltip("Размер области проекции декаля")]
        public Vector3 projectionSize = new Vector3(5, 5, 5);
        
        [Tooltip("Материал декаля (если не указан, берется из Renderer)")]
        public Material decalMaterial;
        
        private Renderer _renderer;
        private MaterialPropertyBlock _propertyBlock;
        private Matrix4x4 _projectionMatrix;
        private Matrix4x4 _inverseMatrix;
        
        private static readonly int DecalProjectionMatrixID = Shader.PropertyToID("_DecalProjectionMatrix");
        private static readonly int DecalInverseMatrixID = Shader.PropertyToID("_DecalInverseMatrix");
        private static readonly int ProjectionSizeID = Shader.PropertyToID("_ProjectionSize");
        
        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            
            if (decalMaterial == null)
            {
                decalMaterial = _renderer.sharedMaterial;
            }
            
            _propertyBlock = new MaterialPropertyBlock();
        }
        
        private void OnEnable()
        {
            UpdateProjectionMatrices();
        }
        
        private void Update()
        {
            // Обновляем матрицы каждый кадр на случай изменения трансформа объекта
            UpdateProjectionMatrices();
        }
        
        /// <summary>
        /// Обновляет проекционные матрицы на основе текущего трансформа объекта
        /// </summary>
        private void UpdateProjectionMatrices()
        {
            // Создаем матрицу масштабирования на основе projectionSize
            Matrix4x4 scaleMatrix = Matrix4x4.Scale(projectionSize);
            
            // Получаем матрицу трансформа объекта (world to local)
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            
            // Комбинируем: сначала масштаб, потом трансформация
            _inverseMatrix = worldToLocal * scaleMatrix;
            
            // Обратная матрица для преобразования из локального пространства декаля в world space
            _projectionMatrix = _inverseMatrix.inverse;
            
            // Устанавливаем матрицы в материал
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetMatrix(DecalProjectionMatrixID, _projectionMatrix);
            _propertyBlock.SetMatrix(DecalInverseMatrixID, _inverseMatrix);
            _propertyBlock.SetVector(ProjectionSizeID, projectionSize);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
        
        private void OnDrawGizmosSelected()
        {
            // Рисуем визуализацию области проекции в редакторе
            Gizmos.color = new Color(0.2f, 0.4f, 0.1f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, projectionSize);
            
            Gizmos.color = new Color(0.2f, 0.4f, 0.1f, 1f);
            Gizmos.DrawWireCube(Vector3.zero, projectionSize);
        }
    }
}
