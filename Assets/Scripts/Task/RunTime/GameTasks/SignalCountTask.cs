using System;
using UnityEngine;

namespace FlexibleTaskSystem
{
    [Serializable]
    public sealed class SignalCountTask : GameTask
    {
        [SerializeField] private string signalName = "EnemyKilled";
        [SerializeField, Min(1)] private int requiredCount = 1;

        private int currentCount;

        protected override void OnStart()
        {
            currentCount = 0;
            SetProgress(currentCount, requiredCount);
            Context.Signals.Subscribe(signalName, OnSignal);
        }

        protected override void OnEnd(GameTaskState finalState)
        {
            Context.Signals.Unsubscribe(signalName, OnSignal);
        }

        protected override void OnReset()
        {
            currentCount = 0;
        }

        private void OnSignal(object payload)
        {
            currentCount++;
            SetProgress(currentCount, requiredCount);

            if (currentCount >= requiredCount)
            {
                Complete();
            }
        }
    }
}