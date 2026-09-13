using UnityEngine;

namespace GameDefinitions
{
    [DisallowMultipleComponent]
    public sealed class NpcIdentity : MonoBehaviour
    {
        [SerializeField] private NpcDefinitionSO definition;

        public NpcDefinitionSO Definition => definition;
        public bool IsConfigured => definition != null;
    }
}