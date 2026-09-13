using DialogueSystem;
using GameDefinitions;
using UnityEngine;

namespace GameplayEvents.Dialogue
{
    public readonly struct DialogueRequestedEvent
    {
        public string RequestId { get; }
        public NpcDefinitionSO Npc { get; }
        public DialogueSO Dialogue { get; }
        public GameObject Interactor { get; }
        public GameObject Source { get; }

        public DialogueRequestedEvent(
            string requestId,
            NpcDefinitionSO npc,
            DialogueSO dialogue,
            GameObject interactor,
            GameObject source)
        {
            RequestId = requestId;
            Npc = npc;
            Dialogue = dialogue;
            Interactor = interactor;
            Source = source;
        }
    }
}