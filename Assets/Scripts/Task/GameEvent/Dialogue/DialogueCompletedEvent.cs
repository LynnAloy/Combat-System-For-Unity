using DialogueSystem;
using GameDefinitions;
using UnityEngine;

namespace GameplayEvents.Dialogue
{
    public readonly struct DialogueCompletedEvent
    {
        public string RequestId { get; }
        public NpcDefinitionSO Npc { get; }
        public DialogueSO Dialogue { get; }
        public GameObject Interactor { get; }
        public GameObject Source { get; }

        public DialogueCompletedEvent(DialogueRequestedEvent request)
        {
            RequestId = request.RequestId;
            Npc = request.Npc;
            Dialogue = request.Dialogue;
            Interactor = request.Interactor;
            Source = request.Source;
        }
    }
}