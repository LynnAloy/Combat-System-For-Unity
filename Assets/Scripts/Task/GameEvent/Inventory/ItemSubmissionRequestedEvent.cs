using GameDefinitions;
using Inventory.Model;
using UnityEngine;

namespace GameplayEvents.Inventory
{
    public readonly struct ItemSubmissionRequestedEvent
    {
        public string RequestId { get; }
        public NpcDefinitionSO Npc { get; }
        public ItemSO Item { get; }
        public int Quantity { get; }
        public GameObject Interactor { get; }
        public GameObject Source { get; }

        public ItemSubmissionRequestedEvent(
            string requestId,
            NpcDefinitionSO npc,
            ItemSO item,
            int quantity,
            GameObject interactor,
            GameObject source)
        {
            RequestId = requestId;
            Npc = npc;
            Item = item;
            Quantity = quantity;
            Interactor = interactor;
            Source = source;
        }
    }
}