using System;
using System.Collections.Generic;
using Inventory.Model;
using UnityEngine;

namespace FlexibleTaskSystem
{
    [Serializable]
    public sealed class AcquireItemTask : GameTask, ITaskNavigationSource
    {
        [SerializeField] private InventorySO inventoryData;
        [SerializeField] private ItemSO targetItem;
        [SerializeField, Min(1)] private int requiredQuantity = 1;

        [NonSerialized] private bool subscribed;

        protected override void OnStart()
        {
            if (inventoryData == null)
            {
                Fail("Inventory data is not assigned.");
                return;
            }

            if (targetItem == null)
            {
                Fail("Target item is not assigned.");
                return;
            }

            requiredQuantity = Mathf.Max(1, requiredQuantity);

            inventoryData.OnInventoryUpdated += OnInventoryUpdated;
            subscribed = true;

            RefreshProgress();
        }

        protected override void OnEnd(GameTaskState finalState)
        {
            Unsubscribe();
        }

        protected override void OnReset()
        {
            Unsubscribe();
        }

        private void OnInventoryUpdated(Dictionary<int, InventorySO.InventoryItem> inventoryState)
        {
            RefreshProgress();
        }

        private void RefreshProgress()
        {
            int currentQuantity = inventoryData.GetItemQuantity(targetItem);

            SetProgress(currentQuantity, requiredQuantity);

            if (currentQuantity >= requiredQuantity)
            {
                Complete();
            }
        }

        public bool TryGetNavigationTarget(out TaskNavigationTarget target)
        {
            target = TaskNavigationTarget.FromDefinition(targetItem);
            return targetItem != null;
        }

        private void Unsubscribe()
        {
            if (!subscribed || inventoryData == null)
            {
                return;
            }

            inventoryData.OnInventoryUpdated -= OnInventoryUpdated;
            subscribed = false;
        }
    }
}