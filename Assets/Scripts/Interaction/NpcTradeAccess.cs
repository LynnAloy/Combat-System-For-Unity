using System;
using GameplayEvents;
using GameplayEvents.Tasks;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class NpcTradeAccess : MonoBehaviour
{
    [SerializeField] private TaskCompletedSignalSO unlockSignal;
    [SerializeField] private bool unlockedByDefault;
    [SerializeField] private UnityEvent onTradeUnlocked;

    private IDisposable subscription;
    private bool unlocked;

    public bool CanPurchase => unlocked;

    private void Awake()
    {
        unlocked = unlockedByDefault;
    }

    private void OnEnable()
    {
        if (unlockSignal == null)
        {
            Debug.LogError("Trade unlock signal is not assigned.", this);

            return;
        }

        subscription = GameEventHub.Subscribe(unlockSignal, OnUnlockTaskCompleted);
    }

    private void OnDisable()
    {
        subscription?.Dispose();
        subscription = null;
    }

    public void Unlock()
    {
        if (unlocked)
        {
            return;
        }

        unlocked = true;

        Debug.Log($"Trading unlocked by '{unlockSignal.name}'.", this);

        onTradeUnlocked?.Invoke();
    }

    private void OnUnlockTaskCompleted(TaskCompletedEvent payload)
    {
        Unlock();
    }
}