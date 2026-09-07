using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class NpcInteractable : MonoBehaviour, IInteractable
{
    [Header("Identity")]
    [SerializeField] private string npcId = "npc_example";
    [SerializeField] private string displayName = "NPC";

    [Header("Interaction")]
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private bool interactionEnabled = true;
    [SerializeField] private string interactionVerb = "交谈";

    [Header("Temporary Inspector Event")]
    [SerializeField] private UnityEvent onInteracted;

    public event Action<NpcInteractable, GameObject> InteractionRequested;

    public string NpcId => npcId;
    public string DisplayName => displayName;

    public Transform InteractionPoint
    {
        get
        {
            return interactionPoint != null ? interactionPoint : transform;
        }
    }

    public bool CanInteract(GameObject interactor)
    {
        return interactionEnabled && isActiveAndEnabled && gameObject.activeInHierarchy;
    }

    public string GetInteractionPrompt(GameObject interactor)
    {
        return $"与 {displayName} {interactionVerb}";
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        Debug.Log($"Interaction requested with NPC '{npcId}'.", this);

        InteractionRequested?.Invoke(this, interactor);
        onInteracted?.Invoke();
    }

    public void SetInteractionEnabled(bool enabled)
    {
        interactionEnabled = enabled;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        npcId = npcId.Trim();
        displayName = displayName.Trim();
        interactionVerb = interactionVerb.Trim();

        if (string.IsNullOrWhiteSpace(npcId))
        {
            Debug.LogWarning("NPC requires a stable and unique NPC ID.", this);
        }
    }
#endif
}