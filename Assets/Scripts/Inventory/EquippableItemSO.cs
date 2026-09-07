using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Inventory.Model

{
    [CreateAssetMenu]
    public class EquippableItemSO : ItemSO, IDestoryableItem, IItemAction
    {
        public string ActionName => "Equip";
        public AudioClip ActionSFX { get; private set; }
        public bool PerformAction(GameObject character, List<ItemParameter> itemState = null)
        {
            // Implement the logic to equip the item to the character
            // For example, you might want to change the character's appearance or stats
            return true;
        }

    }
}