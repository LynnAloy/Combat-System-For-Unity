using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PlayerController : Singleton<PlayerController>
{
    [SerializeField] private float movementSpeed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float dampTime;
    [Header("Ground Check Checking")]
    [SerializeField] private float groundCheckRadius;
    [SerializeField] private Vector3 groundCheckOffset;
    [SerializeField] private LayerMask groundLayer;

    public Vector3 InputDirection { get; private set; }

    private CameraController cameraController;
    private CombatController combatController;
    private Animator animator;
    private CharacterController characterController;
    private MeeleFighter meeleFighter;
    private bool isGrounded;
    private float speedY;

    private Quaternion targetRotation;



    private new void Awake()
    {
        cameraController = Camera.main.GetComponent<CameraController>();
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        meeleFighter = GetComponent<MeeleFighter>();
        combatController = GetComponent<CombatController>();    
    }

    void Update()
    {
        if (meeleFighter.InAction || meeleFighter.Health <= 0)
        {
            targetRotation = transform.rotation;
            animator.SetFloat("ForwardSpeed", 0);
            return;
        }
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        var isMoving = Mathf.Clamp(Mathf.Abs(h) + Mathf.Abs(v), 0, 1);

        var movement = new Vector3(h, 0, v).normalized;
        var moveDirection = cameraController.PlanarRotation * movement;
        InputDirection = moveDirection;

        GroundCheck();
        //Debug.Log(isGrounded);

        if(!isGrounded)
        {
            speedY += Physics.gravity.y * Time.deltaTime;
        }
        else
        {
            speedY = -0.5f;
        }

        
        var velocity = movementSpeed * moveDirection;
        

        if (combatController.InCombat)
        {
            velocity /= 4f;
            var targetVector = combatController.TargetEnemy.transform.position - transform.position;
            targetVector.y = 0;
            if (isMoving > 0)
            {
                targetRotation = Quaternion.LookRotation(-targetVector);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            float forwardSpeed = Vector3.Dot(velocity, transform.forward);
            animator.SetFloat("ForwardSpeed", forwardSpeed / movementSpeed, 0.2f, Time.deltaTime);
            float angle = Vector3.SignedAngle(transform.forward, velocity, Vector3.up);
            float strafeSpeed = -Mathf.Sin(angle * Mathf.Deg2Rad);
            animator.SetFloat("StrafeSpeed", strafeSpeed, 0.2f, Time.deltaTime);
        }
        else
        {
            if (isMoving > 0)
            {
                targetRotation = Quaternion.LookRotation(moveDirection);
            }
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            animator.SetFloat("ForwardSpeed", isMoving, dampTime, Time.deltaTime);
        }
        velocity.y = speedY;
        characterController.Move(Time.deltaTime * velocity);
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
}
