using DialogueSystem;
using GameDefinitions;
using UnityEngine;

namespace GameplayEvents.Npc
{
    [RequireComponent(typeof(NpcIdentity))]
    [RequireComponent(typeof(NpcDialogue))]
    public sealed class NpcDialogueSignalReporter : MonoBehaviour
    {
        [SerializeField] private NpcConversationSignalSO conversationCompletedSignal;

        private NpcIdentity npcIdentity;
        private NpcDialogue npcDialogue;

        private void Awake()
        {
            npcIdentity = GetComponent<NpcIdentity>();
            npcDialogue = GetComponent<NpcDialogue>();
        }

        private void OnEnable()
        {
            npcDialogue.ConversationCompleted += OnConversationCompleted;
        }

        private void OnDisable()
        {
            if (npcDialogue != null)
            {
                npcDialogue.ConversationCompleted -= OnConversationCompleted;
            }
        }

        private void OnConversationCompleted(NpcDialogue completedDialogue)
        {
            if (conversationCompletedSignal == null)
            {
                Debug.LogError("NPC conversation signal is not assigned.", this);
                return;
            }

            if (!npcIdentity.IsConfigured)
            {
                Debug.LogError("NPC definition is not assigned.", this);
                return;
            }

            NpcConversationEvent payload = new NpcConversationEvent(
                npcIdentity.Definition,
                completedDialogue.Dialogue,
                gameObject);

            GameEventHub.Publish(conversationCompletedSignal, payload);
        }
    }
}