using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Inventory.Model;
using Inventory.UI;
using UnityEngine;
using static Inventory.Model.InventorySO;

namespace Inventory
{
    public class InventoryController : MonoBehaviour
    {
        [SerializeField] private UIInventoryPage inventoryUI;

        [SerializeField] InventorySO inventoryData;
        [SerializeField] private CameraController cameraController;

        public List<InventoryItem> initialItem = new();


        private void Start()
        {
            PrepareUI();
            PrepInventoryData();
        }

        private void PrepInventoryData()
        {
            inventoryData.Initialize();
            inventoryData.OnInventoryUpdated += UpdateInventoryUI;
            foreach (var item in initialItem)
            {
                if (item.IsEmpty)
                {
                    continue;
                }
                inventoryData.AddItem(item);
            }
        }

        private void UpdateInventoryUI(Dictionary<int, InventoryItem> inventoryState)
        {
            inventoryUI.ResetAllItems();
            foreach (var item in inventoryState)
            {
                inventoryUI.UpdateData(item.Key, item.Value.item.ItemImage, item.Value.quantity);
            }
        }

        private void PrepareUI()
        {
            inventoryUI.InitializeInventoryUI(inventoryData.Size);
            inventoryUI.OnDescriptionRequest += HandleDescriptionRequest;
            inventoryUI.OnSwapItems += HandleSwapItems;
            inventoryUI.OnStartDragging += HandleDragging;
            inventoryUI.OnItemActionRequest += HandleItemActionRequest;
        }

        private string PrepareDescription(InventoryItem inventoryItem)
        {
            StringBuilder sb = new();
            sb.Append(inventoryItem.item.Description);
            sb.AppendLine();
            for (int i = 0; i < inventoryItem.itemState.Count; i++)
            {
                sb.Append($"{inventoryItem.itemState[i].itemParameter.ParameterName}" + $": {inventoryItem.itemState[i].Value} /" + $" {inventoryItem.item.DefaultParameters[i].Value}");

            }
            return sb.ToString();
        }
        private void HandleItemActionRequest(int itemIndex)
        {
            InventoryItem inventoryItem = inventoryData.GetItemAt(itemIndex);
            if (inventoryItem.IsEmpty)
                return;
            
            IItemAction itemAction = inventoryItem.item as IItemAction;
            if (itemAction != null)
            {

                inventoryUI.ShowItemAction(itemIndex);
                inventoryUI.AddAction(itemAction.ActionName, () => PerformAction(itemIndex));
            }
            IDestoryableItem destroyableItem = inventoryItem.item as IDestoryableItem;
            if (destroyableItem != null)
            {
                inventoryUI.AddAction("Drop", () => DropItem(itemIndex, inventoryItem.quantity));
            }
        }

        private void DropItem(int itemIndex, int quantity)
        {
            inventoryData.RemoveItem(itemIndex, quantity);
            inventoryUI.ResetSelection();
        }

        public void PerformAction(int itemIndex)
        {
            InventoryItem inventoryItem = inventoryData.GetItemAt(itemIndex);
            if (inventoryItem.IsEmpty)
                return;
            IDestoryableItem destroyableItem = inventoryItem.item as IDestoryableItem;
            if (destroyableItem != null)
            {
                inventoryData.RemoveItem(itemIndex, 1);
            }
            IItemAction itemAction = inventoryItem.item as IItemAction;
            if (itemAction != null)
            {
                itemAction.PerformAction(gameObject, null);
                if(inventoryData.GetItemAt(itemIndex).IsEmpty)
                {
                    inventoryUI.ResetSelection();
                }
            }
        }

        private void HandleDragging(int itemIndex)
        {
            InventoryItem inventoryItem = inventoryData.GetItemAt(itemIndex);
            if (inventoryItem.IsEmpty)
            {
                return;
            }
            inventoryUI.CreateDraggedItem(inventoryItem.item.ItemImage, inventoryItem.quantity);
        }

        private void HandleSwapItems(int itemIndex1, int itemIndex2)
        {
            inventoryData.SwapItems(itemIndex1, itemIndex2);
        }

        private void HandleDescriptionRequest(int itemIndex)
        {
            InventoryItem inventoryItem = inventoryData.GetItemAt(itemIndex);
            if (inventoryItem.IsEmpty)
            {
                inventoryUI.ResetSelection();
                return;

            }
            ItemSO item = inventoryItem.item;
            string description = PrepareDescription(inventoryItem);
            inventoryUI.UpdateDescription(itemIndex, item.ItemImage, item.name, description);
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                if (inventoryUI.isActiveAndEnabled == false)
                {
                    OpenInventory();
                }
                else
                {
                    CloseInventory();
                }
            }
        }

        private void CloseInventory()
        {
            inventoryUI.Hide();
            Time.timeScale = 1f;
            if (cameraController != null)
            {
                cameraController.SetCameraEnabled(true);
            }
        }

        private void OpenInventory()
        {
            inventoryUI.Show();
            foreach (var item in inventoryData.GetCurrentInventoryState())
            {
                inventoryUI.UpdateData(item.Key, item.Value.item.ItemImage, item.Value.quantity);
            }
            Time.timeScale = 0f;
            if (cameraController != null)
            {
                cameraController.SetCameraEnabled(false);
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}