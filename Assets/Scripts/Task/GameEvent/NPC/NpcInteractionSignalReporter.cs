using GameDefinitions;
using GameplayEvents;
using UnityEngine;

namespace GameplayEvents.Npc
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NpcIdentity))]
    [RequireComponent(typeof(NpcInteractable))]
    public sealed class NpcInteractionSignalReporter : MonoBehaviour
    {
        [SerializeField]
        private NpcInteractionRequestedSignalSO interactionRequestedSignal;

        private NpcIdentity npcIdentity;
        private NpcInteractable npcInteractable;

        private void Awake()
        {
            npcIdentity = GetComponent<NpcIdentity>();
            npcInteractable = GetComponent<NpcInteractable>();
        }

        private void OnEnable()
        {
            npcInteractable.InteractionRequested += OnInteractionRequested;
        }

        private void OnDisable()
        {
            if (npcInteractable != null)
            {
                npcInteractable.InteractionRequested -= OnInteractionRequested;
            }
        }

        private void OnInteractionRequested(NpcInteractable npc, GameObject interactor)
        {
            if (interactionRequestedSignal == null)
            {
                Debug.LogError("NPC interaction requested signal is not assigned.", this);

                return;
            }

            if (npcIdentity == null || !npcIdentity.IsConfigured)
            {
                Debug.LogError("NPC identity is not configured.", this);
                return;
            }

            NpcInteractionRequestedEvent payload = new NpcInteractionRequestedEvent(npcIdentity.Definition, interactor, gameObject);

            GameEventHub.Publish(interactionRequestedSignal, payload);
        }
    }
}