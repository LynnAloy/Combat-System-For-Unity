using GameplayEvents;
using UnityEngine;

namespace GameplayEvents.Dialogue
{
    [CreateAssetMenu(fileName = "SIG_DialogueRequested", menuName = "Gameplay Events/Dialogue/Requested")]
    public sealed class DialogueRequestedSignalSO : GameplaySignalSO<DialogueRequestedEvent>
    {
    }
}