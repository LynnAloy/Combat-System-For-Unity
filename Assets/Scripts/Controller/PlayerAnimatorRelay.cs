using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class PlayerAnimatorRelay : MonoBehaviour
{
    private Animator animator;
    private CombatController combatController;
    private PlayerController playerController;
    private PlayerRushSkill rushSkill;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        combatController = GetComponentInParent<CombatController>();
        playerController = GetComponentInParent<PlayerController>();
        rushSkill = GetComponentInParent<PlayerRushSkill>();
    }

    private void OnAnimatorMove()
    {
        if (combatController != null)
        {
            combatController.ApplyAnimatorMove(animator);
        }
    }

    public void AnimationEvent_WeaponHandoff()
    {
        if (playerController != null)
        {
            playerController.AnimationEvent_WeaponHandoff();
        }
    }

    public void AnimationEvent_RushSkillHit(AnimationEvent animationEvent)
    {
        if (rushSkill != null)
        {
            rushSkill.AnimationEvent_RushSkillHit(animationEvent);
        }
    }
}
