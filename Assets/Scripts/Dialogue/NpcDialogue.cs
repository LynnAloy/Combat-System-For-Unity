using System;
using UnityEngine;

namespace DialogueSystem
{
    [RequireComponent(typeof(NpcInteractable))]
    public sealed class NpcDialogue : MonoBehaviour
    {
        [SerializeField] private DialogueController dialogueController;
        [SerializeField] private DialogueSO dialogue;

        private NpcInteractable npcInteractable;

        public string NpcId => npcInteractable.NpcId;
        public DialogueSO Dialogue => dialogue;

        public event Action<NpcDialogue> ConversationCompleted;

        private void Awake()
        {
            npcInteractable = GetComponent<NpcInteractable>();
        }

        private void OnEnable()
        {
            if (npcInteractable == null)
            {
                npcInteractable = GetComponent<NpcInteractable>();
            }

            npcInteractable.InteractionRequested += OnInteractionRequested;
        }

        private void OnDisable()
        {
            if (npcInteractable != null)
            {
                npcInteractable.InteractionRequested -=
                    OnInteractionRequested;
            }
        }

        public void SetDialogue(DialogueSO newDialogue)
        {
            dialogue = newDialogue;
        }

        private void OnInteractionRequested(
            NpcInteractable npc,
            GameObject interactor)
        {
            if (dialogueController == null)
            {
                Debug.LogError(
                    $"NPC '{NpcId}' has no DialogueController.",
                    this);
                return;
            }

            if (dialogue == null)
            {
                Debug.LogError(
                    $"NPC '{NpcId}' has no dialogue asset.",
                    this);
                return;
            }

            dialogueController.TryStartDialogue(
                dialogue,
                OnConversationCompleted);
        }

        private void OnConversationCompleted()
        {
            ConversationCompleted?.Invoke(this);
        }
    }
}