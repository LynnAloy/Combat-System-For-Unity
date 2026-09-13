using GameDefinitions;
using UnityEngine;

namespace GameplayEvents.Npc
{
    public readonly struct NpcInteractionRequestedEvent
    {
        public NpcDefinitionSO Npc { get; }
        public GameObject Interactor { get; }
        public GameObject Source { get; }

        public NpcInteractionRequestedEvent(NpcDefinitionSO npc, GameObject interactor, GameObject source)
        {
            Npc = npc;
            Interactor = interactor;
            Source = source;
        }
    }
}