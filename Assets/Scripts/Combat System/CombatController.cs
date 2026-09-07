using UnityEngine;

public class CombatController : MonoBehaviour
{
    private MeeleFighter meeleFighter;
    private Animator animator;
    private PlayerController playerController;
    EnemyController targetEnemy;
    public EnemyController TargetEnemy
    {
        get => targetEnemy;
        set
        {
            targetEnemy = value;
            if(targetEnemy == null)
            {
                InCombat = false;
            }
        }
    }
    private new CameraController camera;

    //public bool InCombat { get; set; }
    bool inCombat;
    public bool InCombat 
    { 
        get => inCombat; 
        set
        {
            inCombat = value;
            if(TargetEnemy == null)
            {
                inCombat = false;
            }
            animator.SetBool("InCombat", inCombat);
        }
    }

    private void Awake()
    {
        meeleFighter = GetComponent<MeeleFighter>();
        animator = GetComponentInChildren<Animator>(true);
        animator.applyRootMotion = true;
        camera = Camera.main.GetComponent<CameraController>();
        playerController = GetComponent<PlayerController>();
    }

    private void Start()
    {
        meeleFighter.OnGotHit += (MeeleFighter attacker) =>
        {
            if (InCombat && attacker != TargetEnemy.Fighter) 
            {
                TargetEnemy = attacker.GetComponent<EnemyController>();
            }
        };
    }

    private void Update()
    {
        if (PauseManager.IsPaused || Time.deltaTime <= NumericGuard.MinDenominator)
        {
            return;
        }
        if (Input.GetButtonDown("Attack") && playerController.CanUseWeapon)  /* && meeleFighter.IsTakingHit */
        {
            var enemy = EnemyManager.Instance.GetAttackingEnemy();
            if(enemy != null && enemy.Fighter.IsCounterable && !meeleFighter.InAction)
            {
                StartCoroutine(meeleFighter.PerformCounterAttack(enemy));
            }
            else
            {
                var enemyToAttack = EnemyManager.Instance.GetClosestEnemy(playerController.GetIntentDirection());
                /*
                Vector3? directionToAttack = null;
                if(enemyToAttack != null)
                {
                    directionToAttack = enemyToAttack.transform.position - transform.position;
                }
                */ 
                Debug.Log($"EnemyManager.Instance = {EnemyManager.Instance}");
                Debug.Log($"enemyToAttack = {enemyToAttack}");
                Debug.Log($"meeleFighter = {meeleFighter}");
                if(enemyToAttack)
                    meeleFighter.TryToAttack(enemyToAttack.Fighter);
                else
                    meeleFighter.TryToAttack();
                InCombat = true;
            }
        }
        if(Input.GetButtonDown("LockOn") || JoystickHelper.Instance.GetAxisDown("LockOnTrigger"))
        {
            InCombat = !InCombat;
        }
    }

    private void OnAnimatorMove()
    {
        ApplyAnimatorMove(animator);
    }

    public void ApplyAnimatorMove(Animator sourceAnimator)
    {
        if (sourceAnimator != animator)
        {
            return;
        }

        if (PauseManager.IsPaused || playerController.ShouldBlockRollRootMotion)
        {
            return;
        }

        if (playerController.IsChangingWeapon || meeleFighter.InCounter)
        {
            return;
        }

        if (!meeleFighter.InAction)
        {
            return;
        }

        transform.position += sourceAnimator.deltaPosition;
        transform.rotation *= sourceAnimator.deltaRotation;
    }

    public Vector3 GetTargetingDirection()
    {
        if (InCombat)
        {
            var vectorFromCamera = transform.position - camera.transform.position;
            vectorFromCamera.y = 0;
            return vectorFromCamera.normalized;
        }
        else
        {
            return transform.forward;
        }
    }
}
