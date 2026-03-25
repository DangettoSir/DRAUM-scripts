using UnityEngine;
using TMPro;

namespace DRAUM.Modules.Combat
{
    /// <summary>
    /// Компонент для врагов, который показывает опцию добивания когда игрок находится сзади врага в зоне Box Gizmo.
    /// Добавляется на врагов со слоем Enemy.
    /// </summary>
    [RequireComponent(typeof(EnemyEntity))]
    public class BrutalityInteractable : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Текст взаимодействия для добивания")]
        public string brutalityInteractionName = "добить";
        
        [Header("Brutality Zone (Box Gizmo)")]
        [Tooltip("Смещение зоны добивания сзади врага (в локальных координатах, по оси Z - назад)")]
        public Vector3 brutalityZoneOffset = new Vector3(0f, 0f, -1f);
        
        [Tooltip("Размеры Box Gizmo зоны добивания (X, Y, Z)")]
        public Vector3 brutalityZoneSize = new Vector3(2f, 2f, 2f);
        
        [Tooltip("Цвет Gizmo зоны добивания")]
        public Color gizmoColor = new Color(1f, 0f, 0f, 0.3f);
        
        [Header("References")]
        [Tooltip("Transform игрока для проверки позиции (автопоиск если не назначен)")]
        public Transform playerTransform;
        
        [Tooltip("InteractionText для отображения текста добивания (автопоиск если не назначен)")]
        public TextMeshProUGUI interactionText;
        
        [Tooltip("BrutalityController для выполнения добивания (автопоиск если не назначен)")]
        public DRAUM.Modules.Combat.Animation.BrutalityController brutalityController;
        
        [Header("Debug")]
        [HideInInspector] public bool showDebugLogs = false;
        
        private EnemyEntity _enemyEntity;
        private InteractableObject _interactableObject;
        private int _enemyLayer;
        private FirstPersonController _playerController;
        
        /// <summary>
        /// Публичное свойство для доступа к InteractableObject извне
        /// </summary>
        public InteractableObject InteractableObject => _interactableObject;
        
        private void Awake()
        {
            _enemyLayer = LayerMask.NameToLayer("Enemy");
            int ragdollLayer = LayerMask.NameToLayer("Ragdoll");
            
            bool isOnEnemyLayer = gameObject.layer == _enemyLayer;
            bool isOnRagdollLayer = gameObject.layer == ragdollLayer;
            
            if (!isOnEnemyLayer && !isOnRagdollLayer)
            {
                Debug.LogWarning($"[BrutalityInteractable] Объект {gameObject.name} не на слое Enemy или Ragdoll (текущий слой: {LayerMask.LayerToName(gameObject.layer)}). BrutalityInteractable работает только для объектов на слоях Enemy или Ragdoll!");
                enabled = false;
                return;
            }
            
            if (showDebugLogs)
            {
                string layerName = isOnEnemyLayer ? "Enemy" : "Ragdoll";
                Debug.Log($"[BrutalityInteractable] Объект {gameObject.name} на слое {layerName} - компонент активен");
            }
            
            _enemyEntity = GetComponent<EnemyEntity>();
            if (_enemyEntity == null)
            {
                _enemyEntity = GetComponentInParent<EnemyEntity>();
            }
            
            if (_enemyEntity == null)
            {
                Debug.LogError("[BrutalityInteractable] EnemyEntity не найден!");
                return;
            }
            
            if (playerTransform == null)
            {
                _playerController = FindFirstObjectByType<FirstPersonController>();
                if (_playerController != null)
                {
                    playerTransform = _playerController.transform;
                    if (showDebugLogs)
                    {
                        Debug.Log($"[BrutalityInteractable] Найден Transform игрока через FirstPersonController: {playerTransform.name}");
                    }
                }
                else if (showDebugLogs)
                {
                    Debug.LogWarning("[BrutalityInteractable] FirstPersonController не найден, проверка позиции игрока не будет работать!");
                }
            }
            
            if (interactionText == null)
            {
                InteractionUIController interactionUI = FindFirstObjectByType<InteractionUIController>();
                if (interactionUI != null)
                {
                    interactionText = interactionUI.interactionText;
                    if (showDebugLogs && interactionText != null)
                    {
                        Debug.Log($"[BrutalityInteractable] Найден InteractionText через InteractionUIController: {interactionText.name}");
                    }
                }
                else if (showDebugLogs)
                {
                    Debug.LogWarning("[BrutalityInteractable] InteractionUIController не найден, InteractionText будет null");
                }
            }
            
            if (brutalityController == null)
            {
                brutalityController = FindFirstObjectByType<DRAUM.Modules.Combat.Animation.BrutalityController>();
                if (brutalityController != null && showDebugLogs)
                {
                    Debug.Log($"[BrutalityInteractable] Найден BrutalityController: {brutalityController.name}");
                }
                else if (showDebugLogs)
                {
                    Debug.LogWarning("[BrutalityInteractable] BrutalityController не найден! Добивание не будет работать.");
                }
            }
            
            _interactableObject = GetComponent<InteractableObject>();
            if (_interactableObject == null)
            {
                _interactableObject = GetComponentInParent<InteractableObject>();
            }
            if (_interactableObject == null)
            {
                _interactableObject = gameObject.AddComponent<InteractableObject>();
                if (showDebugLogs)
                {
                    Debug.Log($"[BrutalityInteractable] Создан новый InteractableObject на {gameObject.name}");
                }
            }
            
            if (_interactableObject == null)
            {
                Debug.LogError($"[BrutalityInteractable] Не удалось создать или найти InteractableObject на {gameObject.name}!");
                enabled = false;
                return;
            }
            
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                col = GetComponentInChildren<Collider>();
            }
            if (col == null && showDebugLogs)
            {
                Debug.LogWarning($"[BrutalityInteractable] На объекте {gameObject.name} нет Collider! Raycast может не работать. Убедитесь что на ragdoll частях есть коллайдеры.");
            }
            
            UpdateBrutalityAvailability();
        }
        
        private void Update()
        {
            if (_enemyEntity == null || !_enemyEntity.IsAlive)
            {
                if (_interactableObject != null)
                {
                    _interactableObject.interactionType = InteractionType.Custom;
                    _interactableObject.interactionName = "";
                }
                enabled = false;
                if (showDebugLogs)
                {
                    Debug.Log($"[BrutalityInteractable] Враг мёртв, компонент отключён для {gameObject.name}");
                }
                return;
            }
            
            if (_interactableObject == null)
            {
                _interactableObject = GetComponent<InteractableObject>();
                if (_interactableObject == null)
                {
                    _interactableObject = GetComponentInParent<InteractableObject>();
                }
                if (_interactableObject == null)
                {
                    return;
                }
            }
            
            UpdateBrutalityAvailability();
        }
        
        private void OnDestroy()
        {
        }
        
        /// <summary>
        /// Обновляет доступность добивания в зависимости от позиции игрока сзади врага
        /// </summary>
        private void UpdateBrutalityAvailability()
        {
            if (_enemyEntity == null || _interactableObject == null) return;
            
            if (!_enemyEntity.IsAlive)
            {
                _interactableObject.interactionType = InteractionType.Custom;
                _interactableObject.interactionName = "";
                enabled = false;
                return;
            }
            
            bool canBrutalize = IsPlayerInBrutalityZone();
            
            if (canBrutalize)
            {
                _interactableObject.interactionType = InteractionType.Brutality;
                _interactableObject.interactionName = brutalityInteractionName;
                
                if (showDebugLogs)
                {
                    Debug.Log($"[BrutalityInteractable] Добивание доступно для {gameObject.name} (Type: {_interactableObject.interactionType}, Name: '{_interactableObject.interactionName}', Available: {_interactableObject.IsInteractionAvailable()})");
                }
            }
            else
            {
                _interactableObject.interactionType = InteractionType.Custom;
                _interactableObject.interactionName = "";
            }
        }
        
        /// <summary>
        /// Проверяет находится ли игрок в зоне добивания сзади врага
        /// </summary>
        private bool IsPlayerInBrutalityZone()
        {
            if (playerTransform == null) return false;
            
            Vector3 zoneCenter = transform.TransformPoint(brutalityZoneOffset);
            
            Vector3 playerPos = playerTransform.position;
            Vector3 localPlayerPos = transform.InverseTransformPoint(playerPos);
            Vector3 localZoneCenter = transform.InverseTransformPoint(zoneCenter);
            
            Vector3 halfSize = brutalityZoneSize * 0.5f;
            Vector3 localOffset = localPlayerPos - localZoneCenter;
            
            bool inBox = Mathf.Abs(localOffset.x) <= halfSize.x &&
                        Mathf.Abs(localOffset.y) <= halfSize.y &&
                        Mathf.Abs(localOffset.z) <= halfSize.z;
            
            if (!inBox) return false;
            
            Vector3 toPlayer = (playerPos - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, toPlayer);
            bool isBehind = dot < 0f;
            
            if (showDebugLogs && isBehind && inBox)
            {
                Debug.Log($"[BrutalityInteractable] Игрок в зоне добивания сзади {gameObject.name} (dot: {dot:F2})");
            }
            
            return isBehind && inBox;
        }
        
        /// <summary>
        /// Отрисовывает Box Gizmo зоны добивания в редакторе
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!enabled) return;
            
            Vector3 zoneCenter = transform.TransformPoint(brutalityZoneOffset);
            
            Gizmos.color = gizmoColor;
            
            Gizmos.matrix = Matrix4x4.TRS(zoneCenter, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, brutalityZoneSize);

            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireCube(Vector3.zero, brutalityZoneSize);

            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, zoneCenter);
        }
    }
}
