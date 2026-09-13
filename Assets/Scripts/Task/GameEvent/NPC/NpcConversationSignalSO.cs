using UnityEngine;

namespace GameplayEvents.Npc
{
    [CreateAssetMenu(fileName = "SIG_NpcConversationCompleted", menuName = "Gameplay Events/NPC/Conversation Completed")]
    public sealed class NpcConversationSignalSO :
        GameplaySignalSO<NpcConversationEvent>
    {
    }
}