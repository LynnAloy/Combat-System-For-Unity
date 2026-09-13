using GameplayEvents;
using UnityEngine;

namespace GameplayEvents.Dialogue
{
    [CreateAssetMenu(fileName = "SIG_DialogueCompleted", menuName = "Gameplay Events/Dialogue/Completed")]
    public sealed class DialogueCompletedSignalSO : GameplaySignalSO<DialogueCompletedEvent>
    {
    }
}