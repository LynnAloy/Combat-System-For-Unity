using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AICombatStates
{
    Idle,
    Chase,
    Circiling
}
public class CombatMovementState : State<EnemyController>
{
    private EnemyController enemy;
    private AICombatStates combatState;
    [SerializeField] private float chaseStateStopDistance;
    [SerializeField] private float toChaseStateThreshold;
    [SerializeField] private float toIdleStateThreshold;
    [SerializeField] private Vector2 idleTimeRange;
    [SerializeField] private Vector2 circlingTimeRange;
    [SerializeField] private float circlingSpeed;

    private float timer;
    private int circlingDirection;
    public override void Enter(EnemyController owner)
    {
        //Debug.Log("Entering Chase State");
        enemy = owner;
        enemy.NavAgent.stoppingDistance = chaseStateStopDistance;
        enemy.CombatMovementTimer = 0f;
        enemy.Animator.SetBool("InCombat", true);
    }

    public override void Execute()
    {
        if(enemy.Target == null)
        {
            enemy.Target = enemy.FindTarget();
            if(enemy.Target == null)
            {
                enemy.ChangeState(EnemyStates.Idle);
                return;
            }
        }
        if(enemy.Target.Health <= 0)
        {
            enemy.Target = null;
            enemy.ChangeState(EnemyStates.Idle);
            return;
        }
        if(Vector3.Distance(enemy.transform.position, enemy.Target.transform.position) > chaseStateStopDistance + toChaseStateThreshold)
        {
            StartChase();
        }
        if (combatState == AICombatStates.Idle)
        {
            if(timer <= 0)
            {
                if(Random.Range(0, 2) == 0)
                {
                    StartIdle();
                }
                else
                {
                    StartCircling();
                }
            }
        }
        else if(combatState == AICombatStates.Chase)
        { 
            if (Vector3.Distance(enemy.transform.position, enemy.Target.transform.position) <= chaseStateStopDistance + toIdleStateThreshold)
            {
                StartIdle();
                return;
            }
            enemy.NavAgent.SetDestination(enemy.Target.transform.position);
        }
        else if(combatState == AICombatStates.Circiling)
        {
            if(timer <= 0)
            {
                StartIdle();
                return;
            }
            //transform.RotateAround(enemy.Target.transform.position, Vector3.up, circlingDirection * Time.deltaTime * circlingSpeed);
            var vectorToTarget = enemy.transform.position - enemy.Target.transform.position;
            var rotatedPosition = Quaternion.Euler(0, circlingDirection * Time.deltaTime * circlingSpeed, 0) * vectorToTarget;
            enemy.NavAgent.Move(rotatedPosition - vectorToTarget);
            enemy.transform.rotation = Quaternion.LookRotation(-rotatedPosition);
        }
        if(timer > 0)
        {
            timer -= Time.deltaTime;
        }  
        enemy.CombatMovementTimer += Time.deltaTime;
    }
      
    public override void Exit()
    {
        Debug.Log("Exiting Chase State");    
        enemy.CombatMovementTimer = 0f;
    }

    private void StartChase()
    {
        combatState = AICombatStates.Chase;
        //enemy.Animator.SetBool("InCombat", false);
        //enemy.Animator.SetBool("ToCircling", false);

    }

    private void StartIdle()
    {
        combatState = AICombatStates.Idle;
        //enemy.Animator.SetBool("InCombat", true);
        timer = Random.Range(idleTimeRange.x, idleTimeRange.y);
        //enemy.Animator.SetBool("ToCircling", false);
    }

    private void StartCircling()
    {
        combatState = AICombatStates.Circiling;
        timer = Random.Range(circlingTimeRange.x, circlingTimeRange.y);
        circlingDirection = Random.Range(0, 2) == 0 ? -1 : 1;

        //enemy.Animator.SetBool("ToCircling", true);
        //enemy.Animator.SetFloat("CirclingDirection", circlingDirection);
    }
}
