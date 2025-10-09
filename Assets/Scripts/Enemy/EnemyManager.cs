using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyManager : Singleton<EnemyManager>
{
    [SerializeField] private Vector2 timeRangeBetweenAttack;
    [SerializeField] private CombatController player;
    [field: SerializeField] public LayerMask EnemyLayer { get; private set; }
    private List<EnemyController> enemiesInRange = new();
    private float stopAttackingTimer = 2f;
    private float timer = 0f;

    private void Update()
    {
        if(enemiesInRange.Count == 0)
        {
            return;
        }
        if (!enemiesInRange.Any(e => e.IsInState(EnemyStates.Attack)))
        {
            if(stopAttackingTimer > 0)
            {
                stopAttackingTimer -= Time.deltaTime;
            }
            if (stopAttackingTimer <= 0)
            {
                var attackingEnemy = SelectEnemyForAttack();
                if (attackingEnemy != null)
                {
                    attackingEnemy.ChangeState(EnemyStates.Attack);
                    stopAttackingTimer = Random.Range(timeRangeBetweenAttack.x, timeRangeBetweenAttack.y);
                }
            }
        }
        if (timer > 0.1f)
        {
            var closestEnemy = GetClosestEnemy(player.GetTargetingDirection());
            timer = 0f;
            if(closestEnemy != null && closestEnemy != player.TargetEnemy)
            {
                var preEnemy = player.TargetEnemy;
                player.TargetEnemy = closestEnemy;
                player?.TargetEnemy?.SkinnedMeshHighlighter?.HighlightMesh(true);
                preEnemy?.SkinnedMeshHighlighter?.HighlightMesh(false);
            }
        }
        timer += Time.deltaTime;
    }

    public void AddEnemyInRange(EnemyController enemy)
    {
        if (!enemiesInRange.Contains(enemy))
        {
            enemiesInRange.Add(enemy);
        }
    }

    public void RemoveEnemyInRange(EnemyController enemy)
    { 
        enemiesInRange.Remove(enemy);
        if(enemy == player.TargetEnemy)
        {
            enemy.SkinnedMeshHighlighter?.HighlightMesh(false);
            player.TargetEnemy = GetClosestEnemy(player.GetTargetingDirection());
            player.TargetEnemy?.SkinnedMeshHighlighter?.HighlightMesh(true);
        }
        
    }

    private EnemyController SelectEnemyForAttack()
    {
        return enemiesInRange.OrderByDescending(e => e.CombatMovementTimer).FirstOrDefault( e => e.Target != null && e.IsInState(EnemyStates.CombatMovement));
    }

    public EnemyController GetAttackingEnemy()
    {
        return enemiesInRange.FirstOrDefault(e => e.IsInState(EnemyStates.Attack));
    }

    public EnemyController GetClosestEnemy(Vector3 direction)
    {
        //var targetingDirection = player.GetTargetingDirection();
        float minDistance = float.MaxValue;
        EnemyController closestEnemy = null;
        foreach (var enemy in enemiesInRange)
        {
            var vectorToEnemy = enemy.transform.position - player.transform.position;
            vectorToEnemy.y = 0;
            float angle = Vector3.Angle(direction, vectorToEnemy);
            float distance = vectorToEnemy.magnitude * Mathf.Sin(angle * Mathf.Deg2Rad);
            if(distance < minDistance)
            {
                minDistance = distance;
                closestEnemy = enemy;
            }
        }
        return closestEnemy;
    }
}
