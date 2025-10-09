using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackState : State<EnemyController>
{
    [SerializeField] private float attackDistance;
    //private int comboCount = 1;
    private EnemyController enemy;
    private bool isAttacking;
    public override void Enter(EnemyController owner)
    {
        enemy = owner;
        enemy.NavAgent.stoppingDistance = attackDistance;
    }

    public override void Execute()
    {
        if(isAttacking)
        {
            return;
        }

        enemy.NavAgent.SetDestination(enemy.Target.transform.position);

        if(Vector3.Distance(enemy.transform.position, enemy.Target.transform.position) <= attackDistance + 0.05f)
        {
            StartCoroutine(Attack(Random.Range(0, enemy.Fighter.Attacks.Count + 1 )));
        }
    }

    IEnumerator Attack(int comboCount = 1)
    {
        isAttacking = true;
        enemy.Animator.applyRootMotion = true;
        enemy.Fighter.TryToAttack(enemy.Target);
        for(int i = 1; i < comboCount; i++)
        {
            yield return new WaitUntil(() => enemy.Fighter.AttackState == InCombat.AttackState.Cooldown);
            enemy.Fighter.TryToAttack(enemy.Target);
        }
        yield return new WaitUntil(() => enemy.Fighter.AttackState == InCombat.AttackState.Idle);
        enemy.Animator.applyRootMotion = false;
        isAttacking = false;
        if(enemy.IsInState(EnemyStates.Attack))
        {
            enemy.ChangeState(EnemyStates.RetreatAfterAttack);
        }
        
    }

    public override void Exit()
    {
        enemy.NavAgent.ResetPath();
        //isAttacking = false;
    }
}
