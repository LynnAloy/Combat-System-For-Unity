using System;
using UnityEngine;

namespace FlexibleTaskSystem
{
    [Serializable]
    public class WaitTask : GameTask
    {
        [SerializeField] private float duration = 1.0f;

        private float elapsed;

        protected override void OnStart()
        {
            elapsed = 0.0f;

            if(duration <= 0.0f)
            {
                Complete();
            }
        }

        protected override void OnTick(float deltaTime)
        {
            elapsed += Time.deltaTime;

            if(elapsed >= duration)
            {
                Complete();
            }
        }

        protected override void OnReset()
        {
            elapsed = 0.0f;
        }
    }
}