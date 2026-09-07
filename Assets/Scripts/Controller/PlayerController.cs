using UnityEngine;


public class PlayerController : Singleton<PlayerController>
{
    private enum SprintState
    {
        Armed,
        Sheathing,
        Sheathed,
        Drawing
    }

    [SerializeField] private float movementSpeed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float dampTime;


    [Header("Sprint")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private float sprintSpeedMultiplier = 1.6f;
    [SerializeField] private float acceleration = 6f;
    [SerializeField] private float deceleration = 10f;
    // 防止 Animator 配置损坏、动画速度为 0 或状态无法进入时永久锁死玩家
    [SerializeField, Min(0.5f)] private float weaponActionTimeout = 3f;
    private float weaponActionElapsed;
    private const string ActionLayerName = "Weapon Layer";
    private int actionLayerIndex = 1;

    private SprintState sprintState = SprintState.Armed;
    private float currentPlanarSpeed;
    private Vector3 lastMoveDirection;
    private int sheatheSwordStateHash;
    private int drawSwordStateHash;
    private int emptyStateHash;

    public bool CanUseWeapon => sprintState == SprintState.Armed && !isRolling;

    [Header("Roll")]
    [SerializeField] private KeyCode rollKey = KeyCode.LeftControl;
    [SerializeField, Range(0f, 0.25f)] private float rollTransitionDuration = 0.08f;
    [SerializeField, Min(0f)] private float rollDistance = 3f;
    [SerializeField, Min(0.1f)] private float rollDuration = 1.6f;
    [SerializeField] private AnimationClip rollAnimation;
    [SerializeField, Range(0.01f, 0.5f)] private float rollInputDeadZone = 0.15f;

    private const string OverrideLayerName = "Override Layer";
    private static readonly int RollPlaybackSpeedHash = Animator.StringToHash("RollPlaybackSpeed");

    private int overrideLayerIndex;
    private int rollStateHash;
    private int overrideEmptyStateHash;
    private bool isRolling;
    private float rollElapsed;
    private Vector3 rollDirection;
    private float activeRollDuration;
    private float activeRollDistance;
    private float activeRollBlendDuration;
    private float rollExitElapsed;
    private bool isRollExiting;

    public bool IsRolling => isRolling;

    [Header("Ground Check Checking")]
    [SerializeField] private float groundCheckRadius;
    [SerializeField] private Vector3 groundCheckOffset;
    [SerializeField] private LayerMask groundLayer;

    public Vector3 InputDirection { get; private set; }

    public bool IsChangingWeapon => sprintState == SprintState.Sheathing || sprintState == SprintState.Drawing;

    private CameraController cameraController;
    private CombatController combatController;
    private Animator animator;
    private CharacterController characterController;
    private MeeleFighter meeleFighter;
    private bool isGrounded;
    private float speedY;
    private Quaternion targetRotation;
    public bool ShouldBlockRollRootMotion
    {
        get
        {
            if (isRolling)
            {
                return true;
            }

            if (animator == null || overrideLayerIndex < 0)
            {
                return false;
            }

            var currentState = animator.GetCurrentAnimatorStateInfo(overrideLayerIndex);

            if (currentState.fullPathHash == rollStateHash)
            {
                return true;
            }

            if (!animator.IsInTransition(overrideLayerIndex))
            {
                return false;
            }

            var nextState = animator.GetNextAnimatorStateInfo(overrideLayerIndex);
            return nextState.fullPathHash == rollStateHash;
        }
    }


    private new void Awake()
    {
        cameraController = Camera.main.GetComponent<CameraController>();
        animator = GetComponentInChildren<Animator>(true);
        characterController = GetComponent<CharacterController>();
        meeleFighter = GetComponent<MeeleFighter>();
        combatController = GetComponent<CombatController>();
        //绑定Animation
        sheatheSwordStateHash = Animator.StringToHash("Weapon Layer.Sheathe Sword");
        drawSwordStateHash = Animator.StringToHash("Weapon Layer.Draw Sword");
        emptyStateHash = Animator.StringToHash("Weapon Layer.Empty");
        actionLayerIndex = animator.GetLayerIndex(ActionLayerName);
        overrideLayerIndex = animator.GetLayerIndex(OverrideLayerName);
        rollStateHash = Animator.StringToHash("Override Layer.Standing Dive Forward");
        overrideEmptyStateHash = Animator.StringToHash("Override Layer.Empty");
    }

    private void Update()
    {
        if (PauseManager.IsPaused || Time.deltaTime <= NumericGuard.MinDenominator)
        {
            return;
        }

        if (meeleFighter.InAction || meeleFighter.Health <= 0)
        {
            if (isRolling)
            {
                EndRoll(false);
            }

            currentPlanarSpeed = 0f;
            targetRotation = transform.rotation;
            animator.SetFloat("ForwardSpeed", 0f);
            animator.SetFloat("StrafeSpeed", 0f);
            return;
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 rawInput = new Vector3(h, 0f, v);
        float inputAmount = Mathf.Clamp01(rawInput.magnitude);
        Vector3 movement = inputAmount > 0.01f ? rawInput.normalized : Vector3.zero;
        Vector3 moveDirection = cameraController.PlanarRotation * movement;

        InputDirection = moveDirection;

        if (inputAmount > 0.01f)
        {
            lastMoveDirection = moveDirection;
        }

        GroundCheck();

        if (!isRolling && Input.GetKeyDown(rollKey))
        {
            TryStartRoll();
        }

        if (isRolling)
        {
            UpdateRoll();
            return;
        }

        bool wantsSprint = inputAmount > 0.01f && !combatController.InCombat &&
                           Input.GetKey(sprintKey);

        UpdateSprintState(wantsSprint);
        UpdateGravity();

        Vector3 velocity;

        if (combatController.InCombat)
        {
            velocity = UpdateCombatMovement(moveDirection, inputAmount);
        }
        else
        {
            velocity = UpdateFreeMovement(moveDirection, inputAmount);
        }

        velocity.y = speedY;
        characterController.Move(velocity * Time.deltaTime);
    }

    private void UpdateGravity()
    {
        GroundCheck();

        if (!isGrounded)
        {
            speedY += Physics.gravity.y * Time.deltaTime;
        }
        else
        {
            speedY = -0.5f;
        }
    }

    private Vector3 UpdateFreeMovement(Vector3 moveDirection, float inputAmount)
    {
        float sprintMaximumSpeed = movementSpeed * sprintSpeedMultiplier;

        float allowedMaximumSpeed = sprintState == SprintState.Sheathed
            ? sprintMaximumSpeed
            : movementSpeed;

        float targetSpeed = allowedMaximumSpeed * inputAmount;
        float speedChangeRate = targetSpeed > currentPlanarSpeed ? acceleration : deceleration;
        currentPlanarSpeed = Mathf.MoveTowards(currentPlanarSpeed, targetSpeed, speedChangeRate * Time.deltaTime);

        Vector3 travelDirection = inputAmount > 0.01f ? moveDirection : lastMoveDirection;

        if (inputAmount > 0.01f)
        {
            targetRotation = Quaternion.LookRotation(moveDirection);
        }

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);

        float normalizedSpeed = NumericGuard.SafeDivide(currentPlanarSpeed, sprintMaximumSpeed);
        animator.SetFloat("ForwardSpeed", Mathf.Clamp01(normalizedSpeed), dampTime, Time.deltaTime);

        return travelDirection * currentPlanarSpeed;
    }

    private Vector3 UpdateCombatMovement(Vector3 moveDirection, float inputAmount)
    {
        currentPlanarSpeed = movementSpeed * 0.25f * inputAmount;
        Vector3 velocity = moveDirection * currentPlanarSpeed;
        Vector3 targetVector = combatController.TargetEnemy.transform.position - transform.position;

        targetVector.y = 0f;

        if (inputAmount > 0.01f && targetVector.sqrMagnitude > NumericGuard.MinDenominator)
        {
            targetRotation = Quaternion.LookRotation(targetVector, Vector3.up);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
        }

        float forwardSpeed = Vector3.Dot(velocity, transform.forward);
        float normalizedForward = NumericGuard.SafeDivide(forwardSpeed, movementSpeed);
        float angle = Vector3.SignedAngle(transform.forward, velocity, Vector3.up);
        float strafeSpeed = velocity.sqrMagnitude > NumericGuard.MinDenominator
            ? -Mathf.Sin(angle * Mathf.Deg2Rad)
            : 0f;

        animator.SetFloat("ForwardSpeed", normalizedForward, 0.2f, Time.deltaTime);
        animator.SetFloat("StrafeSpeed", strafeSpeed, 0.2f, Time.deltaTime);

        return velocity;
    }

    private void UpdateSprintState(bool wantsSprint)
    {
        switch (sprintState)
        {
            case SprintState.Armed:
                if (wantsSprint)
                {
                    BeginSheathing();
                }
                break;

            case SprintState.Sheathing:
                weaponActionElapsed += Time.deltaTime;

                bool sheatheCompleted = IsActionStateComplete(sheatheSwordStateHash);
                bool sheatheTimedOut = HasWeaponActionTimedOut(sheatheSwordStateHash, "Sheathe Sword");

                if (!sheatheCompleted && !sheatheTimedOut)
                {
                    break;
                }

                // 收剑动画完成后才隐藏 Sword
                meeleFighter.SetSwordVisible(false);

                if (wantsSprint)
                {
                    sprintState = SprintState.Sheathed;
                    weaponActionElapsed = 0f;
                    animator.CrossFade(emptyStateHash, 0.1f, actionLayerIndex);
                }
                else
                {
                    BeginDrawing();
                }
                break;

            case SprintState.Sheathed:
                if (!wantsSprint)
                {
                    BeginDrawing();
                }
                break;

            case SprintState.Drawing:
                weaponActionElapsed += Time.deltaTime;

                bool drawCompleted = IsActionStateComplete(drawSwordStateHash);
                bool drawTimedOut = HasWeaponActionTimedOut(drawSwordStateHash, "Draw Sword");

                if (!drawCompleted && !drawTimedOut)
                {
                    break;
                }

                meeleFighter.SetSwordVisible(true);

                sprintState = SprintState.Armed;
                weaponActionElapsed = 0f;
                animator.CrossFade(emptyStateHash, 0.1f, actionLayerIndex);
                break;
        }
    }

    // 由 Erika Archer@Unarmed Equip Over Shoulder 的 Animation Event 调用
    public void AnimationEvent_WeaponHandoff()
    {
        if (meeleFighter == null)
        {
            return;
        }

        switch (sprintState)
        {
            case SprintState.Sheathing:
                // 倒放至该关键帧：剑已经被放回，因此隐藏
                meeleFighter.SetSwordVisible(false);
                break;

            case SprintState.Drawing:
                // 正放至该关键帧：手已经握住剑，因此显示
                meeleFighter.SetSwordVisible(true);
                break;
        }
    }

    private void BeginSheathing()
    {
        if (!CanPlayActionState(sheatheSwordStateHash, "Sheathe Sword"))
        {
            sprintState = SprintState.Armed;
            return;
        }

        weaponActionElapsed = 0f;
        sprintState = SprintState.Sheathing;

        Debug.Log(
            $"Sprint sheathe: layer={actionLayerIndex}, " +
            $"weight={animator.GetLayerWeight(actionLayerIndex):F2}, " +
            $"hasState={animator.HasState(actionLayerIndex, sheatheSwordStateHash)}",
            this);

        // Sheathe Sword 的 Speed 为 -1，因此必须从动画末尾开始
        animator.Play(sheatheSwordStateHash, actionLayerIndex, 0f);
    }

    private void BeginDrawing()
    {
        if (!CanPlayActionState(drawSwordStateHash, "Draw Sword"))
        {
            // 动画无法播放时恢复到安全、可用的持剑状态
            meeleFighter.SetSwordVisible(true);
            sprintState = SprintState.Armed;
            weaponActionElapsed = 0f;
            return;
        }

        weaponActionElapsed = 0f;
        sprintState = SprintState.Drawing;

        animator.Play(drawSwordStateHash, actionLayerIndex, 0f);
    }

    private void TryStartRoll()
    {
        if (isRolling || !isGrounded || IsChangingWeapon)
        {
            return;
        }

        if (meeleFighter.InAction || meeleFighter.Health <= 0f)
        {
            return;
        }

        if (!characterController.enabled || !animator.isActiveAndEnabled)
        {
            return;
        }

        if (overrideLayerIndex < 0 || rollAnimation == null)
        {
            Debug.LogError("Roll requires an Override Layer and a roll animation clip.", this);
            return;
        }

        bool hasRollState = animator.HasState(overrideLayerIndex, rollStateHash);
        bool hasEmptyState = animator.HasState(overrideLayerIndex, overrideEmptyStateHash);

        if (!hasRollState || !hasEmptyState)
        {
            Debug.LogError("Override Layer requires Standing Dive Forward and Empty states.", this);
            return;
        }

        // 在清空普通移动速度前，记录进入翻滚时的速度。
        float sampledSpeed = currentPlanarSpeed;

        if (sampledSpeed <= 0.01f)
        {
            sampledSpeed = movementSpeed;
        }

        sampledSpeed = Mathf.Max(sampledSpeed, 0.01f);
        activeRollDistance = Mathf.Max(rollDistance, 0f);

        // 距离大于零时，由采样速度决定推进时长。
        if (activeRollDistance > 0f)
        {
            activeRollDuration = activeRollDistance / sampledSpeed;
        }
        else
        {
            // 保留距离为零时，只播放原地翻滚的能力。
            activeRollDuration = Mathf.Max(rollDuration, 0.1f);
        }

        activeRollBlendDuration = Mathf.Clamp(rollTransitionDuration, 0f, activeRollDuration);

        // 每次开始时固定参数，避免一次翻滚中途改变距离或时长。
        activeRollDuration = Mathf.Max(rollDuration, 0.1f);
        activeRollDistance = Mathf.Max(rollDistance, 0f);
        activeRollBlendDuration = Mathf.Clamp(rollTransitionDuration, 0f, activeRollDuration);

        rollDirection = ReadRollDirection();
        targetRotation = Quaternion.LookRotation(rollDirection, Vector3.up);
        transform.rotation = targetRotation;

        InputDirection = rollDirection;
        lastMoveDirection = rollDirection;
        currentPlanarSpeed = 0f;

        rollElapsed = 0f;
        rollExitElapsed = 0f;
        isRollExiting = false;
        isRolling = true;

        meeleFighter.SetIsInvulnerable(true);

        float playbackSpeed = rollAnimation.length / activeRollDuration;

        animator.SetFloat(RollPlaybackSpeedHash, playbackSpeed);
        animator.SetFloat("ForwardSpeed", 0f);
        animator.SetFloat("StrafeSpeed", 0f);

        animator.CrossFadeInFixedTime(
            rollStateHash,
            activeRollBlendDuration,
            overrideLayerIndex,
            0f);
    }

    private void UpdateRoll()
    {
        UpdateGravity();

        // 正常退出期间不再水平推进，但仍保留翻滚锁和重力。
        if (isRollExiting)
        {
            characterController.Move(Vector3.up * speedY * Time.deltaTime);
            rollExitElapsed += Time.deltaTime;

            if (rollExitElapsed >= activeRollBlendDuration)
            {
                EndRoll(false);
            }

            return;
        }

        float previousProgress = Mathf.Clamp01(rollElapsed / activeRollDuration);
        rollElapsed = Mathf.Min(rollElapsed + Time.deltaTime, activeRollDuration);
        float currentProgress = Mathf.Clamp01(rollElapsed / activeRollDuration);

        float previousDistance = Mathf.SmoothStep(0f, activeRollDistance, previousProgress);
        float currentDistance = Mathf.SmoothStep(0f, activeRollDistance, currentProgress);

        Vector3 displacement = rollDirection * (currentDistance - previousDistance);
        displacement.y = speedY * Time.deltaTime;

        // 使用进入翻滚时固定的方向和朝向。
        transform.rotation = targetRotation;
        characterController.Move(displacement);

        if (rollElapsed < activeRollDuration)
        {
            return;
        }

        isRollExiting = true;
        rollExitElapsed = 0f;

        animator.CrossFadeInFixedTime(
            overrideEmptyStateHash,
            activeRollBlendDuration,
            overrideLayerIndex,
            0f);

        if (activeRollBlendDuration <= 0f)
        {
            EndRoll(false);
        }
    }

    private void EndRoll(bool returnToEmptyState)
    {
        isRolling = false;
        meeleFighter.SetIsInvulnerable(false);
        isRollExiting = false;
        rollElapsed = 0f;
        rollExitElapsed = 0f;
        currentPlanarSpeed = 0f;
        targetRotation = transform.rotation;

        if (!returnToEmptyState || overrideLayerIndex < 0)
        {
            return;
        }

        if (!animator.isActiveAndEnabled)
        {
            return;
        }

        animator.CrossFadeInFixedTime(
            overrideEmptyStateHash,
            activeRollBlendDuration,
            overrideLayerIndex,
            0f);
    }

    private Vector3 ReadRollDirection()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(horizontal, 0f, vertical);

        Vector3 direction = transform.forward;
        float deadZoneSquared = rollInputDeadZone * rollInputDeadZone;

        if (input.sqrMagnitude > deadZoneSquared)
        {
            direction = cameraController.PlanarRotation * input.normalized;
        }

        direction = Vector3.ProjectOnPlane(direction, Vector3.up);

        if (direction.sqrMagnitude <= NumericGuard.MinDenominator)
        {
            direction = Vector3.forward;
        }

        return direction.normalized;
    }

    private bool CanPlayActionState(int stateHash, string stateName)
    {
        if (actionLayerIndex < 0)
        {
            Debug.LogError($"Animator layer '{ActionLayerName}' was not found.", this);
            return false;
        }

        if (!animator.HasState(actionLayerIndex, stateHash))
        {
            Debug.LogError($"Animator state '{ActionLayerName}.{stateName}' was not found.", this);
            return false;
        }

        return true;
    }

    private bool IsActionStateComplete(int stateHash)
    {
        if (actionLayerIndex < 0)
        {
            return false;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(actionLayerIndex);

        if (stateInfo.fullPathHash != stateHash)
        {
            return false;
        }

        float normalizedTime = stateInfo.normalizedTime;

        if (float.IsNaN(normalizedTime) || float.IsInfinity(normalizedTime))
        {
            return false;
        }

        return normalizedTime >= 0.98f;
    }

    // [新增]
    private bool HasWeaponActionTimedOut(int expectedStateHash, string stateName)
    {
        float timeout = Mathf.Max(weaponActionTimeout, 0.5f);

        if (weaponActionElapsed < timeout)
        {
            return false;
        }

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(actionLayerIndex);

        Debug.LogError(
            $"Weapon action '{stateName}' timed out. " +
            $"expectedHash={expectedStateHash}, " +
            $"currentHash={currentState.fullPathHash}, " +
            $"normalizedTime={currentState.normalizedTime:F3}, " +
            $"stateSpeed={currentState.speed:F2}, " +
            $"animatorSpeed={animator.speed:F2}",
            this);

        return true;
    }

    private void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(transform.TransformPoint(groundCheckOffset), groundCheckRadius, groundLayer);
    }

    public Vector3 GetIntentDirection()
    {
        return InputDirection != Vector3.zero ? InputDirection : transform.forward;
    }
    /*
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.DrawSphere(transform.TransformPoint(groundCheckOffset), groundCheckRadius);
    }
    */

    // [新增]
    private void OnDisable()
    {
        if (meeleFighter != null)
        {
            meeleFighter.SetSwordVisible(true);
            meeleFighter.SetIsInvulnerable(false);
        }

        sprintState = SprintState.Armed;
        currentPlanarSpeed = 0f;
        weaponActionElapsed = 0f;
        isRolling = false;
        rollElapsed = 0f;
        if (isRolling)
        {
            EndRoll(true);
        }
    }
}
