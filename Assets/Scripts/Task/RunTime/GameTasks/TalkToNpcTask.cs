using System;
using DialogueSystem;
using GameDefinitions;
using GameplayEvents;
using GameplayEvents.Dialogue;
using GameplayEvents.Npc;
using UnityEngine;

namespace FlexibleTaskSystem
{
    [Serializable]
    public sealed class TalkToNpcTask : GameTask, ITaskNavigationSource
    {
        [Header("NPC")]
        [SerializeField]
        private NpcInteractionRequestedSignalSO interactionRequestedSignal;

        [SerializeField] private NpcDefinitionSO targetNpc;

        [Header("Dialogue")]
        [SerializeField] private DialogueSO dialogue;
        [SerializeField] private DialogueRequestedSignalSO dialogueRequestedSignal;
        [SerializeField] private DialogueCompletedSignalSO dialogueCompletedSignal;

        [NonSerialized] private IDisposable interactionSubscription;
        [NonSerialized] private IDisposable completionSubscription;
        [NonSerialized] private string activeRequestId;

        protected override void OnStart()
        {
            if (interactionRequestedSignal == null)
            {
                Fail("NPC interaction requested signal is not assigned.");
                return;
            }

            if (targetNpc == null)
            {
                Fail("Target NPC is not assigned.");
                return;
            }

            if (dialogue == null)
            {
                Fail("Dialogue is not assigned.");
                return;
            }

            if (dialogueRequestedSignal == null)
            {
                Fail("Dialogue requested signal is not assigned.");
                return;
            }

            if (dialogueCompletedSignal == null)
            {
                Fail("Dialogue completed signal is not assigned.");
                return;
            }

            interactionSubscription = GameEventHub.Subscribe(
                interactionRequestedSignal,
                OnInteractionRequested);

            completionSubscription = GameEventHub.Subscribe(
                dialogueCompletedSignal,
                OnDialogueCompleted);
        }

        protected override void OnEnd(GameTaskState finalState)
        {
            ReleaseSubscriptions();
        }

        protected override void OnReset()
        {
            ReleaseSubscriptions();
        }

        private void OnInteractionRequested(
            NpcInteractionRequestedEvent payload)
        {
            if (payload.Npc != targetNpc)
            {
                return;
            }

            activeRequestId = Guid.NewGuid().ToString("N");

            DialogueRequestedEvent request =
                new DialogueRequestedEvent(
                    activeRequestId,
                    targetNpc,
                    dialogue,
                    payload.Interactor,
                    payload.Source);

            GameEventHub.Publish(
                dialogueRequestedSignal,
                request);
        }

        private void OnDialogueCompleted(
            DialogueCompletedEvent payload)
        {
            if (string.IsNullOrEmpty(activeRequestId) ||
                payload.RequestId != activeRequestId)
            {
                return;
            }

            activeRequestId = null;
            Complete();
        }

        private void ReleaseSubscriptions()
        {
            interactionSubscription?.Dispose();
            completionSubscription?.Dispose();

            interactionSubscription = null;
            completionSubscription = null;
            activeRequestId = null;
        }

        public bool TryGetNavigationTarget(out TaskNavigationTarget target)
        {
            target = TaskNavigationTarget.FromDefinition(targetNpc);
            return targetNpc != null;
        }
    }
}