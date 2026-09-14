using System.Collections;
using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(MeeleFighter))]
[RequireComponent(typeof(CombatController))]
[RequireComponent(typeof(PlayerController))]
public sealed class PlayerRushSkill : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode skillKey = KeyCode.V;

    [Header("Cast Range")]
    [SerializeField, Min(0f)] private float minCastDistance = 2f;
    [SerializeField, Min(0.1f)] private float maxCastDistance = 12f;
    [SerializeField] private bool requireClearPath = true;
    [SerializeField] private LayerMask obstructionMask;

    [Header("Rush")]
    [SerializeField, Min(0.1f)] private float rushSpeed = 14f;
    [SerializeField, Min(0.1f)] private float rushTimeout = 2f;
    [SerializeField, Min(0.1f)] private float distanceFromTarget = 1.1f;
    [SerializeField, Min(1f)] private float rotationSpeed = 900f;
    [SerializeField, Min(0.001f)] private float arrivalTolerance = 0.03f;

    [Header("Attack")]
    [SerializeField, Min(0.1f)] private float attackTimeout = 8f;
    [SerializeField, Range(0f, 0.5f)] private float attackBlendDuration = 0.08f;
    [SerializeField] private bool invulnerableDuringSkill = true;
    [SerializeField, Range(0f, 0.5f)] private float exitBlendDuration = 0.08f;


    private readonly HashSet<int> processedHitIndexes = new();
    private EnemyController activeTarget;
    private int appliedHitCount;

    private const string OverrideLayerName = "Override Layer";
    private const string WeaponLayerName = "Weapon Layer";

    private CharacterController characterController;
    private MeeleFighter fighter;
    private CombatController combatController;
    private PlayerController playerController;
    private Animator animator;

    private int overrideLayerIndex;
    private int weaponLayerIndex;
    private int rushRunStateHash;
    private int comboStateHash;
    private int overrideEmptyStateHash;
    private int weaponEmptyStateHash;
    private bool previousSuppressAnimatorMove;

    private Coroutine skillRoutine;
    private bool isRunning;
    private bool previousApplyRootMotion;
    private bool previousInvulnerability;
    private float verticalVelocity;

    public bool IsRunning => isRunning;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        fighter = GetComponent<MeeleFighter>();
        combatController = GetComponent<CombatController>();
        playerController = GetComponent<PlayerController>();
        animator = GetComponentInChildren<Animator>(true);

        overrideLayerIndex = animator.GetLayerIndex(OverrideLayerName);
        weaponLayerIndex = animator.GetLayerIndex(WeaponLayerName);

        rushRunStateHash = Animator.StringToHash("Override Layer.Rush Run");
        comboStateHash = Animator.StringToHash("Override Layer.Punch To Elbow Combo");
        overrideEmptyStateHash = Animator.StringToHash("Override Layer.Empty");
        weaponEmptyStateHash = Animator.StringToHash("Weapon Layer.Empty");

        ValidateAnimator();
    }

    private void Update()
    {
        if (PauseManager.IsPaused || Time.deltaTime <= NumericGuard.MinDenominator)
        {
            return;
        }

        if (!isRunning && Input.GetKeyDown(skillKey))
        {
            TryStartSkill();
        }
    }

    private void TryStartSkill()
    {
        if (!TryGetAvailableTarget(out EnemyController target))
        {
            return;
        }

        skillRoutine = StartCoroutine(PerformSkill(target));
    }

    private EnemyController ResolveTarget()
    {
        EnemyController target = combatController.TargetEnemy;

        if (IsTargetUsable(target))
        {
            return target;
        }

        if (EnemyManager.Instance == null)
        {
            return null;
        }

        return EnemyManager.Instance.GetClosestEnemy(
            playerController.GetIntentDirection());
    }

    private bool CanCastAt(EnemyController target)
    {
        if (!IsTargetUsable(target))
        {
            return false;
        }

        Vector3 toTarget = target.transform.position - transform.position;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;

        if (distance < minCastDistance || distance > maxCastDistance)
        {
            return false;
        }

        if (!requireClearPath || obstructionMask.value == 0)
        {
            return true;
        }

        Vector3 start = transform.position + Vector3.up;
        Vector3 end = target.transform.position + Vector3.up;

        return !Physics.Linecast(
            start,
            end,
            obstructionMask,
            QueryTriggerInteraction.Ignore);
    }

    public void AnimationEvent_RushSkillHit(AnimationEvent animationEvent)
    {
        if (!isRunning || !IsTargetUsable(activeTarget))
        {
            return;
        }

        int hitIndex = animationEvent.intParameter;
        float hitDamage = animationEvent.floatParameter;

        if (!processedHitIndexes.Add(hitIndex))
        {
            Debug.LogWarning(
                $"Rush skill hit index {hitIndex} was triggered more than once.",
                this);
            return;
        }

        if (hitDamage <= 0f)
        {
            Debug.LogWarning(
                $"Rush skill hit {hitIndex} has no damage value.",
                this);
            return;
        }

        if (activeTarget.Fighter.TryReceiveDamage(hitDamage, fighter))
        {
            appliedHitCount++;
        }
    }

    private IEnumerator PerformSkill(EnemyController target)
    {
        if (!fighter.TryBeginExternalAction(target.Fighter))
        {
            skillRoutine = null;
            yield break;
        }

        isRunning = true;
        activeTarget = target;
        appliedHitCount = 0;
        processedHitIndexes.Clear();
        verticalVelocity = -2f;

        previousApplyRootMotion = animator.applyRootMotion;
        previousInvulnerability = fighter.IsInvulnerable;
        previousSuppressAnimatorMove = combatController.SuppressAnimatorMove;

        // 技能运动完全交给 CharacterController。
        combatController.SuppressAnimatorMove = true;
        animator.applyRootMotion = false;

        if (invulnerableDuringSkill)
        {
            fighter.SetIsInvulnerable(true);
        }

        combatController.TargetEnemy = target;
        combatController.InCombat = true;

        // 不再等待收剑动画。
        // 按下 V 的同一帧隐藏剑，并清空 Weapon Layer。
        fighter.SetSwordVisible(false);
        animator.Play(
            weaponEmptyStateHash,
            weaponLayerIndex,
            0f);

        if (!IsTargetUsable(target))
        {
            FinishSkill();
            yield break;
        }

        bool reachedTarget = false;
        float rushElapsed = 0f;

        animator.CrossFadeInFixedTime(
            rushRunStateHash,
            0.08f,
            overrideLayerIndex,
            0f);

        while (rushElapsed < rushTimeout)
        {
            if (!IsTargetUsable(target))
            {
                break;
            }

            Vector3 attackPosition = CalculateAttackPosition(target);
            Vector3 toAttackPosition = attackPosition - transform.position;
            toAttackPosition.y = 0f;

            if (toAttackPosition.magnitude <= arrivalTolerance)
            {
                reachedTarget = true;
                break;
            }

            // 冲刺阶段可以持续跟踪目标。
            FaceTargetPosition(target.transform.position);

            float step = Mathf.Min(
                rushSpeed * Time.deltaTime,
                toAttackPosition.magnitude);

            MoveWithGravity(toAttackPosition.normalized * step);

            rushElapsed += Time.deltaTime;
            yield return null;
        }

        if (!reachedTarget || !IsTargetUsable(target))
        {
            Debug.LogWarning(
                "Rush skill could not reach its attack position.",
                this);

            FinishSkill();
            yield break;
        }

        // 连击开始前只确定一次最终朝向。
        FaceTargetImmediately(target.transform.position);

        animator.CrossFadeInFixedTime(
            comboStateHash,
            attackBlendDuration,
            overrideLayerIndex,
            0f);

        yield return WaitForComboAnimation();

        if (appliedHitCount == 0)
        {
            Debug.LogWarning(
                "Rush skill animation ended without applying damage.",
                this);
        }

        // 技能期间仍然屏蔽 Root Motion，
        // 等待完全混合到 Empty 后再归还控制。
        animator.CrossFadeInFixedTime(
            overrideEmptyStateHash,
            exitBlendDuration,
            overrideLayerIndex,
            0f);

        float exitElapsed = 0f;

        while (exitElapsed < exitBlendDuration)
        {
            MoveWithGravity(Vector3.zero);
            exitElapsed += Time.deltaTime;
            yield return null;
        }

        FinishSkill();
    }

    private Vector3 CalculateAttackPosition(EnemyController target)
    {
        Vector3 targetPosition = target.transform.position;

        // 保证攻击点位于玩家接近敌人的这一侧。
        Vector3 awayFromTarget = transform.position - targetPosition;
        awayFromTarget.y = 0f;

        if (awayFromTarget.sqrMagnitude <= NumericGuard.MinDenominator)
        {
            awayFromTarget = -transform.forward;
        }
        else
        {
            awayFromTarget.Normalize();
        }

        return targetPosition + awayFromTarget * distanceFromTarget;
    }

    private IEnumerator WaitForComboAnimation()
    {
        float elapsed = 0f;
        bool enteredComboState = false;

        while (elapsed < attackTimeout)
        {
            // 只维持重力，不改变水平位置和朝向。
            MoveWithGravity(Vector3.zero);

            AnimatorStateInfo stateInfo =
                animator.GetCurrentAnimatorStateInfo(overrideLayerIndex);

            if (stateInfo.fullPathHash == comboStateHash)
            {
                enteredComboState = true;

                if (stateInfo.normalizedTime >= 0.98f)
                {
                    yield break;
                }
            }
            else if (enteredComboState &&
                     !animator.IsInTransition(overrideLayerIndex))
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning(
            "Rush skill combo animation timed out.",
            this);
    }

    private void FaceTargetImmediately(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= NumericGuard.MinDenominator)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(
            direction.normalized,
            Vector3.up);
    }

    private void MoveWithGravity(Vector3 planarDisplacement)
    {
        float deltaTime = Time.deltaTime;

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += Physics.gravity.y * deltaTime;
        }

        planarDisplacement.y = verticalVelocity * deltaTime;
        characterController.Move(planarDisplacement);
    }

    private void FaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude <= NumericGuard.MinDenominator)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(
            direction,
            Vector3.up);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }

    private void FaceTargetPosition(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;
        FaceDirection(direction);
    }

    private static bool IsTargetUsable(EnemyController target)
    {
        return target != null &&
               !target.IsDead &&
               target.Fighter != null &&
               target.Fighter.Health > 0f;
    }

    private void FinishSkill()
    {
        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.SetFloat("ForwardSpeed", 0f);
            animator.SetFloat("StrafeSpeed", 0f);

            if (overrideLayerIndex >= 0)
            {
                AnimatorStateInfo stateInfo =
                    animator.GetCurrentAnimatorStateInfo(overrideLayerIndex);

                // 异常中止时立即回到 Empty，防止恢复 Root Motion
                // 后继续读取连击动画的位移。
                if (stateInfo.fullPathHash != overrideEmptyStateHash)
                {
                    animator.Play(
                        overrideEmptyStateHash,
                        overrideLayerIndex,
                        0f);
                }
            }

            if (weaponLayerIndex >= 0)
            {
                animator.Play(
                    weaponEmptyStateHash,
                    weaponLayerIndex,
                    0f);
            }

            animator.applyRootMotion = previousApplyRootMotion;
        }

        if (combatController != null)
        {
            combatController.SuppressAnimatorMove =
                previousSuppressAnimatorMove;
        }

        // 回到普通持剑状态，但不额外播放拔剑动画。
        fighter.SetSwordVisible(true);
        fighter.SetIsInvulnerable(previousInvulnerability);
        fighter.EndExternalAction();

        verticalVelocity = 0f;
        isRunning = false;
        skillRoutine = null;
        activeTarget = null;
        appliedHitCount = 0;
        processedHitIndexes.Clear();
    }

    public bool TryGetAvailableTarget(out EnemyController target)
    {
        target = null;

        if (!isActiveAndEnabled || PauseManager.IsPaused || isRunning)
        {
            return false;
        }

        if (Time.deltaTime <= NumericGuard.MinDenominator)
        {
            return false;
        }

        if (!playerController.CanUseWeapon || fighter.InAction)
        {
            return false;
        }

        if (playerController.IsRolling || playerController.IsChangingWeapon)
        {
            return false;
        }

        if (!characterController.enabled || !characterController.isGrounded)
        {
            return false;
        }

        EnemyController resolvedTarget = ResolveTarget();

        if (!CanCastAt(resolvedTarget))
        {
            return false;
        }

        target = resolvedTarget;
        return true;
    }

    private void ValidateAnimator()
    {
        bool valid =
            overrideLayerIndex >= 0 &&
            weaponLayerIndex >= 0;

        valid &= animator.HasState(
            overrideLayerIndex,
            rushRunStateHash);

        valid &= animator.HasState(
            overrideLayerIndex,
            comboStateHash);

        valid &= animator.HasState(
            overrideLayerIndex,
            overrideEmptyStateHash);

        valid &= animator.HasState(
            weaponLayerIndex,
            weaponEmptyStateHash);

        if (!valid)
        {
            Debug.LogError(
                "PlayerRushSkill Animator states are incomplete.",
                this);

            enabled = false;
        }
    }

    private void OnDisable()
    {
        if (skillRoutine != null)
        {
            StopCoroutine(skillRoutine);
        }

        if (isRunning)
        {
            FinishSkill();
        }
    }
}