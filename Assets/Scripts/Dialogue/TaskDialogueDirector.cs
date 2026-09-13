using System;
using GameplayEvents;
using GameplayEvents.Dialogue;
using UnityEngine;

namespace DialogueSystem
{
    [DisallowMultipleComponent]
    public sealed class TaskDialogueDirector : MonoBehaviour
    {
        [SerializeField] private DialogueController dialogueController;
        [SerializeField] private DialogueRequestedSignalSO requestedSignal;
        [SerializeField] private DialogueCompletedSignalSO completedSignal;

        private IDisposable requestSubscription;

        private void OnEnable()
        {
            if (dialogueController == null)
            {
                Debug.LogError("Dialogue controller is not assigned.", this);

                return;
            }

            if (requestedSignal == null)
            {
                Debug.LogError("Dialogue requested signal is not assigned.", this);

                return;
            }

            if (completedSignal == null)
            {
                Debug.LogError("Dialogue completed signal is not assigned.", this);

                return;
            }

            requestSubscription = GameEventHub.Subscribe(requestedSignal, OnDialogueRequested);
        }

        private void OnDisable()
        {
            requestSubscription?.Dispose();
            requestSubscription = null;
        }

        private void OnDialogueRequested(
            DialogueRequestedEvent request)
        {
            bool started = dialogueController.TryStartDialogue(request.Dialogue, () => PublishCompleted(request));

            if (!started)
            {
                Debug.LogWarning($"Could not start dialogue request " + $"'{request.RequestId}'.", this);
            }
        }

        private void PublishCompleted(DialogueRequestedEvent request)
        {
            DialogueCompletedEvent payload = new DialogueCompletedEvent(request);

            GameEventHub.Publish(completedSignal, payload);
        }
    }
}