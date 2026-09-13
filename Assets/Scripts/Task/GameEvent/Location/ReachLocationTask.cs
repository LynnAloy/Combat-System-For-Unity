using System;
using GameDefinitions;
using GameplayEvents;
using GameplayEvents.Location;
using UnityEngine;

namespace FlexibleTaskSystem
{
    [Serializable]
    public sealed class ReachLocationTask : GameTask
    {
        [SerializeField] private LocationEnteredSignalSO locationEnteredSignal;
        [SerializeField] private LocationDefinitionSO targetLocation;

        [NonSerialized] private IDisposable subscription;

        protected override void OnStart()
        {
            if (locationEnteredSignal == null)
            {
                Fail("Location entered signal is not assigned.");
                return;
            }

            if (targetLocation == null)
            {
                Fail("Target location is not assigned.");
                return;
            }

            subscription = GameEventHub.Subscribe(
                locationEnteredSignal,
                OnLocationEntered);
        }

        protected override void OnEnd(GameTaskState finalState)
        {
            ReleaseSubscription();
        }

        protected override void OnReset()
        {
            ReleaseSubscription();
        }

        private void OnLocationEntered(LocationEnteredEvent payload)
        {
            if (payload.Location != targetLocation)
            {
                return;
            }

            Complete();
        }

        private void ReleaseSubscription()
        {
            subscription?.Dispose();
            subscription = null;
        }
    }
}