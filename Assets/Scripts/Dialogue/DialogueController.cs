using System;
using UnityEngine;

namespace DialogueSystem
{
    public sealed class DialogueController : MonoBehaviour
    {
        [SerializeField] private DialogueUI dialogueUI;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private KeyCode cancelKey = KeyCode.Escape;

        private DialogueSO currentDialogue;
        private Action completionCallback;
        private PauseToken dialoguePause;
        private int currentLineIndex;
        private bool isOpen;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (cameraController == null && Camera.main != null)
            {
                cameraController =
                    Camera.main.GetComponent<CameraController>();
            }

            if (dialogueUI != null)
            {
                dialogueUI.ContinueRequested += ShowNextLine;
            }
        }

        private void Update()
        {
            if (isOpen && Input.GetKeyDown(cancelKey))
            {
                CloseDialogue(false);
            }
        }

        private void OnDisable()
        {
            if (isOpen)
            {
                CloseDialogue(false);
            }
        }

        private void OnDestroy()
        {
            if (dialogueUI != null)
            {
                dialogueUI.ContinueRequested -= ShowNextLine;
            }

            ReleaseDialoguePause();
        }

        public bool TryStartDialogue(
            DialogueSO dialogue,
            Action onCompleted = null)
        {
            if (isOpen)
            {
                return false;
            }

            if (dialogue == null ||
                dialogue.Lines == null ||
                dialogue.Lines.Count == 0)
            {
                Debug.LogError(
                    "Cannot start an empty dialogue.",
                    this);
                return false;
            }

            currentDialogue = dialogue;
            completionCallback = onCompleted;
            currentLineIndex = 0;
            isOpen = true;

            dialoguePause = PauseManager.Acquire(PauseReason.Dialogue);

            if (cameraController != null)
            {
                cameraController.SetCameraEnabled(false);
            }

            dialogueUI.Show();
            DisplayCurrentLine();

            return true;
        }

        private void ShowNextLine()
        {
            if (!isOpen || currentDialogue == null)
            {
                return;
            }

            currentLineIndex++;

            if (currentLineIndex >= currentDialogue.Lines.Count)
            {
                CloseDialogue(true);
                return;
            }

            DisplayCurrentLine();
        }

        private void DisplayCurrentLine()
        {
            DialogueLine line =
                currentDialogue.Lines[currentLineIndex];

            dialogueUI.SetLine(line.Speaker, line.Text);

            bool isLastLine =
                currentLineIndex == currentDialogue.Lines.Count - 1;

            dialogueUI.SetContinueLabel(isLastLine);
        }

        private void CloseDialogue(bool completed)
        {
            if (!isOpen)
            {
                return;
            }

            Action callback = completionCallback;

            isOpen = false;
            currentDialogue = null;
            completionCallback = null;
            currentLineIndex = 0;

            dialogueUI.Hide();
            ReleaseDialoguePause();

            if (cameraController != null && !PauseManager.IsPaused)
            {
                cameraController.SetCameraEnabled(true);
            }

            if (completed)
            {
                callback?.Invoke();
            }
        }

        private void ReleaseDialoguePause()
        {
            dialoguePause?.Dispose();
            dialoguePause = null;
        }
    }
}