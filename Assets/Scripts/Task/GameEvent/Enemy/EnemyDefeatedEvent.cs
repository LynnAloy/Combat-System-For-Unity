using GameDefinitions;

namespace GameplayEvents.Enemy
{
    public readonly struct EnemyDefeatedEvent
    {
        public EnemyEncounterDefinitionSO Encounter { get; }
        public EnemyController Enemy { get; }
        public int DefeatedCount { get; }
        public int TotalCount { get; }

        public EnemyDefeatedEvent(
            EnemyEncounterDefinitionSO encounter,
            EnemyController enemy,
            int defeatedCount,
            int totalCount)
        {
            Encounter = encounter;
            Enemy = enemy;
            DefeatedCount = defeatedCount;
            TotalCount = totalCount;
        }
    }
}