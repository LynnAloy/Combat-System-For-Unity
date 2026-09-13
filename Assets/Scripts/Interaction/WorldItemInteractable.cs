using Inventory.Model;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Item))]
public sealed class WorldItemInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private bool interactionEnabled = true;

    private Item item;

    public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;

    private void Awake()
    {
        item = GetComponent<Item>();
    }

    public bool CanInteract(GameObject interactor)
    {
        return interactionEnabled &&
               item != null &&
               item.InventoryItem != null &&
               interactor != null &&
               interactor.GetComponent<PickupSystem>() != null;
    }

    public string GetInteractionPrompt(GameObject interactor)
    {
        string itemName = item != null && item.InventoryItem != null ? item.InventoryItem.Name : "物品";

        return $"拾取 {itemName}";
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        PickupSystem pickupSystem = interactor.GetComponent<PickupSystem>();

        if (pickupSystem.TryPickup(item))
        {
            interactionEnabled = false;
        }
    }
}