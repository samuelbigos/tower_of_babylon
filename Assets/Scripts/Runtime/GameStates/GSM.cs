using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GSM : Singleton<GSM>
{
    private IGameState _currentState;
    public IGameState CurrentState => _currentState;

    private IGameState[] _states;

    protected override void Awake()
    {
        base.Awake();
        
        _states = GetComponents<IGameState>();
    }

    public void ManualUpdate()
    {
        foreach (IGameState state in _states)
        {
            if (state == _currentState)
                continue;
            
            if (state.ShouldEnter(_currentState))
            {
                if (_currentState != null)
                    Debug.Log($"Changing state from {_currentState.GetType().Name} to {state.GetType().Name}");

                _currentState?.OnExit(state);
                state.OnEnter(_currentState);
                _currentState = state;
            }
        }
        
        if (_currentState != null)
            _currentState.ManualUpdate();
    }
}
