using System;
using GameDefinitions;
using GameplayEvents;
using GameplayEvents.Enemy;
using UnityEngine;

namespace FlexibleTaskSystem
{
    [Serializable]
    public sealed class KillEnemyGroupTask : GameTask
    {
        [SerializeField] private EnemyDefeatedSignalSO enemyDefeatedSignal;
        [SerializeField] private EnemyEncounterDefinitionSO targetEncounter;
        [NonSerialized] private IDisposable subscription;
        [NonSerialized] private int requiredEnemyCount = 1;

        protected override void OnStart()
        {
            if (enemyDefeatedSignal == null)
            {
                Fail("Enemy defeated signal is not assigned.");
                return;
            }

            if (targetEncounter == null)
            {
                Fail("Target encounter is not assigned.");
                return;
            }

            SetProgress(0, requiredEnemyCount);

            subscription = GameEventHub.Subscribe(enemyDefeatedSignal, OnEnemyDefeated);
        }

        protected override void OnEnd(GameTaskState finalState)
        {
            ReleaseSubscription();
        }

        protected override void OnReset()
        {
            ReleaseSubscription();
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent payload)
        {
            if (payload.Encounter != targetEncounter)
            {
                return;
            }

            SetProgress(payload.DefeatedCount, payload.TotalCount);

            if (payload.DefeatedCount >= payload.TotalCount)
            {
                Complete();
            }
        }

        private void ReleaseSubscription()
        {
            subscription?.Dispose();
            subscription = null;
        }

        public void Configure(EnemyDefeatedSignalSO signal, EnemyEncounterDefinitionSO encounter, int enemyCount)
        {
            enemyDefeatedSignal = signal;
            targetEncounter = encounter;
            requiredEnemyCount = Mathf.Max(1, enemyCount);
        }
    }
}