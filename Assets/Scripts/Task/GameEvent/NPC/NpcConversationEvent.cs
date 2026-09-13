using DialogueSystem;
using GameDefinitions;
using UnityEngine;

namespace GameplayEvents.Npc
{
    public readonly struct NpcConversationEvent
    {
        public NpcDefinitionSO Npc { get; }
        public DialogueSO Dialogue { get; }
        public GameObject Source { get; }

        public NpcConversationEvent(
            NpcDefinitionSO npc,
            DialogueSO dialogue,
            GameObject source)
        {
            Npc = npc;
            Dialogue = dialogue;
            Source = source;
        }
    }
}