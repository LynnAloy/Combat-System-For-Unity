using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public sealed class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private TMP_Text promptText;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    public void Show(string message)
    {
        if (promptText != null)
        {
            promptText.text = message;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void Hide()
    {
        if (promptText != null)
        {
            promptText.text = string.Empty;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}