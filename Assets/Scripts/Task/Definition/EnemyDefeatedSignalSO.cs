using UnityEngine;

namespace GameplayEvents.Enemy
{
    [CreateAssetMenu(
        fileName = "SIG_EnemyDefeated",
        menuName = "Gameplay Events/Enemy/Defeated")]
    public sealed class EnemyDefeatedSignalSO : GameplaySignalSO<EnemyDefeatedEvent>
    {
    }
}