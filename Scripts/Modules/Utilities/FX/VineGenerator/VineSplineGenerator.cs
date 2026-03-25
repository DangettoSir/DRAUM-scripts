using System.Collections.Generic;
using UnityEngine;

namespace DRAUM.Modules.Utilities.FX
{
    /// <summary>
    /// Генерирует spline пути для лозы, которые прилипают к ближайшим мешам от центра
    /// </summary>
    public class VineSplineGenerator : MonoBehaviour
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
        
        [Header("Mesh Detection")]
        [Tooltip("Максимальное расстояние для прилипания к мешу")]
        public float stickDistance = 0.5f;
        
        [Tooltip("Количество лучей для поиска поверхности")]
        public int raycastCount = 16;
        
        private List<VineSplinePath> _vinePaths = new List<VineSplinePath>();
        private List<MeshRenderer> _foundMeshes = new List<MeshRenderer>();
        
        private void Start()
        {
            if (centerPoint == null)
                centerPoint = transform;
            
            GenerateVinePaths();
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Обновляем в реальном времени в редакторе только во время игры
            if (Application.isPlaying && centerPoint != null && enabled)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null && Application.isPlaying)
                    {
                        GenerateVinePaths();
                        VineMeshGenerator meshGen = GetComponent<VineMeshGenerator>();
                        if (meshGen != null)
                            meshGen.UpdateMesh();
                    }
                };
            }
        }
#endif
        
        /// <summary>
        /// Генерирует все пути лозы
        /// </summary>
        public void GenerateVinePaths()
        {
            _vinePaths.Clear();
            FindNearbyMeshes();
            
            for (int i = 0; i < vineCount; i++)
            {
                VineSplinePath path = GenerateSingleVinePath(i);
                if (path != null && path.controlPoints.Count > 1)
                {
                    _vinePaths.Add(path);
                }
            }
        }
        
        /// <summary>
        /// Находит все меши в радиусе
        /// </summary>
        private void FindNearbyMeshes()
        {
            _foundMeshes.Clear();
            Collider[] colliders = Physics.OverlapSphere(centerPoint.position, searchRadius, meshLayerMask);
            
            foreach (Collider col in colliders)
            {
                MeshRenderer meshRenderer = col.GetComponent<MeshRenderer>();
                if (meshRenderer != null && !_foundMeshes.Contains(meshRenderer))
                {
                    _foundMeshes.Add(meshRenderer);
                }
            }
        }
        
        /// <summary>
        /// Генерирует один путь лозы
        /// </summary>
        private VineSplinePath GenerateSingleVinePath(int vineIndex)
        {
            VineSplinePath path = new VineSplinePath();
            
            Vector3 startPos = centerPoint.position;
            Vector3 currentPos = startPos;
            Vector3 currentDir = GetRandomDirection(vineIndex);
            
            path.controlPoints.Add(startPos);
            path.normals.Add(Vector3.up);
            
            float traveledDistance = 0f;
            int maxPoints = Mathf.CeilToInt(vineLength / pointSpacing);
            
            for (int i = 0; i < maxPoints && traveledDistance < vineLength; i++)
            {
                // Ищем ближайшую поверхность меша
                Vector3 nextPos = FindNextPosition(currentPos, currentDir);
                
                // Проверяем, не слишком ли далеко от центра
                float distanceFromCenter = Vector3.Distance(nextPos, startPos);
                if (distanceFromCenter > searchRadius)
                    break;
                
                // Получаем нормаль поверхности ДО добавления точки (используем найденную позицию)
                Vector3 surfaceNormal = GetSurfaceNormal(nextPos);
                
                // Добавляем точку ТОЧНО там, где нашли поверхность
                path.controlPoints.Add(nextPos);
                path.normals.Add(surfaceNormal);
                
                // Обновляем направление вдоль поверхности
                Vector3 movementDir = (nextPos - currentPos);
                if (movementDir.magnitude > 0.001f)
                {
                    currentDir = UpdateDirection(currentDir, surfaceNormal, movementDir);
                }
                
                currentPos = nextPos;
                
                // Вычисляем пройденное расстояние
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
        
        /// <summary>
        /// Находит следующую позицию вдоль поверхности меша
        /// </summary>
        private Vector3 FindNextPosition(Vector3 currentPos, Vector3 direction)
        {
            Vector3 nextPos = currentPos + direction.normalized * pointSpacing;
            
            // Сначала пробуем raycast прямо по направлению движения
            RaycastHit hit;
            if (Physics.Raycast(currentPos, direction.normalized, out hit, pointSpacing * 2f, meshLayerMask))
            {
                // Нашли поверхность - используем точку попадания
                return hit.point;
            }
            
            // Ищем ближайшую поверхность через raycast в конусе вокруг направления
            Vector3 bestPos = nextPos;
            float minDistance = float.MaxValue;
            RaycastHit bestHit = new RaycastHit();
            bool foundHit = false;
            
            // Проверяем несколько направлений для лучшего прилипания
            // Используем конус вокруг направления движения
            for (int i = 0; i < raycastCount; i++)
            {
                float angle = (i / (float)raycastCount) * 360f * Mathf.Deg2Rad;
                
                // Создаем перпендикулярный вектор для вращения
                Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
                if (perpendicular.magnitude < 0.1f)
                    perpendicular = Vector3.Cross(direction, Vector3.forward).normalized;
                
                // Вращаем направление вокруг оси движения
                Vector3 rayDir = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, direction) * perpendicular;
                // Смешиваем с основным направлением для создания конуса
                rayDir = Vector3.Slerp(direction.normalized, rayDir, 0.3f).normalized;
                
                float rayDistance = stickDistance * 3f;
                if (Physics.Raycast(currentPos, rayDir, out hit, rayDistance, meshLayerMask))
                {
                    float dist = Vector3.Distance(hit.point, nextPos);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestPos = hit.point;
                        bestHit = hit;
                        foundHit = true;
                    }
                }
            }
            
            // Если нашли поверхность через raycast, используем её
            if (foundHit && minDistance < stickDistance * 2f)
            {
                return bestPos;
            }
            
            // Иначе ищем ближайшую поверхность через OverlapSphere и ClosestPoint
            Collider[] colliders = Physics.OverlapSphere(nextPos, stickDistance * 2f, meshLayerMask);
            if (colliders.Length > 0)
            {
                // Находим ближайшую точку на поверхности
                Vector3 closestPoint = nextPos;
                float closestDist = float.MaxValue;
                
                foreach (Collider col in colliders)
                {
                    Vector3 pointOnSurface;
                    
                    // ClosestPoint работает только с определенными типами коллайдеров
                    if (IsClosestPointSupported(col))
                    {
                        pointOnSurface = col.ClosestPoint(nextPos);
                    }
                    else
                    {
                        // Для других типов используем raycast от центра коллайдера
                        Vector3 colCenterPos = col.bounds.center;
                        Vector3 rayDirection = (nextPos - colCenterPos).normalized;
                        if (Physics.Raycast(colCenterPos, rayDirection, out hit, col.bounds.size.magnitude * 2f, meshLayerMask))
                        {
                            pointOnSurface = hit.point;
                        }
                        else
                        {
                            continue; // Пропускаем этот коллайдер
                        }
                    }
                    
                    float dist = Vector3.Distance(nextPos, pointOnSurface);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestPoint = pointOnSurface;
                    }
                }
                
                // Если точка достаточно близко, используем её
                if (closestDist < stickDistance * 2f)
                {
                    return closestPoint;
                }
            }
            
            return nextPos;
        }
        
        /// <summary>
        /// Проверяет, поддерживает ли коллайдер метод ClosestPoint
        /// </summary>
        private bool IsClosestPointSupported(Collider col)
        {
            return col is BoxCollider || 
                   col is SphereCollider || 
                   col is CapsuleCollider || 
                   (col is MeshCollider meshCol && meshCol.convex);
        }
        
        /// <summary>
        /// Получает нормаль поверхности в точке
        /// </summary>
        private Vector3 GetSurfaceNormal(Vector3 position)
        {
            RaycastHit hit;
            
            // Пробуем raycast во все стороны с большим радиусом
            Vector3[] directions = { 
                Vector3.down, Vector3.up, 
                Vector3.forward, Vector3.back, 
                Vector3.left, Vector3.right,
                (Vector3.down + Vector3.forward).normalized,
                (Vector3.down + Vector3.back).normalized,
                (Vector3.down + Vector3.left).normalized,
                (Vector3.down + Vector3.right).normalized,
                (Vector3.up + Vector3.forward).normalized,
                (Vector3.up + Vector3.back).normalized,
                (Vector3.up + Vector3.left).normalized,
                (Vector3.up + Vector3.right).normalized
            };
            
            float maxDistance = stickDistance * 2f;
            Vector3 bestNormal = Vector3.up;
            float closestDistance = float.MaxValue;
            
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
            
            // Если не нашли через raycast, пробуем через ClosestPoint
            if (closestDistance >= maxDistance)
            {
                Collider[] colliders = Physics.OverlapSphere(position, stickDistance * 2f, meshLayerMask);
                if (colliders.Length > 0)
                {
                    // Используем нормаль от ближайшего коллайдера
                    Collider closestCol = colliders[0];
                    float closestDist = float.MaxValue;
                    
                    foreach (Collider col in colliders)
                    {
                        Vector3 closestPoint;
                        
                        // ClosestPoint работает только с определенными типами коллайдеров
                        if (IsClosestPointSupported(col))
                        {
                            closestPoint = col.ClosestPoint(position);
                        }
                        else
                        {
                            // Для других типов используем raycast от центра коллайдера
                            Vector3 colCenterPos = col.bounds.center;
                            Vector3 rayDir = (position - colCenterPos).normalized;
                            if (Physics.Raycast(colCenterPos, rayDir, out hit, col.bounds.size.magnitude * 2f, meshLayerMask))
                            {
                                closestPoint = hit.point;
                            }
                            else
                            {
                                continue; // Пропускаем этот коллайдер
                            }
                        }
                        
                        float dist = Vector3.Distance(position, closestPoint);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closestCol = col;
                        }
                    }
                    
                    // Пробуем получить нормаль через raycast от центра коллайдера
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
        /// Обновляет направление движения вдоль поверхности
        /// </summary>
        private Vector3 UpdateDirection(Vector3 currentDir, Vector3 surfaceNormal, Vector3 movement)
        {
            // Проецируем направление на плоскость поверхности
            Vector3 projectedDir = Vector3.ProjectOnPlane(currentDir, surfaceNormal).normalized;
            
            // Если проекция слишком мала, используем движение как основу
            if (projectedDir.magnitude < 0.1f && movement.magnitude > 0.001f)
            {
                projectedDir = Vector3.ProjectOnPlane(movement.normalized, surfaceNormal).normalized;
            }
            
            // Добавляем детерминированное отклонение на основе текущей позиции
            float noiseX = Mathf.PerlinNoise(projectedDir.x * 100f, projectedDir.z * 100f);
            float noiseY = Mathf.PerlinNoise(projectedDir.y * 100f, projectedDir.x * 100f);
            Vector3 variation = new Vector3(noiseX - 0.5f, 0, noiseY - 0.5f) * randomVariation;
            projectedDir += Vector3.ProjectOnPlane(variation, surfaceNormal);
            
            return projectedDir.normalized;
        }
        
        /// <summary>
        /// Получает детерминированное направление для витка
        /// </summary>
        private Vector3 GetRandomDirection(int index)
        {
            // Детерминированная генерация на основе индекса
            float angle = (index / (float)vineCount) * 360f * Mathf.Deg2Rad;
            
            // Используем простую функцию для создания вариации высоты
            float elevationSin = Mathf.Sin(index * 1.618f) * 45f; // Золотое сечение для распределения
            float elevation = elevationSin * Mathf.Deg2Rad;
            
            Vector3 dir = new Vector3(
                Mathf.Cos(elevation) * Mathf.Cos(angle),
                Mathf.Sin(elevation),
                Mathf.Cos(elevation) * Mathf.Sin(angle)
            );
            
            return dir.normalized;
        }
        
        /// <summary>
        /// Получает все сгенерированные пути
        /// </summary>
        public List<VineSplinePath> GetVinePaths()
        {
            return _vinePaths;
        }
        
        private void OnDrawGizmosSelected()
        {
            if (centerPoint == null) centerPoint = transform;
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(centerPoint.position, searchRadius);
            
            // Рисуем пути в редакторе
            foreach (var path in _vinePaths)
            {
                if (path.controlPoints.Count < 2) continue;
                
                // Рисуем контрольные точки (где реально находятся точки)
                Gizmos.color = Color.red;
                for (int i = 0; i < path.controlPoints.Count; i++)
                {
                    Gizmos.DrawSphere(path.controlPoints[i], 0.05f);
                }
                
                // Рисуем линии между контрольными точками
                Gizmos.color = Color.yellow;
                for (int i = 0; i < path.controlPoints.Count - 1; i++)
                {
                    Gizmos.DrawLine(path.controlPoints[i], path.controlPoints[i + 1]);
                }
                
                // Рисуем интерполированный spline
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
                
                // Рисуем нормали
                Gizmos.color = Color.cyan;
                for (int i = 0; i < path.controlPoints.Count; i++)
                {
                    Gizmos.DrawRay(path.controlPoints[i], path.normals[i] * 0.2f);
                }
            }
        }
    }
}
