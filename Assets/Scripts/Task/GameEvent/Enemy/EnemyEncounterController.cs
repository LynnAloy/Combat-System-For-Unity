using System;
using GameDefinitions;
using GameplayEvents.Location;
using UnityEngine;
using FlexibleTaskSystem;

namespace GameplayEvents.Enemy
{
    [DisallowMultipleComponent]
    public sealed class EnemyEncounterController : MonoBehaviour
    {
        [Header("Encounter")]
        [SerializeField] private EnemyEncounterDefinitionSO encounter;
        [SerializeField] private GameObject enemyContainer;

        [Header("Activation")]
        [SerializeField] private LocationEnteredSignalSO locationEnteredSignal;
        [SerializeField] private LocationDefinitionSO activationLocation;
        [SerializeField] private bool deactivateEnemiesOnAwake = true;

        [Header("Output")]
        [SerializeField] private EnemyDefeatedSignalSO enemyDefeatedSignal;

        [Header("Task Interruption")]
        [SerializeField] private AllTasks taskSequence;
        [SerializeReference] private KillEnemyGroupTask encounterTask = new();

        private EnemyController[] enemies = Array.Empty<EnemyController>();
        private IDisposable locationSubscription;
        private int defeatedCount;
        private bool activated;

        private void Awake()
        {
            if (enemyContainer == null)
            {
                Debug.LogError("Enemy container is not assigned.", this);
                return;
            }

            enemies = enemyContainer.GetComponentsInChildren<EnemyController>(true);

            if (deactivateEnemiesOnAwake)
            {
                enemyContainer.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (locationEnteredSignal != null)
            {
                locationSubscription = GameEventHub.Subscribe(
                    locationEnteredSignal,
                    OnLocationEntered);
            }

            foreach (EnemyController enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.OnDead += OnEnemyDied;
                }
            }
        }

        private void OnDisable()
        {
            locationSubscription?.Dispose();
            locationSubscription = null;

            foreach (EnemyController enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.OnDead -= OnEnemyDied;
                }
            }
        }

        private void OnLocationEntered(LocationEnteredEvent payload)
        {
            if (activated || payload.Location != activationLocation)
            {
                return;
            }

            MeeleFighter target = payload.Visitor.GetComponentInChildren<MeeleFighter>(true);

            if (target == null)
            {
                Debug.LogError(
                    $"Encounter visitor '{payload.Visitor.name}' has no MeeleFighter.",
                    this);

                return;
            }

            ActivateEncounter(target);
        }

        public void ActivateEncounter(MeeleFighter target)
        {
            if (activated)
            {
                return;
            }

            if (target == null)
            {
                Debug.LogError("Enemy encounter target is not assigned.", this);
                return;
            }

            if (encounter == null)
            {
                Debug.LogError("Enemy encounter definition is not assigned.", this);
                return;
            }

            if (enemyDefeatedSignal == null)
            {
                Debug.LogError("Enemy defeated signal is not assigned.", this);
                return;
            }

            if (taskSequence == null)
            {
                Debug.LogError("Task sequence is not assigned.", this);
                return;
            }

            if (enemyContainer == null || enemies.Length == 0)
            {
                Debug.LogError("Enemy encounter contains no enemies.", this);
                return;
            }

            encounterTask.Configure(
                enemyDefeatedSignal,
                encounter,
                enemies.Length);

            if (!taskSequence.TryStartInterrupt(encounterTask))
            {
                Debug.LogError("Could not start the enemy encounter task.", this);
                return;
            }

            defeatedCount = 0;
            activated = true;

            enemyContainer.SetActive(true);

            foreach (EnemyController enemy in enemies)
            {
                if (enemy == null || enemy.TryEngageTarget(target))
                {
                    continue;
                }

                Debug.LogWarning(
                    $"Enemy '{enemy.name}' could not engage the encounter target.",
                    enemy);
            }
        }

        private void OnEnemyDied(EnemyController enemy)
        {
            if (!activated)
            {
                return;
            }

            defeatedCount = Mathf.Min(defeatedCount + 1, enemies.Length);

            EnemyDefeatedEvent payload = new EnemyDefeatedEvent(
                encounter,
                enemy,
                defeatedCount,
                enemies.Length);

            GameEventHub.Publish(enemyDefeatedSignal, payload);
        }
    }
}