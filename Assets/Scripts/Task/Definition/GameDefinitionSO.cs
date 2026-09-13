using System;
using UnityEngine;

namespace GameDefinitions
{
    public abstract class GameDefinitionSO : ScriptableObject
    {
        [SerializeField, HideInInspector] private string persistentId;
        [SerializeField] private string displayName;

        public string PersistentId => persistentId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(persistentId))
            {
                persistentId = Guid.NewGuid().ToString("N");
            }

            displayName = displayName?.Trim() ?? string.Empty;
        }

        [ContextMenu("Regenerate Persistent ID")]
        private void RegeneratePersistentId()
        {
            persistentId = Guid.NewGuid().ToString("N");
        }
#endif
    }
}