using GameplayEvents;
using UnityEngine;

namespace GameplayEvents.Inventory
{
    [CreateAssetMenu(
        fileName = "SIG_ItemSubmitted",
        menuName = "Gameplay Events/Inventory/Submitted")]
    public sealed class ItemSubmittedSignalSO
        : GameplaySignalSO<ItemSubmittedEvent>
    {
    }
}