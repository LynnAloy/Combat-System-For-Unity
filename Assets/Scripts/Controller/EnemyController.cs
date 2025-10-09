using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyStates
{
    Idle,
    CombatMovement,
    Attack,
    RetreatAfterAttack,
    Dead,
    GettingHit
}
public class EnemyController : MonoBehaviour
{
    public StateMachine<EnemyController> StateMachine { get; private set; }
    public List<MeeleFighter> TargetsInRange { get; private set; } = new();
    private Dictionary<EnemyStates, State<EnemyController>> stateDictionary;
    [field: SerializeField] public float FOV { get; private set; }
    [SerializeField] private Vector3 enemyAlertRange;
    public MeeleFighter Target { get; set; }
    public NavMeshAgent NavAgent { get; private set; }
    public Animator Animator { get; private set; }
    public float CombatMovementTimer { get; set; } = 0f;
    public MeeleFighter Fighter { get; private set; }
    public VisionSensor VisionSensor { get; set; }
    public CharacterController CharacterController { get; private set; }
    public SkinnedMeshHighlighter SkinnedMeshHighlighter { get; private set; }



    private void Start()
    {
        stateDictionary = new()
        {
            [EnemyStates.Idle] = GetComponent<IdleState>(),
            [EnemyStates.CombatMovement] = GetComponent<CombatMovementState>(),
            [EnemyStates.Attack] = GetComponent<AttackState>(),
            [EnemyStates.RetreatAfterAttack] = GetComponent<RetreatAfterAttackState>(),
            [EnemyStates.Dead] = GetComponent<DeadState>(),
            [EnemyStates.GettingHit] = GetComponent<GettingHitState>()
        };
        NavAgent = GetComponent<NavMeshAgent>();
        Animator = GetComponent<Animator>();
        StateMachine = new StateMachine<EnemyController>(this);
        StateMachine.ChangeState(stateDictionary[EnemyStates.Idle]);
        Fighter = GetComponent<MeeleFighter>();
        CharacterController = GetComponent<CharacterController>();
        SkinnedMeshHighlighter = GetComponent<SkinnedMeshHighlighter>();
        Fighter.OnGotHit += (MeeleFighter attacker) =>
        {
            if (Fighter.Health > 0)
            {
                if(Target == null)
                {
                    Target = attacker;
                    AlertNearbyEnemies();
                }

                ChangeState(EnemyStates.GettingHit);
            }
            else
            {
                ChangeState(EnemyStates.Dead);
            }
        };
    }

    private void Update()
    {
        StateMachine.Execute();

        var deltaPosition = Animator.applyRootMotion ? Vector3.zero : transform.position - enemyPrePosition;
        var velocity = deltaPosition / Time.deltaTime;
        float forwardSpeed = Vector3.Dot(velocity, transform.forward);
        //Debug.Log($"Forward Speed: {forwardSpeed}");
        Animator.SetFloat("ForwardSpeed", forwardSpeed / NavAgent.speed, 0.2f, Time.deltaTime);
        float angle = Vector3.SignedAngle(transform.forward, velocity, Vector3.up);
        float strafeSpeed = -Mathf.Sin(angle * Mathf.Deg2Rad);
        //Debug.Log($"Strafe Speed: {strafeSpeed}");
        Animator.SetFloat("StrafeSpeed", strafeSpeed, 0.2f, Time.deltaTime);
        if(Target?.Health <= 0)
        {
            TargetsInRange.Remove(Target);
            EnemyManager.Instance.RemoveEnemyInRange(this);
        }
        enemyPrePosition = transform.position;
    }

    private void ReactToHit()
    {
        ChangeState(EnemyStates.GettingHit);
    }

    private Vector3 enemyPrePosition;

    public void ChangeState(EnemyStates enemyStates)
    {
        StateMachine.ChangeState(stateDictionary[enemyStates]);
    }

    public bool IsInState(EnemyStates state)
    {
        return StateMachine.CurrentState == stateDictionary[state];
    }

    public MeeleFighter FindTarget()
    {
        foreach (var target in TargetsInRange)
        {
            var vectorToTarget = target.transform.position - transform.position;
            float angle = Vector3.Angle(transform.forward, vectorToTarget);
            if (angle <= FOV / 2)
            {
                return target;

            }

        }
        return null;
    }

    public void AlertNearbyEnemies()
    {
        var coliders = Physics.OverlapBox(transform.position, enemyAlertRange, Quaternion.identity, EnemyManager.Instance.EnemyLayer);
        foreach (var colider in coliders)
        {
            if(colider.gameObject == gameObject)
            {
                continue;
            }
             var enemyNearby = colider.GetComponent<EnemyController>();
            if(enemyNearby != null && enemyNearby.Target == null)
            {
                enemyNearby.Target = Target;
                enemyNearby.ChangeState(EnemyStates.CombatMovement);
            }
        }
    }
}
