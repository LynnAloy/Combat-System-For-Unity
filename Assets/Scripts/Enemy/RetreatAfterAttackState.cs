using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class RetreatAfterAttackState : State<EnemyController>
{
    private EnemyController enemy;
    private Vector3 targetPosition;
    [SerializeField] private float retreatSpeed;
    [SerializeField] private float retreatRotateSpeed;
    [SerializeField] private float stopRetreatDistance;
    public override void Enter(EnemyController owner)
    {
        enemy = owner;
        targetPosition = enemy.Target.transform.position;
        //enemy.NavAgent.ResetPath();
        //enemy.enemyPrePosition = enemy.transform.position;
    }

    public override void Execute()
    {
        if(Vector3.Distance(enemy.transform.position, targetPosition) >= stopRetreatDistance)
        {
            enemy.ChangeState(EnemyStates.CombatMovement);
            return;
        }
        var vectorToTarget = enemy.Target.transform.position - enemy.transform.position;
        enemy.NavAgent.Move(retreatSpeed * Time.deltaTime * -vectorToTarget.normalized);
        vectorToTarget.y = 0; // Ensure we only consider horizontal distance
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(vectorToTarget), retreatRotateSpeed * Time.deltaTime);
    }

}
