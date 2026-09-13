using System;
using UnityEngine;

namespace GameplayEvents
{
    public abstract class TaskSignalSO : ScriptableObject
    {
        [SerializeField, TextArea(2, 5)] private string description;

        public string Description => description;
        public abstract Type PayloadType { get; }
    }

    public abstract class GameplaySignalSO<TPayload> : TaskSignalSO
    {
        public sealed override Type PayloadType => typeof(TPayload);
    }
}