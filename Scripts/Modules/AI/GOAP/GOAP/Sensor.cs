using System;
using UnityEngine;

public enum SensorType {
    Eyes,   // Поле обзора (угол)
    Ears,   // Радиус шума
    Combat, // Радиус боя (сфера)
    Chase   // Максимальное расстояние преследования (сфера)
}

[RequireComponent(typeof(SphereCollider))]
public class Sensor : MonoBehaviour {
    [Header("Sensor Settings")]
    [SerializeField] SensorType sensorType = SensorType.Eyes;
    
    [Header("Detection Settings")]
    [SerializeField] float detectionRadius = 5f;
    [SerializeField] float timerInterval = 1f;
    
    [Header("Eyes Settings (только для типа Eyes)")]
    [SerializeField] [Range(0f, 360f)] float fieldOfViewAngle = 90f;
    
    SphereCollider detectionRange;
    
    public event Action OnTargetChanged = delegate { };
    
    public Vector3 TargetPosition => target ? target.transform.position : Vector3.zero;
    public bool IsTargetInRange => TargetPosition != Vector3.zero;
    public SensorType Type => sensorType;
    
    GameObject target;
    Vector3 lastKnownPosition;
    Vector3 lastTargetVelocity;
    CountdownTimer timer;

    void Awake() {
        detectionRange = GetComponent<SphereCollider>();
        detectionRange.isTrigger = true;
        detectionRange.radius = detectionRadius;
    }

    void Start() {
        timer = new CountdownTimer(timerInterval);
        timer.OnTimerStop += () => {
            UpdateTargetPosition(target.OrNull());
            timer.Start();
        };
        timer.Start();
    }
    
    void Update() {
        timer.Tick(Time.deltaTime);
        

        if (sensorType == SensorType.Eyes && target != null) {
            CheckFieldOfView();
        }
        

        if (sensorType == SensorType.Chase && target != null) {
            Vector3 currentPos = target.transform.position;
            if (lastKnownPosition != Vector3.zero) {
                lastTargetVelocity = (currentPos - lastKnownPosition) / Time.deltaTime;
            }
            lastKnownPosition = currentPos;
        }
    }

    void UpdateTargetPosition(GameObject target = null) {
        bool wasInRange = IsTargetInRange;
        this.target = target;
        if (target == null) {
            if (wasInRange) {
                Debug.Log($"[Sensor] {sensorType}: Игрок потерян");
            }
            lastKnownPosition = Vector3.zero;
            lastTargetVelocity = Vector3.zero;
        } else if (IsTargetInRange && (lastKnownPosition != TargetPosition || lastKnownPosition == Vector3.zero)) {
            if (!wasInRange) {
                Debug.Log($"[Sensor] {sensorType}: Игрок обнаружен на расстоянии {Vector3.Distance(transform.position, TargetPosition):F2}");
            }
            lastKnownPosition = TargetPosition;
            OnTargetChanged.Invoke();
        }
    }
    
    /// <summary>
    /// Получает предсказанную позицию цели для Chase сенсора (куда игрок движется)
    /// </summary>
    public Vector3 GetPredictedTargetPosition(float predictionTime = 0.5f) {
        if (target == null || !IsTargetInRange) return Vector3.zero;
        

        if (sensorType == SensorType.Chase && lastTargetVelocity != Vector3.zero) {
            return target.transform.position + lastTargetVelocity * predictionTime;
        }
        

        return TargetPosition;
    }
    
    /// <summary>
    /// Проверяет, находится ли цель в поле обзора (для Eyes)
    /// </summary>
    void CheckFieldOfView() {
        if (target == null) return;
        
        Vector3 toTarget = target.transform.position - transform.position;
        float distance = toTarget.magnitude;
        

        if (distance > detectionRadius) {
            UpdateTargetPosition();
            return;
        }
        

        Vector3 directionToTarget = toTarget.normalized;
        Vector3 forward = transform.forward;
        
        float angle = Vector3.Angle(forward, directionToTarget);
        

        if (angle > fieldOfViewAngle * 0.5f) {
            UpdateTargetPosition();
        }
    }
    
    void OnTriggerEnter(Collider other) {
        if (!other.CompareTag("Player")) return;
        

        if (sensorType == SensorType.Eyes) {
            Vector3 toTarget = other.transform.position - transform.position;
            float distance = toTarget.magnitude;
            
            if (distance > detectionRadius) {
                return;
            }
            
            Vector3 directionToTarget = toTarget.normalized;
            Vector3 forward = transform.forward;
            float angle = Vector3.Angle(forward, directionToTarget);
  
            if (angle <= fieldOfViewAngle * 0.5f) {
                UpdateTargetPosition(other.gameObject);
            }
        } else {
            UpdateTargetPosition(other.gameObject);
        }
    }
    
    void OnTriggerExit(Collider other) {
        if (!other.CompareTag("Player")) return;
        UpdateTargetPosition();
    }
    
    void OnDrawGizmos() {
        if (sensorType == SensorType.Eyes) {
            Gizmos.color = IsTargetInRange ? Color.red : Color.yellow;
            
            float halfAngle = fieldOfViewAngle * 0.5f;
            Vector3 forward = transform.forward;
            Vector3 up = transform.up;
            
            Quaternion leftRotation = Quaternion.AngleAxis(-halfAngle, up);
            Quaternion rightRotation = Quaternion.AngleAxis(halfAngle, up);
            
            Vector3 leftDirection = leftRotation * forward;
            Vector3 rightDirection = rightRotation * forward;
            
            Gizmos.DrawRay(transform.position, leftDirection * detectionRadius);
            Gizmos.DrawRay(transform.position, rightDirection * detectionRadius);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.color = IsTargetInRange ? Color.red : Color.yellow;
            UnityEditor.Handles.DrawWireArc(
                transform.position,
                up,
                leftDirection,
                fieldOfViewAngle,
                detectionRadius
            );
            #endif
        } else if (sensorType == SensorType.Ears) {
            Gizmos.color = IsTargetInRange ? Color.red : Color.blue;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        } else if (sensorType == SensorType.Combat) {
            Gizmos.color = IsTargetInRange ? Color.red : Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        } else if (sensorType == SensorType.Chase) {
            Gizmos.color = IsTargetInRange ? Color.red : Color.green;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}