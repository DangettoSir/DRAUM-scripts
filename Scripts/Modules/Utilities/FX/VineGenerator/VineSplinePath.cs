using System.Collections.Generic;
using UnityEngine;

namespace DRAUM.Modules.Utilities.FX
{
    /// <summary>
    /// Представляет spline путь для лозы с контрольными точками
    /// </summary>
    [System.Serializable]
    public class VineSplinePath
    {
        public List<Vector3> controlPoints = new List<Vector3>();
        public List<Vector3> normals = new List<Vector3>();
        public float totalLength;
        
        public Vector3 Evaluate(float t)
        {
            if (controlPoints.Count < 2) return Vector3.zero;
            
            t = Mathf.Clamp01(t);
            float segmentLength = 1f / (controlPoints.Count - 1);
            int segmentIndex = Mathf.FloorToInt(t / segmentLength);
            segmentIndex = Mathf.Clamp(segmentIndex, 0, controlPoints.Count - 2);
            
            float localT = (t - segmentIndex * segmentLength) / segmentLength;
            
            // Catmull-Rom spline для плавных кривых
            if (controlPoints.Count >= 4)
            {
                int p0 = Mathf.Max(0, segmentIndex - 1);
                int p1 = segmentIndex;
                int p2 = segmentIndex + 1;
                int p3 = Mathf.Min(controlPoints.Count - 1, segmentIndex + 2);
                
                return CatmullRom(controlPoints[p0], controlPoints[p1], controlPoints[p2], controlPoints[p3], localT);
            }
            else
            {
                // Простая линейная интерполяция для малого количества точек
                return Vector3.Lerp(controlPoints[segmentIndex], controlPoints[segmentIndex + 1], localT);
            }
        }
        
        private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            
            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }
        
        public Vector3 GetNormal(float t)
        {
            if (normals.Count == 0) return Vector3.up;
            
            t = Mathf.Clamp01(t);
            float segmentLength = 1f / (normals.Count - 1);
            int segmentIndex = Mathf.FloorToInt(t / segmentLength);
            segmentIndex = Mathf.Clamp(segmentIndex, 0, normals.Count - 2);
            
            float localT = (t - segmentIndex * segmentLength) / segmentLength;
            return Vector3.Slerp(normals[segmentIndex], normals[segmentIndex + 1], localT);
        }
    }
}
