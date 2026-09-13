using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements.Experimental;


namespace Inventory.Model
{
    [CreateAssetMenu]
    public class InventorySO : ScriptableObject
    {
        [SerializeField] private List<InventoryItem> inventoryItems;

        [field: SerializeField]
        public int Size { get; private set; } = 10;

        public event Action<Dictionary<int, InventoryItem>> OnInventoryUpdated;



        public void Initialize()
        {
            inventoryItems = new List<InventoryItem>();
            for (int i = 0; i < Size; i++)
            {
                inventoryItems.Add(InventoryItem.GetEmptyItem());
            }
        }

        public int AddItem(ItemSO item, int quantity, List<ItemParameter> itemState = null)
        {
            if (item.IsStackable == false)
            {
                for (int i = 0; i < inventoryItems.Count && quantity > 0; i++)
                {
                    while (quantity > 0 && IsInventoryFull() == false)
                    {
                        quantity -= AddItemToFirstSlot(item, 1, itemState);
                    }
                }
                InformAboutChange();
                return quantity;
            }
            quantity = AddStackableItem(item, quantity);
            InformAboutChange();
            return quantity;
        }

        private int AddItemToFirstSlot(ItemSO item, int quantity, List<ItemParameter> itemState = null)
        {
            InventoryItem newItem = new InventoryItem
            {
                item = item,
                quantity = quantity,
                itemState = new(itemState == null ? item.DefaultParameters : itemState)
            };

            for (int i = 0; i < inventoryItems.Count; i++)
            {
                if (inventoryItems[i].IsEmpty)
                {
                    inventoryItems[i] = newItem;
                    return quantity;
                }
            }
            return 0;
        }

        private bool IsInventoryFull()
        => inventoryItems.Where(item => item.IsEmpty).Any() == false;

        private int AddStackableItem(ItemSO item, int quantity)
        {
            for (int i = 0; i < inventoryItems.Count; i++)
            {
                if (inventoryItems[i].IsEmpty)
                {
                    continue;
                }
                if (inventoryItems[i].item.ID == item.ID)
                {
                    int amountPossibleToTake =
                        inventoryItems[i].item.MaxStackSize - inventoryItems[i].quantity;

                    if (quantity > amountPossibleToTake)
                    {
                        inventoryItems[i] = inventoryItems[i]
                            .ChangeQuantity(inventoryItems[i].item.MaxStackSize);
                        quantity -= amountPossibleToTake;
                    }
                    else
                    {
                        inventoryItems[i] = inventoryItems[i]
                            .ChangeQuantity(inventoryItems[i].quantity + quantity);
                        return 0;
                    }
                }
            }
            while (quantity > 0 && IsInventoryFull() == false)
            {
                int newQuantity = Mathf.Clamp(quantity, 0, item.MaxStackSize);
                quantity -= newQuantity;
                AddItemToFirstSlot(item, newQuantity);
            }
            return quantity;
        }

        public Dictionary<int, InventoryItem> GetCurrentInventoryState()
        {
            Dictionary<int, InventoryItem> returnValue = new();
            for (int i = 0; i < inventoryItems.Count; i++)
            {
                if (inventoryItems[i].IsEmpty)
                {
                    continue;
                }
                returnValue[i] = inventoryItems[i];
            }
            return returnValue;
        }

        public int GetItemQuantity(ItemSO targetItem)
        {
            if (targetItem == null || inventoryItems == null)
            {
                return 0;
            }

            int totalQuantity = 0;

            foreach (InventoryItem inventoryItem in inventoryItems)
            {
                if (inventoryItem.IsEmpty || inventoryItem.item != targetItem)
                {
                    continue;
                }

                totalQuantity += inventoryItem.quantity;
            }

            return totalQuantity;
        }

        public InventoryItem GetItemAt(int itemIndex)
        {
            return inventoryItems[itemIndex];
        }

        public void AddItem(InventoryItem item)
        {
            AddItem(item.item, item.quantity);
        }

        public void SwapItems(int itemIndex1, int itemIndex2)
        {
            InventoryItem item1 = inventoryItems[itemIndex1];
            inventoryItems[itemIndex1] = inventoryItems[itemIndex2];
            inventoryItems[itemIndex2] = item1;
            InformAboutChange();
        }

        private void InformAboutChange()
        {
            OnInventoryUpdated?.Invoke(GetCurrentInventoryState());
        }

        public void RemoveItem(int itemIndex, int amount)
        {
            if (inventoryItems.Count > itemIndex)
            {
                if (inventoryItems[itemIndex].IsEmpty)
                {
                    return;
                }
                int reminder = inventoryItems[itemIndex].quantity - amount;
                if (reminder <= 0)
                {
                    inventoryItems[itemIndex] = InventoryItem.GetEmptyItem();
                }
                else
                {
                    inventoryItems[itemIndex] = inventoryItems[itemIndex]
                        .ChangeQuantity(reminder);
                }
                InformAboutChange();
            }
        }

        public bool TryRemoveItem(ItemSO targetItem, int amount)
        {
            if (targetItem == null ||
                amount <= 0 ||
                inventoryItems == null)
            {
                return false;
            }

            if (GetItemQuantity(targetItem) < amount)
            {
                return false;
            }

            int remaining = amount;

            for (int i = 0; i < inventoryItems.Count && remaining > 0; i++)
            {
                InventoryItem inventoryItem = inventoryItems[i];

                if (inventoryItem.IsEmpty ||
                    inventoryItem.item != targetItem)
                {
                    continue;
                }

                int removedAmount = Mathf.Min(
                    inventoryItem.quantity,
                    remaining);

                int newQuantity =
                    inventoryItem.quantity - removedAmount;

                if (newQuantity <= 0)
                {
                    inventoryItems[i] = InventoryItem.GetEmptyItem();
                }
                else
                {
                    inventoryItems[i] =
                        inventoryItem.ChangeQuantity(newQuantity);
                }

                remaining -= removedAmount;
            }

            InformAboutChange();
            return remaining == 0;
        }

        [Serializable]
        //scure data
        public struct InventoryItem
        {
            public int quantity;
            public ItemSO item;
            public List<ItemParameter> itemState;
            public bool IsEmpty => item == null;

            public InventoryItem ChangeQuantity(int newQuatity)
            {
                return new()
                {
                    item = this.item,
                    quantity = newQuatity,
                    itemState = new(this.itemState)
                };
            }

            public static InventoryItem GetEmptyItem()
                => new()
                {
                    item = null,
                    quantity = 0
                };
        }
    }

}