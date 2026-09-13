using GameDefinitions;
using Inventory.Model;
using UnityEngine;

namespace GameplayEvents.Inventory
{
    public readonly struct ItemSubmittedEvent
    {
        public string RequestId { get; }
        public NpcDefinitionSO Npc { get; }
        public ItemSO Item { get; }
        public int Quantity { get; }
        public bool Succeeded { get; }
        public string FailureReason { get; }
        public GameObject Interactor { get; }
        public GameObject Source { get; }

        public ItemSubmittedEvent(
            ItemSubmissionRequestedEvent request,
            bool succeeded,
            string failureReason)
        {
            RequestId = request.RequestId;
            Npc = request.Npc;
            Item = request.Item;
            Quantity = request.Quantity;
            Succeeded = succeeded;
            FailureReason = failureReason;
            Interactor = request.Interactor;
            Source = request.Source;
        }
    }
}