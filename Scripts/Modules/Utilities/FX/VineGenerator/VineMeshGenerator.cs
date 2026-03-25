using System.Collections.Generic;
using UnityEngine;

namespace DRAUM.Modules.Utilities.FX
{
    /// <summary>
    /// Генерирует меш лозы вдоль spline путей
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class VineMeshGenerator : MonoBehaviour
    {
        [Header("Mesh Settings")]
        [Tooltip("Количество сегментов вокруг окружности лозы")]
        public int radialSegments = 8;
        
        [Tooltip("Количество сегментов вдоль длины лозы")]
        public int lengthSegments = 20;
        
        [Tooltip("Толщина лозы")]
        public float thickness = 0.1f;
        
        [Tooltip("Вариация толщины")]
        public float thicknessVariation = 0.02f;
        
        [Header("References")]
        [Tooltip("Генератор spline путей")]
        public VineSplineGenerator splineGenerator;
        
        [Header("Growth Animation")]
        [Tooltip("Анимировать рост лозы")]
        public bool animateGrowth = true;
        
        [Tooltip("Скорость роста")]
        public float growthSpeed = 1f;
        
        [Tooltip("Текущий прогресс роста (0-1)")]
        [Range(0, 1)]
        public float growthProgress = 1f;
        
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh _vineMesh;
        private float _currentGrowthTime = 0f;
        
        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
        }
        
        private void Start()
        {
            if (splineGenerator == null)
                splineGenerator = GetComponent<VineSplineGenerator>();
            
            GenerateMesh();
            
            if (animateGrowth)
            {
                growthProgress = 0f;
                _currentGrowthTime = 0f;
            }
        }
        
        private void Update()
        {
            if (animateGrowth && growthProgress < 1f)
            {
                _currentGrowthTime += Time.deltaTime * growthSpeed;
                growthProgress = Mathf.Clamp01(_currentGrowthTime);
                UpdateMesh();
            }
        }
        
        /// <summary>
        /// Генерирует меш лозы из spline путей
        /// </summary>
        public void GenerateMesh()
        {
            if (splineGenerator == null) return;
            
            List<VineSplinePath> paths = splineGenerator.GetVinePaths();
            if (paths.Count == 0) return;
            
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            
            int vertexOffset = 0;
            
            foreach (VineSplinePath path in paths)
            {
                GenerateVineMesh(path, vertices, triangles, normals, uvs, ref vertexOffset);
            }
            
            _vineMesh = new Mesh();
            _vineMesh.name = "VineMesh";
            _vineMesh.vertices = vertices.ToArray();
            _vineMesh.triangles = triangles.ToArray();
            _vineMesh.normals = normals.ToArray();
            _vineMesh.uv = uvs.ToArray();
            _vineMesh.RecalculateBounds();
            _vineMesh.RecalculateTangents();
            
            _meshFilter.mesh = _vineMesh;
        }
        
        /// <summary>
        /// Генерирует меш для одного пути лозы
        /// </summary>
        private void GenerateVineMesh(VineSplinePath path, List<Vector3> vertices, List<int> triangles, 
            List<Vector3> normals, List<Vector2> uvs, ref int vertexOffset)
        {
            if (path.controlPoints.Count < 2) return;
            
            int segments = Mathf.Max(lengthSegments, path.controlPoints.Count - 1);
            
            // Вычисляем максимальный индекс с учетом роста
            int maxSegment = Mathf.CeilToInt(segments * growthProgress);
            
            for (int i = 0; i <= segments; i++)
            {
                // Пропускаем сегменты, которые еще не выросли
                if (i > maxSegment) break;
                
                float t = i / (float)segments;
                Vector3 position = path.Evaluate(t);
                Vector3 surfaceNormal = path.GetNormal(t);
                
                // Вычисляем направление вдоль пути
                Vector3 forward = Vector3.zero;
                if (i < segments)
                {
                    Vector3 nextPos = path.Evaluate((i + 1) / (float)segments);
                    forward = (nextPos - position).normalized;
                }
                else if (i > 0)
                {
                    Vector3 prevPos = path.Evaluate((i - 1) / (float)segments);
                    forward = (position - prevPos).normalized;
                }
                
                if (forward == Vector3.zero)
                    forward = Vector3.forward;
                
                // Создаем локальную систему координат
                Vector3 right = Vector3.Cross(forward, surfaceNormal).normalized;
                if (right == Vector3.zero)
                    right = Vector3.Cross(forward, Vector3.up).normalized;
                
                Vector3 up = Vector3.Cross(right, forward).normalized;
                
                // Вариация толщины (детерминированная на основе позиции)
                float thicknessNoise = Mathf.PerlinNoise(position.x * 10f, position.z * 10f);
                float currentThickness = thickness + (thicknessNoise - 0.5f) * 2f * thicknessVariation;
                
                // Генерируем кольцо вершин
                for (int j = 0; j <= radialSegments; j++)
                {
                    float angle = (j / (float)radialSegments) * 360f * Mathf.Deg2Rad;
                    Vector3 offset = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * currentThickness;
                    Vector3 vertexPos = position + offset;
                    
                    vertices.Add(vertexPos);
                    normals.Add(offset.normalized);
                    uvs.Add(new Vector2(j / (float)radialSegments, t));
                }
                
                // Создаем треугольники между кольцами
                if (i > 0)
                {
                    int prevRingStart = vertexOffset - (radialSegments + 1);
                    int currentRingStart = vertexOffset;
                    
                    for (int j = 0; j < radialSegments; j++)
                    {
                        int v0 = prevRingStart + j;
                        int v1 = prevRingStart + j + 1;
                        int v2 = currentRingStart + j;
                        int v3 = currentRingStart + j + 1;
                        
                        // Первый треугольник
                        triangles.Add(v0);
                        triangles.Add(v2);
                        triangles.Add(v1);
                        
                        // Второй треугольник
                        triangles.Add(v1);
                        triangles.Add(v2);
                        triangles.Add(v3);
                    }
                }
                
                vertexOffset += radialSegments + 1;
            }
        }
        
        /// <summary>
        /// Обновляет меш (вызывается при изменении путей)
        /// </summary>
        public void UpdateMesh()
        {
            GenerateMesh();
        }
    }
}
