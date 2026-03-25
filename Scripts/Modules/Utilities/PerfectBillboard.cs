using UnityEngine;

[ExecuteAlways] // Работает и в редакторе, и в игре
public class PerfectBillboard : MonoBehaviour
{
    public Camera targetCamera;
    public bool rotateOnlyY = true; // Поворачивать только по вертикали (Y)
    
    [Header("Moon-like Behavior")]
    [Tooltip("Работать как луна - поворачиваться только по Z оси, сохраняя X rotation")]
    public bool moonLikeBehavior = true;
    
    [Tooltip("Сохранять оригинальный X rotation")]
    public float originalXRotation = 0f;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
            
        // Сохраняем оригинальный X rotation
        if (moonLikeBehavior)
        {
            originalXRotation = transform.eulerAngles.x;
        }
    }

    void LateUpdate()
    {
        if (!targetCamera) return;

        if (moonLikeBehavior)
        {
            // Лунное поведение - поворачиваемся только по Z оси
            Vector3 direction = targetCamera.transform.position - transform.position;
            
            // Проецируем направление на плоскость XZ (убираем Y)
            direction.y = 0f;
            
            if (direction.sqrMagnitude > 0.001f)
            {
                // Вычисляем угол поворота по Z
                float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                
                // Применяем поворот только по Z, сохраняя X и Y
                Vector3 currentEuler = transform.eulerAngles;
                transform.rotation = Quaternion.Euler(originalXRotation, currentEuler.y, angle);
            }
        }
        else
        {
            // Обычное билборд поведение
            Vector3 direction = targetCamera.transform.position - transform.position;

            if (rotateOnlyY)
                direction.y = 0f; // Не поворачиваем вверх-вниз (как "столб")

            // Поворачиваемся, чтобы объект смотрел на камеру
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(-direction.normalized, Vector3.up);
        }
    }
}