using GameplayEvents;
using UnityEngine;

namespace GameplayEvents.Npc
{
    [CreateAssetMenu(fileName = "SIG_NpcInteractionRequested", menuName = "Gameplay Events/NPC/Interaction Requested")]
    public sealed class NpcInteractionRequestedSignalSO : GameplaySignalSO<NpcInteractionRequestedEvent>
    {
    }
}