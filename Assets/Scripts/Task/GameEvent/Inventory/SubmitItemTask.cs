using System;
using GameDefinitions;
using GameplayEvents;
using GameplayEvents.Inventory;
using Inventory.Model;
using UnityEngine;

namespace FlexibleTaskSystem
{
    [Serializable]
    public sealed class SubmitItemTask : GameTask
    {
        [Header("Submission")]
        [SerializeField] private NpcDefinitionSO targetNpc;
        [SerializeField] private ItemSO targetItem;
        [SerializeField, Min(1)] private int requiredQuantity = 1;

        [Header("Signals")]
        [SerializeField]
        private ItemSubmissionRequestedSignalSO submissionRequestedSignal;

        [SerializeField] private ItemSubmittedSignalSO itemSubmittedSignal;

        [NonSerialized] private IDisposable resultSubscription;
        [NonSerialized] private string requestId;

        protected override void OnStart()
        {
            if (targetNpc == null)
            {
                Fail("Target NPC is not assigned.");
                return;
            }

            if (targetItem == null)
            {
                Fail("Target item is not assigned.");
                return;
            }

            if (submissionRequestedSignal == null)
            {
                Fail("Submission requested signal is not assigned.");
                return;
            }

            if (itemSubmittedSignal == null)
            {
                Fail("Item submitted signal is not assigned.");
                return;
            }

            requiredQuantity = Mathf.Max(1, requiredQuantity);
            SetProgress(0, requiredQuantity);

            resultSubscription = GameEventHub.Subscribe(
                itemSubmittedSignal,
                OnItemSubmitted);

            requestId = Guid.NewGuid().ToString("N");

            ItemSubmissionRequestedEvent request =
                new ItemSubmissionRequestedEvent(
                    requestId,
                    targetNpc,
                    targetItem,
                    requiredQuantity,
                    null,
                    Context.Owner);

            GameEventHub.Publish(
                submissionRequestedSignal,
                request);
        }

        protected override void OnEnd(GameTaskState finalState)
        {
            ReleaseSubscription();
        }

        protected override void OnReset()
        {
            ReleaseSubscription();
        }

        private void OnItemSubmitted(ItemSubmittedEvent payload)
        {
            if (string.IsNullOrEmpty(requestId) ||
                payload.RequestId != requestId ||
                payload.Npc != targetNpc ||
                payload.Item != targetItem)
            {
                return;
            }

            if (!payload.Succeeded)
            {
                Fail(payload.FailureReason);
                return;
            }

            SetProgress(payload.Quantity, requiredQuantity);
            Complete();
        }

        private void ReleaseSubscription()
        {
            resultSubscription?.Dispose();
            resultSubscription = null;
            requestId = null;
        }
    }
}