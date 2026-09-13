using GameplayEvents;
using UnityEngine;

namespace GameplayEvents.Inventory
{
    [CreateAssetMenu(fileName = "SIG_ItemSubmissionRequested", menuName = "Gameplay Events/Inventory/Submission Requested")]
    public sealed class ItemSubmissionRequestedSignalSO
        : GameplaySignalSO<ItemSubmissionRequestedEvent>
    {
    }
}