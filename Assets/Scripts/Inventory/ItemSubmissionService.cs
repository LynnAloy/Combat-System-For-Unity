using System;
using GameplayEvents;
using GameplayEvents.Inventory;
using Inventory.Model;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ItemSubmissionService : MonoBehaviour
{
    [SerializeField] private InventorySO inventoryData;

    [Header("Signals")]
    [SerializeField]
    private ItemSubmissionRequestedSignalSO submissionRequestedSignal;

    [SerializeField] private ItemSubmittedSignalSO itemSubmittedSignal;

    private IDisposable requestSubscription;

    private void OnEnable()
    {
        if (inventoryData == null)
        {
            Debug.LogError(
                "Inventory data is not assigned.",
                this);

            return;
        }

        if (submissionRequestedSignal == null)
        {
            Debug.LogError(
                "Submission requested signal is not assigned.",
                this);

            return;
        }

        if (itemSubmittedSignal == null)
        {
            Debug.LogError(
                "Item submitted signal is not assigned.",
                               this);

            return;
        }

        requestSubscription = GameEventHub.Subscribe(
            submissionRequestedSignal,
            OnSubmissionRequested);
    }

    private void OnDisable()
    {
        requestSubscription?.Dispose();
        requestSubscription = null;
    }

    private void OnSubmissionRequested(
        ItemSubmissionRequestedEvent request)
    {
        if (request.Item == null || request.Quantity <= 0)
        {
            PublishResult(
                request,
                false,
                "Invalid item submission request.");

            return;
        }

        bool removed = inventoryData.TryRemoveItem(
            request.Item,
            request.Quantity);

        if (!removed)
        {
            PublishResult(
                request,
                false,
                $"Not enough '{request.Item.Name}' in inventory.");

            return;
        }

        PublishResult(request, true, null);
    }

    private void PublishResult(
        ItemSubmissionRequestedEvent request,
        bool succeeded,
        string failureReason)
    {
        ItemSubmittedEvent payload = new ItemSubmittedEvent(
            request,
            succeeded,
            failureReason);

        GameEventHub.Publish(
            itemSubmittedSignal,
            payload);
    }
}