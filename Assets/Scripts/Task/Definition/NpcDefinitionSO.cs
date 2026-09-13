using UnityEngine;

namespace GameDefinitions
{
    [CreateAssetMenu(fileName = "NPC_New", menuName = "Game Definitions/NPC")]
    public sealed class NpcDefinitionSO : GameDefinitionSO
    {
        [SerializeField] private Sprite portrait;

        public Sprite Portrait => portrait;
    }
}