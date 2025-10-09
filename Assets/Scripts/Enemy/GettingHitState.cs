using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GettingHitState : State<EnemyController>
{
    [SerializeField] private float stunningTime;
    private EnemyController enemy;

    public override void Enter(EnemyController owner)
    {
        StopAllCoroutines();
        enemy = owner;
        enemy.Fighter.OnGotHitComplete += () => StartCoroutine(GoToCombatMovement());
    }

    private IEnumerator GoToCombatMovement()
    {
        yield return new WaitForSeconds(stunningTime);
        if (!enemy.IsInState(EnemyStates.Dead))
        {
            enemy.ChangeState(EnemyStates.CombatMovement);
        }
    }
}
