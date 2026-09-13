using Inventory.Model;
using UnityEngine;

public class PickupSystem : MonoBehaviour
{
    [SerializeField] private InventorySO inventoryData;
    [SerializeField] private bool pickupOnTrigger;

    private void OnTriggerEnter(Collider other)
    {
        if (!pickupOnTrigger)
        {
            return;
        }

        Item item = other.GetComponentInParent<Item>();

        if (item != null)
        {
            TryPickup(item);
        }
    }

    public bool TryPickup(Item item)
    {
        if (item == null || item.InventoryItem == null)
        {
            return false;
        }

        if (inventoryData == null)
        {
            Debug.LogError("Inventory data is not assigned.", this);
            return false;
        }

        int requestedQuantity = item.Quantity;
        int remainingQuantity = inventoryData.AddItem(
            item.InventoryItem,
            requestedQuantity);

        if (remainingQuantity >= requestedQuantity)
        {
            return false;
        }

        if (remainingQuantity > 0)
        {
            item.Quantity = remainingQuantity;
            return false;
        }

        item.DestroyItem();
        return true;
    }
}