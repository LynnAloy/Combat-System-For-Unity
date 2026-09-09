using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem
{
    [Serializable]
    public sealed class DialogueLine
    {
        [SerializeField] private string speaker;
        [SerializeField, TextArea(2, 6)] private string text;

        public string Speaker => speaker;
        public string Text => text;
    }

    [CreateAssetMenu(fileName = "Dialogue_New", menuName = "Dialogue/Dialogue Definition")]
    public sealed class DialogueSO : ScriptableObject
    {
        [SerializeField] private string dialogueId;
        [SerializeField] private List<DialogueLine> lines = new();

        public string DialogueId => dialogueId;
        public IReadOnlyList<DialogueLine> Lines => lines;

#if UNITY_EDITOR
        private void OnValidate()
        {
            dialogueId = dialogueId?.Trim() ?? string.Empty;
        }
#endif
    }
}