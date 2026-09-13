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

    [Header("Attack")]
    [SerializeField, Min(0.1f)] private float attackTimeout = 8f;
    [SerializeField, Range(0f, 0.5f)] private float attackBlendDuration = 0.08f;
    [SerializeField] private bool invulnerableDuringSkill = true;

    [Header("Retreat")]
    [SerializeField, Min(0f)] private float retreatDistance = 2.5f;
    [SerializeField, Min(0.1f)] private float retreatSpeed = 6f;

    [Header("Weapon")]
    [SerializeField, Min(0.1f)] private float weaponAnimationTimeout = 3f;
    [SerializeField, Range(0f, 1f)] private float drawSwordVisibleTime = 0.45f;


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
    private int sheatheSwordStateHash;
    private int drawSwordStateHash;
    private int weaponEmptyStateHash;

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
        comboStateHash = Animator.StringToHash(
            "Override Layer.Punch To Elbow Combo");
        overrideEmptyStateHash = Animator.StringToHash("Override Layer.Empty");
        sheatheSwordStateHash = Animator.StringToHash(
            "Weapon Layer.Sheathe Sword");
        drawSwordStateHash = Animator.StringToHash(
            "Weapon Layer.Draw Sword");
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
        if (!playerController.CanUseWeapon || fighter.InAction)
        {
            return;
        }

        if (playerController.IsRolling || playerController.IsChangingWeapon)
        {
            return;
        }

        if (!characterController.enabled || !characterController.isGrounded)
        {
            return;
        }

        EnemyController target = ResolveTarget();

        if (!CanCastAt(target))
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
        previousApplyRootMotion = animator.applyRootMotion;
        previousInvulnerability = fighter.IsInvulnerable;
        verticalVelocity = -2f;

        animator.applyRootMotion = false;

        if (invulnerableDuringSkill)
        {
            fighter.SetIsInvulnerable(true);
        }

        combatController.TargetEnemy = target;
        combatController.InCombat = true;

        yield return PlaySheatheAnimation();

        if (!IsTargetUsable(target))
        {
            FinishSkill();
            yield break;
        }

        fighter.SetSwordVisible(false);
        animator.Play(weaponEmptyStateHash, weaponLayerIndex, 0f);

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

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;

            if (distance <= distanceFromTarget)
            {
                reachedTarget = true;
                break;
            }

            Vector3 direction = toTarget.normalized;
            FaceDirection(direction);

            float availableDistance = distance - distanceFromTarget;
            float step = Mathf.Min(rushSpeed * Time.deltaTime, availableDistance);

            MoveWithGravity(direction * step);

            rushElapsed += Time.deltaTime;
            yield return null;
        }

        if (!reachedTarget)
        {
            Debug.LogWarning("Rush skill could not reach its target.", this);
            FinishSkill();
            yield break;
        }

        Vector3 lastTargetPosition = target.transform.position;
        FaceTargetPosition(lastTargetPosition);

        animator.CrossFadeInFixedTime(
            comboStateHash,
            attackBlendDuration,
            overrideLayerIndex,
            0f);

        float attackElapsed = 0f;

        while (attackElapsed < attackTimeout)
        {
            MoveWithGravity(Vector3.zero);

            if (target != null)
            {
                lastTargetPosition = target.transform.position;
                FaceTargetPosition(lastTargetPosition);
            }

            AnimatorStateInfo stateInfo =
                animator.GetCurrentAnimatorStateInfo(overrideLayerIndex);

            if (stateInfo.fullPathHash == comboStateHash)
            {
                float normalizedTime = stateInfo.normalizedTime;

                if (normalizedTime >= 0.98f)
                {
                    break;
                }
            }

            attackElapsed += Time.deltaTime;
            yield return null;
        }

        if (appliedHitCount == 0)
        {
            Debug.LogWarning("Rush skill animation ended without applying damage.", this);
        }

        animator.CrossFadeInFixedTime(
            overrideEmptyStateHash,
            0.08f,
            overrideLayerIndex,
            0f);

        animator.SetFloat("ForwardSpeed", -0.6f);
        animator.SetFloat("StrafeSpeed", 0f);

        Vector3 retreatDirection = transform.position - lastTargetPosition;
        retreatDirection.y = 0f;

        if (retreatDirection.sqrMagnitude <= NumericGuard.MinDenominator)
        {
            retreatDirection = -transform.forward;
        }
        else
        {
            retreatDirection.Normalize();
        }

        float remainingRetreatDistance = retreatDistance;
        float retreatElapsed = 0f;
        float retreatTimeout = retreatDistance / retreatSpeed + 0.75f;

        while (remainingRetreatDistance > 0.01f &&
               retreatElapsed < retreatTimeout)
        {
            FaceTargetPosition(lastTargetPosition);

            float step = Mathf.Min(
                retreatSpeed * Time.deltaTime,
                remainingRetreatDistance);

            Vector3 positionBeforeMove = transform.position;
            MoveWithGravity(retreatDirection * step);

            Vector3 moved = transform.position - positionBeforeMove;
            moved.y = 0f;
            remainingRetreatDistance -= moved.magnitude;

            retreatElapsed += Time.deltaTime;
            yield return null;
        }

        animator.SetFloat("ForwardSpeed", 0f);
        animator.SetFloat("StrafeSpeed", 0f);

        yield return PlayDrawAnimation();

        FinishSkill();
    }

    private IEnumerator PlaySheatheAnimation()
    {
        animator.Play(sheatheSwordStateHash, weaponLayerIndex, 1f);
        yield return null;

        float elapsed = 0f;
        bool enteredState = false;

        while (elapsed < weaponAnimationTimeout)
        {
            MoveWithGravity(Vector3.zero);

            AnimatorStateInfo stateInfo =
                animator.GetCurrentAnimatorStateInfo(weaponLayerIndex);

            if (stateInfo.fullPathHash == sheatheSwordStateHash)
            {
                enteredState = true;

                if (stateInfo.normalizedTime <= 0.02f)
                {
                    break;
                }
            }
            else if (enteredState)
            {
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator PlayDrawAnimation()
    {
        animator.Play(drawSwordStateHash, weaponLayerIndex, 0f);
        yield return null;

        bool swordShown = false;
        float elapsed = 0f;

        while (elapsed < weaponAnimationTimeout)
        {
            MoveWithGravity(Vector3.zero);

            AnimatorStateInfo stateInfo =
                animator.GetCurrentAnimatorStateInfo(weaponLayerIndex);

            if (stateInfo.fullPathHash == drawSwordStateHash)
            {
                if (!swordShown &&
                    stateInfo.normalizedTime >= drawSwordVisibleTime)
                {
                    fighter.SetSwordVisible(true);
                    swordShown = true;
                }

                if (stateInfo.normalizedTime >= 0.98f)
                {
                    break;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        fighter.SetSwordVisible(true);
        animator.Play(weaponEmptyStateHash, weaponLayerIndex, 0f);
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
                animator.CrossFadeInFixedTime(
                    overrideEmptyStateHash,
                    0.08f,
                    overrideLayerIndex,
                    0f);
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

    private void ValidateAnimator()
    {
        bool valid = overrideLayerIndex >= 0 && weaponLayerIndex >= 0;

        valid &= animator.HasState(overrideLayerIndex, rushRunStateHash);
        valid &= animator.HasState(overrideLayerIndex, comboStateHash);
        valid &= animator.HasState(overrideLayerIndex, overrideEmptyStateHash);
        valid &= animator.HasState(weaponLayerIndex, sheatheSwordStateHash);
        valid &= animator.HasState(weaponLayerIndex, drawSwordStateHash);
        valid &= animator.HasState(weaponLayerIndex, weaponEmptyStateHash);

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