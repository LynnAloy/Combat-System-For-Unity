using System;
using GameDefinitions;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(NpcIdentity))]
public sealed class NpcInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private bool interactionEnabled = true;
    [SerializeField] private string interactionVerb = "交谈";

    [Header("Temporary Inspector Event")]
    [SerializeField] private UnityEvent onInteracted;

    private NpcIdentity npcIdentity;

    public event Action<NpcInteractable, GameObject> InteractionRequested;

    public NpcDefinitionSO Definition => npcIdentity.Definition;

    public string NpcId
    {
        get
        {
            return Definition != null ? Definition.PersistentId : string.Empty;
        }
    }

    public string DisplayName
    {
        get
        {
            return Definition != null ? Definition.DisplayName : "未配置 NPC";
        }
    }

    public Transform InteractionPoint
    {
        get
        {
            return interactionPoint != null ? interactionPoint : transform;
        }
    }

    private void Awake()
    {
        npcIdentity = GetComponent<NpcIdentity>();
    }

    public bool CanInteract(GameObject interactor)
    {
        return interactionEnabled &&
               isActiveAndEnabled &&
               gameObject.activeInHierarchy &&
               npcIdentity != null &&
               npcIdentity.IsConfigured;
    }

    public string GetInteractionPrompt(GameObject interactor)
    {
        return $"与 {DisplayName} {interactionVerb}";
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        Debug.Log(
            $"Interaction requested with NPC '{DisplayName}'.",
            this);

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
        interactionVerb = interactionVerb?.Trim() ?? string.Empty;

        NpcIdentity identity = GetComponent<NpcIdentity>();

        if (identity == null || !identity.IsConfigured)
        {
            Debug.LogWarning(
                "NpcInteractable requires a configured NpcIdentity.",
                this);
        }
    }
#endif
}