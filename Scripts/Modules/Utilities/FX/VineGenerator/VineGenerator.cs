using System.Collections.Generic;
using UnityEngine;

namespace DRAUM.Modules.Utilities.FX
{
    /// <summary>
    /// Единый компонент для генерации лозы - объединяет все функции в одном месте
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class VineGenerator : MonoBehaviour
    {
        [Header("Generation Settings")]
        [Tooltip("Центр генерации лозы")]
        public Transform centerPoint;
        
        [Tooltip("Радиус поиска мешей")]
        public float searchRadius = 10f;
        
        [Tooltip("Количество витков лозы")]
        public int vineCount = 8;
        
        [Tooltip("Длина каждого витка")]
        public float vineLength = 5f;
        
        [Tooltip("Вертикальная длина роста (максимальное вертикальное расстояние от стартовой точки)")]
        public float verticalLength = 3f;
        
        [Tooltip("Расстояние между точками на spline")]
        public float pointSpacing = 0.2f;
        
        [Tooltip("Слой для поиска мешей")]
        public LayerMask meshLayerMask = -1;
        
        [Header("Vine Behavior")]
        [Tooltip("Скорость роста лозы")]
        public float growthSpeed = 1f;
        
        [Tooltip("Толщина лозы")]
        public float vineThickness = 0.1f;
        
        [Tooltip("Случайное отклонение для витков")]
        public float randomVariation = 0.5f;
        
        [Tooltip("Плавность обвития объектов (чем больше, тем плавнее)")]
        [Range(0.1f, 2f)]
        public float wrapSmoothness = 0.8f;
        
        [Tooltip("Предсказание формы поверхности для плавного обхода")]
        [Range(0f, 1f)]
        public float surfacePrediction = 0.5f;
        
        [Header("Mesh Detection")]
        [Tooltip("Максимальное расстояние для прилипания к мешу")]
        public float stickDistance = 0.5f;
        
        [Tooltip("Количество лучей для поиска поверхности")]
        public int raycastCount = 16;
        
        [Tooltip("Смещение лозы наружу от поверхности (чтобы не утопала в объект)")]
        public float surfaceOffset = 0.01f;
        
        [Header("Mesh Settings")]
        [Tooltip("Количество сегментов вокруг окружности лозы")]
        public int radialSegments = 8;
        
        [Tooltip("Количество сегментов вдоль длины лозы")]
        public int lengthSegments = 20;
        
        [Tooltip("Вариация толщины")]
        public float thicknessVariation = 0.02f;
        
        [Header("Growth Animation")]
        [Tooltip("Анимировать рост лозы")]
        public bool animateGrowth = false;
        
        [Tooltip("Текущий прогресс роста (0-1)")]
        [Range(0, 1)]
        public float growthProgress = 1f;
        
        [Header("System Settings")]
        [Tooltip("Автоматически генерировать при старте")]
        public bool generateOnStart = true;
        
        [Header("Mesh Positioning")]
        [Tooltip("Смещение меша относительно центра генерации")]
        public Vector3 meshOffset = Vector3.zero;
        
        [Tooltip("Использовать локальное пространство объекта для позиционирования меша")]
        public bool useLocalSpace = true;
        
        // Приватные поля
        private List<VineSplinePath> _vinePaths = new List<VineSplinePath>();
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh _vineMesh;
        private float _currentGrowthTime = 0f;
        
        // Кеш для физических операций (избегаем аллокаций)
        private Collider[] _colliderCache = new Collider[64];
        private RaycastHit[] _raycastHitCache = new RaycastHit[32];
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        /// <summary>
        /// Инициализирует компоненты (вызывается в Awake и при необходимости в редакторе)
        /// </summary>
        private void InitializeComponents()
        {
            if (_meshFilter == null)
                _meshFilter = GetComponent<MeshFilter>();
            if (_meshRenderer == null)
                _meshRenderer = GetComponent<MeshRenderer>();
            
            if (centerPoint == null)
                centerPoint = transform;
        }
        
        private void Start()
        {
            if (generateOnStart)
            {
                GenerateVines();
            }
            
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
                GenerateMesh();
            }
        }
        
        /// <summary>
        /// Генерирует всю систему лозы (пути + меш)
        /// </summary>
        public void GenerateVines()
        {
            InitializeComponents(); // Убеждаемся, что компоненты инициализированы
            GenerateVinePaths();
            GenerateMesh();
        }
        
        /// <summary>
        /// Генерирует только пути (без меша)
        /// </summary>
        public void GenerateVinePaths()
        {
            _vinePaths.Clear();
            FindNearbyMeshes();
            
            Vector3 centerPos = centerPoint.position;
            
            for (int i = 0; i < vineCount; i++)
            {
                // Для каждого витка находим свою стартовую точку с учетом направления
                Vector3 vineDirection = GetRandomDirection(i);
                
                // Находим ближайшую точку на поверхности в направлении витка
                Vector3 startPos = FindClosestSurfacePointInDirection(centerPos, vineDirection);
                Vector3 startNormal = GetSurfaceNormal(startPos);
                
                // Применяем смещение наружу от поверхности к стартовой точке
                startPos = startPos + startNormal * surfaceOffset;
                
                VineSplinePath path = GenerateSingleVinePath(i, startPos, startNormal, vineDirection);
                if (path != null && path.controlPoints.Count > 1)
                {
                    _vinePaths.Add(path);
                }
            }
        }
        
        /// <summary>
        /// Генерирует только меш (используя существующие пути)
        /// </summary>
        public void GenerateMesh()
        {
            InitializeComponents(); // Убеждаемся, что компоненты инициализированы
            
            if (_meshFilter == null)
            {
                Debug.LogError("MeshFilter не найден на объекте " + gameObject.name);
                return;
            }
            
            if (_vinePaths.Count == 0) return;
            
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            
            int vertexOffset = 0;
            
            foreach (VineSplinePath path in _vinePaths)
            {
                GenerateVineMesh(path, vertices, triangles, normals, uvs, ref vertexOffset);
            }
            
            if (vertices.Count == 0) return;
            
            // ВАЖНО: Преобразуем все позиции из мировых координат в локальные координаты объекта
            // Контрольные точки spline хранятся в мировых координатах, но меш должен быть в локальных
            for (int i = 0; i < vertices.Count; i++)
            {
                // Преобразуем мировую позицию в локальную позицию объекта
                vertices[i] = transform.InverseTransformPoint(vertices[i]);
                // Преобразуем нормали тоже
                normals[i] = transform.InverseTransformDirection(normals[i]).normalized;
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
        /// Получает все сгенерированные пути
        /// </summary>
        public List<VineSplinePath> GetVinePaths()
        {
            return _vinePaths;
        }
        
        // ========== ПРИВАТНЫЕ МЕТОДЫ (скопированы из VineSplineGenerator и VineMeshGenerator) ==========
        
        private void FindNearbyMeshes()
        {
            int count = Physics.OverlapSphereNonAlloc(centerPoint.position, searchRadius, _colliderCache, meshLayerMask);
            // Просто проверяем наличие коллайдеров, детали не нужны
        }
        
        /// <summary>
        /// Находит ближайшую точку на поверхности в заданном направлении
        /// </summary>
        private Vector3 FindClosestSurfacePointInDirection(Vector3 position, Vector3 direction)
        {
            Vector3 closestPoint = position;
            float closestDistance = float.MaxValue;
            
            RaycastHit hit;
            
            // Сначала пробуем raycast в заданном направлении
            if (Physics.Raycast(position, direction.normalized, out hit, searchRadius, meshLayerMask))
            {
                return hit.point;
            }
            
            // Если не нашли в прямом направлении, ищем в конусе вокруг направления
            int rayCount = 16;
            for (int i = 0; i < rayCount; i++)
            {
                float angle = (i / (float)rayCount) * 360f * Mathf.Deg2Rad;
                Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
                if (perpendicular.magnitude < 0.1f)
                    perpendicular = Vector3.Cross(direction, Vector3.forward).normalized;
                
                Vector3 rayDir = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, direction) * perpendicular;
                rayDir = Vector3.Slerp(direction.normalized, rayDir, 0.3f).normalized;
                
                if (Physics.Raycast(position, rayDir, out hit, searchRadius, meshLayerMask))
                {
                    float dist = Vector3.Distance(position, hit.point);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closestPoint = hit.point;
                    }
                }
            }
            
            // Если не нашли через raycast, используем ClosestPoint
            if (closestDistance >= searchRadius)
            {
                int colliderCount = Physics.OverlapSphereNonAlloc(position, searchRadius, _colliderCache, meshLayerMask);
                for (int i = 0; i < colliderCount; i++)
                {
                    Collider col = _colliderCache[i];
                    Vector3 pointOnSurface;
                    
                    if (IsClosestPointSupported(col))
                    {
                        pointOnSurface = col.ClosestPoint(position);
                    }
                    else
                    {
                        Vector3 colCenter = col.bounds.center;
                        Vector3 dirToPosition = (position - colCenter).normalized;
                        if (Physics.Raycast(colCenter, dirToPosition, out hit, col.bounds.size.magnitude * 2f, meshLayerMask))
                        {
                            pointOnSurface = hit.point;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    
                    float dist = Vector3.Distance(position, pointOnSurface);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closestPoint = pointOnSurface;
                    }
                }
            }
            
            return closestPoint;
        }
        
        /// <summary>
        /// Находит ближайшую точку на поверхности коллайдера к заданной позиции
        /// </summary>
        private Vector3 FindClosestSurfacePoint(Vector3 position)
        {
            Vector3 closestPoint = position;
            float closestDistance = float.MaxValue;
            
            // Ищем через raycast во все стороны от центра
            int rayCount = 32;
            RaycastHit hit;
            
            for (int i = 0; i < rayCount; i++)
            {
                // Равномерное распределение по сфере
                float theta = 2f * Mathf.PI * (i / (float)rayCount); // Азимут
                float phi = Mathf.Acos(1f - 2f * (i % (rayCount / 2)) / (float)(rayCount / 2)); // Зенит
                
                Vector3 direction = new Vector3(
                    Mathf.Sin(phi) * Mathf.Cos(theta),
                    Mathf.Cos(phi),
                    Mathf.Sin(phi) * Mathf.Sin(theta)
                );
                
                if (Physics.Raycast(position, direction, out hit, searchRadius, meshLayerMask))
                {
                    float dist = Vector3.Distance(position, hit.point);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closestPoint = hit.point;
                    }
                }
            }
            
            // Если не нашли через raycast, пробуем через OverlapSphere и ClosestPoint
            if (closestDistance >= searchRadius)
            {
                int colliderCount = Physics.OverlapSphereNonAlloc(position, searchRadius, _colliderCache, meshLayerMask);
                for (int i = 0; i < colliderCount; i++)
                {
                    Collider col = _colliderCache[i];
                    Vector3 pointOnSurface;
                    
                    if (IsClosestPointSupported(col))
                    {
                        pointOnSurface = col.ClosestPoint(position);
                    }
                    else
                    {
                        // Для невыпуклых коллайдеров используем raycast от центра коллайдера
                        Vector3 colCenter = col.bounds.center;
                        Vector3 dirToPosition = (position - colCenter).normalized;
                        if (Physics.Raycast(colCenter, dirToPosition, out hit, col.bounds.size.magnitude * 2f, meshLayerMask))
                        {
                            pointOnSurface = hit.point;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    
                    float dist = Vector3.Distance(position, pointOnSurface);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closestPoint = pointOnSurface;
                    }
                }
            }
            
            return closestPoint;
        }
        
        /// <summary>
        /// Убеждается, что позиция точно находится на поверхности коллайдера
        /// </summary>
        private Vector3 EnsurePositionOnSurface(Vector3 position)
        {
            // Проверяем, находится ли позиция на поверхности через raycast
            RaycastHit hit;
            
            // Пробуем raycast в нескольких направлениях для точного позиционирования
            Vector3[] checkDirections = {
                Vector3.down, Vector3.up,
                Vector3.forward, Vector3.back,
                Vector3.left, Vector3.right
            };
            
            float closestDistance = float.MaxValue;
            Vector3 closestPoint = position;
            
            foreach (Vector3 dir in checkDirections)
            {
                if (Physics.Raycast(position + dir * stickDistance, -dir, out hit, stickDistance * 2f, meshLayerMask))
                {
                    float dist = Vector3.Distance(position, hit.point);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closestPoint = hit.point;
                    }
                }
            }
            
            // Если нашли более близкую точку на поверхности, используем её
            if (closestDistance < stickDistance)
            {
                return closestPoint;
            }
            
            // Иначе пробуем через ClosestPoint
            int colliderCountEnsure = Physics.OverlapSphereNonAlloc(position, stickDistance, _colliderCache, meshLayerMask);
            if (colliderCountEnsure > 0)
            {
                Vector3 bestPoint = position;
                float bestDist = float.MaxValue;
                
                for (int i = 0; i < colliderCountEnsure; i++)
                {
                    Collider col = _colliderCache[i];
                    Vector3 pointOnSurface;
                    
                    if (IsClosestPointSupported(col))
                    {
                        pointOnSurface = col.ClosestPoint(position);
                    }
                    else
                    {
                        Vector3 colCenter = col.bounds.center;
                        Vector3 dirToPosition = (position - colCenter).normalized;
                        if (Physics.Raycast(colCenter, dirToPosition, out hit, col.bounds.size.magnitude * 2f, meshLayerMask))
                        {
                            pointOnSurface = hit.point;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    
                    float dist = Vector3.Distance(position, pointOnSurface);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestPoint = pointOnSurface;
                    }
                }
                
                if (bestDist < stickDistance)
                {
                    return bestPoint;
                }
            }
            
            return position;
        }
        
        /// <summary>
        /// Находит следующую позицию с учетом вертикального ограничения
        /// </summary>
        private Vector3 FindNextPositionWithVerticalLimit(Vector3 currentPos, Vector3 direction, float startY, float maxVerticalDistance)
        {
            Vector3 nextPos = FindNextPosition(currentPos, direction);
            
            // Ограничиваем вертикальную составляющую
            float verticalOffset = nextPos.y - startY;
            if (Mathf.Abs(verticalOffset) > maxVerticalDistance)
            {
                float sign = Mathf.Sign(verticalOffset);
                nextPos.y = startY + sign * maxVerticalDistance;
                
                // Пробуем найти позицию на поверхности на этой высоте
                RaycastHit hit;
                Vector3 rayStart = new Vector3(nextPos.x, startY + sign * maxVerticalDistance * 1.1f, nextPos.z);
                Vector3 rayDir = sign > 0 ? Vector3.down : Vector3.up;
                
                if (Physics.Raycast(rayStart, rayDir, out hit, maxVerticalDistance * 0.2f, meshLayerMask))
                {
                    return hit.point;
                }
            }
            
            return nextPos;
        }
        
        private VineSplinePath GenerateSingleVinePath(int vineIndex, Vector3 startPos, Vector3 startNormal, Vector3 initialDirection)
        {
            VineSplinePath path = new VineSplinePath();
            
            // Проецируем начальное направление на плоскость поверхности для движения вдоль неё
            Vector3 currentDir = Vector3.ProjectOnPlane(initialDirection, startNormal).normalized;
            
            // Если проекция слишком мала (вертикальная поверхность), создаем направление вдоль поверхности
            if (currentDir.magnitude < 0.1f)
            {
                // Создаем направление вдоль поверхности, используя перпендикуляр к нормали
                Vector3 right = Vector3.Cross(startNormal, Vector3.up).normalized;
                if (right.magnitude < 0.1f)
                    right = Vector3.Cross(startNormal, Vector3.forward).normalized;
                
                // Добавляем небольшое случайное отклонение для разнообразия
                float angle = (vineIndex / (float)vineCount) * 360f * Mathf.Deg2Rad;
                currentDir = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, startNormal) * right;
            }
            
            // Делаем начальное направление более плавным для обвития
            currentDir = Vector3.Slerp(currentDir, Vector3.ProjectOnPlane(initialDirection, startNormal).normalized, wrapSmoothness);
            
            Vector3 currentPos = startPos;
            
            // Применяем смещение к стартовой точке тоже
            Vector3 startPosWithOffset = startPos + startNormal * surfaceOffset;
            
            path.controlPoints.Add(startPosWithOffset);
            path.normals.Add(startNormal);
            
            float traveledDistance = 0f;
            int maxPoints = Mathf.CeilToInt(vineLength / pointSpacing);
            
            for (int i = 0; i < maxPoints && traveledDistance < vineLength; i++)
            {
                Vector3 nextPos = FindNextPosition(currentPos, currentDir);
                
                // Сразу корректируем позицию на поверхности
                Vector3 correctedPos = EnsurePositionOnSurface(nextPos);
                nextPos = correctedPos;
                
                // Проверяем вертикальное ограничение
                float verticalDistance = Mathf.Abs(nextPos.y - startPos.y);
                if (verticalDistance > verticalLength)
                {
                    // Если превысили вертикальное ограничение, корректируем позицию
                    float verticalOffset = nextPos.y - startPos.y;
                    if (Mathf.Abs(verticalOffset) > verticalLength)
                    {
                        float sign = Mathf.Sign(verticalOffset);
                        // Пробуем найти позицию на поверхности с ограниченной высотой
                        Vector3 limitedPos = FindNextPositionWithVerticalLimit(currentPos, currentDir, startPos.y, verticalLength);
                        if (Vector3.Distance(limitedPos, currentPos) > pointSpacing * 0.1f)
                        {
                            nextPos = EnsurePositionOnSurface(limitedPos);
                        }
                        else
                        {
                            // Если не можем найти позицию с ограничением, прекращаем
                            break;
                        }
                    }
                }
                
                // Проверяем, что мы действительно движемся (не застряли)
                float movementDistance = Vector3.Distance(nextPos, currentPos);
                if (movementDistance < pointSpacing * 0.1f)
                {
                    // Если движение слишком мало, пробуем изменить направление
                    Vector3 currentSurfaceNormal = GetSurfaceNormal(currentPos);
                    // Детерминированное отклонение на основе индекса витка и позиции
                    float noiseX = Mathf.PerlinNoise(currentPos.x * 10f + vineIndex, currentPos.z * 10f);
                    float noiseY = Mathf.PerlinNoise(currentPos.y * 10f + vineIndex, currentPos.x * 10f);
                    float noiseZ = Mathf.PerlinNoise(currentPos.z * 10f + vineIndex, currentPos.y * 10f);
                    Vector3 variation = new Vector3(noiseX - 0.5f, noiseY - 0.5f, noiseZ - 0.5f) * 0.5f;
                    Vector3 newDir = Vector3.ProjectOnPlane(currentDir + variation, currentSurfaceNormal).normalized;
                    if (newDir.magnitude > 0.1f)
                    {
                        currentDir = newDir;
                        Vector3 newNextPos = FindNextPosition(currentPos, currentDir);
                        newNextPos = EnsurePositionOnSurface(newNextPos);
                        
                        // Снова проверяем вертикальное ограничение
                        verticalDistance = Mathf.Abs(newNextPos.y - startPos.y);
                        if (verticalDistance <= verticalLength)
                        {
                            nextPos = newNextPos;
                            movementDistance = Vector3.Distance(nextPos, currentPos);
                        }
                    }
                    
                    // Если всё равно не движемся, прекращаем генерацию этого пути
                    if (movementDistance < pointSpacing * 0.1f)
                        break;
                }
                
                // Финальная проверка вертикального ограничения
                verticalDistance = Mathf.Abs(nextPos.y - startPos.y);
                if (verticalDistance > verticalLength)
                {
                    break;
                }
                
                float distanceFromCenter = Vector3.Distance(nextPos, startPos);
                if (distanceFromCenter > searchRadius)
                    break;
                
                // Финальная корректировка позиции на поверхности
                Vector3 finalPos = EnsurePositionOnSurface(nextPos);
                Vector3 surfaceNormal = GetSurfaceNormal(finalPos);
                
                // Применяем смещение наружу от поверхности, чтобы лоза не утопала в объект
                finalPos = finalPos + surfaceNormal * surfaceOffset;
                
                // Добавляем ТОЧНУЮ позицию на поверхности со смещением
                path.controlPoints.Add(finalPos);
                path.normals.Add(surfaceNormal);
                
                Vector3 movementDir = (finalPos - currentPos);
                if (movementDir.magnitude > 0.001f)
                {
                    // Плавное обновление направления с учетом формы поверхности
                    Vector3 predictedNormal = PredictSurfaceNormal(finalPos, currentDir, surfaceNormal);
                    currentDir = UpdateDirectionSmooth(currentDir, predictedNormal, movementDir, surfaceNormal);
                }
                
                currentPos = finalPos;
                
                if (path.controlPoints.Count > 1)
                {
                    traveledDistance += Vector3.Distance(path.controlPoints[path.controlPoints.Count - 1], 
                                                        path.controlPoints[path.controlPoints.Count - 2]);
                }
            }
            
            if (path.controlPoints.Count < 2)
                return null;
            
            path.totalLength = traveledDistance;
            return path;
        }
        
        private Vector3 FindNextPosition(Vector3 currentPos, Vector3 direction)
        {
            Vector3 nextPos = currentPos + direction.normalized * pointSpacing;
            
            RaycastHit hit;
            
            // Сначала пробуем raycast прямо по направлению движения
            if (Physics.Raycast(currentPos, direction.normalized, out hit, pointSpacing * 2f, meshLayerMask))
            {
                return hit.point;
            }
            
            // Ищем поверхность через конус raycast вокруг направления
            Vector3 bestPos = nextPos;
            float minDistance = float.MaxValue;
            bool foundHit = false;
            
            // Создаем конус вокруг направления движения
            for (int i = 0; i < raycastCount; i++)
            {
                float angle = (i / (float)raycastCount) * 360f * Mathf.Deg2Rad;
                
                // Создаем перпендикулярный вектор для вращения
                Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
                if (perpendicular.magnitude < 0.1f)
                    perpendicular = Vector3.Cross(direction, Vector3.forward).normalized;
                
                // Вращаем вокруг направления движения
                Vector3 rayDir = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, direction) * perpendicular;
                // Смешиваем с основным направлением для создания конуса
                rayDir = Vector3.Slerp(direction.normalized, rayDir, 0.4f).normalized;
                
                float rayDistance = stickDistance * 3f;
                if (Physics.Raycast(currentPos, rayDir, out hit, rayDistance, meshLayerMask))
                {
                    float dist = Vector3.Distance(hit.point, nextPos);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestPos = hit.point;
                        foundHit = true;
                    }
                }
            }
            
            // Если нашли через конус raycast, используем найденную точку
            if (foundHit && minDistance < stickDistance * 2f)
            {
                return bestPos;
            }
            
            // Дополнительно проверяем через OverlapSphere для вертикальных поверхностей
            int nearbyColliderCount = Physics.OverlapSphereNonAlloc(nextPos, stickDistance * 2f, _colliderCache, meshLayerMask);
            if (nearbyColliderCount > 0)
            {
                // Проверяем raycast от текущей позиции к каждому коллайдеру
                for (int idx = 0; idx < nearbyColliderCount; idx++)
                {
                    Collider col = _colliderCache[idx];
                    Vector3 colCenter = col.bounds.center;
                    Vector3 dirToCol = (colCenter - currentPos).normalized;
                    
                    // Проверяем несколько точек вокруг коллайдера
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = (i / 8f) * 360f * Mathf.Deg2Rad;
                        Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * col.bounds.size.magnitude * 0.5f;
                        Vector3 rayStart = currentPos;
                        Vector3 rayEnd = colCenter + offset;
                        Vector3 rayDir = (rayEnd - rayStart).normalized;
                        
                        if (Physics.Raycast(rayStart, rayDir, out hit, Vector3.Distance(rayStart, rayEnd) + 1f, meshLayerMask))
                        {
                            float dist = Vector3.Distance(hit.point, nextPos);
                            if (dist < minDistance)
                            {
                                minDistance = dist;
                                bestPos = hit.point;
                                foundHit = true;
                            }
                        }
                    }
                }
            }
            
            if (foundHit && minDistance < stickDistance * 2f)
            {
                return bestPos;
            }
            
            int colliderCountFinal = Physics.OverlapSphereNonAlloc(nextPos, stickDistance * 2f, _colliderCache, meshLayerMask);
            if (colliderCountFinal > 0)
            {
                Vector3 closestPoint = nextPos;
                float closestDist = float.MaxValue;
                
                for (int i = 0; i < colliderCountFinal; i++)
                {
                    Collider col = _colliderCache[i];
                    Vector3 pointOnSurface;
                    
                    if (IsClosestPointSupported(col))
                    {
                        pointOnSurface = col.ClosestPoint(nextPos);
                    }
                    else
                    {
                        Vector3 colCenterPos = col.bounds.center;
                        Vector3 rayDirection = (nextPos - colCenterPos).normalized;
                        if (Physics.Raycast(colCenterPos, rayDirection, out hit, col.bounds.size.magnitude * 2f, meshLayerMask))
                        {
                            pointOnSurface = hit.point;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    
                    float dist = Vector3.Distance(nextPos, pointOnSurface);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestPoint = pointOnSurface;
                    }
                }
                
                if (closestDist < stickDistance * 2f)
                {
                    return closestPoint;
                }
            }
            
            return nextPos;
        }
        
        private Vector3 GetSurfaceNormal(Vector3 position)
        {
            RaycastHit hit;
            
            // Расширенный набор направлений для поиска поверхностей (включая диагонали)
            Vector3[] directions = { 
                Vector3.down, Vector3.up, 
                Vector3.forward, Vector3.back, 
                Vector3.left, Vector3.right,
                // Диагонали в горизонтальной плоскости
                (Vector3.forward + Vector3.left).normalized,
                (Vector3.forward + Vector3.right).normalized,
                (Vector3.back + Vector3.left).normalized,
                (Vector3.back + Vector3.right).normalized,
                // Диагонали с вертикалью
                (Vector3.down + Vector3.forward).normalized,
                (Vector3.down + Vector3.back).normalized,
                (Vector3.down + Vector3.left).normalized,
                (Vector3.down + Vector3.right).normalized,
                (Vector3.up + Vector3.forward).normalized,
                (Vector3.up + Vector3.back).normalized,
                (Vector3.up + Vector3.left).normalized,
                (Vector3.up + Vector3.right).normalized,
                // Дополнительные направления для стен
                (Vector3.forward + Vector3.up).normalized,
                (Vector3.forward + Vector3.down).normalized,
                (Vector3.back + Vector3.up).normalized,
                (Vector3.back + Vector3.down).normalized,
                (Vector3.left + Vector3.up).normalized,
                (Vector3.left + Vector3.down).normalized,
                (Vector3.right + Vector3.up).normalized,
                (Vector3.right + Vector3.down).normalized
            };
            
            float maxDistance = stickDistance * 3f; // Увеличиваем радиус поиска
            Vector3 bestNormal = Vector3.up;
            float closestDistance = float.MaxValue;
            
            // Сначала пробуем близкие raycast
            foreach (Vector3 dir in directions)
            {
                if (Physics.Raycast(position, dir, out hit, maxDistance, meshLayerMask))
                {
                    float dist = hit.distance;
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        bestNormal = hit.normal;
                    }
                }
            }
            
            // Если не нашли близко, пробуем raycast от позиции к ближайшим коллайдерам
            if (closestDistance >= maxDistance * 0.8f)
            {
                int nearbyColliderCount2 = Physics.OverlapSphereNonAlloc(position, stickDistance * 3f, _colliderCache, meshLayerMask);
                for (int idx = 0; idx < nearbyColliderCount2; idx++)
                {
                    Collider col = _colliderCache[idx];
                    Vector3 colCenter = col.bounds.center;
                    Vector3 dirToCol = (colCenter - position).normalized;
                    
                    // Пробуем raycast от позиции к разным точкам коллайдера
                    for (int i = 0; i < 12; i++)
                    {
                        float angle = (i / 12f) * 360f * Mathf.Deg2Rad;
                        Vector3 offset = new Vector3(
                            Mathf.Cos(angle) * col.bounds.size.x * 0.5f,
                            Mathf.Sin(angle * 2f) * col.bounds.size.y * 0.5f,
                            Mathf.Cos(angle * 1.5f) * col.bounds.size.z * 0.5f
                        );
                        Vector3 targetPoint = colCenter + offset;
                        Vector3 rayDir = (targetPoint - position).normalized;
                        
                        if (Physics.Raycast(position, rayDir, out hit, Vector3.Distance(position, targetPoint) + 1f, meshLayerMask))
                        {
                            float dist = hit.distance;
                            if (dist < closestDistance)
                            {
                                closestDistance = dist;
                                bestNormal = hit.normal;
                            }
                        }
                    }
                }
            }
            
            if (closestDistance >= maxDistance)
            {
                int colliderCount3 = Physics.OverlapSphereNonAlloc(position, stickDistance * 2f, _colliderCache, meshLayerMask);
                if (colliderCount3 > 0)
                {
                    Collider closestCol = _colliderCache[0];
                    float closestDist = float.MaxValue;
                    
                    for (int i = 0; i < colliderCount3; i++)
                    {
                        Collider col = _colliderCache[i];
                        Vector3 closestPoint;
                        
                        if (IsClosestPointSupported(col))
                        {
                            closestPoint = col.ClosestPoint(position);
                        }
                        else
                        {
                            Vector3 colCenterPos = col.bounds.center;
                            Vector3 rayDir = (position - colCenterPos).normalized;
                            if (Physics.Raycast(colCenterPos, rayDir, out hit, col.bounds.size.magnitude * 2f, meshLayerMask))
                            {
                                closestPoint = hit.point;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        
                        float dist = Vector3.Distance(position, closestPoint);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closestCol = col;
                        }
                    }
                    
                    Vector3 finalColCenter = closestCol.bounds.center;
                    Vector3 toPositionDir = (position - finalColCenter).normalized;
                    if (Physics.Raycast(finalColCenter, toPositionDir, out hit, closestCol.bounds.size.magnitude * 2f, meshLayerMask))
                    {
                        return hit.normal;
                    }
                }
            }
            
            return bestNormal;
        }
        
        /// <summary>
        /// Предсказывает нормаль поверхности впереди для плавного обхода углов
        /// </summary>
        private Vector3 PredictSurfaceNormal(Vector3 currentPos, Vector3 direction, Vector3 currentNormal)
        {
            if (surfacePrediction < 0.01f)
                return currentNormal;
            
            // Делаем raycast немного впереди для предсказания формы
            RaycastHit hit;
            Vector3 predictionPoint = currentPos + direction.normalized * pointSpacing * 2f;
            
            // Пробуем несколько направлений вокруг текущего направления
            Vector3 predictedNormal = currentNormal;
            float bestDistance = float.MaxValue;
            
            for (int i = 0; i < 8; i++)
            {
                float angle = (i / 8f) * 360f * Mathf.Deg2Rad;
                Vector3 perpendicular = Vector3.Cross(direction, currentNormal).normalized;
                if (perpendicular.magnitude < 0.1f)
                    perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
                
                Vector3 rayDir = Quaternion.AngleAxis(angle, direction) * perpendicular;
                rayDir = Vector3.Slerp(direction.normalized, rayDir, 0.3f).normalized;
                
                if (Physics.Raycast(predictionPoint, rayDir, out hit, stickDistance * 2f, meshLayerMask))
                {
                    float dist = hit.distance;
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        predictedNormal = hit.normal;
                    }
                }
            }
            
            // Смешиваем текущую и предсказанную нормаль
            return Vector3.Slerp(currentNormal, predictedNormal, surfacePrediction).normalized;
        }
        
        /// <summary>
        /// Плавно обновляет направление с учетом формы поверхности для обвития объектов
        /// </summary>
        private Vector3 UpdateDirectionSmooth(Vector3 currentDir, Vector3 predictedNormal, Vector3 movement, Vector3 currentNormal)
        {
            // Проецируем текущее направление на предсказанную поверхность
            Vector3 projectedDir = Vector3.ProjectOnPlane(currentDir, predictedNormal).normalized;
            
            if (projectedDir.magnitude < 0.1f && movement.magnitude > 0.001f)
            {
                projectedDir = Vector3.ProjectOnPlane(movement.normalized, predictedNormal).normalized;
            }
            
            // Вычисляем направление вдоль поверхности с учетом её кривизны
            Vector3 surfaceTangent = Vector3.Cross(predictedNormal, Vector3.up).normalized;
            if (surfaceTangent.magnitude < 0.1f)
                surfaceTangent = Vector3.Cross(predictedNormal, Vector3.forward).normalized;
            
            // Смешиваем проекцию с касательным направлением для плавного обвития
            Vector3 wrapDirection = Vector3.Slerp(projectedDir, surfaceTangent, wrapSmoothness * 0.3f);
            
            // Добавляем детерминированное отклонение
            float noiseX = Mathf.PerlinNoise(wrapDirection.x * 100f, wrapDirection.z * 100f);
            float noiseY = Mathf.PerlinNoise(wrapDirection.y * 100f, wrapDirection.x * 100f);
            Vector3 variation = new Vector3(noiseX - 0.5f, 0, noiseY - 0.5f) * randomVariation;
            wrapDirection += Vector3.ProjectOnPlane(variation, predictedNormal);
            
            // Плавно интерполируем между старым и новым направлением
            Vector3 smoothDir = Vector3.Slerp(currentDir, wrapDirection.normalized, wrapSmoothness);
            
            return Vector3.ProjectOnPlane(smoothDir, predictedNormal).normalized;
        }
        
        private Vector3 UpdateDirection(Vector3 currentDir, Vector3 surfaceNormal, Vector3 movement)
        {
            Vector3 projectedDir = Vector3.ProjectOnPlane(currentDir, surfaceNormal).normalized;
            
            if (projectedDir.magnitude < 0.1f && movement.magnitude > 0.001f)
            {
                projectedDir = Vector3.ProjectOnPlane(movement.normalized, surfaceNormal).normalized;
            }
            
            float noiseX = Mathf.PerlinNoise(projectedDir.x * 100f, projectedDir.z * 100f);
            float noiseY = Mathf.PerlinNoise(projectedDir.y * 100f, projectedDir.x * 100f);
            Vector3 variation = new Vector3(noiseX - 0.5f, 0, noiseY - 0.5f) * randomVariation;
            projectedDir += Vector3.ProjectOnPlane(variation, surfaceNormal);
            
            return projectedDir.normalized;
        }
        
        private Vector3 GetRandomDirection(int index)
        {
            float angle = (index / (float)vineCount) * 360f * Mathf.Deg2Rad;
            float elevationSin = Mathf.Sin(index * 1.618f) * 45f;
            float elevation = elevationSin * Mathf.Deg2Rad;
            
            Vector3 dir = new Vector3(
                Mathf.Cos(elevation) * Mathf.Cos(angle),
                Mathf.Sin(elevation),
                Mathf.Cos(elevation) * Mathf.Sin(angle)
            );
            
            return dir.normalized;
        }
        
        private bool IsClosestPointSupported(Collider col)
        {
            return col is BoxCollider || 
                   col is SphereCollider || 
                   col is CapsuleCollider || 
                   (col is MeshCollider meshCol && meshCol.convex);
        }
        
        private void GenerateVineMesh(VineSplinePath path, List<Vector3> vertices, List<int> triangles, 
            List<Vector3> normals, List<Vector2> uvs, ref int vertexOffset)
        {
            if (path.controlPoints.Count < 2) return;
            
            // Используем больше сегментов для более точного следования spline
            int segments = Mathf.Max(lengthSegments * 2, path.controlPoints.Count * 2);
            int maxSegment = Mathf.CeilToInt(segments * growthProgress);
            
            // Сохраняем предыдущие векторы для плавности
            Vector3 prevForward = Vector3.zero;
            Vector3 prevRight = Vector3.zero;
            Vector3 prevUp = Vector3.zero;
            
            for (int i = 0; i <= segments; i++)
            {
                if (i > maxSegment) break;
                
                float t = i / (float)segments;
                
                // Получаем ТОЧНУЮ позицию из spline - это центр сечения меша
                Vector3 position = path.Evaluate(t);
                Vector3 surfaceNormal = path.GetNormal(t);
                
                // Вычисляем направление вдоль spline через производную
                Vector3 forward = Vector3.zero;
                float deltaT = 0.0001f; // Очень маленький шаг для точности
                
                if (i < segments)
                {
                    float nextT = Mathf.Min(1f, t + deltaT);
                    Vector3 nextPos = path.Evaluate(nextT);
                    forward = (nextPos - position);
                    if (forward.magnitude > 0.0001f)
                        forward = forward.normalized;
                }
                
                if (forward.magnitude < 0.01f && i > 0)
                {
                    float prevT = Mathf.Max(0f, t - deltaT);
                    Vector3 prevPos = path.Evaluate(prevT);
                    forward = (position - prevPos);
                    if (forward.magnitude > 0.0001f)
                        forward = forward.normalized;
                }
                
                // Fallback на предыдущее направление или направление между контрольными точками
                if (forward.magnitude < 0.01f)
                {
                    if (prevForward.magnitude > 0.01f)
                    {
                        forward = prevForward;
                    }
                    else
                    {
                        int pointIndex = Mathf.FloorToInt(t * (path.controlPoints.Count - 1));
                        pointIndex = Mathf.Clamp(pointIndex, 0, path.controlPoints.Count - 2);
                        forward = (path.controlPoints[pointIndex + 1] - path.controlPoints[pointIndex]);
                        if (forward.magnitude > 0.0001f)
                            forward = forward.normalized;
                        else
                            forward = Vector3.forward;
                    }
                }
                
                // Строим локальную систему координат для сечения меша
                // Используем метод Френе-Серре для стабильной системы координат
                Vector3 right, up;
                
                // Проверяем, является ли поверхность вертикальной
                bool isVerticalSurface = Mathf.Abs(Vector3.Dot(surfaceNormal, Vector3.up)) < 0.3f;
                
                if (isVerticalSurface)
                {
                    // Для вертикальных поверхностей используем более стабильный метод
                    // Right = forward × up (мировой up)
                    Vector3 worldUp = Vector3.up;
                    right = Vector3.Cross(forward, worldUp).normalized;
                    
                    // Если right слишком мал, пробуем другой up
                    if (right.magnitude < 0.1f)
                    {
                        worldUp = Vector3.forward;
                        right = Vector3.Cross(forward, worldUp).normalized;
                    }
                    if (right.magnitude < 0.1f)
                    {
                        worldUp = Vector3.right;
                        right = Vector3.Cross(forward, worldUp).normalized;
                    }
                    
                    // Корректируем right, чтобы он был перпендикулярен surfaceNormal
                    right = Vector3.ProjectOnPlane(right, surfaceNormal).normalized;
                    if (right.magnitude < 0.1f)
                    {
                        // Если проекция слишком мала, создаем right из surfaceNormal
                        right = Vector3.Cross(surfaceNormal, forward).normalized;
                        if (right.magnitude < 0.1f)
                        {
                            right = Vector3.Cross(surfaceNormal, Vector3.up).normalized;
                        }
                    }
                    
                    // Up = right × forward (для правильной ориентации сечения)
                    up = Vector3.Cross(right, forward).normalized;
                    
                    // Убеждаемся, что up направлен наружу от поверхности
                    float dotWithNormal = Vector3.Dot(up, surfaceNormal);
                    if (dotWithNormal < 0)
                    {
                        up = -up;
                    }
                }
                else
                {
                    // Для горизонтальных поверхностей используем стандартный метод
                    // Right = forward × surfaceNormal
                    right = Vector3.Cross(forward, surfaceNormal).normalized;
                    
                    if (right.magnitude < 0.1f)
                    {
                        // Fallback для особых случаев
                        Vector3 worldUp = Vector3.up;
                        if (Mathf.Abs(Vector3.Dot(surfaceNormal, worldUp)) > 0.9f)
                        {
                            worldUp = Vector3.forward;
                        }
                        right = Vector3.Cross(forward, worldUp).normalized;
                        if (right.magnitude < 0.1f)
                        {
                            right = Vector3.Cross(forward, Vector3.right).normalized;
                        }
                        right = Vector3.ProjectOnPlane(right, surfaceNormal).normalized;
                    }
                    
                    // Up = right × forward
                    up = Vector3.Cross(right, forward).normalized;
                    
                    // Убеждаемся, что up направлен наружу от поверхности
                    float dotWithNormal = Vector3.Dot(up, surfaceNormal);
                    if (dotWithNormal < 0)
                    {
                        up = -up;
                    }
                }
                
                // Плавная интерполяция с предыдущим кадром для избежания резких скачков
                if (i > 0 && prevRight.magnitude > 0.1f && prevUp.magnitude > 0.1f)
                {
                    right = Vector3.Slerp(prevRight, right, 0.7f).normalized;
                    up = Vector3.Slerp(prevUp, up, 0.7f).normalized;
                }
                
                // КРИТИЧНО: Для плоских поверхностей up должен быть строго параллелен surfaceNormal
                // Это предотвращает вертикальное смещение меша
                if (!isVerticalSurface)
                {
                    // На плоских поверхностях up = surfaceNormal (направлен наружу от поверхности)
                    up = surfaceNormal;
                    // Пересчитываем right для ортогональности: right = forward × up
                    right = Vector3.Cross(forward, up).normalized;
                    
                    // Если right слишком мал, используем альтернативу
                    if (right.magnitude < 0.1f)
                    {
                        // Создаем right из перпендикуляра к normal
                        Vector3 worldRight = Vector3.right;
                        if (Mathf.Abs(Vector3.Dot(surfaceNormal, worldRight)) > 0.9f)
                            worldRight = Vector3.forward;
                        right = Vector3.Cross(surfaceNormal, worldRight).normalized;
                        if (right.magnitude < 0.1f)
                            right = Vector3.Cross(surfaceNormal, Vector3.up).normalized;
                    }
                }
                
                // Плавная интерполяция с предыдущим кадром
                if (i > 0 && prevRight.magnitude > 0.1f && prevUp.magnitude > 0.1f)
                {
                    // Для вертикальных поверхностей интерполируем, для плоских - используем точные значения
                    if (isVerticalSurface)
                    {
                        right = Vector3.Slerp(prevRight, right, 0.7f).normalized;
                        up = Vector3.Slerp(prevUp, up, 0.7f).normalized;
                    }
                }
                
                prevForward = forward;
                prevRight = right;
                prevUp = up;
                
                float thicknessNoise = Mathf.PerlinNoise(position.x * 10f, position.z * 10f);
                float currentThickness = vineThickness + (thicknessNoise - 0.5f) * 2f * thicknessVariation;
                
                // Генерируем кольцо вершин вокруг ТОЧНОЙ позиции на spline
                // ВАЖНО: смещение строго в плоскости, перпендикулярной forward
                for (int j = 0; j <= radialSegments; j++)
                {
                    float angle = (j / (float)radialSegments) * 360f * Mathf.Deg2Rad;
                    
                    // Создаем смещение в плоскости сечения (перпендикулярной forward)
                    Vector3 offset = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * currentThickness;
                    
                    // КРИТИЧНО: Убираем любую компоненту вдоль forward для точного позиционирования
                    float forwardDot = Vector3.Dot(offset, forward);
                    offset = offset - forward * forwardDot;
                    
                    // Вершина ТОЧНО на позиции из spline (без дополнительных смещений)
                    Vector3 vertexPos = position + offset;
                    
                    vertices.Add(vertexPos);
                    // Нормаль вершины - направление от центра spline к вершине
                    Vector3 vertexNormal = offset.normalized;
                    if (vertexNormal.magnitude < 0.01f)
                        vertexNormal = surfaceNormal;
                    normals.Add(vertexNormal);
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
        
        private void OnDrawGizmosSelected()
        {
            if (centerPoint == null) centerPoint = transform;
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(centerPoint.position, searchRadius);
            
            foreach (var path in _vinePaths)
            {
                if (path.controlPoints.Count < 2) continue;
                
                Gizmos.color = Color.red;
                for (int i = 0; i < path.controlPoints.Count; i++)
                {
                    Gizmos.DrawSphere(path.controlPoints[i], 0.05f);
                }
                
                Gizmos.color = Color.yellow;
                for (int i = 0; i < path.controlPoints.Count - 1; i++)
                {
                    Gizmos.DrawLine(path.controlPoints[i], path.controlPoints[i + 1]);
                }
                
                Gizmos.color = Color.magenta;
                int splineSegments = 50;
                for (int i = 0; i < splineSegments; i++)
                {
                    float t1 = i / (float)splineSegments;
                    float t2 = (i + 1) / (float)splineSegments;
                    Vector3 p1 = path.Evaluate(t1);
                    Vector3 p2 = path.Evaluate(t2);
                    Gizmos.DrawLine(p1, p2);
                }
                
                Gizmos.color = Color.cyan;
                for (int i = 0; i < path.controlPoints.Count; i++)
                {
                    Gizmos.DrawRay(path.controlPoints[i], path.normals[i] * 0.2f);
                }
            }
        }
    }
}
