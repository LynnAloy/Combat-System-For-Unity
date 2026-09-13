using GameplayEvents;
using UnityEngine;

namespace GameplayEvents.Tasks
{
    [CreateAssetMenu(fileName = "SIG_TaskCompleted", menuName = "Gameplay Events/Task/Completed")]
    public sealed class TaskCompletedSignalSO : GameplaySignalSO<TaskCompletedEvent>
    {
    }
}