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

    private static readonly int ForwardSpeedHash = Animator.StringToHash("ForwardSpeed");
    private static readonly int StrafeSpeedHash = Animator.StringToHash("StrafeSpeed");
    private Vector3 enemyPrePosition;

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
        enemyPrePosition = transform.position;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        // 同时防御 PauseManager 暂停和其他代码直接把 timeScale 设为 0。
        if (PauseManager.IsPaused || deltaTime <= NumericGuard.MinDenominator)
        {
            // 持续同步位置，避免恢复游戏的第一帧产生速度尖峰。
            enemyPrePosition = transform.position;
            return;
        }

        StateMachine.Execute();
        UpdateLocomotionAnimation(deltaTime);

        if (Target?.Health <= 0)
        {
            TargetsInRange.Remove(Target);
            EnemyManager.Instance.RemoveEnemyInRange(this);
        }

        enemyPrePosition = transform.position;
    }

    private void UpdateLocomotionAnimation(float deltaTime)
    {
        Vector3 deltaPosition = Animator.applyRootMotion
            ? Vector3.zero
            : transform.position - enemyPrePosition;

        Vector3 velocity = NumericGuard.IsFinite(deltaPosition)
            ? deltaPosition / deltaTime
            : Vector3.zero;

        float forwardSpeed = Vector3.Dot(velocity, transform.forward);
        float normalizedForward = NumericGuard.SafeDivide(forwardSpeed, NavAgent.speed);
        normalizedForward = Mathf.Clamp(normalizedForward, -1f, 1f);

        float strafeSpeed = 0f;
        if (velocity.sqrMagnitude > NumericGuard.MinDenominator *
            NumericGuard.MinDenominator)
        {
            float angle = Vector3.SignedAngle(transform.forward, velocity, Vector3.up);
            strafeSpeed = -Mathf.Sin(angle * Mathf.Deg2Rad);
        }

        Animator.SetFloatSafe(ForwardSpeedHash, normalizedForward, 0.2f, deltaTime);
        Animator.SetFloatSafe(StrafeSpeedHash, strafeSpeed, 0.2f, deltaTime);
    }

    private void ReactToHit()
    {
        ChangeState(EnemyStates.GettingHit);
    }

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
