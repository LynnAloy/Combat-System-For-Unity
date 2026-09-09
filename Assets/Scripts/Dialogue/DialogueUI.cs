using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DialogueSystem
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class DialogueUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text contentText;
        [SerializeField] private Button continueButton;
        [SerializeField] private TMP_Text continueButtonText;

        private CanvasGroup canvasGroup;

        public event Action ContinueRequested;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }

            Hide();
        }

        private void OnDestroy()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueClicked);
            }
        }

        public void Show()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (speakerText != null)
            {
                speakerText.text = string.Empty;
            }

            if (contentText != null)
            {
                contentText.text = string.Empty;
            }
        }

        public void SetLine(string speaker, string content)
        {
            if (speakerText != null)
            {
                speakerText.text = speaker;
                speakerText.gameObject.SetActive(
                    !string.IsNullOrWhiteSpace(speaker));
            }

            if (contentText != null)
            {
                contentText.text = content;
            }
        }

        public void SetContinueLabel(bool isLastLine)
        {
            if (continueButtonText != null)
            {
                continueButtonText.text = isLastLine ? "结束" : "继续";
            }
        }

        private void OnContinueClicked()
        {
            ContinueRequested?.Invoke();
        }
    }
}