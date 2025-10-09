using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class StateMachine<T>
{
    public State<T> CurrentState { get; private set; }
    private T _owner;
    public StateMachine(T owner)
    {
        _owner = owner;
    }
    public void ChangeState(State<T> newState)
    {
        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState.Enter(_owner);
    }

    public void Execute()
    {
        CurrentState?.Execute();
    }
}
