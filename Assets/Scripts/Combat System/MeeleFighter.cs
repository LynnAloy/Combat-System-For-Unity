using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace InCombat
{
    public enum AttackState
    {
        Idle,
        Windup,
        Impact,
        Cooldown
    }
}

public class MeeleFighter : MonoBehaviour
{
    [field: SerializeField] public float Health { get; private set; } 
    private Animator animator;
    private Collider[] swordColliders;
    private SphereCollider rightHandCollider, leftHandCollider, rightFootCollider, leftFootCollider;
    private bool doCombo;
    private int comboCount = 0;
    
    [SerializeField] private List<AttackData> attacks;
    [SerializeField] private List<AttackData> longRangeAttacks;
    [SerializeField] private GameObject sword;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float longRangeAttackThreshold;
    public InCombat.AttackState AttackState { get; private set; }
    public bool InAction { get; private set; } = false;
    public List<AttackData> Attacks => attacks;
    public bool IsCounterable => AttackState == InCombat.AttackState.Windup && comboCount == 0;
    public bool InCounter { get; private set; } = false;
    public bool IsTakingHit { get; private set; } = false;
    public Action<MeeleFighter> OnGotHit;
    public Action OnGotHitComplete;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if(sword)
        {
            swordColliders = sword.GetComponents<Collider>();
            leftFootCollider = animator.GetBoneTransform(HumanBodyBones.LeftFoot).GetComponent<SphereCollider>();
            rightFootCollider = animator.GetBoneTransform(HumanBodyBones.RightFoot).GetComponent<SphereCollider>();
            leftHandCollider = animator.GetBoneTransform(HumanBodyBones.LeftHand).GetComponent<SphereCollider>();
            rightHandCollider = animator.GetBoneTransform(HumanBodyBones.RightHand).GetComponent<SphereCollider>();
            DisableAllHitboxes();
        }
    }

    public void TryToAttack(MeeleFighter target = null)
    {
        if(!InAction)
        {
            StartCoroutine(Attack(target));
        }
        else if(AttackState == InCombat.AttackState.Impact || AttackState == InCombat.AttackState.Cooldown)
        {
           doCombo = true;
        }
    }

    private MeeleFighter currentTarget;
    private IEnumerator Attack(MeeleFighter target = null)
    {
        InAction = true;
        currentTarget = target;
        AttackState = InCombat.AttackState.Windup;
        if (comboCount < 0 || comboCount >= attacks.Count)
            comboCount = 0;
        var attack = attacks[comboCount];
        
        var attackDirection = transform.forward;
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = Vector3.zero;
        if (target != null)
        {
            var vectorToTarget = target.transform.position - transform.position;
            vectorToTarget.y = 0;
            attackDirection = vectorToTarget.normalized;
            float distanceToTarget = vectorToTarget.magnitude - attack.DistanceFromTarget;
            if (distanceToTarget > longRangeAttackThreshold && longRangeAttacks.Count > 0)
            {
                attack = longRangeAttacks[0];
            }
            if (attack.MoveToTarget)
            {
                if (distanceToTarget < attack.MaxMoveDistance)
                {
                    targetPosition = target.transform.position - attackDirection * attack.DistanceFromTarget;
                }
                else
                {
                    targetPosition = startPosition + attackDirection * attack.MaxMoveDistance;
                }
            }
        }
        animator.CrossFade(attack.AnimationName, 0.2f);
        yield return null;
        var animatorState = animator.GetNextAnimatorStateInfo(1);
        float timer = 0;
        
        while(timer < animatorState.length)
        { 
            if(IsTakingHit)
            {
                break;
            }
            timer += Time.deltaTime;
            float normalizedTime = timer / animatorState.length;
            if(target != null && attack.MoveToTarget)
            {
                float moveTiming = (normalizedTime - attack.MoveStartTime) / (attack.MoveEndTime - attack.MoveStartTime);
                transform.position = Vector3.Lerp(startPosition, targetPosition, moveTiming);
            }
            if (attackDirection != null)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(attackDirection), rotationSpeed * Time.deltaTime);
            }
            if(AttackState == InCombat.AttackState.Windup)
            {
                if(InCounter)
                {
                    break;
                }
                if(normalizedTime >= attack.ImpactStartTime)
                {
                    AttackState = InCombat.AttackState.Impact;
                    EnableHitbox(attack);
                }
            }
            else if(AttackState == InCombat.AttackState.Impact)
            {
                if(normalizedTime >= attack.ImpactEndTime)
                {
                    AttackState = InCombat.AttackState.Cooldown;
                    DisableAllHitboxes();
                }
            }
            else if(AttackState == InCombat.AttackState.Cooldown)
            {
                if(doCombo)
                {
                    doCombo = false;
                    comboCount = (comboCount + 1) % attacks.Count;
                    StartCoroutine(Attack(target));
                    yield break;
                }
            }
            yield return null;
        }
        AttackState = InCombat.AttackState.Idle;
        //yield return new WaitForSeconds(animatorState.length);
        comboCount = 0;
        InAction = false;
        currentTarget = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Hitbox") && !IsTakingHit && !InCounter)
        {
            var attacker = other.GetComponentInParent<MeeleFighter>();
            if(attacker.currentTarget != this)  
            {
                return;
            }
            TakeDamage(5f);
            OnGotHit?.Invoke(attacker);
            if (Health > 0)
            {
                StartCoroutine(GetHitReaction(attacker));
            }
            else
            {
                PlayDeathAnimation(attacker);
            }
                
        }
    }

    private void TakeDamage(float damage)
    {
        Health = Mathf.Clamp(Health - damage, 0, Health);
    }

    public void Heal(float amount)
    {
        Health += amount;
    }

    private void PlayDeathAnimation(MeeleFighter attacker)
    {
        animator.CrossFade("Fallback Death", 0.2f);
    }

    private IEnumerator GetHitReaction(MeeleFighter attacker)
    {
        InAction = true;
        IsTakingHit = true;
        var disVector = attacker.transform.position - transform.position;
        disVector.y = 0;
        transform.rotation = Quaternion.LookRotation(disVector);
        
        animator.CrossFade("Sword Impact", 0.2f);
        yield return null;
        var animatorState = animator.GetNextAnimatorStateInfo(1);
        yield return new WaitForSeconds(animatorState.length * 0.8f);
        OnGotHitComplete?.Invoke();
        InAction = false;
        IsTakingHit = false;
    }

    public IEnumerator PerformCounterAttack(EnemyController opponent)
    {
        InAction = true;
        InCounter = true;
        opponent.Fighter.InCounter = true;
        opponent.ChangeState(EnemyStates.Dead);
        var facingVector = opponent.transform.position - transform.position;
        facingVector.y = 0;
        transform.rotation = Quaternion.LookRotation(facingVector);
        opponent.transform.rotation = Quaternion.LookRotation(-facingVector);
        var targetPosition = opponent.transform.position - facingVector.normalized * 1f;
        animator.CrossFade("CounterAttack", 0.2f);
        opponent.Animator.CrossFade("CounterAttackVictim", 0.2f);
        yield return null;
        var animatorState = animator.GetNextAnimatorStateInfo(1);
        float timer = 0f;
        while(timer < animatorState.length)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, 5f * Time.deltaTime); 
            yield return null;
            timer += Time.deltaTime;
        }
        InAction = false;
        InCounter = false;
        opponent.Fighter.InCounter = false;
    }

    private void DisableAllHitboxes()
    {
        foreach (var swordCollider in swordColliders)
        {
           swordCollider.enabled = false;
        }
        if(leftFootCollider != null)
            leftFootCollider.enabled = false;
        if(rightFootCollider != null)
            rightFootCollider.enabled = false;
        if(leftHandCollider != null)
            leftHandCollider.enabled = false;
        if(rightHandCollider != null)
            rightHandCollider.enabled = false;
    }

    private void EnableHitbox(AttackData attack)
    {
        switch (attack.UsedHitbox)
        {
            case AttackHitbox.Sword:
                foreach (var swordCollider in swordColliders)
                {
                    swordCollider.enabled = true;
                }
                break;
            case AttackHitbox.LeftFoot:
                leftFootCollider.enabled = true;
                break;
            case AttackHitbox.RightFoot:
                rightFootCollider.enabled = true;
                break;
            case AttackHitbox.LeftHand:
                leftHandCollider.enabled = true;
                break;
            case AttackHitbox.RightHand:
                rightHandCollider.enabled = true;
                break;
            default:
                break;
        }
    }
}
