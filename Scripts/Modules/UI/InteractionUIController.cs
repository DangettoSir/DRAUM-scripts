using UnityEngine;
using TMPro;

/// <summary>
/// Контроллер UI для отображения подсказок взаимодействия
/// Использует данные от FirstPersonController
/// </summary>
public class InteractionUIController : MonoBehaviour
{
    [Header("References")]
    public FirstPersonController playerController;
    public TextMeshProUGUI interactionText;
    public string interactionPrompt = "[F] {0}"; // {0} заменится на название предмета

    private void Start()
    {
        if (playerController == null)
        {
            playerController = UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();
            if (playerController != null)
            {
                Debug.Log($"[InteractionUI] FirstPersonController найден автоматически");
            }
            else
            {
                Debug.LogError("[InteractionUI] FirstPersonController не найден!");
            }
        }

        if (interactionText == null)
        {
            interactionText = GetComponentInChildren<TextMeshProUGUI>();
            if (interactionText != null)
            {
                Debug.Log($"[InteractionUI] TextMeshProUGUI найден: {interactionText.name}");
            }
            else
            {
                Debug.LogError("[InteractionUI] TextMeshProUGUI не найден!");
            }
        }
    }

    private void Update()
    {
        if (playerController != null && interactionText != null)
        {
            if (playerController.IsLookingAtInteractable())
            {
                // Показываем UI
                string itemName = playerController.GetCurrentInteractionName();
                interactionText.text = string.Format(interactionPrompt, itemName);
                interactionText.gameObject.SetActive(true);
            }
            else
            {
                // СКРЫВАЕМ UI когда не смотрим на объект
                interactionText.gameObject.SetActive(false);
            }
        }
    }
}

